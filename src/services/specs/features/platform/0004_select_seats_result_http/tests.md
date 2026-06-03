# 0004 · SelectSeats Result→HTTP — Outside-in test spec

> **Deviation from the default template (intentional, per PRD — same family as slices `0002`/`0003`).**
> The standard outside-in test for this project goes through `WebApplicationFactory<Program>`. **No
> such harness exists** in this repository (the suites are `Domain.UnitTests`,
> `Domain.ArchitectureTests`, `Infrastructure.UnitTests`, `Application.LoadTests`, and the
> `API.UnitTests` project from `0002`). Slice `0003` already pinned the shared `Error → IResult`
> mapper (`ErrorResults`) with its own gate, so it is **not** re-tested here. The PRD's Testing
> Decisions choose **a focused unit spec of the converted `SelectSeatCommandHandler` as the
> acceptance/RED gate** — the load-bearing, externally-observable behaviour this slice changes is
> *which `Result` each outcome of the use-case produces* (and therefore the status the reused mapper
> yields) plus the atomicity invariant. The "entry point" below is therefore the handler's `Handle`
> method driven with its collaborators substituted; the endpoint's `Match` wiring is covered by
> compilation + code review, and the `MovieSessionSeat.Select` transition is pinned by a separate
> domain unit test (plan §6). The handler gate is **RED** until the conversion lands (today the
> handler throws `ContentNotFoundException` / only ever returns `Result.Success()`).

## Goal

Prove that the converted `select-seat` use-case **returns** the right `Result` for each outcome
instead of throwing — cart missing ⇒ `NotFoundError`; seat status not Available ⇒ `ConflictError`;
seat held by another cart ⇒ `ConflictError`; available seat ⇒ `Result.Success()` — and that on any
**failing** seat claim the shopping cart is **never persisted** (the atomicity invariant the thrown
path provided implicitly).

## Entry point

Not an HTTP route via `WebApplicationFactory`. The test constructs the handler and invokes it
directly:

- **Method under test:** `SelectSeatCommandHandler.Handle(SelectSeatCommand, CancellationToken)`
  (in `Application/ShoppingCarts/Command/SelectSeats/`), returning `Task<Result>`.
- **Command:** `new SelectSeatCommand(MovieSessionId: <sessionId>, SeatRow: 1, SeatNumber: 1,
  ShoppingCartId: <cartId>)`.
- **Headers / auth / idempotency:** none — this is an application-layer unit test, not a routed
  call.

## Wired real

- `SelectSeatCommandHandler` (the real handler, real control flow, real
  short-circuit-before-save).
- `MovieSessionSeatService` — a **`sealed` concrete class**, constructed **real** over its two
  substituted repositories (it cannot be a substitute). Exercises the real `SelectSeat` →
  `MovieSessionSeat.Select` path and the real `Result` propagation.
- `MovieSessionSeat` aggregate — real; its `Select` produces the real `ConflictError`s and the
  domain event.
- The real `Result` / `NotFoundError` / `ConflictError` types from `Domain/Error`.
- `ActiveShoppingCartHandler.SaveShoppingCart` base behaviour (real) — its
  `ActiveShoppingCartRepository.SaveAsync(...)` call is the observable persistence side-effect the
  scenarios assert on.

## Mocked

NSubstitute (the project's mocking library, as in `AssignClientCartCommandHandlerTests`):

- `IActiveShoppingCartRepository` — `GetByIdAsync(cartId)` returns the seeded cart or `null`;
  `SaveAsync(...)` is observed (the atomicity assertion).
- `IMovieSessionSeatRepository` — `GetByIdAsync(sessionId, row, number, ct)` returns the seeded
  `MovieSessionSeat` in the scenario's state; `UpdateAsync(...)` observed.
- `IMovieSessionsRepository` — `GetByIdAsync(sessionId, ct)` returns a non-terminated
  `MovieSession` (so `CheckSeatSaleAvailability` passes).
- `IDistributedLock` — `TryAcquireAsync(...)` returns an `ILockHandler` whose `IsLocked == true`
  (the lock is always acquired in these scenarios).
- `IShoppingCartSeatLifecycleManager` — `SetAsync(...)` returns `true` (Redis lifecycle succeeds on
  the happy path; not reached on the failing-claim scenarios because the handler short-circuits
  first).
- `IShoppingCartLifecycleManager` — `SetAsync(...)` no-op.
- `Serilog.ILogger` — no-op substitute.

No database, Redis, or RabbitMQ instance is touched.

## Fixtures / setup

- **Cart:** `ShoppingCart.Create(5, dataHasher)` (a substituted `IDataHasher`, as in
  `AssignClientCartCommandHandlerTests`), in a state that accepts a seat (so `EnsureSeatCanBeAdded`
  passes for the conflict/happy scenarios). Its id is the command's `ShoppingCartId`.
- **Movie session:** a substituted/`Create`d `MovieSession` with `SalesTerminated == false`.
- **Seat state construction** (`MovieSessionSeat` for `(sessionId, row 1, number 1)`):
  - *Available, unclaimed:* `MovieSessionSeat.Create(sessionId, number: 1, row: 1, price: 10m)`.
  - *Not Available:* an otherwise-identical seat whose `Status` is `Selected` (e.g. produced by
    invoking the internal `Select` once, given the Domain UnitTests assembly has `InternalsVisibleTo`,
    or by deserializing via the `[JsonConstructor]`).
  - *Available but owned by another cart:* a seat with `Status == Available` and
    `ShoppingCartId == <otherCartId>` (a non-empty, different cart) — this combination is a
    materialized state, built via the `[JsonConstructor]` (Newtonsoft) since the aggregate's own
    transitions never leave an Available seat with a non-empty owner.
- **Auth:** none — the unit under test has no authentication.

## Test scenarios

> These four scenarios are the slice's RED gate (the five PRD gate facts: the four outcomes plus the
> atomicity assertion folded into the two seat-conflict scenarios).

### Scenario 1: available seat ⇒ Result.Success() and the cart is saved

**Setup:**
- `GetByIdAsync(cartId)` ⇒ the seeded cart.
- `GetByIdAsync(sessionId, 1, 1, ct)` ⇒ an **Available, unclaimed** seat.
- `IMovieSessionsRepository.GetByIdAsync(sessionId, ct)` ⇒ non-terminated session.
- lock acquired (`IsLocked == true`); `IShoppingCartSeatLifecycleManager.SetAsync(...)` ⇒ `true`.

**Act:**
- `await handler.Handle(new SelectSeatCommand(sessionId, 1, 1, cartId), CancellationToken.None);`

**Expect:**
- `result.IsSuccess == true`.
- Persistence side-effect: `ActiveShoppingCartRepository.Received(1).SaveAsync(Arg.Any<ShoppingCart>())`,
  and `IMovieSessionSeatRepository.Received(1).UpdateAsync(...)` (seat persisted as Selected).

**Covers requirement(s):** F7, F8.

### Scenario 2: missing cart ⇒ NotFoundError, nothing saved

**Setup:**
- `GetByIdAsync(cartId)` ⇒ `null`.

**Act:**
- `await handler.Handle(new SelectSeatCommand(sessionId, 1, 1, cartId), CancellationToken.None);`

**Expect:**
- `result.IsFailure == true` and `result.Error` is a `NotFoundError`.
- Persistence side-effect: `ActiveShoppingCartRepository.DidNotReceive().SaveAsync(Arg.Any<ShoppingCart>())`.

**Covers requirement(s):** F4, F6.

### Scenario 3: seat status not Available ⇒ ConflictError, nothing saved

**Setup:**
- `GetByIdAsync(cartId)` ⇒ the seeded cart.
- `GetByIdAsync(sessionId, 1, 1, ct)` ⇒ a seat whose `Status` is **not Available** (e.g. Selected).
- non-terminated session; lock acquired.

**Act:**
- `await handler.Handle(new SelectSeatCommand(sessionId, 1, 1, cartId), CancellationToken.None);`

**Expect:**
- `result.IsFailure == true` and `result.Error` is a `ConflictError`.
- Persistence side-effect: `ActiveShoppingCartRepository.DidNotReceive().SaveAsync(Arg.Any<ShoppingCart>())`
  (atomicity: the cart is not persisted holding a seat whose claim failed).

**Covers requirement(s):** F5, F6, F11.

### Scenario 4: seat held by another cart ⇒ ConflictError, nothing saved

**Setup:**
- `GetByIdAsync(cartId)` ⇒ the seeded cart.
- `GetByIdAsync(sessionId, 1, 1, ct)` ⇒ a seat with `Status == Available` and
  `ShoppingCartId == <otherCartId>` (non-empty, different).
- non-terminated session; lock acquired.

**Act:**
- `await handler.Handle(new SelectSeatCommand(sessionId, 1, 1, cartId), CancellationToken.None);`

**Expect:**
- `result.IsFailure == true` and `result.Error` is a `ConflictError` — proving the
  `InvalidOperation → ConflictError` change keeps this a `409`, not a `500` (it is no longer a base
  `Error`).
- Persistence side-effect: `ActiveShoppingCartRepository.DidNotReceive().SaveAsync(Arg.Any<ShoppingCart>())`.

**Covers requirement(s):** F5, F6, F10.

> All four scenarios are **RED** against the current code: today the handler throws
> `ContentNotFoundException` for the missing cart, and `MovieSessionSeatService.SelectSeat` throws
> `ConflictException` for both seat conflicts (so no failing `Result` is ever returned), while the
> handler's only `Result` value is `Result.Success()`. The scenarios fail until the conversion in
> plan §5 lands.

## Out of scope for this test

- The shared `ErrorResults.ToProblem` mapping (`NotFoundError ⇒ 404`, `ConflictError ⇒ 409`,
  else `⇒ 500`) — already covered by slice `0003`'s `ErrorResultsOutsideInTests`; not re-tested.
- The `MovieSessionSeat.Select` domain transition in isolation (status not Available ⇒
  `ConflictError`, no event; another cart ⇒ `ConflictError`, no event; success ⇒ `Selected` + event)
  — covered by `MovieSessionSeatSpecification` (plan §6), written after green.
- The endpoint's `Match` wiring, the deleted `BadRequest` branch, and the `.Produces(...)` OpenAPI
  declarations (F1, F2, F18, F19) — covered by compilation + code review (validation.md).
- The still-thrown paths: distributed-lock `LockedException` (423), Redis-lifecycle
  `InvalidOperationException` (500) and its rollback, `EnsureSeatCanBeAdded` guards (409/400), and
  the shared-helper not-found checks (404) — unchanged by this slice (F14–F17).
- The end-to-end routing of `POST .../seats/select` to `200`/`404`/`409` — no
  `WebApplicationFactory` harness; verified manually (validation.md scenarios).
- Field-level validation, `DbUpdateException` translation, performance/load/real concurrency.
