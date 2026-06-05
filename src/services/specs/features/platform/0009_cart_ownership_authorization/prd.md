# PRD — Shopping-cart object-level authorization: the `CartOwnershipBehaviour` mechanism (ADR-003, slice 1)

Slice: `0009_cart_ownership_authorization` · Module: `platform` · Status: 📋 Started

## Problem Statement

As a cinema customer who has logged in and had a shopping cart assigned to me, I have **no
protection** against another person acting on *my* cart once they learn its id. Every
cart-scoped operation in the BookingManagement service identifies the cart by a
**caller-supplied `ShoppingCartId`** (a GUID) and performs **no check that the caller is
allowed to act on that specific cart**. Authentication, where it is required at all, proves
*who you are*; it never proves *this cart is yours*. This is the OWASP API Security Top-10 #1
class — **Broken Object Level Authorization (BOLA) / IDOR** — analysed in full in
**ADR-003**.

Concretely, on the two operations this slice targets:

- **`POST /api/shoppingcarts/{id}/purchase`** has no authentication and no ownership check. It
  only requires the cart to be *assigned* (non-empty `ClientId`). Anyone who learns the id of
  an assigned cart can drive its purchase-completion side effects.
- **`GET /api/shoppingcarts/{id}`** has no authentication and no ownership check. It returns the
  full `ShoppingCartDto` of *any* cart — selected seats, prices, status, assignment flag — to
  anyone holding the id (information disclosure).

The cart id is a 122-bit random GUID, so the *anonymous, guest* phase legitimately treats the
id as an unguessable **capability** (a bearer token). That is **by design** and must keep
working without authentication. The defect is that the **authenticated** phase inherits the
same "whoever passes the id wins" behaviour: once a cart has an owner, nothing verifies the
caller *is* that owner. The capability also leaks through ordinary channels (logs, URLs, shared
devices), so "unguessable id" is security-by-obscurity, not authorization.

The prerequisite for any ownership check — that `cart.ClientId` actually holds the **real
owner** — was a bug (the handler assigned the cart's own id as the owner). That bug is
**already fixed** (commit `4a72be7`, slice `0003`) and pinned by
`AssignClientCartCommandHandlerTests`, so `cart.ClientId == caller` can now be implemented
correctly. This slice builds that check.

## Solution

Introduce a **single, central object-level authorization mechanism** for cart-scoped use-cases
— a MediatR pipeline behaviour — and apply it to exactly two entry points (`purchase`,
`getById`) as the first, canonical slice. Other cart-scoped operations (select / unselect /
reserve / unreserve, the SignalR hub methods, `assignclient` hardening) adopt the same
mechanism in **later slices** tracked by ADR-003.

After this slice:

- A new MediatR pipeline behaviour, **`CartOwnershipBehaviour`**, runs before the handler for
  any request that opts in by implementing a small marker interface, **`ICartScopedRequest`**
  (which exposes `ShoppingCartId`). The behaviour is the `Result`-era analogue of
  `ValidationBehaviour`: a cross-cutting guard that runs in the pipeline and **throws** when the
  rule is violated, so handlers stay free of authorization logic.
- The behaviour loads the cart by `ShoppingCartId` and applies the ADR-003 **two-mode ownership
  rule**:
  - **Anonymous cart** (`ClientId == Guid.Empty`): the capability model is preserved — the
    request passes through unchanged. (Hardening the anonymous phase to validate the `HashId`
    instead of the raw id is **explicitly a later slice**.)
  - **Assigned cart** (`ClientId != Guid.Empty`): strong ownership — the caller must be
    authenticated and the caller's id must equal `cart.ClientId`. Otherwise the behaviour throws
    `ForbiddenAccessException`.
  - **Cart not found**: the behaviour passes through and lets the handler produce the existing
    `404`, so authorization does not change the not-found contract or leak existence as a `403`.
    (The broader `403`-vs-`404` *disclosure* policy is an open question deferred to a later
    slice.)
- The caller's identity reaches the behaviour through a new, framework-thin abstraction,
  **`ICurrentUser`** (interface in `Application`; implementation in the API/composition root via
  `IHttpContextAccessor`). It reads the **same** `nameidentifier` claim the endpoints' existing
  `GetClientId` helper reads, so "who is the caller" is decided one way across the service. This
  keeps the behaviour in the `Application` layer **framework-free** (Dependency Rule intact) and
  makes the identity reusable by the future hub slice.
- A violation surfaces as **HTTP 403 Forbidden**. No new exception type and **no change to the
  stable mechanism** are needed: `ForbiddenAccessException` **already exists**
  (`Application.Exceptions`) and is **already mapped to 403** by `CustomExceptionHandler`. This
  slice merely starts *throwing* it. Throwing (rather than returning a `Result`) is the correct
  model: an authorization breach is a cross-cutting guard failure, like structural validation —
  not an expected business outcome of the use-case — and it works uniformly for the command
  (`purchase`, returns `Result`) and the query (`getById`, returns a DTO).

The two endpoints **stay anonymous** (no `.RequireAuthorization()` added): guests must still act
on guest carts. The strong check is **conditional** on the cart already having an owner, exactly
as ADR-003 requires.

This is a **behaviour-adding** change on an existing path. Observable effect: acting on an
**assigned** cart you do **not** own changes from `200`/`200 + body` to **`403`**; acting on an
**anonymous** cart, or on your **own** assigned cart, is **unchanged**. It follows the
"modifying an existing slice" flow (update `tests.md` → red → green).

ADR-003 stays **Proposed**; this slice implements its first remediation step (option #1, the
preferred pipeline behaviour). Flipping the ADR to Accepted is reserved to the Decider and is not
part of this slice.

## User Stories

1. As a logged-in cinema customer, I want a stranger who knows my assigned cart's id to be unable to read it via `GET /{id}`, so that my seats, prices, and status stay private.
2. As a logged-in cinema customer, I want a stranger who knows my assigned cart's id to be unable to trigger `POST /{id}/purchase` on it, so that nobody can drive my cart's purchase side effects.
3. As a logged-in cinema customer, I want only *me* to act on my assigned cart, so that ownership actually means something once I have logged in.
4. As a guest (anonymous) customer, I want to keep using my cart by its id without logging in, so that the guest checkout flow is unaffected by the new check.
5. As a guest customer, I want `purchase` and `getById` on an anonymous cart to behave exactly as before, so that no guest scenario regresses.
6. As a logged-in customer acting on my own assigned cart, I want `purchase` and `getById` to behave exactly as before, so that the legitimate authenticated path is unchanged.
7. As an attacker calling `GET /{id}` on someone else's assigned cart, I want to receive `403 Forbidden`, so that the service refuses object-level access I am not entitled to.
8. As an attacker calling `POST /{id}/purchase` on someone else's assigned cart, I want to receive `403 Forbidden`, so that the operation is refused.
9. As an unauthenticated caller acting on an *assigned* cart, I want to receive `403 Forbidden`, so that an owned cart cannot be operated anonymously.
10. As a client developer, I want a `403` from these endpoints to carry a `ProblemDetails` body identical in shape to every other error in the service, so that my client parses one response shape.
11. As a client developer, I want a request for a non-existent cart to still return `404 Not Found` (not `403`), so that the not-found contract is unchanged by the authorization layer.
12. As a service developer, I want object-level authorization enforced in **one** place (a pipeline behaviour) rather than duplicated per handler/endpoint, so that the next cart-scoped operation cannot forget it.
13. As a service developer, I want a use-case to opt into the check by implementing a small marker interface that exposes `ShoppingCartId`, so that participation is explicit and type-checked.
14. As a service developer, I want the authorization behaviour to **throw** `ForbiddenAccessException` (not return a `Result`), so that it composes uniformly over both commands and queries and reuses the existing central 403 mapping.
15. As a service developer, I want to reuse the existing `ForbiddenAccessException` and its existing `CustomExceptionHandler` 403 mapping, so that this slice adds no new cross-cutting exception type and does not touch the stable error mechanism.
16. As an architect, I want the behaviour to obtain the caller's identity through an `ICurrentUser` abstraction defined in `Application` and implemented in the composition root, so that the `Application` layer stays framework-free and the Dependency Rule is honoured.
17. As an architect, I want `ICurrentUser` to read the same `nameidentifier` claim the endpoints' `GetClientId` helper reads, so that "who is the caller" is decided consistently in exactly one way.
18. As a future maintainer, I want `ICurrentUser` and `CartOwnershipBehaviour` to be reusable by the SignalR hub and by the remaining cart-scoped endpoints, so that later ADR-003 slices are thin adoptions rather than re-designs.
19. As a domain reasoner, I want the strong ownership check to apply **only** when `cart.ClientId != Guid.Empty`, so that anonymous guest carts remain a pure capability model.
20. As a security reviewer, I want the anonymous capability phase left intact in this slice (HashId hardening deferred), so that the slice stays small and does not change the guest contract prematurely.
21. As an API consumer reading OpenAPI, I want the `purchase` and `getById` endpoints to declare `403` among their responses, so that the published contract matches runtime behaviour.
22. As a maintainer, I want `PurchaseTicketsCommand` and `GetShoppingCartQuery` to simply implement `ICartScopedRequest` (they already carry `ShoppingCartId`), so that adoption is a one-line opt-in with no contract change to the command/query.
23. As a maintainer, I want the two endpoints to remain anonymous (no `RequireAuthorization`), so that guest flows keep working and the check stays conditional on assignment.
24. As a maintainer, I want the `CartOwnershipBehaviour` registered as an open-generic MediatR pipeline behaviour in the correct order (after validation, before the handler), so that only well-formed, authorized requests reach the handler.
25. As a maintainer, I want the behaviour pinned by a focused unit test as the acceptance gate, covering: anonymous cart ⇒ pass; assigned + matching caller ⇒ pass; assigned + different caller ⇒ `ForbiddenAccessException`; assigned + anonymous caller ⇒ `ForbiddenAccessException`; cart not found ⇒ pass.
26. As a maintainer, I want a test pinning that `ForbiddenAccessException` maps to `403` + `ProblemDetails` in `CustomExceptionHandler`, so that the central mapping this slice relies on is regression-protected.
27. As a maintainer, I want the `ICurrentUser` implementation unit-tested (valid `nameidentifier` ⇒ authenticated client id; missing/non-Guid ⇒ anonymous), so that the identity source is verified independently of the behaviour.
28. As a maintainer, I want an end-to-end `WebApplicationFactory<Program>` test for `purchase` and `getById` (stranger on an assigned cart ⇒ `403`; owner ⇒ success; anonymous cart ⇒ success), so that routing, the pipeline order, and `CustomExceptionHandler` translation are verified together.
29. As a maintainer, I want this slice to stand up the first `WebApplicationFactory<Program>` integration harness, so that subsequent ADR-003 adoption slices and other endpoint tests have an established end-to-end gate.
30. As a maintainer, I want the architecture tests (`Domain.ArchitectureTests`) and the full suite to stay green, so that the new abstraction and behaviour honour the structural rules (Application stays framework-free; the behaviour does not touch `HttpContext`).
31. As a service developer, I want this slice limited to `purchase` and `getById`, with the other cart-scoped operations and the hub deferred, so that the mechanism lands small, reviewable, and reusable.

## Implementation Decisions

**Nature of the slice.** ADR-003 **slice 1**: build the central object-level authorization
mechanism and apply it to two assigned-cart operations. It is a `ShoppingCarts` security change
that *also* produces cross-cutting **platform** artifacts (`CartOwnershipBehaviour`,
`ICartScopedRequest`, `ICurrentUser`) reused by every later ADR-003 slice and by the SignalR hub
— hence filed under the `platform` module, consistent with the `0001`–`0008` cross-cutting
series. Sanctioned by ADR-003 (the behaviour + 403 are the ADR's preferred remediation option
#1). Runs the full spec chain.

**Use-cases / aggregates touched.**

- **`CartOwnershipBehaviour` (new, deep module).** A MediatR `IPipelineBehavior` constrained to
  requests implementing `ICartScopedRequest`. It loads the cart, applies the two-mode rule, and
  **throws `ForbiddenAccessException`** on violation; otherwise calls the next delegate. It hides
  the entire object-level authorization policy behind the pipeline — handlers and endpoints are
  unaware of it. Lives alongside `ValidationBehaviour` in the Application behaviours area.
- **`ICartScopedRequest` (new marker interface).** Exposes `Guid ShoppingCartId`. Implemented by
  the requests that opt into the check.
- **`ICurrentUser` (new abstraction, deep module).** Interface in `Application` exposing the
  authenticated caller's client id and whether the caller is authenticated. Implementation in the
  API/composition root over `IHttpContextAccessor`, reading the `nameidentifier` claim — the same
  claim and parsing rule as the endpoints' `GetClientId`. Encapsulates claim-reading behind a
  one-property interface; `Application` gains no framework dependency.
- **`PurchaseTicketsCommand` (modified — opt-in only).** Implements `ICartScopedRequest`; already
  carries `ShoppingCartId`. No change to its fields, handler, or `Result` contract.
- **`GetShoppingCartQuery` (modified — opt-in only).** Implements `ICartScopedRequest`; already
  carries `ShoppingCartId`. No change to its fields, handler, or DTO contract.
- **`ForbiddenAccessException` (reused, unchanged).** Already defined in `Application.Exceptions`
  and already mapped to `403 Forbidden` + `ProblemDetails` by `CustomExceptionHandler`. No new
  type; no handler change.
- **Endpoints (`purchase`, `getById`) (unchanged behaviour, OpenAPI only).** Remain anonymous; no
  `RequireAuthorization`. Their `.Produces(...)` declarations gain `403`.
- **DI / composition root (new registrations).** Register `CartOwnershipBehaviour` as an
  open-generic pipeline behaviour in the correct order; register `ICurrentUser` →
  HttpContext-based implementation; add `IHttpContextAccessor`.

**The two-mode rule (the behaviour).** Load cart by `ShoppingCartId`. `null` ⇒ pass through (let
the handler return the existing `404`). `ClientId == Guid.Empty` ⇒ pass through (capability
model). `ClientId != Guid.Empty` ⇒ require `ICurrentUser.IsAuthenticated` **and**
`ICurrentUser.ClientId == cart.ClientId`; else throw `ForbiddenAccessException`.

**Mechanism = throw, not `Result`.** Parallel to `ValidationBehaviour`. An authorization failure
is a cross-cutting guard outcome (the "unexpected" half of ADR-002), so it is an exception routed
once through `CustomExceptionHandler`, and it composes over both the command and the query
without depending on either's return type.

**Pipeline ordering.** The behaviour runs **after** structural validation and **before** the
handler; its exact position relative to the idempotency behaviour is a plan-level detail. The
behaviour re-reads the cart that the `purchase` handler will also load — a minor, accepted
double-read on a non-hot path (no optimization in this slice).

**Anonymous endpoints preserved.** No `.RequireAuthorization()` is added to `purchase`/`getById`;
the conditional strong check (only for assigned carts) is what protects owned carts while leaving
guest flows open.

**ADR status.** ADR-003 stays **Proposed**; this slice implements its first step.

**Explicitly deferred (NOT in this slice).**
- Applying the mechanism to `select` / `unselect` / `reserve` / `unreserve` and the SignalR hub
  methods (`SeatSelect`, `SeatUnselect`, `RegisterShoppingCart`, `UnsubscribeShoppingCart`) —
  later ADR-003 slices that reuse `CartOwnershipBehaviour` + `ICurrentUser`.
- Hardening `assignclient` against adopting a stranger's anonymous cart (HashId presentation).
- Validating the `HashId` (rather than the raw id) for the anonymous capability phase, and the
  logging-hygiene / raw-id redaction work.
- The final `403`-vs-`404` *disclosure* policy decision (this slice deliberately keeps not-found
  ⇒ `404` and only adds `403` for owned-cart violations).
- Any change to `Result` / `Result<T>` usage or to the `CustomExceptionHandler` mechanism.
- Flipping ADR-003 to Accepted.

## Testing Decisions

**What makes a good test here.** The externally observable behaviour is: which caller, against
which cart state (anonymous / assigned-mine / assigned-other / not-found), gets through versus
gets `403` — and that a `403` carries the standard `ProblemDetails`. Tests assert that observable
outcome (pass-through vs thrown `ForbiddenAccessException`; HTTP status + body), not internal
wiring. All four default coverage levels are in scope for this slice (the user opted in to the
full set, including the end-to-end harness).

**Units under test.**
- **`CartOwnershipBehaviour` (the acceptance gate / RED gate).** With the cart repository and
  `ICurrentUser` mocked: anonymous cart ⇒ next delegate invoked (pass); assigned cart + caller
  equals owner ⇒ pass; assigned cart + different authenticated caller ⇒ `ForbiddenAccessException`
  and next **not** invoked; assigned cart + unauthenticated caller ⇒ `ForbiddenAccessException`;
  cart not found ⇒ pass (handler owns the 404). This is the outside-in gate and is **RED** until
  the behaviour exists.
- **`ForbiddenAccessException` ⇒ 403 mapping** in `CustomExceptionHandler` (`DefaultHttpContext`,
  `ProblemDetails` body assertions), mirroring slice `0008`'s central-handler mapping tests —
  pins the existing mapping this slice newly depends on.
- **`ICurrentUser` implementation:** valid `nameidentifier` claim ⇒ authenticated, `ClientId`
  parsed; missing / non-Guid claim ⇒ anonymous (`IsAuthenticated == false`). Mirrors `GetClientId`
  parsing rules.
- **End-to-end via `WebApplicationFactory<Program>`** for `purchase` and `getById`: stranger on an
  assigned cart ⇒ `403` + `ProblemDetails`; owner ⇒ success; anonymous cart ⇒ success; non-existent
  cart ⇒ `404`. **This slice stands up the first `WebApplicationFactory<Program>` integration
  harness** in the repository, which prior slices (`0001`–`0008`) deferred for lack of one.

**Prior art.** `ValidationBehaviour` for the pipeline-behaviour shape; slice `0008`'s
`CustomExceptionHandlerInputGuardsTests` for the `DefaultHttpContext` + `ProblemDetails` mapping
tests; `AssignClientCartCommandHandlerTests` / `ShoppingCarts/ShoppingCartSpecification.cs`
(NSubstitute, AAA, `*Specification`) for the mocked behaviour test and cart construction;
ASP.NET Core `WebApplicationFactory<Program>` guidance for the new harness (authenticated vs
anonymous requests, test authentication handler for the `nameidentifier` claim).

**Out of the net (by decision):** no tests for the deferred entry points or the hub; no HashId /
logging-hygiene tests (deferred); no `Result<T>` changes to test.

## Out of Scope

- Applying the mechanism to any cart-scoped operation other than `purchase` and `getById`
  (`select`/`unselect`/`reserve`/`unreserve` and the SignalR hub methods — later ADR-003 slices).
- Hardening `assignclient` against adopting a stranger's anonymous cart.
- Validating the `HashId` for the anonymous capability phase; logging hygiene / raw-cart-id
  redaction.
- The final `403`-vs-`404` disclosure-policy decision (kept as an open question; this slice keeps
  not-found ⇒ `404`).
- Adding `RequireAuthorization` to the two endpoints, or otherwise closing the anonymous guest
  flow.
- Any change to `Result`/`Result<T>` usage, the `CustomExceptionHandler` mechanism, the validation
  behaviour, or any base type. No new `*Exception` or `Error` type is introduced.
- Flipping ADR-003 to Accepted.
- Publishing this PRD to a remote issue tracker — `gh` is not installed and no triage-label
  vocabulary was provided, so the `needs-triage` step could not run; the PRD is stored locally
  instead (same as slices `0001`–`0008`).

## Further Notes

- The diff is small but load-bearing: this is the **template** for every remaining ADR-003
  adoption, so its shape (opt-in marker interface; one central behaviour that loads the cart and
  throws `ForbiddenAccessException`; identity via `ICurrentUser`; reuse of the existing 403
  mapping; endpoints unchanged and still anonymous) matters more than its size.
- The slice is cheaper than ADR-003 assumed: the ADR planned a *new* `ForbiddenException` + a
  `CustomExceptionHandler` change, but `ForbiddenAccessException` and its 403 mapping **already
  exist**, so this slice touches no stable mechanism — only adds a behaviour, a marker interface,
  an identity abstraction, and DI registrations.
- `CartOwnershipBehaviour`, `ICartScopedRequest`, and `ICurrentUser` are deliberately **deep
  modules**: simple participation/usage surfaces hiding (respectively) the whole authorization
  policy and the whole claim-reading detail. Keeping the authorization policy in exactly one place
  — as `CustomExceptionHandler` keeps exception-to-HTTP in one place — is the structural win.
- `ICurrentUser` is the first general "current caller" abstraction in the service; the SignalR
  hub slice will provide its own implementation over `Context.User` to satisfy the same interface,
  which is why the abstraction (not endpoint-passed ids) was chosen.
- Verification per `CLAUDE.md`: `dotnet format`, `dotnet build -warnaserror`, `dotnet test`. As
  recorded in MEMORY (`dotnet10-migration`, `warnaserror-baseline-debt`), the build trips the
  accepted AutoMapper `NU1903` NuGet-audit advisory and pre-existing nullable debt under
  `-warnaserror`; scope the build/format so the real new warnings are what is validated, and run
  `dotnet` via the PowerShell tool against the `Program Files (x86)\dotnet` SDK.
