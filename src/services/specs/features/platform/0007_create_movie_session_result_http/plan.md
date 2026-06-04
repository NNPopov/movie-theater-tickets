# 0007 · CreateMovieSession Result→HTTP — Implementation plan

## 1. Header

- **Aggregate / Module:** `MovieSessions` (the `CreateMovieSession` write path: its command +
  handler and the `POST {BaseRoute}` create endpoint). Filed under the `platform` slice folder to
  keep the ADR-002 step-3 series (`0001`–`0007`) together, consistent with `0003`–`0006`.
- **Slice:** `0007_create_movie_session_result_http`
- **PRD:** ./prd.md
- **ADR:** `docs/adr/ADR-002-error-handling-model-result-vs-exceptions.md` (Accepted 2026-06-04).
  This is the fifth end-to-end conversion and the **first endpoint use of the generic `Result<Guid>`**
  (the generic was built in `0001` but never exercised at an endpoint; `0003`–`0006` all used the
  non-generic `Result`).
- **Reference slice:** `../0006_purchase_tickets_result_http/plan.md` and
  `../0005_reserve_tickets_result_http/plan.md` — same `platform` module, same ADR-002 step-3 shape
  (replace a direct HTTP result with `Match`-to-HTTP through the **shared** `ErrorResults.ToProblem`
  mapper; pin with a focused handler unit gate; **no** `WebApplicationFactory`). `0005`'s
  `ReserveTicketsCommandHandlerTests` is the closest handler-gate template (NSubstitute mocks, real
  domain factories, assert which `Result`/`Error` is returned plus the persistence side-effects).
  **Shape difference from `0005`/`0006`:** this slice carries **no domain change** (no `void → Result`
  conversion) and returns the **generic `Result<Guid>`** rather than the non-generic `Result`.
- **HTTP path (no new route; existing route, mechanism swap):**
  - `POST /api/moviesessions` (`BaseRoute = "api/moviesessions"`, endpoint name
    `CreateMovieSessions`). The direct `Results.CreatedAtRoute(...)` is replaced by
    `result.Match(id => Results.CreatedAtRoute("GetShowtimeById", new { id }, id), ErrorResults.ToProblem)`.
    **Behaviour-changing** (the point of the slice): a referenced auditorium/movie that does not exist
    moves from `500` (the bare `throw new Exception()` falling into `CustomExceptionHandler`'s
    `typeof(Exception)` arm) to `404`. The success contract (`201 Created`, `CreatedAtRoute` to
    `GetShowtimeById`, body = the new id) is **unchanged**.
- **STABLE files touched:** **none.** No DI line (the four repositories are already registered and
  injected by today's handler), no `CustomExceptionHandler`, no base type, no MediatR pipeline, no
  validation behaviour, no `ErrorResults`/`DomainErrors`/`Error` change. The endpoint class
  (`MovieSessionEndpointApplicationBuilderExtensions`) is a per-resource feature file, not stable
  infrastructure. **No EF Core entity added or altered → no migration.**

## 2. Context summary

ADR-002 step-3's fifth conversion, on the `MovieSessions` write path. The `CreateMovieSession`
handler currently advertises `IRequest<Guid>` and, when the referenced cinema hall (auditorium) or
movie does not exist, executes a bare `throw new Exception()` (no message). `CustomExceptionHandler`
maps any unrecognised exception to `500`, so an ordinary "you referenced an id that isn't there"
mistake is reported as a server bug carrying no useful `ProblemDetails`. This slice retypes the
command to `IRequest<Result<Guid>>`, makes the handler return `DomainErrors<CinemaHall>.NotFound(...)`
/ `DomainErrors<Movie>.NotFound(...)` for the two missing-reference cases (short-circuiting **before**
any creation/persistence) and `Result.Success(showtime.Id)` on success, and resolves the result at
the endpoint with the shared `ErrorResults.ToProblem` mapper so the two not-found cases become `404`
while success stays `201 Created`. It closes the last named bare-`throw new Exception()` defect on
this aggregate and is the first endpoint to exercise the generic `Result<Guid>`. The acceptance gate
is a focused unit spec of the converted handler in `BookingManagementService.Domain.UnitTests`.

## 3. API contract

Mechanism swap on an existing route — no new request/response model. The endpoint already binds the
MediatR command directly as the body (`[FromBody] CreateMovieSessionCommand`); this quirk is
preserved (introducing a separate request model is out of scope).

### Endpoint — `POST /api/moviesessions` (name `CreateMovieSessions`)

- **Request:** `[FromBody] CreateMovieSessionCommand request` — fields
  `Guid MovieId`, `Guid AuditoriumId`, `DateTime SessionDate`. **Validation unchanged**:
  `CreateMovieSessionCommandValidator` keeps `AuditoriumId NotEmpty`, `MovieId NotEmpty`,
  `SessionDate NotEmpty` (structural `400` via `ValidationBehaviour`).
- **Command:** `CreateMovieSessionCommand` changes from `IRequest<Guid>` to
  **`IRequest<Result<Guid>>`**.
- **Resolution:**
  `result.Match(id => Results.CreatedAtRoute("GetShowtimeById", new { id }, id), ErrorResults.ToProblem)`
  — replaces the direct `Results.CreatedAtRoute(...)`. Success carries the new session id as the body.
- **Status codes (the contract this slice locks in):**

  | Outcome | Mechanism before | Status before | Mechanism after | Status after |
  |---|---|---|---|---|
  | Both references exist | `return showtime.Id;` ⇒ `Results.CreatedAtRoute` | 201 + id body | `Result.Success(id)` ⇒ `Results.CreatedAtRoute` (id body) | **201** |
  | Auditorium (cinema hall) not found | `throw new Exception()` ⇒ `typeof(Exception)` arm | **500** | `Result` `NotFoundError` (`CinemaHall.NotFound`) ⇒ mapper | **404** |
  | Movie not found | `throw new Exception()` ⇒ `typeof(Exception)` arm | **500** | `Result` `NotFoundError` (`Movie.NotFound`) ⇒ mapper | **404** |
  | Malformed input (empty ids / date) | `ValidationBehaviour` ⇒ `ValidationException` | 400 | unchanged | 400 |
  | Repository / infrastructure fault | exception | 500 | exception (unchanged) | 500 |

- **`.Produces` corrected:** declare `201` (with `<Guid>`) and `404`; **drop the stale
  `.Produces(204)`** (the create endpoint never returns `204`). The `500` path stays
  exception-driven via `CustomExceptionHandler` and is not declared (consistent with sibling
  endpoints).

### Shared mapper — `ErrorResults.ToProblem(Error)` (existing, reused unchanged)

`API/Endpoints/Common/ErrorResults.cs` from slice `0003`: `NotFoundError ⇒ 404`,
`ConflictError ⇒ 409`, any other `Error ⇒ 500`. Both not-found outcomes here are `NotFoundError`, so
they map cleanly to `404`. The mapper is already pinned by `0003`'s `ErrorResultsOutsideInTests` and
is **not edited or re-tested** here.

### Generic `Result<Guid>` ergonomics (existing, reused unchanged)

`Result<TValue>` (slice `0001`) carries implicit conversions: `Guid ⇒ Result<Guid>.Success` and
`Error ⇒ Result<Guid>.Failure`. So the handler can `return showtime.Id;` (success) and
`return DomainErrors<CinemaHall>.NotFound(...);` (failure) directly, and the generic
`Match<TValue, TOut>` overload in `ResultExtensions` resolves it at the endpoint. **No new code in
the Error infrastructure.**

## 4. File structure

```
BookingManagement/
├── BookingManagementService.Application/
│   └── MovieSessions/Commands/CreateShowtime/
│       └── CreateMovieSessionCommandHandler.cs   # EDIT: command IRequest<Guid> → IRequest<Result<Guid>>;
│                                                  #       handler IRequestHandler<…, Result<Guid>>;
│                                                  #       the two `throw new Exception()` → NotFoundError returns
│                                                  #       (short-circuit before creation/persistence);
│                                                  #       success `return showtime.Id;` (implicit Result<Guid>.Success)
│                                                  #       (CreateMovieSessionCommandValidator.cs UNCHANGED)
└── BookingManagementService.API/
    └── Endpoints/
        └── MovieSessionEndpointApplicationBuilderExtensions.cs
                                                   # EDIT: create delegate direct CreatedAtRoute → Match(
                                                   #       id => Results.CreatedAtRoute("GetShowtimeById", new { id }, id),
                                                   #       ErrorResults.ToProblem);
                                                   #       .Produces<Guid>(201) + .Produces(404); drop .Produces(204)

BookingManagement/tests/
└── BookingManagementService.Domain.UnitTests/      # EXISTING (RootNamespace CinemaTicketBooking.Application.UnitTests;
    └── MovieSessions/                              #  references Application; NSubstitute + xUnit + FluentAssertions)
        └── CreateMovieSessionCommandHandlerTests.cs  # NEW: the RED acceptance gate (written by /slice-test-red, step 5)
```

> Project-naming note (carried from `0002`–`0006`): `BookingManagementService.Domain.UnitTests` is
> named "Domain" but its `RootNamespace` is `CinemaTicketBooking.Application.UnitTests` and it
> references the **Application** project with NSubstitute — the correct home for the handler gate.
> The `MovieSessions/` test sub-folder is new (the existing handler gates live under `ShoppingCarts/`).

No EF Core entity is added or altered → **no migration**.

## 5. Implementation steps

1. **Application — retype the command.** In `CreateMovieSessionCommandHandler.cs`, change the record
   from `IRequest<Guid>` to `IRequest<Result<Guid>>`. Add `using CinemaTicketBooking.Domain.Error;`
   (and `using CinemaTicketBooking.Domain.CinemaHalls;` / `using CinemaTicketBooking.Domain.Movies;`
   for the `DomainErrors<T>` type arguments if not already pulled in transitively).

2. **Application — retype the handler and convert the two bare throws.** Change the handler to
   `IRequestHandler<CreateMovieSessionCommand, Result<Guid>>` and `Handle` to return
   `Task<Result<Guid>>`. Replace the two `throw new Exception();` checks with `NotFoundError`
   returns that **short-circuit before** `MovieSession.Create`, the seat-creation loop, and
   persistence (atomicity — nothing is written when a reference is missing). Preserve the current
   check order (auditorium first, then movie):
   ```csharp
   public async Task<Result<Guid>> Handle(CreateMovieSessionCommand request,
       CancellationToken cancellationToken)
   {
       var auditorium = await _cinemaHallRepository.GetAsync(request.AuditoriumId, cancellationToken);
       if (auditorium == null)
           return DomainErrors<CinemaHall>.NotFound($"Cinema hall {request.AuditoriumId} was not found.");

       var movie = await _moviesRepository.GetByIdAsync(request.MovieId, cancellationToken);
       if (movie == null)
           return DomainErrors<Movie>.NotFound($"Movie {request.MovieId} was not found.");

       var showtime = MovieSession.Create(movie.Id, auditorium.Id, request.SessionDate, auditorium.Seats.Count);

       foreach (var seat in auditorium.Seats)
       {
           var showtimeSeat = MovieSessionSeat.Create(showtime.Id, seat.Row, seat.SeatNumber, 15);
           await _movieSessionSeatRepository.AddAsync(showtimeSeat, cancellationToken);
       }

       await _movieSessionsRepository.MovieSession(showtime, cancellationToken);

       return showtime.Id;   // implicit Result<Guid>.Success(showtime.Id)
   }
   ```
   `DomainErrors<CinemaHall>.NotFound(...)` yields code `CinemaHall.NotFound`,
   `DomainErrors<Movie>.NotFound(...)` yields code `Movie.NotFound` (a `NotFoundError` each), matching
   the PRD. **No domain change** — `MovieSession.Create`, the seat-creation loop, the hardcoded seat
   price `15`, and the repository calls are byte-for-byte the same on the success path. The four
   repository fields/constructor stay as-is (no DI change).

3. **API — endpoint: direct result → shared mapper.** In
   `MovieSessionEndpointApplicationBuilderExtensions.cs`, the `CreateMovieSessions` delegate
   (currently lines 45–59) becomes:
   ```csharp
   endpointRouteBuilder.MapPost($"{BaseRoute}", async ([FromBody] CreateMovieSessionCommand request,
           ISender sender,
           CancellationToken cancellationToken) =>
       {
           var result = await sender.Send(request, cancellationToken);

           return result.Match(
               id => Results.CreatedAtRoute("GetShowtimeById", new { id }, id),
               ErrorResults.ToProblem);
       })
       .WithName("CreateMovieSessions")
       .WithTags(Tag)
       .Produces<Guid>(201, "application/json")
       .Produces(404);
   ```
   Add `using CinemaTicketBooking.Api.Endpoints.Common;` (where `ErrorResults` lives) and
   `using CinemaTicketBooking.Domain.Error;` (for the generic `Match`) if not already present. The
   delegate's return type becomes `IResult`. The route value name (`id`) and the `GetShowtimeById`
   target are unchanged from today. Drop `.Produces(204)`; add `.Produces(404)`.

   > Note: the existing code passed `routeValues: new { id = result.ToString() }, value: result`. The
   > new form passes the raw `Guid id` for both the route value and the body, which is equivalent for
   > the `GetShowtimeById` link (`id` is bound as a `Guid`) and keeps the body a bare `Guid` exactly
   > as `.Produces<Guid>(201)` advertises.

4. **Verify (pre-test).** From `src/services` (use the x86 SDK at
   `C:\Program Files (x86)\dotnet\dotnet.exe` via the PowerShell tool — see MEMORY `dotnet-sdk-path`):
   ```
   dotnet format CinemaBookingManagement.sln
   dotnet build  CinemaBookingManagement.sln -warnaserror
   ```
   The command retype ripples to exactly one caller (the endpoint, step 3) and the new handler gate
   (step 5) — there is no other `sender.Send(new CreateMovieSessionCommand(...))` consumer to update.
   Resolve warnings; confirm no import became unused. Known environment notes (MEMORY
   `warnaserror-baseline-debt`, `dotnet10-migration`): handle the accepted AutoMapper **NU1903**
   NuGet-audit advisory so real warnings are what is validated, and scope `dotnet format` to the
   touched files (or `git checkout` `ReserveSeatsCommandValidatorSpecification.cs` if the formatter
   rewrites it).

## 6. Tests planned

The externally observable behaviour is the `Result<Guid>` each outcome produces (hence the status the
shared mapper yields) and the persistence side-effects. There is **no** `WebApplicationFactory`
harness; the change is pinned by a focused unit test of the converted handler, consistent with
`0003`–`0006`.

- **Handler unit test — RED acceptance gate — NEW
  `BookingManagement/tests/BookingManagementService.Domain.UnitTests/MovieSessions/CreateMovieSessionCommandHandlerTests.cs`.**
  xUnit + FluentAssertions + NSubstitute (same conventions as `ReserveTicketsCommandHandlerTests.cs`).
  NSubstitute mocks `ICinemaHallRepository`, `IMoviesRepository`, `IMovieSessionSeatRepository`,
  `IMovieSessionsRepository`. Drive a real `CinemaHall` (with a known seat collection) and a real
  `Movie` via their factories. Facts (RED until the conversion in §5 lands — today the handler returns
  `Guid` and throws a bare `Exception`, so no failing `Result` is ever produced):
  1. **auditorium (cinema hall) missing** ⇒ result `IsFailure`, `Error` is `NotFoundError`; **no**
     `MovieSessionSeat` added (`_movieSessionSeatRepository.AddAsync` `DidNotReceive`) and **no**
     session saved (`_movieSessionsRepository.MovieSession` `DidNotReceive`) — atomicity.
  2. **movie missing** (auditorium present) ⇒ result `IsFailure`, `Error` is `NotFoundError`; nothing
     persisted (`AddAsync` / `MovieSession` `DidNotReceive`) — atomicity.
  3. **both present** ⇒ result `IsSuccess`, `result.Value` equals the created session id; **one**
     `MovieSessionSeat` created per auditorium seat (`AddAsync` received once per seat) and the
     session saved (`MovieSession` received once). The returned `Value` must equal the id the session
     was persisted with.

- **No `WebApplicationFactory` end-to-end / endpoint integration test — skipped (opt-out):** no HTTP
  harness exists in this repo (consistent with `0003`–`0006`); the endpoint's `Match` wiring is
  covered by compilation and the shared mapper is already pinned by `0003`'s
  `ErrorResultsOutsideInTests`.
- **No domain unit test — skipped (opt-out):** the domain (`MovieSession.Create`,
  `MovieSessionSeat.Create`) is **unchanged** by this slice.
- **No repository / adapter unit test — skipped (opt-out):** no repository/adapter logic changes and
  no new business-meaningful infrastructure-exception translation on this path.

**Full-suite gate:** the entire `dotnet test` run, **including
`BookingManagementService.Domain.ArchitectureTests`**, must stay green.

## 7. Out of scope for this slice

- The endpoint-helper bare throws `GetClientId` / `CreateShoppingCart` in the ShoppingCart endpoints
  (a separate, later ADR-002 tail).
- The composition-root guard `throw new Exception("identityOptions is null")` in
  `ConfigureApiServices` (a config-time startup check, not a business path).
- Converting query/read handlers that throw `ContentNotFoundException` (intentional exception tails).
- Any other `MovieSessions` use-case (update, delete, list, get-by-id).
- Changing `MovieSession.Create`, the seat-creation loop, or the hardcoded seat price `15`.
- Introducing a separate HTTP request model distinct from the bound command (the endpoint keeps
  binding the command as the body, as today).
- Adopting `Result<T>` on any other path, adding a `400`-class mapper arm, or any
  `ErrorResults` / `DomainErrors` / `Error` / `CustomExceptionHandler` / base-type change.
- A `WebApplicationFactory` HTTP integration harness; schema changes / EF Core migrations.
- The Flutter client follow-up to this slice's `500 → 404` contract change.

## 8. Open questions

None. The PRD's one open question — are "auditorium not found" / "movie not found" a `404` or a `400`?
— is **resolved to `404` (`NotFoundError`)**: a missing referenced resource is a not-found business
outcome, consistent with the rest of ADR-002; structural/malformed input stays `400` via
`ValidationBehaviour`. To be reaffirmed in `requirements.md`.
