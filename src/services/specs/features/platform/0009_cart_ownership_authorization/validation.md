# 0009 · CartOwnershipAuthorization — Validation

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
- Test database provisioned and migrations applied (no new migration in this slice):
  ```
  dotnet ef database update \
    -p BookingManagement/BookingManagementService.Infrastructure \
    -s BookingManagement/BookingManagementService.API
  ```
- Seed data:
  - an **anonymous** cart `{anonCartId}` — created but never assigned
    (`ClientId == Guid.Empty`); seed seats on it so `purchase` has something to act on;
  - an **assigned** cart `{ownedCartId}` — assigned to user **A** via
    `PUT /api/shoppingcarts/{id}/assignclient` while authenticated as A;
  - a non-existent id `{missingCartId}` (any random Guid not in the DB).
- Two Keycloak bearer tokens:
  - `{tokenA}` — user **A** (the owner of `{ownedCartId}`; the `nameidentifier` claim equals
    that cart's `ClientId`);
  - `{tokenB}` — user **B**, a different authenticated user (the "stranger").

> Note: the strong check is **conditional on assignment**. An anonymous cart accepts any
> caller (guest capability); an assigned cart accepts only its owner. The endpoints stay
> anonymous, so an *unauthenticated* call to an *anonymous* cart still succeeds.

## Manual scenarios

### S1 — `getById`, anonymous cart, no auth ⇒ 200

**Steps:**
```
curl -s -i http://localhost:<port>/api/shoppingcarts/{anonCartId}
```
**Expected:** HTTP 200; body is the `ShoppingCartDto` (`isAssigned: false`). No auth required.
**Covers:** F6, F14, F15.

### S2 — `getById`, owner on own assigned cart ⇒ 200

**Steps:**
```
curl -s -i http://localhost:<port>/api/shoppingcarts/{ownedCartId} \
  -H "Authorization: Bearer {tokenA}"
```
**Expected:** HTTP 200; body is the `ShoppingCartDto` (`isAssigned: true`). Legitimate owner
path unchanged.
**Covers:** F7, F14.

### S3 — `getById`, stranger on someone else's assigned cart ⇒ 403

**Steps:**
```
curl -s -i http://localhost:<port>/api/shoppingcarts/{ownedCartId} \
  -H "Authorization: Bearer {tokenB}"
```
**Expected:** HTTP 403; body is a `ProblemDetails` with `status: 403`, `title: "Forbidden"`,
`type: ".../rfc7231#section-6.5.3"`. No `ShoppingCartDto` is leaked.
**Covers:** F8, F9, F14.

### S4 — `getById`, unauthenticated on an assigned cart ⇒ 403

**Steps:**
```
curl -s -i http://localhost:<port>/api/shoppingcarts/{ownedCartId}
```
**Expected:** HTTP 403 (`ProblemDetails`). An owned cart cannot be read anonymously — note
this is **403, not 401**, because the endpoint is anonymous and the behaviour, not the auth
middleware, rejects it.
**Covers:** F8, F14.

### S5 — `getById`, non-existent cart ⇒ 404 (not 403)

**Steps:**
```
curl -s -i http://localhost:<port>/api/shoppingcarts/{missingCartId} \
  -H "Authorization: Bearer {tokenB}"
```
**Expected:** HTTP 404 (`ProblemDetails`, `ContentNotFoundException`). The authorization layer
does **not** turn a missing cart into a 403; existence is not leaked.
**Covers:** F5, F14.

### S6 — `purchase`, anonymous cart, no auth ⇒ unchanged (200 or its prior business outcome)

**Steps:**
```
curl -s -i -X POST http://localhost:<port>/api/shoppingcarts/{anonCartId}/purchase
```
**Expected:** Same result as before this slice (200 on success, or the existing 404/409 for the
business state) — the guest flow is **not** regressed. (Note: a cart with empty `ClientId`
reaching purchase has its own pre-existing domain behaviour; this slice does not change it.)
**Covers:** F6, F13, F15.

### S7 — `purchase`, owner on own assigned cart ⇒ unchanged success

**Steps:**
```
curl -s -i -X POST http://localhost:<port>/api/shoppingcarts/{ownedCartId}/purchase \
  -H "Authorization: Bearer {tokenA}"
```
**Expected:** HTTP 200 (or the same business outcome as before) — legitimate authenticated
purchase path unchanged.
**Covers:** F7, F13.

### S8 — `purchase`, stranger on someone else's assigned cart ⇒ 403

**Steps:**
```
curl -s -i -X POST http://localhost:<port>/api/shoppingcarts/{ownedCartId}/purchase \
  -H "Authorization: Bearer {tokenB}"
```
**Expected:** HTTP 403 (`ProblemDetails`). The purchase side effects are **not** triggered
(verify no state change on `{ownedCartId}` and its seats).
**Covers:** F8, F9, F13.

### S9 — `purchase`, unauthenticated on an assigned cart ⇒ 403

**Steps:**
```
curl -s -i -X POST http://localhost:<port>/api/shoppingcarts/{ownedCartId}/purchase
```
**Expected:** HTTP 403 (`ProblemDetails`). An owned cart cannot be purchased anonymously.
**Covers:** F8, F13.

### S10 — `purchase`, non-existent cart ⇒ 404 (not 403)

**Steps:**
```
curl -s -i -X POST http://localhost:<port>/api/shoppingcarts/{missingCartId}/purchase \
  -H "Authorization: Bearer {tokenB}"
```
**Expected:** HTTP 404 — the behaviour passes through and the handler returns the existing
not-found result.
**Covers:** F5, F13.

### S11 — OpenAPI declares 403

**Steps:** Open `/swagger` and inspect the `PurchaseSeats` and `GetShoppingCartById`
operations.
**Expected:** Both list `403` among their responses, and retain their previously-declared
statuses (purchase: 200/404/409; getById: 200/404).
**Covers:** F16.

## Code review checklist

Each line is yes/no. Reject the PR until all are yes.

### Architecture

- [ ] `CartOwnershipBehaviour<TRequest, TResponse>` lives in
      `Application/Common/Behaviours/`, implements `IPipelineBehavior<TRequest, TResponse>`,
      and is constrained `where TRequest : ICartScopedRequest`.
- [ ] `ICartScopedRequest` (in `Application/Common/Behaviours/`) exposes only
      `Guid ShoppingCartId`; `PurchaseTicketsCommand` and `GetShoppingCartQuery` implement it
      with **no** other change to their fields, handlers, `Result`, or DTO.
- [ ] `ICurrentUser` is defined in `Application/Abstractions/` and exposes only
      `bool IsAuthenticated` and `Guid ClientId`; it has **no** ASP.NET / `HttpContext` types.
- [ ] The `CurrentUser` implementation lives in the **API** layer
      (`Api.Authentication`), uses `IHttpContextAccessor`, and reads the same
      `nameidentifier` claim string as `GetClientId`.
- [ ] No EF Core type or `DbContext` reference appears in `Domain` or `Application`; the
      behaviour depends only on the `IActiveShoppingCartRepository` interface and
      `ICurrentUser`.
- [ ] The two endpoint delegates contain no authorization logic and gain **only**
      `.Produces(403)`; neither receives `.RequireAuthorization()`.

### Error handling

- [ ] The behaviour signals a breach by **throwing** `ForbiddenAccessException`
      (`Application.Exceptions`) — not by returning a `Result`, not via a bare
      `new Exception(...)`.
- [ ] No new cross-cutting `*Exception`/`Error` type is introduced and **no**
      `CustomExceptionHandler` mapping is added or changed (the existing
      `ForbiddenAccessException → 403` arm is reused).
- [ ] The behaviour invokes `next()` exactly once on each pass-through branch (not-found,
      anonymous, owner) and does **not** invoke `next()` on the forbidden branch.
- [ ] The not-found branch passes through (no 403 for a missing cart); existence is not
      leaked.
- [ ] The behaviour sets no HTTP status and references no `HttpContext`.
- [ ] The behaviour does not log (the central `CustomExceptionHandler` logs the 403).

### Stable infrastructure

- [ ] `CustomExceptionHandler`, the `Result`/`Error` types, base types
      (`AggregateRoot`, `Entity`), `ValidationBehaviour`,
      `IdempotentCommandPipelineBehaviour`, and the `IEndpoints` plumbing are unchanged.
- [ ] `Program.cs` is not edited; the `public partial class Program { }` marker is added in a
      separate new file.
- [ ] The MediatR pipeline **mechanism** is unchanged; only one `AddOpenBehavior` registration
      line is added — sanctioned by ADR-003. No new ADR is required.

### DI and wiring

- [ ] `CartOwnershipBehaviour<,>` is registered in `Application/ConfigureServices.cs` via
      `cfg.AddOpenBehavior(typeof(CartOwnershipBehaviour<,>))`, placed **after**
      `ValidationBehaviour<,>` and **before** `IdempotentCommandPipelineBehaviour<,>`.
- [ ] `API/ConfigureApiServices.cs` registers `services.AddHttpContextAccessor()` and
      `services.AddScoped<ICurrentUser, CurrentUser>()`.
- [ ] No new library outside the locked stack (see `CLAUDE.md`) was referenced.

### Tests

- [ ] The **behaviour unit test** (`CartOwnershipBehaviourTests`, the acceptance gate) exists
      and covers all five branches: anonymous ⇒ pass; assigned+owner ⇒ pass; assigned+other ⇒
      `ForbiddenAccessException` (`next` not invoked); assigned+unauthenticated ⇒
      `ForbiddenAccessException`; not-found ⇒ pass. It is **GREEN** at completion.
- [ ] A **central-mapping test** pins `ForbiddenAccessException ⇒ 403 ProblemDetails` in
      `CustomExceptionHandler` (mirrors slice `0008`'s `DefaultHttpContext` mapping tests).
- [ ] A **`CurrentUserTests`** unit covers valid `nameidentifier` ⇒ authenticated + parsed
      `ClientId`, and missing/non-Guid ⇒ anonymous (`Guid.Empty`, no throw).
- [ ] The **`WebApplicationFactory<Program>`** end-to-end harness (new test project) covers
      `purchase` and `getById`: stranger ⇒ 403, owner ⇒ success, anonymous ⇒ success,
      non-existent ⇒ 404 — with a test authentication handler injecting the `nameidentifier`
      claim.
- [ ] The repository/adapter-translation level is correctly **opted out** (this slice adds no
      infrastructure-exception translation), per `plan.md` §6.
- [ ] Each WAF test resets DB state between runs (respawn/truncate or a transaction per test);
      no test leaves rows in shared state.

### Quality gates

Run from `src/services` (via the PowerShell tool against the `Program Files (x86)\dotnet` SDK;
scope to surface only *new* warnings — MEMORY `warnaserror-baseline-debt`):

```
dotnet format CinemaBookingManagement.sln
dotnet build  CinemaBookingManagement.sln -warnaserror
dotnet test   CinemaBookingManagement.sln
dotnet test --filter "FullyQualifiedName~CartOwnershipBehaviour"
```

All must pass, including:
- `BookingManagementService.Domain.ArchitectureTests` (domain has no dependency on
  application; aggregate roots have a private parameterless ctor; domain events are `sealed`
  and `*DomainEvent`) — and, critically, the Application layer stays framework-free.
- The slice's behaviour acceptance gate.

No EF Core model change in this slice ⇒ **no** `dotnet ef migrations add` / `database update`.
