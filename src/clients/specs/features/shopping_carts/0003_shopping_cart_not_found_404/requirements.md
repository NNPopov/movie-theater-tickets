# 0003 · shopping_cart_not_found_404 — Requirements

## Functional Requirements

| ID | Requirement |
|---|---|
| F1 | When a cart fetched by id is missing and the backend answers `404`, the user sees a clean empty cart instead of an error screen. |
| F2 | When a cart fetched by id is missing (`404`), the stale cart id is cleared from local storage so the user no longer points at a non-existent cart on subsequent launches. |
| F3 | The missing-cart-by-id `404` resolves to exactly the same externally observable outcome (empty cart + id cleared) that the old `204` produced. |
| F4 | A brand-new user with no active cart (`GET /shoppingcarts/current` returning `204`) continues to see the unchanged empty-cart experience. |
| F5 | The follow-up load immediately after creating a cart resolves to a clean empty-cart state, not an error, when the cart cannot be found. |
| F6 | The rare `404` from the current-cart endpoint (id recorded, record gone) recovers gracefully into an empty cart. |
| F7 | The "cart absent" decision recognizes both `204` and `404` as meaning the cart is gone. |
| F8 | No new user-visible state is introduced by this change. |

## Non-functional Requirements

| ID | Requirement |
|---|---|
| N1 | The change is a minimal legacy modification confined to the `shopping_carts` slice under `lib/src/shopping_carts/`; no `_shared/` and no `core/` files are modified. |
| N2 | The legacy Dio-with-hand-written-repo style is preserved; no conversion to ports/adapters, retrofit, `slang`, or renaming of `*Repo` classes. |
| N3 | Control flow branches on the HTTP **status code**, never on the human-readable `detail` of the `ProblemDetails` body. |
| N4 | A missing cart-by-id `404` from `getShoppingCart` is mapped to `DataFailure(statusCode: 404)` — the same failure type the legacy `204` branch used. |
| N5 | The `getShoppingCart` adapter handles `DioException` before the generic `Exception` catch, so a `404` is not swallowed into `ServerFailure(500)`. |
| N6 | The `getShoppingCart` adapter retains a catch-all that logs on the unexpected-exception path, per the project adapter rule. |
| N7 | The rare `404` from `getCurrentUserShoppingCart` is returned as `NotFoundFailure` with an explicit `statusCode: 404`, not relying on the `NotFoundFailure` default of `204`. |
| N8 | The stale-id cleanup guard in `GetShoppingCartById` and both empty-cart guards in `ShoppingCartCubit` recognize `statusCode == 204 || statusCode == 404`, and `204` remains recognized. |
| N9 | `pubspec.yaml` is not modified; no new dependency is added. |
| N10 | Test coverage is scoped to the single changed behavior at the cubit level (`bloc_test` + `mocktail`); no new adapter/use-case/widget default layers are introduced. |
| N11 | Tests assert externally observable cubit behavior (emitted state and whether the stored cart id is cleared), never internal status-code branching or Dio internals. |

## Out of scope

- `getMovieById` / `getMovieSession` (by-id) not-found hardening.
- `reserveSeats` `404` mapping.
- Removing the `NotFoundFailure` default `statusCode = 204` footgun in `core/errors/`.
- The `GET /shoppingcarts/current` `204` empty-cart flow.
- `GET /movies/{movieId}/moviesessions` handling (backend now returns `200` + `[]`).
- `assignClient` (already handles `404` and `204`).
- Any re-architecture of the legacy repos toward retrofit, ports/adapters, or the target test stack beyond the single regression test.
