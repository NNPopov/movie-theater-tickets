# 0006 · PurchaseTickets Result→HTTP — Validation

## Inputs

- PRD: ./prd.md
- Plan: ./plan.md
- Requirements: ./requirements.md

> Nature of this slice: the **fourth and final** ADR-002 step-3 conversion, at one endpoint
> (`POST /api/shoppingcarts/{ShoppingCartId}/purchase`). It replaces `return result;` with
> `result.Match(() => Results.Ok(), ErrorResults.ToProblem)` (the **existing** shared mapper from
> slice `0003`); retypes `MovieSessionSeat.Sell`'s one "another shopping cart" case from
> `InvalidOperation` to `ConflictError` (defusing the purchase-path `InvalidOperation → 500` trap);
> converts `ShoppingCart.PurchaseComplete()` `void → Result` (idempotent on already-completed, event
> on a genuine transition only, `ConflictError` otherwise); and makes the handler consume that
> `Result` and short-circuit before persistence. **Every failing outcome that `0005` parked at an
> interim `200` is corrected on purpose:** cart-not-found `200 → 404`, session-not-found `200 → 404`,
> terminated `200 → 409`, seat-already-sold `200 → 409`, seat-held-by-another-cart `200 → 409`; the
> success body changes from a serialized `Result` object to **empty** (status stays `200`). Being the
> final write-path conversion, it also flips **ADR-002 to Accepted** and reconciles
> `agent_docs/error_handling.md` + `CLAUDE.md` rule #9. The acceptance gate is a **focused unit spec
> of the converted handler** (`PurchaseTicketsCommandHandlerTests`) in
> `BookingManagementService.Domain.UnitTests` — there is **no** `WebApplicationFactory` harness in
> this repo (see Prerequisites and the test section).

## Prerequisites

- Service running locally:
  ```
  dotnet run --project BookingManagement/BookingManagementService.API
  ```
  (default port: check `BookingManagement/BookingManagementService.API/Properties/launchSettings.json`;
  examples below use `http://localhost:<port>`).
- Test database provisioned and migrations applied (no new migration in this slice):
  ```
  dotnet ef database update \
    -p BookingManagement/BookingManagementService.Infrastructure \
    -s BookingManagement/BookingManagementService.API
  ```
- Seed data: at least one movie session (note its `showtimeId`); a shopping cart created via
  `POST /api/shoppingcarts` with a client assigned (`PUT .../assignclient`), seats selected into it,
  and the seats **reserved** (`POST .../reservations`) so the cart is `SeatsReserved` and
  `PurchaseComplete` has a legal transition; for the terminated scenario, a movie session whose
  `SalesTerminated` is `true`; for the already-sold / another-cart scenarios, a held seat already
  `Sold`, or owned by a different `ShoppingCartId`.
- Redis and the distributed-lock backend running (the purchase handler writes the cart/seat lifecycle
  to Redis via `IShoppingCartLifecycleManager` / `IShoppingCartSeatLifecycleManager`).

## Manual scenarios

### S1 — Happy path: purchase the seats reserved in a cart

**Steps:**

1. ```
   curl -s -i -X POST http://localhost:<port>/api/shoppingcarts/<cartId>/purchase
   ```
2. Read the cart back:
   ```
   curl -s http://localhost:<port>/api/shoppingcarts/<cartId>
   ```

**Expected:**

- Step 1: HTTP `200 OK` with an **empty body** (previously the response serialized a `Result`
  object — confirm the body is now empty).
- Step 2: the cart's status is `PurchaseCompleted` and its seats show status `Sold`.

**Covers:** F1, F2, F10, F14, F23.

### S2 — Not found: purchase against a non-existent cart (the corrected `200 → 404`)

**Steps:**

1. ```
   curl -s -i -X POST http://localhost:<port>/api/shoppingcarts/00000000-0000-0000-0000-000000000000/purchase
   ```

**Expected:**

- HTTP `404 Not Found` (**was an interim `200` after `0005`**).
- `ProblemDetails` body: `status: 404`,
  `type: "https://tools.ietf.org/html/rfc7231#section-6.5.4"`,
  `title: "The specified resource was not found."`, `detail` present (the handler's `NotFoundError`
  description). Shape identical to any other `404` in the service — proving the cart-not-found path
  now flows through the shared mapper rather than serializing a `Result` body.

**Covers:** F6, F24.

### S3 — Not found: movie session missing (shared helper, corrected `200 → 404`)

**Steps:**

1. Arrange a cart whose `MovieSessionId` references a session that does not exist (or delete the
   session), then purchase:
   ```
   curl -s -i -X POST http://localhost:<port>/api/shoppingcarts/<cartId>/purchase
   ```

**Expected:**

- HTTP `404 Not Found` with the **same** `ProblemDetails` shape as S2. This `404` originates in the
  shared `CheckSeatSaleAvailability` returning `NotFoundError`, propagated through `SelSeats` and the
  handler as a `Result` (now routed through `Match`, no longer serialized as `200`).

**Covers:** F7, F24.

### S4 — Conflict: sales terminated (the corrected `200 → 409`)

**Steps:**

1. Point the cart at a movie session whose sales are terminated (`SalesTerminated == true`), then
   purchase:
   ```
   curl -s -i -X POST http://localhost:<port>/api/shoppingcarts/<cartId>/purchase
   ```

**Expected:**

- HTTP `409 Conflict` (**was an interim `200`**).
- `ProblemDetails` body: `status: 409`,
  `type: "https://tools.ietf.org/html/rfc7231#section-6.5.8"`, `title: "Conflict"`, **no** `detail`.
- The cart is **not** persisted as `PurchaseCompleted` (atomicity).

**Covers:** F7, F9, F24.

### S5 — Conflict: a seat already sold (the corrected `200 → 409`)

**Steps:**

1. Arrange one of the cart's seats so it is already `Sold` (by this or another cart), then purchase:
   ```
   curl -s -i -X POST http://localhost:<port>/api/shoppingcarts/<cartId>/purchase
   ```

**Expected:**

- HTTP `409 Conflict` with the **same** `ProblemDetails` shape as S4. This `409` originates from
  `MovieSessionSeat.Sell`'s already-`Sold` guard returning a `ConflictError`, propagated through
  `SelSeats` and the handler as a `Result`.
- The cart is **not** persisted as `PurchaseCompleted` (atomicity).

**Covers:** F7, F20, F24.

### S6 — Conflict: a seat held by another cart (the corrected `200/500 → 409`, the `Sell` retype)

**Steps:**

1. Arrange one of the cart's seats so its `ShoppingCartId` belongs to a **different** cart, then
   purchase:
   ```
   curl -s -i -X POST http://localhost:<port>/api/shoppingcarts/<cartId>/purchase
   ```

**Expected:**

- HTTP `409 Conflict` with the **same** `ProblemDetails` shape as S4/S5 — **not** `500`. Before this
  slice the endpoint serialized this `InvalidOperation` `Result` as `200`, and under a naive `Match`
  it would have mapped to `500`; the `Sell` retype to `ConflictError` is what makes it `409`, exactly
  like the select/reserve paths report the identical condition.
- The cart is **not** persisted as `PurchaseCompleted` (atomicity).

**Covers:** F7, F19, F24.

### S7 — Conflict: cart not reserved (purchase directly from `InWork`, the corrected `200-buggy → 409`)

**Steps:**

1. Arrange a cart in status `InWork` (seats selected but **never reserved**), then purchase:
   ```
   curl -s -i -X POST http://localhost:<port>/api/shoppingcarts/<cartId>/purchase
   ```

**Expected:**

- HTTP `409 Conflict` with the same `ProblemDetails` shape as S4–S6, produced by
  `PurchaseComplete()` returning a `ConflictError` for a non-`SeatsReserved` status.
- The cart is **not** persisted as `PurchaseCompleted`, and **no** `ShoppingCartPurchaseDomainEvent`
  is raised — fixing the prior `void` behaviour, which fired the event and persisted from `InWork`
  without transitioning.

**Covers:** F8, F9, F17, F18, F24.

### S8 — Re-purchase a completed cart surfaces as `409` (idempotency nuance)

**Steps:**

1. Purchase `<cartId>` successfully (S1).
2. Purchase the **same** cart again:
   ```
   curl -s -i -X POST http://localhost:<port>/api/shoppingcarts/<cartId>/purchase
   ```

**Expected:**

- Step 1: HTTP `200 OK`, empty body.
- Step 2: HTTP `409 Conflict`. The seats are already `Sold`, so `MovieSessionSeat.Sell`'s `Sold`
  guard returns a `ConflictError` **before** `PurchaseComplete()` is reached — the endpoint surfaces
  `409`. (The idempotent `Result.Success()` of `PurchaseComplete()` on an already-`PurchaseCompleted`
  cart is a domain-method contract verified by a domain test, **not** an endpoint `200` — do not
  expect a `200` here.)

**Covers:** F16, F20, F26.

### S9 — Body-shape parity check (regression for the reused mapper)

**Steps:**

1. Capture the `404` body from S2/S3 and compare it field-by-field to any exception-driven not-found
   body, e.g. `GET /api/shoppingcarts/00000000-0000-0000-0000-000000000000`.
2. Capture the `409` bodies from S4/S5/S6/S7 and compare them to each other.

**Expected:**

- The `404` bodies are identical in shape (`status`/`type`/`title`/`detail`).
- All four `409` bodies (terminated, already-sold, another-cart, not-reserved) are identical in shape
  (`status`/`type`/`title`, no `detail`) — proving every conflict on this path is produced by the
  **same** mechanism and `ProblemDetails` shape.

**Covers:** F3, F8, F24.

## Code review checklist

Each line is a yes/no question. Reject the PR until all are yes.

### Architecture

- [ ] The converted use-case stays in `Application/ShoppingCarts/Command/PurchaseSeats/`; the command
      remains a `record` `PurchaseTicketsCommand : IRequest<Result>` and the handler a MediatR
      `IRequestHandler<PurchaseTicketsCommand, Result>`. (N1)
- [ ] The shared mapper `API/Endpoints/Common/ErrorResults.cs` is **reused unchanged**; no new
      mapping module is created. (F3)
- [ ] No `IResult` / `HttpContext` / ASP.NET type appears in `Domain` or `Application`; the
      `Error → IResult` mapping exists only in the `API` layer (Dependency Rule). (N4)
- [ ] The endpoint delegate contains no business logic: it binds the route value, builds the command,
      `sender.Send`, and shapes the result via `result.Match(() => Results.Ok(), ErrorResults.ToProblem)`. (N6)
- [ ] No new aggregate, domain event, or repository interface was introduced; `ShoppingCart`,
      `MovieSessionSeat`, `MovieSessionSeatService`, and `ShoppingCartPurchaseDomainEvent` are
      pre-existing.

### Error handling

- [ ] The `purchase` endpoint's `return result;` is **deleted**; success is `Results.Ok()` (empty
      `200`) and the failure branch is `ErrorResults.ToProblem`. (F1, F2)
- [ ] The handler returns `NotFoundError` for the missing cart (already present from prior work) and
      propagates the `SelSeats` failing `Result` unchanged (already present). (F6, F7)
- [ ] The handler consumes `cart.PurchaseComplete()`'s `Result` and short-circuits on `IsFailure`; the
      previous unconditional `void` call `cart.PurchaseComplete();` is **removed**. (F8)
- [ ] The handler short-circuits and returns any failing `Result` **before** `SaveAsync`, the
      cart-lifecycle `DeleteAsync(cart.Id)`, and the per-seat `DeleteAsync` calls — no persistence or
      lifecycle side-effect on failure. (F9)
- [ ] On success the handler returns `Result.Success()` after `SaveAsync`, the cart-lifecycle
      `DeleteAsync`, and per-seat deletes. (F10)
- [ ] Repository / Redis lifecycle faults and the `ClientId`-empty invariant still propagate as
      exceptions (not converted to `Result`). (F11)
- [ ] `ShoppingCart.PurchaseComplete()` is retyped `void → Result`: `Ensure.NotEmpty(ClientId)` stays
      a **throw** evaluated first (F13); already `PurchaseCompleted` ⇒ `Result.Success()`, **no** event
      (F16); `SeatsReserved` ⇒ `PurchaseCompleted` + exactly one `ShoppingCartPurchaseDomainEvent` +
      success (F14); any other status (`InWork`, `Deleted`) ⇒ `ConflictError`, no event (F17); the
      event is appended **only** on the genuine `SeatsReserved → PurchaseCompleted` transition (F18).
- [ ] `PurchaseComplete()` **no longer calls** `EnsurePurchaseIsNotCompleted()`, and that shared guard
      is otherwise **unchanged** (still used by `CalculateCartAmount` / others). (F15)
- [ ] `MovieSessionSeat.Sell`'s "another shopping cart" case (`ShoppingCartId != shoppingCartId`)
      returns `DomainErrors<MovieSessionSeat>.ConflictException(...)` (was `InvalidOperation`); the
      already-`Sold` case is **unchanged** and returns a `ConflictError`. (F19, F20)
- [ ] No new mapper arm was added; `InvalidOperation` still maps to `500`. Only the `Error` kind
      returned by the one `Sell` case changed. (F21)
- [ ] The shared `GetMovieSessionSeat` helper is **unchanged** (seat-not-found stays a thrown
      `ContentNotFoundException`). (F22)
- [ ] No new cross-cutting `*Exception` or `Error` type was introduced; the conversion reuses the
      existing `DomainErrors<T>.ConflictException` / `.NotFound` factories. (N3)
- [ ] No handler sets an HTTP status code or references `HttpContext`. (N5)

### Stable infrastructure

- [ ] `CustomExceptionHandler.cs` and `ErrorResults.cs` are **unchanged** (this slice only re-routes
      outcomes through the existing policy objects). (N7)
- [ ] No base type (`AggregateRoot`, `Entity`, `Result`, `Error`), MediatR pipeline behaviour,
      `IEndpoints`/`EndpointExtensions` mechanism, or `Program.cs` was changed.
- [ ] No DI registration line was needed; if one was added, flag and justify it.

### ADR-002 adoption close-out (docs only)

- [ ] `docs/adr/ADR-002-error-handling-model-result-vs-exceptions.md` status is **`Accepted`**, dated
      `2026-06-04`; only the status/date changed, the ADR body is not rewritten. (F27)
- [ ] `agent_docs/error_handling.md` is rewritten from "two models coexist / undecided" to the
      **decided hybrid**, and it names the intentional exception tails (read/query
      `ContentNotFoundException`, `GetMovieSessionSeat`, the `ClientId`-empty invariant). (F28)
- [ ] `CLAUDE.md` rule #9 and the project-at-a-glance "the error model is not yet unified" line are
      amended to "decided — see ADR-002"; **no other rule** and **not the locked-stack table** were
      touched. (F29)
- [ ] These STABLE doc edits change **wording only** — no mechanism, base type, or pipeline. (N12)

### Scope / completeness

- [ ] `PurchaseTickets` is the only converted use-case; `MovieSessionSeat.Sell` is touched for the one
      mislabelled case only; `GetMovieSessionSeat` / `EnsurePurchaseIsNotCompleted` are unchanged. (N13)
- [ ] The interim purchase-path regression `0005` parked (session-not-found / terminated serializing
      as `200`) is **closed** by this slice; the whole `ShoppingCarts` write path now reports expected
      outcomes through `Result → mapper`. (F25)

### OpenAPI / metadata

- [ ] The `purchase` endpoint declares `.Produces(200)`, `.Produces(404)`, `.Produces(409)` and no
      longer declares `.Produces<bool>(201)` / `.Produces(204)`. (F4)
- [ ] `.WithName("PurchaseSeats")`, `.WithTags(Tag)` are retained, and the endpoint still maps the
      route `ShoppingCartId` to `PurchaseTicketsCommand`. (F5)

### Tests

- [ ] The `PurchaseTicketsCommandHandler` outside-in gate test exists in
      `BookingManagement/tests/BookingManagementService.Domain.UnitTests/ShoppingCarts/PurchaseTicketsCommandHandlerTests.cs`,
      is the acceptance gate, and is GREEN: cart missing ⇒ `NotFoundError` (no save); session missing
      ⇒ `NotFoundError`; terminated ⇒ `ConflictError`; seat held by another cart ⇒ `ConflictError`;
      success ⇒ `Result.Success()` (cart saved, cart-lifecycle removed, per-seat deletes) — and on
      **every** failure `SaveAsync` and both lifecycle side-effects are not received (atomicity).
- [ ] The `ShoppingCart.PurchaseComplete` domain facts in
      `.../ShoppingCarts/ShoppingCartSpecification.cs` cover: `SeatsReserved` ⇒ `PurchaseCompleted` +
      event; already `PurchaseCompleted` ⇒ `Result.Success()`, no event; `InWork` ⇒ `ConflictError`,
      no event.
- [ ] The `MovieSessionSeat.Sell` domain facts in
      `.../Seats/MovieSessionSeatSpecification.cs` cover: another cart ⇒ `ConflictError` (not
      `InvalidOperation`); already-`Sold` ⇒ `ConflictError`.
- [ ] Slice `0004`'s `SelectSeatCommandHandlerTests` **and** slice `0005`'s
      `ReserveTicketsCommandHandlerTests` are **re-run unchanged** as regression gates and stay GREEN.
- [ ] **Opt-outs honoured (per plan §6):** no `WebApplicationFactory` endpoint/integration test (no
      harness exists; the `Match` wiring is covered by compilation, the mapper by slice `0003`'s
      gate); no repository/adapter test (no infrastructure-exception translation changes on this
      path); the real-concurrency seat-race test is deferred to a later Infrastructure-level test.

### Quality gates

Run from `src/services`:

```
dotnet format CinemaBookingManagement.sln
dotnet build  CinemaBookingManagement.sln -warnaserror
dotnet test   CinemaBookingManagement.sln
dotnet test --filter "FullyQualifiedName~PurchaseTicketsCommandHandlerTests"
```

All must pass, including:
- `BookingManagementService.Domain.ArchitectureTests` (domain has no dependency on application,
  aggregate roots have a private parameterless constructor, domain events are `sealed` and named
  `*DomainEvent` — `ShoppingCartPurchaseDomainEvent` already complies).
- The slice's outside-in handler gate (`PurchaseTicketsCommandHandlerTests`).
- The `0004` / `0005` regression gates (`SelectSeatCommandHandlerTests`,
  `ReserveTicketsCommandHandlerTests`).

> Note: the accepted AutoMapper `NU1903` NuGet-audit advisory trips `-warnaserror` at restore time
> (MEMORY `dotnet10-migration`); handle the NuGet audit so the real build/warnings are what is
> validated. `dotnet format` is known to reformat `ReserveSeatsCommandValidatorSpecification.cs` —
> scope the format to touched files or `git checkout` that file (MEMORY `warnaserror-baseline-debt`).
> The working .NET 10 SDK is the x86 install at `C:\Program Files (x86)\dotnet\dotnet.exe` (MEMORY
> `dotnet-sdk-path`). No EF Core model changes in this slice → no migration to run.

If the architecture tests fail, the slice is **not done** even if every other test is green.
