# PRD — Shopping cart by-id not-found: 204 → 404 client adaptation

- **Slice:** `0003_shopping_cart_not_found_404`
- **Feature:** shopping_carts
- **Type:** Legacy modification (minimal change) — client adaptation to a backend contract change
- **Source:** `docs/api-not-found-204-to-404-migration.md` (backend slice `0002_content_not_found_404`, ADR-002 step 2) + grilling session 2026-06-03
- **Status:** Planned

## Problem Statement

A returning user whose shopping cart has expired or been removed on the server still
has that cart's id stored locally. When the app tries to load the cart by id on
startup (or right after creating one), the backend used to answer **`204 No Content`**
for the missing cart, and the client read that as "no cart" — it quietly emptied the
cart view and cleared the stale id. After the backend migration, the same missing-cart
read now answers **`404 Not Found`**. The client does not recognize this: instead of a
clean empty cart, the user is dropped onto an **error screen**, and the dead cart id is
**never cleared from local storage**, so the user stays stuck pointing at a cart that no
longer exists on every subsequent launch.

## Solution

Teach the client that, for a cart fetched **by id**, a `404` means the same thing the
old `204` meant: "this cart is gone." From the user's seat, nothing new appears — a
missing cart again resolves to a clean empty-cart state and the stale id is cleared, so
the next action (e.g. creating a fresh cart) works normally.

The genuinely-empty flows the backend deliberately left unchanged stay untouched:
`GET /shoppingcarts/current` still answers `204` when the user simply has no active cart,
so the empty-cart experience for a brand-new user is unaffected.

## User Stories

1. As a returning moviegoer whose cart expired server-side, I want the app to show me a clean empty cart instead of an error screen, so that I can start a new booking without confusion.
2. As a returning moviegoer with a stale cart id, I want that dead id cleared automatically when the cart is gone, so that I am not stuck pointing at a non-existent cart on every launch.
3. As a moviegoer who just created a cart, I want the immediate follow-up load to behave correctly when the cart cannot be found, so that I land on an honest empty state rather than an error.
4. As a brand-new moviegoer with no cart at all, I want the empty-cart experience to be exactly as before, so that the change to missing-by-id carts does not disturb my normal flow.
5. As a moviegoer hitting the rare server-inconsistent state on the "current cart" check, I want the app to recover gracefully into an empty cart, so that a backend edge case is not a dead end for me.
6. As a developer, I want a missing cart-by-id `404` mapped to the same internal "cart absent" signal the old `204` produced, so that the existing cleanup and empty-state logic keeps working without re-architecture.
7. As a developer, I want the "cart absent" decision to recognize both `204` and `404`, so that the legitimate `204` from the current-cart endpoint and the new `404` from by-id reads both resolve correctly.
8. As a developer, I want the change confined to the `shopping_carts` slice, so that it stays atomic and reviewable and does not touch neighbouring slices or `core/`.
9. As a developer, I want the rare `404` from the current-cart endpoint to carry an explicit status code instead of relying on a misleading default, so that the behavior does not depend on a hidden magic value.
10. As a maintainer, I want a focused regression test locking "missing cart-by-id ⇒ empty cart + id cleared", so that this UX cannot silently regress again.
11. As a maintainer, I want the unaffected paths (current-cart empty `204`, `assignClient` already handling `404`, `movies/{id}/moviesessions` now `200`+`[]`) documented as deliberately untouched, so that reviewers know they were considered, not missed.

## Implementation Decisions

**Framing.** This is a **legacy modification**, not a new slice and not a re-architecture.
Per `CLAUDE.md`, the change is minimal and matches the surrounding legacy Dio-with-
hand-written-repo style; it does **not** convert the repo to ports/adapters, retrofit,
`slang`, or rename anything. The whole change lives inside the `shopping_carts` slice.

**Modules built / modified:**

- **`getShoppingCart` (by-id) adapter path (modified).** Today a missing cart arrives as
  `204` on the success path and becomes a `DataFailure(statusCode: 204)`. After the
  migration it arrives as a `404`, which Dio raises as a `DioException` that currently
  falls through to a generic `ServerFailure(statusCode: 500)`. Add explicit
  `DioException` handling that maps `404` → **`DataFailure(statusCode: 404)`** (same
  failure type already used by the legacy 204 branch). Other Dio errors keep mapping to
  the existing generic server failure. The now-dead `204` success-branch may remain as a
  harmless no-op (server no longer sends it for this path).
- **"Cart absent" recognition (modified).** Broaden the three downstream checks that gate
  the empty-cart / cleanup behavior from `statusCode == 204` to **`statusCode == 204 || statusCode == 404`**:
  the stale-id cleanup in the get-shopping-cart use-case, and the two cubit branches that
  emit the empty-cart (`initState`) state. `204` must stay recognized because the
  current-cart endpoint still legitimately returns it.
- **`getCurrentUserShoppingCart` rare-404 (minor, modified).** The endpoint now returns
  `404` only in the rare inconsistent state (id recorded, record gone). The existing
  catch already routes `404` to a not-found failure; make its **`statusCode` explicit**
  rather than relying on the `NotFoundFailure` default of `204`. Behavior is unchanged
  (graceful empty cart), but it stops depending on a hidden default.

**Behavioral contract:**

- Cart fetched **by id** that is missing (`404`) ⇒ empty-cart state **and** stale id
  cleared — identical to the old `204` behavior.
- A brand-new user with no active cart (`204` from current) ⇒ empty cart, exactly as
  before.
- The rare current-cart `404` (inconsistent state) ⇒ graceful empty cart.
- No new user-visible states are introduced.

## Testing Decisions

**What a good test is here.** Tests assert externally observable cubit behavior — which
state the `ShoppingCartCubit` emits and whether the stored cart id is cleared — never the
internal status-code branching or Dio internals.

**Regression test (the focused guard).** One `bloc_test` on `ShoppingCartCubit`:
when the get-shopping-cart path yields a `DataFailure(statusCode: 404)`, the cubit emits
the empty-cart (`initState`) state and the stored `SHOPPING_CARD_ID` is cleared — i.e.
the new `404` produces the same outcome the old `204` did. The use-case/repo dependencies
are mocked with the locked `bloc_test` + `mocktail` stack.

**Default layer tests.** The full per-layer default coverage in `CLAUDE.md` targets
**new** slices; this is a minimal legacy modification, so coverage is deliberately scoped
to the single behavior that changed (the cubit-level `404` ⇒ empty-cart + cleanup guard).
No new adapter, use-case, or widget is introduced, so those default layers do not apply.

**Prior art.** `test/lib/shopping_cart/data/models/shopping_cart_model_test.dart`
(existing shopping-cart-area test) and the migration outside-in tests under
`test/features/platform/` show the project's existing test placement and style.

## Out of Scope

- **`getMovieById` / `getMovieSession` (by-id) not-found hardening.** These catch only
  `ServerException` (never thrown by Dio), so a `404` goes unhandled — but they were
  already broken for the old `204` (empty-body `FormatException`); the migration does not
  create this. Tracked as a separate backlog item.
- **`reserveSeats` 404 mapping.** A `404` there becomes a generic `ServerFailure(500)`;
  acceptable for now, deferred to the same backlog item.
- **`NotFoundFailure` default `statusCode = 204` footgun in `core/errors/`.** Fixing the
  misleading default touches `core/` (outside the slice) with a wider blast radius;
  tracked as a separate backlog item.
- **`GET /shoppingcarts/current` empty-cart flow.** Backend keeps `204` for "no active
  cart"; unchanged and intentionally untouched.
- **`GET /movies/{movieId}/moviesessions`.** Backend now always returns `200` + `[]`;
  this actually removes the old `as List`-on-`204` fragility, so no client change is
  needed.
- **`assignClient`.** Already handles `404` (and `204`); no change required.
- **Any re-architecture** of the legacy repos toward retrofit, ports/adapters, or the
  target test stack beyond the single regression test.

## Further Notes

- Backend reference: `docs/api-not-found-204-to-404-migration.md` — `detail` in the
  `ProblemDetails` body is human-readable; the client switches on the **status code**, not
  the message.
- Scope was settled in the 2026-06-03 grilling session: (B) fix the shopping-cart by-id
  regression now, defer movie/session/reserve hardening; (B) honest `404` in the repo +
  broadened downstream checks; (A) graceful empty cart for the rare current-`404`; (A)
  keep `core/` untouched and pass status codes explicitly; (B) one focused `bloc_test`.
- Two backlog items spun off by this slice: **(1)** not-found hardening for the
  movie/session by-id reads and `reserveSeats`; **(2)** remove the `NotFoundFailure`
  default-`204` footgun in `core/errors/`.
