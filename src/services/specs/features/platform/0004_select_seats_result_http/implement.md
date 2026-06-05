Implement slice 0004_select_seats_result_http. All specs and the red outside-in gate are ready.

Sources (read in this order):
- specs/features/platform/0004_select_seats_result_http/plan.md
- specs/features/platform/0004_select_seats_result_http/requirements.md
- specs/features/platform/0004_select_seats_result_http/tests.md
- specs/features/platform/0004_select_seats_result_http/validation.md

Acceptance gate:
- BookingManagement/tests/BookingManagementService.Domain.UnitTests/ShoppingCarts/SelectSeatCommandHandlerTests.cs
  must turn fully GREEN (dotnet test --filter "FullyQualifiedName~SelectSeatCommandHandlerTests").
  It is currently RED: 1 of 4 facts passes (the status-preserving happy path), and 3 fail because
  the code still THROWS (ContentNotFoundException for the missing cart; ConflictException from
  MovieSessionSeatService.SelectSeat for both seat conflicts) instead of RETURNING a failing Result.
- This slice has NO WebApplicationFactory harness; the converted handler unit test IS the gate
  (same pattern as slice 0003). The shared ErrorResults.ToProblem mapper already exists and is
  reused unchanged.
- Do not touch the test file. If the test fails due to a bug in the implementation — fix the
  implementation, not the test. If the test fails due to a defect in the test itself — stop and
  ask, do not silently fix it.

Implementation summary (see plan.md section 5 for the exact edits):
- Domain/Seats/MovieSessionSeat.cs: Select "another cart" branch InvalidOperation -> ConflictException
  (a ConflictError); event stays success-only.
- Domain/Services/MovieSessionSeatService.cs: SelectSeat Task<MovieSessionSeat> -> Task<Result>;
  delete the else-throw ConflictException bridge; propagate the aggregate Result (persist on success).
- Application/ShoppingCarts/Command/SelectSeats/SelectSeatCommandHandler.cs: cart-missing returns
  NotFoundError; consume SelectSeat's Result and short-circuit on IsFailure BEFORE SaveShoppingCart.
- API/Endpoints/ShoppingCartEndpointApplicationBuilderExtensions.cs (seats/select delegate): replace
  the dead failure => Results.BadRequest(...) branch with ErrorResults.ToProblem; .Produces 200/404/409.

Once the gate is green — write the missing domain unit test per the plan (plan.md section
"Tests planned": MovieSessionSeatSpecification for MovieSessionSeat.Select — status not Available =>
ConflictError, no event; another cart => ConflictError, no event; success => Selected + event) and
agent_docs/testing.md.

Quality gates before completion (run from src/services):
- dotnet format CinemaBookingManagement.sln
- dotnet build  CinemaBookingManagement.sln -warnaserror
- dotnet test   CinemaBookingManagement.sln
  (includes BookingManagementService.Domain.ArchitectureTests)

Environment note: the working .NET 10 SDK on this machine is at "C:\Program Files (x86)\dotnet\dotnet.exe"
(the dotnet.exe on PATH at "C:\Program Files\dotnet" has no SDK and reports "No .NET SDKs were found").
Use the (x86) dotnet for build/test. The accepted AutoMapper NU1903 NuGet-audit advisory trips
-warnaserror at restore time (MEMORY dotnet10-migration); handle the NuGet audit so the real
build/warnings are validated. No EF Core model change in this slice => no migration.

All must pass with no new warnings or architecture-test failures.
