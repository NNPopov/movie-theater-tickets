# 0001 · flutter_dart_deps_migration — Outside-in test spec

> **Note on shape.** This is a migration slice, not a CRUD slice, so there is no
> new network Cubit to drive with a mocked `Dio`. The single outside-in
> acceptance test instead pins the **highest-risk end-to-end path the migration
> touches**: a shopping-cart **round-trip through local storage**
> (`ShoppingCartLocalRepo.setShoppingCart` → `getShoppingCart`). One behavior,
> exercised through the slice's public data surface, transitively proves that the
> three riskiest bumps still work together:
> - **freezed 3** codegen produces a working `ShoppingCartDto`,
> - **json_serializable** `toJson`/`fromJson` round-trip is intact,
> - the **`localstorage`** major bump still stores and returns the item.
>
> Per the PRD, widget smoke tests (movies/sessions carousels, cart, auth) and the
> existing `shopping_cart_model_test` are the rest of the safety net; they live
> alongside this test but are not the single outside-in gate.

## Goal

Prove that after the dependency migration a `ShoppingCart` can be written to and
read back from local storage unchanged — i.e. freezed 3 / json_serializable /
`localstorage` still cooperate end-to-end through the slice's storage port.

## Entry point

The `ShoppingCartLocalRepo` storage port, exercised as a pair:

- write: `localRepo.setShoppingCart(cart)`
- read: `localRepo.getShoppingCart()`

(Concrete type under test: `ShoppingCartLocalRepoImpl`, the production adapter.)

## Wired real (production code in the test)

- `ShoppingCartLocalRepoImpl` (the slice's local-storage adapter — system under test)
- `ShoppingCartLocalRepo` (the port it implements)
- `ShoppingCartDto` + `ShoppingCartSeatDto` (freezed 3 / json_serializable models)
- `ShoppingCart` + `ShoppingCartSeat` domain entities and the entity `.map()` →
  DTO conversion

## Mocked (system boundaries only)

- **`localstorage` (`LocalStorage`)**: the only boundary. The production adapter
  `ShoppingCartLocalRepoImpl` constructs its own `LocalStorage('movie_theatre.json')`
  and exposes it as a public `storage` field — there is **no injection point**, so
  the test does a **real in-process round-trip** through that store rather than a
  fake. The test initializes the binding
  (`TestWidgetsFlutterBinding.ensureInitialized()`), awaits store readiness via the
  public `storage` field, and clears the `'ShoppingCart'` key between scenarios for
  isolation. **Checkpoint:** if the migrated `localstorage` major changes the API
  (e.g. removes the named constructor or `.ready`, requires global
  `initLocalStorage()`, or makes `getItem` async), both this test setup **and** the
  adapter must adapt — this is exactly the Module-E decision the test is meant to
  catch.

## Test scenarios

### Scenario 1: cart round-trips through local storage (happy path)

**Setup:**
- Fake `LocalStorage` starts empty.
- Build a `ShoppingCart` carrying one `ShoppingCartSeat`
  (`seatRow: 1, seatNumber: 1`) and a known `id`
  (`'3fa85f64-5717-4562-b3fc-2c963f66afa6'`).

**Act:**
- `await localRepo.setShoppingCart(cart)`
- `final result = await localRepo.getShoppingCart()`

**Expect:**
- `setShoppingCart` returns `Right(null)`.
- `getShoppingCart` returns `Right(cart)` whose `id` and `seats` equal the stored
  cart (full DTO round-trip, no data loss).
- Boundary verified: `LocalStorage.setItem('ShoppingCart', …)` called once with
  the DTO JSON; `getItem('ShoppingCart')` called once.

### Scenario 2: nothing stored yields a 404 DataFailure (failure path)

**Setup:**
- Fake `LocalStorage` is empty; `getItem('ShoppingCart')` returns `null`.

**Act:**
- `final result = await localRepo.getShoppingCart()`

**Expect:**
- `getShoppingCart` returns `Left(DataFailure)` with `statusCode == 404` and
  message `'ShoppingCart not stored'`.
- No exception escapes; no `fromJson` is attempted on `null`.

## Out of scope for this test

- Widget rendering of the cart, movies, or movie-session screens (covered by the
  separate widget smoke tests).
- The `ShoppingCartCubit` orchestration, its `EventBus` subscription, and
  `FlutterSecureStorage` reads (network/event paths covered by manual web smoke
  run + analyze, per the PRD).
- Server-side / `Dio` paths — this slice has no migrated network contract to pin.
- The `flutter_bloc` 8→9 API surface (verified via screen smoke tests + `dart
  analyze` + manual web run, per the PRD).
