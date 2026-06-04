# 0007 · CreateMovieSession Result→HTTP — Validation

## Inputs

- PRD: ./prd.md
- Plan: ./plan.md
- Requirements: ./requirements.md

> Nature of this slice: the **fifth** ADR-002 step-3 conversion, at one endpoint
> (`POST /api/moviesessions`), and the **first endpoint use of the generic `Result<Guid>`**. It
> retypes `CreateMovieSessionCommand` from `IRequest<Guid>` to `IRequest<Result<Guid>>`, replaces the
> handler's two bare `throw new Exception()` calls (auditorium/cinema-hall not found, movie not found)
> with `NotFoundError` returns that short-circuit before any creation/persistence, and resolves the
> endpoint with `result.Match(id => Results.CreatedAtRoute("GetShowtimeById", new { id }, id),
> ErrorResults.ToProblem)` (the **existing** shared mapper from slice `0003`). **The two
> missing-reference outcomes are corrected on purpose: `500 → 404`.** The success contract
> (`201 Created`, `CreatedAtRoute` to `GetShowtimeById`, body = the new id) is **preserved**. There is
> **no domain change** and **no `WebApplicationFactory` harness** in this repo; the acceptance gate is
> a **focused unit spec of the converted handler** (`CreateMovieSessionCommandHandlerTests`) in
> `BookingManagementService.Domain.UnitTests` (see Prerequisites and the test section). The manual
> curl scenarios below are for a human poking the running service and are not the automated gate.

## Prerequisites

- Service running locally:
  ```
  dotnet run --project BookingManagement/BookingManagementService.API
  ```
  (default port: check
  `BookingManagement/BookingManagementService.API/Properties/launchSettings.json`; examples below use
  `http://localhost:<port>`).
- Test database provisioned and migrations applied (**no new migration in this slice**):
  ```
  dotnet ef database update \
    -p BookingManagement/BookingManagementService.Infrastructure \
    -s BookingManagement/BookingManagementService.API
  ```
- Seed data: at least one **cinema hall (auditorium)** with a known `auditoriumId` and a non-empty
  seat collection, and one **movie** with a known `movieId`. Note both ids for S1. For the not-found
  scenarios, use ids that are **not** present in the database.

## Manual scenarios

### S1 — Happy path: create a movie session for an existing auditorium and movie

**Steps:**

1. ```
   curl -s -i -X POST http://localhost:<port>/api/moviesessions \
     -H "Content-Type: application/json" \
     -d '{"movieId": "<movieId>", "auditoriumId": "<auditoriumId>", "sessionDate": "2026-07-01T18:00:00Z"}'
   ```
2. Follow the `Location` header (or call `GET /api/moviesessions/<newId>`) to read the session back.

**Expected:**

- Step 1: HTTP `201 Created`; the `Location` header points at the `GetShowtimeById` route
  (`/api/moviesessions/<newId>`); the response body is the new session id (a bare `Guid`).
- Step 2: the session exists, references `<movieId>` and `<auditoriumId>`, and has one
  `MovieSessionSeat` per auditorium seat.

**Covers:** F1, F4, F5, F12, F17.

### S2 — Not found: auditorium (cinema hall) does not exist (the corrected `500 → 404`)

**Steps:**

1. ```
   curl -s -i -X POST http://localhost:<port>/api/moviesessions \
     -H "Content-Type: application/json" \
     -d '{"movieId": "<movieId>", "auditoriumId": "00000000-0000-0000-0000-000000000000", "sessionDate": "2026-07-01T18:00:00Z"}'
   ```

**Expected:**

- HTTP `404 Not Found` (**was `500` before this slice** — the bare `throw new Exception()` fell into
  `CustomExceptionHandler`'s `typeof(Exception)` arm).
- `ProblemDetails` body: `status: 404`,
  `type: "https://tools.ietf.org/html/rfc7231#section-6.5.4"`,
  `title: "The specified resource was not found."`, `detail` present (the handler's `NotFoundError`
  description for `CinemaHall.NotFound`). Shape identical to any other `404` in the service.
- No movie session and no seats are persisted (atomicity).

**Covers:** F9, F11, F16.

### S3 — Not found: movie does not exist (the corrected `500 → 404`)

**Steps:**

1. ```
   curl -s -i -X POST http://localhost:<port>/api/moviesessions \
     -H "Content-Type: application/json" \
     -d '{"movieId": "00000000-0000-0000-0000-000000000000", "auditoriumId": "<auditoriumId>", "sessionDate": "2026-07-01T18:00:00Z"}'
   ```

**Expected:**

- HTTP `404 Not Found` with the **same** `ProblemDetails` shape as S2 (here the `detail` reflects
  `Movie.NotFound`). The auditorium is checked first and passes, so this `404` comes from the movie
  check.
- No movie session and no seats are persisted (atomicity).

**Covers:** F10, F11, F16.

### S4 — Validation failure: empty required field (status preserved at `400`)

**Steps:**

1. ```
   curl -s -i -X POST http://localhost:<port>/api/moviesessions \
     -H "Content-Type: application/json" \
     -d '{"movieId": "00000000-0000-0000-0000-000000000000", "auditoriumId": "<auditoriumId>", "sessionDate": "2026-07-01T18:00:00Z"}'
   ```
   (Submit with `movieId` as an all-zero `Guid` — empty — or omit `sessionDate`.)

**Expected:**

- HTTP `400 Bad Request` with a `ValidationProblemDetails` body listing the offending field
  (`MovieId` / `AuditoriumId` / `SessionDate`), produced by `ValidationBehaviour` — **unchanged** by
  this slice (reference *existence* is a `404`, but a *structurally empty* id is still a `400`).

> Note: `Guid.Empty` (`00000000-…`) is what the `NotEmpty` validator rejects, so this `400` precedes
> the handler's existence checks. Use it to confirm structural validation still fires before the
> business not-found logic.

**Covers:** F7, F17.

### S5 — Body-shape parity check (regression for the reused mapper)

**Steps:**

1. Capture the `404` body from S2/S3 and compare it field-by-field to any exception-driven not-found
   body in the service, e.g. `GET /api/moviesessions/00000000-0000-0000-0000-000000000000`.

**Expected:**

- The `404` bodies are identical in shape (`status`/`type`/`title`/`detail`) — proving the
  missing-reference path now flows through the shared `ErrorResults.ToProblem` mapper rather than
  serializing a `500`.

**Covers:** F3, F16.

## Code review checklist

Each line is a yes/no question. Reject the PR until all are yes.

### Architecture

- [ ] The converted use-case stays in `Application/MovieSessions/Commands/CreateShowtime/`; the
      command is a `record` `CreateMovieSessionCommand : IRequest<Result<Guid>>` and the handler a
      MediatR `IRequestHandler<CreateMovieSessionCommand, Result<Guid>>`. (N1, F6, F8)
- [ ] The shared mapper `API/Endpoints/Common/ErrorResults.cs` is **reused unchanged**; no new mapping
      module is created. (F3)
- [ ] The generic `Result<TValue>` / `Match<TValue, TOut>` in `Domain/Error` is reused unchanged (no
      new code in the Error infrastructure). (F15)
- [ ] No `IResult` / `HttpContext` / ASP.NET type appears in `Domain` or `Application`; the
      `Error → IResult` mapping exists only in the `API` layer (Dependency Rule). (N4)
- [ ] The endpoint delegate contains no business logic: it binds the body, builds/sends the command,
      and shapes the result via `result.Match(id => Results.CreatedAtRoute(...), ErrorResults.ToProblem)`. (N6)
- [ ] No new aggregate, domain event, or repository interface was introduced; `MovieSession`,
      `MovieSessionSeat`, `CinemaHall`, `Movie`, and the four repositories are pre-existing.

### Error handling

- [ ] The endpoint's direct `Results.CreatedAtRoute(...)` is **replaced** by `.Match`; the failure
      branch is `ErrorResults.ToProblem`. (F1, F2)
- [ ] The handler's **two** `throw new Exception();` calls are **removed**: auditorium missing ⇒
      `DomainErrors<CinemaHall>.NotFound(...)` (code `CinemaHall.NotFound`); movie missing ⇒
      `DomainErrors<Movie>.NotFound(...)` (code `Movie.NotFound`). (F9, F10)
- [ ] Each not-found check short-circuits **before** `MovieSession.Create`, the per-seat creation loop,
      and any persistence call (atomicity); check order is auditorium-then-movie. (F11)
- [ ] On success the handler returns `Result.Success(showtime.Id)` (via the implicit `Guid ⇒
      Result<Guid>` conversion) after creating one `MovieSessionSeat` per auditorium seat and
      persisting the session; the seat-creation loop, the hardcoded price `15`, the `MovieSession.Create`
      args, and the repository calls are **unchanged**. (F12, F13)
- [ ] Repository / infrastructure faults still propagate as exceptions to `CustomExceptionHandler`
      (`500`); they are not converted to `Result`s. (F14)
- [ ] `CreateMovieSessionCommandValidator` is **unchanged** (still `NotEmpty` on `AuditoriumId`,
      `MovieId`, `SessionDate`); reference existence is not moved into the validator. (F7)
- [ ] No new cross-cutting `*Exception` or `Error` type was introduced; the conversion reuses the
      existing `DomainErrors<T>.NotFound(...)` factory and the `NotFoundError` kind. (N3)
- [ ] No handler sets an HTTP status code or references `HttpContext`. (N5)

### Stable infrastructure

- [ ] `CustomExceptionHandler.cs`, `ErrorResults.cs`, `DomainErrors`, and the `Error` kinds are
      **unchanged** (this slice only routes two outcomes through the existing policy objects). (N7)
- [ ] No base type (`AggregateRoot`, `Entity`, `Result`, `Error`), MediatR pipeline behaviour,
      `IEndpoints`/`EndpointExtensions` mechanism, or `Program.cs` was changed.
- [ ] No DI registration line was needed (the four repositories were already registered and injected);
      if one was added, flag and justify it. (N12)

### Domain / schema (unchanged)

- [ ] `MovieSession.Create`, `MovieSessionSeat.Create`, the seat-creation loop, and the hardcoded seat
      price `15` are **unchanged**; no EF Core entity or configuration is altered and **no migration**
      is added. (N11, F13)

### Scope / completeness

- [ ] `CreateMovieSession` is the **only** converted use-case; no other `MovieSessions` use-case is
      touched, and the endpoint-helper bare throws / composition-root guard are **not** touched. (N12, F18)
- [ ] The two bare `throw new Exception()` calls — the last named bare-exception defect on the
      `MovieSessions` write path per ADR-002 — are removed. (F18)

### OpenAPI / metadata

- [ ] The create endpoint declares `.Produces<Guid>(201, "application/json")` and `.Produces(404)` and
      no longer declares `.Produces(204)`. (F4)
- [ ] `.WithName("CreateMovieSessions")`, `.WithTags(Tag)` are retained, and the endpoint still binds
      `[FromBody] CreateMovieSessionCommand`. (F5)

### Tests

- [ ] The `CreateMovieSessionCommandHandler` outside-in gate test exists in
      `BookingManagement/tests/BookingManagementService.Domain.UnitTests/MovieSessions/CreateMovieSessionCommandHandlerTests.cs`,
      is the acceptance gate, and is GREEN: auditorium missing ⇒ `NotFoundError` (nothing persisted);
      movie missing ⇒ `NotFoundError` (nothing persisted); both present ⇒ `Result.Success` whose
      `Value` is the session id, one `AddAsync` per auditorium seat, session saved once — and on
      **every** failure `IMovieSessionSeatRepository.AddAsync` and `IMovieSessionsRepository.MovieSession`
      are not received (atomicity).
- [ ] **Opt-outs honoured (per plan §6):** no `WebApplicationFactory` endpoint/integration test (no
      harness exists; the `Match` wiring is covered by compilation, the mapper by slice `0003`'s
      `ErrorResultsOutsideInTests`); no domain unit test (the domain is unchanged); no repository/adapter
      test (no infrastructure-exception translation changes on this path).

### Quality gates

Run from `src/services`:

```
dotnet format CinemaBookingManagement.sln
dotnet build  CinemaBookingManagement.sln -warnaserror
dotnet test   CinemaBookingManagement.sln
dotnet test --filter "FullyQualifiedName~CreateMovieSessionCommandHandlerTests"
```

All must pass, including:
- `BookingManagementService.Domain.ArchitectureTests` (domain has no dependency on application,
  aggregate roots have a private parameterless constructor, domain events are `sealed` and named
  `*DomainEvent`) — unaffected, since this slice changes no domain type.
- The slice's outside-in handler gate (`CreateMovieSessionCommandHandlerTests`).

> Note: the accepted AutoMapper `NU1903` NuGet-audit advisory trips `-warnaserror` at restore time
> (MEMORY `dotnet10-migration`); handle the NuGet audit so the real build/warnings are what is
> validated. `dotnet format` is known to reformat `ReserveSeatsCommandValidatorSpecification.cs` —
> scope the format to touched files or `git checkout` that file (MEMORY `warnaserror-baseline-debt`).
> The working .NET 10 SDK is the x86 install at `C:\Program Files (x86)\dotnet\dotnet.exe`, run via
> the PowerShell tool (MEMORY `dotnet-sdk-path`). No EF Core model changes in this slice → no
> migration to run.

If the architecture tests fail, the slice is **not done** even if every other test is green.
