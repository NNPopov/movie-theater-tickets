# 0004 · SelectSeats Result→HTTP — Requirements

## Inputs

- PRD: ./prd.md
- Plan: ./plan.md

> Contract note: HTTP statuses below are derived from the `CustomExceptionHandler` mapping table
> in `agent_docs/error_handling.md` (`ContentNotFoundException`/`NotFoundException → 404`,
> `ConflictException → 409`, `DomainValidationException/ValidationException → 400`,
> `LockedException → 423`, anything else `→ 500`) and, for the converted paths, the shared
> `ErrorResults.ToProblem` mapper introduced by slice `0003` (`NotFoundError → 404`,
> `ConflictError → 409`, any other `Error → 500`). This slice changes **neither** the exception
> table **nor** the mapper; it routes three previously-thrown outcomes through the existing mapper
> instead, leaving every observable status unchanged.

## Functional requirements

- **F1.** The endpoint `POST /api/shoppingcarts/{ShoppingCartId}/seats/select` resolves the
  handler's `Result` with `result.Match(() => Results.Ok(), ErrorResults.ToProblem)`, returning
  HTTP `200 OK` with an empty body on success and an `IResult` from the shared mapper on failure —
  the failure branch never throws.
- **F2.** The `seats/select` endpoint's dead failure branch
  `failure => Results.BadRequest(failure.Description)` is removed (no `400 Bad Request` is produced
  by the `Match` failure branch).
- **F3.** The `seats/select` endpoint reuses the existing shared mapper
  `ErrorResults.ToProblem(Error)` from `API/Endpoints/Common/` unchanged; no new mapping module is
  created and `ErrorResults.cs` is not edited.
- **F4.** The `SelectSeatCommandHandler` returns a `NotFoundError`
  (`DomainErrors<ShoppingCart>.NotFound`) when the shopping cart does not exist
  (`ActiveShoppingCartRepository.GetByIdAsync` returns `null`), surfacing as HTTP `404` via the
  mapper, instead of throwing `ContentNotFoundException`.
- **F5.** The `SelectSeatCommandHandler` propagates a failing `Result` returned by
  `MovieSessionSeatService.SelectSeat` (seat status not Available, or seat held by another cart)
  back to the endpoint, surfacing as HTTP `409` via the mapper.
- **F6.** The `SelectSeatCommandHandler` short-circuits and returns the failing `Result` **before**
  calling `SaveShoppingCart`, so a shopping cart is never persisted holding a seat whose claim
  failed (atomicity invariant).
- **F7.** On the happy path the `SelectSeatCommandHandler` returns `Result.Success()` after
  `SaveShoppingCart`, surfacing as HTTP `200`.
- **F8.** `MovieSessionSeatService.SelectSeat` is retyped from `Task<MovieSessionSeat>` to
  `Task<Result>`; it returns the aggregate's `Result` and no longer returns the
  `MovieSessionSeat`.
- **F9.** The internal `Result → ConflictException` bridge in `MovieSessionSeatService.SelectSeat`
  (`else { throw new ConflictException(nameof(MovieSessionSeat), ...); }`) is removed: on failure
  the aggregate `Result` is returned unchanged; on success the seat is persisted via
  `UpdateAsync` and `Result.Success()` is returned.
- **F10.** `MovieSessionSeat.Select` returns a `ConflictError`
  (`DomainErrors<MovieSessionSeat>.ConflictException`) — not a base `Error` via
  `DomainErrors<MovieSessionSeat>.InvalidOperation` — for the "the place is already being processed
  by another shopping cart" case (`shoppingCartId != ShoppingCartId && ShoppingCartId != Guid.Empty`),
  so it maps to HTTP `409` rather than falling through the mapper to `500`.
- **F11.** `MovieSessionSeat.Select` keeps returning a `ConflictError`
  (`DomainErrors<MovieSessionSeat>.ConflictException`) for the "status is not Available" case
  (unchanged).
- **F12.** `MovieSessionSeat.Select` appends `MovieSessionSeatStatusUpdatedDomainEvent` **only** on
  the success branch (after both conflict guards), so no event is raised on either conflict
  outcome.
- **F13.** `MovieSessionSeat.Select` keeps `Ensure.NotEmpty(shoppingCartId, ...)` and
  `Ensure.NotEmpty(hashId, ...)` as thrown structural guards, and on success sets
  `Status = SeatStatus.Selected` (assigning `ShoppingCartId`/`ShoppingCartHashId` when previously
  empty).
- **F14.** The distributed-lock-not-acquired case in the handler remains a thrown `LockedException`
  (HTTP `423`); it is not converted to a `Result`.
- **F15.** The Redis seat-lifecycle failure and its `ReturnSeatToAvailable` rollback in the handler
  remain a thrown `InvalidOperationException` (HTTP `500`); the rollback `try/catch` is a
  compensating action that re-throws and is not converted to a `Result`.
- **F16.** The `cart.EnsureSeatCanBeAdded(...)` guards remain thrown exceptions
  (`ConflictException → 409`, `DomainValidationException → 400`); they are not converted in this
  slice.
- **F17.** The not-found checks in the shared `MovieSessionSeatService` helpers
  (`GetMovieSessionSeat` seat-not-found, `CheckSeatSaleAvailability` movie-session-not-found) remain
  thrown `ContentNotFoundException` (HTTP `404`), and the sales-terminated bare `throw new
  Exception(...)` (HTTP `500`) is left unchanged, because those helpers are shared with the
  not-yet-converted `Reserve`/`Purchase` paths.
- **F18.** The `seats/select` endpoint's OpenAPI metadata declares `.Produces(200)`,
  `.Produces(404)`, `.Produces(409)` and drops the stale `.Produces(201)` and `.Produces(204)`.
- **F19.** The `seats/select` endpoint retains `.WithName("SelectSeat")` and `.WithTags(Tag)`, and
  continues to map `ReserveSeatsRequest` to `SelectSeatCommand` unchanged.
- **F20.** The externally observable status codes across the conversion are unchanged: success
  `200`, cart/session/seat not-found `404`, both seat conflicts `409`, `EnsureSeatCanBeAdded`
  validation `400`, lock `423`, infrastructure `500`.

## Non-functional requirements

- **N1.** The converted use-case remains a MediatR `IRequestHandler<SelectSeatCommand, Result>`
  with the command a `record` implementing `IRequest<Result>`; it is not converted to another
  style. Per `agent_docs/architecture.md`.
- **N2.** No use-case sets `HttpContext.Response.StatusCode`; the success status comes from the
  endpoint (`Results.Ok`) and every converted failure status comes from the shared mapper's
  `IResult`. Per `CLAUDE.md` rule 5 and `agent_docs/error_handling.md`.
- **N3.** No new cross-cutting `*Exception` or `Error` type is introduced; the
  `InvalidOperation → ConflictError` change reuses the existing
  `DomainErrors<T>.ConflictException(...)` factory / `ConflictError` from `Domain/Error`. Per
  `CLAUDE.md` § Forbidden.
- **N4.** `Domain` and `Application` contain no EF Core types, `DbContext`, ASP.NET, or
  `IResult`/`HttpContext` references; the `Error → IResult` mapping lives only in the `API` layer.
  Per `agent_docs/architecture.md` (Dependency Rule).
- **N5.** No handler raises an HTTP-transport exception or writes to `HttpContext`; the domain
  service and aggregate express the business conflict as a `Result`, and HTTP shaping happens only
  in the API layer. Per `CLAUDE.md` rule 5.
- **N6.** The endpoint delegate contains no business logic: it binds the request, builds the
  command, calls `ISender.Send`, and shapes the HTTP result via `Match`. Per
  `agent_docs/entry_points/minimal-api.md`.
- **N7.** The `CustomExceptionHandler` mechanism and all its writers, and the shared
  `ErrorResults` mapper, are unchanged; this slice only re-routes outcomes through the existing
  policy objects. Per `agent_docs/stable_vs_feature.md`.
- **N8.** All I/O remains `async`/`await` with `CancellationToken` threaded through; no synchronous
  database or I/O call is introduced. Per `CLAUDE.md`.
- **N9.** The build produces no new warnings (`dotnet build -warnaserror`), accounting for the
  accepted AutoMapper `NU1903` NuGet-audit advisory. Per `CLAUDE.md` § Verifying changes and
  MEMORY `dotnet10-migration`.
- **N10.** Architecture tests (`BookingManagementService.Domain.ArchitectureTests`) and the full
  `dotnet test` suite pass without new failures. Per `CLAUDE.md` § Verifying changes.
- **N11.** ADR-002 remains **Proposed**; this slice does not flip it to Accepted. Per the PRD.
- **N12.** `SelectSeats` is the **only** converted use-case in this slice; `ReserveTickets`,
  `PurchaseTickets`, and the shared `MovieSessionSeatService` helpers are left unchanged for later
  slices. Per the PRD.

## Out of scope

- Converting any use-case other than `SelectSeats` (`ReserveTickets`, `PurchaseTickets` — later
  slices that reuse this slice's pattern and the `0003` mapper).
- Adopting `Result<T>` (the generic) on this path; `SelectSeats` uses the non-generic `Result`.
- Fixing the bare `throw new Exception(...terminated)` in the shared `CheckSeatSaleAvailability`,
  or converting any shared `MovieSessionSeatService` helper (`GetMovieSessionSeat`, the
  movie-session lookup) to `Result`.
- Adding a `400`-class arm to `ErrorResults.ToProblem` or otherwise changing the shared mapper /
  introducing a new `Error` kind.
- The bare `throw` in the endpoints' `GetClientId` / `CreateShoppingCart` helpers.
- Changing `CustomExceptionHandler`, the MediatR pipeline, the validation behaviour, or any base
  type.
- Standing up a `WebApplicationFactory` HTTP integration harness, or a real-concurrency seat-race
  integration test.
- Flipping ADR-002 to Accepted.

## Traceability

| Requirement | Verified by |
|---|---|
| F1 | code review checklist (validation.md); endpoint `Match` wiring verified by compilation + handler gate |
| F2 | code review checklist (dead `BadRequest` branch deleted) |
| F3 | code review checklist (mapper reused, `ErrorResults.cs` unchanged) |
| F4 | `SelectSeatCommandHandler` unit test (cart missing ⇒ `NotFoundError`) |
| F5 | `SelectSeatCommandHandler` unit test (seat not Available ⇒ `ConflictError`; another cart ⇒ `ConflictError`) |
| F6 | `SelectSeatCommandHandler` unit test (failing claim ⇒ `SaveAsync` not received) |
| F7 | `SelectSeatCommandHandler` unit test (available seat ⇒ `Result.Success()`, cart saved) |
| F8 | compilation (`Task<Result>` signature) + handler unit test (propagated `Result`) |
| F9 | `SelectSeatCommandHandler` unit test (conflict propagated, not thrown) + code review |
| F10 | `MovieSessionSeat.Select` domain unit test (another cart ⇒ `ConflictError`) |
| F11 | `MovieSessionSeat.Select` domain unit test (status not Available ⇒ `ConflictError`) |
| F12 | `MovieSessionSeat.Select` domain unit test (no event on either conflict; event on success) |
| F13 | `MovieSessionSeat.Select` domain unit test (success ⇒ `Status == Selected`) + code review |
| F14 | code review checklist (lock guard stays `LockedException`) |
| F15 | code review checklist (Redis rollback stays `InvalidOperationException`, re-throws) |
| F16 | code review checklist (`EnsureSeatCanBeAdded` guards unchanged) |
| F17 | code review checklist (shared helpers unchanged) |
| F18 | code review checklist (`.Produces` declarations) |
| F19 | code review checklist (endpoint metadata + request mapping) |
| F20 | handler + domain unit tests (per-outcome `Result`) + code review (status preservation) |
| N1–N12 | code review checklist in validation.md + architecture tests + full suite |
