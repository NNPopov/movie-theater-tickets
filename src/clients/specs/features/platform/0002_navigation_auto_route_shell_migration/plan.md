# Feature Spec / Implementation Plan — `0002_navigation_auto_route_shell_migration`

- **Slice:** `0002_navigation_auto_route_shell_migration`
- **Feature:** platform
- **Type:** Tracked migration slice (legacy `Navigator` + `generateRoute` → `auto_route` shell)
- **Sources:** `prd.md` (this folder) + the legacy navigation code under `lib/`
- **Status:** Planned

> This is a **migration** slice, not a CRUD slice. There is **no backend endpoint**.
> The "contract" the implementation must honor is a **navigation contract** (the typed
> route table + three behavioral guarantees), not an HTTP contract. Block 3 below
> replaces the usual "API" block with that navigation contract.

---

## 1. Title

Replace the hand-rolled **nested `Navigator` + `generateRoute` string matching** with the
locked target navigation stack, **`auto_route` 9.x**, structured as a **shell route**: a
persistent `HomeShell` (top app bar + menu) hosts the five existing screens as child
routes. Outcome for the user: tapping the active menu item is a no-op (no duplicate
screen), menu-to-menu bouncing keeps a bounded back stack, and an unknown route shows an
honest localized **404 page** instead of being silently dropped on Movies. The booking
funnel (Movies → Movie Sessions → Seats → Cart) behaves exactly as before.

---

## 2. Context

### READ

- `@CLAUDE.md` — fully (locked stack, migration rules, hard rules).
- `@specs/features/platform/0002_navigation_auto_route_shell_migration/prd.md` — the PRD this plan implements.
- `@agent_docs/navigation.md` — target navigation conventions (`core/routing/app_router.dart`, `@RoutePage()`, navigate via typed `*Route` classes).
- **Files that will be replaced/modified:**
  - `@lib/core/services/router.main.dart` — the legacy `generateRoute` to be **replaced** by `AppRouter`. Note the **per-route `BlocProvider` wiring** here — it must move into the new route pages (see §5, CRITICAL).
  - `@lib/main.dart` — hosts the nested `Navigator` (lines ~126–134) and creates the `navigatorKey`; both go away.
  - `@lib/src/home/presentation/widgets/home_app_bar.dart` — currently takes `GlobalKey<NavigatorState>`; the shell hosts it instead.
  - `@lib/src/dashboards/presentation/dashboard_widget.dart` + `@lib/src/dashboards/presentation/menu_item_widget.dart` — the menu; moves into `HomeShell`, navigates via the router.
  - `@lib/src/shopping_carts/presentation/widgens/shopping_cart_icon_widget.dart` — stops using `navigatorKey`, navigates via router context.
  - The five screens (mechanical edits — remove the embedded `DashboardWidget`):
    - `@lib/src/movies/presentation/views/movie_view.dart` (`MoviesView`, no args)
    - `@lib/src/movie_sessions/presentation/views/movie_session_view.dart` (`MovieSessionsView`, arg `String movieId`)
    - `@lib/src/seats/presentation/views/seats_view.dart` (`SeatsView`, arg `MovieSession movieSession`)
    - `@lib/src/shopping_carts/presentation/views/shopping_cart_view.dart` (`ShoppingCartView`, no args)
    - `@lib/src/about/presentation/views/shopping_cart_view.dart` (`AboutUsView`, no args — note the misleading filename)
- **Localization (existing `AppLocalizations`, NOT `slang`):**
  - `@lib/l10n/app_en.arb`, `@lib/l10n/app_es.arb`, `@lib/l10n/app_ru.arb` — add the 404 keys.
- **DI:** `@lib/injection_container.dart` — manual `get_it`; register `AppRouter` here.
- **Prior art / safety net:**
  - `@test/features/platform/0001_flutter_dart_deps_migration/flutter_dart_deps_migration_outside_in_test.dart` — migration outside-in test shape (one file, several `test(...)` cases, gate on highest-risk paths).
  - `@test/features/platform/0001_flutter_dart_deps_migration/movies_screen_smoke_test.dart` (and the two sibling smoke tests) — Module G safety net + widget-test idiom (real Cubit + mocktail use-case, no `bloc_test` dependency).

### DO NOT READ

- Any `data/` / `domain/` of the five features beyond what is needed to know a screen's
  constructor signature and its per-route `BlocProvider`s (those are already visible in
  `router.main.dart`). This slice does not touch use-cases, adapters, or DTOs.
- The shopping-cart **Cubit state machine** (create-cart dialog desync) — explicitly out
  of scope (Audit Part 4). Reading it will tempt scope creep.
- Other slices' specs.

---

## 3. Navigation Contract (replaces the "API" block)

There is **no HTTP**. The implementation must realize this exact route table and the three
behavioral guarantees.

### Route table (typed)

| Route page | Path | Args (typed) | Parent | In menu? |
|---|---|---|---|---|
| `HomeShell` (shell) | `/` | — | root | — |
| `MoviesRoute` | `''` (initial child) | none | `HomeShell` | ✅ Movies |
| `AboutRoute` | `about` | none | `HomeShell` | ✅ About Us |
| `ShoppingCartRoute` | `cart` | none | `HomeShell` | ✅ Shopping Cart |
| `MovieSessionsRoute` | `sessions` | `String movieId` (**required**) | `HomeShell` | ❌ (funnel) |
| `SeatsRoute` | `seats` | `MovieSession movieSession` (**required**) | `HomeShell` | ❌ (funnel) |
| `NotFoundRoute` | `*` (catch-all, last) | none | `HomeShell` | ❌ |

- The menu has **three** items only (Movies, About Us, Shopping Cart) — matching today's
  `DashboardWidget`. Movie Sessions and Seats are reached by in-funnel navigation, never
  from the menu.
- Required args mean a **missing argument is a compile/build error**, not a silent
  fallback. This is the whole point of replacing `generateRoute`.
- Paths are kept simple/relative; **deep-link/path-URL strategy and web-refresh survival
  are out of scope** (parity with today — refresh already breaks).

### Behavioral guarantees (the acceptance contract)

1. **No-op on active.** Tapping the menu item for the screen already shown adds **no**
   second instance of that screen. Use the router's **`navigate`** semantics (replace the
   active child), not `push`.
2. **Bounded stack.** Bouncing Movies ↔ About repeatedly does **not** grow the navigation
   stack without bound (one Back press behaves predictably).
3. **Honest 404.** An unknown/unmatched route renders **`NotFoundView`** (localized "page
   not found" + a button back to the main screen), **never** a silent Movies screen.
4. **Funnel parity (unchanged behavior — no new gate).** Movies → Movie Sessions → Seats →
   Cart still works; only its argument mechanism changes (typed constructors). Funnel
   `push` calls still build a back stack intentionally.

### Active-item state

The menu's active-item highlight stops being a cosmetic `route:` string passed in by each
screen. It is derived from the router's **current active child route** (read inside
`HomeShell`), so it stays correct after `navigate`.

---

## 4. Target structure

New / moved files (screens stay under `lib/src/` per the migration rules — only navigation
wiring moves):

```
lib/
├── core/
│   └── routing/
│       └── app_router.dart            # NEW — @AutoRouterConfig RootStackRouter.
│                                       #       The single typed route table + shell def.
│                                       #       REPLACES core/services/router.main.dart.
│       └── app_router.gr.dart         # GENERATED by build_runner.
├── src/
│   └── home/
│       └── presentation/
│           ├── home_shell.dart        # NEW — @RoutePage shell:
│           │                           #   Scaffold(appBar: HomeAppBar, body: AutoRouter())
│           │                           #   + hosts the menu (DashboardWidget) once.
│           └── views/
│               └── not_found_view.dart # NEW — @RoutePage 404 page, localized + "back home".
```

Route-page wrappers for the five screens (CRITICAL — they carry the per-route
`BlocProvider`s that today live in `generateRoute`). Co-locate each next to its view, e.g.:

```
lib/src/movies/presentation/views/movie_view.dart            # +@RoutePage wrapper MoviesRoute (provides MovieTheaterCubit)
lib/src/movie_sessions/presentation/views/movie_session_view.dart  # +MovieSessionsRoute (provides MovieSessionBloc; arg movieId)
lib/src/seats/presentation/views/seats_view.dart             # +SeatsRoute (provides SeatBloc + CinemaHallInfoBloc; arg movieSession)
lib/src/shopping_carts/presentation/views/shopping_cart_view.dart  # +ShoppingCartRoute (no extra provider today)
lib/src/about/presentation/views/shopping_cart_view.dart     # +AboutRoute (no provider)
```

> The wrapper may be a small `@RoutePage()` class in the same file as the view, or the
> view annotated directly with the `BlocProvider` wiring moved into it. Either is fine —
> the **non-negotiable** part is that each route page reproduces the exact
> `MultiBlocProvider` set that `generateRoute` supplied for that screen.

Removed after migration:

```
lib/core/services/router.main.dart     # replaced by core/routing/app_router.dart
```

`DashboardWidget` / `MenuItemWidget` are **modified in place** (not removed): they lose the
`route:` string input and navigate via the router.

---

## 5. What to do — step by step

### Step 0 — Dependency (needs user approval per CLAUDE.md; confirmed in grilling)

Add to `pubspec.yaml`: `auto_route: ^9.x` (deps) and `auto_route_generator: ^9.x`
(dev-deps). `build_runner` is already present. **Ask before adding** if not already
greenlit in-session.

### Step 1 — `AppRouter` (new, deep module) — `lib/core/routing/app_router.dart`

- `@AutoRouterConfig()` class extending `RootStackRouter`.
- Override `routes` with the table from §3: an `AutoRoute(page: HomeShell.page, path: '/')`
  whose `children:` are `MoviesRoute` (initial), `AboutRoute`, `ShoppingCartRoute`,
  `MovieSessionsRoute`, `SeatsRoute`, and **last** the catch-all `NotFoundRoute`
  (`path: '*'`).
- This class encapsulates **all** route resolution that used to live in `generateRoute`.
  Keep it simple and declarative — no logic beyond the table.

### Step 2 — `HomeShell` (new) — `lib/src/home/presentation/home_shell.dart`

- `@RoutePage()` widget returning
  `Scaffold(appBar: HomeAppBar(...), body: const AutoRouter())`.
- Render the menu (`DashboardWidget`) **once**, inside the shell (top of body or app bar
  area — preserve current visual placement/parity).
- `HomeAppBar` no longer receives a `GlobalKey<NavigatorState>` (see Step 6).

### Step 3 — `NotFoundView` (new) — `lib/src/home/presentation/views/not_found_view.dart`

- `@RoutePage()` page showing a localized "page not found" message + a button that
  navigates back to the main screen (`context.router.navigate(const MoviesRoute())` or
  `replaceAll` to the shell root).
- Text via `AppLocalizations.of(context)!.<key>` — **not** `slang`.

### Step 4 — Route-page wrappers for the five screens (CRITICAL)

For each screen create/annotate a `@RoutePage()` that wraps the existing `*View` in the
**same** `BlocProvider`s `generateRoute` used:

- `MoviesRoute` → `MultiBlocProvider([BlocProvider(create: (_) => MovieTheaterCubit(getIt.get()))], child: MoviesView())`.
- `MovieSessionsRoute({required String movieId})` → `BlocProvider(create: (_) => MovieSessionBloc(getIt.get())), child: MovieSessionsView(movieId)`.
- `SeatsRoute({required MovieSession movieSession})` → `MultiBlocProvider([SeatBloc(getIt.get(), getIt.get()), CinemaHallInfoBloc(getIt.get())], child: SeatsView(movieSession))`.
- `ShoppingCartRoute` → `ShoppingCartView()` (no extra provider today; the global
  `ShoppingCartCubit` stays provided from `main.dart`).
- `AboutRoute` → `AboutUsView()`.

> **CRITICAL:** If a provider is dropped here, the screen will throw `ProviderNotFound` at
> runtime even though it compiles. The Module G smoke tests will catch a build failure but
> not necessarily a missing provider on a deeper path — verify each screen renders.

### Step 5 — Menu navigation (modified) — `dashboard_widget.dart` + `menu_item_widget.dart`

- Replace `Navigator.pushNamed(context, navigateId)` in `MenuItemWidget` with router
  **navigate** to the corresponding typed route (`context.router.navigate(const MoviesRoute())`,
  `AboutRoute()`, `ShoppingCartRoute()`).
- Drop the passed-in `route:` string. Derive the active/bold state from the router's
  current child route (read in `DashboardWidget`/`HomeShell`), so guarantee #1 holds.
- `DashboardWidget` no longer needs a `route` constructor arg.

### Step 6 — `HomeAppBar` + shopping-cart icon (modified, mechanical)

- `HomeAppBar`: drop the `GlobalKey<NavigatorState>` constructor param; it is hosted by the
  shell and uses router context.
- `ShoppingCartIconWidget`: drop `navigatorKey`; replace
  `widget.navigatorKey.currentState?.pushNamed(ShoppingCartView.id)` with
  `context.router.navigate(const ShoppingCartRoute())`.

### Step 7 — Screens (modified, mechanical)

- Remove the embedded `const DashboardWidget(route: <X>.id)` from all five screens — the
  menu now lives once in `HomeShell`. Keep everything else (parity).
- Replace in-funnel `Navigator.pushNamed(...)` calls with typed router pushes:
  - `MoviesView.movieSeat` → `context.router.push(MovieSessionsRoute(movieId: movie.id))`.
  - `MovieSessionsView.pressMovieSession` → `context.router.push(SeatsRoute(movieSession: movieSession))`.
  - `SeatsView.movieSeat` → `context.router.push(MovieSessionsRoute(movieId: movieId))`.

### Step 8 — `main.dart` (modified)

- Remove `navigatorKey`, the nested `Navigator(onGenerateRoute: generateRoute)`, and the
  `import 'core/services/router.main.dart'`.
- Swap `MaterialApp(... home: ConnectivitySafeAreaWidget(child: Scaffold(...)))` for
  `MaterialApp.router(routerConfig: getIt<AppRouter>().config())`, keeping the
  `ConnectivitySafeAreaWidget`, theme, and localization delegates wired (wrap via the
  router's `builder` if needed for the connectivity overlay). **Do not** touch the overlay
  bug itself (Audit §2, out of scope) — just preserve current placement.

### Step 9 — DI — `lib/injection_container.dart`

- Register `AppRouter` with the **existing manual `get_it`** (e.g.
  `getIt.registerLazySingleton<AppRouter>(() => AppRouter())`). **No `injectable`.**

### Step 10 — Localization — `app_en.arb` / `app_es.arb` / `app_ru.arb`

- Add the 404 keys (e.g. `page_not_found_title`, `page_not_found_message`,
  `back_to_home`) to all three ARBs; regenerate `AppLocalizations`.

### Step 11 — Codegen + cleanup

- `dart run build_runner build --delete-conflicting-outputs` (generates `app_router.gr.dart`).
- Delete `lib/core/services/router.main.dart`.
- `dart format .`, `dart analyze` (no warnings).

---

## 6. Tests

> Per CLAUDE.md default coverage, **adapted** to this slice: there is **no adapter,
> use-case, or Cubit** introduced, so those default layers do not apply. The new
> observable surfaces are navigation behavior + two widgets.

`test/features/platform/0002_navigation_auto_route_shell_migration/`:

### a) `navigation_shell_migration_outside_in_test.dart` (the RED acceptance gate)

One file, **three** `test(...)`/`testWidgets(...)` cases — one per changed behavior:

1. **No-op on active** — pump the app at `MoviesRoute`; tap the Movies menu item; assert
   exactly **one** `MoviesView` in the tree (no duplicate).
2. **Bounded stack** — bounce Movies ↔ About several times via the menu; assert the router
   stack depth stays bounded (does not grow per tap).
3. **Honest 404** — navigate to an unknown route; assert `NotFoundView` is shown and
   `MoviesView` is **not**.

The booking funnel gets **no** new red case (unchanged behavior) — it stays covered by the
Module G smoke tests, kept green as the parity safety net.

### b) Default widget tests

- `home_shell_test.dart` — shell builds; app bar + menu present; child `AutoRouter` area
  renders the initial (Movies) child; active menu item is highlighted.
- `not_found_view_test.dart` — renders localized title/message; the "back home" button is
  present and triggers navigation (verify with a mocked/stubbed router or a
  `findsOneWidget` on the navigation effect).

### c) Parity safety net (not new — must stay green)

- The three Module G smoke tests
  (`movies_screen_smoke_test.dart`, `movie_session_screen_smoke_test.dart`,
  `auth_flow_smoke_test.dart`) must remain green through the migration.

---

## 7. Report (what the implementing agent must hand back)

- Files created / modified / deleted (expect: `app_router.dart` + `.gr.dart` created;
  `home_shell.dart`, `not_found_view.dart` created; five screens, menu widgets, app bar,
  cart icon, `main.dart`, `injection_container.dart`, three ARBs modified;
  `router.main.dart` deleted).
- Confirmation that **no** use-case/adapter/DTO and **no** other slice's logic was touched.
- Confirmation each of the five screens renders (no `ProviderNotFound`) — the per-route
  `BlocProvider`s were correctly migrated.
- The new outside-in test is **RED** for the right reason before implementation, and the
  three Module G smoke tests are **GREEN** after.
- `dart format` clean, `dart analyze` clean, `build_runner` ran.

---

## 8. What NOT to do (scope fences)

- ❌ Do **not** add `injectable`, `slang`, or a path-URL/deep-link strategy — each is a
  separate migration axis. Router uses manual `get_it`; 404 text uses `AppLocalizations`.
- ❌ Do **not** attempt to fix the create-cart dialog desync (Audit Part 4) — only the
  root/inner navigator mismatch is resolved **for free** by removing the nested
  `Navigator`. Do not touch the `ShoppingCartCubit` state machine.
- ❌ Do **not** touch the connectivity overlay (`connectivity_safe_area_widget`) bug
  (Audit §2) — just preserve its placement under the new router.
- ❌ Do **not** hide the menu during the booking funnel — menu stays visible on all five
  screens (parity; a future UX slice).
- ❌ Do **not** re-architect the screens beyond removing the embedded `DashboardWidget` and
  swapping the navigation calls. No renames of `*Repo`, no folder restructure of the
  legacy screens.
- ❌ Do **not** add a new red test for the booking funnel — its behavior is unchanged.
- ❌ Do **not** silently drop a per-route `BlocProvider` when moving wiring out of
  `generateRoute`.
```
