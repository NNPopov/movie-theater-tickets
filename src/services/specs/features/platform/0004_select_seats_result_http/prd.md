# PRD — `SelectSeats` `Result → HTTP` conversion (ADR-002, step 3)

Slice: `0004_select_seats_result_http` · Module: `platform` · Status: 📋 Started

## Problem Statement

As a client developer (and as a service developer reasoning about the error path) of the
BookingManagement service, I cannot trust how an *expected business outcome* of the
"select a seat for a movie session and hold it in my shopping cart" operation reaches me,
because that use-case advertises the `Result` model while actually running on exceptions and
hiding a second `Result → exception` bridge inside the domain service:

- The `SelectSeats` command handler is typed `IRequest<Result>`, yet it **never returns a
  failure `Result`**. Every business failure on this path is a `throw` — cart not found
  (`ContentNotFoundException`), seat not available / already held by another cart
  (`ConflictException`), cart-state and validation guards, distributed-lock contention
  (`LockedException`), and infrastructure faults (`InvalidOperationException`). The handler's
  only `Result` value is `Result.Success()`.
- The HTTP endpoint therefore carries a **dead, wrong failure branch**:
  `result.Match(() => Results.Ok(), failure => Results.BadRequest(failure.Description))`.
  No real failure `Result` ever reaches it, and if one did it would collapse **every**
  expected outcome to `400 Bad Request` regardless of its nature — masking the `404`/`409`
  the path actually produces today via thrown exceptions.
- There is a **hidden second bridge inside the domain service**: `MovieSessionSeatService.SelectSeat`
  calls the aggregate method `MovieSessionSeat.Select`, which correctly returns a `Result`, and
  then **re-throws it as a `ConflictException`** on failure — the exact anti-pattern ADR-002
  calls out, one layer deeper than the endpoint bridge 0003 removed. It also **collapses two
  distinct domain errors** ("status is not Available" and "this seat is being processed by
  another shopping cart") into one opaque `ConflictException`, discarding the distinction.
- The aggregate raises one of those conflicts as `DomainErrors<MovieSessionSeat>.InvalidOperation(...)`,
  which is a **base `Error`** (not a `ConflictError`). Today this is masked because the service
  throws a `ConflictException` (`409`) for *all* `Select` failures. The moment the path is
  honestly converted to `Result`, the shared `Error → IResult` mapper (`ErrorResults.ToProblem`,
  from slice 0003) would route that base `Error` to its `_ ⇒ 500` fallback — silently turning a
  `409` into a `500`. This is a latent hazard the conversion must defuse, not a stylistic point.
- The endpoint's OpenAPI surface is wrong in **both** directions: the success path returns
  `200 OK` (`Results.Ok()`), but `.Produces(201)` / `.Produces(204)` are declared — neither
  matches, and both are semantically inapplicable (nothing is created at a new URL; `200` is
  not `204`).

ADR-002 ("`Result` for expected outcomes, exceptions for the unexpected") resolves the
error-model question and is explicitly **incremental**. Its **step 3** is "per touched slice:
remove the endpoint `Result → exception` bridge, convert expected-failure paths to `Result` +
`Match`-to-HTTP, replace bare `throw new Exception(...)`". Steps 1 (the `Result<T>`
infrastructure, slice `0001`) and 2 (the `ContentNotFoundException` `204 → 404` contract,
slice `0002`) are done; slice `0003` (`AssignClientCart`) landed the **canonical** step-3
reference and the shared `ErrorResults.ToProblem` mapper. This slice is the **second** step-3
conversion — the first to convert a use-case with an in-aggregate, event-raising transition
behind real infrastructure (distributed lock, Redis seat lifecycle, the seat repository) — and
it **reuses** 0003's mapper rather than introducing a new one.

## Solution

As a client developer, after this slice the `select-seat` operation reports its expected
outcomes through **one** model — `Result` matched straight to HTTP — and stops pretending,
while keeping its observable HTTP contract intact:

- The handler genuinely **returns a failure `Result`** for the three expected business
  outcomes on this path:
  - shopping cart not found ⇒ `NotFoundError` ⇒ `404`;
  - the seat's status is not Available ⇒ `ConflictError` ⇒ `409`;
  - the seat is being processed by another shopping cart ⇒ `ConflictError` ⇒ `409`.
- `MovieSessionSeatService.SelectSeat` changes from `Task<MovieSessionSeat>` to `Task<Result>`,
  and the **internal `Result → ConflictException` bridge is deleted** — it propagates the
  aggregate's `Result` unchanged. The two distinct `Select` conflicts stay distinct in their
  `Error.Description` instead of being flattened into one exception.
- The aggregate method `MovieSessionSeat.Select` returns a **`ConflictError`** (not a base
  `Error` via `InvalidOperation`) for the "another shopping cart" case, so both of its conflict
  outcomes are `ConflictError` and map to `409` through the existing `ErrorResults.ToProblem`
  arms. This defuses the `InvalidOperation ⇒ 500` hazard and preserves today's `409`. The
  `MovieSessionSeatStatusUpdatedDomainEvent` continues to be appended **only on the success
  branch** — the in-aggregate state-transition-that-raises-an-event that ADR-002 says `Result`
  genuinely fits.
- The endpoint resolves the handler's `Result` with `Match(() => Results.Ok(), ErrorResults.ToProblem)`
  — the **same shared mapper** introduced by slice 0003 — and the dead
  `failure => Results.BadRequest(...)` branch is removed. No new mapping module is created.
- **Atomicity is preserved.** Today a thrown failure inside `Select` aborts the handler before
  `SaveShoppingCart`, so a cart is never persisted holding a seat it failed to claim. With the
  conversion to `Result`, the handler **short-circuits on `IsFailure` and returns the failure
  before `SaveShoppingCart`**, reproducing that invariant explicitly: *seat-claim failure ⇒ the
  shopping cart is not persisted with the seat.*
- The OpenAPI surface for this endpoint is made honest: it declares `200` / `404` / `409` and
  drops the stale `201` / `204`. Success is `200 OK` with an empty body.

The observable HTTP **status codes are unchanged** by this conversion (success `200`,
cart/session/seat not-found `404`, all conflicts `409`, validation `400`, lock `423`, infra
`500` — in and out). It is a *mechanism* swap (exceptions + two hidden bridges ⇒ `Result` +
the shared mapper) plus the OpenAPI-honesty fix. There is **no intentional behaviour change**
visible to clients in this slice.

ADR-002 stays **Proposed**; this slice implements its step 3 for one more use-case, per the
ADR's own incremental plan.

## User Stories

1. As a cinema customer, I want selecting an available seat to succeed with `200 OK`, so that I know the seat is held in my shopping cart.
2. As a client developer, I want selecting a seat in a cart id that does not exist to return `404 Not Found`, so that a bad cart id is reported as missing rather than as an opaque error.
3. As a client developer, I want selecting a seat whose status is not Available to return `409 Conflict`, so that "the seat is already taken" is unambiguous.
4. As a client developer, I want selecting a seat that is currently being processed by another shopping cart to return `409 Conflict`, so that concurrent contention on a seat is reported consistently with the "already taken" conflict.
5. As a client developer, I want both seat conflicts on this path produced by the *same* mechanism and carrying the *same* `ProblemDetails` body shape, so that I handle "conflict" uniformly regardless of which rule was violated.
6. As a client developer, I want a `404`/`409` from this endpoint to carry a `ProblemDetails` body identical in shape to every other `404`/`409` in the service, so that my client parses one response shape.
7. As the Flutter client, I want the `409` contract on seat selection to stay exactly as it is, so that the existing `DioException.statusCode == 409 ⇒ ConflictFailure` handling keeps working unchanged.
8. As a service developer, I want the `SelectSeats` handler to *return* a `Result` for cart-not-found and seat-conflict instead of only ever returning `Result.Success()`, so that the use-case's `IRequest<Result>` signature stops being a lie.
9. As a service developer, I want the endpoint to resolve the handler's `Result` by matching it straight to HTTP via the shared `ErrorResults.ToProblem`, so that the dead, wrong `BadRequest` failure branch is gone and the request runs one error pass.
10. As a service developer, I want the internal `Result → ConflictException` bridge inside `MovieSessionSeatService.SelectSeat` removed, so that the domain `Result` is propagated rather than re-thrown one layer down.
11. As a service developer, I want `MovieSessionSeatService.SelectSeat` retyped from `Task<MovieSessionSeat>` to `Task<Result>`, since the returned seat value is not consumed by the handler.
12. As a domain developer, I want `MovieSessionSeat.Select` to return a `ConflictError` (not a base `Error` via `InvalidOperation`) for the "another shopping cart" case, so that it maps to `409` through the existing mapper instead of falling through to `500`.
13. As a domain developer, I want `MovieSessionSeat.Select` to keep appending `MovieSessionSeatStatusUpdatedDomainEvent` only on the success branch, so that the event is raised exactly when the seat actually transitioned to Selected.
14. As a service developer, I want the handler to short-circuit on a failing `Result` and return it *before* `SaveShoppingCart`, so that a cart is never persisted holding a seat whose claim failed (the atomicity invariant the thrown path provided implicitly).
15. As a service developer, I want the distributed-lock-not-acquired case to remain a `LockedException` (`423`), so that transient concurrency contention stays an exception, not a business `Result`.
16. As a service developer, I want the Redis seat-lifecycle failure and its rollback to remain an exception (`500`), so that an infrastructure fault is treated as unexpected, not as a business outcome.
17. As a service developer, I want the cart-state and seat-validation guards in `ShoppingCart.EnsureSeatCanBeAdded` to remain exceptions (`409` / `400`), so that they are unchanged — there is no `400` arm in the shared mapper and these are not the transition being converted.
18. As a service developer, I want the not-found checks that live in the *shared* `MovieSessionSeatService` helpers (movie session not found, seat not found) to remain exceptions (`404`) in this slice, so that shared code used by the not-yet-converted `Reserve`/`Purchase` paths is not changed pre-emptively.
19. As an API consumer reading the OpenAPI document, I want the `select-seat` endpoint to declare `200`, `404`, and `409`, so that the published contract matches runtime behaviour.
20. As an API consumer, I want the stale `201`/`204` declarations on that endpoint removed, so that the OpenAPI document is not wrong in the other direction.
21. As a maintainer, I want the externally observable status codes (`200`/`404`/`409`/`400`/`423`/`500`) to stay the same across this conversion, so that the change is low-risk with no client-visible behaviour change.
22. As a maintainer, I want this slice to reuse slice 0003's shared `ErrorResults.ToProblem` mapper rather than introduce a new one, so that the `Result`-to-HTTP policy stays in exactly one place.
23. As a reviewer, I want this conversion to follow the spec chain (PRD → plan → requirements → validation → tests → red gate → implementation), so that even a status-preserving refactor has a traceable rationale and an acceptance gate.
24. As a maintainer, I want the `SelectSeats` handler pinned by a unit test (cart missing ⇒ `NotFoundError`; seat not Available ⇒ `ConflictError`; another cart ⇒ `ConflictError`; success ⇒ `Result.Success()`; and seat-claim failure ⇒ `SaveShoppingCart` is not invoked), so that the conversion and the atomicity invariant are both covered.
25. As a maintainer, I want `MovieSessionSeat.Select` pinned by a domain unit test (status not Available ⇒ `ConflictError`, no event; another cart ⇒ `ConflictError`, no event; success ⇒ status Selected and the event raised), so that the `InvalidOperation → ConflictError` change and the event-on-success behaviour are covered.
26. As a maintainer, I want the architecture tests (`Domain.ArchitectureTests`) and the full suite to stay green, so that the conversion honours the structural rules.
27. As a service developer, I want this slice to convert *only* `SelectSeats`, with `ReserveTickets`/`PurchaseTickets` and the shared-helper defects left for later slices, so that the change lands small and reviewable.

## Implementation Decisions

**Nature of the slice.** ADR-002 **step 3**, second conversion, reusing slice 0003's shared
`Error → IResult` mapper. It converts the `SelectSeats` use-case (a `ShoppingCarts` command that
drives an in-aggregate, event-raising transition on the `MovieSessionSeat` aggregate behind real
infrastructure) and is filed under the `platform` module alongside `0001`–`0003` to keep the
ADR-002 series together. Settled in a grill-me interview; runs the full spec chain.

**Conversion depth.** Depth "B": convert the endpoint, the handler, the domain service method,
and the aggregate method. The *shared* `MovieSessionSeatService` helpers
(`CheckSeatSaleAvailability`, `GetMovieSessionSeat`) are **not** touched, because they are also
called by the not-yet-converted `Reserve`/`Purchase` paths.

**Use-cases / aggregates touched.**

- **`SelectSeats` command (use-case, modified).** Contract stays `IRequest<Result>`. The
  handler returns `NotFoundError` when the shopping cart is missing, propagates the domain
  service's `Result`, and short-circuits on `IsFailure` *before* persisting the cart. The
  distributed-lock guard and the Redis-lifecycle rollback remain exceptions.
- **`MovieSessionSeatService.SelectSeat` (domain service, modified).** Retyped
  `Task<MovieSessionSeat>` → `Task<Result>`; the internal `Result → ConflictException` re-throw
  is removed; the aggregate `Result` is propagated.
- **`MovieSessionSeat.Select` aggregate (modified).** Returns `ConflictError` (instead of a base
  `Error` via `InvalidOperation`) for the "another shopping cart" case; keeps appending the
  status-updated domain event only on success.
- **`select-seat` endpoint (modified).** Replaces `Match(Ok, BadRequest)` with
  `Match(() => Results.Ok(), ErrorResults.ToProblem)`; `.Produces` corrected to `200`/`404`/`409`.

**Failure-path classification (the contract this slice locks in).**

| Failure | Mechanism after slice | Status |
|---|---|---|
| Shopping cart not found (handler-local) | **`Result` `NotFoundError`** | 404 |
| Seat status not Available (`Select`) | **`Result` `ConflictError`** | 409 |
| Seat held by another cart (`Select`) | **`Result` `ConflictError`** (was base `Error`) | 409 |
| `EnsureSeatCanBeAdded`: cart not InWork | exception (`ConflictException`) | 409 |
| `EnsureSeatCanBeAdded`: wrong session / duplicate / max seats | exception (`DomainValidationException`) | 400 |
| Movie session not found (shared helper) | exception (unchanged) | 404 |
| Movie session seat not found (shared helper) | exception (unchanged) | 404 |
| Sales terminated (shared helper, bare `Exception`) | **deferred** — unchanged | 500 |
| Distributed lock not acquired | exception (`LockedException`) | 423 |
| Redis seat-lifecycle failure / rollback | exception (`InvalidOperationException`) | 500 |

**Mapping policy.** Reuses `ErrorResults.ToProblem` (slice 0003): `NotFoundError ⇒ 404`,
`ConflictError ⇒ 409`, unrecognised `Error ⇒ 500`. No new `Error` types are introduced; `Error`
definitions stay centralised in `Domain/Error`. The `InvalidOperation → ConflictError` change
is *not* a new `Error` kind — it reuses the existing `ConflictError`.

**Result type.** Non-generic `Result` throughout — the selected seat's value is not needed by
the handler to build the response (the seat price for the cart line is fetched separately,
before the claim).

**Atomicity.** The handler returns a failing `Result` before `SaveShoppingCart`, reproducing
the implicit guarantee of the previous thrown path: a failed seat claim never persists a cart
holding that seat. The seat-lifecycle rollback for *infrastructure* failures remains an
exception.

**Status-preservation.** The conversion is mechanism-only at the HTTP layer plus an
OpenAPI-honesty fix; every observable status is unchanged in and out. There is no intentional
client-visible behaviour change in this slice.

**ADR status.** ADR-002 stays **Proposed**; this slice implements its step 3 for one use-case.

**Explicitly deferred (NOT in this slice).**
- Converting `ReserveTickets` / `PurchaseTickets` (which currently return a serialized `Result`
  straight from the endpoint) — later slices that reuse this slice's pattern and the 0003 mapper.
- Fixing the bare `throw new Exception(...terminated)` in the *shared* `CheckSeatSaleAvailability`
  (ADR-002 defect #2). It is shared by `SelectSeat`, `ReserveSeats`, and `SelSeats`; changing its
  exception type would alter `Reserve`/`Purchase` behaviour pre-emptively. Fixed when that shared
  helper is converted. Its `500` is left as-is here.
- Converting the shared not-found helpers (`GetMovieSessionSeat`, the movie-session lookup in
  `CheckSeatSaleAvailability`) to `Result`; they stay exceptions until `Reserve`/`Purchase` land.
- Any `400`-class arm in `ErrorResults.ToProblem` (would be needed to convert the
  `DomainValidationException` guards) — an ADR-level mapper change, out of scope.
- The bare `throw` in the endpoints' `GetClientId` / `CreateShoppingCart` helpers (a
  malformed-identity / idempotency-key concern, not part of this error-model conversion).
- Standing up a `WebApplicationFactory<Program>` HTTP harness, and a real-concurrency
  integration test for two carts racing the same seat (a separate, later test).

## Testing Decisions

**What makes a good test here.** The externally observable behaviour is the `Result` each
outcome of the use-case produces (and therefore the status the shared mapper yields), the
domain transition's outcome and its event, and the atomicity invariant. Tests assert that
external behaviour — the returned `Result`/`Error`, the raised event, whether the cart was
persisted — not internal wiring. As with `0001`/`0002`/`0003`, and because the repository has
**no `WebApplicationFactory<Program>` HTTP harness**, the change is pinned with **focused unit
tests of the changed units** rather than an end-to-end HTTP test.

**Units under test.**
- **`SelectSeats` handler (the acceptance / RED gate):** with the repositories, the
  `MovieSessionSeatService` collaborators, the distributed lock, and the Redis seat-lifecycle
  manager substituted (mocked). Asserts: cart missing ⇒ returns `NotFoundError`; seat not
  Available ⇒ returns `ConflictError`; seat held by another cart ⇒ returns `ConflictError`;
  available seat ⇒ `Result.Success()`; and — pinning the atomicity invariant — on a failing
  seat claim the shopping-cart save is **not** invoked. This is the slice's RED gate (red until
  the handler genuinely returns these `Result`s).
- **`MovieSessionSeat.Select` (domain):** status not Available ⇒ `ConflictError`, no event;
  another shopping cart ⇒ `ConflictError`, no event; success ⇒ status `Selected` and
  `MovieSessionSeatStatusUpdatedDomainEvent` raised. Pins the `InvalidOperation → ConflictError`
  change and the event-on-success behaviour.

**Prior art.** Slice `0003`'s `ErrorResultsOutsideInTests` (the shared mapper this slice reuses
is already covered there — no need to re-test the mapping); `BookingManagementService.Domain.UnitTests`
(`ShoppingCarts/ShoppingCartSpecification.cs`, `Error/ResultOfTSpecification.cs`) for the AAA /
`*Specification` domain conventions; existing mocked-collaborator application handler tests for
the handler test.

**Out of the net (by decision):** no `WebApplicationFactory` end-to-end test (no harness exists;
standing one up for one endpoint is disproportionate — the endpoint's `Match` wiring is covered
by compilation, and the shared mapper is already covered by slice 0003); a real-concurrency test
(two carts racing the same seat, exercising the distributed lock) is deferred to a separate
Infrastructure-level integration test, not this slice's gate; no tests for the use-cases left
out of scope.

## Out of Scope

- Converting any use-case other than `SelectSeats` (`ReserveTickets`, `PurchaseTickets` — later
  slices that reuse this slice's pattern and the 0003 mapper).
- Adopting `Result<T>` (the generic) on this path — `SelectSeats` uses the non-generic `Result`;
  the generic is exercised by a later conversion that needs to carry a value.
- Fixing the bare `throw new Exception(...terminated)` in the shared `CheckSeatSaleAvailability`
  (ADR-002 defect #2), or converting any shared `MovieSessionSeatService` helper to `Result`.
- Adding a `400`-class arm to `ErrorResults.ToProblem` or otherwise changing the shared mapper /
  introducing a new `Error` kind (ADR-level).
- The bare `throw` in the `GetClientId` / `CreateShoppingCart` endpoint helpers.
- Changing `CustomExceptionHandler`, the MediatR pipeline, the validation behaviour, or any base
  type.
- Standing up a `WebApplicationFactory` HTTP integration harness, or a real-concurrency seat-race
  integration test.
- Flipping ADR-002 to Accepted.
- Publishing this PRD to a remote issue tracker — `gh` is not installed and no triage-label
  vocabulary was provided, so the `needs-triage` step could not run; the PRD is stored locally
  instead (same as slices `0001`–`0003`).

## Further Notes

- The diff is small but load-bearing in a different way than 0003: this is the first step-3
  conversion to sit behind real infrastructure (distributed lock, Redis seat lifecycle, the seat
  repository) and the first to remove a **second, hidden** `Result → exception` bridge — the one
  inside `MovieSessionSeatService.SelectSeat`, one layer below the endpoint bridge 0003 removed.
- The single sharpest hazard is silent: converting `Select` to a real `Result` without changing
  the aggregate's `InvalidOperation` to `ConflictError` would route the "another cart" conflict
  through the mapper's `_ ⇒ 500` fallback, turning today's `409` into a `500`. The aggregate
  change (user story 12) is what keeps the status contract intact, and the domain unit test
  (user story 25) is what guards it.
- The atomicity invariant (user story 14 / 24) is the part most easily lost in a throw→return
  refactor: the thrown path aborted before the cart save; the returned path must short-circuit
  explicitly. It is called out as its own test assertion for that reason.
- Status-preservation (every observable status unchanged) means the "modifying an existing
  slice" red→green is driven by the handler gate and the domain test, not by a contract flip.
- Verification per `CLAUDE.md`: `dotnet format`, `dotnet build -warnaserror`, `dotnet test`.
  As recorded in MEMORY (`dotnet10-migration`), the build trips the accepted AutoMapper `NU1903`
  NuGet-audit advisory under `-warnaserror` at restore time; handle NuGet audit accordingly so
  the real build/warnings are what is validated. No EF Core model change in this slice → no
  migration.
