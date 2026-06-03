# 0002 · ContentNotFound404 — Requirements

## Inputs

- PRD: ./prd.md
- Plan: ./plan.md

> Contract note: the `CustomExceptionHandler` mapping table in
> `agent_docs/error_handling.md` currently reads `ContentNotFoundException → 204`; **this slice
> changes it to `→ 404`** (ADR-002 step 2). All HTTP statuses below are derived from the
> **post-change** mapping, which this slice also writes into `error_handling.md` (F13). The
> stale `→ 204` guidance still present in the spec-chain skills is a known-stale input being
> corrected here (F14), not a source of truth.

## Functional requirements

- **F1.** `CustomExceptionHandler.TryHandleAsync` maps `ContentNotFoundException` to HTTP
  `404 Not Found` (was `204 No Content`), setting `HttpContext.Response.StatusCode = 404`.
- **F2.** For `ContentNotFoundException`, `CustomExceptionHandler` writes a `ProblemDetails`
  body with `Status = 404`, `Type = "https://tools.ietf.org/html/rfc7231#section-6.5.4"`,
  `Title = "The specified resource was not found."`, and `Detail = exception.Message`.
- **F3.** The `404` `ProblemDetails` produced for `ContentNotFoundException` is identical in
  shape (same `Status`, `Type`, `Title`, `Detail` semantics) to the one produced for
  `NotFoundException`.
- **F4.** `CustomExceptionHandler` continues to map `NotFoundException` to HTTP `404` with its
  existing `ProblemDetails` body (regression: unchanged).
- **F5.** `CustomExceptionHandler` logs the `ContentNotFoundException` at `Warning` level (log
  behaviour preserved; only status/body change).
- **F6.** Every addressed-resource read path that throws `ContentNotFoundException` — `GET
  /api/movies/{movieId}`, `GET /api/moviesessions/{movieSessionId}`, `GET
  /api/shoppingcarts/{ShoppingCartId}` — returns `404` after the central change, with no
  per-handler edit.
- **F7.** `GetCurrentShoppingCartQueryHandler` returns `null` (a `CreateShoppingCartResponse?`)
  without throwing when the customer has no active cart (repository returns `Guid.Empty`).
- **F8.** `GetCurrentShoppingCartQueryHandler` still throws `ContentNotFoundException` (⇒ HTTP
  `404`) when an active-cart id exists but `GetByIdAsync` returns `null` (the inconsistent case).
- **F9.** The `GET /api/shoppingcarts/current` endpoint returns `204 No Content` when the query
  result is `null`, and `200 OK` with the `CreateShoppingCartResponse` body when it is non-null.
- **F10.** `GetMovieSessionsQueryHandler` returns an empty collection without throwing when the
  repository yields no upcoming sessions, and the projected collection when sessions exist.
- **F11.** The `GET /api/movies/{movieId}/moviesessions` endpoint returns `200 OK` with `[]`
  when there are no upcoming sessions (no `204`, no `404` for emptiness).
- **F12.** The OpenAPI `.Produces(...)` declarations are corrected: `.Produces(404)` is declared
  on `GET /api/shoppingcarts/{ShoppingCartId}` and `GET /api/moviesessions/{movieSessionId}`
  (replacing the former not-found `.Produces(204)`); `GET /api/shoppingcarts/current` declares
  `.Produces<CreateShoppingCartResponse>(200)`, `.Produces(204)`, `.Produces(404)` (and drops
  the stale `.Produces(201)` / `.Produces(409)`); `GET /api/movies/{movieId}/moviesessions`
  drops its not-found `.Produces(204)` and keeps `.Produces(200)`; `GET /api/movies/{movieId}`
  is unchanged (already declares `.Produces(404)`).
- **F13.** `agent_docs/error_handling.md`'s `CustomExceptionHandler` mapping table states
  `ContentNotFoundException → 404`.
- **F14.** The spec-chain skills `feature-tests`, `slice-test-red`, `feature-validation`,
  `feature-requirements`, and `spec-workflow` no longer hard-code `ContentNotFoundException →
  204` / "`204`, not `404`"; they state `→ 404`.
- **F15.** `.Produces(204)` declarations where `204` legitimately means "no body on success"
  (create cart, select/unselect/reserve/purchase, `activemovies`, `seats`) are left unchanged;
  no `.Produces(404)` is added to write endpoints in this slice.

## Non-functional requirements

- **N1.** The carve-out use-cases remain MediatR `IRequestHandler<TQuery, TResult>` with the
  query a `record` implementing `IRequest<TResult>`; no use-case is converted to another style.
  Per `agent_docs/architecture.md`.
- **N2.** The status-code flip happens at the single central translation point
  (`CustomExceptionHandler`); no use-case sets `HttpContext.Response.StatusCode` and no new
  HTTP status is encoded in a handler. Per `CLAUDE.md` rule 5 and `agent_docs/error_handling.md`.
- **N3.** No new cross-cutting `*Exception` or `Error` type is introduced; the existing
  `ContentNotFoundException` and `NotFoundException` are left un-deduplicated. Per `CLAUDE.md`
  § Forbidden and `agent_docs/error_handling.md`.
- **N4.** `Domain` and `Application` contain no EF Core types or `DbContext` references; the
  change touches only `Application` query handlers (control flow), the API endpoints, and the
  API `CustomExceptionHandler`. Per `agent_docs/architecture.md` (Dependency Rule).
- **N5.** No handler raises an HTTP-transport exception or writes to `HttpContext`; HTTP
  translation stays in `CustomExceptionHandler`. Per `CLAUDE.md` rule 5.
- **N6.** The endpoint delegates contain no business logic: they bind the request, build the
  query, call `ISender.Send`, and shape the HTTP result (including the `null ⇒ 204` mapping).
  Per `agent_docs/entry_points/minimal-api.md`.
- **N7.** The `CustomExceptionHandler` mechanism (the `FrozenDictionary` dispatch, the
  `IExceptionHandler` contract, and every other writer) is unchanged; only the
  `ContentNotFoundException` writer body changes. Per `agent_docs/stable_vs_feature.md`.
- **N8.** All I/O remains `async`/`await` with `CancellationToken` threaded through; no
  synchronous database or I/O call is introduced. Per `CLAUDE.md`.
- **N9.** The build produces no new warnings (`dotnet build -warnaserror`), accounting for the
  accepted AutoMapper `NU1903` NuGet-audit advisory. Per `CLAUDE.md` § Verifying changes and
  MEMORY `dotnet10-migration`.
- **N10.** Architecture tests (`BookingManagementService.Domain.ArchitectureTests`) and the full
  `dotnet test` suite pass without new failures. Per `CLAUDE.md` § Verifying changes.
- **N11.** ADR-002 remains **Proposed**; this slice does not flip it to Accepted. Per the PRD.

## Out of scope

- Removing the `assignclient` `Result → exception` bridge or converting any endpoint to
  `Match`-to-HTTP (ADR step 3). Its not-found path changing `204 → 404` is an accepted side
  effect.
- Adopting `Result<T>` in any handler (ADR step 3).
- Deduplicating `NotFoundException` and `ContentNotFoundException`.
- Replacing bare `throw new Exception(...)` in the domain/handlers (ADR defect #2).
- Updating the Flutter client to treat the by-id cart not-found as `404` (separate tracked task).
- Flipping ADR-002 to Accepted.
- Changing the `CustomExceptionHandler` mechanism, the MediatR pipeline, the validation
  behaviour, or any base type.
- Changing the `TimeProvider.System` "upcoming" filter in `GetMovieSessionsQueryHandler` (only
  the throw-on-empty is removed).

## Traceability

| Requirement | Verified by |
|---|---|
| F1 | `CustomExceptionHandler` outside-in gate test (status 404) |
| F2 | `CustomExceptionHandler` outside-in gate test (ProblemDetails body) |
| F3 | `CustomExceptionHandler` outside-in gate test (shape parity with NotFoundException) |
| F4 | `CustomExceptionHandler` outside-in gate test (NotFoundException regression) |
| F5 | code review checklist (validation.md); gate test asserts status/body, not log |
| F6 | derived from F1 (central mapping) + code review checklist |
| F7 | `GetCurrentShoppingCartQueryHandler` unit test (no-active-cart ⇒ null) |
| F8 | `GetCurrentShoppingCartQueryHandler` unit test (inconsistent ⇒ ContentNotFoundException) |
| F9 | code review checklist (endpoint null ⇒ 204 / non-null ⇒ 200); manual scenario in validation.md |
| F10 | `GetMovieSessionsQueryHandler` unit test (empty ⇒ empty collection; present ⇒ mapped) |
| F11 | code review checklist + manual scenario in validation.md |
| F12 | code review checklist (.Produces declarations) |
| F13 | code review checklist (error_handling.md table) |
| F14 | code review checklist (skill text) |
| F15 | code review checklist (.Produces left untouched) |
| N1–N11 | code review checklist in validation.md + architecture tests + full suite |
