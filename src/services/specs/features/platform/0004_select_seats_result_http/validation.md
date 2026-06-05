# 0004 · SelectSeats Result→HTTP — Validation

## Inputs

- PRD: ./prd.md
- Plan: ./plan.md
- Requirements: ./requirements.md

> Nature of this slice: a **mechanism swap** at one endpoint (delete the dead
> `failure => Results.BadRequest(...)` branch; resolve the handler's `Result` with `Match`-to-HTTP
> via the **existing** shared `ErrorResults.ToProblem` mapper from slice `0003`) plus the deletion
> of a **second, hidden** `Result → ConflictException` bridge inside
> `MovieSessionSeatService.SelectSeat`, plus one defusing domain change
> (`MovieSessionSeat.Select` "another cart" branch `InvalidOperation → ConflictError`). Externally
> observable status codes are **unchanged**: `200` / `404` / `409` / `400` / `423` / `500`. The
> acceptance gate is a **focused unit spec of the converted handler**
> (`SelectSeatCommandHandlerTests`) in `BookingManagementService.Domain.UnitTests` — there is **no**
> `WebApplicationFactory` harness in this repo (see Prerequisites and the test section).

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
- Seed data: at least one movie session with available seats (note its `showtimeId` and an
  available `row`/`number`), and a shopping cart created via `POST /api/shoppingcarts` (note its
  id). For the "another cart" conflict, a second cart that has already selected the same seat.
- Redis and the distributed-lock backend running (the `select` handler acquires a distributed lock
  and writes the seat lifecycle to Redis).

## Manual scenarios

### S1 — Happy path: select an available seat

**Steps:**

1. ```
   curl -s -i -X POST http://localhost:<port>/api/shoppingcarts/<cartId>/seats/select \
     -H "Content-Type: application/json" \
     -d '{"showtimeId": "<sessionId>", "row": 1, "number": 1}'
   ```
2. Read the cart back and confirm the seat is held:
   ```
   curl -s http://localhost:<port>/api/shoppingcarts/<cartId>
   ```

**Expected:**

- Step 1: HTTP `200 OK`, empty body.
- Step 2: the cart contains the selected seat `(row 1, number 1)` for `<sessionId>`, and the
  movie-session seat's status is `Selected`.

**Covers:** F1, F7, F13, F19.

### S2 — Not found: select against a non-existent cart

**Steps:**

1. ```
   curl -s -i -X POST http://localhost:<port>/api/shoppingcarts/00000000-0000-0000-0000-000000000000/seats/select \
     -H "Content-Type: application/json" \
     -d '{"showtimeId": "<sessionId>", "row": 1, "number": 1}'
   ```

**Expected:**

- HTTP `404 Not Found`.
- `ProblemDetails` body: `status: 404`,
  `type: "https://tools.ietf.org/html/rfc7231#section-6.5.4"`,
  `title: "The specified resource was not found."`, `detail` present (the handler's `NotFoundError`
  description). Shape identical to any other `404` in the service — proving the cart-not-found path
  now flows through the shared mapper, not a thrown `ContentNotFoundException`.

**Covers:** F1, F4, F20.

### S3 — Conflict A: seat status is not Available

**Steps:**

1. Select seat `(1,1)` successfully on `<cartId>` (S1).
2. With a *different* cart `<cartIdB>`, attempt to select the **same** seat after it has progressed
   beyond Available (e.g. it is already Selected/Reserved/Sold by the first cart):
   ```
   curl -s -i -X POST http://localhost:<port>/api/shoppingcarts/<cartIdB>/seats/select \
     -H "Content-Type: application/json" \
     -d '{"showtimeId": "<sessionId>", "row": 1, "number": 1}'
   ```

**Expected:**

- HTTP `409 Conflict`.
- `ProblemDetails` body: `status: 409`,
  `type: "https://tools.ietf.org/html/rfc7231#section-6.5.8"`, `title: "Conflict"`, **no** `detail`.
  This `409` originates from `MovieSessionSeat.Select`'s "status is not Available" `ConflictError`,
  propagated through the domain service and the handler as a `Result`.
- The first cart still holds the seat; `<cartIdB>` was **not** persisted holding it.

**Covers:** F1, F5, F6, F11, F20.

### S4 — Conflict B: seat is being processed by another shopping cart

**Steps:**

1. Arrange a movie-session seat whose `ShoppingCartId` is a *different*, non-empty cart but whose
   status is still Available (the "claimed-by-another-cart-but-not-yet-transitioned" case).
2. Attempt to select it from `<cartId>`:
   ```
   curl -s -i -X POST http://localhost:<port>/api/shoppingcarts/<cartId>/seats/select \
     -H "Content-Type: application/json" \
     -d '{"showtimeId": "<sessionId>", "row": 1, "number": 1}'
   ```

**Expected:**

- HTTP `409 Conflict` with the **same** `ProblemDetails` body shape as S3 (`title: "Conflict"`, no
  `detail`). This is the case that was a base `Error` via `InvalidOperation` before the slice and
  would have silently become a `500` once the path was honestly converted; the
  `InvalidOperation → ConflictError` change keeps it a `409` — proving both seat conflicts are
  produced by the same mechanism and carry the same shape.

**Covers:** F1, F5, F6, F10, F20.

### S5 — Validation/state guard unchanged: cart not in a state that accepts seats

**Steps:**

1. Drive `<cartId>` into a state where `EnsureSeatCanBeAdded` rejects the seat (e.g. a non-InWork
   cart, a duplicate seat, or exceeding max seats), then attempt the select.

**Expected:**

- HTTP `409` (cart not InWork ⇒ `ConflictException`) or HTTP `400`
  (`ValidationProblemDetails` for wrong session / duplicate / max-seats ⇒ `DomainValidationException`),
  exactly as before this slice — these guards are **not** converted.

**Covers:** F16, F20.

### S6 — Body-shape parity check (regression for the reused mapper)

**Steps:**

1. Capture the `404` body from S2 and compare it field-by-field to any exception-driven not-found
   body, e.g. `GET /api/shoppingcarts/00000000-0000-0000-0000-000000000000`.
2. Capture the `409` body from S3/S4 and compare it to any exception-driven `409` (e.g. the S5 cart
   conflict).

**Expected:**

- The `404` bodies are identical in shape (`status`/`type`/`title`/`detail`).
- The `409` bodies are identical in shape (`status`/`type`/`title`, no `detail`).
- Confirms the converted `Result` path and the exception path are indistinguishable to clients
  (the Flutter client's `statusCode == 409 ⇒ ConflictFailure` handling keeps working).

**Covers:** F3, F20.

## Code review checklist

Each line is a yes/no question. Reject the PR until all are yes.

### Architecture

- [ ] The converted use-case stays in `Application/ShoppingCarts/Command/SelectSeats/`; the command
      remains a `record` `SelectSeatCommand : IRequest<Result>` and the handler a MediatR
      `IRequestHandler<SelectSeatCommand, Result>`. (N1)
- [ ] The shared mapper `API/Endpoints/Common/ErrorResults.cs` is **reused unchanged**; no new
      mapping module is created. (F3)
- [ ] No `IResult` / `HttpContext` / ASP.NET type appears in `Domain` or `Application`; the
      `Error → IResult` mapping exists only in the `API` layer (Dependency Rule). (N4)
- [ ] The endpoint delegate contains no business logic: it binds the request, builds the command,
      `sender.Send`, and shapes the result via `result.Match(() => Results.Ok(), ErrorResults.ToProblem)`. (N6)
- [ ] No new aggregate, domain event, or repository interface was introduced; `MovieSessionSeat`,
      `MovieSessionSeatService`, and `MovieSessionSeatStatusUpdatedDomainEvent` are pre-existing.

### Error handling

- [ ] The `seats/select` endpoint's dead `failure => Results.BadRequest(failure.Description)`
      branch is **deleted**; the failure branch is `ErrorResults.ToProblem`. (F2)
- [ ] `MovieSessionSeatService.SelectSeat` is retyped to `Task<Result>`; the
      `else { throw new ConflictException(...); }` bridge is **deleted**; on failure the aggregate
      `Result` is returned, on success the seat is persisted and `Result.Success()` returned. (F8, F9)
- [ ] `MovieSessionSeat.Select` **returns** `DomainErrors<MovieSessionSeat>.ConflictException(...)`
      (a `ConflictError`) for the "another shopping cart" case — **not** `InvalidOperation`. (F10)
- [ ] `MovieSessionSeat.Select` still returns a `ConflictError` for the "status not Available" case,
      and appends `MovieSessionSeatStatusUpdatedDomainEvent` **only** on the success branch. (F11, F12)
- [ ] `Ensure.NotEmpty(shoppingCartId, ...)` / `Ensure.NotEmpty(hashId, ...)` in `Select` remain
      thrown structural guards. (F13)
- [ ] The handler returns `NotFoundError` (`DomainErrors<ShoppingCart>.NotFound`) for the missing
      cart instead of throwing `ContentNotFoundException`. (F4)
- [ ] The handler short-circuits on the failing seat-claim `Result` and returns it **before**
      `SaveShoppingCart`, so the cart is not persisted on a failed claim. (F5, F6)
- [ ] The handler keeps the distributed-lock guard as a thrown `LockedException` (423) and the Redis
      seat-lifecycle failure / `ReturnSeatToAvailable` rollback as a thrown `InvalidOperationException`
      (500) that re-throws (a compensating action, not over-catching). (F14, F15)
- [ ] The `cart.EnsureSeatCanBeAdded(...)` guards and the shared-helper not-found checks
      (`GetMovieSessionSeat`, `CheckSeatSaleAvailability`, the sales-terminated bare `Exception`)
      are **unchanged**. (F16, F17)
- [ ] No new cross-cutting `*Exception` or `Error` type was introduced; the
      `InvalidOperation → ConflictError` change reuses the existing
      `DomainErrors<T>.ConflictException` / `ConflictError`. (N3)
- [ ] No handler sets an HTTP status code or references `HttpContext`. (N5)

### Stable infrastructure

- [ ] `CustomExceptionHandler.cs` and `ErrorResults.cs` are **unchanged** (this slice only re-routes
      outcomes through the existing policy objects). (N7)
- [ ] No base type (`AggregateRoot`, `Entity`, `Result`, `Error`), MediatR pipeline behaviour,
      `IEndpoints`/`EndpointExtensions` mechanism, or `Program.cs` was changed.
- [ ] No DI registration line was needed; if one was added, flag and justify it.

### OpenAPI / metadata

- [ ] The `seats/select` endpoint declares `.Produces(200)`, `.Produces(404)`, `.Produces(409)` and
      no longer declares `.Produces(201)` / `.Produces(204)`. (F18)
- [ ] `.WithName("SelectSeat")`, `.WithTags(Tag)` are retained, and `ReserveSeatsRequest` is still
      mapped to `SelectSeatCommand`. (F19)

### Tests

- [ ] The `SelectSeatCommandHandler` outside-in gate test exists in
      `BookingManagement/tests/BookingManagementService.Domain.UnitTests/ShoppingCarts/SelectSeatCommandHandlerTests.cs`,
      is the acceptance gate, and is GREEN: cart missing ⇒ `NotFoundError` (no save); seat not
      Available ⇒ `ConflictError` (no save); another cart ⇒ `ConflictError` (no save); available
      seat ⇒ `Result.Success()` (saved).
- [ ] The `MovieSessionSeat.Select` domain unit test
      (`.../Seats/MovieSessionSeatSpecification.cs`) covers: status not Available ⇒ `ConflictError`,
      no event; another cart ⇒ `ConflictError`, no event; success ⇒ `Status == Selected` and the
      event raised.
- [ ] **Opt-outs honoured (per plan §6):** no `WebApplicationFactory` endpoint/integration test
      (no harness exists; the `Match` wiring is covered by compilation, the mapper by slice 0003's
      gate); no repository/adapter test (no infrastructure-exception translation changes on this
      path); the real-concurrency seat-race test is deferred to a later Infrastructure-level test.

### Quality gates

Run from `src/services`:

```
dotnet format CinemaBookingManagement.sln
dotnet build  CinemaBookingManagement.sln -warnaserror
dotnet test   CinemaBookingManagement.sln
dotnet test --filter "FullyQualifiedName~SelectSeatCommandHandlerTests"
```

All must pass, including:
- `BookingManagementService.Domain.ArchitectureTests` (domain has no dependency on application,
  aggregate roots have a private parameterless constructor, domain events are `sealed` and named
  `*DomainEvent` — `MovieSessionSeatStatusUpdatedDomainEvent` already complies).
- The slice's outside-in handler gate (`SelectSeatCommandHandlerTests`).

> Note: the accepted AutoMapper `NU1903` NuGet-audit advisory trips `-warnaserror` at restore time
> (MEMORY `dotnet10-migration`); handle the NuGet audit so the real build/warnings are what is
> validated. No EF Core model changes in this slice → no migration to run.

If the architecture tests fail, the slice is **not done** even if every other test is green.
