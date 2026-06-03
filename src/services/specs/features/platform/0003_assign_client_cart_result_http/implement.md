Implement slice 0003_assign_client_cart_result_http. All specs and the red outside-in test are ready.

Sources (read in this order):
- specs/features/platform/0003_assign_client_cart_result_http/plan.md
- specs/features/platform/0003_assign_client_cart_result_http/requirements.md
- specs/features/platform/0003_assign_client_cart_result_http/tests.md
- specs/features/platform/0003_assign_client_cart_result_http/validation.md

Acceptance gate:
- tests/BookingManagementService.API.UnitTests/Endpoints/Common/ErrorResultsOutsideInTests.cs
  must turn GREEN (dotnet test --filter "FullyQualifiedName~ErrorResultsOutsideInTests").
- Do not touch the test file. If the test fails due to a bug in the
  implementation — fix the implementation, not the test.
- If the test fails due to a defect in the test itself — stop and ask,
  do not silently fix it.

Implementation summary (per plan.md §5):
1. Domain — ShoppingCart.AssignClientId: return DomainErrors<ShoppingCart>.ConflictException(...)
   instead of throwing ConflictException on the already-assigned case; keep appending
   ShoppingCartAssignedToClientDomainEvent only on the success branch; Ensure.NotEmpty stays a throw.
2. Application — AssignClientCartCommandHandler: pass request.ClientId (not request.ShoppingCartId)
   to cart.AssignClientId(...) (the wrong-owner bug fix).
3. API — new API/Endpoints/Common/ErrorResults.cs: static ToProblem(Error) => IResult mapping
   NotFoundError => 404 (+ Detail), ConflictError => 409 (title only), anything else => 500,
   with ProblemDetails shapes mirroring CustomExceptionHandler.
4. API — assignclient endpoint: result.Match(() => Results.Ok(), ErrorResults.ToProblem); delete the
   ConflictError/NotFoundError re-throw bridge and the bare throw new Exception(...); fix .Produces
   to 200/404/409 (drop 201/204). Do NOT modify CustomExceptionHandler.

Once the outside-in test is green — write the missing unit tests per the
plan (see plan.md section "Tests planned" and agent_docs/testing.md):
- AssignClientCartCommandHandlerTests (cart missing => NotFoundError; other active cart => ConflictError;
  success => Result.Success and owner == client id; domain IsFailure propagated).
- ShoppingCart.AssignClientId domain test (already-assigned => ConflictError, no event; success =>
  owner assigned + event raised).

Environment note: the .NET 10 SDK on this machine is the x86 install. Invoke it explicitly:
"C:\Program Files (x86)\dotnet\dotnet.exe" (the 64-bit C:\Program Files\dotnet has only the .NET 8 runtime).

Quality gates before completion (run from src/services):
- dotnet format CinemaBookingManagement.sln
- dotnet build  CinemaBookingManagement.sln -warnaserror
- dotnet test   CinemaBookingManagement.sln
  (includes BookingManagementService.Domain.ArchitectureTests)

Note: the accepted AutoMapper NU1903 NuGet-audit advisory trips -warnaserror at restore time
(MEMORY dotnet10-migration); handle the NuGet audit so the real build/warnings are validated.
No EF Core model changes in this slice → no migration.

All must pass with no new warnings or architecture-test failures.
