# 0008 · EndpointInputGuards — Requirements

## Inputs

- PRD: ./prd.md
- Plan: ./plan.md

> Contract note: all HTTP statuses below are derived from the **existing**
> `CustomExceptionHandler` mapping table in `agent_docs/error_handling.md`
> (`DomainValidationException → 400`, `UnauthorizedAccessException → 401`). This slice
> adds **no** mapping and **no** exception/`Error` type; it only makes two endpoint guards
> raise the correct already-mapped exception instead of a bare `Exception` (which maps to
> `500`).

## Functional requirements

- **F1.** `ShoppingCartEndpointApplicationBuilderExtensions.ParseIdempotencyKey(string requestId)`
  is an `internal static Guid` helper that returns the parsed `Guid` when `requestId` is a
  valid `Guid` string.
- **F2.** `ParseIdempotencyKey` throws `DomainValidationException`
  (`CinemaTicketBooking.Domain.Exceptions`) when `requestId` is not a valid `Guid` (including
  the empty/whitespace string), with a message that includes the raw `requestId` value.
- **F3.** `POST /api/shoppingcarts` (`CreateShoppingCart`) parses `X-Idempotency-Key` via
  `ParseIdempotencyKey`, so a malformed header returns HTTP `400 Bad Request` (was `500`)
  and a valid header proceeds to `CreateShoppingCartCommand` unchanged.
- **F4.** `DELETE /api/shoppingcarts/{ShoppingCartId}/unreserve` (`UnreserveSeats`) parses
  `X-Idempotency-Key` via `ParseIdempotencyKey`, so a malformed header returns HTTP `400`
  with a `ValidationProblemDetails` body (was an empty `400` from
  `Results.BadRequest()`) and a valid header proceeds to `UnreserveSeatsCommand` unchanged.
- **F5.** For `DomainValidationException`, `CustomExceptionHandler` sets
  `HttpContext.Response.StatusCode = 400` and writes a `ValidationProblemDetails` body with
  `Status = 400` and `Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1"` (existing
  behaviour, relied upon by F3/F4).
- **F6.** `ShoppingCartEndpointApplicationBuilderExtensions.GetClientId(ClaimsPrincipal user)`
  is an `internal static Guid` helper that returns the parsed `Guid` when the
  `nameidentifier` claim is a valid `Guid` string.
- **F7.** `GetClientId` throws `UnauthorizedAccessException` (`System`) when the
  `nameidentifier` claim is absent or not a valid `Guid`, with a message that includes the
  raw claim value (`id`) and no longer references the always-empty parsed variable or the
  unrelated `CreateShoppingCartRequest` type.
- **F8.** `GET /api/shoppingcarts/current` (`current`) and
  `PUT /api/shoppingcarts/{ShoppingCartId}/assignclient` (`AssignUser`) resolve the client
  id via `GetClientId`, so a non-Guid or missing `nameidentifier` claim returns HTTP
  `401 Unauthorized` (was `500`) and a valid claim proceeds unchanged.
- **F9.** For `UnauthorizedAccessException`, `CustomExceptionHandler` sets
  `HttpContext.Response.StatusCode = 401` and writes a `ProblemDetails` body with
  `Status = 401`, `Title = "Unauthorized"`, and
  `Type = "https://tools.ietf.org/html/rfc7235#section-3.1"` (existing behaviour, relied
  upon by F8).
- **F10.** The success contracts of all four endpoints are unchanged: a valid
  `X-Idempotency-Key` still creates the cart (`CreateShoppingCart`) / unreserves
  (`UnreserveSeats`), and a valid `nameidentifier` claim still resolves the client id
  (`current`, `assignclient`).
- **F11.** The OpenAPI `.Produces(...)` declarations are corrected: `.Produces(400)` is
  declared on `CreateShoppingCart` and `UnreserveSeats`; `.Produces(401)` is declared on
  `current` and `AssignUser`; all previously-declared status codes on those endpoints are
  retained.
- **F12.** No bare `throw new Exception(...)` remains in
  `ShoppingCartEndpointApplicationBuilderExtensions.cs`.
- **F13.** The `BookingManagementService.API` assembly grants `InternalsVisibleTo` to the
  `BookingManagementService.API.UnitTests` assembly (by assembly name), so the two
  `internal static` guards are unit-testable.

## Non-functional requirements

- **N1.** No MediatR use-case is added or modified; the existing `CreateShoppingCart`,
  `UnreserveSeats`, `current`, and `assignclient` use-cases keep their command/query and
  handler shapes. Per `agent_docs/architecture.md`.
- **N2.** Failure statuses come from the single central translation point
  (`CustomExceptionHandler`) keyed on the exception type; no endpoint encodes a failure
  status (the `Results.BadRequest()` in `UnreserveSeats` is removed). Per `CLAUDE.md`
  rule 5, `agent_docs/entry_points/minimal-api.md` § Status codes, and
  `agent_docs/error_handling.md`.
- **N3.** No new cross-cutting `*Exception` or `Error` type is introduced and no new
  `CustomExceptionHandler` mapping is added; only the existing `DomainValidationException`
  and `UnauthorizedAccessException` mappings are reused. Per `CLAUDE.md` § Forbidden and
  `agent_docs/error_handling.md`.
- **N4.** No bare `Exception` is thrown in new/edited code; each guard raises a **specific**
  exception. Per `agent_docs/error_handling.md` § Checklist.
- **N5.** `Domain` and `Application` contain no new EF Core types or `DbContext` references;
  this slice edits only the API endpoint class and the API project file. Per
  `agent_docs/architecture.md` (Dependency Rule). The API throwing the Domain
  `DomainValidationException` is permitted (API depends inward on Domain).
- **N6.** The endpoint delegates contain no business logic: they bind the request, parse the
  transport/auth inputs via the guards, build the command, call `ISender.Send`, and shape
  the HTTP result. Per `agent_docs/entry_points/minimal-api.md`.
- **N7.** The `CustomExceptionHandler` mechanism (the `FrozenDictionary` dispatch, the
  `IExceptionHandler` contract, and every writer) is unchanged. Per
  `agent_docs/stable_vs_feature.md`.
- **N8.** All I/O remains `async`/`await` with `CancellationToken` threaded through; no
  synchronous database or I/O call is introduced (the guards are pure, synchronous parsing).
  Per `CLAUDE.md`.
- **N9.** The build produces no new warnings (`dotnet build -warnaserror`), accounting for
  the accepted AutoMapper `NU1903` NuGet-audit advisory. Per `CLAUDE.md` § Verifying changes
  and MEMORY `dotnet10-migration` / `warnaserror-baseline-debt`.
- **N10.** Architecture tests (`BookingManagementService.Domain.ArchitectureTests`) and the
  full `dotnet test` suite pass without new failures. Per `CLAUDE.md` § Verifying changes.
- **N11.** ADR-002 stays **Accepted**; this slice (its endpoint-helper tail) does not change
  the ADR. Per the PRD.

## Out of scope

- Any MediatR handler, aggregate, value object, repository, or domain change.
- The composition-root guard `throw new Exception("identityOptions is null")` in
  `ConfigureApiServices` — a config-time startup check, not a request path.
- The query-side handlers that throw `ContentNotFoundException` — intentional exception
  tails documented in `agent_docs/error_handling.md`.
- The Flutter client follow-up to `0002`'s `204 → 404`; any client effect of this slice's
  new `400`/`401`.
- Any change to `CustomExceptionHandler`, the MediatR pipeline, `ValidationBehaviour`,
  `ErrorResults`, the `IEndpoints` plumbing, or any base type.
- Adding a new exception type, `Error` kind, mapper arm, or status code.
- Re-examining the `.Produces(204)`/success contracts beyond adding the new failure-status
  declarations.
- A `WebApplicationFactory` HTTP integration harness; schema changes / EF Core migrations.

## Traceability

| Requirement | Verified by |
|---|---|
| F1 | `EndpointInputGuardsTests` unit test (valid key ⇒ Guid) |
| F2 | `EndpointInputGuardsTests` unit test (malformed/empty ⇒ DomainValidationException) |
| F3 | derived from F2 + F5 (central mapping); code review checklist (CreateShoppingCart uses helper) |
| F4 | derived from F2 + F5 (central mapping); code review checklist (UnreserveSeats uses helper, no Results.BadRequest) |
| F5 | `CustomExceptionHandlerInputGuardsTests` (DomainValidationException ⇒ 400 ValidationProblemDetails) |
| F6 | `EndpointInputGuardsTests` unit test (valid claim ⇒ Guid) |
| F7 | `EndpointInputGuardsTests` unit test (non-Guid/missing claim ⇒ UnauthorizedAccessException); code review checklist (message fix) |
| F8 | derived from F7 + F9 (central mapping); code review checklist (current/assignclient use GetClientId) |
| F9 | `CustomExceptionHandlerInputGuardsTests` (UnauthorizedAccessException ⇒ 401 ProblemDetails) |
| F10 | code review checklist (success branches unchanged); existing handler/command coverage |
| F11 | code review checklist (.Produces declarations) |
| F12 | code review checklist (no bare Exception in the endpoint file) |
| F13 | compilation of `EndpointInputGuardsTests` against the internal guards |
| N1–N11 | code review checklist in validation.md + architecture tests + full suite |
