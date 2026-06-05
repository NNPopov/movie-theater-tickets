Implement slice 0006_purchase_tickets_result_http. All specs and the red gate test are ready.

Sources (read in this order):
- specs/features/platform/0006_purchase_tickets_result_http/plan.md
- specs/features/platform/0006_purchase_tickets_result_http/requirements.md
- specs/features/platform/0006_purchase_tickets_result_http/tests.md
- specs/features/platform/0006_purchase_tickets_result_http/validation.md

Acceptance gate:
- BookingManagement/tests/BookingManagementService.Domain.UnitTests/ShoppingCarts/PurchaseTicketsCommandHandlerTests.cs
  must be fully GREEN
  (dotnet test --filter "FullyQualifiedName~PurchaseTicketsCommandHandlerTests").
  Note: this project deviates from the default WebApplicationFactory outside-in
  pattern — no HTTP harness exists in the repo. The acceptance gate is the
  PurchaseTickets handler unit spec, consistent with slices 0002–0005.
- Do not touch the test file. If the test fails due to a bug in the
  implementation — fix the implementation, not the test.
- If the test fails due to a defect in the test itself — stop and ask,
  do not silently fix it.

Implementation (per plan.md section 5):
1. Domain — MovieSessionSeat.Sell(): retype the one "another shopping cart"
   case (ShoppingCartId != shoppingCartId) from
   DomainErrors<MovieSessionSeat>.InvalidOperation(...) to .ConflictException(...).
   Leave the already-Sold branch unchanged. This is what turns the currently-red
   scenario green.
2. Domain — ShoppingCart.PurchaseComplete(): convert void -> Result, following
   the 0005 SeatsReserve() template. Ensure.NotEmpty(ClientId) stays a throw,
   evaluated first; already PurchaseCompleted -> idempotent Result.Success() with
   no event; SeatsReserved -> transition + ShoppingCartPurchaseDomainEvent +
   Result.Success(); any other status -> ConflictError, no event. Stop calling
   EnsurePurchaseIsNotCompleted() (leave that shared guard unchanged).
3. Application — PurchaseTicketsCommandHandler: consume cart.PurchaseComplete()'s
   Result and short-circuit on IsFailure BEFORE SaveAsync, the cart-lifecycle
   DeleteAsync, and the per-seat DeleteAsync (atomicity). Remove the unconditional
   void call.
4. API — ShoppingCartEndpointApplicationBuilderExtensions: the purchase delegate
   return result; -> result.Match(() => Results.Ok(), ErrorResults.ToProblem);
   .Produces corrected to 200/404/409 (drop 201/204).
5. Docs (ADR-002 adoption close-out — this is the final write-path conversion):
   - docs/adr/ADR-002-...: status Proposed -> Accepted, dated 2026-06-04.
   - agent_docs/error_handling.md: rewrite "two models coexist / undecided" to
     the decided hybrid; name the intentional exception tails.
   - CLAUDE.md: amend rule #9 + the project-at-a-glance line to "decided — see
     ADR-002". Surgical wording change only; touch no other rule or the
     locked-stack table.

Confirm the open question in requirements.md before finishing (purchase requires a
prior reservation, so PurchaseComplete() on InWork is a ConflictError — assumed in
the gate and the domain tests).

Once the gate is green — write the missing unit tests per the plan
(see plan.md "Tests planned" and agent_docs/testing.md):
- ShoppingCarts/ShoppingCartSpecification.cs: add PurchaseComplete facts
  (SeatsReserved => PurchaseCompleted + event; already PurchaseCompleted =>
  Success, no event; InWork => ConflictError, no event).
- Seats/MovieSessionSeatSpecification.cs (new): Sell "another cart" => ConflictError;
  already-Sold => ConflictError.
- Re-run 0004 SelectSeatCommandHandlerTests and 0005 ReserveTicketsCommandHandlerTests
  unchanged as regression gates.

Quality gates before completion (run from src/services, x86 SDK at
"C:\Program Files (x86)\dotnet\dotnet.exe" via PowerShell; NuGet audit trips the
accepted AutoMapper NU1903 at restore — handle it so real warnings are validated;
dotnet format is known to reformat ReserveSeatsCommandValidatorSpecification.cs —
scope the format or git-checkout that file):
- dotnet format CinemaBookingManagement.sln
- dotnet build  CinemaBookingManagement.sln -warnaserror
- dotnet test   CinemaBookingManagement.sln
  (includes BookingManagementService.Domain.ArchitectureTests)

No EF Core model change in this slice => no migration.

All must pass with no new warnings or architecture-test failures.
