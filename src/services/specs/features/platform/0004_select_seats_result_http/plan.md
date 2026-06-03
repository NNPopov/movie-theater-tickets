# 0004 · SelectSeats Result→HTTP — Implementation plan

## 1. Header

- **Aggregate / Module:** `platform` (a `SelectSeats` use-case conversion that spans
  `ShoppingCarts` (the command + handler), the `MovieSessionSeatService` domain service, and
  the `MovieSessionSeat` aggregate; filed under `platform` to keep the ADR-002 step-3 series
  together, like `0001`–`0003`).
- **Slice:** `0004_select_seats_result_http`
- **PRD:** ./prd.md
- **ADR:** `docs/adr/ADR-002-error-handling-model-result-vs-exceptions.md` (step 3, second
  conversion; ADR stays **Proposed**).
- **Reference slice:** `../0003_assign_client_cart_result_http/plan.md` — same `platform` module,
  same ADR-002 step-3 shape (remove a `Result → exception` bridge, resolve the handler `Result`
  with `Match`-to-HTTP through the **shared** `ErrorResults.ToProblem` mapper, status-preserving,
  pinned by a focused unit gate with **no** `WebApplicationFactory`). 0003 is the canonical
  template; this slice **reuses** its mapper unchanged and adds the wrinkles 0003 did not have: a
  **second, hidden** bridge one layer deeper (inside `MovieSessionSeatService.SelectSeat`), an
  in-aggregate event-raising transition behind real infrastructure, and the silent
  `InvalidOperation ⇒ 500` hazard.
- **HTTP path (no new route; existing route, mechanism swap):**
  - `POST /api/shoppingcarts/{ShoppingCartId}/seats/select` — the dead
    `failure => Results.BadRequest(failure.Description)` branch is replaced by
    `Match(() => Results.Ok(), ErrorResults.ToProblem)`. Status codes unchanged in and out:
    `200` / `404` / `409` (+ `400` / `423` / `500` from the still-thrown paths).
- **STABLE files touched:** **none.**
  - The endpoint file (`ShoppingCartEndpointApplicationBuilderExtensions.cs`, an `IEndpoints`
    implementation), the handler, the domain service, and the aggregate are all **feature code** —
    editing an endpoint delegate, a handler, a domain-service method, and an aggregate method is
    ordinary feature work.
  - `ErrorResults.cs` already exists (added by 0003); it is **reused unchanged** — not edited.
  - `CustomExceptionHandler.cs`, `DomainErrors`, `Error`/`ConflictError`/`NotFoundError`, the
    MediatR pipeline, and every base type are **not touched**. The
    `InvalidOperation → ConflictError` change reuses the **existing** `ConflictError` kind via the
    existing `DomainErrors<T>.ConflictException(...)` factory — no new `Error` type. If anything
    beyond §5 proves necessary — a new `Error` type, a `CustomExceptionHandler` change, a
    `400`-arm in the mapper, a base-type change — **stop and ask**; that exceeds ADR-002 step 3
    for this use-case.
- **No EF Core entity is added or altered → no migration.**

## 2. Context summary

This is ADR-002 step 3's **second conversion**. The `select-seat` use-case advertises the
`Result` model (`SelectSeatCommand : IRequest<Result>`) but actually runs on exceptions: every
business failure on the path is a `throw`, the handler's only `Result` value is
`Result.Success()`, and the endpoint carries a **dead, wrong** failure branch
(`failure => Results.BadRequest(...)`) that no real failure `Result` ever reaches. There is a
**second, hidden** bridge one layer deeper: `MovieSessionSeatService.SelectSeat` calls the
aggregate `MovieSessionSeat.Select` (which correctly returns a `Result`) and **re-throws** any
failure as a `ConflictException`, collapsing two distinct domain conflicts into one opaque
exception. This slice converts the path to a genuine `Result`: the handler **returns**
`NotFoundError` for cart-not-found and **propagates** the domain service's `Result` for the two
seat conflicts, short-circuiting on `IsFailure` **before** `SaveShoppingCart` (preserving the
"cart not saved on a failed claim" atomicity the thrown path provided implicitly); the service is
retyped `Task<MovieSessionSeat>` → `Task<Result>` and its `Result → ConflictException` re-throw is
deleted; and the aggregate's "another cart" branch returns a **`ConflictError`** (was a base
`Error` via `InvalidOperation`) so both conflicts map to `409` through the existing mapper rather
than one of them falling through to `500`. The endpoint resolves the handler's `Result` with
`Match(() => Results.Ok(), ErrorResults.ToProblem)` — the **same shared mapper** from 0003 — and
the OpenAPI surface is corrected to `200` / `404` / `409`. Every observable status is unchanged;
there is no intentional client-visible behaviour change. The acceptance gate is a focused unit
spec of the **converted handler** in `BookingManagementService.Domain.UnitTests` (no
`WebApplicationFactory`).

## 3. API contract

Mechanism swap only — request/response shapes are unchanged. No new request/response model.

### Endpoint — `POST /api/shoppingcarts/{ShoppingCartId}/seats/select`

- **Request:** `[FromRoute] Guid ShoppingCartId`; body `ReserveSeatsRequest(short Row, short
  Number, Guid ShowtimeId)` — unchanged. Mapped to
  `SelectSeatCommand(MovieSessionId: ShowtimeId, SeatRow: Row, SeatNumber: Number, ShoppingCartId)`
  — unchanged, `IRequest<Result>`.
- **Resolution:** `result.Match(() => Results.Ok(), ErrorResults.ToProblem)` — the failure branch
  returns an `IResult` directly through the shared mapper; the dead
  `failure => Results.BadRequest(failure.Description)` branch is **deleted**.
- **Status codes (unchanged in/out):**

  | Outcome | Mechanism after slice | Status |
  |---|---|---|
  | Available seat selected | `Result.Success()` ⇒ `Results.Ok()` | **200** |
  | Shopping cart not found (handler-local) | `Result` `NotFoundError` ⇒ mapper | **404** |
  | Seat status not Available (`Select`) | `Result` `ConflictError` ⇒ mapper | **409** |
  | Seat held by another cart (`Select`) | `Result` `ConflictError` (was base `Error`) ⇒ mapper | **409** |
  | `EnsureSeatCanBeAdded`: cart not InWork | `ConflictException` (unchanged) | 409 |
  | `EnsureSeatCanBeAdded`: wrong session / duplicate / max seats | `DomainValidationException` (unchanged) | 400 |
  | Movie session / seat not found (shared helper) | `ContentNotFoundException` (unchanged) | 404 |
  | Sales terminated (shared helper, bare `Exception`) | **deferred** — unchanged | 500 |
  | Distributed lock not acquired | `LockedException` (unchanged) | 423 |
  | Redis seat-lifecycle failure / rollback | `InvalidOperationException` (unchanged) | 500 |

- **`.Produces` corrected:** declare `200` / `404` / `409`; drop the stale `201` / `204`
  (user stories 19, 20). The `400` / `423` / `500` paths remain exception-driven via
  `CustomExceptionHandler` and are not declared here (consistent with sibling endpoints that do
  not enumerate exception statuses).

### Shared mapper — `ErrorResults.ToProblem(Error)` (existing, reused unchanged)

`API/Endpoints/Common/ErrorResults.cs` from slice 0003. `NotFoundError ⇒ 404`,
`ConflictError ⇒ 409` (title-only `"Conflict"`, no `Detail`), any other `Error ⇒ 500`. Already
covered by `ErrorResultsOutsideInTests` in `0003`; **not re-tested** here and **not edited**.

## 4. File structure

```
BookingManagement/
├── BookingManagementService.Domain/
│   ├── Seats/
│   │   └── MovieSessionSeat.cs                                    # EDIT: Select "another cart" branch InvalidOperation → ConflictException (ConflictError); event stays success-only
│   └── Services/
│       └── MovieSessionSeatService.cs                             # EDIT: SelectSeat Task<MovieSessionSeat> → Task<Result>; delete Result→ConflictException re-throw; propagate the aggregate Result
├── BookingManagementService.Application/
│   └── ShoppingCarts/Command/SelectSeats/
│       └── SelectSeatCommandHandler.cs                            # EDIT: cart-missing throw → return NotFoundError; SelectSeat now returns Result, short-circuit on IsFailure before SaveShoppingCart
└── BookingManagementService.API/
    └── Endpoints/
        └── ShoppingCartEndpointApplicationBuilderExtensions.cs   # EDIT: select delegate Match→mapper (delete dead BadRequest branch); .Produces 200/404/409

BookingManagement/tests/
└── BookingManagementService.Domain.UnitTests/                    # EXISTING (root namespace CinemaTicketBooking.Application.UnitTests; references Application; NSubstitute)
    ├── ShoppingCarts/
    │   └── SelectSeatCommandHandlerTests.cs                       # NEW: the RED acceptance gate (written by /slice-test-red, step 5)
    └── Seats/
        └── MovieSessionSeatSpecification.cs                       # NEW (after green): domain facts for MovieSessionSeat.Select
```

> Note (project naming, carried from slices 0002/0003): `BookingManagementService.Domain.UnitTests`
> is named "Domain" but its `RootNamespace` is `CinemaTicketBooking.Application.UnitTests` and it
> references the **Application** project with NSubstitute available — the correct home for the
> handler gate. Unlike 0003 (whose gate was the new mapper, living in `API.UnitTests`), **this
> slice's gate is the converted handler**, so it lives here next to
> `AssignClientCartCommandHandlerTests.cs`.

No EF Core entity is added or altered → **no migration**.

## 5. Implementation steps

1. **Domain — `MovieSessionSeat.Select`: return `ConflictError` for the "another cart" case.** In
   `Domain/Seats/MovieSessionSeat.cs`, change the second guard from `InvalidOperation` (a **base**
   `Error`) to `ConflictException` (a `ConflictError`), so both conflict outcomes of `Select` are
   `ConflictError` and map to `409`:
   ```csharp
   if (shoppingCartId != ShoppingCartId && ShoppingCartId != Guid.Empty)
       return DomainErrors<MovieSessionSeat>.ConflictException(
           "The place is already being processed by another shopping cart"); // was: .InvalidOperation(...)
   ```
   The first guard (`Status != SeatStatus.Available ⇒ ConflictException(...)`) is **unchanged**.
   The `MovieSessionSeatStatusUpdatedDomainEvent` continues to be appended **only on the success
   branch** (it already is — after both guards). This is the single sharpest hazard: without this
   change the converted "another cart" conflict would route through the mapper's `_ ⇒ 500`
   fallback, silently turning today's `409` into a `500` (user stories 12, 13; PRD "Further
   Notes"). No new `Error` type — reuses the existing `DomainErrors<T>.ConflictException(...)`
   factory / `ConflictError`. **Do not** touch `Sell`'s `InvalidOperation` (out of scope — that is
   the `Purchase` path).

2. **Domain — `MovieSessionSeatService.SelectSeat`: retype to `Task<Result>` and delete the
   hidden bridge.** In `Domain/Services/MovieSessionSeatService.cs`, change the signature from
   `Task<MovieSessionSeat>` to `Task<Result>`, remove the
   `else throw new ConflictException(...)`, and propagate the aggregate `Result`:
   ```csharp
   public async Task<Result> SelectSeat(Guid movieSessionId, short seatRow, short seatNumber,
       Guid shoppingCartId, string hashId, CancellationToken cancellationToken)
   {
       await CheckSeatSaleAvailability(movieSessionId, cancellationToken);          // unchanged (US18) — still throws

       var movieSessionSeat = await GetMovieSessionSeat(movieSessionId, seatRow, seatNumber, cancellationToken); // unchanged (US18)

       var result = movieSessionSeat.Select(shoppingCartId, hashId);

       if (result.IsFailure)
           return result;                                                            // propagate — no re-throw

       await _movieSessionSeatRepository.UpdateAsync(movieSessionSeat, cancellationToken); // success branch only (as before)

       return Result.Success();
   }
   ```
   The persist-only-on-success behaviour is preserved; the two distinct `Select` conflicts now stay
   distinct in their `Error.Description` instead of being flattened into one `ConflictException`
   (user stories 10, 11). `CheckSeatSaleAvailability` and `GetMovieSessionSeat` (the **shared**
   helpers, also used by `Reserve`/`Purchase`) keep throwing — **not** converted in this slice
   (user story 18; out of scope). The now-unused `using
   CinemaTicketBooking.Domain.Exceptions;` / `Application.Exceptions;` imports in this file are
   removed **only if** no other method in the file still needs them (`GetMovieSessionSeat` and
   `CheckSeatSaleAvailability` still throw `ContentNotFoundException` → `Domain.Exceptions` stays).

3. **Application — `SelectSeatCommandHandler`: cart-missing returns `NotFoundError`; propagate the
   seat-claim `Result`; short-circuit before save.** In
   `Application/ShoppingCarts/Command/SelectSeats/SelectSeatCommandHandler.cs`:
   - Replace `GetShoppingCartOrThrow` (which throws `ContentNotFoundException`) with a load that
     **returns** a `NotFoundError` when the cart is missing, so the handler-local not-found becomes
     a `Result` (`404` via the mapper — user stories 2, 8):
     ```csharp
     var cart = await ActiveShoppingCartRepository.GetByIdAsync(request.ShoppingCartId);
     if (cart is null)
         return DomainErrors<ShoppingCart>.NotFound(
             $"The shopping cart {request.ShoppingCartId} was not found.");
     ```
   - `SelectSaveSeatWithTimeoutRollback` now consumes `SelectSeat`'s `Result`. Make it return
     `Task<Result>`: if the seat claim fails, **return the failure** (do **not** run the Redis
     lifecycle); on success, run the existing Redis-lifecycle-with-rollback block unchanged and
     return `Result.Success()`:
     ```csharp
     var selectResult = await movieSessionSeatService.SelectSeat(
         request.MovieSessionId, request.SeatRow, request.SeatNumber,
         request.ShoppingCartId, cart.HashId, cancellationToken);

     if (selectResult.IsFailure)
         return selectResult;                       // seat conflict — short-circuit, before any persistence

     // ... existing try { SetAsync ... if(!result) ReturnSeatToAvailable } catch { ReturnSeatToAvailable; throw } ...
     return Result.Success();
     ```
   - In `Handle`, short-circuit on the seat-claim failure **before** `SaveShoppingCart`, so a cart
     is never persisted holding a seat whose claim failed (the atomicity invariant — user stories
     14, 24):
     ```csharp
     cart.AddSeats(selectSeat, request.MovieSessionId);

     var claimResult = await SelectSaveSeatWithTimeoutRollback(request, cancellationToken, cart, expires);
     if (claimResult.IsFailure)
         return claimResult;                        // cart NOT saved on a failed claim

     await SaveShoppingCart(cart);
     // (after the using block)
     return Result.Success();
     ```
   - **Unchanged and still exceptions** (per the PRD failure-classification table): the
     distributed-lock guard (`EnsureDistributedLockIsNotLocked ⇒ LockedException ⇒ 423`, US15), the
     Redis seat-lifecycle failure and its `ReturnSeatToAvailable` rollback
     (`InvalidOperationException ⇒ 500`, US16), and the `cart.EnsureSeatCanBeAdded(...)` guards
     (`ConflictException` / `DomainValidationException ⇒ 409` / `400`, US17). The unused
     `GetShoppingCartOrThrow` helper and now-unused `using CinemaTicketBooking.Domain.Exceptions;`
     /`Application.Exceptions;` imports are removed **only if** nothing else in the file uses them
     (the lock/Redis paths still throw `LockedException` / `InvalidOperationException` → those
     namespaces stay; `ContentNotFoundException` is no longer thrown here so `Domain.Exceptions`
     may become removable — verify before deleting).

4. **API — endpoint: replace the dead branch with the shared mapper.** In
   `ShoppingCartEndpointApplicationBuilderExtensions.cs`, the `seats/select` delegate becomes:
   ```csharp
   var result = await sender.Send(query, cancellationToken);

   return result.Match(
       () => Results.Ok(),
       ErrorResults.ToProblem);
   ```
   Delete the `failure => Results.BadRequest(failure.Description)` branch (user stories 4, 5, 9).
   Correct the metadata: replace `.Produces(201).Produces(204)` with
   `.Produces(200).Produces(404).Produces(409)` (user stories 19, 20). `.WithName("SelectSeat")`,
   `.WithTags(Tag)` unchanged. `ErrorResults` is already imported in this file (the `assignclient`
   delegate uses it).

5. **Verify (pre-test).** From `src/services`:
   ```
   dotnet format CinemaBookingManagement.sln
   dotnet build  CinemaBookingManagement.sln -warnaserror
   ```
   Resolve warnings — in particular the `SelectSeat` retype ripples to every caller of
   `MovieSessionSeatService.SelectSeat` (only `SelectSeatCommandHandler` calls it; confirm no other
   caller assumes the returned `MovieSessionSeat`). The accepted AutoMapper **NU1903** NuGet-audit
   advisory trips `-warnaserror` at restore time (known, accepted — see MEMORY `dotnet10-migration`);
   handle the NuGet audit so the real build/warnings are what is validated, not the advisory.

## 6. Tests planned

The externally observable behaviour is the `Result`/`Error` each outcome produces (hence the
status the shared mapper yields), the domain transition's outcome and its event, and the atomicity
invariant. There is **no** `WebApplicationFactory<Program>` harness; the change is pinned by
focused unit tests of the changed units, consistent with `0001`–`0003`.

- **Handler unit test — RED acceptance gate — NEW
  `BookingManagement/tests/BookingManagementService.Domain.UnitTests/ShoppingCarts/SelectSeatCommandHandlerTests.cs`.**
  xUnit + FluentAssertions + NSubstitute (same conventions as
  `AssignClientCartCommandHandlerTests.cs`). NSubstitute mocks `IActiveShoppingCartRepository`,
  `IShoppingCartSeatLifecycleManager`, `IDistributedLock` (+ `ILockHandler` with `IsLocked == true`),
  `IShoppingCartLifecycleManager`, and `ILogger`. **`MovieSessionSeatService` is a `sealed`
  concrete class** — it is constructed **real** over mocked `IMovieSessionSeatRepository` +
  `IMovieSessionsRepository` (not substituted), with a non-terminated `MovieSession` and a
  `MovieSessionSeat` driven into the required state via its factory/`Select`. Facts (the gate, red
  until the handler genuinely returns these `Result`s):
  1. cart missing ⇒ result is `NotFoundError`; `SaveAsync` **not** received.
  2. seat status not Available ⇒ result is `ConflictError`; `SaveAsync` **not** received (atomicity).
  3. seat held by another cart ⇒ result is `ConflictError`; `SaveAsync` **not** received (atomicity).
  4. available seat ⇒ `Result.Success()`; cart **is** saved.
  This is RED until steps 1–3 land (today the handler throws / only ever returns `Result.Success()`).

- **Domain unit test (after green)** — NEW
  `BookingManagement/tests/BookingManagementService.Domain.UnitTests/Seats/MovieSessionSeatSpecification.cs`
  (AAA / `*Specification` convention, like `ShoppingCarts/ShoppingCartSpecification.cs`). Facts for
  `MovieSessionSeat.Select`: status not Available ⇒ `ConflictError`, **no**
  `MovieSessionSeatStatusUpdatedDomainEvent`; another shopping cart ⇒ `ConflictError`, **no** event;
  success ⇒ `Status == Selected` **and** the event raised. Pins the `InvalidOperation →
  ConflictError` change and the event-on-success behaviour (user story 25). *(`Select` is
  `internal`; the test project needs visibility — confirm `InternalsVisibleTo` for the Domain
  UnitTests assembly exists, as the existing domain specs already exercise internal aggregate
  methods; if absent it is added to the Domain project — flag, do not assume.)*

**Opt-outs (explicit, per `agent_docs/testing.md` and the PRD Testing Decisions):**
- **`WebApplicationFactory` end-to-end / endpoint integration test — skipped:** no HTTP harness
  exists; standing one up for one endpoint is disproportionate. The endpoint's `Match` wiring is
  covered by compilation, and the shared mapper is already covered by slice 0003's
  `ErrorResultsOutsideInTests` (PRD "Out of the net"; user story 22).
- **Repository / adapter unit test — skipped:** no repository or adapter logic changes (no new
  business-meaningful infrastructure-exception translation on this path).
- **Real-concurrency (two carts racing one seat) test — deferred** to a separate
  Infrastructure-level integration test, not this slice's gate (PRD Out of Scope).

**Full-suite gate:** the entire `dotnet test` run, **including
`BookingManagementService.Domain.ArchitectureTests`**, must stay green (user story 26).

## 7. Out of scope for this slice

- Converting any other use-case (`ReserveTickets`, `PurchaseTickets`) — later slices that reuse
  this slice's pattern and the 0003 mapper (user story 27).
- Adopting `Result<T>` (the generic) on this path — `SelectSeats` uses the non-generic `Result`.
- Fixing the bare `throw new Exception(...terminated)` in the **shared**
  `CheckSeatSaleAvailability`, or converting any shared `MovieSessionSeatService` helper
  (`GetMovieSessionSeat`, the movie-session lookup) to `Result` — they stay exceptions until
  `Reserve`/`Purchase` land (user story 18).
- Adding a `400`-class arm to `ErrorResults.ToProblem` or otherwise changing the shared mapper /
  introducing a new `Error` kind (ADR-level).
- The bare `throw` in the endpoints' `GetClientId` / `CreateShoppingCart` helpers.
- Changing `CustomExceptionHandler`, the MediatR pipeline, the validation behaviour, or any base
  type.
- Standing up a `WebApplicationFactory` HTTP integration harness, or a real-concurrency seat-race
  integration test.
- Flipping ADR-002 to Accepted.

## 8. Open questions

None blocking. Two items are verified during implementation rather than assumed:
- **`InternalsVisibleTo` for the Domain UnitTests assembly** — `MovieSessionSeat.Select` is
  `internal`; the domain spec needs visibility. Existing domain specs exercise internal aggregate
  methods, so it is expected to be present already; confirm before writing the domain test (§6).
- **Removability of now-unused `using` imports** in the handler and the domain service after the
  conversion — verified by `dotnet build -warnaserror` (§5), since other paths in both files still
  throw `LockedException` / `InvalidOperationException` / `ContentNotFoundException`.
