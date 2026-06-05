# 0002 · ContentNotFound404 — Validation

## Inputs

- PRD: ./prd.md
- Plan: ./plan.md
- Requirements: ./requirements.md

> This slice is a cross-cutting HTTP-contract correction at `CustomExceptionHandler` plus two
> empty-state carve-outs. It deliberately touches a **stable** file (ADR-002 step 2) and adds
> **no** new use-case folder, repository, migration, or aggregate. The checklist below is
> adapted accordingly — many generic "new use-case" items are marked **N/A** with a reason.

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
- Seed data: at least one movie, one movie session, and one shopping cart with a known id;
  also note one **non-existent** GUID to probe the not-found paths.
- For authenticated endpoints (`/api/shoppingcarts/current`): a valid Bearer token from
  Keycloak (or a dev test JWT) whose `nameidentifier` claim is a client with **no** active cart
  for scenario S5.

## Manual scenarios

### S1 — Non-existent movie by id ⇒ 404 + ProblemDetails

**Steps:**
1. ```
   curl -s -i http://localhost:<port>/api/movies/00000000-0000-0000-0000-000000000000
   ```

**Expected:**
- HTTP `404 Not Found` (was `204` before this slice).
- Body is `application/problem+json` with `status: 404`,
  `type: "https://tools.ietf.org/html/rfc7231#section-6.5.4"`,
  `title: "The specified resource was not found."`, and a `detail` string.

**Covers:** F1, F2, F6.

### S2 — Non-existent movie session by id ⇒ 404

**Steps:**
1. ```
   curl -s -i http://localhost:<port>/api/moviesessions/00000000-0000-0000-0000-000000000000
   ```

**Expected:**
- HTTP `404`; `ProblemDetails` body as in S1.

**Covers:** F1, F2, F6, F12.

### S3 — Non-existent shopping cart by id ⇒ 404

**Steps:**
1. ```
   curl -s -i http://localhost:<port>/api/shoppingcarts/00000000-0000-0000-0000-000000000000
   ```

**Expected:**
- HTTP `404`; `ProblemDetails` body as in S1.

**Covers:** F1, F2, F6, F12.

### S4 — Shape parity with NotFoundException

**Steps:**
1. Hit any endpoint that raises the application `NotFoundException` (if one is reachable), or
   compare the S1–S3 body against the existing `NotFoundException` `ProblemDetails` shape.

**Expected:**
- The `ContentNotFoundException` `404` body is indistinguishable in shape from the
  `NotFoundException` `404` body (same `status`, `type`, `title`, `detail` fields).

**Covers:** F3, F4.

### S5 — Current cart, no active cart ⇒ 204 (carve-out A, empty state)

**Steps:**
1. ```
   curl -s -i http://localhost:<port>/api/shoppingcarts/current \
     -H "Authorization: Bearer <token-for-client-with-no-active-cart>"
   ```

**Expected:**
- HTTP `204 No Content`, empty body (not `404`, not `200`).

**Covers:** F7, F9.

### S6 — Current cart, active cart exists ⇒ 200 + body

**Steps:**
1. Create/assign a cart for the authenticated client, then:
   ```
   curl -s -i http://localhost:<port>/api/shoppingcarts/current \
     -H "Authorization: Bearer <token>"
   ```

**Expected:**
- HTTP `200 OK` with a `CreateShoppingCartResponse` JSON body (`shoppingCartId`, `hashId`).

**Covers:** F9.

### S7 — Current cart, inconsistent state ⇒ 404

**Steps:**
1. Arrange (DB) an active-cart id recorded for the client whose cart record is missing, then
   call `GET /api/shoppingcarts/current` with that client's token.

**Expected:**
- HTTP `404` with `ProblemDetails` (the inconsistent branch still throws
  `ContentNotFoundException`).

**Covers:** F8, F9.

### S8 — Movie sessions list, none upcoming ⇒ 200 [] (carve-out B, empty list)

**Steps:**
1. ```
   curl -s -i http://localhost:<port>/api/movies/<movieId-with-no-upcoming-sessions>/moviesessions
   ```

**Expected:**
- HTTP `200 OK` with body `[]` (not `204`, not `404`).

**Covers:** F10, F11, F12.

### S9 — Movie sessions list, sessions present ⇒ 200 with items

**Steps:**
1. ```
   curl -s -i http://localhost:<port>/api/movies/<movieId-with-upcoming-sessions>/moviesessions
   ```

**Expected:**
- HTTP `200 OK` with a non-empty JSON array of `MovieSessionsDto`.

**Covers:** F10.

### S10 — OpenAPI document reflects the corrected contract

**Steps:**
1. Open the OpenAPI/Swagger document and inspect the affected paths.

**Expected:**
- `GET /api/shoppingcarts/{ShoppingCartId}` and `GET /api/moviesessions/{movieSessionId}`
  declare `404` (not the old not-found `204`).
- `GET /api/shoppingcarts/current` declares `200` (with body), `204`, and `404` (no `201`/`409`).
- `GET /api/movies/{movieId}/moviesessions` declares `200` only (no not-found `204`).
- `GET /api/movies/{movieId}` declares `404` (unchanged).
- `204`-on-success endpoints (create cart, select/unselect/reserve/purchase, `activemovies`,
  `seats`) still declare `204`.

**Covers:** F12, F15.

## Code review checklist

### The central flip (CustomExceptionHandler)

- [ ] `HandleContentNotFoundException` sets `StatusCode = 404` and writes a `ProblemDetails`
      (`Status`/`Type`/`Title`/`Detail`), no longer calling `Response.CompleteAsync()` with
      `204`. (F1, F2)
- [ ] The body shape matches `HandleNotFoundException` exactly (same `Type`, `Title` semantics,
      `Detail = ex.Message`). (F3)
- [ ] `HandleNotFoundException` is unchanged. (F4)
- [ ] The exception is still logged at `Warning`. (F5)
- [ ] **Only** the `HandleContentNotFoundException` body changed: the `FrozenDictionary`
      registration, `TryHandleAsync`, and every other writer are untouched. (N7)

### Carve-out A — current cart

- [ ] `GetCurrentShoppingCartQuery`/handler return type is `CreateShoppingCartResponse?`. (F7)
- [ ] No-active-cart (`Guid.Empty`) branch returns `null` instead of throwing. (F7)
- [ ] The inconsistent branch (`shoppingCart is null`) still throws `ContentNotFoundException`. (F8)
- [ ] The `current` endpoint maps `null ⇒ Results.NoContent()` and non-null ⇒ `Results.Ok(result)`. (F9)

### Carve-out B — movie sessions list

- [ ] `GetMovieSessionsQueryHandler` no longer throws on empty; returns the (possibly empty)
      mapped collection. (F10)
- [ ] The `TimeProvider.System` "upcoming" filter is unchanged. (out of scope)
- [ ] The `api/movies/{movieId}/moviesessions` endpoint returns `200` with `[]` when empty. (F11)

### OpenAPI `.Produces`

- [ ] `.Produces(404)` added to `GetShoppingCartById` and `GetMovieSessionsById`, replacing the
      former not-found `.Produces(204)`. (F12)
- [ ] `current` declares `.Produces<CreateShoppingCartResponse>(200)`, `.Produces(204)`,
      `.Produces(404)`; stale `.Produces(201)` and `.Produces(409)` removed. (F12)
- [ ] `GetActiveMovieSessionsByMovieId` drops the not-found `.Produces(204)`, keeps `200`. (F12)
- [ ] `GetMovieById` unchanged (already `404`). (F12)
- [ ] `204`-on-success declarations elsewhere are untouched; no `.Produces(404)` added to write
      endpoints. (F15)

### Docs & skills

- [ ] `agent_docs/error_handling.md` mapping table reads `ContentNotFoundException → 404`. (F13)
- [ ] `feature-tests`, `slice-test-red`, `feature-validation`, `feature-requirements`,
      `spec-workflow` skills no longer hard-code `→ 204` for `ContentNotFoundException`. (F14)
- [ ] ADR-002 is **not** flipped to Accepted. (N11)

### Architecture & error model (unchanged invariants)

- [ ] No new `*Exception` or `Error` type; `NotFoundException` and `ContentNotFoundException`
      remain separate (not deduplicated). (N3)
- [ ] No EF Core type or `DbContext` reference appears in `Domain`/`Application`; only handler
      control flow, endpoints, and `CustomExceptionHandler` changed. (N4)
- [ ] No handler sets an HTTP status or touches `HttpContext`; the status flip is central. (N2, N5)
- [ ] Endpoint delegates contain no business logic. (N6)
- [ ] No `catch (Exception)` added; read-only paths still have no `try/catch`.
- [ ] The carve-out handlers remain MediatR `IRequestHandler<,>` with `record` queries. (N1)

### Not applicable for this slice (state the reason in the PR)

- [ ] New use-case folder / command+validator+response — **N/A** (no new use-case; two existing
      query handlers edited).
- [ ] New repository interface / EF Core implementation / DI registration — **N/A** (no new port).
- [ ] EF Core migration — **N/A** (no model change).
- [ ] `WebApplicationFactory<Program>` endpoint integration test — **N/A** (no HTTP harness
      exists; PRD decision — gate is the `CustomExceptionHandler` unit test).

### Quality gates

Run from `src/services`:

```
dotnet format CinemaBookingManagement.sln
dotnet build  CinemaBookingManagement.sln -warnaserror
dotnet test   CinemaBookingManagement.sln
dotnet test --filter "FullyQualifiedName~ContentNotFound404OutsideInTests"
```

All must pass, including:
- `BookingManagementService.Domain.ArchitectureTests`. (N10)
- The new `BookingManagementService.API.UnitTests` project (the gate). (N10)

Account for the accepted AutoMapper `NU1903` NuGet-audit advisory so `-warnaserror` validates
real warnings, not the advisory (MEMORY `dotnet10-migration`). (N9)

No EF Core model change ⇒ **no** `dotnet ef migrations add` step.
