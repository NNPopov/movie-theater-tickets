# 0005 · ReserveTickets Result→HTTP — Outside-in test spec

> **Deviation from the default template (intentional, per PRD — same family as slices
> `0002`/`0003`/`0004`).** The standard outside-in test for this project goes through
> `WebApplicationFactory<Program>`. **No such harness exists** in this repository (the suites are
> `Domain.UnitTests`, `Domain.ArchitectureTests`, `Infrastructure.UnitTests`, `Application.LoadTests`,
> and the `API.UnitTests` project from `0002`). Slice `0003` already pinned the shared
> `Error → IResult` mapper (`ErrorResults`) with its own gate, so it is **not** re-tested here. The
> PRD's Testing Decisions choose **a focused unit spec of the converted `ReserveTicketsCommandHandler`
> as the acceptance/RED gate** — the load-bearing, externally-observable behaviour this slice changes
> is *which `Result` each outcome of the use-case produces* (and therefore the status the reused
> mapper yields) plus the atomicity invariant. The "entry point" below is therefore the handler's
> `Handle` method driven with its collaborators substituted; the endpoint's `Match` wiring is covered
> by compilation + code review, and the `ShoppingCart.SeatsReserve` transition is pinned by a separate
> domain unit test (plan §6). The handler gate is **RED** until the conversion lands (today the handler
> throws `ContentNotFoundException` for the missing cart, `cart.SeatsReserve()` throws
> `ConflictException` for an already-purchased cart, and a failing reservation `Result` is re-thrown
> as a bare `Exception` — so no failing `Result` is ever returned).

## Goal

Prove that the converted `reserve-tickets` use-case **returns** the right `Result` for each outcome
instead of throwing or swallowing — cart missing ⇒ `NotFoundError`; movie session missing ⇒
`NotFoundError`; sales terminated ⇒ `ConflictError`; a seat not reservable ⇒ `ConflictError`; cart
already purchased ⇒ `ConflictError`; all seats reservable ⇒ `Result.Success()` — and that on **any**
failing outcome the shopping cart is **never persisted** and **no lifecycle side-effect runs** (the
atomicity invariant the thrown/bare-`Exception` path provided implicitly).

## Entry point

Not an HTTP route via `WebApplicationFactory`. The test constructs the handler and invokes it
directly:

- **Method under test:** `ReserveTicketsCommandHandler.Handle(ReserveTicketsCommand, CancellationToken)`
  (in `Application/ShoppingCarts/Command/ReserveSeats/`), returning `Task<Result>`.
- **Command:** `new ReserveTicketsCommand(ShoppingCartId: <cartId>)`.
- **Headers / auth / idempotency:** none — this is an application-layer unit test, not a routed call.

## Wired real

- `ReserveTicketsCommandHandler` (the real handler, real control flow, real
  short-circuit-before-persistence).
- `MovieSessionSeatService` — a **`sealed` concrete class**, constructed **real** over its two
  substituted repositories (it cannot be a substitute). Exercises the real
  `ReserveSeats` → `CheckSeatSaleAvailability` and `MovieSessionSeat.Reserve` paths and the real
  `Result` propagation.
- `ShoppingCart` aggregate — real; its converted `SeatsReserve()` produces the real `Result`
  (success/idempotent/`ConflictError`) and the real `ShoppingCartReservedDomainEvent`.
- `MovieSessionSeat` aggregate — real; its `Reserve` produces the real `ConflictError` and the domain
  event.
- The real `Result` / `NotFoundError` / `ConflictError` types from `Domain/Error`.
- `IActiveShoppingCartRepository.SaveAsync(...)`, `IShoppingCartLifecycleManager.SetAsync(...)`, and
  `IShoppingCartSeatLifecycleManager.DeleteAsync(...)` are the observable persistence/lifecycle
  side-effects the scenarios assert on (received on success, **not** received on any failure).

## Mocked

NSubstitute (the project's mocking library, as in `SelectSeatCommandHandlerTests` /
`AssignClientCartCommandHandlerTests`):

- `IActiveShoppingCartRepository` — `GetByIdAsync(cartId)` returns the seeded cart or `null`;
  `SaveAsync(...)` is observed (the atomicity assertion).
- `IMovieSessionSeatRepository` — `GetByIdAsync(sessionId, row, number, ct)` returns the seeded
  `MovieSessionSeat` in the scenario's state; `UpdateRangeAsync(...)` observed.
- `IMovieSessionsRepository` — `GetByIdAsync(sessionId, ct)` returns a non-terminated `MovieSession`
  (happy/seat-conflict scenarios), a **terminated** `MovieSession` (terminated scenario), or `null`
  (session-missing scenario).
- `IShoppingCartLifecycleManager` — `SetAsync(cart.Id)` observed (received on success, not on
  failure).
- `IShoppingCartSeatLifecycleManager` — `DeleteAsync(sessionId, row, number)` observed (received per
  held seat on success, not on failure).
- `Serilog.ILogger` — no-op substitute.

> Note: unlike `SelectSeatCommandHandler`, `ReserveTicketsCommandHandler` does **not** take an
> `IDistributedLock` — no lock substitute is needed.

No database, Redis, or RabbitMQ instance is touched.

## Fixtures / setup

- **Cart with seats (happy / terminated / seat-conflict / session-missing scenarios):**
  `ShoppingCart.Create(5, dataHasher)` (a real or substituted `IDataHasher`), then set its session
  (`SetShowTime(sessionId)`) and add one seat (`AddSeats(new SeatShoppingCart(1, 1, 10m), sessionId)`)
  so `cart.Seats` is non-empty and `cart.MovieSessionId == sessionId`. Status is `InWork` (so
  `SeatsReserve()` performs a genuine transition). Its id is the command's `ShoppingCartId`.
- **Already-purchased cart (already-purchased scenario):** a `ShoppingCart` in status
  `PurchaseCompleted` — built either by driving the legal transitions
  (`Create` → reserve → `PurchaseComplete` with a `ClientId`) or, more directly, via the private
  `[JsonConstructor]` (Newtonsoft) used for deserialization, as `SelectSeatCommandHandlerTests` does
  for materialized seat states. `SeatsReserve()` must short-circuit on this before reaching
  `ReserveSeats`.
- **Movie session:** `MovieSession.Create(...)` with `SalesTerminated == false` (happy / seat
  conflict); a terminated session (terminated scenario); the session repo returns `null`
  (session-missing scenario).
- **Seat state construction** (`MovieSessionSeat` for `(sessionId, row 1, number 1)`):
  - *Reservable:* `MovieSessionSeat.Create(sessionId, number: 1, row: 1, price: 10m)` (status
    `Available`; `Reserve` allows `Available` or `Selected`).
  - *Not reservable:* an otherwise-identical seat whose `Status` is neither `Selected` nor
    `Available` (e.g. `Sold`/`Reserved`), built via the `[JsonConstructor]` since the aggregate's own
    transitions do not freely leave a seat in those states for this fixture.
- **Auth:** none — the unit under test has no authentication.

## Test scenarios

> These six scenarios are the slice's RED gate (the PRD gate facts: the six outcomes, with the
> atomicity assertion folded into every failure scenario).

### Scenario 1: all seats reservable ⇒ Result.Success(), cart saved, lifecycle set, seats deleted

**Setup:**
- `GetByIdAsync(cartId)` ⇒ the seeded **InWork** cart with one seat `(1,1)` and
  `MovieSessionId == sessionId`.
- `IMovieSessionsRepository.GetByIdAsync(sessionId, ct)` ⇒ non-terminated session.
- `IMovieSessionSeatRepository.GetByIdAsync(sessionId, 1, 1, ct)` ⇒ a **reservable** (`Available`)
  seat.

**Act:**
- `await handler.Handle(new ReserveTicketsCommand(cartId), CancellationToken.None);`

**Expect:**
- `result.IsSuccess == true`.
- Side-effects: `IActiveShoppingCartRepository.Received(1).SaveAsync(Arg.Any<ShoppingCart>())`;
  `IShoppingCartLifecycleManager.Received(1).SetAsync(cartId)`;
  `IShoppingCartSeatLifecycleManager.Received(1).DeleteAsync(sessionId, 1, 1)`;
  `IMovieSessionSeatRepository.Received(1).UpdateRangeAsync(...)`.

**Covers requirement(s):** F8, F11, F14.

### Scenario 2: missing cart ⇒ NotFoundError, nothing persisted

**Setup:**
- `GetByIdAsync(cartId)` ⇒ `null`.

**Act:**
- `await handler.Handle(new ReserveTicketsCommand(cartId), CancellationToken.None);`

**Expect:**
- `result.IsFailure == true` and `result.Error` is a `NotFoundError`.
- `IActiveShoppingCartRepository.DidNotReceive().SaveAsync(Arg.Any<ShoppingCart>())`;
  `IShoppingCartLifecycleManager.DidNotReceive().SetAsync(Arg.Any<Guid>())`.

**Covers requirement(s):** F6, F10.

### Scenario 3: movie session missing ⇒ NotFoundError, nothing persisted

**Setup:**
- `GetByIdAsync(cartId)` ⇒ the seeded **InWork** cart.
- `IMovieSessionsRepository.GetByIdAsync(..., ct)` ⇒ `null` (the shared `CheckSeatSaleAvailability`
  returns `NotFoundError`).

**Act:**
- `await handler.Handle(new ReserveTicketsCommand(cartId), CancellationToken.None);`

**Expect:**
- `result.IsFailure == true` and `result.Error` is a `NotFoundError`.
- `SaveAsync` / `SetAsync` **not** received.

**Covers requirement(s):** F8, F19, F10.

### Scenario 4: sales terminated ⇒ ConflictError (was 500), nothing persisted

**Setup:**
- `GetByIdAsync(cartId)` ⇒ the seeded **InWork** cart.
- `IMovieSessionsRepository.GetByIdAsync(..., ct)` ⇒ a **terminated** `MovieSession`
  (`SalesTerminated == true`).

**Act:**
- `await handler.Handle(new ReserveTicketsCommand(cartId), CancellationToken.None);`

**Expect:**
- `result.IsFailure == true` and `result.Error` is a `ConflictError` — proving the bare
  `throw new Exception(...)` in the shared helper is gone and the outcome maps to `409`, not `500`.
- `SaveAsync` / `SetAsync` **not** received.

**Covers requirement(s):** F8, F19, F24, F10.

### Scenario 5: a seat is not reservable ⇒ ConflictError (was 500), nothing persisted

**Setup:**
- `GetByIdAsync(cartId)` ⇒ the seeded **InWork** cart with seat `(1,1)`.
- `IMovieSessionsRepository.GetByIdAsync(..., ct)` ⇒ non-terminated session.
- `IMovieSessionSeatRepository.GetByIdAsync(sessionId, 1, 1, ct)` ⇒ a seat whose `Status` is neither
  `Selected` nor `Available` (e.g. `Sold`), so `MovieSessionSeat.Reserve` returns a `ConflictError`.

**Act:**
- `await handler.Handle(new ReserveTicketsCommand(cartId), CancellationToken.None);`

**Expect:**
- `result.IsFailure == true` and `result.Error` is a `ConflictError` — proving the deleted bare
  `throw new Exception("Couldn't Reserve …")` bridge no longer downgrades this to `500`, and the
  failing `Result` from `ReserveSeats` is propagated unchanged.
- `IActiveShoppingCartRepository.DidNotReceive().SaveAsync(Arg.Any<ShoppingCart>())` and
  `IShoppingCartLifecycleManager.DidNotReceive().SetAsync(Arg.Any<Guid>())` (atomicity: the cart is
  not persisted as `SeatsReserved` when a seat could not be reserved).

**Covers requirement(s):** F8, F9, F10, F21, F24.

### Scenario 6: cart already purchased ⇒ ConflictError, nothing persisted

**Setup:**
- `GetByIdAsync(cartId)` ⇒ a cart in status `PurchaseCompleted`.

**Act:**
- `await handler.Handle(new ReserveTicketsCommand(cartId), CancellationToken.None);`

**Expect:**
- `result.IsFailure == true` and `result.Error` is a `ConflictError` — produced by the converted
  `SeatsReserve()` returning a `ConflictError` (was a thrown `ConflictException`, same `409` status).
- `SaveAsync` / `SetAsync` **not** received; `ReserveSeats` (and therefore
  `IMovieSessionSeatRepository.UpdateRangeAsync`) **not** invoked (short-circuit before the seat
  service).

**Covers requirement(s):** F7, F10, F16, F23.

> All six scenarios are **RED** against the current code: today the handler throws
> `ContentNotFoundException` for the missing cart, `cart.SeatsReserve()` throws `ConflictException`
> for an already-purchased cart (and is `void`), the shared `CheckSeatSaleAvailability` throws
> `ContentNotFoundException` / a bare `Exception`, and a failing `ReserveSeats` `Result` is re-thrown
> as `throw new Exception("Couldn't Reserve …")` — so no failing `Result` is ever returned. The
> scenarios fail until the conversion in plan §5 lands.

## Out of scope for this test

- The shared `ErrorResults.ToProblem` mapping (`NotFoundError ⇒ 404`, `ConflictError ⇒ 409`,
  else `⇒ 500`) — already covered by slice `0003`'s `ErrorResultsOutsideInTests`; not re-tested.
- The `ShoppingCart.SeatsReserve` domain transition in isolation (`InWork` ⇒ `SeatsReserved` + event;
  already `SeatsReserved` ⇒ `Result.Success()`, no event; `PurchaseCompleted` ⇒ `ConflictError`, no
  event) — covered by `ShoppingCartSpecification` (plan §6), written after green.
- The idempotent already-`SeatsReserved` re-reserve (success, no duplicate event) — exercised at the
  domain level by `ShoppingCartSpecification`; not a distinct handler-gate scenario.
- The endpoint's `Match` wiring, the removed `return result;`, and the `.Produces(...)` OpenAPI
  declarations (F1–F5) — covered by compilation + code review (validation.md).
- The `0004` regression (`SelectSeatCommandHandlerTests` re-run unchanged after the shared-helper
  retype) — a separate, pre-existing test, not part of this gate file.
- The still-thrown paths: repository / Redis lifecycle faults (`500`) and the shared
  `GetMovieSessionSeat` seat-not-found (`404`) — unchanged by this slice (F12, F22).
- The accepted interim purchase-path side-effect of the shared-helper retype (F25) — verified by code
  review, not pinned by a purchase-path test (PRD Out of Scope).
- The end-to-end routing of `POST .../reservations` to `200`/`404`/`409` — no `WebApplicationFactory`
  harness; verified manually (validation.md scenarios).
- Field-level validation, `DbUpdateException` translation, performance/load/real concurrency.
