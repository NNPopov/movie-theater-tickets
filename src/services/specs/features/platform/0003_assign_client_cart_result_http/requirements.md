# 0003 · AssignClientCart Result→HTTP — Requirements

## Inputs

- PRD: ./prd.md
- Plan: ./plan.md

> Contract note: HTTP statuses below are derived from the `CustomExceptionHandler` mapping table
> in `agent_docs/error_handling.md` (post-`0002`: `ContentNotFoundException`/`NotFoundException →
> 404`, `ConflictException → 409`, anything else `→ 500`). This slice does **not** change that
> table; it introduces a `Result`-side mapper whose `ProblemDetails` shapes **mirror** those
> writers so the two error models are indistinguishable to clients.

## Functional requirements

- **F1.** The endpoint `PUT /api/shoppingcarts/{ShoppingCartId}/assignclient` resolves the
  handler's `Result` with `result.Match(() => Results.Ok(), ErrorResults.ToProblem)`, returning
  HTTP `200 OK` on success and an `IResult` from the shared mapper on failure — the failure branch
  never throws.
- **F2.** The `assignclient` endpoint's `Result → exception` bridge is removed: the
  `if (failure is ConflictError) throw new ConflictException(...)` and
  `if (failure is NotFoundError) throw new ContentNotFoundException(...)` re-throws are deleted.
- **F3.** The bare `throw new Exception(failure.Description)` in the `assignclient` endpoint's
  failure branch is removed.
- **F4.** A new shared mapper `ErrorResults.ToProblem(Error)` (in `API/Endpoints/Common/`) maps
  `NotFoundError` to an `IResult` producing HTTP `404 Not Found` with a `ProblemDetails` body:
  `Status = 404`, `Type = "https://tools.ietf.org/html/rfc7231#section-6.5.4"`,
  `Title = "The specified resource was not found."`, `Detail = error.Description`.
- **F5.** `ErrorResults.ToProblem(Error)` maps `ConflictError` to an `IResult` producing HTTP
  `409 Conflict` with a `ProblemDetails` body: `Status = 409`,
  `Type = "https://tools.ietf.org/html/rfc7231#section-6.5.8"`, `Title = "Conflict"`, and **no**
  `Detail` (mirroring `HandleConflictException`).
- **F6.** `ErrorResults.ToProblem(Error)` maps any unrecognised `Error` kind to an `IResult`
  producing HTTP `500 Internal Server Error` with a `ProblemDetails` body: `Status = 500`,
  `Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1"`, `Title = "Internal Server Error"`
  (mirroring `HandleException`; preserves the former bare-throw-to-500 behaviour).
- **F7.** The `ProblemDetails` shapes emitted by `ErrorResults` for `404`/`409`/`500` are
  identical in shape to the bodies `CustomExceptionHandler` emits for the same statuses, so a
  `404`/`409` looks the same to a client whether produced by an exception or a matched `Result`.
- **F8.** The `AssignClientCartCommandHandler` returns `NotFoundError`
  (`DomainErrors<AssignClientCartCommandHandler>.NotFound`) when the target cart does not exist
  (`GetByIdAsync` returns `null`), surfacing as HTTP `404` via the mapper.
- **F9.** The `AssignClientCartCommandHandler` returns `ConflictError`
  (`DomainErrors<AssignClientCartCommandHandler>.ConflictException`) when the signed-in client
  already owns a *different* active cart, surfacing as HTTP `409` via the mapper.
- **F10.** The `AssignClientCartCommandHandler` propagates a failing `Result` from
  `cart.AssignClientId(...)` via its now-live `if (result.IsFailure) return result;` branch
  (no longer dead code), so a domain `ConflictError` flows through the same `Match`-to-HTTP path.
- **F11.** On success the `AssignClientCartCommandHandler` calls
  `cart.AssignClientId(request.ClientId)` (the signed-in client id), not
  `request.ShoppingCartId`, so the cart records the client as its owner (bug fix).
- **F12.** `ShoppingCart.AssignClientId(Guid clientId)` returns a `ConflictError`
  (`DomainErrors<ShoppingCart>.ConflictException`) instead of throwing `ConflictException` when
  the cart already has an owner (`ClientId != Guid.Empty`).
- **F13.** `ShoppingCart.AssignClientId` appends `ShoppingCartAssignedToClientDomainEvent` **only**
  on the success branch (after the conflict guard), so no event is raised on the already-assigned
  case.
- **F14.** `ShoppingCart.AssignClientId` keeps `Ensure.NotEmpty(clientId, ...)` as a thrown
  structural guard (an empty client id is a bug, not a business outcome) and assigns
  `ClientId = clientId` on success.
- **F15.** The `assignclient` endpoint's OpenAPI metadata declares `.Produces(200)`,
  `.Produces(404)`, `.Produces(409)` and drops the stale `.Produces(201)` and `.Produces(204)`.
- **F16.** The endpoint retains `.WithName("AssignUser")`, `.WithTags(Tag)`, and
  `.RequireAuthorization()`; the client id is read from `ClaimsPrincipal` via `GetClientId(user)`.

## Non-functional requirements

- **N1.** The converted use-case remains a MediatR `IRequestHandler<AssignClientCartCommand,
  Result>` with the command a `record` implementing `IRequest<Result>`; it is not converted to
  another style. Per `agent_docs/architecture.md`.
- **N2.** No use-case sets `HttpContext.Response.StatusCode`; the success status comes from the
  endpoint (`Results.Ok`) and every failure status comes from the shared mapper's `IResult`. Per
  `CLAUDE.md` rule 5 and `agent_docs/error_handling.md`.
- **N3.** No new cross-cutting `*Exception` or `Error` type is introduced; the mapper consumes
  the existing `NotFoundError`/`ConflictError`/`Error` types from `Domain/Error`. Per `CLAUDE.md`
  § Forbidden.
- **N4.** `Domain` and `Application` contain no EF Core types, `DbContext`, ASP.NET, or
  `IResult`/`HttpContext` references; the `Error → IResult` mapping lives only in the `API`
  layer. Per `agent_docs/architecture.md` (Dependency Rule).
- **N5.** No handler raises an HTTP-transport exception or writes to `HttpContext`; the aggregate
  expresses the business conflict as a `Result`, and HTTP shaping happens only in the API layer.
  Per `CLAUDE.md` rule 5.
- **N6.** The endpoint delegate contains no business logic: it reads identity, builds the command,
  calls `ISender.Send`, and shapes the HTTP result via `Match`. Per
  `agent_docs/entry_points/minimal-api.md`.
- **N7.** The `CustomExceptionHandler` mechanism and all its writers are unchanged; the shared
  mapper is a new sibling policy object, not a modification of the exception handler. Per
  `agent_docs/stable_vs_feature.md`.
- **N8.** All I/O remains `async`/`await` with `CancellationToken` threaded through; no
  synchronous database or I/O call is introduced. Per `CLAUDE.md`.
- **N9.** The build produces no new warnings (`dotnet build -warnaserror`), accounting for the
  accepted AutoMapper `NU1903` NuGet-audit advisory. Per `CLAUDE.md` § Verifying changes and
  MEMORY `dotnet10-migration`.
- **N10.** Architecture tests (`BookingManagementService.Domain.ArchitectureTests`) and the full
  `dotnet test` suite pass without new failures. Per `CLAUDE.md` § Verifying changes.
- **N11.** ADR-002 remains **Proposed**; this slice does not flip it to Accepted. Per the PRD.
- **N12.** `AssignClientCart` is the **only** converted use-case in this slice; `ReserveTickets`,
  `PurchaseTickets`, and `SelectSeats` are left unchanged for later slices. Per the PRD.

## Out of scope

- Converting any use-case other than `AssignClientCart` (`ReserveTickets`, `PurchaseTickets`,
  `SelectSeats` — later slices that reuse this slice's mapper).
- Adopting `Result<T>` (the generic) in any handler.
- Deduplicating `NotFoundException` and `ContentNotFoundException`, or relocating the misplaced
  `ContentNotFoundException` file.
- Replacing the bare `throw new Exception(...)` in `CreateMovieSessionCommandHandler`,
  `ReserveTicketsCommandHandler`, `MovieSessionSeatService`, or the `GetClientId` endpoint helper.
- Standing up a `WebApplicationFactory` HTTP integration harness.
- Changing the `CustomExceptionHandler` mechanism, the MediatR pipeline, the validation
  behaviour, or any base type.
- Flipping ADR-002 to Accepted.

## Traceability

| Requirement | Verified by |
|---|---|
| F1 | code review checklist (validation.md); endpoint `Match` wiring verified by compilation + mapper gate |
| F2 | code review checklist (bridge deleted) |
| F3 | code review checklist (bare throw deleted) |
| F4 | `ErrorResults` outside-in gate test (`NotFoundError ⇒ 404` + ProblemDetails) |
| F5 | `ErrorResults` outside-in gate test (`ConflictError ⇒ 409` + ProblemDetails, no Detail) |
| F6 | `ErrorResults` outside-in gate test (unrecognised `Error ⇒ 500` + ProblemDetails) |
| F7 | `ErrorResults` outside-in gate test (shape parity with `CustomExceptionHandler`) + code review |
| F8 | `AssignClientCartCommandHandler` unit test (cart missing ⇒ `NotFoundError`) |
| F9 | `AssignClientCartCommandHandler` unit test (other active cart ⇒ `ConflictError`) |
| F10 | `AssignClientCartCommandHandler` unit test (domain `IsFailure` propagated) |
| F11 | `AssignClientCartCommandHandler` unit test (success ⇒ owner equals client id — bug fix) |
| F12 | `ShoppingCart.AssignClientId` domain unit test (already-assigned ⇒ `ConflictError`) |
| F13 | `ShoppingCart.AssignClientId` domain unit test (no event on already-assigned; event on success) |
| F14 | `ShoppingCart.AssignClientId` domain unit test (success ⇒ owner assigned) + code review |
| F15 | code review checklist (`.Produces` declarations) |
| F16 | code review checklist (endpoint metadata + identity) |
| N1–N12 | code review checklist in validation.md + architecture tests + full suite |
