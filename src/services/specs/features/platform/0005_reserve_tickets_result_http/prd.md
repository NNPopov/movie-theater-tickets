# PRD — `ReserveTickets` `Result → HTTP` conversion (ADR-002, step 3)

Slice: `0005_reserve_tickets_result_http` · Module: `platform` · Status: 📋 Started

## Problem Statement

As a client developer (and as a service developer reasoning about the error path) of the
BookingManagement service, the "reserve the seats currently held in my shopping cart"
operation is the **most broken** of the remaining `ShoppingCarts` use-cases with respect to
ADR-002: it advertises the `Result` model on its command, yet runs entirely on exceptions,
re-throws a real domain `Result` as a bare `Exception`, and returns the `Result` object
straight out of the endpoint so that **failures are reported as `200 OK`**.

- The `ReserveTickets` command handler is typed `IRequest<Result>`, but every business failure
  on the path is either a `throw` or is collapsed into one:
  - shopping cart not found ⇒ `throw new ContentNotFoundException(...)` (`404` centrally);
  - the aggregate transition guard (already-purchased) ⇒ `ShoppingCart.SeatsReserve()` calls
    `EnsurePurchaseIsNotCompleted()`, which `throw`s `ConflictException` (`409` centrally);
  - **a failing seat reservation `Result` from `MovieSessionSeatService.ReserveSeats` is
    re-thrown as `throw new Exception("Couldn't Reserve ...")`** — a bare `Exception`, the exact
    anti-pattern ADR-002 forbids (defect #2), which discards the domain `Error` and collapses a
    legitimate "seat already taken" conflict to a generic **`500`**;
  - "sales terminated" inside the shared `MovieSessionSeatService.CheckSeatSaleAvailability` is a
    bare `throw new Exception(...)` ⇒ **`500`** (ADR-002 defect, deferred by slice 0004).
- The HTTP endpoint (`POST {cart}/reservations`) does `return result;` — it returns the
  **`Result` object itself**. On success this serializes `Result.Success()` to a `200` JSON
  body; and were a failing `Result` ever to reach it (it cannot today, because the handler
  throws first), it too would serialize to **`200`**. There is **no `Match`-to-HTTP** and the
  endpoint never reaches the shared `ErrorResults.ToProblem` mapper introduced by slice 0003.
- `ShoppingCart.SeatsReserve()` carries a **latent domain bug independent of the error model**:
  it appends `ShoppingCartReservedDomainEvent` **unconditionally**, even when the
  `if (Status == InWork)` guard does not fire — so a no-op "reserve" on a cart that is already
  `SeatsReserved` still emits a `ShoppingCartReservedDomainEvent`.
- The endpoint's OpenAPI surface is wrong: it declares `.Produces<bool>(201)` / `.Produces(204)`,
  neither of which matches the actual `200`-with-`Result`-body behaviour, and the `404`/`409`
  the path really produces (via thrown exceptions) are not declared at all.

ADR-002 ("`Result` for expected outcomes, exceptions for the unexpected") is explicitly
**incremental**. Steps 1 (the `Result<T>` infrastructure, slice `0001`) and 2 (the
`ContentNotFoundException` `204 → 404` contract, slice `0002`) are done; step 3 — "per touched
slice: remove the endpoint `Result → exception` bridge, convert expected-failure paths to
`Result` + `Match`-to-HTTP, replace bare `throw new Exception(...)`" — landed its **canonical**
reference in slice `0003` (`AssignClientCart`, with the shared `ErrorResults.ToProblem` mapper)
and its **second** conversion in slice `0004` (`SelectSeats`). This slice is the **third**
step-3 conversion. It is the first to remove a **bare-`Exception` bridge in the handler**
(not just at the endpoint), the first to convert a **shared** domain-service helper
(`CheckSeatSaleAvailability`) to `Result`, and the first whose conversion **changes observable
failure statuses** (`500 → 409`) because the current path genuinely mis-reports those failures.

## Solution

After this slice the `reserve-tickets` operation reports its expected outcomes through **one**
model — `Result` matched straight to HTTP via the shared `ErrorResults.ToProblem` — and stops
re-throwing, swallowing, and mis-statusing them:

- The handler genuinely **returns a failing `Result`** for every expected business outcome on
  the path, and **short-circuits before persistence**:
  - shopping cart not found ⇒ `NotFoundError` ⇒ `404`;
  - movie session not found (shared helper) ⇒ `NotFoundError` ⇒ `404`;
  - sales terminated (shared helper) ⇒ `ConflictError` ⇒ `409` (**was `500`**);
  - a seat is not reservable / held by another cart (`ReserveSeats` ⇒ `MovieSessionSeat.Reserve`)
    ⇒ `ConflictError` ⇒ `409` (**was `500`** via the bare-`Exception` bridge);
  - the cart is already purchased (`SeatsReserve`) ⇒ `ConflictError` ⇒ `409` (was a thrown
    `ConflictException`, same status).
- **The bare `throw new Exception("Couldn't Reserve ...")` in the handler is deleted.** The
  failing `Result` from `ReserveSeats` is propagated unchanged (`return result;`) and resolved
  at the endpoint.
- **`ShoppingCart.SeatsReserve()` is converted from `void` to `Result`** — the ADR-002 flagship
  case (an in-aggregate state transition that raises a domain event), following slice 0003's
  `AssignClientId` template:
  - `InWork` ⇒ transition to `SeatsReserved`, append `ShoppingCartReservedDomainEvent`,
    `Result.Success()`;
  - already `SeatsReserved` ⇒ **idempotent `Result.Success()`, with no duplicate event** (this
    also fixes the unconditional-event bug — the event is now appended **only on a genuine
    transition**);
  - `PurchaseCompleted` ⇒ **`ConflictError`** (replacing the `EnsurePurchaseIsNotCompleted()`
    `throw`), `409` via the mapper.
  The shared `EnsurePurchaseIsNotCompleted()` helper (still used by `PurchaseComplete` and
  `CalculateCartAmount`) is **not** modified; `SeatsReserve` stops calling it and inlines a
  `Result`-returning guard.
- **`MovieSessionSeatService.CheckSeatSaleAvailability` is retyped from `Task` (void) to
  `Task<Result>`:** movie-session-not-found ⇒ `NotFoundError`; sales-terminated ⇒ `ConflictError`
  (replacing the bare `Exception`). Its callers `SelSeats`, `ReserveSeats`, and `SelectSeat`
  consume the new `Result` and short-circuit on `IsFailure`. Because `SelectSeat` is the already
  green slice-0004 path, **slice 0004's `SelectSeatCommandHandlerTests` is re-run as a regression
  gate** — its observable behaviour is unchanged (its tests never exercised the terminated
  branch).
- The endpoint resolves the handler's `Result` with `Match(() => Results.Ok(), ErrorResults.ToProblem)`
  — the **same shared mapper** from slice 0003 — and `return result;` is removed. Success is
  `200 OK` with an **empty body** (previously a serialized `Result` object). `.Produces` is
  corrected to `200`/`404`/`409`; the stale `201`/`204` are dropped.
- **Atomicity is preserved and made explicit.** The handler short-circuits and returns the
  failing `Result` **before** `_activeShoppingCartRepository.SaveAsync(cart)` and the lifecycle
  side-effects (`SetAsync`, the per-seat selection-lifecycle deletes), so a cart is never
  persisted as `SeatsReserved` when a seat could not actually be reserved — the invariant the
  thrown path provided implicitly. The pre-existing seat-then-cart persistence ordering inside
  `ReserveSeats`/the handler (a known, separate transactional gap) is **not** changed by this
  slice.

Unlike slice 0004, this conversion is **not** purely status-preserving — and that is the point.
The genuinely broken failure statuses are corrected:

- seat-not-reservable on reserve: **`500 → 409`** (was the bare-`Exception` bridge);
- sales terminated: **`500 → 409`** (was the bare-`Exception` in the shared helper);
- success response body: a serialized `Result` object **⇒ empty body** (status stays `200`).

Status is **preserved** for: cart-not-found (`404`), movie-session-not-found (`404`), and
already-purchased (`409`). The genuinely unexpected faults (repository/Redis/infrastructure)
continue to propagate as exceptions to `CustomExceptionHandler` and `500`.

ADR-002 stays **Proposed**; this slice implements its step 3 for one more use-case, per the
ADR's own incremental plan. (Flipping ADR-002 to Accepted and updating `agent_docs/error_handling.md`
is deferred to slice `0006`, which completes the `ShoppingCarts` conversion with `PurchaseTickets`.)

## User Stories

1. As a cinema customer, I want reserving the seats held in my shopping cart to succeed with `200 OK`, so that I know my seats are reserved before I purchase.
2. As a client developer, I want reserving against a shopping cart id that does not exist to return `404 Not Found`, so that a bad cart id is reported as missing rather than as an opaque error.
3. As a client developer, I want reserving against a movie session that does not exist to return `404 Not Found`, so that a missing session is reported consistently with a missing cart.
4. As a client developer, I want reserving seats in a movie session whose sales have been terminated to return `409 Conflict` (not `500`), so that "you can no longer reserve here" is an explicit business outcome rather than a server error.
5. As a client developer, I want reserving a seat that is not reservable (already taken / held by another shopping cart) to return `409 Conflict` (not `500`), so that seat contention is reported as a conflict instead of an opaque server error.
6. As a client developer, I want reserving against a shopping cart that has already been purchased to return `409 Conflict`, so that re-reserving a completed cart is reported as a conflict (unchanged status, now via `Result`).
7. As a client developer, I want every `404`/`409` from this endpoint to carry a `ProblemDetails` body identical in shape to every other `404`/`409` in the service, so that my client parses one response shape.
8. As a client developer, I want all the conflict outcomes on this path (terminated, seat-taken, already-purchased) produced by the *same* mechanism and `ProblemDetails` shape, so that I handle "conflict" uniformly regardless of which rule was violated.
9. As the Flutter client, I want a successful reservation to remain `200`, so that existing success handling keeps working, while accepting that the success response body becomes empty.
10. As a service developer, I want the `ReserveTickets` handler to *return* a failing `Result` for cart-not-found, session-not-found, terminated, seat-not-reservable, and already-purchased, instead of throwing or swallowing them, so that its `IRequest<Result>` signature stops being a lie.
11. As a service developer, I want the bare `throw new Exception("Couldn't Reserve ...")` in the handler deleted and the failing `Result` from `ReserveSeats` propagated unchanged, so that a real domain `Error` is no longer discarded and downgraded to `500`.
12. As a service developer, I want the endpoint to resolve the handler's `Result` via `Match(() => Results.Ok(), ErrorResults.ToProblem)` instead of `return result;`, so that failures stop serializing as `200` and the request runs one error pass through the shared mapper.
13. As a service developer, I want the handler's cart-not-found check to return `NotFoundError` instead of throwing `ContentNotFoundException`, so that it matches the 0003 canonical template and its sibling `PurchaseTickets`.
14. As a domain developer, I want `ShoppingCart.SeatsReserve()` converted from `void` to `Result`, returning `ConflictError` for the already-purchased case instead of throwing `ConflictException`, so that the in-aggregate transition expresses its expected failure as a value (the ADR-002 flagship case).
15. As a domain developer, I want `SeatsReserve()` to append `ShoppingCartReservedDomainEvent` **only on a genuine `InWork → SeatsReserved` transition**, so that the unconditional-event bug is fixed and the event is raised exactly when the cart actually transitioned.
16. As a domain developer, I want calling `SeatsReserve()` on a cart that is already `SeatsReserved` to be an idempotent `Result.Success()` with **no** duplicate event, so that a repeated reserve is safe and does not emit a second reservation event.
17. As a domain developer, I want `SeatsReserve()` to stop calling the shared `EnsurePurchaseIsNotCompleted()` (and inline its own `Result`-returning guard), so that the shared helper still used by `PurchaseComplete`/`CalculateCartAmount` is left unchanged.
18. As a service developer, I want `MovieSessionSeatService.CheckSeatSaleAvailability` retyped from `Task` to `Task<Result>`, returning `NotFoundError` for movie-session-not-found and `ConflictError` for sales-terminated, so that the shared helper stops throwing a bare `Exception` and reports both outcomes as values.
19. As a service developer, I want `SelSeats`, `ReserveSeats`, and `SelectSeat` to consume `CheckSeatSaleAvailability`'s new `Result` and short-circuit on `IsFailure`, so that the retyped helper is threaded through all three call sites consistently.
20. As a maintainer, I want slice 0004's `SelectSeatCommandHandlerTests` re-run as a regression gate after the shared-helper retype, so that the already-green `SelectSeats` path is proven unchanged.
21. As a service developer, I want the handler to short-circuit on any failing `Result` and return it **before** `SaveAsync`, `SetAsync`, and the per-seat selection-lifecycle deletes, so that a cart is never persisted as `SeatsReserved` when a seat could not be reserved (the atomicity invariant the thrown path provided implicitly).
22. As a service developer, I want the seat-not-found lookup in `GetMovieSessionSeat` to remain a thrown `ContentNotFoundException` (`404`) in this slice, so that a shared helper used by the not-yet-converted paths is not changed pre-emptively, and a missing seat record for a valid session stays a data-integrity exception rather than a business outcome.
23. As a service developer, I want genuinely unexpected faults (repository failures, the Redis seat/cart lifecycle managers) to continue propagating as exceptions to `CustomExceptionHandler` and `500`, so that infrastructure faults stay unexpected, not business `Result`s.
24. As an API consumer reading the OpenAPI document, I want the `reservations` endpoint to declare `200`, `404`, and `409`, so that the published contract matches runtime behaviour.
25. As an API consumer, I want the stale `.Produces<bool>(201)`/`.Produces(204)` declarations on that endpoint removed, so that the OpenAPI document is not wrong in the other direction.
26. As a maintainer, I want this slice to reuse slice 0003's shared `ErrorResults.ToProblem` mapper rather than introduce a new one, so that the `Result`-to-HTTP policy stays in exactly one place.
27. As a maintainer, I want the `ReserveTickets` handler pinned by a unit test (cart missing ⇒ `NotFoundError`; session missing ⇒ `NotFoundError`; terminated ⇒ `ConflictError`; seat not reservable ⇒ `ConflictError`; already-purchased ⇒ `ConflictError`; success ⇒ `Result.Success()` with cart saved, lifecycle set, and per-seat selection-lifecycle entries deleted; and — pinning atomicity — on any failure the cart save and lifecycle side-effects are **not** invoked), so that the conversion and the atomicity invariant are both covered.
28. As a maintainer, I want `ShoppingCart.SeatsReserve()` pinned by a domain unit test (`InWork` ⇒ `SeatsReserved` + event; already `SeatsReserved` ⇒ `Result.Success()`, no event; `PurchaseCompleted` ⇒ `ConflictError`, no event), so that the `void → Result`, idempotency, and event-on-success-only changes are covered.
29. As a maintainer, I want the architecture tests (`Domain.ArchitectureTests`) and the full suite to stay green, so that the conversion honours the structural rules.
30. As a service developer, I want this slice to convert *only* `ReserveTickets` (plus the shared `CheckSeatSaleAvailability` it must touch and the `SeatsReserve` aggregate method), leaving `PurchaseTickets` for slice 0006, so that the change lands small and reviewable.

## Implementation Decisions

**Nature of the slice.** ADR-002 **step 3**, third conversion, reusing slice 0003's shared
`Error → IResult` mapper. It converts the `ReserveTickets` use-case (a `ShoppingCarts` command
that drives the `ShoppingCart.SeatsReserve` transition and the `MovieSessionSeat.Reserve`
transition behind the seat repository and the Redis cart/seat lifecycle managers) and is filed
under the `platform` module alongside `0001`–`0004` to keep the ADR-002 series together.
Settled in a grill-me interview; runs the full spec chain.

**Conversion depth.** Convert the endpoint, the handler, the shared domain-service helper
`CheckSeatSaleAvailability`, and the aggregate method `ShoppingCart.SeatsReserve`. The shared
`GetMovieSessionSeat` helper and the shared `EnsurePurchaseIsNotCompleted` guard are **not**
modified.

**Use-cases / aggregates touched.**

- **`ReserveTickets` command (use-case, modified).** Contract stays `IRequest<Result>`. The
  handler returns `NotFoundError` when the cart is missing, consumes the `Result` from
  `SeatsReserve` and from `ReserveSeats` and short-circuits on `IsFailure` *before* any
  persistence/lifecycle side-effect, and the bare `throw new Exception(...)` is removed.
- **`ShoppingCart.SeatsReserve` aggregate (modified).** Retyped `void → Result`. `InWork`
  ⇒ transition + event + success; already `SeatsReserved` ⇒ idempotent success, no event;
  `PurchaseCompleted` ⇒ `ConflictError`. Stops calling `EnsurePurchaseIsNotCompleted`; the event
  is appended only on a genuine transition.
- **`MovieSessionSeatService.CheckSeatSaleAvailability` (shared domain service, modified).**
  Retyped `Task → Task<Result>`. Session-not-found ⇒ `NotFoundError`; sales-terminated ⇒
  `ConflictError` (replacing the bare `Exception`). Its three callers (`SelSeats`, `ReserveSeats`,
  `SelectSeat`) consume the `Result` and short-circuit.
- **`reservations` endpoint (modified).** Replaces `return result;` with
  `Match(() => Results.Ok(), ErrorResults.ToProblem)`; `.Produces` corrected to `200`/`404`/`409`.

**Failure-path classification (the contract this slice locks in).**

| Failure | Mechanism before | Status before | Mechanism after | Status after |
|---|---|---|---|---|
| Shopping cart not found (handler-local) | `throw ContentNotFoundException` | 404 | **`Result` `NotFoundError`** | 404 |
| Movie session not found (shared helper) | `throw ContentNotFoundException` | 404 | **`Result` `NotFoundError`** | 404 |
| Sales terminated (shared helper) | `throw new Exception(...)` | **500** | **`Result` `ConflictError`** | **409** |
| Seat not reservable / another cart (`Reserve`) | bare `throw new Exception` (handler bridge) | **500** | **`Result` `ConflictError`** | **409** |
| Cart already purchased (`SeatsReserve`) | `throw ConflictException` | 409 | **`Result` `ConflictError`** | 409 |
| Movie session seat not found (`GetMovieSessionSeat`) | `throw ContentNotFoundException` | 404 | exception (unchanged) | 404 |
| Repository / Redis lifecycle fault | exception | 500 | exception (unchanged) | 500 |
| Success | `return result;` (serialized `Result` body) | 200 + body | **`Results.Ok()`** (empty body) | 200 |

**Mapping policy.** Reuses `ErrorResults.ToProblem` (slice 0003): `NotFoundError ⇒ 404`,
`ConflictError ⇒ 409`, unrecognised `Error ⇒ 500`. No new `Error` types are introduced; `Error`
definitions stay centralised in `Domain/Error`. `MovieSessionSeat.Reserve` already returns
`ConflictError` for its bad-status case, so the seat-conflict path maps to `409` with no
`InvalidOperation → 500` trap (the hazard slice 0004 had to defuse for `Select` does **not**
exist on the `Reserve` path).

**Result type.** Non-generic `Result` throughout — the reservation does not need to carry a
value back to the caller (success is an empty `200`).

**Atomicity.** The handler returns a failing `Result` before `SaveAsync` and before the
lifecycle side-effects (`IShoppingCartLifecycleManager.SetAsync`, the per-seat
`IShoppingCartSeatLifecycleManager.DeleteAsync` calls), reproducing the implicit guarantee of
the previous thrown path. The pre-existing seat-then-cart persistence ordering inside
`ReserveSeats` (seats are `UpdateRangeAsync`-persisted before the cart save) is a separate,
known transactional gap and is **not** addressed here.

**Behaviour change (intentional).** This slice deliberately corrects two failure statuses that
the current code gets wrong (`500 → 409` for seat-not-reservable and for sales-terminated) and
changes the success body from a serialized `Result` object to empty. These are the explicit
client-visible changes; everything else is status-preserving. Per the ADR's "modifying an
existing slice" flow, `tests.md` is updated first and the handler gate is driven red → green.

**ADR status.** ADR-002 stays **Proposed**; flipping it to Accepted and updating
`agent_docs/error_handling.md` rides with slice `0006` (`PurchaseTickets`), which completes the
`ShoppingCarts` conversion.

**Explicitly deferred (NOT in this slice).**
- Converting `PurchaseTickets` and its endpoint `Match` wiring — slice `0006`. Note `0006` must
  also handle `MovieSessionSeat.Sell`'s `InvalidOperation` branch (a base `Error`), which is the
  0004-style `409 → 500` trap on the *purchase* path.
- Converting the shared `GetMovieSessionSeat` (seat-not-found) to `Result` — it stays a thrown
  `ContentNotFoundException` (`404`); a missing seat record for a valid session is treated as a
  data-integrity exception.
- The bare `throw` in the endpoints' `GetClientId` / `CreateShoppingCart` helpers (a
  malformed-identity / idempotency-key concern, not part of this error-model conversion).
- Adding a `400`-class arm to `ErrorResults.ToProblem` or otherwise changing the shared mapper /
  introducing a new `Error` kind (ADR-level).
- The pre-existing seat-then-cart transactional ordering gap inside `ReserveSeats`.
- The Flutter client follow-up to the `0002` `204 → 404` contract change — orthogonal, lives in
  the `clients/` tree, tracked separately.
- Standing up a `WebApplicationFactory<Program>` HTTP harness, or a real-concurrency
  two-carts-racing-the-same-seat integration test.

## Testing Decisions

**What makes a good test here.** The externally observable behaviour is the `Result` each
outcome of the use-case produces (and therefore the status the shared mapper yields), the
`SeatsReserve` transition's outcome and its event, and the atomicity invariant (no persistence
on failure). Tests assert that external behaviour — the returned `Result`/`Error`, the raised
(or not raised) event, whether the cart was saved and the lifecycle touched — not internal
wiring. As with `0001`–`0004`, and because the repository has **no `WebApplicationFactory<Program>`
HTTP harness**, the change is pinned with **focused unit tests of the changed units** rather than
an end-to-end HTTP test. The handler unit test is the slice's RED acceptance gate.

**Units under test.**
- **`ReserveTickets` handler (the acceptance / RED gate):** with the repositories
  (`IActiveShoppingCartRepository`), the `MovieSessionSeatService` collaborators (the seat /
  movie-session repositories behind it), and the Redis lifecycle managers
  (`IShoppingCartLifecycleManager`, `IShoppingCartSeatLifecycleManager`) substituted (mocked).
  Asserts: cart missing ⇒ `NotFoundError`; movie session missing ⇒ `NotFoundError`; sales
  terminated ⇒ `ConflictError`; a seat not reservable ⇒ `ConflictError`; already-purchased cart
  ⇒ `ConflictError`; success ⇒ `Result.Success()` with cart saved, lifecycle set, and the
  per-seat selection-lifecycle entries deleted; and — pinning the atomicity invariant — on a
  failing reservation the cart save and lifecycle side-effects are **not** invoked. Red until the
  handler genuinely returns these `Result`s instead of throwing/swallowing.
- **`ShoppingCart.SeatsReserve` (domain):** `InWork` ⇒ status `SeatsReserved` and
  `ShoppingCartReservedDomainEvent` raised; already `SeatsReserved` ⇒ `Result.Success()` and
  **no** event; `PurchaseCompleted` ⇒ `ConflictError` and no event. Pins the `void → Result`,
  idempotency, and event-on-success-only changes.
- **Regression:** slice `0004`'s `SelectSeatCommandHandlerTests` is re-run unchanged to prove the
  shared-helper retype (`CheckSeatSaleAvailability → Task<Result>`) did not alter the already-green
  `SelectSeats` path.

**Prior art.** Slice `0004`'s `SelectSeatCommandHandlerTests` (the closest handler-gate template,
same mocked-collaborator shape) and slice `0003`'s `AssignClientCartCommandHandlerTests`; slice
`0003`'s `ErrorResultsOutsideInTests` already covers the shared mapper (no need to re-test the
mapping); `BookingManagementService.Domain.UnitTests` (`ShoppingCarts/ShoppingCartSpecification.cs`)
for the AAA / `*Specification` domain conventions used by the `SeatsReserve` domain test.

**Out of the net (by decision):** no `WebApplicationFactory` end-to-end test (no harness exists;
the endpoint's `Match` wiring is covered by compilation and the shared mapper by slice 0003); a
real-concurrency test (two carts racing the same seat, exercising the seat repository / lifecycle)
is deferred to a separate Infrastructure-level integration test, not this slice's gate; no tests
for `PurchaseTickets` or any out-of-scope use-case.

## Out of Scope

- Converting any use-case other than `ReserveTickets` (`PurchaseTickets` is slice `0006`).
- Converting the shared `GetMovieSessionSeat` (seat-not-found) helper to `Result`.
- Modifying the shared `EnsurePurchaseIsNotCompleted` guard (still used by `PurchaseComplete` /
  `CalculateCartAmount`).
- Adopting `Result<T>` (the generic) on this path — `ReserveTickets` uses the non-generic
  `Result`; success carries no value.
- Adding a `400`-class arm to `ErrorResults.ToProblem` or otherwise changing the shared mapper /
  introducing a new `Error` kind (ADR-level).
- The bare `throw` in the `GetClientId` / `CreateShoppingCart` endpoint helpers.
- Fixing the pre-existing seat-then-cart transactional ordering gap inside `ReserveSeats`.
- Changing `CustomExceptionHandler`, the MediatR pipeline, the validation behaviour, or any base
  type.
- Standing up a `WebApplicationFactory` HTTP integration harness, or a real-concurrency seat-race
  integration test.
- Flipping ADR-002 to Accepted and updating `agent_docs/error_handling.md` (rides with slice
  `0006`).
- The Flutter client follow-up to the `0002` `204 → 404` contract change.
- Publishing this PRD to a remote issue tracker — `gh` is not installed and no triage-label
  vocabulary was provided, so the `needs-triage` step could not run; the PRD is stored locally
  instead (same as slices `0001`–`0004`).

## Further Notes

- This is the third step-3 conversion and the most behaviour-correcting of them: it is the first
  to delete a **bare-`Exception` bridge in the handler** (`throw new Exception("Couldn't Reserve")`),
  the first to convert a **shared** domain-service helper (`CheckSeatSaleAvailability`, hit by all
  three seat operations), and the first whose conversion intentionally **changes observable failure
  statuses** (`500 → 409` for seat-not-reservable and sales-terminated) because the current path
  genuinely mis-reports them.
- Unlike slice 0004, there is **no `InvalidOperation → 500` trap** on this path:
  `MovieSessionSeat.Reserve` already returns `ConflictError` for its bad-status case. (That trap
  *does* exist on the purchase path via `MovieSessionSeat.Sell`'s `InvalidOperation` branch — it
  is slice `0006`'s problem, flagged here so it is not forgotten.)
- The shared-helper retype is the riskiest edit because it touches the already-green `SelectSeats`
  path. The mitigation is mechanical: thread the `Result` through all three callers and re-run
  slice 0004's handler test as a regression gate. The terminated branch was never exercised by
  0004's tests, so 0004's observable behaviour is unchanged.
- The `SeatsReserve` unconditional-event bug (event raised even on a no-op transition) is fixed
  as a side-effect of the `void → Result` conversion: the event moves onto the genuine-transition
  branch, and the already-`SeatsReserved` case becomes an idempotent success with no event
  (user stories 15/16, pinned by the domain test in user story 28).
- The atomicity invariant (user stories 21/27) is the part most easily lost in a throw→return
  refactor: the thrown/bare-`Exception` path aborted before the cart save and lifecycle writes;
  the returned path must short-circuit explicitly before all of them.
- Verification per `CLAUDE.md`: `dotnet format`, `dotnet build -warnaserror`, `dotnet test`.
  As recorded in MEMORY (`dotnet10-migration`), the build trips the accepted AutoMapper `NU1903`
  NuGet-audit advisory under `-warnaserror` at restore time; handle NuGet audit accordingly so the
  real build/warnings are what is validated. The working .NET 10 SDK is the x86 install at
  `C:\Program Files (x86)\dotnet\dotnet.exe`. No EF Core model change in this slice → no migration.
