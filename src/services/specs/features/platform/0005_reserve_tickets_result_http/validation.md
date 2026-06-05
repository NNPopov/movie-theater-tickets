# 0005 · ReserveTickets Result→HTTP — Validation

## Inputs

- PRD: ./prd.md
- Plan: ./plan.md
- Requirements: ./requirements.md

> Nature of this slice: a **behaviour-correcting** conversion at one endpoint
> (`POST /api/shoppingcarts/{ShoppingCartId}/reservations`). It replaces `return result;` with
> `result.Match(() => Results.Ok(), ErrorResults.ToProblem)` (the **existing** shared mapper from
> slice `0003`); deletes the handler's bare `throw new Exception("Couldn't Reserve …")` bridge;
> converts `ShoppingCart.SeatsReserve()` `void → Result` (event on a genuine transition only); and
> retypes the **shared** `MovieSessionSeatService.CheckSeatSaleAvailability` `void → Task<Result>`,
> threaded through `SelSeats`/`ReserveSeats`/`SelectSeat`. **Unlike `0004`, two observable statuses
> change on purpose:** seat-not-reservable `500 → 409` and sales-terminated `500 → 409`; the success
> body changes from a serialized `Result` object to **empty** (status stays `200`). The acceptance
> gate is a **focused unit spec of the converted handler** (`ReserveTicketsCommandHandlerTests`) in
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
  `POST /api/shoppingcarts` with one or more seats **selected** into it (so `SeatsReserve` has seats
  to reserve); for the terminated scenario, a movie session whose `SalesTerminated` is `true`; for
  the seat-conflict scenario, a held seat that another cart has already progressed past
  Selected/Available.
- Redis and the distributed-lock backend running (the reserve handler writes the cart/seat lifecycle
  to Redis via `IShoppingCartLifecycleManager` / `IShoppingCartSeatLifecycleManager`).

## Manual scenarios

### S1 — Happy path: reserve the seats held in a cart

**Steps:**

1. ```
   curl -s -i -X POST http://localhost:<port>/api/shoppingcarts/<cartId>/reservations
   ```
2. Read the cart back and confirm it is reserved:
   ```
   curl -s http://localhost:<port>/api/shoppingcarts/<cartId>
   ```

**Expected:**

- Step 1: HTTP `200 OK` with an **empty body** (previously the response serialized a `Result`
  object — confirm the body is now empty).
- Step 2: the cart's status is `SeatsReserved` and its held seats show status `Reserved`.

**Covers:** F1, F2, F11, F14, F23.

### S2 — Not found: reserve against a non-existent cart

**Steps:**

1. ```
   curl -s -i -X POST http://localhost:<port>/api/shoppingcarts/00000000-0000-0000-0000-000000000000/reservations
   ```

**Expected:**

- HTTP `404 Not Found`.
- `ProblemDetails` body: `status: 404`,
  `type: "https://tools.ietf.org/html/rfc7231#section-6.5.4"`,
  `title: "The specified resource was not found."`, `detail` present (the handler's `NotFoundError`
  description). Shape identical to any other `404` in the service — proving the cart-not-found path
  now flows through the shared mapper, not a thrown `ContentNotFoundException`.

**Covers:** F6, F23.

### S3 — Not found: movie session missing (shared helper)

**Steps:**

1. Arrange a cart whose `MovieSessionId` references a session that does not exist (or delete the
   session), then reserve:
   ```
   curl -s -i -X POST http://localhost:<port>/api/shoppingcarts/<cartId>/reservations
   ```

**Expected:**

- HTTP `404 Not Found` with the **same** `ProblemDetails` shape as S2. This `404` originates in the
  shared `CheckSeatSaleAvailability` returning `NotFoundError` (was a thrown
  `ContentNotFoundException`, same status), propagated through `ReserveSeats` and the handler as a
  `Result`.

**Covers:** F8, F19, F23.

### S4 — Conflict: sales terminated (the corrected `500 → 409`)

**Steps:**

1. Point the cart at a movie session whose sales are terminated (`SalesTerminated == true`), then
   reserve:
   ```
   curl -s -i -X POST http://localhost:<port>/api/shoppingcarts/<cartId>/reservations
   ```

**Expected:**

- HTTP `409 Conflict` (**was `500`** before this slice, when the shared helper threw a bare
  `Exception`).
- `ProblemDetails` body: `status: 409`,
  `type: "https://tools.ietf.org/html/rfc7231#section-6.5.8"`, `title: "Conflict"`, **no** `detail`.
- The cart is **not** persisted as `SeatsReserved` (atomicity).

**Covers:** F8, F19, F24, F10.

### S5 — Conflict: a seat is not reservable (the corrected `500 → 409`)

**Steps:**

1. Arrange one of the cart's held seats so it is no longer Selected/Available (e.g. already Reserved
   or Sold by another cart), then reserve:
   ```
   curl -s -i -X POST http://localhost:<port>/api/shoppingcarts/<cartId>/reservations
   ```

**Expected:**

- HTTP `409 Conflict` (**was `500`** before this slice, via the deleted bare
  `throw new Exception("Couldn't Reserve …")` bridge) with the **same** `ProblemDetails` shape as S4.
  This `409` originates from `MovieSessionSeat.Reserve`'s `ConflictError`, propagated through
  `ReserveSeats` and the handler as a `Result` (no `InvalidOperation → 500` trap on the reserve
  path).
- The cart is **not** persisted as `SeatsReserved`, and no lifecycle side-effect ran (atomicity).

**Covers:** F8, F9, F10, F21, F24.

### S6 — Conflict: cart already purchased (status preserved)

**Steps:**

1. Drive `<cartId>` to `PurchaseCompleted`, then attempt to reserve:
   ```
   curl -s -i -X POST http://localhost:<port>/api/shoppingcarts/<cartId>/reservations
   ```

**Expected:**

- HTTP `409 Conflict` with the same `ProblemDetails` shape as S4/S5 (status **unchanged** from before
  the slice, now produced by `SeatsReserve()` returning a `ConflictError` instead of throwing
  `ConflictException`). No event is raised and the cart is unchanged.

**Covers:** F7, F16, F23.

### S7 — Idempotent re-reserve (no duplicate event, success)

**Steps:**

1. Reserve `<cartId>` successfully (S1).
2. Reserve the **same** cart again:
   ```
   curl -s -i -X POST http://localhost:<port>/api/shoppingcarts/<cartId>/reservations
   ```

**Expected:**

- HTTP `200 OK`, empty body, on both calls. The second call is an idempotent success — the cart
  stays `SeatsReserved` and **no second** `ShoppingCartReservedDomainEvent` is raised (the
  unconditional-event bug is fixed; observable downstream as no duplicate reservation side-effects).

**Covers:** F11, F14, F15.

### S8 — Body-shape parity check (regression for the reused mapper)

**Steps:**

1. Capture the `404` body from S2/S3 and compare it field-by-field to any exception-driven not-found
   body, e.g. `GET /api/shoppingcarts/00000000-0000-0000-0000-000000000000`.
2. Capture the `409` body from S4/S5/S6 and compare them to each other and to any exception-driven
   `409`.

**Expected:**

- The `404` bodies are identical in shape (`status`/`type`/`title`/`detail`).
- All three `409` bodies (terminated, seat-not-reservable, already-purchased) are identical in shape
  (`status`/`type`/`title`, no `detail`) — proving every conflict on this path is produced by the
  **same** mechanism and `ProblemDetails` shape.

**Covers:** F3, F23, F24.

## Code review checklist

Each line is a yes/no question. Reject the PR until all are yes.

### Architecture

- [ ] The converted use-case stays in `Application/ShoppingCarts/Command/ReserveSeats/`; the command
      remains a `record` `ReserveTicketsCommand : IRequest<Result>` and the handler a MediatR
      `IRequestHandler<ReserveTicketsCommand, Result>`. (N1)
- [ ] The shared mapper `API/Endpoints/Common/ErrorResults.cs` is **reused unchanged**; no new
      mapping module is created. (F3)
- [ ] No `IResult` / `HttpContext` / ASP.NET type appears in `Domain` or `Application`; the
      `Error → IResult` mapping exists only in the `API` layer (Dependency Rule). (N4)
- [ ] The endpoint delegate contains no business logic: it binds the route value, builds the command,
      `sender.Send`, and shapes the result via `result.Match(() => Results.Ok(), ErrorResults.ToProblem)`. (N6)
- [ ] No new aggregate, domain event, or repository interface was introduced; `ShoppingCart`,
      `MovieSessionSeatService`, `MovieSessionSeat`, and `ShoppingCartReservedDomainEvent` are
      pre-existing.

### Error handling

- [ ] The `reservations` endpoint's `return result;` is **deleted**; success is `Results.Ok()` (empty
      `200`) and the failure branch is `ErrorResults.ToProblem`. (F1, F2)
- [ ] The handler's bare `throw new Exception("Couldn't Reserve …")` is **deleted**; the failing
      `Result` from `ReserveSeats` is returned unchanged. (F9)
- [ ] The handler returns `NotFoundError` (`DomainErrors<ShoppingCart>.NotFound`) for the missing cart
      instead of throwing `ContentNotFoundException`; `GetShoppingCartOrThrow` is removed. (F6)
- [ ] The handler consumes `cart.SeatsReserve()`'s `Result` and short-circuits on `IsFailure`
      (already-purchased ⇒ `ConflictError`). (F7)
- [ ] The handler short-circuits and returns any failing `Result` **before** `SaveAsync`, `SetAsync`,
      and the per-seat `DeleteAsync` calls — no persistence or lifecycle side-effect on failure. (F10)
- [ ] On success the handler returns `Result.Success()` after `SaveAsync`, `SetAsync`, and per-seat
      deletes. (F11)
- [ ] Repository and Redis lifecycle faults still propagate as exceptions (not converted to
      `Result`). (F12)
- [ ] `ShoppingCart.SeatsReserve()` is retyped `void → Result`: `InWork` ⇒ `SeatsReserved` + exactly
      one `ShoppingCartReservedDomainEvent` + success; already `SeatsReserved` ⇒ `Result.Success()`,
      **no** event; `PurchaseCompleted` ⇒ `ConflictError`, no event; any other status ⇒
      `ConflictError`, no event. (F13–F17)
- [ ] `SeatsReserve()` **no longer calls** `EnsurePurchaseIsNotCompleted()`, and that shared guard is
      otherwise **unchanged** (still used by `PurchaseComplete` / `CalculateCartAmount`). (F18)
- [ ] `MovieSessionSeatService.CheckSeatSaleAvailability` is retyped `Task → Task<Result>`:
      session-not-found ⇒ `NotFoundError`; sales-terminated ⇒ `ConflictError` (the bare
      `throw new Exception(...)` is **deleted**); success ⇒ `Result.Success()`. (F19)
- [ ] All three callers — `SelSeats`, `ReserveSeats`, `SelectSeat` — consume the new `Result` and
      short-circuit on `IsFailure`. (F20)
- [ ] `MovieSessionSeat.Reserve` is **unchanged** and returns a `ConflictError` for the bad-status
      case. (F21)
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

### Scope / known side-effect

- [ ] Only `ReserveTickets` is converted at the endpoint level; the `purchase` delegate still does
      `return result;` (deferred to `0006`). (N12)
- [ ] The accepted interim purchase-path side-effect of the shared-helper retype
      (session-not-found / terminated serialize as `200` on the purchase path until `0006`) is
      acknowledged in the PR description and flagged for `0006`. (F25)

### OpenAPI / metadata

- [ ] The `reservations` endpoint declares `.Produces(200)`, `.Produces(404)`, `.Produces(409)` and
      no longer declares `.Produces<bool>(201)` / `.Produces(204)`. (F4)
- [ ] `.WithName("ReserveSeats")`, `.WithTags(Tag)` are retained, and the endpoint still maps the
      route `ShoppingCartId` to `ReserveTicketsCommand`. (F5)

### Tests

- [ ] The `ReserveTicketsCommandHandler` outside-in gate test exists in
      `BookingManagement/tests/BookingManagementService.Domain.UnitTests/ShoppingCarts/ReserveTicketsCommandHandlerTests.cs`,
      is the acceptance gate, and is GREEN: cart missing ⇒ `NotFoundError` (no save); session missing
      ⇒ `NotFoundError`; terminated ⇒ `ConflictError`; seat not reservable ⇒ `ConflictError`;
      already-purchased ⇒ `ConflictError`; success ⇒ `Result.Success()` (cart saved, lifecycle set,
      per-seat deletes) — and on **every** failure `SaveAsync`/`SetAsync`/`DeleteAsync` are not
      received (atomicity).
- [ ] The `ShoppingCart.SeatsReserve` domain facts in
      `.../ShoppingCarts/ShoppingCartSpecification.cs` cover: `InWork` ⇒ `SeatsReserved` + event;
      already `SeatsReserved` ⇒ `Result.Success()`, no event; `PurchaseCompleted` ⇒ `ConflictError`,
      no event.
- [ ] Slice `0004`'s `SelectSeatCommandHandlerTests` is **re-run unchanged** as the regression gate
      for the shared-helper retype and stays GREEN.
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
dotnet test --filter "FullyQualifiedName~ReserveTicketsCommandHandlerTests"
```

All must pass, including:
- `BookingManagementService.Domain.ArchitectureTests` (domain has no dependency on application,
  aggregate roots have a private parameterless constructor, domain events are `sealed` and named
  `*DomainEvent` — `ShoppingCartReservedDomainEvent` already complies).
- The slice's outside-in handler gate (`ReserveTicketsCommandHandlerTests`).
- The `0004` regression gate (`SelectSeatCommandHandlerTests`).

> Note: the accepted AutoMapper `NU1903` NuGet-audit advisory trips `-warnaserror` at restore time
> (MEMORY `dotnet10-migration`); handle the NuGet audit so the real build/warnings are what is
> validated. The working .NET 10 SDK is the x86 install at `C:\Program Files (x86)\dotnet\dotnet.exe`
> (MEMORY `dotnet-sdk-path`). No EF Core model changes in this slice → no migration to run.

If the architecture tests fail, the slice is **not done** even if every other test is green.
