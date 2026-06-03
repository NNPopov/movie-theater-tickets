# 0003 · shopping_cart_not_found_404 — Validation Checklist

## Manual testing

| # | Step | Expected result |
|---|---|---|
| M1 | With a stale cart id in storage whose cart no longer exists server-side, launch the app. | A clean empty cart is shown — no error screen. (F1) |
| M2 | After M1, inspect secure storage for `SHOPPING_CARD_ID`. | The stale id has been cleared. (F2) |
| M3 | After M1/M2, relaunch the app again. | Empty cart again; the app does not get stuck pointing at the dead cart. (F2) |
| M4 | After M1, create a fresh cart and add a seat. | The new cart works normally; no leftover state from the dead cart. (F2, F5) |
| M5 | As a brand-new user with no cart at all (`GET /shoppingcarts/current` → `204`), launch the app. | The empty-cart experience is exactly as before. (F4) |
| M6 | Create a cart, then trigger the immediate follow-up load while the cart is reported missing. | An honest empty-cart state appears, not an error. (F5) |
| M7 | Force the rare current-cart inconsistent state (id recorded server-side, record gone → `404`). | The app recovers into a graceful empty cart, not an error. (F6) |
| M8 | Compare the missing-by-id `404` outcome against the previous `204` behavior. | Identical externally observable result: empty cart + id cleared. (F3, F7) |
| M9 | Walk the whole flow and watch for any new screen, dialog, or status. | No new user-visible state appears anywhere. (F8) |

## Code review

- [ ] Change is confined to `lib/src/shopping_carts/`; no `_shared/` and no `core/` file modified — `git diff --stat` shows only the three slice files + the new test. (N1)
- [ ] No conversion to ports/adapters, retrofit, or `slang`; no `*Repo` class renamed. (N2)
- [ ] Branching is on `response.statusCode` / `e.response?.statusCode`, never on the `ProblemDetails` `detail` text. (N3)
- [ ] `getShoppingCart` maps `404` to `DataFailure(statusCode: 404)` (same type as the legacy `204` branch). (N4)
- [ ] `getShoppingCart` has an `on DioException` clause **before** the generic `on Exception` catch. (N5)
- [ ] `getShoppingCart` retains an outer catch-all on the unexpected-exception path. (N6)
- [ ] `getCurrentUserShoppingCart` returns `NotFoundFailure(statusCode: 404)` explicitly for the rare `404`. (N7)
- [ ] `GetShoppingCartById` cleanup guard and both `ShoppingCartCubit` empty-cart guards test `statusCode == 204 || statusCode == 404`; `204` is still recognized — `grep -n "== 204" lib/src/shopping_carts` shows each `204` paired with `404`. (N8)
- [ ] `pubspec.yaml` unchanged — `git diff pubspec.yaml` is empty. (N9)
- [ ] Test coverage is the single cubit-level `bloc_test` regression guard; no new adapter/use-case/widget test layers added. (N10)
- [ ] The regression test asserts emitted state + storage cleared, not internal branching or Dio internals. (N11)
- [ ] `NotFoundFailure` default in `core/errors/failures.dart` is untouched; movie/session/reserve hardening not started (out of scope). 
- [ ] `dart run build_runner build --delete-conflicting-outputs` — no errors (no codegen inputs changed; expected no-op).
- [ ] `dart run slang` — no errors (no `*.json` i18n changed; expected no-op).
- [ ] `dart analyze` — no warnings.
- [ ] All tests green.
