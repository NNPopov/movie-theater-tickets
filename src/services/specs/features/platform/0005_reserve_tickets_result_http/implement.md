Implement slice 0005_reserve_tickets_result_http. All specs and the red outside-in test are ready.

Sources (read in this order):
- specs/features/platform/0005_reserve_tickets_result_http/plan.md
- specs/features/platform/0005_reserve_tickets_result_http/requirements.md
- specs/features/platform/0005_reserve_tickets_result_http/tests.md
- specs/features/platform/0005_reserve_tickets_result_http/validation.md

Acceptance gate:
- BookingManagement/tests/BookingManagementService.Domain.UnitTests/ShoppingCarts/ReserveTicketsCommandHandlerTests.cs
  must turn GREEN (dotnet test --filter "FullyQualifiedName~ReserveTicketsCommandHandlerTests").
  (This slice family has no WebApplicationFactory harness; the acceptance gate is a focused handler
  unit test, per plan.md section 6 and tests.md — same as slices 0003/0004.)
- Do not touch the test file. If the test fails due to a bug in the implementation — fix the
  implementation, not the test.
- If the test fails due to a defect in the test itself — stop and ask, do not silently fix it.

Implementation summary (see plan.md section 5 for the exact code):
1. Domain — ShoppingCart.SeatsReserve() void -> Result: InWork => SeatsReserved + one
   ShoppingCartReservedDomainEvent + Result.Success(); already SeatsReserved => Result.Success() with
   no event (idempotent); PurchaseCompleted (and any other status) => ConflictError, no event. Stop
   calling EnsurePurchaseIsNotCompleted() (leave that shared guard unchanged).
2. Domain — MovieSessionSeatService.CheckSeatSaleAvailability Task -> Task<Result>: session-not-found
   => NotFoundError; sales-terminated => ConflictError (delete the bare throw new Exception). Thread
   the Result through all three callers (SelSeats, ReserveSeats, SelectSeat), short-circuiting on
   IsFailure.
3. Application — ReserveTicketsCommandHandler: cart-missing => return NotFoundError (remove
   GetShoppingCartOrThrow); consume cart.SeatsReserve()'s Result and short-circuit; delete the bare
   throw new Exception("Couldn't Reserve …") and return the failing ReserveSeats Result; short-circuit
   before SaveAsync / SetAsync / the per-seat DeleteAsync (atomicity).
4. API — the reservations endpoint: return result; => Match(() => Results.Ok(), ErrorResults.ToProblem);
   .Produces 200/404/409 (drop 201/204).

Known accepted side-effect (do not "fix" it here): retyping the shared CheckSeatSaleAvailability makes
the not-yet-converted purchase path (PurchaseTickets, whose endpoint still does return result;)
serialize session-not-found / terminated as 200 until slice 0006 converts it. Flag it for 0006; leave
the purchase endpoint untouched.

Once the outside-in gate is green — write the missing unit tests per the plan (see plan.md section
"Tests planned" and agent_docs/testing.md): add the ShoppingCart.SeatsReserve domain facts to
ShoppingCarts/ShoppingCartSpecification.cs (InWork => SeatsReserved + event; already SeatsReserved =>
Result.Success(), no event; PurchaseCompleted => ConflictError, no event), and re-run slice 0004's
SelectSeatCommandHandlerTests unchanged as the regression gate for the shared-helper retype.

Quality gates before completion (run from src/services, using the x86 SDK at
"C:\Program Files (x86)\dotnet\dotnet.exe" per MEMORY dotnet-sdk-path; suppress the accepted AutoMapper
NU1903 NuGet-audit advisory so the real warnings are validated):
- dotnet format CinemaBookingManagement.sln
- dotnet build  CinemaBookingManagement.sln -warnaserror
- dotnet test   CinemaBookingManagement.sln
  (includes BookingManagementService.Domain.ArchitectureTests)

No EF Core schema change in this slice -> no migration.

All must pass with no new warnings or architecture-test failures.
