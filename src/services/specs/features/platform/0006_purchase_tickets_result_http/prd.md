# PRD — `PurchaseTickets` `Result → HTTP` conversion + ADR-002 adoption close-out (step 3, final)

Slice: `0006_purchase_tickets_result_http` · Module: `platform` · Status: 📋 Started

## Problem Statement

As a client developer (and as a service developer reasoning about the error path) of the
BookingManagement service, the "purchase the seats held in my shopping cart" operation is the
**last unconverted** `ShoppingCarts` use-case with respect to ADR-002, and after slice `0005` it
is also the operation that most visibly demonstrates the cost of a half-converted path: its
handler already *returns* `Result`s for most failures, but its endpoint still serializes them as
`200 OK`.

- The HTTP endpoint (`POST {cart}/purchase`) does `return result;` — it returns the **`Result`
  object itself**. After slice `0005` retyped the shared `MovieSessionSeatService.CheckSeatSaleAvailability`
  to `Task<Result>`, the purchase handler (which calls `SelSeats`) now genuinely **returns**
  failing `Result`s for movie-session-not-found, sales-terminated, and every seat-level failure —
  but because the endpoint never reaches `Match`/`ErrorResults.ToProblem`, **all of them serialize
  as `200 OK` with a `Result` body**. This is the interim regression slice `0005` explicitly
  accepted and handed to `0006`:
  - shopping cart not found ⇒ handler returns `NotFoundError` ⇒ serialized **`200`** (should be `404`);
  - movie session not found (shared helper) ⇒ `NotFoundError` ⇒ **`200`** (should be `404`);
  - sales terminated (shared helper) ⇒ `ConflictError` ⇒ **`200`** (should be `409`);
  - a seat already sold (`MovieSessionSeat.Sell`) ⇒ `ConflictError` ⇒ **`200`** (should be `409`);
  - a seat held by another cart (`MovieSessionSeat.Sell`) ⇒ **`InvalidOperation`** base `Error` ⇒
    **`200`** (and even under `Match` would map to **`500`**, not `409` — the purchase-path twin of
    the `Select` `InvalidOperation → 500` trap slice `0004` had to defuse).
- `MovieSessionSeat.Sell` mislabels the "another shopping cart" case as a base `InvalidOperation`
  error (defect): every sibling seat transition — `Select`, `Reserve` — returns a `ConflictError`
  for the identical "the place is already being processed by another shopping cart" condition, so
  `Sell` is the one path where seat contention would be reported as `500` instead of `409` once the
  endpoint is matched to HTTP.
- `ShoppingCart.PurchaseComplete()` is still `void`, `throw`s `ConflictException` (via the shared
  `EnsurePurchaseIsNotCompleted()` guard) for an already-purchased cart, and carries the **same
  unconditional-event bug** slice `0005` fixed in `SeatsReserve`: it appends
  `ShoppingCartPurchaseDomainEvent` **even when the `if (Status == SeatsReserved)` guard does not
  fire** — so completing a purchase on a cart that is not in `SeatsReserved` still emits the
  purchase event without a state transition (and the handler then persists it).
- The endpoint's OpenAPI surface is wrong: it declares `.Produces<bool>(201)` / `.Produces(204)`,
  neither of which matches the actual `200`-with-`Result`-body behaviour, and the `404`/`409` the
  path should produce are not declared at all.
- **The cross-cutting decision is still recorded as undecided.** ADR-002 is `Proposed`;
  `agent_docs/error_handling.md` still says "two models coexist"; and `CLAUDE.md` (loaded into every
  session, overriding everything) still states in rule #9 and the project-at-a-glance that "the
  error model is not yet unified" and "the canonical choice is not yet decided." With the entire
  `ShoppingCarts` write path converted after this slice, those statements become stale and
  actively misleading to future sessions.

ADR-002 ("`Result` for expected outcomes, exceptions for the unexpected") is explicitly
**incremental**. Steps 1 (the `Result<T>` infrastructure, slice `0001`) and 2 (the
`ContentNotFoundException` `204 → 404` contract, slice `0002`) are done; step 3 converted
`AssignClientCart` (`0003`, the canonical reference that introduced the shared
`ErrorResults.ToProblem` mapper), `SelectSeats` (`0004`), and `ReserveTickets` (`0005`). This
slice is the **fourth and final** step-3 conversion: it finishes the `ShoppingCarts` write path
with `PurchaseTickets`, closes the interim purchase-path regression `0005` parked, defuses the
`Sell` `InvalidOperation → 500` trap, and — because the write path is then complete — **carries
ADR-002 to Accepted** and reconciles the three docs that still call the model undecided.

## Solution

After this slice the `purchase-tickets` operation reports its expected outcomes through **one**
model — `Result` matched straight to HTTP via the shared `ErrorResults.ToProblem` — and ADR-002
is **Accepted**, with `agent_docs/error_handling.md` and `CLAUDE.md` updated to match.

- The endpoint resolves the handler's `Result` with `Match(() => Results.Ok(), ErrorResults.ToProblem)`
  — the **same shared mapper** from slice `0003` — and `return result;` is removed. Success is
  `200 OK` with an **empty body** (previously a serialized `Result` object). `.Produces` is
  corrected to `200`/`404`/`409`; the stale `201`/`204` are dropped. This single change closes the
  whole interim regression: cart-not-found ⇒ `404`, session-not-found ⇒ `404`, terminated ⇒ `409`,
  seat-already-sold ⇒ `409`.
- **`MovieSessionSeat.Sell`'s "another shopping cart" case is retyped from `InvalidOperation` to
  `ConflictError`**, so seat contention on the purchase path maps to `409` through the existing
  mapper, exactly like `Select`/`Reserve` already do for the identical condition. No new arm is
  added to `ErrorResults.ToProblem` (that would be an ADR-level change); `InvalidOperation` keeps
  meaning "genuinely unexpected ⇒ `500`."
- **`ShoppingCart.PurchaseComplete()` is converted from `void` to `Result`** — the ADR-002
  flagship case (an in-aggregate state transition that raises a domain event), following the
  `SeatsReserve` template settled in slice `0005`:
  - `SeatsReserved` (genuine) ⇒ transition to `PurchaseCompleted`, append
    `ShoppingCartPurchaseDomainEvent`, `Result.Success()`;
  - already `PurchaseCompleted` ⇒ **idempotent `Result.Success()`, with no duplicate event** —
    a deliberate **`409 → 200`** change at the domain-method level (a retried completion of an
    already-complete purchase is the idempotent identity: no transition, no event, no double
    side-effect);
  - any other status (`InWork` not yet reserved, `Deleted`) ⇒ **`ConflictError`** (`409`), which
    also **fixes the unconditional-event bug** — the event is now appended **only on a genuine
    `SeatsReserved → PurchaseCompleted` transition**.
  The shared `EnsurePurchaseIsNotCompleted()` helper (still used by `CalculateCartAmount` and
  others) is **not** modified; `PurchaseComplete` stops calling it and inlines a `Result`-returning
  guard. The `Ensure.NotEmpty(ClientId)` precondition stays a **throw** — by purchase time the cart
  must have an assigned client (guaranteed since slice `0003`), so an empty `ClientId` here is an
  invariant violation / bug (`500`-class), not an expected business outcome.
- The handler **consumes `PurchaseComplete()`'s `Result` and short-circuits before persistence**:
  it returns the failing `Result` before `_activeShoppingCartRepository.SaveAsync(cart)`, the
  `IShoppingCartLifecycleManager.DeleteAsync` lifecycle removal, and the per-seat
  `IShoppingCartSeatLifecycleManager.DeleteAsync` calls — so a cart is never persisted as
  `PurchaseCompleted` when the completion was not legal. (The handler's cart-not-found
  `NotFoundError` and `SelSeats` `Result` short-circuit are already in place from prior work; this
  slice only adds the `PurchaseComplete` short-circuit and removes the `void` call.)
- **ADR-002 is flipped to `Accepted`** (dated 2026-06-04); `agent_docs/error_handling.md` is
  rewritten from "two models coexist / undecided" to the decided hybrid (expected outcome ⇒
  `Result`; in-aggregate transition that raises an event ⇒ `Result`; structural validation ⇒
  `ValidationBehaviour`/`ValidationException`; unexpected/infrastructure ⇒ exception ⇒
  `CustomExceptionHandler`; the endpoint `Result → exception` bridge is gone); and **`CLAUDE.md`
  rule #9 and the project-at-a-glance line are amended** from "not yet unified / undecided" to
  "decided — see ADR-002," stating the hybrid split. The tails left as intentional exception usage
  (see Out of Scope) are named in `agent_docs/error_handling.md` so they are not mistaken for
  un-migrated debt.

This conversion **changes observable failure statuses** — and that is the point. The purchase
path's failures stop being silently reported as `200`:

- cart not found / session not found: **`200 → 404`**;
- sales terminated / seat already sold: **`200 → 409`**;
- seat held by another cart: **`200 → 409`** (and avoids the `InvalidOperation → 500` trap via the
  `Sell` retype);
- success response body: a serialized `Result` object **⇒ empty body** (status stays `200`).

Status changes at the domain-method level for already-purchased completion (**`409 → 200`,
idempotent**); see Further Notes for how this interacts with the seat-level `Sold` guard at the
endpoint. The genuinely unexpected faults (repository / Redis lifecycle / `ClientId`-empty
invariant) continue to propagate as exceptions to `CustomExceptionHandler` and `500`.

## User Stories

1. As a cinema customer, I want purchasing the seats held in my shopping cart to succeed with `200 OK`, so that I know my tickets are bought.
2. As a client developer, I want purchasing against a shopping cart id that does not exist to return `404 Not Found` (not `200`), so that a bad cart id is reported as missing rather than as a fake success.
3. As a client developer, I want purchasing against a movie session that does not exist to return `404 Not Found` (not `200`), so that a missing session is reported consistently with a missing cart.
4. As a client developer, I want purchasing seats in a movie session whose sales have been terminated to return `409 Conflict` (not `200`), so that "you can no longer buy here" is an explicit business outcome rather than a fake success.
5. As a client developer, I want purchasing a seat that has already been sold to return `409 Conflict` (not `200`), so that buying an already-sold seat is reported as a conflict.
6. As a client developer, I want purchasing a seat that is held by another shopping cart to return `409 Conflict` (not `200`, and not `500`), so that seat contention on the purchase path is reported as a conflict, exactly like it is on the select and reserve paths.
7. As a client developer, I want every `404`/`409` from this endpoint to carry a `ProblemDetails` body identical in shape to every other `404`/`409` in the service, so that my client parses one response shape.
8. As a client developer, I want all the conflict outcomes on this path (terminated, already-sold, another-cart) produced by the *same* mechanism and `ProblemDetails` shape, so that I handle "conflict" uniformly regardless of which rule was violated.
9. As the Flutter client, I want a successful purchase to remain `200`, so that existing success handling keeps working, while accepting that the success response body becomes empty.
10. As a service developer, I want the `purchase` endpoint to resolve the handler's `Result` via `Match(() => Results.Ok(), ErrorResults.ToProblem)` instead of `return result;`, so that failures stop serializing as `200` and the request runs one error pass through the shared mapper.
11. As a service developer, I want the `purchase` endpoint to reuse slice `0003`'s shared `ErrorResults.ToProblem` mapper rather than introduce a new one, so that the `Result`-to-HTTP policy stays in exactly one place.
12. As a domain developer, I want `MovieSessionSeat.Sell`'s "another shopping cart" case retyped from `InvalidOperation` to `ConflictError`, so that seat contention maps to `409` via the existing mapper and the purchase-path `InvalidOperation → 500` trap is removed without changing the shared mapper.
13. As a domain developer, I want `InvalidOperation` to keep mapping to `500` (no new mapper arm), so that it continues to mean "genuinely unexpected" and the ADR-level mapper policy is untouched.
14. As a domain developer, I want `ShoppingCart.PurchaseComplete()` converted from `void` to `Result`, returning `ConflictError` for an illegal completion instead of throwing `ConflictException`, so that the in-aggregate transition expresses its expected failure as a value (the ADR-002 flagship case).
15. As a domain developer, I want `PurchaseComplete()` to append `ShoppingCartPurchaseDomainEvent` **only on a genuine `SeatsReserved → PurchaseCompleted` transition**, so that the unconditional-event bug is fixed and the event is raised exactly when the cart actually transitioned.
16. As a domain developer, I want calling `PurchaseComplete()` on a cart that is already `PurchaseCompleted` to be an idempotent `Result.Success()` with **no** duplicate event, so that a retried completion is safe and does not emit a second purchase event or re-run side-effects.
17. As a domain developer, I want calling `PurchaseComplete()` on a cart that is not `SeatsReserved` and not `PurchaseCompleted` (e.g. `InWork`, `Deleted`) to return `ConflictError` with no event, so that a purchase cannot complete from a status that never reserved the seats.
18. As a domain developer, I want `PurchaseComplete()` to keep `Ensure.NotEmpty(ClientId)` as a **throw**, so that a cart reaching purchase without an assigned client is treated as an invariant violation (a bug ⇒ `500`), not as an expected business outcome.
19. As a domain developer, I want `PurchaseComplete()` to stop calling the shared `EnsurePurchaseIsNotCompleted()` (and inline its own `Result`-returning guard), so that the shared helper still used by `CalculateCartAmount` and others is left unchanged.
20. As a service developer, I want the `PurchaseTickets` handler to consume `PurchaseComplete()`'s `Result` and short-circuit on `IsFailure`, so that its `IRequest<Result>` signature stops being a lie for the completion step.
21. As a service developer, I want the handler to return any failing `Result` **before** `SaveAsync`, the cart-lifecycle `DeleteAsync`, and the per-seat selection-lifecycle deletes, so that a cart is never persisted as `PurchaseCompleted` when the completion was not legal (the atomicity invariant the thrown path provided implicitly).
22. As a service developer, I want the seat-not-found lookup in `GetMovieSessionSeat` to remain a thrown `ContentNotFoundException` (`404`) in this slice, so that a missing seat record for a valid session stays a data-integrity exception rather than a business outcome (consistent with `0005`).
23. As a service developer, I want genuinely unexpected faults (repository failures, the Redis seat/cart lifecycle managers, the `ClientId`-empty invariant) to continue propagating as exceptions to `CustomExceptionHandler` and `500`, so that infrastructure/invariant faults stay unexpected, not business `Result`s.
24. As an API consumer reading the OpenAPI document, I want the `purchase` endpoint to declare `200`, `404`, and `409`, so that the published contract matches runtime behaviour.
25. As an API consumer, I want the stale `.Produces<bool>(201)`/`.Produces(204)` declarations on that endpoint removed, so that the OpenAPI document is not wrong in the other direction.
26. As a maintainer, I want the `PurchaseTickets` handler pinned by a unit test (cart missing ⇒ `NotFoundError`; session missing ⇒ `NotFoundError`; terminated ⇒ `ConflictError`; seat held by another cart ⇒ `ConflictError`; success ⇒ `Result.Success()` with cart saved, cart-lifecycle removed, and per-seat selection-lifecycle entries deleted; and — pinning atomicity — on any failure the cart save and lifecycle side-effects are **not** invoked), so that the conversion and the atomicity invariant are both covered.
27. As a maintainer, I want `ShoppingCart.PurchaseComplete()` pinned by a domain unit test (`SeatsReserved` ⇒ `PurchaseCompleted` + event; already `PurchaseCompleted` ⇒ `Result.Success()`, no event; `InWork` ⇒ `ConflictError`, no event), so that the `void → Result`, idempotency, and event-on-success-only changes are covered.
28. As a maintainer, I want `MovieSessionSeat.Sell` pinned by a domain unit test for the "another shopping cart" case returning a `ConflictError` (not `InvalidOperation`), so that the retype is covered and cannot silently regress to `500`.
29. As a maintainer, I want slice `0004`'s `SelectSeatCommandHandlerTests` and slice `0005`'s `ReserveTicketsCommandHandlerTests` re-run unchanged as regression gates, so that the `Sell`/`PurchaseComplete` changes are proven not to disturb the already-green select and reserve paths.
30. As a maintainer, I want the architecture tests (`Domain.ArchitectureTests`) and the full suite to stay green, so that the conversion honours the structural rules.
31. As a maintainer, I want ADR-002 flipped to `Accepted` (2026-06-04) in this slice, so that the now-complete write-path decision is recorded as settled.
32. As a future contributor, I want `agent_docs/error_handling.md` rewritten from "two models coexist / undecided" to the decided hybrid, so that the guidance I read matches the accepted ADR.
33. As a future contributor, I want `CLAUDE.md` rule #9 and the project-at-a-glance line amended from "not yet unified / undecided" to "decided — see ADR-002," so that the always-loaded instructions stop telling me the choice is open.
34. As a future contributor, I want the deliberately un-converted tails (read/query `ContentNotFoundException`, `GetMovieSessionSeat`) documented in `agent_docs/error_handling.md` as **intentional** exception usage, so that I do not mistake them for un-migrated debt.

## Implementation Decisions

**Nature of the slice.** ADR-002 **step 3**, fourth and final conversion, reusing slice `0003`'s
shared `Error → IResult` mapper, plus the ADR-002 **adoption close-out** (flip to Accepted +
doc reconciliation). It converts the `PurchaseTickets` use-case (a `ShoppingCarts` command that
drives the `ShoppingCart.PurchaseComplete` transition and the `MovieSessionSeat.Sell` transition
behind the seat repository and the Redis cart/seat lifecycle managers) and is filed under the
`platform` module alongside `0001`–`0005` to keep the ADR-002 series together. Settled in a
grill-me interview; runs the full spec chain.

**Conversion depth.** Convert the endpoint, the aggregate method `ShoppingCart.PurchaseComplete`,
and the seat transition `MovieSessionSeat.Sell` (one mislabelled case only). The handler is
already partly converted (cart-not-found ⇒ `NotFoundError`; `SelSeats` `Result` short-circuit) —
this slice only adds the `PurchaseComplete` `Result` consumption and the short-circuit before
persistence. The shared `GetMovieSessionSeat` helper and the shared `EnsurePurchaseIsNotCompleted`
guard are **not** modified.

**Use-cases / aggregates touched.**

- **`PurchaseTickets` command (use-case, modified).** Contract stays `IRequest<Result>`. The
  handler consumes the `Result` from `PurchaseComplete` and short-circuits on `IsFailure` *before*
  `SaveAsync` and the lifecycle side-effects.
- **`ShoppingCart.PurchaseComplete` aggregate (modified).** Retyped `void → Result`. `SeatsReserved`
  ⇒ transition + event + success; already `PurchaseCompleted` ⇒ idempotent success, no event; any
  other status ⇒ `ConflictError`. `Ensure.NotEmpty(ClientId)` stays a throw. Stops calling
  `EnsurePurchaseIsNotCompleted`; the event is appended only on a genuine transition.
- **`MovieSessionSeat.Sell` (seat transition, modified — one case).** The "the place is already
  being processed by another shopping cart" branch returns `ConflictError` instead of
  `InvalidOperation`; the already-`Sold` branch already returns `ConflictError` (unchanged).
- **`purchase` endpoint (modified).** Replaces `return result;` with
  `Match(() => Results.Ok(), ErrorResults.ToProblem)`; `.Produces` corrected to `200`/`404`/`409`.

**Failure-path classification (the contract this slice locks in).** "Status now" reflects the
post-`0005` state, where the handler already returns `Result`s but the endpoint serializes them as
`200`.

| Failure | Mechanism now (post-`0005`) | Status now | Mechanism after `0006` | Status after |
|---|---|---|---|---|
| Shopping cart not found (handler-local) | returns `NotFoundError`; endpoint `return result;` | **200** | `NotFoundError` ⇒ mapper | 404 |
| Movie session not found (shared helper via `SelSeats`) | returns `NotFoundError`; `return result;` | **200** | `NotFoundError` ⇒ mapper | 404 |
| Sales terminated (shared helper via `SelSeats`) | returns `ConflictError`; `return result;` | **200** | `ConflictError` ⇒ mapper | **409** |
| Seat already sold (`Sell`) | returns `ConflictError`; `return result;` | **200** | `ConflictError` ⇒ mapper | **409** |
| Seat held by another cart (`Sell`) | returns **`InvalidOperation`**; `return result;` | **200** | **retype ⇒ `ConflictError`** ⇒ mapper | **409** |
| Cart not in a completable status (`PurchaseComplete`, e.g. `InWork`) | `void`; no throw, fires event, persists (bug) | 200 (buggy) | `ConflictError` ⇒ mapper | **409** |
| Cart already purchased (`PurchaseComplete`) | `throw ConflictException` | 409 | idempotent `Result.Success()` (no event) | **200** (domain); see Further Notes |
| Movie session seat not found (`GetMovieSessionSeat`) | `throw ContentNotFoundException` | 404 | exception (unchanged) | 404 |
| `ClientId` empty at completion (`PurchaseComplete`) | `Ensure` throw | 500 | `Ensure` throw (unchanged) | 500 |
| Repository / Redis lifecycle fault | exception | 500 | exception (unchanged) | 500 |
| Success | `return result;` (serialized `Result` body) | 200 + body | `Results.Ok()` (empty body) | 200 |

**Mapping policy.** Reuses `ErrorResults.ToProblem` (slice `0003`): `NotFoundError ⇒ 404`,
`ConflictError ⇒ 409`, unrecognised `Error ⇒ 500`. No new `Error` types and **no new mapper arm**
are introduced; `Error` definitions stay centralised in `Domain/Error`. The `Sell` retype turns
the one purchase-path `InvalidOperation → 500` trap into a clean `ConflictError → 409`.

**Result type.** Non-generic `Result` throughout — the purchase does not need to carry a value
back to the caller (success is an empty `200`). `Result<T>` remains available (from `0001`) but is
not exercised here; its first real consumer is left to a future value-returning slice.

**Atomicity.** The handler returns a failing `Result` before `SaveAsync`, the cart-lifecycle
`DeleteAsync`, and the per-seat `IShoppingCartSeatLifecycleManager.DeleteAsync` calls, reproducing
the implicit guarantee of the previous thrown path. The pre-existing seat-then-cart persistence
ordering inside `SelSeats`/the handler (a known, separate transactional gap) is **not** changed by
this slice.

**ADR adoption close-out.** This slice flips ADR-002 `Proposed → Accepted` (2026-06-04), rewrites
`agent_docs/error_handling.md` to the decided hybrid, and amends `CLAUDE.md` rule #9 + the
project-at-a-glance line. These are docs-only edits to a stable file (allowed — not a mechanism
change), made because the `ShoppingCarts` write path is complete after this conversion. The
adoption line is **this slice**: the remaining exception usages (reads/queries, `GetMovieSessionSeat`)
are documented as intentional, not as debt.

**Explicitly deferred (NOT in this slice).**
- Converting any read/query handler that throws `ContentNotFoundException` (`404`) to `Result` —
  reads have neither motivation the ADR cited (no domain-event-on-success, no endpoint
  `Result → exception` bridge), and `404` is already the correct contract. Documented as
  intentional exception usage.
- Converting the shared `GetMovieSessionSeat` (seat-not-found) helper to `Result` — a missing seat
  record for a valid session stays a thrown `ContentNotFoundException` (`404`), data-integrity, not
  a business outcome.
- The bare `throw` in the endpoints' `GetClientId` / `CreateShoppingCart` helpers — a
  malformed-identity / idempotency-key concern, not part of the error-model conversion; tracked as a
  separate low-priority cleanup (`0007`).
- Adding a `400`-class arm to `ErrorResults.ToProblem` or otherwise changing the shared mapper /
  introducing a new `Error` kind (ADR-level).
- The pre-existing seat-then-cart transactional ordering gap inside `SelSeats`.
- Adopting `Result<T>` (the generic) on this path.
- The Flutter client follow-up to the `0002` `204 → 404` contract change — orthogonal, lives in the
  `clients/` tree, tracked separately.
- Standing up a `WebApplicationFactory<Program>` HTTP harness, or a real-concurrency
  two-carts-racing-the-same-seat integration test.

## Testing Decisions

**What makes a good test here.** The externally observable behaviour is the `Result` each outcome
of the use-case produces (and therefore the status the shared mapper yields), the `PurchaseComplete`
transition's outcome and its event, the `Sell` "another cart" `Error` kind, and the atomicity
invariant (no persistence on failure). Tests assert that external behaviour — the returned
`Result`/`Error`, the raised (or not raised) event, whether the cart was saved and the lifecycle
touched — not internal wiring. As with `0001`–`0005`, and because the repository has **no
`WebApplicationFactory<Program>` HTTP harness**, the change is pinned with **focused unit tests of
the changed units** rather than an end-to-end HTTP test. The handler unit test is the slice's RED
acceptance gate.

**Units under test.**
- **`PurchaseTickets` handler (the acceptance / RED gate):** with the repositories
  (`IActiveShoppingCartRepository`, `IMovieSessionSeatRepository`), the `MovieSessionSeatService`
  collaborators, and the Redis lifecycle managers (`IShoppingCartLifecycleManager`,
  `IShoppingCartSeatLifecycleManager`) substituted (mocked). `MovieSessionSeatService` is `sealed`
  and concrete — construct it real over mocked seat / movie-session repositories (same shape as
  `0005`'s gate). Asserts: cart missing ⇒ `NotFoundError`; movie session missing ⇒ `NotFoundError`;
  sales terminated ⇒ `ConflictError`; a seat held by another cart ⇒ `ConflictError`; success ⇒
  `Result.Success()` with cart saved, cart-lifecycle removed, and the per-seat selection-lifecycle
  entries deleted; and — pinning atomicity — on any failure the cart save and lifecycle side-effects
  are **not** invoked. Red until the endpoint/handler/domain genuinely produce these `Result`s end
  to end.
- **`ShoppingCart.PurchaseComplete` (domain):** `SeatsReserved` ⇒ status `PurchaseCompleted` and
  `ShoppingCartPurchaseDomainEvent` raised; already `PurchaseCompleted` ⇒ `Result.Success()` and
  **no** event; `InWork` ⇒ `ConflictError` and no event. Pins the `void → Result`, idempotency, and
  event-on-success-only changes.
- **`MovieSessionSeat.Sell` (domain):** the "another shopping cart" case returns a `ConflictError`
  (not `InvalidOperation`); the already-`Sold` case returns a `ConflictError` (unchanged regression).
- **Regression:** slice `0004`'s `SelectSeatCommandHandlerTests` and slice `0005`'s
  `ReserveTicketsCommandHandlerTests` are re-run **unchanged** to prove the `Sell` / `PurchaseComplete`
  changes did not disturb the already-green select and reserve paths.

**Prior art.** Slice `0005`'s `ReserveTicketsCommandHandlerTests` (the closest handler-gate
template, same mocked-collaborator shape and the real-`MovieSessionSeatService` construction) and
slice `0003`'s `AssignClientCartCommandHandlerTests`; slice `0003`'s `ErrorResultsOutsideInTests`
already covers the shared mapper (no need to re-test the mapping);
`BookingManagementService.Domain.UnitTests` (`ShoppingCarts/ShoppingCartSpecification.cs`,
`Seats/MovieSessionSeatSpecification.cs`) for the AAA / `*Specification` domain conventions used by
the `PurchaseComplete` and `Sell` domain tests.

**Out of the net (by decision):** no `WebApplicationFactory` end-to-end test (no harness exists;
the endpoint's `Match` wiring is covered by compilation and the shared mapper by slice `0003`); a
real-concurrency test (two carts racing the same seat) is deferred to a separate Infrastructure-level
integration test, not this slice's gate; no tests for read/query handlers or any out-of-scope
use-case.

## Out of Scope

- Converting any read/query handler (or the shared `GetMovieSessionSeat`) that throws
  `ContentNotFoundException` to `Result` — intentional exception usage, documented as such.
- Modifying the shared `EnsurePurchaseIsNotCompleted` guard (still used by `CalculateCartAmount`
  and others).
- Adopting `Result<T>` (the generic) on this path — `PurchaseTickets` uses the non-generic `Result`;
  success carries no value.
- Adding a `400`-class arm to `ErrorResults.ToProblem` or otherwise changing the shared mapper /
  introducing a new `Error` kind (ADR-level).
- The bare `throw` in the `GetClientId` / `CreateShoppingCart` endpoint helpers (slice `0007`).
- Fixing the pre-existing seat-then-cart transactional ordering gap inside `SelSeats`.
- Changing `CustomExceptionHandler`, the MediatR pipeline, the validation behaviour, or any base
  type.
- Standing up a `WebApplicationFactory` HTTP integration harness, or a real-concurrency seat-race
  integration test.
- The Flutter client follow-up to the `0002` `204 → 404` contract change.
- Publishing this PRD to a remote issue tracker — `gh` is not installed and no triage-label
  vocabulary was provided, so the `needs-triage` step could not run; the PRD is stored locally
  instead (same as slices `0001`–`0005`).

## Further Notes

- This is the fourth and final step-3 conversion and the one that **completes the `ShoppingCarts`
  write path**, which is why it — and only it — carries the ADR-002 adoption close-out (flip to
  Accepted, reconcile `agent_docs/error_handling.md` and `CLAUDE.md`). The adoption line is drawn
  here deliberately: reads/queries keep throwing `ContentNotFoundException` (`404`) by design, and
  that intent is written into `agent_docs/error_handling.md` so it is not later mistaken for debt.
- **The `Sell` `InvalidOperation → 500` trap is the purchase-path twin of the `Select` trap slice
  `0004` defused.** The fix is identical in spirit: retype the one mislabelled "another shopping
  cart" case to `ConflictError`. Sibling transitions (`Select`, `Reserve`) already return
  `ConflictError` for the same condition, so this is a one-line correction of an inconsistency, not
  a new policy. `InvalidOperation` stays a `500`-class error so the shared mapper is untouched.
- **Idempotent already-purchased vs the seat-level `Sold` guard.** The decision (grill-me) is that
  `PurchaseComplete()` on an already-`PurchaseCompleted` cart is an idempotent `Result.Success()`
  with no event — a domain-method-level `409 → 200` change, mirroring how `0005` made
  already-`SeatsReserved` idempotent. Note the handler reaches `PurchaseComplete` only **after**
  `SelSeats` succeeds, and on a fully-completed cart the seats are already `Sold`, so `Sell`'s
  `Sold` guard returns a `ConflictError` (`409`) **first** — meaning a real re-`POST /purchase` on a
  completed cart normally surfaces as `409` at the endpoint. The idempotent `Success` is therefore
  primarily the domain-method contract (kept consistent with `SeatsReserve` and covering the
  inconsistent-state case where seats are not `Sold` while the cart is `PurchaseCompleted`); it is
  pinned by the `PurchaseComplete` domain test, not by an endpoint test. This nuance is recorded so
  the requirements/tests steps do not assert a contradictory endpoint-level `200` for re-purchase.
- **Open question for `/feature-requirements` — purchase directly from `InWork`.** The matrix maps
  a non-`SeatsReserved`/non-`PurchaseCompleted` status (notably `InWork`, a cart whose seats were
  selected but never reserved) to `ConflictError`. This assumes the product flow is **select →
  reserve → purchase** and that purchasing without reserving first is illegal. If a "select →
  purchase directly" flow is intended, `PurchaseComplete` must instead allow the `InWork →
  PurchaseCompleted` transition. **Recommendation: require a prior reservation (`ConflictError` on
  `InWork`)** — it matches the existence of a distinct reserve step and fixes the current latent
  behaviour (the old `void` code fired the purchase event and persisted from `InWork` without
  transitioning). Confirm in `requirements.md` before red.
- The `PurchaseComplete` unconditional-event bug (event raised even when no transition happened) is
  fixed as a side-effect of the `void → Result` conversion, the same way `0005` fixed it for
  `SeatsReserve` — the event moves onto the genuine-transition branch.
- The atomicity invariant (user stories 21/26) is the part most easily lost in a throw→return
  refactor: the thrown path aborted before the cart save, the cart-lifecycle removal, and the
  per-seat lifecycle writes; the returned path must short-circuit explicitly before all of them.
- Verification per `CLAUDE.md`: `dotnet format`, `dotnet build -warnaserror`, `dotnet test`.
  As recorded in MEMORY (`dotnet10-migration`), the build trips the accepted AutoMapper `NU1903`
  NuGet-audit advisory under `-warnaserror` at restore time; handle NuGet audit accordingly so the
  real build/warnings are what is validated. The working .NET 10 SDK is the x86 install at
  `C:\Program Files (x86)\dotnet\dotnet.exe`; run dotnet via the PowerShell tool (MEMORY
  `dotnet-sdk-path`). No EF Core model change in this slice → no migration.
