# 0008 · EndpointInputGuards — Validation

## Inputs

- PRD: ./prd.md
- Plan: ./plan.md
- Requirements: ./requirements.md

## Prerequisites

- Service running locally:
  ```
  dotnet run --project BookingManagement/BookingManagementService.API
  ```
  (default port: check `BookingManagement/BookingManagementService.API/Properties/launchSettings.json`;
  examples below use `http://localhost:<port>`).
- Test database provisioned and migrations applied (no schema change in this slice, but the
  app needs a working DB to reach the endpoints):
  ```
  dotnet ef database update \
    -p BookingManagement/BookingManagementService.Infrastructure \
    -s BookingManagement/BookingManagementService.API
  ```
- For the authenticated endpoints (`current`, `assignclient`): a valid Bearer token from
  Keycloak. To exercise the `401` guard you need a token whose **`nameidentifier`
  (`http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier`) claim is not a
  `Guid`, or is absent** — e.g. a hand-crafted dev JWT or a Keycloak user whose subject
  mapping does not emit a Guid. If a dev-auth bypass is configured, use it to set the claim.
- An existing `shoppingCartId` for the `unreserve` / `assignclient` scenarios.

## Manual scenarios

### S1 — Create cart, valid idempotency key (happy path, unchanged)

**Steps:**

1. ```
   curl -s -i -X POST http://localhost:<port>/api/shoppingcarts \
     -H "Content-Type: application/json" \
     -H "Authorization: Bearer <token>" \
     -H "X-Idempotency-Key: 11111111-1111-1111-1111-111111111111" \
     -d '{"maxNumberOfSeats": 4}'
   ```

**Expected:**

- HTTP 201 Created with the `CreateShoppingCartResponse` body and a `Location` header to
  `GetShoppingCartById` (success contract unchanged).

**Covers:** F10.

### S2 — Create cart, malformed idempotency key (was 500 → now 400)

**Steps:**

1. ```
   curl -s -i -X POST http://localhost:<port>/api/shoppingcarts \
     -H "Content-Type: application/json" \
     -H "Authorization: Bearer <token>" \
     -H "X-Idempotency-Key: not-a-guid" \
     -d '{"maxNumberOfSeats": 4}'
   ```
2. Repeat with an **empty** `X-Idempotency-Key:` header value.

**Expected:**

- HTTP 400 Bad Request (not 500).
- Body is `ValidationProblemDetails` (`Content-Type: application/problem+json`) with
  `status: 400` and `type` ending `rfc7231#section-6.5.1`.

**Covers:** F2, F3, F5.

### S3 — Unreserve, malformed idempotency key (was empty 400 → now 400 + ProblemDetails)

**Steps:**

1. ```
   curl -s -i -X DELETE http://localhost:<port>/api/shoppingcarts/<shoppingCartId>/unreserve \
     -H "Authorization: Bearer <token>" \
     -H "X-Idempotency-Key: not-a-guid"
   ```

**Expected:**

- HTTP 400 Bad Request.
- Body is now a `ValidationProblemDetails` JSON body (previously the response was an empty
  `400` from `Results.BadRequest()`), identical in shape to S2.

**Covers:** F4.

### S4 — Unreserve, valid idempotency key (happy path, unchanged)

**Steps:**

1. ```
   curl -s -i -X DELETE http://localhost:<port>/api/shoppingcarts/<shoppingCartId>/unreserve \
     -H "Authorization: Bearer <token>" \
     -H "X-Idempotency-Key: 22222222-2222-2222-2222-222222222222"
   ```

**Expected:**

- The pre-existing success behaviour (`200`/`204`), unchanged.

**Covers:** F10.

### S5 — Current cart, token with non-Guid / missing nameidentifier (was 500 → now 401)

**Steps:**

1. ```
   curl -s -i http://localhost:<port>/api/shoppingcarts/current \
     -H "Authorization: Bearer <token-with-non-guid-nameidentifier>"
   ```
2. Repeat with a token whose `nameidentifier` claim is **absent**.

**Expected:**

- HTTP 401 Unauthorized (not 500).
- Body is `ProblemDetails` with `status: 401`, `title: "Unauthorized"`, `type` ending
  `rfc7235#section-3.1`.

**Covers:** F7, F8, F9.

### S6 — Assign client, token with non-Guid / missing nameidentifier (was 500 → now 401)

**Steps:**

1. ```
   curl -s -i -X PUT http://localhost:<port>/api/shoppingcarts/<shoppingCartId>/assignclient \
     -H "Authorization: Bearer <token-with-non-guid-nameidentifier>"
   ```

**Expected:**

- HTTP 401 Unauthorized (not 500), `ProblemDetails` body as in S5.

**Covers:** F7, F8, F9.

### S7 — Current / assign client, valid token (happy path, unchanged)

**Steps:**

1. ```
   curl -s -i http://localhost:<port>/api/shoppingcarts/current \
     -H "Authorization: Bearer <valid-token-with-guid-nameidentifier>"
   ```

**Expected:**

- HTTP 200 (cart present) or 204 (no active cart) — the `GetClientId` guard returns the Guid
  and the request proceeds as before.

**Covers:** F6, F10.

### S8 — No Authorization header on a protected endpoint (regression)

**Steps:**

1. ```
   curl -s -i http://localhost:<port>/api/shoppingcarts/current
   ```

**Expected:**

- HTTP 401 from `RequireAuthorization()` (rejected before `GetClientId` runs) — unchanged by
  this slice.

**Covers:** regression (auth middleware), not a new requirement.

## Code review checklist

For the reviewer (human or AI) to verify on the PR. Each line is a yes/no question. Reject
the PR until all are yes.

### Slice-specific

- [ ] `ParseIdempotencyKey(string)` exists as an `internal static Guid` in
      `ShoppingCartEndpointApplicationBuilderExtensions`, throws `DomainValidationException`
      on a non-Guid/empty input, and returns the parsed `Guid` otherwise (F1, F2).
- [ ] `CreateShoppingCart` and `UnreserveSeats` both parse `X-Idempotency-Key` via
      `ParseIdempotencyKey`; `UnreserveSeats` no longer contains `return Results.BadRequest()`
      (F3, F4).
- [ ] `GetClientId` is `internal static`, throws `UnauthorizedAccessException` on a
      non-Guid/missing `nameidentifier` claim, and its message uses the raw claim value
      (`id`) — not the always-empty parsed variable, not `nameof(CreateShoppingCartRequest)`
      (F6, F7).
- [ ] No bare `throw new Exception(...)` remains anywhere in
      `ShoppingCartEndpointApplicationBuilderExtensions.cs` (F12).
- [ ] `.Produces(400)` is declared on `CreateShoppingCart` and `UnreserveSeats`;
      `.Produces(401)` on `current` and `AssignUser`; no previously-declared status was
      dropped (F11).
- [ ] The API project grants `InternalsVisibleTo` to `BookingManagementService.API.UnitTests`
      (by assembly name) (F13).

### Error handling

- [ ] Each guard raises a **specific** exception (`DomainValidationException` /
      `UnauthorizedAccessException`), never a bare `Exception`. Per
      `agent_docs/error_handling.md` (N4).
- [ ] Failure statuses are produced by `CustomExceptionHandler` keyed on the exception type;
      no endpoint encodes a failure status. Per `agent_docs/entry_points/minimal-api.md`
      § Status codes (N2).
- [ ] `DomainValidationException` and `UnauthorizedAccessException` are **existing** types with
      **existing** `CustomExceptionHandler` mappings (`400` / `401`); no new exception/`Error`
      type and no new mapping were added (N3).
- [ ] No `catch (Exception)` was introduced; the guards do not log. Per
      `agent_docs/error_handling.md`.

### Architecture

- [ ] No MediatR handler, validator, aggregate, value object, domain event, or repository was
      added or changed; this is an endpoint-edge slice only (N1).
- [ ] The endpoint delegates contain no business logic: bind → parse via guards → build
      command → `sender.Send` → shape result. Per `agent_docs/entry_points/minimal-api.md`
      (N6).
- [ ] No EF Core type or `DbContext` reference appears in `Domain`/`Application`; the API
      throwing the Domain `DomainValidationException` is acceptable (API → Domain) (N5).

### Stable infrastructure

- [ ] `CustomExceptionHandler` is unchanged (its dispatch dictionary and all writers). Per
      `agent_docs/stable_vs_feature.md` (N7).
- [ ] Base types (`AggregateRoot`, `Entity`, `Result`, `Error`), `IEndpoints` /
      `EndpointExtensions`, the MediatR pipeline, `ValidationBehaviour`, and `Program.cs` were
      not changed.
- [ ] The only project-file change is adding the `InternalsVisibleTo` item (a test-enablement
      attribute, not a mechanism change).

### DI and wiring

- [ ] No new DI registration was required (no new repository/service/handler).
- [ ] No new library outside the locked stack (see `CLAUDE.md`) was referenced.

### Tests

- [ ] `EndpointInputGuardsTests` exists in `BookingManagementService.API.UnitTests` and pins
      both guards' thrown types + happy-path returns (F1, F2, F6, F7).
- [ ] `CustomExceptionHandlerInputGuardsTests` exists and pins `DomainValidationException ⇒ 400`
      and `UnauthorizedAccessException ⇒ 401` against a `DefaultHttpContext` (F5, F9).
- [ ] The outside-in acceptance gate is GREEN.
- [ ] **Opt-outs are intentional and stated:** no handler/repository/domain unit test and no
      `WebApplicationFactory` integration test, because no such layer is touched (per
      `plan.md` §6 and `agent_docs/testing.md`).

### Quality gates

Run from `src/services`:

```
dotnet format CinemaBookingManagement.sln
dotnet build  CinemaBookingManagement.sln -warnaserror
dotnet test   CinemaBookingManagement.sln
dotnet test --filter "FullyQualifiedName~EndpointInputGuards"
```

All must pass, including:
- `BookingManagementService.Domain.ArchitectureTests`.
- The slice's guard + mapping tests.

Note (MEMORY `warnaserror-baseline-debt`, `dotnet10-migration`): `-warnaserror` only passes
incrementally against the pre-existing nullable debt and the accepted AutoMapper `NU1903`
advisory; scope the gate to this slice's edits and do not let `dotnet format` rewrite
unrelated files (e.g. `ReserveSeatsCommandValidatorSpecification.cs`).

No EF Core model change ⇒ no migration to run.
