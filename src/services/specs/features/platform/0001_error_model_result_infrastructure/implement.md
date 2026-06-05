Implement slice 0001_error_model_result_infrastructure. All specs and the red acceptance-gate test are ready.

Sources (read in this order):
- specs/features/platform/0001_error_model_result_infrastructure/plan.md
- specs/features/platform/0001_error_model_result_infrastructure/requirements.md
- specs/features/platform/0001_error_model_result_infrastructure/tests.md
- specs/features/platform/0001_error_model_result_infrastructure/validation.md

Context: this is ADR-002 step 1, a Domain/Error shared-kernel change with NO HTTP endpoint and NO MediatR use-case. Scope is exactly the six files named in plan.md (introduce Result<T>, open the base Result ctor to protected, add the generic Match overload, fix DomainErrors<T> to use typeof(T).Name, rename NotFountError to NotFoundError, update its one endpoint call site). Do not exceed that scope; if anything more seems necessary, stop and ask (it would exceed the ADR's step-1 boundary).

Acceptance gate (this slice has no HTTP outside-in test — the gate is an in-process unit spec):
- BookingManagement/tests/BookingManagementService.Domain.UnitTests/Error/ResultOfTSpecification.cs
  must turn GREEN (dotnet test --filter "FullyQualifiedName~ResultOfTSpecification").
- Do not touch the test file. If the test fails due to a bug in the implementation — fix the implementation, not the test.
- If the test fails due to a defect in the test itself — stop and ask, do not silently fix it.

The unit spec IS the slice's unit test per the plan (handler/repository/endpoint levels are opted out — no such components). No further unit tests need to be authored after green.

Quality gates before completion (run from src/services):
- dotnet format CinemaBookingManagement.sln
- dotnet build  CinemaBookingManagement.sln -warnaserror
- dotnet test   CinemaBookingManagement.sln
  (includes BookingManagementService.Domain.ArchitectureTests)

Note: the build trips the accepted AutoMapper NU1903 NuGet-audit advisory under -warnaserror at restore time (a known, accepted project constraint) — handle NuGet audit accordingly so real build warnings are what is validated.

No schema change in this slice, so no EF migration is needed.

All must pass with no new warnings or architecture-test failures.
