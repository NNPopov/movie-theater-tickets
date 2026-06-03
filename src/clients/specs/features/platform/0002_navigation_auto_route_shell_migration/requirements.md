# 0002 · navigation_auto_route_shell_migration — Requirements

## Functional Requirements

| ID | Requirement |
|---|---|
| F1 | A persistent shell (`HomeShell`) keeps the top app bar and the menu in place while only the content area changes between screens. |
| F2 | The menu shows exactly three items — Movies, About Us, Shopping Cart — visible on all five screens. |
| F3 | Tapping the menu item for the screen already shown adds no second instance of that screen (no-op on active route). |
| F4 | Repeatedly switching between Movies and About via the menu keeps the back stack bounded rather than growing one entry per tap. |
| F5 | The menu item for the currently shown screen is visually highlighted, and the highlight stays correct after a menu switch. |
| F6 | Navigating to an unknown or unmatched route shows a localized "page not found" screen, never a silent Movies screen. |
| F7 | The not-found screen provides a one-tap action that returns the user to the main (Movies) screen. |
| F8 | The booking funnel Movies → Movie Sessions → Seats → Cart remains navigable with the same observable behavior as before the migration. |
| F9 | Selecting a movie opens its Movie Sessions screen carrying that movie's id; selecting a session opens the Seats screen carrying that movie session. |
| F10 | The shopping-cart icon in the app bar opens the Shopping Cart screen from any screen. |
| F11 | The not-found screen and menu labels are presented in the active locale (English, Spanish, or Russian). |

## Non-functional Requirements

| ID | Requirement |
|---|---|
| N1 | Navigation is implemented with `auto_route` 9.x structured as a single shell route with the five screens as child routes; the hand-rolled nested `Navigator` and `generateRoute` are removed. |
| N2 | The route table lives in a single `AppRouter` (`lib/core/routing/app_router.dart`) and `lib/core/services/router.main.dart` is deleted. |
| N3 | Route arguments are typed and required (`String movieId` for Movie Sessions, `MovieSession movieSession` for Seats), so a missing argument is a compile/build error, not a runtime fallback. |
| N4 | Menu navigation uses the router's `navigate` (replace active child) semantics, not an unconditional push. |
| N5 | Each route page reproduces the exact `BlocProvider` set that `generateRoute` previously supplied for that screen, so no screen throws `ProviderNotFound`. |
| N6 | `AppRouter` is registered through the existing manual `get_it`; `injectable` is not introduced. |
| N7 | All new user-facing strings (404 page) use the existing `AppLocalizations` (ARB/gen-l10n) mechanism; `slang` is not introduced, and no UI string is hardcoded. |
| N8 | The 404 keys are added to all three ARB files (`app_en.arb`, `app_es.arb`, `app_ru.arb`) and `AppLocalizations` is regenerated. |
| N9 | The app bar and shopping-cart icon navigate via the router context and no longer depend on a `GlobalKey<NavigatorState>`. |
| N10 | The embedded menu (`DashboardWidget`) is rendered once inside `HomeShell` and removed from each of the five screens, with menu visibility preserved on all five (parity). |
| N11 | Legacy screens under `lib/src/` are changed only minimally (remove embedded menu, swap navigation calls); no `*Repo` renames, folder restructuring, or unrelated re-architecture. |
| N12 | No use-case, adapter, or DTO is added or modified; the slice touches navigation wiring only. |
| N13 | `app_router.gr.dart` is generated via `build_runner`, and `dart format` and `dart analyze` are clean before the slice is declared done. |
| N14 | The three Module G smoke tests remain green throughout the migration as the parity safety net. |
| N15 | Adding `auto_route` and `auto_route_generator` to `pubspec.yaml` is done only with explicit user approval. |

## Out of scope

- Deep linking, path-URL strategy, and web-refresh survival (arguments stay as typed objects/primitives; refetch-by-id is a later slice).
- `injectable` DI migration.
- `slang` localization migration.
- Hiding the menu during the booking funnel (focused seat-selection UX).
- Active fix of the create-cart dialog desync (`ShoppingCartCubit` state machine — Audit Part 4); only the root/inner navigator mismatch is resolved for free.
- Audit §2 (connectivity overlay freeze), §3 (SignalR/EventHub), §4 (Bloc/Cubit cleanup), §5 (other deviations).
