# 0002 · navigation_auto_route_shell_migration — Validation Checklist

## Manual testing

| # | Step | Expected result |
|---|---|---|
| M1 | Launch the app | App opens on the Movies screen inside the shell; top app bar and three-item menu (Movies, About Us, Shopping Cart) are visible. (F1, F2) |
| M2 | While on Movies, tap the **Movies** menu item | Nothing happens — no second Movies screen appears, no flicker/duplicate. (F3) |
| M3 | Tap **About Us**, then tap **Movies**, then **About Us**, repeating ~6 times | Each tap switches the content; pressing Back once afterwards leaves the shell predictably — the stack did not grow one entry per tap. (F4) |
| M4 | Observe the menu while switching between Movies and About Us | The menu item for the currently shown screen is highlighted (bold); the highlight moves with each switch. (F5) |
| M5 | Switch to each of the three menu screens and the funnel screens | The top app bar and the menu stay put; only the content area below changes. (F1) |
| M6 | Manually navigate to an unknown route (e.g. via a stale/typed in-app path) | A localized "page not found" screen is shown — **not** the Movies screen. (F6) |
| M7 | On the not-found screen, tap the "back home" action | The app returns to the Movies screen. (F7) |
| M8 | From Movies, select a movie | The Movie Sessions screen for that movie opens (its sessions load for the correct movie id). (F8, F9) |
| M9 | From Movie Sessions, select a session | The Seats screen opens for that movie session (correct hall/seats shown). (F8, F9) |
| M10 | From Seats, use "select another session" / go back through the funnel | Navigation returns through Movie Sessions without duplicate screens appearing. (F8) |
| M11 | Tap the shopping-cart icon in the app bar from Movies, then from Seats | The Shopping Cart screen opens correctly from both. (F10) |
| M12 | Open the create-shopping-cart dialog during seat selection | The dialog opens once and closes reliably (no stuck/doubled dialog from the navigator mismatch). |
| M13 | Switch the app locale to Spanish, then Russian | The menu labels and the not-found screen text appear in the active locale. (F11) |
| M14 | (Web) Use the browser Back button after several menu switches | Back history behaves predictably and matches the in-app navigation. (F4) |
| M15 | Confirm the menu is visible on all five screens (Movies, Movie Sessions, Seats, Shopping Cart, About Us) | The menu appears on every screen — parity with before the migration. (F2, N10) |

## Code review

- [ ] Navigation uses `auto_route` 9.x as a single shell route with the five screens as children; the nested `Navigator` and `onGenerateRoute` are gone from `main.dart`. (N1)
- [ ] The route table lives in `lib/core/routing/app_router.dart`; `lib/core/services/router.main.dart` is deleted (grep: no references to `generateRoute`/`router.main.dart` remain). (N2)
- [ ] `MovieSessionsRoute` requires `String movieId` and `SeatsRoute` requires `MovieSession movieSession`; omitting an argument fails to compile. (N3)
- [ ] Menu items call `context.router.navigate(...)` (not `push`/`pushNamed`). (N4)
- [ ] Each route page wraps its view in the same `BlocProvider`(s) `generateRoute` supplied (`MovieTheaterCubit`, `MovieSessionBloc`, `SeatBloc`+`CinemaHallInfoBloc`); no screen throws `ProviderNotFound` at runtime. (N5)
- [ ] `AppRouter` is registered through the manual `get_it` in `injection_container.dart`; no `injectable` annotations are added. (N6)
- [ ] The 404 strings are read via `AppLocalizations.of(context)!.<key>`; no hardcoded UI strings; `slang` is not introduced. (N7)
- [ ] The 404 keys exist in `app_en.arb`, `app_es.arb`, and `app_ru.arb`. (N8)
- [ ] `HomeAppBar` and `ShoppingCartIconWidget` no longer take/use a `GlobalKey<NavigatorState>`; they navigate via the router context. (N9)
- [ ] `DashboardWidget` is rendered once inside `HomeShell` and removed from all five screens; the `route:` string arg is gone. (N10)
- [ ] Legacy screens changed only minimally (embedded menu removed, navigation calls swapped); no `*Repo` renames or folder restructuring. (N11)
- [ ] Diff touches no use-case, adapter, or DTO files. (N12)
- [ ] The three Module G smoke tests (`movies_screen_smoke_test.dart`, `movie_session_screen_smoke_test.dart`, `auth_flow_smoke_test.dart`) still pass. (N14)
- [ ] `auto_route` + `auto_route_generator` added to `pubspec.yaml` only after explicit user approval. (N15)
- [ ] `dart run build_runner build --delete-conflicting-outputs` — no errors (generates `app_router.gr.dart`). (N13)
- [ ] `flutter gen-l10n` (or build-triggered gen-l10n) regenerated `AppLocalizations` for the new keys — no errors. (N8) *(This slice uses ARB/gen-l10n, not `slang`.)*
- [ ] `dart analyze` — no warnings. (N13)
- [ ] All tests green (the new outside-in acceptance test + the two new widget tests + the Module G safety net).
