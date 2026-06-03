Implement slice 0002_content_not_found_404 (ADR-002 step 2: flip ContentNotFoundException 204 -> 404 centrally, with empty-state carve-outs). All specs and the red gate test are ready.

Sources (read in this order):
- specs/features/platform/0002_content_not_found_404/plan.md
- specs/features/platform/0002_content_not_found_404/requirements.md
- specs/features/platform/0002_content_not_found_404/tests.md
- specs/features/platform/0002_content_not_found_404/validation.md

Acceptance gate:
- tests/BookingManagementService.API.UnitTests/Infrastructure/CustomExceptionHandlerContentNotFound404OutsideInTests.cs
  must turn GREEN (dotnet test --filter "FullyQualifiedName~ContentNotFound404OutsideInTests").
  It is currently RED: "Expected httpContext.Response.StatusCode to be 404, but found 204".
- Do not touch the test file. If the test fails due to a bug in the implementation — fix the
  implementation, not the test.
- If the test fails due to a defect in the test itself — stop and ask, do not silently fix it.

Implementation scope (per plan.md section 5):
1. STABLE (ADR-002 step 2, pre-authorized): in
   BookingManagement/BookingManagementService.API/Infrastructure/CustomExceptionHandler.cs,
   rewrite HandleContentNotFoundException to set StatusCode 404 and write a ProblemDetails
   (Status=404, Type="https://tools.ietf.org/html/rfc7231#section-6.5.4",
   Title="The specified resource was not found.", Detail=ex.Message) — mirror
   HandleNotFoundException. Change only this writer; leave the dictionary, TryHandleAsync, and
   every other writer untouched.
2. Carve-out A: GetCurrentShoppingCartQueryHandler returns CreateShoppingCartResponse? and
   returns null (not throw) when there is no active cart (Guid.Empty); keep throwing
   ContentNotFoundException for the inconsistent branch (id recorded, record missing). The
   /api/shoppingcarts/current endpoint maps null => Results.NoContent(), non-null => Results.Ok(result).
3. Carve-out B: GetMovieSessionsQueryHandler returns the (possibly empty) mapped collection
   instead of throwing on empty.
4. Correct OpenAPI .Produces(...) declarations on the affected read paths (see plan.md section 5,
   step 5); leave 204-on-success declarations untouched.
5. Docs: update agent_docs/error_handling.md mapping table to ContentNotFoundException -> 404.
6. Skills: update feature-tests, slice-test-red, feature-validation, feature-requirements,
   spec-workflow to state -> 404 instead of -> 204. Do NOT flip ADR-002 to Accepted.

Once the gate test is green — write the missing unit tests per the plan (see plan.md section
"Tests planned" and agent_docs/testing.md): GetCurrentShoppingCartQueryHandlerTests and
GetMovieSessionsQueryHandlerTests in BookingManagement/tests/BookingManagementService.Domain.UnitTests.

Quality gates before completion (run from src/services):
- dotnet format CinemaBookingManagement.sln
- dotnet build  CinemaBookingManagement.sln -warnaserror
- dotnet test   CinemaBookingManagement.sln
  (includes BookingManagementService.Domain.ArchitectureTests and the new
   BookingManagementService.API.UnitTests gate)

No EF Core schema change in this slice — no migration step. Account for the accepted AutoMapper
NU1903 NuGet-audit advisory so -warnaserror validates real warnings, not the advisory.

All must pass with no new warnings or architecture-test failures.
