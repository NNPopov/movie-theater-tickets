# 0001 · flutter_dart_deps_migration — Requirements

> This is a migration slice. "Functional" requirements describe observable
> outcomes of the toolchain/build pipeline and the preserved runtime behavior of
> the app; "non-functional" requirements are the scope and architectural
> invariants the migration must not violate.

## Functional Requirements

| ID | Requirement |
|---|---|
| F1 | `flutter pub get` resolves successfully on the installed toolchain (Flutter 3.41 / Dart 3.11) with no manual intervention. |
| F2 | The `intl` constraint resolves against the SDK-pinned `intl 0.20.2` with no version conflict. |
| F3 | `dart run build_runner build --delete-conflicting-outputs` completes cleanly and regenerates all `*.freezed.dart` / `*.g.dart` outputs against the new majors. |
| F4 | `dart analyze` reports no errors after the migration. |
| F5 | `flutter build web` succeeds. |
| F6 | The localization pipeline still generates and renders for all three locales (en, es, ru). |
| F7 | The movies and movie-session screens render their carousels after the `carousel_slider` bump. |
| F8 | The shopping cart round-trips an item through local storage after the `localstorage` bump. |
| F9 | The auth flow reaches its expected authenticated state after the `flutter_web_auth_2` / `jwt_decoder` / secure-storage bumps. |
| F10 | Real-time messaging via `signalr_netcore` continues to function (or its checkpoint decision is recorded). |
| F11 | All 31 bloc/cubit-driven screens behave identically after the `flutter_bloc` 8→9 bump. |
| F12 | A manual web smoke run passes end-to-end (carousels, cart, auth, locales). |
| F13 | The safety-net tests (movies/sessions screens, shopping cart, auth flow) and the existing `shopping_cart_model_test` pass unchanged before and after the migration. |
| F14 | The slice's outside-in acceptance test passes (green). |

## Non-functional Requirements

| ID | Requirement |
|---|---|
| N1 | `environment` is set to `sdk: ^3.9.0` and `flutter: '>=3.41.0'`, reflecting the installed toolchain. |
| N2 | Every runtime package resides in `dependencies`; only build/test tooling (`flutter_test`, `flutter_lints`, `freezed`, `json_serializable`, `build_runner`, `mocktail`) resides in `dev_dependencies`. |
| N3 | Every retained library is on its latest major version that resolves; exact versions are recorded in `pubspec.lock`, not pinned in spec files. |
| N4 | The two live `@freezed` classes are migrated to freezed 3 syntax (`abstract class … with _$…`); generated files are never hand-edited. |
| N5 | The commented-out `movie_session_dto.dart` is not un-commented or migrated. |
| N6 | No package from the locked-but-not-adopted target stack (`slang`, `auto_route`, `retrofit`, `injectable`, `very_good_analysis`) is introduced. |
| N7 | No new dependency is added to `pubspec.yaml` without explicit user approval. |
| N8 | The existing layered `lib/src/<feature>/{data,domain,presentation}` folder structure is left unchanged; no `*Repo` → port/adapter rename or vertical-slice reorganization. |
| N9 | Any package whose latest major requires a disproportionate rewrite or breaks web triggers a pause-and-decide checkpoint surfaced to the user before committing (`localstorage`, `carousel_slider`, `flutter_web_auth_2`, `signalr_netcore`). |
| N10 | Only blocking lint errors/warnings from `flutter_lints` 6 are fixed; non-blocking `info` findings are left untouched. |
| N11 | The migration is performed on a dedicated branch off `development` with incremental, per-module commits (A–F) so any single step can be rolled back. |
| N12 | `dart format .` produces no diff at completion. |
| N13 | Safety-net tests assert externally observable behavior, not the internal wiring of any package, so they remain valid across the bumps. |
| N14 | Default four-layer test coverage is intentionally overridden: no dedicated `bloc_test` units are added for the 8→9 bump; only the three agreed safety-net surfaces are tested. |

## Out of scope

- The target-stack architectural migration: `slang`, `auto_route`, `retrofit`, `injectable`, `very_good_analysis`, and any port/adapter (`*Repo` → port) restructuring or vertical-slice reorganization.
- Changing the existing layered `lib/src/<feature>/{data,domain,presentation}` folder structure.
- Adding any new dependency.
- A full four-layer test suite for every slice — only the three agreed smoke-test surfaces are written here.
- Mass `dart fix` / style-only cleanup of new `flutter_lints` 6 `info` findings.
- iOS/Android/desktop build verification — web is the single verification target on this machine.
- Publishing the PRD to a remote issue tracker (`gh` unavailable; stored locally).
