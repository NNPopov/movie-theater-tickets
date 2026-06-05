# 0005 · ReserveTickets Result→HTTP — Implementation plan

## 1. Header

- **Aggregate / Module:** `platform` (a `ReserveTickets` use-case conversion that spans
  `ShoppingCarts` (the command + handler and the `ShoppingCart.SeatsReserve` aggregate method),
  the **shared** `MovieSessionSeatService.CheckSeatSaleAvailability` domain-service helper, and
  the `reservations` endpoint; filed under `platform` to keep the ADR-002 step-3 series together,
  like `0001`–`0004`).
- **Slice:** `0005_reserve_tickets_result_http`
- **PRD:** ./prd.md
- **ADR:** `docs/adr/ADR-002-error-handling-model-result-vs-exceptions.md` (step 3, **third**
  conversion; ADR stays **Proposed** — acceptance rides with `0006`).
- **Reference slice:** `../0004_select_seats_result_http/plan.md` — same `platform` module, same
  ADR-002 step-3 shape (remove a `Result → exception` bridge, resolve the handler `Result` with
  `Match`-to-HTTP through the **shared** `ErrorResults.ToProblem` mapper, pin with a focused
  handler unit gate, **no** `WebApplicationFactory`). `0004` is the closest handler-gate template
  (same mocked-collaborator shape). `0003`'s `AssignClientId` is the template for the
  `void → Result` aggregate-method conversion (event on a genuine transition only).
- **HTTP path (no new route; existing route, mechanism swap):**
  - `POST /api/shoppingcarts/{ShoppingCartId}/reservations` — `return result;` is replaced by
    `result.Match(() => Results.Ok(), ErrorResults.ToProblem)`. **Behaviour-changing** (this is the
    point of the slice, see §3): seat-not-reservable and sales-terminated move `500 → 409`; the
    success body changes from a serialized `Result` object to **empty**; status stays `200`.
- **STABLE files touched:** **none.**
  - The endpoint file (`ShoppingCartEndpointApplicationBuilderExtensions.cs`, an `IEndpoints`
    implementation), the handler, the domain-service helper, and the aggregate method are all
    **feature code** — editing an endpoint delegate, a handler, a domain-service method, and an
    aggregate method is ordinary feature work.
  - `ErrorResults.cs` already exists (added by `0003`, reused unchanged by `0004`); it is
    **reused unchanged** — not edited.
  - `CustomExceptionHandler.cs`, `DomainErrors`, `Error`/`ConflictError`/`NotFoundError`, the
    MediatR pipeline, the validation behaviour, and every base type are **not touched**. The
    conversion reuses the **existing** `ConflictError` / `NotFoundError` kinds via the existing
    `DomainErrors<ShoppingCart>.ConflictException(...)` / `.NotFound(...)` factories — **no new
    `Error` type**. If anything beyond §5 proves necessary — a new `Error` type, a
    `CustomExceptionHandler` change, a `400`-arm in the mapper, a base-type change — **stop and
    ask**; that exceeds ADR-002 step 3 for this use-case.
- **No EF Core entity is added or altered → no migration.**

## 2. Context summary

This is ADR-002 step 3's **third conversion**, and the most behaviour-correcting of the series.
The `reserve-tickets` use-case advertises the `Result` model (`ReserveTicketsCommand :
IRequest<Result>`) but runs entirely on exceptions: cart-not-found `throw`s
`ContentNotFoundException`; `ShoppingCart.SeatsReserve()` is `void` and `throw`s
`ConflictException` (via the shared `EnsurePurchaseIsNotCompleted()` guard) for an
already-purchased cart; a failing seat-reservation `Result` from `MovieSessionSeatService.ReserveSeats`
is **re-thrown as a bare `throw new Exception("Couldn't Reserve …")`** (the exact anti-pattern
ADR-002 forbids), collapsing a legitimate seat conflict to `500`; the shared
`CheckSeatSaleAvailability` `throw`s a bare `Exception` for a terminated session (`500`); and the
endpoint does `return result;`, serializing the `Result` object as a `200` body and never reaching
`ErrorResults.ToProblem`. This slice makes the handler genuinely **return** a failing `Result` for
every expected outcome and short-circuit **before** persistence; converts `SeatsReserve()` to
`Result` (fixing the unconditional-event bug as a side-effect); retypes the shared
`CheckSeatSaleAvailability` to `Task<Result>` and threads it through its three callers; deletes the
bare-`Exception` bridge; and resolves the endpoint with the shared mapper. Unlike `0004`, this
conversion **intentionally changes two observable statuses** (`500 → 409` for seat-not-reservable
and for terminated) and empties the success body — those are the explicit client-visible changes.
The acceptance gate is a focused unit spec of the **converted handler** in
`BookingManagementService.Domain.UnitTests` (no `WebApplicationFactory`).

## 3. API contract

Mechanism swap on an existing route — no new request/response model.

### Endpoint — `POST /api/shoppingcarts/{ShoppingCartId}/reservations`

- **Request:** `[FromRoute] Guid ShoppingCartId`; **no body**. Mapped to
  `ReserveTicketsCommand(ShoppingCartId)` — unchanged, `IRequest<Result>`.
- **Resolution:** `result.Match(() => Results.Ok(), ErrorResults.ToProblem)` — replaces
  `return result;`. Success is `Results.Ok()` (**empty** `200` body, previously a serialized
  `Result` object).
- **Status codes (the contract this slice locks in):**

  | Outcome | Mechanism before | Status before | Mechanism after | Status after |
  |---|---|---|---|---|
  | Reservation succeeds | `return result;` (serialized `Result` body) | 200 + body | `Result.Success()` ⇒ `Results.Ok()` (empty) | **200** |
  | Shopping cart not found (handler-local) | `throw ContentNotFoundException` | 404 | `Result` `NotFoundError` ⇒ mapper | 404 |
  | Movie session not found (shared helper) | `throw ContentNotFoundException` | 404 | `Result` `NotFoundError` ⇒ mapper | 404 |
  | Sales terminated (shared helper) | `throw new Exception(...)` | **500** | `Result` `ConflictError` ⇒ mapper | **409** |
  | Seat not reservable / another cart (`Reserve`) | bare `throw new Exception` (handler bridge) | **500** | `Result` `ConflictError` ⇒ mapper | **409** |
  | Cart already purchased (`SeatsReserve`) | `throw ConflictException` | 409 | `Result` `ConflictError` ⇒ mapper | 409 |
  | Movie session seat not found (`GetMovieSessionSeat`) | `throw ContentNotFoundException` | 404 | exception (**unchanged**) | 404 |
  | Repository / Redis lifecycle fault | exception | 500 | exception (**unchanged**) | 500 |

- **`.Produces` corrected:** declare `200` / `404` / `409`; drop the stale `.Produces<bool>(201)` /
  `.Produces(204)` (user stories 24, 25). The `500` (genuinely-unexpected) path remains
  exception-driven via `CustomExceptionHandler` and is not declared here (consistent with sibling
  endpoints).

### Shared mapper — `ErrorResults.ToProblem(Error)` (existing, reused unchanged)

`API/Endpoints/Common/ErrorResults.cs` from slice `0003`. `NotFoundError ⇒ 404`,
`ConflictError ⇒ 409` (title-only `"Conflict"`, no `Detail`), any other `Error ⇒ 500`. Already
covered by `ErrorResultsOutsideInTests` in `0003`; **not re-tested** here and **not edited** (user
story 26). Note: `MovieSessionSeat.Reserve` already returns `ConflictError` for its bad-status case
(verified in source — `DomainErrors<MovieSessionSeat>.ConflictException(...)`, not
`InvalidOperation`), so the seat-conflict path maps cleanly to `409`; there is **no**
`InvalidOperation → 500` trap on the reserve path (that hazard lives on the *purchase* path via
`Sell`, and is `0006`'s problem).

## 4. File structure

```
BookingManagement/
├── BookingManagementService.Domain/
│   ├── ShoppingCarts/
│   │   └── ShoppingCart.cs                                       # EDIT: SeatsReserve() void → Result; stop calling EnsurePurchaseIsNotCompleted; event only on genuine InWork→SeatsReserved transition; idempotent on SeatsReserved; ConflictError on PurchaseCompleted
│   └── Services/
│       └── MovieSessionSeatService.cs                            # EDIT: CheckSeatSaleAvailability Task → Task<Result> (session-not-found ⇒ NotFoundError, terminated ⇒ ConflictError, replacing bare Exception); thread the Result through SelSeats, ReserveSeats, SelectSeat (short-circuit on IsFailure)
├── BookingManagementService.Application/
│   └── ShoppingCarts/Command/ReserveSeats/
│       └── ReserveTicketsCommandHandler.cs                       # EDIT: cart-missing throw → return NotFoundError; consume SeatsReserve() Result; delete bare throw new Exception; propagate ReserveSeats Result; short-circuit BEFORE SaveAsync/SetAsync/per-seat DeleteAsync (atomicity)
└── BookingManagementService.API/
    └── Endpoints/
        └── ShoppingCartEndpointApplicationBuilderExtensions.cs   # EDIT: reservations delegate return result; → Match(() => Results.Ok(), ErrorResults.ToProblem); .Produces 200/404/409 (drop 201/204)

BookingManagement/tests/
└── BookingManagementService.Domain.UnitTests/                    # EXISTING (RootNamespace CinemaTicketBooking.Application.UnitTests; references Application; NSubstitute)
    └── ShoppingCarts/
        ├── ReserveTicketsCommandHandlerTests.cs                  # NEW: the RED acceptance gate (written by /slice-test-red, step 5) — next to AssignClientCartCommandHandlerTests.cs / SelectSeatCommandHandlerTests.cs
        └── ShoppingCartSpecification.cs                          # EDIT (after green): add SeatsReserve facts (InWork ⇒ SeatsReserved + event; SeatsReserved ⇒ success, no event; PurchaseCompleted ⇒ ConflictError, no event)
```

> Project-naming note (carried from `0002`–`0004`): `BookingManagementService.Domain.UnitTests` is
> named "Domain" but its `RootNamespace` is `CinemaTicketBooking.Application.UnitTests` and it
> references the **Application** project with NSubstitute available — the correct home for the
> handler gate. The `SeatsReserve` domain facts also live here (the existing
> `ShoppingCartSpecification.cs` already exercises `ShoppingCart`).

No EF Core entity is added or altered → **no migration**.

## 5. Implementation steps

1. **Domain — `ShoppingCart.SeatsReserve()`: `void → Result`.** In
   `Domain/ShoppingCarts/ShoppingCart.cs`, convert the method (currently lines 187–195). Stop
   calling the shared `EnsurePurchaseIsNotCompleted()` (it stays for `PurchaseComplete` /
   `CalculateCartAmount` / others — **do not modify it**) and inline a `Result`-returning guard,
   following the `0003` `AssignClientId` template. Append the event **only** on a genuine
   `InWork → SeatsReserved` transition (this fixes the unconditional-event bug; user stories
   14/15/16/17):
   ```csharp
   public Result SeatsReserve()
   {
       if (Status == ShoppingCartStatus.PurchaseCompleted)
           return DomainErrors<ShoppingCart>.ConflictException(
               $"The shopping cart {Id} has already been purchased.");

       if (Status == ShoppingCartStatus.SeatsReserved)
           return Result.Success();                  // idempotent — no duplicate event

       if (Status != ShoppingCartStatus.InWork)
           return DomainErrors<ShoppingCart>.ConflictException(
               $"The shopping cart {Id} cannot be reserved from status {Status}.");

       Status = ShoppingCartStatus.SeatsReserved;    // genuine transition
       _domainEvents.Add(new ShoppingCartReservedDomainEvent(Id));

       return Result.Success();
   }
   ```
   `DomainErrors` and `ShoppingCartReservedDomainEvent` are already imported in this file.
   The third guard (non-`InWork`, non-`SeatsReserved`, non-`PurchaseCompleted` — only `Deleted`
   realistically) returns a `ConflictError`; see §8 (the prior `void` code added an event to a
   `Deleted` cart without transitioning — that latent path is not in the PRD's three-case contract).

2. **Domain — `MovieSessionSeatService.CheckSeatSaleAvailability`: `Task → Task<Result>`.** In
   `Domain/Services/MovieSessionSeatService.cs`, retype the private helper and replace both throws
   with returned errors (user story 18):
   ```csharp
   private async Task<Result> CheckSeatSaleAvailability(Guid movieSessionId,
       CancellationToken cancellationToken)
   {
       var movieSession = await _movieSessionsRepository.GetByIdAsync(movieSessionId, cancellationToken);

       if (movieSession is null)
           return DomainErrors<MovieSession>.NotFound(movieSessionId.ToString());

       if (movieSession.SalesTerminated)
           return DomainErrors<MovieSession>.ConflictException(
               $"{nameof(MovieSession)} has been terminated.");

       return Result.Success();
   }
   ```
   Then thread the `Result` through **all three** callers — `SelSeats`, `ReserveSeats`, and
   `SelectSeat` — short-circuiting on `IsFailure` at the top of each (user story 19). For each:
   ```csharp
   var availability = await CheckSeatSaleAvailability(movieSessionId, cancellationToken);
   if (availability.IsFailure)
       return availability;
   ```
   `GetMovieSessionSeat` (the other shared helper) keeps throwing `ContentNotFoundException`
   (`404`) — **not** converted (user story 22; out of scope). `using
   CinemaTicketBooking.Domain.Exceptions;` stays (still used by `GetMovieSessionSeat`);
   `Application.Exceptions` becomes unused only if nothing else needs it — verify by
   `-warnaserror`, remove only if flagged.

3. **Application — `ReserveTicketsCommandHandler`: return Results, delete the bare bridge,
   short-circuit before persistence.** In
   `Application/ShoppingCarts/Command/ReserveSeats/ReserveTicketsCommandHandler.cs`, rewrite
   `Handle` (user stories 10/11/13/21):
   ```csharp
   public async Task<Result> Handle(ReserveTicketsCommand request, CancellationToken cancellationToken)
   {
       var cart = await _activeShoppingCartRepository.GetByIdAsync(request.ShoppingCartId);
       if (cart is null)
           return DomainErrors<ShoppingCart>.NotFound(request.ShoppingCartId.ToString());   // was: throw ContentNotFoundException

       var reserveResult = cart.SeatsReserve();           // now returns Result (PurchaseCompleted ⇒ ConflictError)
       if (reserveResult.IsFailure)
           return reserveResult;

       var result = await _movieSessionSeatService.ReserveSeats(cart.MovieSessionId,
           cart.Seats.Select(t => (t.SeatRow, t.SeatNumber)).ToList(),
           request.ShoppingCartId,
           cancellationToken);

       if (result.IsFailure)
           return result;                                 // was: throw new Exception("Couldn't Reserve …") — DELETED

       await _activeShoppingCartRepository.SaveAsync(cart);          // reached only on full success
       await _shoppingCartLifecycleManager.SetAsync(cart.Id);

       foreach (var seat in cart.Seats)
           await _shoppingCartSeatLifecycleManager.DeleteAsync(cart.MovieSessionId, seat.SeatRow, seat.SeatNumber);

       _logger.Debug("ShoppingCart was reserved {@ShoppingCart}", cart);
       return Result.Success();
   }
   ```
   Delete the now-unused `GetShoppingCartOrThrow` helper. The short-circuits put every failing
   `Result` **before** `SaveAsync`, `SetAsync`, and the per-seat `DeleteAsync` — the atomicity
   invariant the thrown path provided implicitly (user stories 21/27). `using
   CinemaTicketBooking.Application.Exceptions;` (for `ContentNotFoundException`) becomes unused —
   remove it (verify by `-warnaserror`). Genuinely unexpected faults (repository/Redis) still
   propagate as exceptions (user story 23). The pre-existing seat-then-cart persistence ordering
   inside `ReserveSeats` is **not** changed (out of scope).

4. **API — endpoint: `return result;` → shared mapper.** In
   `ShoppingCartEndpointApplicationBuilderExtensions.cs`, the `reservations` delegate (currently
   lines 136–149) becomes:
   ```csharp
   var query = new ReserveTicketsCommand(ShoppingCartId: shoppingCartId);
   var result = await sender.Send(query, cancellationToken);

   return result.Match(
       () => Results.Ok(),
       ErrorResults.ToProblem);
   ```
   Replace `.Produces<bool>(201, "application/json").Produces(204)` with
   `.Produces(200).Produces(404).Produces(409)` (user stories 24/25). `.WithName("ReserveSeats")`,
   `.WithTags(Tag)` unchanged. `ErrorResults` is already imported in this file (the `assignclient`
   and `seats/select` delegates use it). **Do not** touch the sibling `purchase` delegate — it stays
   `return result;` until `0006` (but see §8 for the side-effect of step 2 on that path).

5. **Verify (pre-test).** From `src/services` (use the x86 SDK at
   `C:\Program Files (x86)\dotnet\dotnet.exe`; run via the PowerShell tool — see MEMORY
   `dotnet-sdk-path`):
   ```
   dotnet format CinemaBookingManagement.sln
   dotnet build  CinemaBookingManagement.sln -warnaserror
   ```
   Resolve warnings. The `CheckSeatSaleAvailability` retype ripples to all three callers (confirmed:
   `SelSeats`, `ReserveSeats`, `SelectSeat` — no other callers). The accepted AutoMapper **NU1903**
   NuGet-audit advisory trips `-warnaserror` at restore time (known/accepted — MEMORY
   `dotnet10-migration`); handle the NuGet audit so real build warnings are what is validated.

## 6. Tests planned

The externally observable behaviour is the `Result`/`Error` each outcome produces (hence the status
the shared mapper yields), the `SeatsReserve` transition's outcome and its event, and the atomicity
invariant (no persistence on failure). There is **no** `WebApplicationFactory<Program>` harness; the
change is pinned by focused unit tests of the changed units, consistent with `0001`–`0004`.

- **Handler unit test — RED acceptance gate — NEW
  `BookingManagement/tests/BookingManagementService.Domain.UnitTests/ShoppingCarts/ReserveTicketsCommandHandlerTests.cs`.**
  xUnit + FluentAssertions + NSubstitute (same conventions as `SelectSeatCommandHandlerTests.cs`).
  NSubstitute mocks `IActiveShoppingCartRepository`, `IShoppingCartSeatLifecycleManager`,
  `IShoppingCartLifecycleManager`, and `ILogger`. **`MovieSessionSeatService` is a `sealed`
  concrete class** — construct it **real** over mocked `IMovieSessionSeatRepository` +
  `IMovieSessionsRepository` (drive a non-terminated/terminated `MovieSession` and the
  `MovieSessionSeat`s into the required state via factory/`Reserve`). Facts (RED until the handler
  genuinely returns these `Result`s):
  1. cart missing ⇒ result is `NotFoundError`; `SaveAsync` **not** received.
  2. movie session missing ⇒ `NotFoundError`; `SaveAsync` / `SetAsync` **not** received.
  3. sales terminated ⇒ `ConflictError`; `SaveAsync` / `SetAsync` **not** received.
  4. a seat not reservable ⇒ `ConflictError`; `SaveAsync` / `SetAsync` **not** received (atomicity).
  5. cart already purchased ⇒ `ConflictError`; `SaveAsync` **not** received.
  6. success ⇒ `Result.Success()`; cart **is** saved, `SetAsync` called, and the per-seat
     `IShoppingCartSeatLifecycleManager.DeleteAsync` entries deleted.
  RED today because the handler throws (`ContentNotFoundException`, bare `Exception`,
  `ConflictException`) or returns a serialized `Result` rather than these typed `Error`s.

- **Domain unit test (after green)** — EDIT
  `BookingManagement/tests/BookingManagementService.Domain.UnitTests/ShoppingCarts/ShoppingCartSpecification.cs`
  (AAA / `*Specification` convention). Facts for `SeatsReserve`: `InWork` ⇒ `Status == SeatsReserved`
  **and** `ShoppingCartReservedDomainEvent` raised; already `SeatsReserved` ⇒ `Result.Success()`
  and **no** event; `PurchaseCompleted` ⇒ `ConflictError` and no event. Pins the `void → Result`,
  idempotency, and event-on-success-only changes (user story 28).

- **Regression** — re-run slice `0004`'s
  `ShoppingCarts/SelectSeatCommandHandlerTests.cs` **unchanged** as the regression gate for the
  shared-helper retype (`CheckSeatSaleAvailability → Task<Result>` threaded through `SelectSeat`).
  Its observable behaviour is unchanged — its tests never exercised the terminated branch (user
  story 20).

**Opt-outs (explicit, per `agent_docs/testing.md` and the PRD Testing Decisions):**
- **`WebApplicationFactory` end-to-end / endpoint integration test — skipped:** no HTTP harness
  exists; the endpoint's `Match` wiring is covered by compilation and the shared mapper by `0003`'s
  `ErrorResultsOutsideInTests`.
- **Repository / adapter unit test — skipped:** no repository/adapter logic changes (no new
  business-meaningful infrastructure-exception translation on this path).
- **Real-concurrency (two carts racing one seat) test — deferred** to a separate
  Infrastructure-level integration test, not this slice's gate (PRD Out of Scope).

**Full-suite gate:** the entire `dotnet test` run, **including
`BookingManagementService.Domain.ArchitectureTests`**, must stay green (user story 29).

## 7. Out of scope for this slice

- Converting `PurchaseTickets` and its endpoint `Match` wiring — slice `0006` (which must also handle
  `MovieSessionSeat.Sell`'s `InvalidOperation` `409 → 500` trap on the purchase path).
- Converting the shared `GetMovieSessionSeat` (seat-not-found) helper to `Result` — it stays a thrown
  `ContentNotFoundException` (`404`) (user story 22).
- Modifying the shared `EnsurePurchaseIsNotCompleted` guard (still used by `PurchaseComplete` /
  `CalculateCartAmount`) (user story 17).
- Adopting `Result<T>` (the generic) on this path — `ReserveTickets` uses the non-generic `Result`;
  success carries no value.
- Adding a `400`-class arm to `ErrorResults.ToProblem` or otherwise changing the shared mapper /
  introducing a new `Error` kind (ADR-level).
- The bare `throw` in the endpoints' `GetClientId` / `CreateShoppingCart` helpers.
- Fixing the pre-existing seat-then-cart transactional ordering gap inside `ReserveSeats`.
- Changing `CustomExceptionHandler`, the MediatR pipeline, the validation behaviour, or any base type.
- Standing up a `WebApplicationFactory` HTTP integration harness, or a real-concurrency seat-race
  integration test.
- Flipping ADR-002 to Accepted and updating `agent_docs/error_handling.md` (rides with `0006`).
- The Flutter client follow-up to the `0002` `204 → 404` contract change.

## 8. Open questions

Both items below were raised with the user and **resolved** (2026-06-03); recorded here so the
downstream spec steps follow the settled reading and do not re-open them.

- **Q1 — purchase-path side-effect → RESOLVED: accept as interim (option a).** The interim
  `404 → 200` / `500 → 200` on the *purchase* path is accepted; `0006` closes it. Flag in `0006`'s
  notes so it is not forgotten.
- **Q2 — `SeatsReserve()` on `Deleted`/other status → RESOLVED: return `ConflictError`, no event**
  (option a), as written in §5 step 1.

1. **The shared-helper retype has an observable side-effect on the not-yet-converted purchase
   path.** `PurchaseTicketsCommandHandler` **already** does `if (result.IsFailure) return result;`
   on `SelSeats` and **already** returns `NotFoundError` for a missing cart — but its endpoint still
   does `return result;` (no `Match`; deferred to `0006`). Today `CheckSeatSaleAvailability`
   *throws*, so on the purchase path a missing session ⇒ `404` and a terminated session ⇒ `500`
   (via `CustomExceptionHandler`). After step 2 retypes it to return a `Result`, `SelSeats` will
   **return** those failures, which the purchase handler propagates and the **unconverted** purchase
   endpoint serializes as a `200`-with-`Result`-body — i.e. session-not-found `404 → 200` and
   terminated `500 → 200` **on the purchase path**. This is not a *new* class of brokenness (the
   purchase endpoint already serializes the `Sell`-conflict `Result` as `200` today — that is
   exactly why `0006` exists); the retype merely routes two more failures into that same pre-existing
   gap until `0006` lands. The PRD frames the slice as "convert only `ReserveTickets`, leaving
   `PurchaseTickets` for `0006`" and lists no purchase-path test, so this side-effect would go
   unpinned. **Options:** (a) accept it as an interim regression on an already-broken path that
   `0006` closes within the same series — recommended, matches the PRD's incremental intent and US19;
   (b) pull the purchase-endpoint `Match` conversion forward into this slice (scope creep — really
   `0006`); (c) keep `SelSeats` throwing by re-raising inside it after the check (reintroduces a
   bridge, contradicts US19). Recommend (a); flag in `roadmap`/`0006` notes so it is not forgotten.
2. **`SeatsReserve()` on a `Deleted` cart.** The PRD specifies only three statuses (`InWork`,
   `SeatsReserved`, `PurchaseCompleted`). The prior `void` code, for a `Deleted` cart, added a
   `ShoppingCartReservedDomainEvent` **without** transitioning (a latent bug). The plan's §5 step 1
   returns a `ConflictError` for any non-`InWork`/non-`SeatsReserved`/non-`PurchaseCompleted` status
   (the safest reading of "event only on a genuine transition"). Confirm this is acceptable, or
   specify the desired `Deleted` behaviour in `requirements.md`. This path is not reachable through
   the `reservations` endpoint for a `Deleted` cart in normal flow, so impact is minimal.
