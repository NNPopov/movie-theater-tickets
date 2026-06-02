# 0001 · flutter_dart_deps_migration — Validation Checklist

> Migration slice. Manual scenarios verify the build/resolution pipeline and the
> preserved runtime behavior on the **web** target (the only verification target
> on this machine). The code-review checklist traces the N-requirements.
> Note: this slice uses `intl` + gen-l10n, **not** `slang`; the universal-tail
> `slang` line is replaced with the gen-l10n / `flutter build web` gates this
> slice actually relies on.

## Manual testing

| # | Step | Expected result |
|---|---|---|
| M1 | Run `flutter pub get` on a clean checkout of the branch | Resolves with no error, no manual edits (F1, F2) |
| M2 | Inspect resolved `intl` in `pubspec.lock` | Resolves to `0.20.2`, no conflict with `flutter_localizations` (F2) |
| M3 | Run `dart run build_runner build --delete-conflicting-outputs` | Completes clean; `movie.freezed.dart`/`.g.dart` and `movie_session.*` regenerated (F3) |
| M4 | Run `dart analyze` | Zero errors reported (F4) |
| M5 | Run `flutter build web` | Build succeeds (F5) |
| M6 | Launch web app, switch locale to en, then es, then ru | UI strings render correctly in each locale; no missing-key fallbacks (F6) |
| M7 | Open the Movies screen in the web app | Movie carousel renders and scrolls (F7) |
| M8 | Open a movie-session screen in the web app | Session carousel renders and scrolls (F7) |
| M9 | Add a seat/item to the shopping cart, reload the page | Cart still contains the item (local storage round-trip) (F8) |
| M10 | Run the auth flow (login) in the web app | Flow completes and reaches the authenticated state; token decoded, stored, read back (F9) |
| M11 | With a real-time event source active, trigger a server event | The signalr connection delivers the event (or its checkpoint decision is documented) (F10) |
| M12 | Navigate the bloc/cubit-driven screens (movies, sessions, seats, cart, auth) | Each behaves identically to pre-migration; no state-emission regressions (F11) |
| M13 | Full manual web smoke run: carousels → cart → auth → locales in one session | All steps pass end-to-end (F12) |
| M14 | Run `flutter test` (safety-net + existing `shopping_cart_model_test`) | All green, unchanged before and after migration (F13) |
| M15 | Run the slice outside-in test | Green (F14) |

## Code review

- [ ] `environment.sdk` is `^3.9.0` and `environment.flutter` is `'>=3.41.0'` (N1)
- [ ] Every runtime package is under `dependencies`; only `flutter_test`, `flutter_lints`, `freezed`, `json_serializable`, `build_runner`, `mocktail` remain under `dev_dependencies` (N2)
- [ ] Each retained library is on its latest resolvable major; `pubspec.lock` is committed as the record of versions (N3)
- [ ] `movie.dart` and `movie_session.dart` use freezed 3 `abstract class … with _$…` syntax; no `*.freezed.dart` / `*.g.dart` hand-edits in the diff (N4)
- [ ] `movie_session_dto.dart` is unchanged-commented or deleted — not un-commented/migrated (N5)
- [ ] No `slang`, `auto_route`, `retrofit`, `injectable`, or `very_good_analysis` appears in `pubspec.yaml` (N6)
- [ ] No new dependency added beyond version bumps of existing ones (N7)
- [ ] `lib/src/<feature>/{data,domain,presentation}` layout unchanged; no `*Repo` → port/adapter rename, no slice reorg (N8)
- [ ] Each Module-E checkpoint (`localstorage`, `carousel_slider`, `flutter_web_auth_2`, `signalr_netcore`) has a recorded push-through / pin-below decision (N9)
- [ ] Only blocking lint errors/warnings fixed; new `info` findings left untouched (no mass `dart fix` diff) (N10)
- [ ] Commit history shows a dedicated branch off `development` with per-module (A–F) commits (N11)
- [ ] `dart format .` produces no diff (N12)
- [ ] Safety-net tests assert observable behavior, not package internals (N13)
- [ ] No `bloc_test` units added for the 8→9 bump; only the three agreed safety-net surfaces tested (N14)
- [ ] `dart run build_runner build --delete-conflicting-outputs` — no errors
- [ ] gen-l10n / ARB regenerate for en/es/ru — no errors (this slice's localization gate; replaces `slang`)
- [ ] `dart analyze` — no warnings
- [ ] `flutter build web` — succeeds
- [ ] All tests green
