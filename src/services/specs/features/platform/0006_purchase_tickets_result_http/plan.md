# 0006 · PurchaseTickets Result→HTTP — Implementation plan

## 1. Header

- **Aggregate / Module:** `platform` (a `PurchaseTickets` use-case conversion that spans
  `ShoppingCarts` (the command + handler and the `ShoppingCart.PurchaseComplete` aggregate method),
  the `MovieSessionSeat.Sell` seat transition (one mislabelled case), and the `purchase` endpoint;
  filed under `platform` to keep the ADR-002 step-3 series together with `0001`–`0005`). Plus the
  **ADR-002 adoption close-out** — docs-only edits flipping ADR-002 to Accepted and reconciling
  `agent_docs/error_handling.md` + `CLAUDE.md` rule #9.
- **Slice:** `0006_purchase_tickets_result_http`
- **PRD:** ./prd.md
- **ADR:** `docs/adr/ADR-002-error-handling-model-result-vs-exceptions.md` (step 3, **fourth and
  final** conversion; this slice **flips ADR-002 `Proposed → Accepted`**, dated 2026-06-04).
- **Reference slice:** `../0005_reserve_tickets_result_http/plan.md` — same `platform` module, same
  ADR-002 step-3 shape (replace `return result;` with `Match`-to-HTTP through the **shared**
  `ErrorResults.ToProblem` mapper, convert a `void` aggregate method to `Result`, pin with a focused
  handler unit gate, **no** `WebApplicationFactory`). `0005`'s `ReserveTicketsCommandHandlerTests`
  is the closest handler-gate template (same mocked-collaborator shape, same real-`MovieSessionSeatService`
  construction). `0005`'s `SeatsReserve()` is the template for the `PurchaseComplete()`
  `void → Result` conversion (idempotent on the terminal status, event on a genuine transition only).
- **HTTP path (no new route; existing route, mechanism swap):**
  - `POST /api/shoppingcarts/{ShoppingCartId}/purchase` — `return result;` is replaced by
    `result.Match(() => Results.Ok(), ErrorResults.ToProblem)`. **Behaviour-changing** (this is the
    point of the slice, see §3): every failing outcome currently serialized as `200` moves to its
    correct `404`/`409`; the success body changes from a serialized `Result` object to **empty**;
    success status stays `200`.
- **STABLE files touched:**
  - **`CLAUDE.md`** — amend rule #9 and the project-at-a-glance line from "not yet unified /
    undecided" to "decided — see ADR-002." **Docs-only edit to the always-loaded instructions**,
    not a mechanism change; explicitly mandated by the PRD adoption close-out (user story 33).
  - **`agent_docs/error_handling.md`** — rewrite "two models coexist / undecided" to the decided
    hybrid (user stories 32, 34). Docs-only.
  - `ErrorResults.cs`, `CustomExceptionHandler.cs`, `DomainErrors`, `Error`/`ConflictError`/
    `NotFoundError`, the MediatR pipeline, the validation behaviour, and every base type are **not
    touched**. The conversion reuses the **existing** `ConflictError` / `NotFoundError` kinds via the
    existing `DomainErrors<T>.ConflictException(...)` / `.NotFound(...)` factories — **no new `Error`
    type and no new mapper arm**. If anything beyond §5 proves necessary — a new `Error` type, a
    `CustomExceptionHandler` change, a `400`-arm in the mapper, a base-type change — **stop and ask**;
    that exceeds ADR-002 step 3 for this use-case.
  - The two `*.md` doc edits are the deliberate adoption close-out carried **only** by this final
    conversion (the `ShoppingCarts` write path is complete after it). They are still STABLE-file
    touches and are listed here so the reviewer sees them; they change wording, not behaviour.
- **No EF Core entity is added or altered → no migration.**

## 2. Context summary

This is ADR-002 step 3's **fourth and final** conversion, and the one that completes the
`ShoppingCarts` write path. The `purchase-tickets` use-case advertises the `Result` model
(`PurchaseTicketsCommand : IRequest<Result>`) and the handler **already returns** `Result`s for most
failures (cart-not-found ⇒ `NotFoundError`; the `SelSeats` `Result` short-circuit is in place from
`0005`), but the endpoint still does `return result;` — so every failing `Result` (cart-not-found,
session-not-found, terminated, seat-already-sold, seat-held-by-another-cart) serializes as a `200`
with a `Result` body and never reaches `ErrorResults.ToProblem`. This is the interim regression
`0005` explicitly parked for `0006`. This slice: (a) resolves the endpoint with the shared mapper;
(b) retypes `MovieSessionSeat.Sell`'s one "another shopping cart" case from `InvalidOperation` to
`ConflictError`, defusing the purchase-path `InvalidOperation → 500` trap (the twin of the `Select`
trap `0004` defused); (c) converts `ShoppingCart.PurchaseComplete()` from `void` to `Result`
(fixing the unconditional-event bug as a side-effect, the same way `0005` fixed it for `SeatsReserve`);
(d) makes the handler consume that `Result` and short-circuit **before** persistence; and (e) carries
the ADR-002 adoption close-out (flip to Accepted, reconcile the two docs). The acceptance gate is a
focused unit spec of the **converted handler** in `BookingManagementService.Domain.UnitTests` (no
`WebApplicationFactory`).

## 3. API contract

Mechanism swap on an existing route — no new request/response model.

### Endpoint — `POST /api/shoppingcarts/{ShoppingCartId}/purchase`

- **Request:** `[FromRoute] Guid ShoppingCartId`; **no body**. Mapped to
  `PurchaseTicketsCommand(ShoppingCartId)` — unchanged, `IRequest<Result>`.
- **Resolution:** `result.Match(() => Results.Ok(), ErrorResults.ToProblem)` — replaces
  `return result;`. Success is `Results.Ok()` (**empty** `200` body, previously a serialized
  `Result` object).
- **Status codes (the contract this slice locks in).** "Status before" reflects the post-`0005`
  state, where the handler already returns `Result`s but the endpoint serializes them as `200`:

  | Outcome | Mechanism before | Status before | Mechanism after | Status after |
  |---|---|---|---|---|
  | Purchase succeeds | `return result;` (serialized `Result` body) | 200 + body | `Result.Success()` ⇒ `Results.Ok()` (empty) | **200** |
  | Shopping cart not found (handler-local) | returns `NotFoundError`; `return result;` | **200** | `Result` `NotFoundError` ⇒ mapper | **404** |
  | Movie session not found (shared helper via `SelSeats`) | returns `NotFoundError`; `return result;` | **200** | `Result` `NotFoundError` ⇒ mapper | **404** |
  | Sales terminated (shared helper via `SelSeats`) | returns `ConflictError`; `return result;` | **200** | `Result` `ConflictError` ⇒ mapper | **409** |
  | Seat already sold (`Sell`) | returns `ConflictError`; `return result;` | **200** | `Result` `ConflictError` ⇒ mapper | **409** |
  | Seat held by another cart (`Sell`) | returns **`InvalidOperation`**; `return result;` | **200** | **retype ⇒ `ConflictError`** ⇒ mapper | **409** |
  | Cart not in a completable status (`PurchaseComplete`, e.g. `InWork`) | `void`; no throw, fires event, persists (bug) | 200 (buggy) | `Result` `ConflictError` ⇒ mapper | **409** |
  | Cart already purchased (`PurchaseComplete`) | `throw ConflictException` | 409 | idempotent `Result.Success()` (no event); but `Sell`'s `Sold` guard fires first ⇒ `409` at the endpoint (see §8) | **409** (endpoint) |
  | Movie session seat not found (`GetMovieSessionSeat`) | `throw ContentNotFoundException` | 404 | exception (**unchanged**) | 404 |
  | `ClientId` empty at completion (`PurchaseComplete`) | `Ensure` throw | 500 | `Ensure` throw (**unchanged**) | 500 |
  | Repository / Redis lifecycle fault | exception | 500 | exception (**unchanged**) | 500 |

- **`.Produces` corrected:** declare `200` / `404` / `409`; drop the stale `.Produces<bool>(201)` /
  `.Produces(204)` (user stories 24, 25). The `500` (genuinely-unexpected) path remains
  exception-driven via `CustomExceptionHandler` and is not declared here (consistent with sibling
  endpoints — `assignclient`, `seats/select`, `reservations`).

### Shared mapper — `ErrorResults.ToProblem(Error)` (existing, reused unchanged)

`API/Endpoints/Common/ErrorResults.cs` from slice `0003`. `NotFoundError ⇒ 404`,
`ConflictError ⇒ 409` (title-only `"Conflict"`, no `Detail`), any other `Error ⇒ 500`. Already
covered by `ErrorResultsOutsideInTests` in `0003`; **not re-tested** here and **not edited** (user
story 11/13). The `Sell` retype from `InvalidOperation` to `ConflictError` is what makes the
"another cart" purchase path map cleanly to `409` instead of falling into the `_ => 500` arm — **the
mapper itself stays unchanged**; only the `Error` kind the domain returns changes.

## 4. File structure

```
BookingManagement/
├── BookingManagementService.Domain/
│   ├── ShoppingCarts/
│   │   └── ShoppingCart.cs                                       # EDIT: PurchaseComplete() void → Result; stop calling EnsurePurchaseIsNotCompleted; inline a Result guard; event only on genuine SeatsReserved→PurchaseCompleted transition; idempotent Success on already-PurchaseCompleted (no event); ConflictError on any other status; Ensure.NotEmpty(ClientId) stays a throw
│   └── Seats/
│       └── MovieSessionSeat.cs                                   # EDIT: Sell() "another shopping cart" case InvalidOperation → ConflictError (one line); already-Sold case unchanged (already ConflictError)
├── BookingManagementService.Application/
│   └── ShoppingCarts/Command/PurchaseSeats/
│       └── PurchaseTicketsCommandHandler.cs                      # EDIT: consume PurchaseComplete() Result; short-circuit on IsFailure BEFORE SaveAsync / cart-lifecycle DeleteAsync / per-seat DeleteAsync (atomicity). Cart-missing NotFoundError + SelSeats short-circuit already present from prior work.
└── BookingManagementService.API/
    └── Endpoints/
        └── ShoppingCartEndpointApplicationBuilderExtensions.cs   # EDIT: purchase delegate return result; → Match(() => Results.Ok(), ErrorResults.ToProblem); .Produces 200/404/409 (drop 201/204)

docs/adr/
└── ADR-002-error-handling-model-result-vs-exceptions.md         # EDIT (docs): status Proposed → Accepted, dated 2026-06-04

agent_docs/
└── error_handling.md                                            # EDIT (docs, STABLE): "two models coexist / undecided" → the decided hybrid; name the intentional exception tails (reads/queries, GetMovieSessionSeat)

CLAUDE.md                                                        # EDIT (docs, STABLE): rule #9 + project-at-a-glance line "not yet unified / undecided" → "decided — see ADR-002"

BookingManagement/tests/
└── BookingManagementService.Domain.UnitTests/                    # EXISTING (RootNamespace CinemaTicketBooking.Application.UnitTests; references Application; NSubstitute + xUnit + FluentAssertions)
    ├── ShoppingCarts/
    │   ├── PurchaseTicketsCommandHandlerTests.cs                 # NEW: the RED acceptance gate (written by /slice-test-red, step 5) — next to ReserveTicketsCommandHandlerTests.cs
    │   └── ShoppingCartSpecification.cs                          # EDIT (after green): add PurchaseComplete facts (SeatsReserved ⇒ PurchaseCompleted + event; already PurchaseCompleted ⇒ Success, no event; InWork ⇒ ConflictError, no event)
    └── Seats/
        └── MovieSessionSeatSpecification.cs                      # NEW (after green): Sell "another cart" ⇒ ConflictError (not InvalidOperation); already-Sold ⇒ ConflictError (regression). Folder exists; no MovieSessionSeat spec file yet.
```

> Project-naming note (carried from `0002`–`0005`): `BookingManagementService.Domain.UnitTests` is
> named "Domain" but its `RootNamespace` is `CinemaTicketBooking.Application.UnitTests` and it
> references the **Application** project with NSubstitute available — the correct home for the
> handler gate. The `PurchaseComplete` and `Sell` domain facts also live here (the existing
> `ShoppingCartSpecification.cs` already exercises `ShoppingCart`; the `Seats/` folder already exists
> but has no `MovieSessionSeat` spec yet — add one).

No EF Core entity is added or altered → **no migration**.

## 5. Implementation steps

1. **Domain — `MovieSessionSeat.Sell()`: retype the "another shopping cart" case.** In
   `Domain/Seats/MovieSessionSeat.cs`, the `Sell` method (currently lines 122–146), change the one
   mislabelled branch (currently lines 126–130) from `InvalidOperation` to `ConflictException`
   (user stories 12/13):
   ```csharp
   if (ShoppingCartId != shoppingCartId)
   {
       return DomainErrors<MovieSessionSeat>.ConflictException(
           "The place is already being processed by another shopping cart");
   }
   ```
   This mirrors the sibling transitions `Select`/`Reserve`, which already return `ConflictError` for
   the identical "another shopping cart" condition. The already-`Sold` branch (lines 132–135) already
   returns `ConflictError` — **leave it unchanged**. `DomainErrors` is already imported in this file.
   No new `Error` type; `InvalidOperation` keeps meaning "genuinely unexpected ⇒ `500`" everywhere
   else (user story 13).

2. **Domain — `ShoppingCart.PurchaseComplete()`: `void → Result`.** In
   `Domain/ShoppingCarts/ShoppingCart.cs`, convert the method (currently lines 206–216), following
   the `SeatsReserve()` template settled in `0005` (lines 187–204). Stop calling the shared
   `EnsurePurchaseIsNotCompleted()` (it stays for `CalculateCartAmount` and others — **do not modify
   it**) and inline a `Result`-returning guard. Keep `Ensure.NotEmpty(ClientId)` as a **throw** (an
   invariant violation, `500`-class — user story 18). Append the event **only** on a genuine
   `SeatsReserved → PurchaseCompleted` transition (this fixes the unconditional-event bug; user
   stories 14/15/16/17/19):
   ```csharp
   public Result PurchaseComplete()
   {
       Ensure.NotEmpty(ClientId, "The ClientId is required.", nameof(ClientId));   // stays a throw (invariant)

       if (Status == ShoppingCartStatus.PurchaseCompleted)
           return Result.Success();                  // idempotent — no duplicate event (409 → 200 at the method level)

       if (Status != ShoppingCartStatus.SeatsReserved)
           return DomainErrors<ShoppingCart>.ConflictException(
               $"The shopping cart {Id} cannot be purchased from status {Status}.");

       Status = ShoppingCartStatus.PurchaseCompleted;     // genuine transition
       _domainEvents.Add(new ShoppingCartPurchaseDomainEvent(Id));

       return Result.Success();
   }
   ```
   `DomainErrors`, `ShoppingCartPurchaseDomainEvent`, and `Ensure` are already imported in this file.
   The third guard (non-`SeatsReserved`, non-`PurchaseCompleted` — `InWork`, `Deleted`) returns a
   `ConflictError`; see §8 (this is the settled "require a prior reservation" reading and fixes the
   latent behaviour where the old `void` code fired the purchase event and persisted from `InWork`
   without transitioning). **Ordering note:** `Ensure.NotEmpty(ClientId)` runs first so a
   client-less cart is a `500` invariant violation regardless of status — this is intentional and
   matches the PRD (user story 18).

3. **Application — `PurchaseTicketsCommandHandler`: consume `PurchaseComplete()`'s `Result`,
   short-circuit before persistence.** In
   `Application/ShoppingCarts/Command/PurchaseSeats/PurchaseTicketsCommandHandler.cs`, change the tail
   of `Handle` (currently lines 59–68). The cart-not-found `NotFoundError` (lines 43–46) and the
   `SelSeats` `Result` short-circuit (lines 53–56) are **already present** — this slice only adds the
   `PurchaseComplete` `Result` consumption and the short-circuit before the side-effects (user stories
   20/21):
   ```csharp
   var purchaseResult = cart.PurchaseComplete();      // now returns Result
   if (purchaseResult.IsFailure)
       return purchaseResult;                         // BEFORE SaveAsync / lifecycle deletes (atomicity)

   await _activeShoppingCartRepository.SaveAsync(cart);          // reached only on full success
   await _shoppingCartLifecycleManager.DeleteAsync(cart.Id);

   foreach (var seat in cart.Seats)
       await _shoppingCartSeatLifecycleManager.DeleteAsync(cart.MovieSessionId, seat.SeatRow, seat.SeatNumber);

   return Result.Success();
   ```
   The short-circuit puts the failing `Result` **before** `SaveAsync`, the cart-lifecycle
   `DeleteAsync`, and the per-seat `DeleteAsync` — the atomicity invariant the thrown path provided
   implicitly (user stories 21/26). Genuinely unexpected faults (repository / Redis / the
   `ClientId`-empty invariant) still propagate as exceptions (user story 23). The pre-existing
   seat-then-cart persistence ordering inside `SelSeats` is **not** changed (out of scope). Verify
   no imports become unused (`-warnaserror`); the handler already imports `Domain.Error`.

4. **API — endpoint: `return result;` → shared mapper.** In
   `ShoppingCartEndpointApplicationBuilderExtensions.cs`, the `purchase` delegate (currently lines
   176–188) becomes:
   ```csharp
   var query = new PurchaseTicketsCommand(ShoppingCartId: shoppingCartId);
   var result = await sender.Send(query, cancellationToken);

   return result.Match(
       () => Results.Ok(),
       ErrorResults.ToProblem);
   ```
   Replace `.Produces<bool>(201, "application/json").Produces(204)` with
   `.Produces(200).Produces(404).Produces(409)` (user stories 24/25). `.WithName("PurchaseSeats")`,
   `.WithTags(Tag)` unchanged. `ErrorResults` is already imported in this file (the `assignclient`,
   `seats/select`, and `reservations` delegates use it). The delegate's return type becomes `IResult`
   (it currently returns the `Result` object) — confirm the lambda compiles under `-warnaserror`.

5. **Docs — ADR-002 adoption close-out (carried only by this final conversion).** Three docs-only
   edits, made because the `ShoppingCarts` write path is complete after this conversion (user stories
   31/32/33/34):
   - **`docs/adr/ADR-002-error-handling-model-result-vs-exceptions.md`:** flip status
     `Proposed → Accepted`, dated **2026-06-04**. Read the ADR first to match its existing header
     format; change only the status/date line(s), do not rewrite the body.
   - **`agent_docs/error_handling.md`:** rewrite the "Two models coexist (on purpose, not yet
     unified)" section to the **decided hybrid**: expected business outcome ⇒ `Result`; in-aggregate
     transition that raises a domain event ⇒ `Result`; structural validation ⇒
     `ValidationBehaviour`/`ValidationException`; unexpected/infrastructure ⇒ exception ⇒
     `CustomExceptionHandler`; the endpoint `Result → exception` bridge is gone. Name the
     deliberately un-converted tails (read/query `ContentNotFoundException`, the shared
     `GetMovieSessionSeat` seat-not-found helper, the `ClientId`-empty invariant throw) as
     **intentional** exception usage so they are not mistaken for debt.
   - **`CLAUDE.md`:** amend **rule #9** (and the project-at-a-glance "the error model is not yet
     unified" line) from "not yet unified / undecided" to "decided — see ADR-002," stating the hybrid
     split. **Surgical wording change only** — do not touch any other rule or the locked-stack table.
   These are STABLE-file touches; they change wording, not mechanism. If the rewrite tempts a
   behavioural or structural change, **stop** — this step is documentation reconciliation only.

6. **Verify (pre-test).** From `src/services` (use the x86 SDK at
   `C:\Program Files (x86)\dotnet\dotnet.exe`; run via the PowerShell tool — see MEMORY
   `dotnet-sdk-path`):
   ```
   dotnet format CinemaBookingManagement.sln
   dotnet build  CinemaBookingManagement.sln -warnaserror
   ```
   Resolve warnings. The `Sell` retype has no caller ripple (it is an internal seat method returning
   `Result` either way). The `PurchaseComplete` `void → Result` ripples only to the single caller
   (the handler, step 3 — confirmed no other callers). The accepted AutoMapper **NU1903** NuGet-audit
   advisory trips `-warnaserror` at restore time (known/accepted — MEMORY `dotnet10-migration` and
   `warnaserror-baseline-debt`); handle the NuGet audit so real build warnings are what is validated.
   Note (MEMORY `warnaserror-baseline-debt`): `dotnet format` is known to break
   `ReserveSeatsCommandValidatorSpecification.cs` — scope the format to the touched files or
   `git checkout` that file if it is reformatted.

## 6. Tests planned

The externally observable behaviour is the `Result`/`Error` each outcome produces (hence the status
the shared mapper yields), the `PurchaseComplete` transition's outcome and its event, the `Sell`
"another cart" `Error` kind, and the atomicity invariant (no persistence on failure). There is **no**
`WebApplicationFactory<Program>` harness; the change is pinned by focused unit tests of the changed
units, consistent with `0001`–`0005`.

- **Handler unit test — RED acceptance gate — NEW
  `BookingManagement/tests/BookingManagementService.Domain.UnitTests/ShoppingCarts/PurchaseTicketsCommandHandlerTests.cs`.**
  xUnit + FluentAssertions + NSubstitute (same conventions as `ReserveTicketsCommandHandlerTests.cs`).
  NSubstitute mocks `IActiveShoppingCartRepository`, `IMovieSessionSeatRepository`,
  `IShoppingCartSeatLifecycleManager`, and `IShoppingCartLifecycleManager`. **`MovieSessionSeatService`
  is a `sealed` concrete class** — construct it **real** over mocked `IMovieSessionSeatRepository` +
  `IMovieSessionsRepository` (drive a non-terminated/terminated `MovieSession` and the
  `MovieSessionSeat`s into the required state via factory/`Select`/`Reserve`). Facts (RED until the
  endpoint/handler/domain genuinely produce these `Result`s end to end):
  1. cart missing ⇒ result is `NotFoundError`; `SaveAsync` **not** received.
  2. movie session missing ⇒ `NotFoundError`; `SaveAsync` / cart-lifecycle `DeleteAsync` **not** received.
  3. sales terminated ⇒ `ConflictError`; `SaveAsync` / lifecycle **not** received.
  4. a seat held by another cart ⇒ `ConflictError` (not `InvalidOperation`); `SaveAsync` / lifecycle
     **not** received (atomicity + the `Sell` retype).
  5. success (cart `SeatsReserved`, seats sellable by this cart) ⇒ `Result.Success()`; cart **is**
     saved, cart-lifecycle removed, and the per-seat `IShoppingCartSeatLifecycleManager.DeleteAsync`
     entries deleted.
  6. **atomicity** — on any failure (cases 1–4) the cart save and **both** lifecycle side-effects
     are **not** invoked.
  RED today because the handler still calls the `void` `PurchaseComplete()` and the endpoint
  serializes failures as `200`; case 4 is RED until the `Sell` retype lands.

- **Domain unit test (after green)** — EDIT
  `BookingManagement/tests/BookingManagementService.Domain.UnitTests/ShoppingCarts/ShoppingCartSpecification.cs`
  (AAA / `*Specification` convention). Facts for `PurchaseComplete`: `SeatsReserved` ⇒
  `Status == PurchaseCompleted` **and** `ShoppingCartPurchaseDomainEvent` raised; already
  `PurchaseCompleted` ⇒ `Result.Success()` and **no** event; `InWork` ⇒ `ConflictError` and no event.
  Pins the `void → Result`, idempotency, and event-on-success-only changes (user story 27).

- **Domain unit test (after green)** — NEW
  `BookingManagement/tests/BookingManagementService.Domain.UnitTests/Seats/MovieSessionSeatSpecification.cs`
  (AAA / `*Specification` convention; the `Seats/` folder exists, no `MovieSessionSeat` spec yet).
  Facts for `Sell`: the "another shopping cart" case (a seat whose `ShoppingCartId` differs from the
  caller's) ⇒ `ConflictError` (**not** `InvalidOperation`); the already-`Sold` case ⇒ `ConflictError`
  (unchanged regression). Pins the retype so it cannot silently regress to `500` (user story 28).

- **Regression** — re-run slice `0004`'s `ShoppingCarts/SelectSeatCommandHandlerTests.cs` **and**
  slice `0005`'s `ShoppingCarts/ReserveTicketsCommandHandlerTests.cs` **unchanged** as regression
  gates, to prove the `Sell` / `PurchaseComplete` changes did not disturb the already-green select
  and reserve paths (user story 29).

**Opt-outs (explicit, per `agent_docs/testing.md` and the PRD Testing Decisions):**
- **`WebApplicationFactory` end-to-end / endpoint integration test — skipped:** no HTTP harness
  exists; the endpoint's `Match` wiring is covered by compilation and the shared mapper by `0003`'s
  `ErrorResultsOutsideInTests`.
- **Repository / adapter unit test — skipped:** no repository/adapter logic changes (no new
  business-meaningful infrastructure-exception translation on this path).
- **Real-concurrency (two carts racing one seat) test — deferred** to a separate
  Infrastructure-level integration test, not this slice's gate (PRD Out of Scope).
- **Endpoint-level re-purchase test — not added:** the idempotent `409 → 200` is a domain-method
  contract pinned by the `PurchaseComplete` domain test; at the endpoint a real re-`POST /purchase`
  surfaces as `409` via `Sell`'s `Sold` guard (see §8) — no contradictory endpoint `200` is asserted.

**Full-suite gate:** the entire `dotnet test` run, **including
`BookingManagementService.Domain.ArchitectureTests`**, must stay green (user stories 29/30).

## 7. Out of scope for this slice

- Converting any read/query handler (or the shared `GetMovieSessionSeat`) that throws
  `ContentNotFoundException` to `Result` — intentional exception usage, documented as such in
  `agent_docs/error_handling.md` (user stories 22/34).
- Modifying the shared `EnsurePurchaseIsNotCompleted` guard (still used by `CalculateCartAmount` and
  others) (user story 19).
- Adopting `Result<T>` (the generic) on this path — `PurchaseTickets` uses the non-generic `Result`;
  success carries no value.
- Adding a `400`-class arm to `ErrorResults.ToProblem` or otherwise changing the shared mapper /
  introducing a new `Error` kind (ADR-level).
- The bare `throw` in the endpoints' `GetClientId` / `CreateShoppingCart` helpers (slice `0007`).
- Fixing the pre-existing seat-then-cart transactional ordering gap inside `SelSeats`.
- Changing `CustomExceptionHandler`, the MediatR pipeline, the validation behaviour, or any base type.
- Standing up a `WebApplicationFactory` HTTP integration harness, or a real-concurrency seat-race
  integration test.
- The Flutter client follow-up to the `0002` `204 → 404` contract change.
- Publishing this PRD/spec to a remote issue tracker (`gh` not installed; stored locally as for
  `0001`–`0005`).

## 8. Open questions

The PRD raised one open question for this step and recommended a resolution; recorded here so the
downstream spec steps follow the settled reading and do not re-open it. Confirm in `requirements.md`.

1. **Purchase directly from `InWork` (PRD Further Notes).** The status matrix maps a
   non-`SeatsReserved`/non-`PurchaseCompleted` status (notably `InWork`, a cart whose seats were
   selected but never reserved) to `ConflictError`. This assumes the product flow is
   **select → reserve → purchase** and that purchasing without reserving first is illegal.
   **PRD recommendation (followed by §5 step 2): require a prior reservation — `ConflictError` on
   `InWork`** — it matches the existence of a distinct reserve step and fixes the latent behaviour
   (the old `void` code fired the purchase event and persisted from `InWork` without transitioning).
   If a "select → purchase directly" flow were intended instead, §5 step 2 would have to allow the
   `InWork → PurchaseCompleted` transition. **Resolve in `requirements.md` before red.**

2. **Idempotent already-purchased vs the seat-level `Sold` guard (PRD Further Notes — recorded, not
   re-opened).** `PurchaseComplete()` on an already-`PurchaseCompleted` cart is an idempotent
   `Result.Success()` with no event (a domain-method `409 → 200`, mirroring `0005`). But the handler
   reaches `PurchaseComplete` only **after** `SelSeats` succeeds, and on a fully-completed cart the
   seats are already `Sold`, so `Sell`'s `Sold` guard returns a `ConflictError` (`409`) **first** —
   meaning a real re-`POST /purchase` on a completed cart surfaces as `409` at the endpoint. The
   idempotent `Success` is therefore the domain-method contract (kept consistent with `SeatsReserve`,
   covering the inconsistent-state case where seats are not `Sold` while the cart is
   `PurchaseCompleted`); it is pinned by the `PurchaseComplete` **domain** test, not by an endpoint
   test. The `tests.md`/`requirements.md` steps must **not** assert a contradictory endpoint-level
   `200` for re-purchase.
