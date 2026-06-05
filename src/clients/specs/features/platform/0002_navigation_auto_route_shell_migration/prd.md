# PRD — Navigation: `auto_route` shell migration (Audit Part 1)

- **Slice:** `0002_navigation_auto_route_shell_migration`
- **Feature:** platform
- **Type:** Tracked migration slice (legacy → target stack), navigation only
- **Source:** `docs/audit/legacy-client-audit-2026-06-02.md` §1 (Navigation) + grilling session 2026-06-03
- **Status:** Planned

## Problem Statement

Moving around the app produces a "window-in-window" feeling and apparent freezes.
Tapping a menu item that is already the active screen opens **another copy** of that
screen, and bouncing between menu items (Movies → About → Movies → About…) grows the
navigation stack without bound — the user must press Back as many times as they tapped.
Typing or following an unknown URL silently drops the user onto the Movies screen with
no explanation, and a broken/incomplete link is masked as "yet another Movies screen."
A modal dialog (create shopping cart) can get stuck open or open twice. From the user's
seat the app feels like it stacks duplicate windows and occasionally locks up.

## Solution

Replace the hand-rolled nested `Navigator` + `generateRoute` navigation with the locked
target navigation stack, `auto_route`, structured as a **shell route**: a persistent
`HomeShell` hosts the top app bar and the menu, and the five screens (Movies, Movie
Sessions, Seats, Shopping Cart, About Us) become its child routes. Menu taps switch
child routes instead of pushing new ones, so:

- tapping the already-active menu item does nothing (no duplicate screen);
- bouncing between menu items no longer grows the stack;
- an unknown route lands on an honest, localized **404 "page not found"** screen with a
  way back home, instead of a silent Movies fallback;
- the old root-vs-inner navigator mismatch that destabilized the create-cart dialog
  disappears once the nested `Navigator` is removed.

The user-visible behavior of the booking funnel (Movies → Movie Sessions → Seats →
Cart) is unchanged; only the duplication, the silent fallback, and the navigator
mismatch go away.

## User Stories

1. As a moviegoer, I want tapping the menu item for the screen I am already on to do nothing, so that I do not pile up duplicate screens.
2. As a moviegoer, I want switching between Movies and About repeatedly to keep a single, predictable back history, so that one Back press returns me where I expect.
3. As a moviegoer, I want the top bar and menu to stay put while the content area changes, so that navigation feels stable rather than like windows opening inside windows.
4. As a moviegoer, I want a clear "page not found" screen when I open an unknown or stale link, so that I understand what happened instead of being silently dropped on Movies.
5. As a moviegoer, I want a one-tap way back to the main screen from the not-found page, so that a bad link is not a dead end.
6. As a moviegoer, I want to go Movies → a movie's sessions → seat selection and back without duplicate screens appearing, so that booking feels linear and trustworthy.
7. As a moviegoer, I want the shopping-cart icon to always open the cart screen correctly, so that I can review my cart from anywhere.
8. As a moviegoer, I want the "create shopping cart" dialog to open once and close reliably, so that I am never stuck behind a stuck or doubled dialog.
9. As a moviegoer on the web, I want the Back button history to behave predictably after the migration, so that navigation matches what I expect from a website.
10. As a returning user following an old in-app path, I want broken navigation to surface as a real not-found state rather than a masked Movies screen, so that errors are visible, not hidden.
11. As a developer, I want navigation defined as a typed route table instead of string matching in `generateRoute`, so that a missing or wrong route argument is a compile/build error rather than a silent fallback.
12. As a developer, I want the persistent app bar and menu to live in one shell instead of being embedded inside every screen, so that the menu is defined once.
13. As a developer, I want the existing Module G smoke tests to stay green through the migration, so that I have a safety net proving the booking funnel still works.
14. As a developer, I want a small set of red outside-in tests that lock the new navigation contract (dedup, bounded stack, 404), so that the slice has a clear acceptance gate.
15. As a maintainer, I want this navigation migration tracked as its own slice, so that it is a deliberate step and not a side effect of an unrelated change.

## Implementation Decisions

**Approach.** Do not patch the legacy nested-`Navigator` design in place; migrate
navigation to `auto_route` now, as a dedicated tracked migration slice. The throwaway
cost of an in-place patch (a dedup guard + an explicit fallback) is what makes a patch
pointless when the target migration is happening immediately.

**Dependencies.** Add only `auto_route` and `auto_route_generator` (+ `build_runner`,
already present). Do **not** pull in `injectable`, `slang`, or a path-URL strategy in
this slice — each is a separate migration axis. The router is instantiated/registered
through the existing **manual `get_it`**, not `injectable`.

**Modules built / modified:**

- **`AppRouter` (new, deep module).** The single typed route table + shell definition.
  Simple, rarely-changing surface (the list of routes and their typed arguments);
  encapsulates all route resolution that used to live in `generateRoute`. Replaces
  `router.main.dart`.
- **`HomeShell` (new).** Hosts `Scaffold(appBar: HomeAppBar, body: AutoRouter())`. The
  five screens become its child routes. Removes the manual `Navigator` from `main.dart`.
- **Menu navigation (modified).** Menu items navigate via the router's `navigate`
  semantics (switch child route, no duplicate push) instead of unconditional
  `pushNamed`. The active-item state stops being purely cosmetic.
- **Screens (modified, mechanical).** The embedded menu/dashboard widget is removed from
  each of the five screens; the menu now lives once in `HomeShell`. Menu visibility is
  preserved on all five screens (parity).
- **Route arguments (modified).** Typed route constructors carry the existing payloads —
  `MovieSession` object for Seats, `movieId` string for Movie Sessions. A missing
  argument becomes a build error, removing the silent fallback path.
- **`NotFoundView` (new).** Localized "page not found" page + a button back to the main
  screen, wired as the router's unknown-route target. Text via the existing
  `AppLocalizations` mechanism (not `slang`).
- **Shopping-cart icon (modified, mechanical).** Stops using the passed `GlobalKey<NavigatorState>`; navigates via the router context.

**Behavioral contract:**

- Navigating to the active route is a no-op (no duplicate screen).
- Menu-to-menu bouncing keeps a bounded stack.
- Unknown route → 404 page, never a silent Movies screen.
- Booking funnel behavior is unchanged (parity).

## Testing Decisions

**What a good test is here.** Tests assert externally observable navigation behavior
(which screen is shown, whether a duplicate appears, stack depth, 404 shown) — never the
internal route-resolution mechanism or `auto_route` internals.

**Outside-in acceptance gate.** One file,
`navigation_shell_migration_outside_in_test.dart`, containing **one red test case per
changed behavior** (three total):

1. Tapping the already-active menu item does not add a second instance of that screen.
2. Movies ↔ About bouncing does not grow the stack without bound.
3. An unknown route renders the 404 page, not Movies.

The booking funnel is **not** a changed behavior (only its argument mechanism changes),
so it gets **no new red case**; it is covered by the existing Module G smoke tests, kept
green as the parity safety net.

**Default layer tests.** Per `CLAUDE.md` default coverage, add widget tests for the new
`HomeShell` and `NotFoundView` (observable states), and a navigation/behavior test for
the menu dedup logic. There is no new network adapter, use-case, or Cubit in this slice,
so the adapter/use-case/cubit default layers do not apply.

**Prior art.** `test/features/platform/0001_flutter_dart_deps_migration/flutter_dart_deps_migration_outside_in_test.dart`
— a migration-slice outside-in test: one file, multiple `test(...)` cases, gating on the
highest-risk end-to-end paths the migration touches. Module G smoke tests
(movies/sessions screens + auth flow) are the existing end-to-end safety net.

## Out of Scope

- **Deep linking / path-URL strategy / web refresh survival.** Arguments stay as typed
  objects/primitives via route constructors (parity with today, where refresh already
  breaks). Refetch-by-id and path URLs are a separate later slice.
- **`injectable` migration.** Router uses the existing manual `get_it`.
- **`slang` localization migration.** New 404 text uses the existing `AppLocalizations`.
- **Hiding the menu during the booking funnel** (focused seat-selection UX). Menu stays
  visible on all five screens; this is a future UX slice.
- **Active fix of the create-cart dialog desync (Audit §1.3, second layer).** The
  root/inner navigator mismatch is resolved for free by removing the nested `Navigator`,
  but the flag/`emit`-driven open-close fragility is rooted in the Cubit state machine
  and is deferred to the Audit Part 4 (Cubit cleanup) slice.
- **Audit §2 (overlay freeze), §3 (SignalR/EventHub), §4 (Bloc/Cubit), §5 (other
  deviations).** Separate slices.

## Further Notes

- The hard *freeze* reported by users is most likely Audit §2 (the
  `connectivity_safe_area_widget` `late final` overlay bug), not navigation. Part 1
  addresses the "window-in-window" / duplicate-screen symptom and the masked-error
  symptom; it is not expected to fully resolve a hard lockup on its own.
- The backend slice `0002_content_not_found_404` (in the `.NET` services roadmap) is a
  different layer (HTTP 204 → 404) and does not overlap with this client routing
  fallback; the client 404 page is thematically aligned but independent.
- Adding `auto_route` + `auto_route_generator` requires user approval per `CLAUDE.md`
  (no new dependency without asking) — confirmed during the grilling session.
