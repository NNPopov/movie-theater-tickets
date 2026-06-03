# 0003 · AssignClientCart Result→HTTP — Validation

## Inputs

- PRD: ./prd.md
- Plan: ./plan.md
- Requirements: ./requirements.md

> Nature of this slice: a **mechanism swap** at one endpoint (delete the `Result → exception`
> bridge; resolve the handler's `Result` with `Match`-to-HTTP via a new shared `Error → IResult`
> mapper) plus one bug fix (record the signed-in client as owner). Externally observable status
> codes are unchanged: `200` / `404` / `409`. The acceptance gate is a **focused unit spec of the
> mapper** (`ErrorResults`) in `BookingManagementService.API.UnitTests` — there is **no**
> `WebApplicationFactory` harness in this repo (see Prerequisites and the test section below).

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
- Seed data: at least one shopping cart created via `POST /api/shoppingcarts` (note its id),
  and a second cart for the "already owns a different active cart" scenario.
- A valid Bearer token from Keycloak for the signed-in client (the `assignclient` endpoint
  is `[RequireAuthorization]`; the client id is read from the token's `nameidentifier` claim).

## Manual scenarios

### S1 — Happy path: assign the signed-in client to a cart

**Steps:**

1. ```
   curl -s -i -X PUT http://localhost:<port>/api/shoppingcarts/<cartId>/assignclient \
     -H "Authorization: Bearer <token>"
   ```
2. Read the cart back and confirm its owner:
   ```
   curl -s http://localhost:<port>/api/shoppingcarts/<cartId> \
     -H "Authorization: Bearer <token>"
   ```

**Expected:**

- Step 1: HTTP `200 OK`, empty body.
- Step 2: the returned cart's `clientId` equals the **signed-in client's id** (the
  `nameidentifier` claim in `<token>`), **not** the cart id — this confirms the wrong-owner
  bug fix.

**Covers:** F1, F11, F16.

### S2 — Not found: assign to a non-existent cart

**Steps:**

1. ```
   curl -s -i -X PUT http://localhost:<port>/api/shoppingcarts/00000000-0000-0000-0000-000000000000/assignclient \
     -H "Authorization: Bearer <token>"
   ```

**Expected:**

- HTTP `404 Not Found`.
- `ProblemDetails` body: `status: 404`,
  `type: "https://tools.ietf.org/html/rfc7231#section-6.5.4"`,
  `title: "The specified resource was not found."`, `detail` present (the handler's
  `NotFoundError` description). Shape identical to any other `404` in the service.

**Covers:** F1, F4, F7, F8.

### S3 — Conflict A: client already owns a *different* active cart

**Steps:**

1. With `<token>`, create cart A and assign the client to it (S1 happy path on `<cartIdA>`).
2. Create a second cart B (`<cartIdB>`), then attempt to assign the **same** client:
   ```
   curl -s -i -X PUT http://localhost:<port>/api/shoppingcarts/<cartIdB>/assignclient \
     -H "Authorization: Bearer <token>"
   ```

**Expected:**

- HTTP `409 Conflict`.
- `ProblemDetails` body: `status: 409`,
  `type: "https://tools.ietf.org/html/rfc7231#section-6.5.8"`, `title: "Conflict"`, **no**
  `detail`. (This `409` originates from the **handler's** `ConflictError`.)

**Covers:** F1, F5, F7, F9.

### S4 — Conflict B: assign to a cart that already has an owner

**Steps:**

1. Assign client X to `<cartIdC>` (S1).
2. As a *different* signed-in client Y (token `<tokenY>`) who does **not** already own an active
   cart, attempt to assign to the same `<cartIdC>`:
   ```
   curl -s -i -X PUT http://localhost:<port>/api/shoppingcarts/<cartIdC>/assignclient \
     -H "Authorization: Bearer <tokenY>"
   ```

**Expected:**

- HTTP `409 Conflict` with the **same** `ProblemDetails` body shape as S3 (`title: "Conflict"`,
  no `detail`). This `409` originates from the **domain** `ConflictError` now *returned* by
  `ShoppingCart.AssignClientId` and propagated through the handler's `IsFailure` branch — proving
  both `409`s are produced by the same mechanism and carry the same shape.

**Covers:** F1, F5, F7, F10, F12, F13.

### S5 — Unauthorized: no token

**Steps:**

1. ```
   curl -s -i -X PUT http://localhost:<port>/api/shoppingcarts/<cartId>/assignclient
   ```

**Expected:**

- HTTP `401 Unauthorized` (enforced by `.RequireAuthorization()`, before the handler runs).

**Covers:** F16.

### S6 — Body-shape parity check (regression for the canonical mapper)

**Steps:**

1. Capture the `404` body from S2 and compare it field-by-field to the `404` body from any
   exception-driven not-found path, e.g.:
   ```
   curl -s http://localhost:<port>/api/shoppingcarts/00000000-0000-0000-0000-000000000000 \
     -H "Authorization: Bearer <token>"
   ```
2. Capture the `409` body from S3/S4 and compare it to any exception-driven `409`.

**Expected:**

- The `404` bodies are identical in shape (`status`/`type`/`title`/`detail`).
- The `409` bodies are identical in shape (`status`/`type`/`title`, no `detail`).
- Confirms the `Result` path and the exception path are indistinguishable to clients.

**Covers:** F7.

## Code review checklist

Each line is a yes/no question. Reject the PR until all are yes.

### Architecture

- [ ] The converted use-case stays in `Application/ShoppingCarts/Command/AssingClientCart/`; the
      command remains a `record` `AssignClientCartCommand : IRequest<Result>` and the handler a
      MediatR `IRequestHandler<AssignClientCartCommand, Result>`.
- [ ] The new shared mapper lives in `API/Endpoints/Common/ErrorResults.cs` (API layer only) and
      is the single `Error → IResult` policy point.
- [ ] No `IResult` / `HttpContext` / ASP.NET type appears in `Domain` or `Application`; the
      `Error → IResult` mapping exists only in the `API` layer (Dependency Rule).
- [ ] The endpoint delegate contains no business logic: it reads identity, builds the command,
      `sender.Send`, and shapes the result via `result.Match(() => Results.Ok(), ErrorResults.ToProblem)`.
- [ ] No new aggregate, domain event, or repository interface was introduced; `ShoppingCart` and
      `ShoppingCartAssignedToClientDomainEvent` are pre-existing.

### Error handling

- [ ] The `assignclient` endpoint's `Result → exception` bridge is **deleted**: no
      `throw new ConflictException(...)` / `throw new ContentNotFoundException(...)` re-throw of a
      matched `Error` remains in the delegate. (F2)
- [ ] The bare `throw new Exception(failure.Description)` in the failure branch is **deleted**. (F3)
- [ ] `ShoppingCart.AssignClientId` **returns** a `ConflictError` on the already-assigned case
      instead of throwing `ConflictException`. (F12)
- [ ] `ShoppingCart.AssignClientId` appends `ShoppingCartAssignedToClientDomainEvent` **only** on
      the success branch (after the conflict guard). (F13)
- [ ] `Ensure.NotEmpty(clientId, ...)` in `AssignClientId` remains a thrown structural guard
      (not modelled as a `Result`). (F14)
- [ ] The handler returns `NotFoundError` for the missing cart and `ConflictError` for the
      other-active-cart case, and its `if (result.IsFailure) return result;` branch propagates the
      domain `Result` (no longer dead). (F8, F9, F10)
- [ ] The handler passes `request.ClientId` (not `request.ShoppingCartId`) to
      `cart.AssignClientId(...)`. (F11)
- [ ] `ErrorResults.ToProblem` maps `NotFoundError ⇒ 404`, `ConflictError ⇒ 409`, anything else
      `⇒ 500` via `Results.Problem`, with `ProblemDetails` shapes mirroring `CustomExceptionHandler`
      (404 carries `Detail = error.Description`; 409 and 500 are title-only). (F4, F5, F6, F7)
- [ ] No new cross-cutting `*Exception` or `Error` type was introduced; the mapper consumes
      existing `NotFoundError`/`ConflictError`/`Error`. (N3)
- [ ] No handler sets an HTTP status code or references `HttpContext`. (N5)

### Stable infrastructure

- [ ] `CustomExceptionHandler.cs` is **unchanged** (the mapper mirrors its shapes; it does not
      modify them). (N7)
- [ ] No base type (`AggregateRoot`, `Entity`, `Result`, `Error`), MediatR pipeline behaviour,
      `IEndpoints`/`EndpointExtensions` mechanism, or `Program.cs` was changed.
- [ ] No DI registration line was needed (the mapper is a static deep module); if one was added,
      flag and justify it.

### OpenAPI / metadata

- [ ] The `assignclient` endpoint declares `.Produces(200)`, `.Produces(404)`, `.Produces(409)`
      and no longer declares `.Produces(201)` / `.Produces(204)`. (F15)
- [ ] `.WithName("AssignUser")`, `.WithTags(Tag)`, `.RequireAuthorization()` are retained. (F16)

### Tests

- [ ] The `ErrorResults` outside-in gate test exists in
      `tests/BookingManagementService.API.UnitTests/Endpoints/Common/ErrorResultsOutsideInTests.cs`,
      is the acceptance gate, and is GREEN (`NotFoundError ⇒ 404`, `ConflictError ⇒ 409`,
      unrecognised `Error ⇒ 500`, each with the `ProblemDetails` body).
- [ ] The `AssignClientCartCommandHandler` unit test covers: cart missing ⇒ `NotFoundError`;
      other active cart ⇒ `ConflictError`; success ⇒ `Result.Success()` and owner equals the
      client id (bug fix); domain `IsFailure` propagated.
- [ ] The `ShoppingCart.AssignClientId` domain unit test covers: already-assigned ⇒ `ConflictError`
      with **no** event; success ⇒ owner assigned and `ShoppingCartAssignedToClientDomainEvent`
      raised.
- [ ] **Opt-outs honoured (per plan §6):** no `WebApplicationFactory` endpoint/integration test
      (no harness exists; `Match` wiring covered by compilation + the mapper gate); no
      repository/adapter test (no infrastructure-exception translation changes on this path).

### Quality gates

Run from `src/services`:

```
dotnet format CinemaBookingManagement.sln
dotnet build  CinemaBookingManagement.sln -warnaserror
dotnet test   CinemaBookingManagement.sln
dotnet test --filter "FullyQualifiedName~ErrorResultsOutsideInTests"
```

All must pass, including:
- `BookingManagementService.Domain.ArchitectureTests` (domain has no dependency on application,
  aggregate roots have a private parameterless constructor, domain events are `sealed` and named
  `*DomainEvent` — `ShoppingCartAssignedToClientDomainEvent` already complies).
- The slice's outside-in gate (`ErrorResultsOutsideInTests`).

> Note: the accepted AutoMapper `NU1903` NuGet-audit advisory trips `-warnaserror` at restore
> time (MEMORY `dotnet10-migration`); handle the NuGet audit so the real build/warnings are what
> is validated. No EF Core model changes in this slice → no migration to run.

If the architecture tests fail, the slice is **not done** even if every other test is green.
