# 0006 · PurchaseTickets Result→HTTP — Outside-in test spec

> **Deviation from the default template (intentional, per PRD — same family as slices
> `0002`/`0003`/`0004`/`0005`).** The standard outside-in test for this project goes through
> `WebApplicationFactory<Program>`. **No such harness exists** in this repository (the suites are
> `Domain.UnitTests`, `Domain.ArchitectureTests`, `Infrastructure.UnitTests`, `Application.LoadTests`,
> and the `API.UnitTests` project from `0002`). Slice `0003` already pinned the shared
> `Error → IResult` mapper (`ErrorResults`) with its own gate, so it is **not** re-tested here. The
> PRD's Testing Decisions choose **a focused unit spec of the converted `PurchaseTicketsCommandHandler`
> as the acceptance/RED gate** — the load-bearing, externally-observable behaviour this slice changes
> is *which `Result` each outcome of the use-case produces* (and therefore the status the reused
> mapper yields) plus the atomicity invariant. The "entry point" below is therefore the handler's
> `Handle` method driven with its collaborators substituted; the endpoint's `Match` wiring is covered
> by compilation + code review, and the `ShoppingCart.PurchaseComplete` transition and the
> `MovieSessionSeat.Sell` retype are pinned by separate domain unit tests (plan §6). The handler gate
> is **RED** until the conversion lands (today the handler calls the `void` `cart.PurchaseComplete()`
> and then unconditionally persists; the only failing `Result`s it returns are already serialized as
> `200` by the endpoint, and the "another cart" case is an `InvalidOperation`, not a `ConflictError`).

## Goal

Prove that the converted `purchase-tickets` use-case **returns** the right `Result` for each outcome
instead of completing unconditionally — cart missing ⇒ `NotFoundError`; movie session missing ⇒
`NotFoundError`; sales terminated ⇒ `ConflictError`; a seat held by another cart ⇒ `ConflictError`
(the `Sell` retype, **not** `InvalidOperation`); the seats sellable by this cart and the cart
`SeatsReserved` ⇒ `Result.Success()` — and that on **any** failing outcome the shopping cart is
**never persisted** and **no lifecycle side-effect runs** (the atomicity invariant the thrown path
provided implicitly).

## Entry point

Not an HTTP route via `WebApplicationFactory`. The test constructs the handler and invokes it
directly:

- **Method under test:** `PurchaseTicketsCommandHandler.Handle(PurchaseTicketsCommand, CancellationToken)`
  (in `Application/ShoppingCarts/Command/PurchaseSeats/`), returning `Task<Result>`.
- **Command:** `new PurchaseTicketsCommand(ShoppingCartId: <cartId>)`.
- **Headers / auth / idempotency:** none — this is an application-layer unit test, not a routed call.

## Wired real

- `PurchaseTicketsCommandHandler` (the real handler, real control flow, real
  short-circuit-before-persistence).
- `MovieSessionSeatService` — a **`sealed` concrete class**, constructed **real** over its two
  substituted repositories (it cannot be a substitute). Exercises the real
  `SelSeats` → `CheckSeatSaleAvailability` and `MovieSessionSeat.Sell` paths and the real `Result`
  propagation.
- `ShoppingCart` aggregate — real; its converted `PurchaseComplete()` produces the real `Result`
  (success/idempotent/`ConflictError`) and the real `ShoppingCartPurchaseDomainEvent`.
- `MovieSessionSeat` aggregate — real; its `Sell` produces the real `ConflictError` (after the
  retype) and the domain event.
- The real `Result` / `NotFoundError` / `ConflictError` types from `Domain/Error`.
- `IActiveShoppingCartRepository.SaveAsync(...)`, `IShoppingCartLifecycleManager.DeleteAsync(...)`,
  and `IShoppingCartSeatLifecycleManager.DeleteAsync(...)` are the observable persistence/lifecycle
  side-effects the scenarios assert on (received on success, **not** received on any failure).

## Mocked

NSubstitute (the project's mocking library, as in `ReserveTicketsCommandHandlerTests` /
`SelectSeatCommandHandlerTests` / `AssignClientCartCommandHandlerTests`):

- `IActiveShoppingCartRepository` — `GetByIdAsync(cartId)` returns the seeded cart or `null`;
  `SaveAsync(...)` is observed (the atomicity assertion).
- `IMovieSessionSeatRepository` — `GetByIdAsync(sessionId, row, number, ct)` returns the seeded
  `MovieSessionSeat` in the scenario's state; `UpdateRangeAsync(...)` observed.
- `IMovieSessionsRepository` — `GetByIdAsync(sessionId, ct)` returns a non-terminated `MovieSession`
  (happy / another-cart scenarios), a **terminated** `MovieSession` (terminated scenario), or `null`
  (session-missing scenario).
- `IShoppingCartLifecycleManager` — `DeleteAsync(cart.Id)` observed (received on success, not on
  failure).
- `IShoppingCartSeatLifecycleManager` — `DeleteAsync(sessionId, row, number)` observed (received per
  held seat on success, not on failure).

> Note: `PurchaseTicketsCommandHandler` takes no `IDistributedLock` and no `ILogger` — its
> constructor is `(IShoppingCartSeatLifecycleManager, IMovieSessionSeatRepository,
> IActiveShoppingCartRepository, MovieSessionSeatService, IShoppingCartLifecycleManager)`; substitute
> exactly those (the service is constructed real over its two substituted repos).

No database, Redis, or RabbitMQ instance is touched.

## Fixtures / setup

- **Reserved cart with seats (happy / terminated / session-missing / another-cart scenarios):** a
  `ShoppingCart` whose status is `SeatsReserved`, with a non-empty `ClientId`, `MovieSessionId ==
  sessionId`, and one seat `(1,1)` — so `PurchaseComplete()` performs a genuine
  `SeatsReserved → PurchaseCompleted` transition. Build it either by driving the legal transitions
  (`ShoppingCart.Create(5, dataHasher)` → `SetShowTime(sessionId)` → `AssignClientId(clientId)` →
  `AddSeats(new SeatShoppingCart(1, 1, 10m), sessionId)` → `SeatsReserve()`) or, more directly, via
  the private `[JsonConstructor]` (Newtonsoft) used for deserialization — as
  `SelectSeatCommandHandlerTests` does for materialized states — supplying `status:
  ShoppingCartStatus.SeatsReserved`, a non-empty `clientId`, and the seat array. Its id is the
  command's `ShoppingCartId`.
- **Movie session:** `MovieSession.Create(...)` with `SalesTerminated == false` (happy /
  another-cart); a terminated session (terminated scenario); the session repo returns `null`
  (session-missing scenario).
- **Seat state construction** (`MovieSessionSeat` for `(sessionId, row 1, number 1)`), used by
  `SelSeats` → `Sell(cartId)`:
  - *Sellable by this cart:* a seat whose `ShoppingCartId == cartId` and `Status` is not `Sold`
    (e.g. `Reserved`), built via the `[JsonConstructor]` so `Sell(cartId)` succeeds.
  - *Held by another cart:* an otherwise-identical seat whose `ShoppingCartId` is a **different**
    Guid, built via the `[JsonConstructor]`, so `Sell(cartId)` hits the
    `ShoppingCartId != shoppingCartId` branch and (after the retype) returns a `ConflictError`.
- **Auth:** none — the unit under test has no authentication.

## Test scenarios

> These five scenarios are the slice's RED gate (the PRD gate facts: the five outcomes, with the
> atomicity assertion folded into every failure scenario).

### Scenario 1: seats sellable, cart reserved ⇒ Result.Success(), cart saved, lifecycle removed, seats deleted

**Setup:**
- `GetByIdAsync(cartId)` ⇒ the seeded **SeatsReserved** cart with one seat `(1,1)`, a non-empty
  `ClientId`, and `MovieSessionId == sessionId`.
- `IMovieSessionsRepository.GetByIdAsync(sessionId, ct)` ⇒ non-terminated session.
- `IMovieSessionSeatRepository.GetByIdAsync(sessionId, 1, 1, ct)` ⇒ a seat **sellable by this cart**
  (`ShoppingCartId == cartId`, status `Reserved`).

**Act:**
- `await handler.Handle(new PurchaseTicketsCommand(cartId), CancellationToken.None);`

**Expect:**
- `result.IsSuccess == true`.
- Side-effects: `IActiveShoppingCartRepository.Received(1).SaveAsync(Arg.Any<ShoppingCart>())`;
  `IShoppingCartLifecycleManager.Received(1).DeleteAsync(cartId)`;
  `IShoppingCartSeatLifecycleManager.Received(1).DeleteAsync(sessionId, 1, 1)`;
  `IMovieSessionSeatRepository.Received(1).UpdateRangeAsync(...)`.

**Covers requirement(s):** F7, F8, F10, F14.

### Scenario 2: missing cart ⇒ NotFoundError, nothing persisted

**Setup:**
- `GetByIdAsync(cartId)` ⇒ `null`.

**Act:**
- `await handler.Handle(new PurchaseTicketsCommand(cartId), CancellationToken.None);`

**Expect:**
- `result.IsFailure == true` and `result.Error` is a `NotFoundError`.
- `IActiveShoppingCartRepository.DidNotReceive().SaveAsync(Arg.Any<ShoppingCart>())`;
  `IShoppingCartLifecycleManager.DidNotReceive().DeleteAsync(Arg.Any<Guid>())`.

**Covers requirement(s):** F6, F9.

### Scenario 3: movie session missing ⇒ NotFoundError, nothing persisted

**Setup:**
- `GetByIdAsync(cartId)` ⇒ the seeded **SeatsReserved** cart.
- `IMovieSessionsRepository.GetByIdAsync(..., ct)` ⇒ `null` (the shared `CheckSeatSaleAvailability`
  returns `NotFoundError` via `SelSeats`).

**Act:**
- `await handler.Handle(new PurchaseTicketsCommand(cartId), CancellationToken.None);`

**Expect:**
- `result.IsFailure == true` and `result.Error` is a `NotFoundError`.
- `SaveAsync` / cart-lifecycle `DeleteAsync` **not** received.

**Covers requirement(s):** F7, F9, F24.

### Scenario 4: sales terminated ⇒ ConflictError (was interim 200), nothing persisted

**Setup:**
- `GetByIdAsync(cartId)` ⇒ the seeded **SeatsReserved** cart.
- `IMovieSessionsRepository.GetByIdAsync(..., ct)` ⇒ a **terminated** `MovieSession`
  (`SalesTerminated == true`).

**Act:**
- `await handler.Handle(new PurchaseTicketsCommand(cartId), CancellationToken.None);`

**Expect:**
- `result.IsFailure == true` and `result.Error` is a `ConflictError` — proving the terminated outcome
  now reaches the endpoint as a failing `Result` (`409` via the mapper) instead of being serialized
  as `200`.
- `SaveAsync` / cart-lifecycle `DeleteAsync` **not** received.

**Covers requirement(s):** F7, F9, F24.

### Scenario 5: a seat held by another cart ⇒ ConflictError (the Sell retype), nothing persisted

**Setup:**
- `GetByIdAsync(cartId)` ⇒ the seeded **SeatsReserved** cart with seat `(1,1)`.
- `IMovieSessionsRepository.GetByIdAsync(..., ct)` ⇒ non-terminated session.
- `IMovieSessionSeatRepository.GetByIdAsync(sessionId, 1, 1, ct)` ⇒ a seat whose `ShoppingCartId` is
  a **different** Guid (held by another cart), so `MovieSessionSeat.Sell(cartId)` hits the
  `ShoppingCartId != shoppingCartId` branch.

**Act:**
- `await handler.Handle(new PurchaseTicketsCommand(cartId), CancellationToken.None);`

**Expect:**
- `result.IsFailure == true` and `result.Error` is a **`ConflictError`** — proving the `Sell` retype
  from `InvalidOperation` to `ConflictError`, so this maps to `409` (not the `_ => 500` mapper arm)
  and not the interim `200`.
- `IActiveShoppingCartRepository.DidNotReceive().SaveAsync(Arg.Any<ShoppingCart>())` and
  `IShoppingCartLifecycleManager.DidNotReceive().DeleteAsync(Arg.Any<Guid>())` (atomicity: the cart
  is not persisted as `PurchaseCompleted` when a seat could not be sold).

**Covers requirement(s):** F7, F9, F19, F24.

> All five scenarios are **RED** against the current code: today the handler calls the `void`
> `cart.PurchaseComplete()` (so no `PurchaseComplete` `Result` is consumed), and the "another cart"
> case returns an `InvalidOperation` (Scenario 5 expects a `ConflictError`). The failing `Result`s
> the handler does return today are never surfaced as such because the endpoint serializes them as
> `200`; this gate asserts on the **returned `Result`** directly, and Scenario 5 in particular fails
> until the `Sell` retype in plan §5 step 1 lands. The scenarios pass once plan §5 steps 1–3 land.

## Out of scope for this test

- The shared `ErrorResults.ToProblem` mapping (`NotFoundError ⇒ 404`, `ConflictError ⇒ 409`,
  else `⇒ 500`) — already covered by slice `0003`'s `ErrorResultsOutsideInTests`; not re-tested.
- The `ShoppingCart.PurchaseComplete` domain transition in isolation (`SeatsReserved` ⇒
  `PurchaseCompleted` + event; already `PurchaseCompleted` ⇒ `Result.Success()`, no event; `InWork`
  ⇒ `ConflictError`, no event) — covered by `ShoppingCartSpecification` (plan §6), written after
  green. In particular the **idempotent already-purchased** `Result.Success()` (F16) and the
  **purchase-from-`InWork` `ConflictError`** (F17) are domain-level facts, not distinct handler-gate
  scenarios (the handler reaches `PurchaseComplete` only after `SelSeats`, and on a completed cart
  `Sell`'s `Sold` guard fires first — see PRD Further Notes / F26).
- The `MovieSessionSeat.Sell` retype in isolation (another cart ⇒ `ConflictError`; already-`Sold` ⇒
  `ConflictError`) — covered by `MovieSessionSeatSpecification` (plan §6), written after green.
- The endpoint's `Match` wiring, the removed `return result;`, and the `.Produces(...)` OpenAPI
  declarations (F1–F5) — covered by compilation + code review (validation.md).
- The ADR-002 adoption close-out docs edits (F27–F29) — verified by code review, not by a test.
- The `0004` / `0005` regression (`SelectSeatCommandHandlerTests` / `ReserveTicketsCommandHandlerTests`
  re-run unchanged) — separate, pre-existing tests, not part of this gate file.
- The still-thrown paths: repository / Redis lifecycle faults (`500`), the `ClientId`-empty invariant
  (`500`), and the shared `GetMovieSessionSeat` seat-not-found (`404`) — unchanged by this slice
  (F11, F13, F22).
- The end-to-end routing of `POST .../purchase` to `200`/`404`/`409` — no `WebApplicationFactory`
  harness; verified manually (validation.md scenarios).
- Field-level validation, `DbUpdateException` translation, performance/load/real concurrency.
