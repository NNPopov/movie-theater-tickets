# 0003 · shopping_cart_not_found_404 — Outside-in test spec

## Goal

Prove that when the by-id shopping-cart read reports the cart is gone via
`DataFailure(statusCode: 404)`, the `ShoppingCartCubit` resolves to the clean empty
cart and the stale cart id is cleared from secure storage — the same externally
observable outcome the old `204` produced.

## Entry point

`cubit.getShoppingCart()` — the public Cubit method that loads the current cart.

> Note for `/slice-test-red`: `ShoppingCartCubit`'s constructor auto-triggers
> `getShoppingCartIfExits()` → `getShoppingCart()`. The Dart test must account for the
> constructor-driven load (e.g. drive the behavior through construction, or settle the
> initial load before acting) so the asserted state sequence is deterministic.

## Wired real (production code in the test)

- `GetShoppingCartUseCase` (the slice use-case under test — runs the real stale-id
  cleanup guard in `GetShoppingCartById`).
- `ShoppingCartCubit` (the system under test — runs the real empty-cart guard).
- `ShoppingCartState` and domain entities (`ShoppingCart.empty()`).

## Mocked (system boundaries only)

- **`ShoppingCartRepo`** (the network boundary for this legacy slice):
  `getShoppingCart(<staleId>)` returns `Left(DataFailure(message: ..., statusCode: 404))`
  in Scenario 1, and `Left(DataFailure(statusCode: 204))` in Scenario 2.
- **`AuthService`**: `getCurrentStatus()` returns `Left(...)` (unauthenticated) so the
  use-case takes the stored-id path and calls `getShoppingCart(<staleId>)`.
- **`FlutterSecureStorage`**: seeded with `SHOPPING_CARD_ID = '<staleId>'` and
  `SHOPPING_CARD_HASH_ID = '<staleHash>'` before the act; asserted cleared after.
- **`EventHub`**: `shoppingCartUpdateSubscribe(...)` is a no-op stub.
- **`EventBus`**: captures published events (`ShoppingCartHashIdUpdated`); its `stream`
  is an empty/controlled stream so the cubit's subscription does not interfere.
- **Sibling use-cases** (`CreateShoppingCartUseCase`, `SelectSeatUseCase`,
  `UnselectSeatUseCase`, `ShoppingCartUpdateSubscribeUseCase`, `ReserveSeatsUseCase`):
  mocked and not exercised by this scenario.

## Test scenarios

### Scenario 1: missing cart by id (`404`) ⇒ empty cart + stale id cleared

**Setup:**
- `FlutterSecureStorage` seeded: `SHOPPING_CARD_ID = 'stale-cart-1'`,
  `SHOPPING_CARD_HASH_ID = 'stale-hash-1'`.
- `AuthService.getCurrentStatus()` ⇒ `Left(...)` (unauthenticated).
- `ShoppingCartRepo.getShoppingCart('stale-cart-1')` ⇒
  `Left(DataFailure(message: "shoppingCartId doesn't exist", statusCode: 404))`.

**Act:**
- `cubit.getShoppingCart()`

**Expect:**
- States emitted by the Cubit (in order):
  `[ state.copyWith(status: ShoppingCartStateStatus.creating),
     ShoppingCartState.initState() (status == ShoppingCartStateStatus.initial) ]`
- Side effects observed:
  - `SHOPPING_CARD_ID` and `SHOPPING_CARD_HASH_ID` deleted from secure storage.
  - `EventBus` received `ShoppingCartHashIdUpdated`.
- Mocks verified:
  - `ShoppingCartRepo.getShoppingCart('stale-cart-1')` called once.
  - No `error`-status state was emitted.

### Scenario 2: regression — missing cart by id (`204`) still ⇒ empty cart + id cleared

**Setup:**
- Same storage seed and `AuthService` stub as Scenario 1.
- `ShoppingCartRepo.getShoppingCart('stale-cart-1')` ⇒
  `Left(DataFailure(message: "shoppingCartId doesn't exist", statusCode: 204))`.

**Act:**
- `cubit.getShoppingCart()`

**Expect:**
- States emitted by the Cubit (in order):
  `[ state.copyWith(status: ShoppingCartStateStatus.creating),
     ShoppingCartState.initState() (status == ShoppingCartStateStatus.initial) ]`
- Side effects observed: `SHOPPING_CARD_ID` and `SHOPPING_CARD_HASH_ID` cleared.
- Mocks verified: no `error`-status state emitted — proving the `204`-recognition was
  preserved when `404` was added.

## Out of scope for this test

- Widget rendering and route navigation (covered by widget tests separately, if any).
- The adapter's Dio-level `DioException(404) → DataFailure(404)` mapping in isolation
  (covered by an adapter unit test later, per `agent_docs/testing.md`; here the repo is
  mocked at the boundary).
- `getCurrentUserShoppingCart`'s explicit-`404` change and the authenticated path
  (the rare-inconsistent-state edge — covered by unit tests later).
- Manual UX scenarios from `validation.md` that do not change observable state through
  this Cubit.
