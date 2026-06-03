# Plan — `0003_shopping_cart_not_found_404`

> **Type:** Legacy modification (minimal change), per `CLAUDE.md` migration rules.
> This is **not** a new slice and **not** a re-architecture. Match the surrounding
> legacy Dio-with-hand-written-repo style. Do **not** convert to ports/adapters,
> retrofit, `slang`, or rename `*Repo`. The whole change lives inside the legacy
> `shopping_carts` slice under `lib/src/shopping_carts/`.

---

## 1. Header

Teach the client that a cart fetched **by id** answering `404 Not Found` means the
same thing the old `204 No Content` meant: "this cart is gone." The user lands on a
clean empty cart and the stale cart id is cleared from secure storage — identical to
the pre-migration behavior. No new user-visible state is introduced. The legitimate
`204` from `GET /shoppingcarts/current` (brand-new user, no active cart) stays
recognized and unchanged.

---

## 2. Context

**READ:**

- `@CLAUDE.md` — fully (migration rules: minimal legacy change, no re-architecture).
- `@docs/api-not-found-204-to-404-migration.md` — backend contract change (204 → 404).
- `@lib/src/shopping_carts/data/repos/shopping_cart_repo_impl.dart` — **modified**
  (`getShoppingCart`, `getCurrentUserShoppingCart`).
- `@lib/src/shopping_carts/domain/usecases/get_shopping_cart.dart` — **modified**
  (`GetShoppingCartById` stale-id cleanup guard).
- `@lib/src/shopping_carts/presentation/cubit/shopping_cart_cubit.dart` — **modified**
  (two `statusCode == 204` branches).
- `@lib/core/errors/failures.dart` — read-only reference (`DataFailure`,
  `NotFoundFailure` and its default `statusCode = 204` footgun — **do not change**).
- `@lib/src/helpers/constants.dart` — `SHOPPING_CARD_ID`, `SHOPPING_CARD_HASH_ID` keys.
- `@test/lib/shopping_cart/data/models/shopping_cart_model_test.dart` — existing
  test placement/style in the shopping-cart area.
- `@.claude/skills/bloc/SKILL.md` — `bloc_test` + `mocktail` conventions.

**DO NOT READ:**

- Other slices' code outside `shopping_carts` (movies, seats, auth internals).
- `*.gr.dart`, `*.config.dart`, other generated files.
- `getMovieById` / `getMovieSession` / `reserveSeats` internals — explicitly out of
  scope (separate backlog item).

---

## 3. API (backend contract being adapted to)

The backend slice `0002_content_not_found_404` (ADR-002 step 2) changed missing-record
reads from `204 No Content` to `404 Not Found` with a `ProblemDetails` body.

| Endpoint | Missing-resource response | Client switches on |
|---|---|---|
| `GET /api/shoppingcarts/{id}` (by id) | **`404`** (was `204`) | **status code** |
| `GET /api/shoppingcarts/current` | still `204` for "no active cart"; `404` **only** in the rare inconsistent state (id recorded, record gone) | **status code** |

- The `404` body is `ProblemDetails` JSON; `detail` is human-readable and **not** used
  for control flow — the client branches on the **status code only**.
- Dio raises a non-2xx (`404`) as a `DioException` (a subtype of `Exception`), so a bare
  `on Exception` catch currently swallows it into a generic `ServerFailure(500)`.

Deliberately untouched (documented as considered, not missed):

- `GET /shoppingcarts/current` `204` empty-cart flow — backend keeps `204`.
- `GET /movies/{movieId}/moviesessions` — backend now always `200` + `[]`; no client change.
- `assignClient` — already handles both `404` and `204`.

---

## 4. Touched files (no new files in `lib/`)

```
lib/src/shopping_carts/
├── data/repos/shopping_cart_repo_impl.dart        # MODIFIED — getShoppingCart, getCurrentUserShoppingCart
├── domain/usecases/get_shopping_cart.dart         # MODIFIED — GetShoppingCartById cleanup guard
└── presentation/cubit/shopping_cart_cubit.dart    # MODIFIED — two "204" branches

test/  (NEW test only)
└── features/shopping_carts/0003_shopping_cart_not_found_404/
    └── shopping_cart_not_found_404_outside_in_test.dart   # the RED → GREEN acceptance gate
```

No new files under `lib/`. No `_shared/` or `core/` changes. No `pubspec.yaml` change.

---

## 5. What to do (step by step)

### Step 1 — `getShoppingCart(shoppingCartId)` adapter (by-id 404 mapping)

File: `lib/src/shopping_carts/data/repos/shopping_cart_repo_impl.dart`, method
`getShoppingCart` (currently lines ~52–85).

Today: the `204` success-branch returns `DataFailure(statusCode: 204)`; any thrown
`Exception` (including the new `DioException(404)`) falls into the single
`on Exception catch (e)` → `ServerFailure(statusCode: 500)`.

Change: add an explicit `on DioException catch (e, st)` **before** the existing
`on Exception` catch:

- `e.response?.statusCode == 404` ⇒ `Left(DataFailure(message: ..., statusCode: 404))`
  — the **same failure type** the legacy `204` branch already produced, only with code
  `404`.
- any other `DioException` ⇒ keep the existing `ServerFailure(statusCode: 500)` behavior.

Keep the now-dead `if (response.statusCode == 204)` success-branch as a harmless no-op
(the server no longer sends `204` for this path; removing it is not required and would
widen the diff).

**CRITICAL:** order matters — `DioException` is an `Exception`, so the `on DioException`
clause must come first or the `404` keeps being swallowed by `on Exception`.
**CLAUDE.md hard rule:** the adapter must still end with a catch-all
`catch (e, st)` (or `on Exception`) that logs — preserve logging on the unexpected path.

### Step 2 — `getCurrentUserShoppingCart` rare-404 (explicit status code)

Same file, method `getCurrentUserShoppingCart` (currently lines ~112–145).

The `DioException` handler already routes `404` to a not-found failure but relies on
`NotFoundFailure`'s misleading **default `statusCode = 204`**. Make it explicit:

- `e.response?.statusCode == 404` ⇒ `Left(NotFoundFailure(message: ..., statusCode: 404))`.

Behavior is unchanged (graceful empty cart downstream) but it stops depending on a hidden
default. **Do not** touch the `NotFoundFailure` default in `core/errors/` — that footgun
is a separate backlog item, out of scope.

### Step 3 — "cart absent" recognition in the use-case

File: `lib/src/shopping_carts/domain/usecases/get_shopping_cart.dart`, method
`GetShoppingCartById` (currently line ~90).

Broaden the stale-id cleanup guard:

- `if (l.statusCode == 204)` ⇒ `if (l.statusCode == 204 || l.statusCode == 404)`.

When true, the stale `SHOPPING_CARD_ID` + `SHOPPING_CARD_HASH_ID` are deleted and
`ShoppingCartHashIdUpdated` is sent — exactly as before, now also for `404`.

### Step 4 — "cart absent" recognition in the cubit (two branches)

File: `lib/src/shopping_carts/presentation/cubit/shopping_cart_cubit.dart`.

Broaden both empty-cart guards from `failure.statusCode == 204` to
`failure.statusCode == 204 || failure.statusCode == 404`:

- `createShoppingCart` follow-up load branch (currently line ~119).
- `getShoppingCart` branch (currently line ~154).

In both, the `true` branch emits `ShoppingCartState.initState()` (the clean empty cart).
`204` **must stay recognized** because `getCurrentUserShoppingCart` still legitimately
yields it.

> Consider extracting the repeated `code == 204 || code == 404` test into a tiny private
> helper (e.g. `bool _isCartAbsent(dynamic code)`) to avoid drift between the two cubit
> branches and the use-case — optional, only if it does not widen the diff noticeably.

### Step 5 — verify

- `dart format .` — no diff.
- `dart analyze` — no warnings.
- No `build_runner` needed (no freezed/injectable/retrofit/slang inputs changed).
- Run the outside-in test (Step 6 in §6) — RED before the code change, GREEN after.

---

## 6. Tests

Per the PRD and `CLAUDE.md`, the full four-layer default coverage targets **new** slices.
This is a minimal legacy modification introducing **no** new adapter, use-case, or widget,
so coverage is deliberately scoped to the single behavior that changed.

**Acceptance gate (outside-in, the focused regression guard):**

`test/features/shopping_carts/0003_shopping_cart_not_found_404/shopping_cart_not_found_404_outside_in_test.dart`

One `bloc_test` on `ShoppingCartCubit` with use-case/repo dependencies mocked
(`bloc_test` + `mocktail`):

- **Given** the get-shopping-cart path yields a `DataFailure(statusCode: 404)`
  (the new by-id missing-cart signal),
- **Then** the cubit emits the empty-cart `initState` state
  (`ShoppingCartStateStatus.initial`), and
- the stored `SHOPPING_CARD_ID` (and `SHOPPING_CARD_HASH_ID`) is cleared —
  i.e. the new `404` produces the same externally observable outcome the old `204` did.

Tests assert **externally observable cubit behavior** (emitted state + storage cleared),
never the internal status-code branching or Dio internals.

**Regression-coverage note (no behavior change expected):** the existing `204` path must
keep resolving to the empty cart — assert it stays green so the broadening did not break it.

---

## 7. Report (on completion)

- Files changed (exactly the three `lib/` files in §4) + the one new test file.
- Confirmation that **no** other slice, no `_shared/`, and no `core/` file was touched.
- Confirmation `pubspec.yaml` was not modified.
- `dart format` / `dart analyze` clean; outside-in test RED → GREEN.
- The two spun-off backlog items remain untouched (movie/session/reserve hardening;
  `NotFoundFailure` default-`204` footgun).

---

## 8. Out of scope (do NOT do)

- ❌ `getMovieById` / `getMovieSession` (by-id) not-found hardening — separate backlog item.
- ❌ `reserveSeats` `404` mapping — same backlog item.
- ❌ Changing `NotFoundFailure`'s default `statusCode = 204` in `core/errors/` — wider
  blast radius, separate backlog item.
- ❌ Touching the `GET /shoppingcarts/current` `204` empty-cart flow.
- ❌ Touching `movies/{id}/moviesessions` handling or `assignClient`.
- ❌ Any re-architecture toward retrofit / ports-and-adapters / `slang` / `*Repo` rename.
- ❌ Adding a dependency to `pubspec.yaml`.
- ❌ Removing the dead `204` success-branch in `getShoppingCart` (harmless; would widen diff).
