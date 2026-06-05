Implement slice cart_ownership_authorization (0009, module platform). All specs and the red acceptance-gate test are ready.

Sources (read in this order):
- specs/features/platform/0009_cart_ownership_authorization/plan.md
- specs/features/platform/0009_cart_ownership_authorization/requirements.md
- specs/features/platform/0009_cart_ownership_authorization/tests.md
- specs/features/platform/0009_cart_ownership_authorization/validation.md

Acceptance gate (RED now):
- BookingManagement/tests/BookingManagementService.Domain.UnitTests/ShoppingCarts/CartOwnershipBehaviourTests.cs
  must turn GREEN (dotnet test --filter "FullyQualifiedName~CartOwnershipBehaviourTests").
- It is currently a build-failure red: ICartScopedRequest, ICurrentUser, and CartOwnershipBehaviour do not exist yet.
- Do not touch the test file. If it fails due to a bug in the implementation — fix the implementation, not the test.
- If it fails due to a defect in the test itself — stop and ask, do not silently fix it.
- The test pins the behaviour constructor as CartOwnershipBehaviour(IActiveShoppingCartRepository carts, ICurrentUser currentUser); keep that shape.

Implement per plan.md section 5 (summary):
1. Application/Abstractions/ICurrentUser.cs — bool IsAuthenticated; Guid ClientId (Guid.Empty when anonymous). Framework-free.
2. Application/Common/Behaviours/ICartScopedRequest.cs — Guid ShoppingCartId.
3. Application/Common/Behaviours/CartOwnershipBehaviour.cs — IPipelineBehavior<TRequest,TResponse> where TRequest : ICartScopedRequest; two-mode rule (not-found => next; anonymous ClientId==Guid.Empty => next; assigned => require IsAuthenticated && ClientId==cart.ClientId, else throw ForbiddenAccessException; next not invoked on throw).
4. PurchaseTicketsCommand and GetShoppingCartQuery — add ", ICartScopedRequest" only (no field/contract change).
5. Application/ConfigureServices.cs — cfg.AddOpenBehavior(typeof(CartOwnershipBehaviour<,>)) AFTER ValidationBehaviour and BEFORE IdempotentCommandPipelineBehaviour (ADR-003-sanctioned single line; not a new ADR).
6. API/Authentication/CurrentUser.cs — ICurrentUser over IHttpContextAccessor, reading the same nameidentifier claim ("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier") as GetClientId; missing/non-Guid => anonymous (no throw).
7. API/ConfigureApiServices.cs (AddApiServices) — services.AddHttpContextAccessor(); services.AddScoped<ICurrentUser, CurrentUser>();
8. ShoppingCartEndpointApplicationBuilderExtensions — add .Produces(403) to the PurchaseSeats and GetShoppingCartById endpoints; do NOT add .RequireAuthorization().
9. API/ProgramExtensions.cs (new) — public partial class Program { } (for the WAF harness; do not edit Program.cs).

Reuse the existing ForbiddenAccessException and its existing CustomExceptionHandler 403 mapping — introduce NO new exception/Error type and make NO change to CustomExceptionHandler, the Result/Error types, the validation/idempotency behaviours, or any base type.

After the acceptance gate is green — write the remaining tests per plan.md section 6 and agent_docs/testing.md:
- CustomExceptionHandler mapping test: ForbiddenAccessException => 403 ProblemDetails (mirror slice 0008's DefaultHttpContext mapping tests, in BookingManagementService.API.UnitTests).
- CurrentUserTests (API.UnitTests/Authentication): valid nameidentifier => authenticated + parsed ClientId; missing/non-Guid => anonymous (Guid.Empty), no throw.
- The first WebApplicationFactory<Program> end-to-end harness (new integration-test project) implementing tests.md scenarios 1-5 (owner 200, stranger 403, stranger-purchase 403 + no side effect, not-found 404, anonymous 200) with a test authentication handler injecting the nameidentifier claim. NOTE: Program.cs performs real startup I/O (InitialiseDatabaseAsync, RabbitMQ SubscribeAsync, hosted services, OTLP exporters); the factory must override the cart Redis (real test Redis/Testcontainers or an in-memory IConnectionMultiplexer substitute), stub IEventBus, and disable the hosted services/exporters so the host boots. The test DB / Redis / RabbitMQ strategy is an open question in plan.md section 8 — decide it here (a new dependency such as Testcontainers needs explicit approval, per CLAUDE.md locked stack).
- The repository/adapter-translation level is opted out (no infrastructure-exception translation added) — see plan.md section 6.

Quality gates before completion (run from src/services, via the PowerShell tool against the "C:\Program Files (x86)\dotnet" SDK; the build trips the accepted AutoMapper NU1903 advisory and pre-existing nullable debt — scope so only NEW warnings are validated, per MEMORY warnaserror-baseline-debt):
- dotnet format CinemaBookingManagement.sln
- dotnet build  CinemaBookingManagement.sln -warnaserror
- dotnet test   CinemaBookingManagement.sln
  (includes BookingManagementService.Domain.ArchitectureTests — the Application layer must stay framework-free)

No EF Core schema change in this slice — no migration.

All must pass with no new warnings or architecture-test failures.
