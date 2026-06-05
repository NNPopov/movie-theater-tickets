Implement slice 0007_create_movie_session_result_http. All specs and the red outside-in test are ready.

Sources (read in this order):
- specs/features/platform/0007_create_movie_session_result_http/plan.md
- specs/features/platform/0007_create_movie_session_result_http/requirements.md
- specs/features/platform/0007_create_movie_session_result_http/tests.md
- specs/features/platform/0007_create_movie_session_result_http/validation.md

Acceptance gate:
- BookingManagement/tests/BookingManagementService.Domain.UnitTests/MovieSessions/CreateMovieSessionCommandHandlerTests.cs
  must turn GREEN (dotnet test --filter "FullyQualifiedName~CreateMovieSessionCommandHandlerTests").
- Do not touch the test file. If the test fails due to a bug in the
  implementation — fix the implementation, not the test.
- If the test fails due to a defect in the test itself — stop and ask,
  do not silently fix it.

Implementation summary (see plan.md section 5 for detail):
- Application/MovieSessions/Commands/CreateShowtime/CreateMovieSessionCommandHandler.cs:
  retype the command from IRequest<Guid> to IRequest<Result<Guid>>; retype the handler to
  IRequestHandler<CreateMovieSessionCommand, Result<Guid>> (Handle returns Task<Result<Guid>>);
  replace the two `throw new Exception();` calls with `return DomainErrors<CinemaHall>.NotFound(...)`
  (auditorium missing) and `return DomainErrors<Movie>.NotFound(...)` (movie missing), each
  short-circuiting BEFORE MovieSession.Create / the seat-creation loop / persistence; keep the success
  path unchanged and `return showtime.Id;` (implicit Result<Guid>.Success). The validator is unchanged.
- BookingManagement/BookingManagementService.API/Endpoints/MovieSessionEndpointApplicationBuilderExtensions.cs:
  replace the create delegate's direct Results.CreatedAtRoute(...) with
  result.Match(id => Results.CreatedAtRoute("GetShowtimeById", new { id }, id), ErrorResults.ToProblem);
  change .Produces to .Produces<Guid>(201, "application/json").Produces(404) and drop .Produces(204).
- No domain change, no new Error type, no mapper/CustomExceptionHandler/base-type change, no DI line,
  no schema/migration. If anything beyond this proves necessary, stop and ask (ADR territory).

Once the outside-in test is green — write the missing unit tests per the
plan (see plan.md section "Tests planned" and agent_docs/testing.md). For this slice the handler
gate IS the only added test (the domain is unchanged); confirm the opt-outs in plan.md section 6
still hold.

Quality gates before completion (run from src/services, using the x86 SDK at
"C:\Program Files (x86)\dotnet\dotnet.exe" via the PowerShell tool — MEMORY dotnet-sdk-path):
- dotnet format CinemaBookingManagement.sln
- dotnet build  CinemaBookingManagement.sln -warnaserror
- dotnet test   CinemaBookingManagement.sln
  (includes BookingManagementService.Domain.ArchitectureTests)

Known environment notes (MEMORY warnaserror-baseline-debt / dotnet10-migration): the accepted
AutoMapper NU1903 NuGet-audit advisory trips -warnaserror at restore; handle the NuGet audit so real
warnings are what is validated. dotnet format is known to reformat
Seats/ReserveSeatsCommandValidatorSpecification.cs — scope the format to touched files or git checkout
that file.

No schema change in this slice → no EF Core migration to add or apply.

All must pass with no new warnings or architecture-test failures.
