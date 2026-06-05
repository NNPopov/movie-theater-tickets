# 0009 · CartOwnershipAuthorization — Requirements

## Inputs

- PRD: ./prd.md
- Plan: ./plan.md

> Contract note: the `403` status below is derived from the **existing**
> `CustomExceptionHandler` mapping table in `agent_docs/error_handling.md`
> (`ForbiddenAccessException → 403`). This slice adds **no** mapping and **no**
> exception/`Error` type; it introduces a pipeline behaviour that **throws** the already-mapped
> `ForbiddenAccessException`. ADR-003 sanctions the behaviour as its preferred remediation
> (option #1); registering it is a one-line feature addition, not a stable-mechanism change.

## Functional requirements

- **F1.** `CartOwnershipBehaviour<TRequest, TResponse>`
  (`CinemaTicketBooking.Application.Common.Behaviours`) is a MediatR
  `IPipelineBehavior<TRequest, TResponse>` constrained `where TRequest : ICartScopedRequest`,
  so it executes **only** for requests implementing that marker.
- **F2.** `ICartScopedRequest`
  (`CinemaTicketBooking.Application.Common.Behaviours`) exposes a single `Guid ShoppingCartId`
  property.
- **F3.** `PurchaseTicketsCommand` and `GetShoppingCartQuery` each implement
  `ICartScopedRequest` (opt-in only — their existing positional `ShoppingCartId` satisfies the
  property; no field, handler, `Result`, or DTO contract changes).
- **F4.** The behaviour loads the cart via
  `IActiveShoppingCartRepository.GetByIdAsync(request.ShoppingCartId)` before deciding, and
  invokes `next()` exactly once on every pass-through branch.
- **F5.** When the cart is `null` (not found), the behaviour invokes `next()` (pass-through),
  so the downstream handler still produces its existing `404` and existence is not leaked as a
  `403`.
- **F6.** When the cart's `ClientId == Guid.Empty` (anonymous cart), the behaviour invokes
  `next()` (pass-through), preserving the guest capability model.
- **F7.** When the cart's `ClientId != Guid.Empty` (assigned cart) and
  `ICurrentUser.IsAuthenticated` is `true` and `ICurrentUser.ClientId == cart.ClientId`, the
  behaviour invokes `next()` (the legitimate owner path is unchanged).
- **F8.** When the cart's `ClientId != Guid.Empty` and either `ICurrentUser.IsAuthenticated`
  is `false` **or** `ICurrentUser.ClientId != cart.ClientId`, the behaviour throws
  `ForbiddenAccessException` (`CinemaTicketBooking.Application.Exceptions`) and does **not**
  invoke `next()`.
- **F9.** For `ForbiddenAccessException`, `CustomExceptionHandler` sets
  `HttpContext.Response.StatusCode = 403` and writes a `ProblemDetails` body with
  `Status = 403`, `Title = "Forbidden"`, and
  `Type = "https://tools.ietf.org/html/rfc7231#section-6.5.3"` (existing behaviour, relied
  upon by F8).
- **F10.** `ICurrentUser` (`CinemaTicketBooking.Application.Abstractions`) exposes
  `bool IsAuthenticated` and `Guid ClientId`, with `ClientId == Guid.Empty` when the caller is
  not authenticated.
- **F11.** The `CurrentUser` implementation (`CinemaTicketBooking.Api.Authentication`, over
  `IHttpContextAccessor`) reads the `nameidentifier` claim
  (`"http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier"`, the **same**
  claim as the endpoints' `GetClientId`); a valid `Guid` claim ⇒ `IsAuthenticated == true`
  and `ClientId` parsed from it.
- **F12.** `CurrentUser` returns `IsAuthenticated == false` and `ClientId == Guid.Empty` when
  the `nameidentifier` claim is absent or not a valid `Guid` (it does **not** throw — unlike
  `GetClientId`, the conditional check decides `403`).
- **F13.** `POST /api/shoppingcarts/{ShoppingCartId}/purchase` returns HTTP `403` with a
  `ProblemDetails` body when called against an **assigned** cart by a non-owner or
  unauthenticated caller; its `200` / `404` / `409` outcomes are otherwise unchanged.
- **F14.** `GET /api/shoppingcarts/{ShoppingCartId}` returns HTTP `403` with a `ProblemDetails`
  body when called against an **assigned** cart by a non-owner or unauthenticated caller; its
  `200` (`ShoppingCartDto`) and `404` outcomes are otherwise unchanged.
- **F15.** Both endpoints remain anonymous — no `.RequireAuthorization()` is added — so guest
  flows on anonymous carts and the legitimate owner path keep working.
- **F16.** The OpenAPI `.Produces(...)` declarations of the `PurchaseSeats` and
  `GetShoppingCartById` endpoints gain `.Produces(403)`; all previously-declared status codes
  are retained.
- **F17.** `CartOwnershipBehaviour<,>` is registered as an open-generic MediatR behaviour in
  `Application/ConfigureServices.cs` **after** `ValidationBehaviour<,>` and **before**
  `IdempotentCommandPipelineBehaviour<,>` (authorization precedes any idempotency record
  creation).
- **F18.** `ICurrentUser` is registered to `CurrentUser` and `IHttpContextAccessor` is
  registered, in `API/ConfigureApiServices.cs` (`AddApiServices`).

## Non-functional requirements

- **N1.** The new authorization unit is a MediatR `IPipelineBehavior<,>` (a cross-cutting
  guard parallel to `ValidationBehaviour<,>`), not a use-case handler; no command/query
  use-case is added. Per `agent_docs/architecture.md` and `CLAUDE.md` rule 3.
- **N2.** The `ICurrentUser` port is defined in `Application/Abstractions/` and implemented in
  the API composition root; `Application` gains no framework dependency (no `HttpContext`, no
  ASP.NET types). Per `agent_docs/architecture.md` (Dependency Rule) and `CLAUDE.md` rule 2.
- **N3.** No new cross-cutting `*Exception` or `Error` type is introduced and no new
  `CustomExceptionHandler` mapping is added; only the existing `ForbiddenAccessException → 403`
  mapping is reused. Per `CLAUDE.md` § Forbidden and `agent_docs/error_handling.md`.
- **N4.** The authorization breach is signalled by **throwing** (not by a `Result`), matching
  the ADR-002 split for cross-cutting guards (the "unexpected"/guard half) and composing
  uniformly over the command (`Result`) and the query (`ShoppingCart`). Per
  `agent_docs/error_handling.md` and `CLAUDE.md` rule 9.
- **N5.** The behaviour sets no HTTP status and touches no `HttpContext`; HTTP translation
  happens once, in `CustomExceptionHandler`. Per `CLAUDE.md` rule 5.
- **N6.** The endpoint delegates contain no business/authorization logic and stay anonymous;
  the only endpoint change is the additive `.Produces(403)` OpenAPI metadata. Per
  `agent_docs/entry_points/minimal-api.md`.
- **N7.** The `CustomExceptionHandler` mechanism, the `Result`/`Error` types, the
  `ValidationBehaviour`/`IdempotentCommandPipelineBehaviour` mechanisms, and every base type
  are unchanged; the only stable-file edits are registration lines. Per
  `agent_docs/stable_vs_feature.md`.
- **N8.** All I/O remains `async`/`await`; the behaviour awaits `GetByIdAsync` and introduces
  no synchronous database or I/O call (and does not alter the `GetByIdAsync` signature). Per
  `CLAUDE.md`.
- **N9.** The build produces no new warnings (`dotnet build -warnaserror`), accounting for the
  accepted AutoMapper `NU1903` NuGet-audit advisory. Per `CLAUDE.md` § Verifying changes and
  MEMORY `dotnet10-migration` / `warnaserror-baseline-debt`.
- **N10.** Architecture tests (`BookingManagementService.Domain.ArchitectureTests`) and the
  full `dotnet test` suite pass without new failures — in particular the Application layer
  stays framework-free. Per `CLAUDE.md` § Verifying changes.
- **N11.** ADR-003 stays **Proposed**; this slice implements its first remediation step and
  does not flip the ADR. Per the PRD.

## Out of scope

- Applying the mechanism to any cart-scoped operation other than `purchase` and `getById`
  (`select`/`unselect`/`reserve`/`unreserve` and the SignalR hub methods — later ADR-003
  slices).
- Hardening `assignclient` against adopting a stranger's anonymous cart.
- Validating the `HashId` for the anonymous capability phase; logging hygiene / raw-cart-id
  redaction.
- The final `403`-vs-`404` disclosure-policy decision (this slice keeps not-found ⇒ `404`).
- Adding `.RequireAuthorization()` to the two endpoints, or otherwise closing the guest flow.
- Any change to `Result`/`Result<T>`, `CustomExceptionHandler`, the validation/idempotency
  behaviours, or any base/exception/`Error` type. No new cross-cutting type.
- Adding a `CancellationToken` parameter to `IActiveShoppingCartRepository.GetByIdAsync`.
- Flipping ADR-003 to Accepted.

## Traceability

| Requirement | Verified by |
|---|---|
| F1 | `CartOwnershipBehaviourTests` (constraint compiles; behaviour resolves only for `ICartScopedRequest`) + code review |
| F2 | compilation of `CartOwnershipBehaviour` against `ICartScopedRequest`; code review |
| F3 | compilation (`PurchaseTicketsCommand`/`GetShoppingCartQuery` implement the marker); code review |
| F4 | `CartOwnershipBehaviourTests` (repository `GetByIdAsync` invoked; `next` invoked once per pass-through) |
| F5 | `CartOwnershipBehaviourTests` (cart not found ⇒ `next` invoked) + WAF (non-existent cart ⇒ 404) |
| F6 | `CartOwnershipBehaviourTests` (anonymous cart ⇒ `next` invoked) + WAF (anonymous cart ⇒ success) |
| F7 | `CartOwnershipBehaviourTests` (assigned + owner ⇒ `next` invoked) + WAF (owner ⇒ success) |
| F8 | `CartOwnershipBehaviourTests` (assigned + other / unauthenticated ⇒ `ForbiddenAccessException`, `next` not invoked) |
| F9 | `CustomExceptionHandler` mapping test (`ForbiddenAccessException` ⇒ 403 `ProblemDetails`) |
| F10 | `CurrentUserTests` (interface surface) + `CartOwnershipBehaviourTests` (consumes the abstraction) |
| F11 | `CurrentUserTests` (valid `nameidentifier` ⇒ authenticated, `ClientId` parsed) |
| F12 | `CurrentUserTests` (missing/non-Guid claim ⇒ anonymous, `Guid.Empty`) |
| F13 | WAF end-to-end (`purchase`: stranger ⇒ 403; owner ⇒ success; anonymous ⇒ success; missing ⇒ 404) |
| F14 | WAF end-to-end (`getById`: stranger ⇒ 403; owner ⇒ success; anonymous ⇒ success; missing ⇒ 404) |
| F15 | code review checklist (no `.RequireAuthorization()` on the two endpoints) + WAF (anonymous/guest paths) |
| F16 | code review checklist (`.Produces(403)` added, prior statuses retained) |
| F17 | code review checklist (registration order in `ConfigureServices.cs`) + WAF (pipeline actually enforces) |
| F18 | code review checklist (`AddHttpContextAccessor` + `AddScoped<ICurrentUser, CurrentUser>`) + WAF resolves `ICurrentUser` |
| N1–N11 | code review checklist in validation.md + architecture tests + full suite |
