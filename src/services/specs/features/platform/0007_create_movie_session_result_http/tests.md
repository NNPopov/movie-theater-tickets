# 0007 · CreateMovieSession Result→HTTP — Outside-in test spec

> **Deviation from the default template (intentional, per PRD — same family as slices
> `0002`–`0006`).** The standard outside-in test for this project goes through
> `WebApplicationFactory<Program>`. **No such harness exists** in this repository (the suites are
> `Domain.UnitTests`, `Domain.ArchitectureTests`, `Infrastructure.UnitTests`, `Application.LoadTests`,
> and the `API.UnitTests` project from `0002`). Slice `0003` already pinned the shared
> `Error → IResult` mapper (`ErrorResults`) with its own gate, so it is **not** re-tested here. The
> PRD's Testing Decisions choose **a focused unit spec of the converted `CreateMovieSessionCommandHandler`
> as the acceptance/RED gate** — the load-bearing, externally-observable behaviour this slice changes
> is *which `Result<Guid>` each outcome of the use-case produces* (and therefore the status the reused
> mapper yields) plus the atomicity invariant (nothing persisted on a missing reference). The "entry
> point" below is therefore the handler's `Handle` method driven with its collaborators substituted;
> the endpoint's `Match` wiring is covered by compilation + code review. The handler gate is **RED**
> until the conversion lands (today the handler returns a bare `Guid` and `throw new Exception()` on a
> missing reference, so no failing `Result` is ever produced and the success path has no `Result<Guid>`).

## Goal

Prove that the converted `create-movie-session` use-case **returns** the right `Result<Guid>` for each
outcome instead of throwing or returning a bare `Guid` — referenced cinema hall (auditorium) missing
⇒ `NotFoundError`; referenced movie missing ⇒ `NotFoundError`; both present ⇒ `Result.Success` whose
`Value` is the created session id (with one `MovieSessionSeat` created per auditorium seat and the
session persisted) — and that on **either** missing-reference outcome **no seat is added and no session
is saved** (the atomicity invariant the thrown path provided implicitly).

## Entry point

Not an HTTP route via `WebApplicationFactory`. The test constructs the handler and invokes it
directly:

- **Method under test:**
  `CreateMovieSessionCommandHandler.Handle(CreateMovieSessionCommand, CancellationToken)`
  (in `Application/MovieSessions/Commands/CreateShowtime/`), returning `Task<Result<Guid>>`.
- **Command:** `new CreateMovieSessionCommand(MovieId: <movieId>, AuditoriumId: <auditoriumId>,
  SessionDate: <a future DateTime>)`.
- **Headers / auth / idempotency:** none — this is an application-layer unit test, not a routed call.

## Wired real

- `CreateMovieSessionCommandHandler` (the real handler, real control flow, real
  short-circuit-before-creation/persistence).
- `CinemaHall` aggregate — real; built via `CinemaHall.Create(name, description, seats)` so its `Seats`
  collection (`SeatEntity` with `.Row` / `.SeatNumber`) drives the per-seat creation loop.
- `Movie` aggregate — real; built via `Movie.Create(title, releaseDate, imdbId, stars)`.
- `MovieSession` / `MovieSessionSeat` aggregates — real; created on the success path by the handler's
  `MovieSession.Create(...)` and `MovieSessionSeat.Create(...)` calls (unchanged by this slice).
- The real `Result<Guid>` / `NotFoundError` types from `Domain/Error`, including the implicit
  `Guid ⇒ Result<Guid>.Success` and `Error ⇒ Result<Guid>.Failure` conversions.
- `IMovieSessionSeatRepository.AddAsync(...)` and `IMovieSessionsRepository.MovieSession(...)` are the
  observable persistence side-effects the scenarios assert on (received on success, **not** received on
  either missing-reference failure).

## Mocked

NSubstitute (the project's mocking library, as in `ReserveTicketsCommandHandlerTests` /
`PurchaseTicketsCommandHandlerTests`):

- `ICinemaHallRepository` — `GetAsync(auditoriumId, ct)` returns the seeded real `CinemaHall` (happy /
  movie-missing scenarios) or `null` (auditorium-missing scenario).
- `IMoviesRepository` — `GetByIdAsync(movieId, ct)` returns the seeded real `Movie` (happy scenario) or
  `null` (movie-missing scenario). Not reached in the auditorium-missing scenario (auditorium is
  checked first).
- `IMovieSessionSeatRepository` — `AddAsync(MovieSessionSeat, ct)` observed (received once per
  auditorium seat on success; **not** received on either failure).
- `IMovieSessionsRepository` — `MovieSession(MovieSession, ct)` observed (received once on success;
  **not** received on either failure).

> Note: `CreateMovieSessionCommandHandler`'s constructor is
> `(IMovieSessionsRepository, ICinemaHallRepository, IMoviesRepository, IMovieSessionSeatRepository)`;
> substitute exactly those four. It takes no `IDistributedLock`, no `ILogger`.

No database, Redis, or RabbitMQ instance is touched.

## Fixtures / setup

- **Auditorium with seats (happy / movie-missing scenarios):** a real
  `CinemaHall.Create("Hall A", "desc", seats)` where `seats` is a known `IList<(short Row, short
  SeatNumber)>` — e.g. two seats `[(1,1), (1,2)]` so the success scenario can assert `AddAsync` is
  received **exactly twice** (once per auditorium seat). `ICinemaHallRepository.GetAsync(...)` returns
  this instance; its `Id` is the command's `AuditoriumId`.
- **Movie (happy scenario):** a real `Movie.Create("Some Title", <releaseDate>, "tt0000000", "stars")`;
  `IMoviesRepository.GetByIdAsync(...)` returns it; its `Id` is the command's `MovieId`.
- **Missing reference:** the corresponding repository substitute returns `null`
  (`(CinemaHall)null!` / `(Movie)null!`).
- **Session date:** any future `DateTime` (e.g. `DateTime.UtcNow.AddDays(1)`); the validator is not in
  the path of this unit test, but use a non-default value for realism.
- **Auth:** none — the unit under test has no authentication.

## Test scenarios

> These three scenarios are the slice's RED gate (the PRD gate facts: the three outcomes, with the
> atomicity assertion folded into both failure scenarios).

### Scenario 1: both references present ⇒ Result.Success(sessionId), one seat per auditorium seat, session saved

**Setup:**
- `ICinemaHallRepository.GetAsync(auditoriumId, ct)` ⇒ the seeded `CinemaHall` with two seats
  `[(1,1), (1,2)]`.
- `IMoviesRepository.GetByIdAsync(movieId, ct)` ⇒ the seeded `Movie`.

**Act:**
- `await handler.Handle(new CreateMovieSessionCommand(movieId, auditoriumId, futureDate),
  CancellationToken.None);`

**Expect:**
- `result.IsSuccess == true`.
- `result.Value` is a non-empty `Guid` — the created session id (the same id the session was persisted
  with).
- Side-effects: `IMovieSessionSeatRepository.Received(2).AddAsync(Arg.Any<MovieSessionSeat>(),
  Arg.Any<CancellationToken>())` (one per auditorium seat); `IMovieSessionsRepository.Received(1)
  .MovieSession(Arg.Any<MovieSession>(), Arg.Any<CancellationToken>())`.

**Covers requirement(s):** F8, F12, F13, F15, F17.

### Scenario 2: auditorium (cinema hall) missing ⇒ NotFoundError, nothing persisted

**Setup:**
- `ICinemaHallRepository.GetAsync(auditoriumId, ct)` ⇒ `null`.
- (`IMoviesRepository.GetByIdAsync` need not be stubbed — the auditorium is checked first and
  short-circuits.)

**Act:**
- `await handler.Handle(new CreateMovieSessionCommand(movieId, auditoriumId, futureDate),
  CancellationToken.None);`

**Expect:**
- `result.IsFailure == true` and `result.Error` is a `NotFoundError` — proving the first bare
  `throw new Exception()` is replaced by a `NotFoundError` (`CinemaHall.NotFound`), surfacing as `404`
  via the mapper.
- `IMovieSessionSeatRepository.DidNotReceive().AddAsync(Arg.Any<MovieSessionSeat>(),
  Arg.Any<CancellationToken>())` and `IMovieSessionsRepository.DidNotReceive()
  .MovieSession(Arg.Any<MovieSession>(), Arg.Any<CancellationToken>())` (atomicity).

**Covers requirement(s):** F9, F11, F16.

### Scenario 3: movie missing (auditorium present) ⇒ NotFoundError, nothing persisted

**Setup:**
- `ICinemaHallRepository.GetAsync(auditoriumId, ct)` ⇒ the seeded `CinemaHall`.
- `IMoviesRepository.GetByIdAsync(movieId, ct)` ⇒ `null`.

**Act:**
- `await handler.Handle(new CreateMovieSessionCommand(movieId, auditoriumId, futureDate),
  CancellationToken.None);`

**Expect:**
- `result.IsFailure == true` and `result.Error` is a `NotFoundError` — proving the second bare
  `throw new Exception()` is replaced by a `NotFoundError` (`Movie.NotFound`), surfacing as `404` via
  the mapper.
- `IMovieSessionSeatRepository.DidNotReceive().AddAsync(...)` and
  `IMovieSessionsRepository.DidNotReceive().MovieSession(...)` (atomicity — nothing persisted even
  though the auditorium existed).

**Covers requirement(s):** F10, F11, F16.

> All three scenarios are **RED** against the current code: today `Handle` returns `Task<Guid>` (so a
> `Result<Guid>` cannot even be asserted — Scenario 1 fails to compile/return a `Result`), and the two
> missing-reference cases `throw new Exception()` rather than returning a `NotFoundError` (Scenarios 2
> and 3 would observe a thrown bare `Exception`, not a failing `Result`). The scenarios pass once plan
> §5 steps 1–3 land.

## Out of scope for this test

- The shared `ErrorResults.ToProblem` mapping (`NotFoundError ⇒ 404`, `ConflictError ⇒ 409`,
  else `⇒ 500`) — already covered by slice `0003`'s `ErrorResultsOutsideInTests`; not re-tested.
- The endpoint's `Match` wiring, the replaced `Results.CreatedAtRoute(...)`, and the `.Produces(...)`
  OpenAPI declarations (F1–F5) — covered by compilation + code review (validation.md).
- Field-level / structural validation (`Guid.Empty`, empty `SessionDate` ⇒ `400` via
  `ValidationBehaviour`) — the validator is unchanged (F7) and not part of this handler gate.
- The domain (`MovieSession.Create`, `MovieSessionSeat.Create`, the hardcoded seat price `15`) — it is
  unchanged by this slice (N11) and has no new domain unit test.
- Repository / adapter error translation and `DbUpdateException` variants — no such logic changes on
  this path (no repository test).
- The end-to-end routing of `POST /api/moviesessions` to `201`/`404` — no `WebApplicationFactory`
  harness; verified manually (validation.md scenarios).
- Performance, load, and concurrency.
