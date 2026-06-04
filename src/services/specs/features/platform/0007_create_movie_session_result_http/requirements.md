# 0007 · CreateMovieSession Result→HTTP — Requirements

## Inputs

- PRD: ./prd.md
- Plan: ./plan.md

> Contract note: HTTP statuses below are derived from the `CustomExceptionHandler` mapping table in
> `agent_docs/error_handling.md` (`ValidationException → 400`, `ContentNotFoundException → 404`,
> anything else `→ 500`) and, for the converted path, the shared `ErrorResults.ToProblem` mapper
> introduced by slice `0003` (`NotFoundError → 404`, `ConflictError → 409`, any other `Error → 500`).
> This slice changes **neither** the exception table **nor** the mapper. It is **deliberately
> behaviour-changing**: the two missing-reference cases the create handler currently signals with a
> bare `throw new Exception()` (auditorium/cinema-hall not found, movie not found) fall into the
> handler's `typeof(Exception)` arm and serialize as `500`; returning a `NotFoundError` and routing
> it through the mapper moves each to its correct `404`. The success contract (`201 Created`,
> `CreatedAtRoute` to `GetShowtimeById`, body = the new id) is **preserved**. This is the **first
> endpoint use of the generic `Result<Guid>`** (the generic was built in `0001`; `0003`–`0006` used
> the non-generic `Result`).
>
> Open-question resolution assumed by these requirements (reaffirmed here): "auditorium not found" /
> "movie not found" are `404` (`NotFoundError`), a missing referenced resource being a not-found
> business outcome consistent with ADR-002; structural/malformed input stays `400` via
> `ValidationBehaviour` (PRD Further Notes).

## Functional requirements

### Endpoint — `POST /api/moviesessions` (name `CreateMovieSessions`)

- **F1.** The endpoint `POST /api/moviesessions` resolves the handler's `Result<Guid>` with
  `result.Match(id => Results.CreatedAtRoute("GetShowtimeById", new { id }, id), ErrorResults.ToProblem)`,
  returning HTTP `201 Created` (with the new session id as the body and a `Location` to
  `GetShowtimeById`) on success and an `IResult` from the shared mapper on failure — the failure
  branch never throws.
- **F2.** The endpoint's previous direct `Results.CreatedAtRoute(...)` call (which assumed the
  handler returned a bare `Guid`) is removed; the create result is now obtained through `.Match`.
- **F3.** The endpoint reuses the existing shared mapper `ErrorResults.ToProblem(Error)` from
  `API/Endpoints/Common/` unchanged; no new mapping module is created and `ErrorResults.cs` is not
  edited.
- **F4.** The endpoint's OpenAPI metadata declares `.Produces<Guid>(201, "application/json")` and
  `.Produces(404)` and drops the stale `.Produces(204)`.
- **F5.** The endpoint retains `.WithName("CreateMovieSessions")` and `.WithTags(Tag)` and continues
  to bind the MediatR command directly from the body (`[FromBody] CreateMovieSessionCommand request`);
  no separate HTTP request model is introduced.

### Command + Validator

- **F6.** `CreateMovieSessionCommand` is retyped from `IRequest<Guid>` to `IRequest<Result<Guid>>`;
  its fields (`Guid MovieId`, `Guid AuditoriumId`, `DateTime SessionDate`) are unchanged.
- **F7.** `CreateMovieSessionCommandValidator` is unchanged: it still rejects the command with a
  `ValidationException` (HTTP `400 ValidationProblemDetails`) when `AuditoriumId`, `MovieId`, or
  `SessionDate` is empty; reference *existence* remains a handler/business concern, not a validator
  concern.

### Handler — `CreateMovieSessionCommandHandler`

- **F8.** The handler is retyped to `IRequestHandler<CreateMovieSessionCommand, Result<Guid>>` and
  `Handle` returns `Task<Result<Guid>>`.
- **F9.** The handler returns a `NotFoundError` with code `CinemaHall.NotFound`
  (`DomainErrors<CinemaHall>.NotFound(...)`) when the referenced cinema hall (auditorium) does not
  exist (`ICinemaHallRepository.GetAsync` returns `null`), surfacing as HTTP `404` via the mapper;
  this replaces the first bare `throw new Exception()`.
- **F10.** The handler returns a `NotFoundError` with code `Movie.NotFound`
  (`DomainErrors<Movie>.NotFound(...)`) when the referenced movie does not exist
  (`IMoviesRepository.GetByIdAsync` returns `null`), surfacing as HTTP `404` via the mapper; this
  replaces the second bare `throw new Exception()`.
- **F11.** The handler evaluates the two not-found checks in the existing order (auditorium first,
  then movie) and short-circuits each **before** `MovieSession.Create`, the per-seat creation loop,
  and any persistence call, so nothing is written when a referenced resource is missing (atomicity
  invariant the thrown path provided implicitly).
- **F12.** On the happy path (both references present) the handler creates the `MovieSession` via
  `MovieSession.Create(movie.Id, auditorium.Id, request.SessionDate, auditorium.Seats.Count)`, creates
  one `MovieSessionSeat` per auditorium seat via `MovieSessionSeat.Create(...)` and adds each through
  `IMovieSessionSeatRepository.AddAsync`, persists the session via
  `IMovieSessionsRepository.MovieSession`, and returns `Result.Success(showtime.Id)` (via the implicit
  `Guid ⇒ Result<Guid>` conversion), surfacing as HTTP `201`.
- **F13.** The handler's success-path creation logic — the seat-creation loop, the hardcoded seat
  price `15`, the `MovieSession.Create` arguments, and the repository calls — is unchanged from the
  pre-conversion handler; only the return type and the two error branches change.
- **F14.** Genuinely unexpected faults on the path (repository / infrastructure failures) continue to
  propagate as exceptions to `CustomExceptionHandler` (HTTP `500`); they are not converted to
  `Result`s.

### Generic `Result<Guid>` (reused, unchanged)

- **F15.** The conversion reuses the generic `Result<TValue>` and its `Match<TValue, TOut>` extension
  from `Domain/Error` (slice `0001`) unchanged — including the implicit `TValue ⇒ Success` and
  `Error ⇒ Failure` conversions — and is the first endpoint to exercise `Result<Guid>` end to end; no
  new code is added to the Error infrastructure.

### Observable status contract

- **F16.** Status is **corrected** (intentional behaviour change): cinema-hall (auditorium) not found
  `500 → 404`; movie not found `500 → 404`. Each moves off the misleading `500` produced by the bare
  `throw new Exception()` falling into `CustomExceptionHandler`'s `typeof(Exception)` arm.
- **F17.** Status is **preserved** for: success (`201 Created`, `CreatedAtRoute` to `GetShowtimeById`,
  body = the new id), malformed input (`400` via `ValidationBehaviour`), and repository/infrastructure
  faults (`500`).
- **F18.** This slice closes the last named bare-`throw new Exception()` defect on the `MovieSessions`
  write path flagged by ADR-002; no other `MovieSessions` use-case is touched.

## Non-functional requirements

- **N1.** The converted use-case remains a MediatR `IRequestHandler<CreateMovieSessionCommand,
  Result<Guid>>` with the command a `record` implementing `IRequest<Result<Guid>>`; it is not
  converted to another style. Per `agent_docs/architecture.md`.
- **N2.** No use-case sets `HttpContext.Response.StatusCode`; the success status comes from the
  endpoint (`Results.CreatedAtRoute`) and the converted failure status comes from the shared mapper's
  `IResult`. Per `CLAUDE.md` rule 5 and `agent_docs/error_handling.md`.
- **N3.** No new cross-cutting `*Exception` or `Error` type is introduced; the conversion reuses the
  existing `DomainErrors<T>.NotFound(...)` factory and the `NotFoundError` kind from `Domain/Error`.
  Per `CLAUDE.md` § Forbidden.
- **N4.** `Domain` and `Application` contain no EF Core types, `DbContext`, ASP.NET, or
  `IResult`/`HttpContext` references; the `Error → IResult` mapping lives only in the `API` layer.
  Per `agent_docs/architecture.md` (Dependency Rule).
- **N5.** The handler raises no HTTP-transport exception and writes to no `HttpContext`; it expresses
  the two missing-reference outcomes as a `Result<Guid>` and HTTP shaping happens only in the API
  layer. Per `CLAUDE.md` rule 5.
- **N6.** The endpoint delegate contains no business logic: it binds the body, sends the command via
  `ISender.Send`, and shapes the HTTP result via `Match`. Per `agent_docs/entry_points/minimal-api.md`.
- **N7.** The `CustomExceptionHandler` mechanism and all its writers, the shared `ErrorResults` mapper,
  the `DomainErrors` factories, and every base type are unchanged; this slice only routes two outcomes
  through the existing policy objects. Per `agent_docs/stable_vs_feature.md`.
- **N8.** All I/O remains `async`/`await` with `CancellationToken` threaded through; no synchronous
  database or I/O call is introduced. Per `CLAUDE.md`.
- **N9.** The build produces no new warnings (`dotnet build -warnaserror`), accounting for the
  accepted AutoMapper `NU1903` NuGet-audit advisory and the pre-existing nullable baseline debt. Per
  `CLAUDE.md` § Verifying changes and MEMORY `dotnet10-migration` / `warnaserror-baseline-debt`.
- **N10.** Architecture tests (`BookingManagementService.Domain.ArchitectureTests`) and the full
  `dotnet test` suite pass without new failures. Per `CLAUDE.md` § Verifying changes.
- **N11.** No EF Core entity is added or altered and no migration is created; the domain
  (`MovieSession.Create`, `MovieSessionSeat.Create`) is unchanged. Per the PRD and plan §1.
- **N12.** `CreateMovieSession` is the **only** converted use-case in this slice; no STABLE file is
  edited (no DI line, since the four repositories are already registered and injected). Per the PRD
  and plan §1.

## Out of scope

- The endpoint-helper bare throws `GetClientId` / `CreateShoppingCart` in the ShoppingCart endpoints
  (a separate, later ADR-002 tail).
- The composition-root guard `throw new Exception("identityOptions is null")` in
  `ConfigureApiServices` (a config-time startup check, not a business path).
- Converting any read/query handler that throws `ContentNotFoundException` to `Result` — intentional
  exception usage, documented as such in `agent_docs/error_handling.md`.
- Any other `MovieSessions` use-case (update, delete, list, get-by-id).
- Changing `MovieSession.Create`, the seat-creation loop, or the hardcoded seat price `15`.
- Introducing a separate HTTP request model distinct from the bound command.
- Adopting `Result<T>` on any other path, adding a `400`-class mapper arm, or any `ErrorResults` /
  `DomainErrors` / `Error` / `CustomExceptionHandler` / MediatR-pipeline / validation-behaviour /
  base-type change.
- Standing up a `WebApplicationFactory` HTTP integration harness; schema changes / EF Core migrations.
- The Flutter client follow-up to this slice's `500 → 404` contract change.

## Traceability

| Requirement | Verified by |
|---|---|
| F1 | code review checklist (validation.md); endpoint `Match` wiring verified by compilation + handler gate |
| F2 | code review checklist (direct `CreatedAtRoute` replaced by `.Match`) |
| F3 | code review checklist (mapper reused, `ErrorResults.cs` unchanged) |
| F4 | code review checklist (`.Produces<Guid>(201)` + `.Produces(404)`; `.Produces(204)` dropped) |
| F5 | code review checklist (endpoint metadata + command-as-body binding) |
| F6 | compilation (`IRequest<Result<Guid>>` signature) + `CreateMovieSessionCommandHandler` unit test |
| F7 | code review checklist (validator unchanged); existing validator behaviour |
| F8 | compilation (`IRequestHandler<…, Result<Guid>>` signature) |
| F9 | `CreateMovieSessionCommandHandler` unit test (auditorium missing ⇒ `NotFoundError`; nothing persisted) |
| F10 | `CreateMovieSessionCommandHandler` unit test (movie missing ⇒ `NotFoundError`; nothing persisted) |
| F11 | `CreateMovieSessionCommandHandler` unit test (on each missing-reference case `AddAsync` / `MovieSession` `DidNotReceive`) |
| F12 | `CreateMovieSessionCommandHandler` unit test (both present ⇒ `Result.Success` whose `Value` is the session id; one `AddAsync` per seat; session saved once) |
| F13 | code review checklist (success-path creation logic, seat price `15`, repository calls unchanged) |
| F14 | code review checklist (repository/infrastructure faults still propagate as exceptions) |
| F15 | compilation (generic `Result<Guid>` + `Match`) + `CreateMovieSessionCommandHandler` unit test (`result.Value`) |
| F16 | `CreateMovieSessionCommandHandler` unit test (both missing-reference cases ⇒ `NotFoundError`) + code review (status corrected `500 → 404`) |
| F17 | `CreateMovieSessionCommandHandler` unit test (success ⇒ `Result.Success`) + code review (preserved statuses) |
| F18 | code review checklist (the two bare `throw new Exception()` removed; no other use-case touched) |
| N1–N12 | code review checklist in validation.md + architecture tests + full suite |
