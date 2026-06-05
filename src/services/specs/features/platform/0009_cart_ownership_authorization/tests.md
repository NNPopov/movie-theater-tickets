# 0009 · CartOwnershipAuthorization — Outside-in test spec

## Goal

Prove end-to-end, over HTTP through the full pipeline, that once a cart is **assigned** the
object-level guard refuses any caller who is not its owner — a stranger calling `getById` or
`purchase` on an assigned cart receives **403** (and the purchase side effects do not fire) —
while the **owner**, the **anonymous-cart guest**, and the **not-found** contract are
unchanged.

> This is the first `WebApplicationFactory<Program>` harness in the repository (a deliverable
> of this slice). Because the slice's deep module is the *pipeline behaviour*, this HTTP
> outside-in suite is the end-to-end gate; the five-branch logic matrix is additionally pinned
> at unit level by `CartOwnershipBehaviourTests` (see `plan.md` §6).

## Entry points

Two existing endpoints, both **anonymous** (no `RequireAuthorization`):

- **Query:** `GET /api/shoppingcarts/{ShoppingCartId}` → `ShoppingCartDto`.
- **Command:** `POST /api/shoppingcarts/{ShoppingCartId}/purchase` → 200 / `ProblemDetails`.

Seeding uses the existing black-box endpoints:

- `POST /api/shoppingcarts` with header `X-Idempotency-Key: <guid>` and body
  `{ "maxNumberOfSeats": 4 }` → creates an **anonymous** cart (`ClientId == Guid.Empty`),
  returns its id.
- `PUT /api/shoppingcarts/{ShoppingCartId}/assignclient` authenticated as user **A** → assigns
  the cart to A (`ClientId == A`), per slice `0003`.

**Identity header (test scheme):** `X-Test-User: <userGuid>` selects the authenticated caller
(see Fixtures). Omitting it ⇒ anonymous request.

## Wired real

- ASP.NET Core app via `WebApplicationFactory<Program>` — full middleware stack including
  `app.UseExceptionHandler` + `CustomExceptionHandler`.
- The MediatR pipeline in registration order: `ValidationBehaviour<,>`, **`CartOwnershipBehaviour<,>`
  (the unit under test, wired live)**, `IdempotentCommandPipelineBehaviour<,>`.
- `ICurrentUser` → `CurrentUser` over `IHttpContextAccessor`, fed by the test authentication
  handler (so the real claim-reading path is exercised).
- `GetShoppingCartQueryHandler`, `PurchaseTicketsCommandHandler`, and the
  `CreateShoppingCart` / `AssignClientCart` use-cases used for seeding (all real).
- `IActiveShoppingCartRepository` → `ActiveShoppingCartRepository` over **Redis** (the cart
  store; a real test Redis — Testcontainers — or a deterministic in-memory `IConnectionMultiplexer`
  substitute). The cart is loaded by the behaviour and the handlers from this store.

## Mocked / stubbed (true external boundaries only)

- **RabbitMQ event bus (`IEventBus`)** — no-op stub; the harness must not require a broker.
  (No integration-event assertion is made by these scenarios.)
- **Hosted background services** (`RedisSubscriber`, `TimeWorker`) and the OpenTelemetry OTLP
  exporters — disabled/no-op in the factory so the app boots without the external collectors.
- **Keycloak/JWT** — replaced by the test authentication scheme below; no real IdP.
- **Distributed lock / clock** — not exercised by these scenarios: `getById` takes no lock, and
  the `purchase`-by-stranger case is rejected by the behaviour **before** the handler runs, so
  no lock/seat/Postgres path is reached. Leave them real or no-op; no scenario depends on them.

If a real Redis is impractical in the chosen CI, substitute a deterministic in-memory
`IConnectionMultiplexer`; the seeding-via-HTTP and assertion-via-HTTP flow is unchanged.

## Fixtures / setup

- **`BookingApiFactory : WebApplicationFactory<Program>`** — overrides the cart Redis
  connection to the test instance, stubs `IEventBus`, removes the hosted services and OTLP
  exporters, and registers the test authentication scheme as the default.
- **Test authentication handler** — an `AuthenticationHandler<>` registered as the default
  scheme that, when the request carries `X-Test-User: <guid>`, produces an authenticated
  `ClaimsPrincipal` with the claim
  `"http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier" = <guid>` (the exact
  claim `CurrentUser`/`GetClientId` read); with no header, it produces **no** authenticated
  result (anonymous). Define two ids: `A` (owner) and `B` (stranger).
- **Seeding helpers (black-box, over HTTP):**
  - `CreateAnonymousCartAsync()` → `POST /api/shoppingcarts` with a fresh `X-Idempotency-Key`
    and no `X-Test-User`; returns the new cart id (`ClientId == Guid.Empty`).
  - `CreateAssignedCartAsync(A)` → create as above, then
    `PUT /api/shoppingcarts/{id}/assignclient` with `X-Test-User: A`; returns the cart id now
    owned by A.
- **State reset:** flush the test Redis keyspace (and truncate Postgres if touched) between
  tests; no scenario relies on another's leftover state.

## Test scenarios

### Scenario 1: owner reads own assigned cart ⇒ 200 (pass-through)

**Setup:**
- `cartId = CreateAssignedCartAsync(A)`.

**Act:**
- `GET /api/shoppingcarts/{cartId}` with `X-Test-User: A`.

**Expect:**
- HTTP `200`.
- Body is a `ShoppingCartDto` with `id == cartId` and `isAssigned == true`.

**Covers:** F7, F14, F15.

### Scenario 2: stranger reads someone else's assigned cart ⇒ 403 (the core protection)

**Setup:**
- `cartId = CreateAssignedCartAsync(A)`.

**Act:**
- `GET /api/shoppingcarts/{cartId}` with `X-Test-User: B`.

**Expect:**
- HTTP `403`.
- Body is a `ProblemDetails` with `status == 403`, `title == "Forbidden"`,
  `type == "https://tools.ietf.org/html/rfc7231#section-6.5.3"`.
- The response is **not** a `ShoppingCartDto` (no seats/prices/status leaked).

**Covers:** F8, F9, F14.

### Scenario 3: stranger purchases someone else's assigned cart ⇒ 403, no side effect

**Setup:**
- `cartId = CreateAssignedCartAsync(A)`.

**Act:**
- `POST /api/shoppingcarts/{cartId}/purchase` with `X-Test-User: B`.

**Expect:**
- HTTP `403` (`ProblemDetails` as in Scenario 2).
- **State side-effect assertion (Redis cart store):** the cart still exists and is unchanged —
  re-reading `GET /api/shoppingcarts/{cartId}` as `X-Test-User: A` still returns `200` with
  `isAssigned == true`. The purchase side effects (cart deletion / seat finalization) did
  **not** fire, proving the behaviour blocked the handler before any mutation.

**Covers:** F8, F13.

### Scenario 4: not-found cart ⇒ 404, not 403 (existence not leaked)

**Setup:**
- `missingCartId` = a random Guid never created.

**Act:**
- `GET /api/shoppingcarts/{missingCartId}` with `X-Test-User: B`.

**Expect:**
- HTTP `404` (`ContentNotFoundException` → `ProblemDetails`, per
  `agent_docs/error_handling.md`) — **not** `403`. The authorization layer passes a missing
  cart through to the handler's existing not-found contract.

**Covers:** F5, F14.

### Scenario 5: guest on an anonymous cart ⇒ 200 (capability preserved)

**Setup:**
- `cartId = CreateAnonymousCartAsync()` (`ClientId == Guid.Empty`).

**Act:**
- `GET /api/shoppingcarts/{cartId}` with **no** `X-Test-User` header (anonymous request).

**Expect:**
- HTTP `200`.
- Body is a `ShoppingCartDto` with `isAssigned == false`. The guest flow on an anonymous cart
  is unaffected by the new check.

**Covers:** F6, F14, F15.

## Out of scope for this test

- The five-branch behaviour matrix at unit level with mocked repository + `ICurrentUser`
  (covered by `CartOwnershipBehaviourTests`, the unit gate).
- The `ForbiddenAccessException → 403 ProblemDetails` mapping in isolation (covered by the
  `CustomExceptionHandler` mapping test).
- `CurrentUser` claim parsing in isolation — valid vs missing/non-Guid (covered by
  `CurrentUserTests`).
- Field-level / structural validation (`400 ValidationProblemDetails` via
  `ValidationBehaviour`).
- A full owner-`purchase`-success scenario with seat seeding and the Redis lock lifecycle —
  the legitimate purchase path is unchanged by this slice and is covered by the existing
  purchase coverage; Scenarios 1 and 3 already prove the guard's pass-through and block for the
  command/query without re-testing the purchase business logic.
- Performance, load, and concurrency.
