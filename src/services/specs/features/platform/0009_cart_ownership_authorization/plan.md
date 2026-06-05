# 0009 · CartOwnershipAuthorization — Implementation plan

## 1. Header

- **Aggregate / Module:** `platform` (cross-cutting artifacts), touching `ShoppingCarts`.
- **Slice:** `0009_cart_ownership_authorization`
- **PRD:** ./prd.md
- **Reference slice (if any):** **No shape-match exists.** This slice is not a
  command/query use-case; it adds a **MediatR pipeline behaviour**. The prior art for the
  *shape* is the existing `ValidationBehaviour<,>` /
  `IdempotentCommandPipelineBehaviour<,>`
  (`Application/Common/Behaviours/`). For the central-handler mapping test, the reference is
  slice `0008` (`../0008_endpoint_input_guards/`). For HTTP-status conversions, the
  reference series is `0003`–`0007`.
- **HTTP paths affected (behaviour-adding, contracts unchanged otherwise):**
  - `POST /api/shoppingcarts/{ShoppingCartId}/purchase`
  - `GET  /api/shoppingcarts/{ShoppingCartId}`
- **STABLE files touched (all are single-line registrations / metadata — NOT mechanism
  changes; explicitly sanctioned by ADR-003 option #1):**
  - `Application/ConfigureServices.cs` — **one** `cfg.AddOpenBehavior(typeof(CartOwnershipBehaviour<,>))`
    line, registering a new pipeline behaviour. Registering a behaviour is normally a
    stable-pipeline touch, but ADR-003 (the source of this slice) *prescribes exactly this
    behaviour as its preferred remediation*, so it is a sanctioned feature addition, **not a
    new ADR**. The behaviour mechanism (`IPipelineBehavior<,>`) is unchanged.
  - `API/ConfigureApiServices.cs` (`AddApiServices`) — two registration lines:
    `services.AddHttpContextAccessor();` and
    `services.AddScoped<ICurrentUser, CurrentUser>();`.
  - `API/Endpoints/ShoppingCartEndpointApplicationBuilderExtensions.cs` — add `.Produces(403)`
    to the `purchase` and `getById` endpoints (OpenAPI metadata only; this is a feature
    endpoint group, not stable plumbing).
  - `API/Program.cs` is **not** edited: the WAF harness needs `Program` to be a `public
    partial class`, which is added in a **new** standalone file (`API/ProgramExtensions.cs`
    or `API/Program.Partial.cs`) to avoid touching the composition root.
  - **No change** to `CustomExceptionHandler`, the `Result`/`Error` types, base types, the
    validation/idempotency behaviours, or any exception/`Error` hierarchy. No new
    `*Exception`/`Error` type is introduced.

## 2. Context summary

This slice builds the **central object-level authorization mechanism** for cart-scoped
use-cases — a MediatR pipeline behaviour, `CartOwnershipBehaviour` — and applies it to the
first two assigned-cart operations: `purchase` (a command returning `Result`) and `getById`
(a query returning the cart). A request opts in by implementing the new marker interface
`ICartScopedRequest` (exposes `Guid ShoppingCartId`). The behaviour loads the cart and
applies the ADR-003 **two-mode rule**: an **anonymous** cart (`ClientId == Guid.Empty`)
passes through untouched (the guest capability model is preserved); an **assigned** cart
(`ClientId != Guid.Empty`) requires the caller to be authenticated **and** to equal the
owner, else the behaviour **throws `ForbiddenAccessException`** (already mapped to `403` by
`CustomExceptionHandler`); a **not-found** cart passes through so the handler still produces
its existing `404`. Caller identity is read through a new `ICurrentUser` abstraction
(interface in `Application`, implementation in the API over `IHttpContextAccessor`, reading
the same `nameidentifier` claim as the endpoints' `GetClientId`), keeping `Application`
framework-free. The two endpoints stay anonymous (no `RequireAuthorization`); only their
OpenAPI gains `403`.

## 3. API contract

This slice adds **no new request/response models** and changes **no field**. It adds one
failure status to two existing operations.

### `POST /api/shoppingcarts/{ShoppingCartId}/purchase`

- **Request:** unchanged — `ShoppingCartId` from the route; no body. Command:
  `PurchaseTicketsCommand(Guid ShoppingCartId)` (now also `: ICartScopedRequest`).
- **Status codes:**

  | Status | Trigger | Source | Change |
  |---|---|---|---|
  | 200 | success | endpoint `result.Match(Results.Ok, …)` | unchanged |
  | **403** | **assigned cart, caller is not the owner or is unauthenticated** | **`CartOwnershipBehaviour` ⇒ `ForbiddenAccessException` ⇒ `CustomExceptionHandler`** | **new** |
  | 404 | cart not found / session not found | handler `NotFoundError` / `Result` | unchanged |
  | 409 | seat/state conflict | handler `ConflictError` | unchanged |

### `GET /api/shoppingcarts/{ShoppingCartId}`

- **Request:** unchanged — `ShoppingCartId` from the route. Query:
  `GetShoppingCartQuery(Guid ShoppingCartId)` (now also `: ICartScopedRequest`).
- **Response:** unchanged — `ShoppingCartDto` (200, `application/json`).
- **Status codes:**

  | Status | Trigger | Source | Change |
  |---|---|---|---|
  | 200 | success, returns `ShoppingCartDto` | endpoint | unchanged |
  | **403** | **assigned cart, caller is not the owner or is unauthenticated** | **`CartOwnershipBehaviour` ⇒ `ForbiddenAccessException` ⇒ `CustomExceptionHandler`** | **new** |
  | 404 | cart not found | handler `ContentNotFoundException` | unchanged |

The `403` body is the standard `ProblemDetails` already emitted by
`CustomExceptionHandler.HandleForbiddenAccessException` (Status 403, Title "Forbidden",
Type `…rfc7231#section-6.5.3`). Mapping per `agent_docs/error_handling.md`
(`ForbiddenAccessException ⇒ 403`). No new status code is invented.

## 4. File structure

```
BookingManagement/
├── BookingManagementService.Application/
│   ├── Abstractions/
│   │   └── ICurrentUser.cs                         # NEW — application port: caller identity
│   ├── Common/Behaviours/
│   │   ├── ICartScopedRequest.cs                   # NEW — marker: exposes Guid ShoppingCartId
│   │   └── CartOwnershipBehaviour.cs               # NEW — the pipeline behaviour (two-mode rule)
│   ├── ShoppingCarts/Command/PurchaseSeats/
│   │   └── PurchaseTicketsCommandHandler.cs        # MODIFY — record adds `, ICartScopedRequest`
│   ├── ShoppingCarts/Queries/
│   │   └── GetShoppingCartQueryHandler.cs          # MODIFY — record adds `, ICartScopedRequest`
│   └── ConfigureServices.cs                        # STABLE — +1 AddOpenBehavior line (ADR-003)
└── BookingManagementService.API/
    ├── Authentication/
    │   └── CurrentUser.cs                          # NEW — ICurrentUser impl over IHttpContextAccessor
    ├── ConfigureApiServices.cs                     # STABLE — +AddHttpContextAccessor + AddScoped<ICurrentUser,…>
    ├── Endpoints/
    │   └── ShoppingCartEndpointApplicationBuilderExtensions.cs  # MODIFY — .Produces(403) on purchase + getById
    └── ProgramExtensions.cs                        # NEW — `public partial class Program { }` for WAF<Program>
```

No EF Core entity, configuration, or migration changes — **no migration command** for this
slice. (The behaviour reuses the existing `IActiveShoppingCartRepository.GetByIdAsync`.)

## 5. Implementation steps

1. **Application — `ICurrentUser` port.** Create
   `Application/Abstractions/ICurrentUser.cs`:
   ```csharp
   namespace CinemaTicketBooking.Application.Abstractions;

   public interface ICurrentUser
   {
       bool IsAuthenticated { get; }
       Guid ClientId { get; }   // Guid.Empty when not authenticated
   }
   ```
   No framework types — keeps `Application` framework-free (Dependency Rule).

2. **Application — `ICartScopedRequest` marker.** Create
   `Application/Common/Behaviours/ICartScopedRequest.cs`:
   ```csharp
   namespace CinemaTicketBooking.Application.Common.Behaviours;

   public interface ICartScopedRequest
   {
       Guid ShoppingCartId { get; }
   }
   ```
   The positional `ShoppingCartId` on the two records already satisfies this property — opt-in
   is a one-token change with no contract impact.

3. **Application — `CartOwnershipBehaviour`.** Create
   `Application/Common/Behaviours/CartOwnershipBehaviour.cs` modelled on `ValidationBehaviour`:
   ```csharp
   public class CartOwnershipBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
       where TRequest : ICartScopedRequest
   {
       private readonly IActiveShoppingCartRepository _carts;
       private readonly ICurrentUser _currentUser;
       // ctor injects both

       public async Task<TResponse> Handle(TRequest request,
           RequestHandlerDelegate<TResponse> next, CancellationToken ct)
       {
           var cart = await _carts.GetByIdAsync(request.ShoppingCartId);

           // not found ⇒ let the handler own the 404 (do not leak existence as 403)
           if (cart is null)
               return await next();

           // anonymous cart ⇒ capability model preserved
           if (cart.ClientId == Guid.Empty)
               return await next();

           // assigned cart ⇒ strong ownership
           if (!_currentUser.IsAuthenticated || _currentUser.ClientId != cart.ClientId)
               throw new ForbiddenAccessException();

           return await next();
       }
   }
   ```
   Throws (not `Result`) so it composes uniformly over the command (`Result`) and the query
   (`ShoppingCart`), and reuses the existing central `403` mapping — per
   `agent_docs/error_handling.md` (cross-cutting guard ⇒ exception). The behaviour touches no
   `HttpContext`. `GetByIdAsync` has no `CancellationToken` parameter today; call it as-is
   (do not change the repository interface in this slice).

4. **Application — opt the command in.** In
   `ShoppingCarts/Command/PurchaseSeats/PurchaseTicketsCommandHandler.cs` change:
   ```csharp
   public record PurchaseTicketsCommand(Guid ShoppingCartId)
       : IRequest<Result>, ICartScopedRequest;
   ```
   No field, handler, or `Result` contract change.

5. **Application — opt the query in.** In
   `ShoppingCarts/Queries/GetShoppingCartQueryHandler.cs` change:
   ```csharp
   public record GetShoppingCartQuery(Guid ShoppingCartId)
       : IRequest<ShoppingCart>, ICartScopedRequest;
   ```
   No DTO change.

6. **Application — register the behaviour (STABLE, +1 line, ADR-003).** In
   `ConfigureServices.AddApplicationServices`, add the behaviour **after** `ValidationBehaviour`
   and **before** `IdempotentCommandPipelineBehaviour` so authorization runs before any
   idempotency record is created:
   ```csharp
   cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehaviour<,>));
   cfg.AddOpenBehavior(typeof(CartOwnershipBehaviour<,>));        // NEW (ADR-003 §option-1)
   cfg.AddOpenBehavior(typeof(IdempotentCommandPipelineBehaviour<,>));
   ```
   (Open-generic registration with an interface constraint only resolves for requests
   implementing `ICartScopedRequest`; all other requests skip it.)

7. **API — `ICurrentUser` implementation.** Create
   `API/Authentication/CurrentUser.cs` over `IHttpContextAccessor`, reading the **same** claim
   string the endpoints' `GetClientId` reads
   (`"http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier"`, i.e.
   `ClaimTypes.NameIdentifier`):
   ```csharp
   namespace CinemaTicketBooking.Api.Authentication;

   public sealed class CurrentUser(IHttpContextAccessor accessor) : ICurrentUser
   {
       private const string NameIdentifier =
           "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier";

       public bool IsAuthenticated => TryGetClientId(out _);
       public Guid ClientId => TryGetClientId(out var id) ? id : Guid.Empty;

       private bool TryGetClientId(out Guid clientId)
       {
           var raw = accessor.HttpContext?.User.FindFirst(NameIdentifier)?.Value;
           return Guid.TryParse(raw, out clientId);   // missing/non-Guid ⇒ anonymous
       }
   }
   ```
   Mirrors `GetClientId` parsing, but returns *anonymous* (no throw) on a missing/non-Guid
   claim — the conditional check, not the identity reader, decides `403`.

8. **API — register identity (STABLE, +2 lines).** In
   `ConfigureApiServices.AddApiServices`:
   ```csharp
   services.AddHttpContextAccessor();
   services.AddScoped<ICurrentUser, CurrentUser>();
   ```

9. **API — OpenAPI only.** In `ShoppingCartEndpointApplicationBuilderExtensions`, add
   `.Produces(403)` to the `PurchaseSeats` and `GetShoppingCartById` endpoint builders. Do
   **not** add `.RequireAuthorization()` — both endpoints stay anonymous so guest flows keep
   working and the strong check stays conditional on assignment.

10. **API — expose `Program` for the WAF harness.** Add a new file
    `API/ProgramExtensions.cs` containing only `public partial class Program { }` so
    `WebApplicationFactory<Program>` can reference the entry point. (Program.cs itself is not
    edited.)

11. **Verify (per `CLAUDE.md`, via the PowerShell tool against the
    `Program Files (x86)\dotnet` SDK; scope to surface only *new* warnings — see MEMORY
    `warnaserror-baseline-debt`):**
    ```
    dotnet format CinemaBookingManagement.sln
    dotnet build  CinemaBookingManagement.sln -warnaserror
    ```

## 6. Tests planned

The PRD opts into **all four** levels. Note the unusual mapping: because the slice's deep
module is the *behaviour*, the **acceptance / RED gate is the behaviour unit test**, and the
end-to-end HTTP level is the *new* WAF harness.

- **Behaviour unit test (the acceptance gate / RED) —**
  `tests/BookingManagementService.Domain.UnitTests/ShoppingCarts/CartOwnershipBehaviourTests.cs`
  (NSubstitute, AAA, alongside `PurchaseTicketsCommandHandlerTests` /
  `ShoppingCartSpecification`). Mocks `IActiveShoppingCartRepository` + `ICurrentUser`. Cases:
  anonymous cart ⇒ `next` invoked (pass); assigned + caller == owner ⇒ pass; assigned +
  different authenticated caller ⇒ `ForbiddenAccessException`, `next` **not** invoked; assigned
  + unauthenticated caller ⇒ `ForbiddenAccessException`; cart not found ⇒ pass. **RED until the
  behaviour exists** (build error: type not found). This is the `/slice-test-red` artifact.
- **Central mapping pin —**
  `tests/BookingManagementService.API.UnitTests/Infrastructure/…` mirroring slice `0008`'s
  `CustomExceptionHandler*` tests (`DefaultHttpContext`, assert `403` + `ProblemDetails` body)
  for `ForbiddenAccessException`. Pins the existing mapping this slice now depends on.
- **`ICurrentUser` implementation unit test —**
  `tests/BookingManagementService.API.UnitTests/Authentication/CurrentUserTests.cs`: valid
  `nameidentifier` ⇒ `IsAuthenticated == true`, `ClientId` parsed; missing / non-Guid claim ⇒
  `IsAuthenticated == false`, `ClientId == Guid.Empty`. Uses a stubbed `IHttpContextAccessor`
  with a `DefaultHttpContext` carrying a `ClaimsPrincipal`.
- **End-to-end via `WebApplicationFactory<Program>` (NEW harness) —** a new integration test
  project (e.g. `tests/BookingManagementService.API.IntegrationTests/`) standing up the first
  `WebApplicationFactory<Program>` in the repo, with a test authentication handler that injects
  the `nameidentifier` claim. Covers `purchase` and `getById`: stranger on an assigned cart ⇒
  `403` + `ProblemDetails`; owner ⇒ success; anonymous cart ⇒ success; non-existent cart ⇒
  `404`. The exact project layout, test-auth handler, and test-database strategy are detailed
  by `/feature-tests` and `/slice-test-red`.

**Opt-outs:** the standard *repository/adapter translation* level is **N/A** for this slice —
it introduces no infrastructure-exception translation (no new `DbUpdateException` mapping; the
behaviour only reads via the existing `GetByIdAsync`). The four covered levels are the
behaviour gate, the central-mapping pin, the `ICurrentUser` unit, and the WAF end-to-end.

## 7. Out of scope for this slice

- Applying the mechanism to `select` / `unselect` / `reserve` / `unreserve` or the SignalR hub
  methods (later ADR-003 slices reuse `CartOwnershipBehaviour` + `ICurrentUser`).
- Hardening `assignclient` against adopting a stranger's anonymous cart.
- Validating the `HashId` (instead of the raw id) for the anonymous capability phase; logging
  hygiene / raw-cart-id redaction.
- The final `403`-vs-`404` disclosure-policy decision (this slice keeps not-found ⇒ `404`).
- Adding `RequireAuthorization` to the two endpoints; any change to the guest flow.
- Any change to `Result`/`Result<T>`, `CustomExceptionHandler`, the validation/idempotency
  behaviours, or any base/exception/`Error` type. No new cross-cutting type.
- Adding a `CancellationToken` to `IActiveShoppingCartRepository.GetByIdAsync`.
- Flipping ADR-003 to Accepted.

## 8. Open questions

- **Pipeline order vs idempotency.** Plan chooses **Validation → CartOwnership → Idempotency
  → handler** so an unauthorized caller never creates an idempotency record. Neither of *this
  slice's* two requests is idempotent (no `RequestId`), so the order is presently
  observationally neutral; the choice is for future cart-scoped commands. Confirm acceptable.
- **WAF test-database & test-auth strategy.** Standing up the first
  `WebApplicationFactory<Program>` requires deciding the test database (real Postgres vs a
  test container vs an in-pipeline substitute) and a test authentication scheme for the
  `nameidentifier` claim. Deferred to `/feature-tests` / `/slice-test-red`; flagged here
  because it is the largest unknown in the slice.
- Otherwise: **None.**
