# 0002 · navigation_auto_route_shell_migration — Outside-in test spec

> **Boundary note (migration slice).** This slice introduces **no Cubit, no use-case,
> no Dio call** — its public surface is *navigation behavior*, not a Cubit method. So,
> exactly like the precedent migration test
> `0001_flutter_dart_deps_migration/flutter_dart_deps_migration_outside_in_test.dart`
> (which gates at the storage level, not a Cubit), this outside-in test gates at the
> **router + widget level**: it pumps the real app shell and drives navigation through
> the tester, asserting externally observable outcomes (which screen is shown, whether a
> duplicate appears, stack depth, 404 shown). It never asserts `auto_route` internals.

## Goal

Prove that after the migration the shell-based router enforces the three changed
navigation behaviors: tapping the active menu item is a no-op, menu-to-menu bouncing
keeps a bounded stack, and an unknown route renders the localized 404 page instead of
silently showing Movies.

## Entry point

A pumped `MaterialApp.router` driven by the real `AppRouter`, with navigation performed
through the widget tester (tapping menu items) and through `router.pushNamed('/<unknown>')`
for the 404 case. There is no Cubit method to call — the "entry point" is the rendered
app + the router.

## Wired real (production code in the test)

- `AppRouter` (the typed route table + shell definition — the system under test).
- `HomeShell` (persistent app bar + menu hosting `AutoRouter()`).
- `DashboardWidget` / `MenuItemWidget` (the menu that issues `navigate`).
- `NotFoundView` (the catch-all route target).
- The route pages `MoviesRoute`, `AboutRoute`, `ShoppingCartRoute` and their views.

## Mocked (system boundaries only)

- **`get_it` use-cases** that the rendered screens resolve on init (e.g.
  `GetActiveMovies`) are registered as mocktail fakes returning `Right(<empty list>)`,
  so screens build to a stable empty state **without hitting the network**. This is the
  only boundary mocked; it is the navigation that is under test, and navigation is
  observable regardless of each screen's data state.
- No Dio, no real HTTP. No AuthCubit needed for these scenarios.

## Test scenarios

### Scenario 1: tapping the already-active menu item does not duplicate the screen

**Setup:**
- Register the screen use-cases as fakes returning empty results.
- Pump the app; it starts on `MoviesRoute` (initial child of `HomeShell`).

**Act:**
- Tap the **Movies** menu item.
- Pump and settle.

**Expect:**
- Exactly **one** `MoviesView` is in the tree (`findsOneWidget`) — no second instance.
- The shown route is still Movies (no new entry pushed on top of itself).

### Scenario 2: bouncing Movies ↔ About does not grow the stack without bound

**Setup:**
- Same fakes and initial pump as Scenario 1.

**Act:**
- Tap **About Us**, then **Movies**, repeating the pair several times (e.g. 5 round trips),
  pumping and settling between taps.

**Expect:**
- After the bouncing, the router's child stack depth is **bounded** (it does not increase
  one entry per tap — switching is `navigate`/replace, not `push`).
- The currently shown screen matches the last tapped item, and a single back action
  returns to a predictable state rather than unwinding N duplicate entries.

### Scenario 3: an unknown route renders the 404 page, not Movies

**Setup:**
- Same fakes and initial pump as Scenario 1.

**Act:**
- Navigate to an unmatched path (e.g. `router.pushNamed('/totally-unknown')`).
- Pump and settle.

**Expect:**
- `NotFoundView` is shown (`findsOneWidget`).
- `MoviesView` is **not** shown (`findsNothing`) — the silent Movies fallback is gone.

## Out of scope for this test

- The booking funnel Movies → Movie Sessions → Seats → Cart: its behavior is **unchanged**
  by this slice (only the argument mechanism changes), so it gets no new red case here —
  it stays covered by the existing Module G smoke tests kept green as the parity net.
- Per-screen data rendering, loading/error states (covered by each screen's own tests).
- Widget-level details of `HomeShell` and `NotFoundView` (active-item highlight, the
  "back home" button, localized text) — covered by the dedicated widget tests
  `home_shell_test.dart` and `not_found_view_test.dart` written after green.
- The create-cart dialog behavior (out of scope; Audit Part 4).
