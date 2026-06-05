Implement slice endpoint_input_guards. All specs and the red outside-in test are ready.

Sources (read in this order):
- specs/features/platform/0008_endpoint_input_guards/plan.md
- specs/features/platform/0008_endpoint_input_guards/requirements.md
- specs/features/platform/0008_endpoint_input_guards/tests.md
- specs/features/platform/0008_endpoint_input_guards/validation.md

Acceptance gate:
- tests/BookingManagementService.API.UnitTests/Endpoints/ShoppingCart/EndpointInputGuardsOutsideInTests.cs
  must turn GREEN (dotnet test --filter "FullyQualifiedName~EndpointInputGuardsOutsideInTests").
- Do not touch the test file. If the test fails due to a bug in the
  implementation — fix the implementation, not the test.
- If the test fails due to a defect in the test itself — stop and ask,
  do not silently fix it.

Note on this slice (endpoint-edge, no MediatR use-case): the gate lives in the
existing BookingManagementService.API.UnitTests project (no WebApplicationFactory),
per the 0002 precedent. Implementation per plan.md section 5:
1. Add internal static Guid ParseIdempotencyKey(string requestId) to
   ShoppingCartEndpointApplicationBuilderExtensions, throwing DomainValidationException
   on a non-Guid/empty value; use it in CreateShoppingCart and UnreserveSeats
   (remove the bare throw and the Results.BadRequest()).
2. Change GetClientId from private static to internal static; replace the bare
   throw with throw new UnauthorizedAccessException(...) using the raw claim value.
3. Add .Produces(400) to CreateShoppingCart and UnreserveSeats, .Produces(401) to
   current and AssignUser.
4. Add <InternalsVisibleTo Include="BookingManagementService.API.UnitTests" /> to
   BookingManagementService.API.csproj.
Do NOT change CustomExceptionHandler (the 400/401 mappings already exist), and do
NOT add any new exception/Error type.

Once the outside-in test is green — write the missing unit tests per the
plan (see plan.md section "Tests planned" and agent_docs/testing.md).

Quality gates before completion (run from src/services):
- dotnet format CinemaBookingManagement.sln
- dotnet build  CinemaBookingManagement.sln -warnaserror
- dotnet test   CinemaBookingManagement.sln
  (includes BookingManagementService.Domain.ArchitectureTests)

No schema change in this slice, so no EF Core migration is required.

All must pass with no new warnings or architecture-test failures.
