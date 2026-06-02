# PRD — Flutter/Dart & dependency migration (gentle)

Slice: `0001_flutter_dart_deps_migration` · Feature: `platform` · Status: 📋

## Problem Statement

As a developer on this project, I currently cannot get a clean dependency
resolution or build on my machine. The installed toolchain is fresh
(Flutter 3.41.7 / Dart 3.11.5), but the project still declares old constraints
(`sdk: '>=3.2.3 <4.0.0'`, `flutter: ^3.16.4`) and an `intl 0.18.1` pin that
directly conflicts with the `intl 0.20.2` that `flutter_localizations` forces
from the SDK. The result is that `flutter pub get` fails outright, so the app
does not build and no further work can proceed. On top of that, the
`pubspec.yaml` is structurally wrong: almost every runtime package (`dio`,
`get_it`, `flutter_bloc`, `flutter_secure_storage`, `signalr_netcore`,
`jwt_decoder`, etc.) is declared under `dev_dependencies`, and most package
versions are years behind their current releases.

I want a **gentle** migration: the same set of libraries and the same
architecture as today, just moved to versions that resolve and build on the
current toolchain — without being forced into the larger architectural
migration (slang / auto_route / retrofit / injectable / very_good_analysis)
described in `CLAUDE.md`.

## Solution

As a developer, after this slice I can run `flutter pub get`, code generation,
and `flutter build web` cleanly on the installed toolchain, with every existing
library bumped to its latest major version while keeping the exact same stack
and architecture. Specifically:

- The project resolves and builds on Flutter 3.41 / Dart 3.11 without manual
  intervention.
- `pubspec.yaml` is corrected so runtime packages live in `dependencies` and
  only test/codegen tooling stays in `dev_dependencies`.
- Every retained library is on its latest major (e.g. `flutter_bloc` 9,
  `freezed` 3, `intl` 0.20.2, `get_it` 8, `flutter_lints` 6), with breaking
  changes handled in the existing code.
- A thin safety net of smoke/widget tests guards the riskiest screens before
  the migration touches them, so regressions surface immediately.
- Nothing in the locked-but-not-yet-adopted target stack (slang, auto_route,
  retrofit, injectable, very_good_analysis) is introduced — that remains a
  separate, deliberate migration.

## User Stories

1. As a developer, I want `flutter pub get` to succeed on the installed toolchain, so that I can build and run the app at all.
2. As a developer, I want the `intl` constraint aligned with the SDK-pinned `intl 0.20.2`, so that localization resolves without a version conflict.
3. As a developer, I want the `environment` SDK/Flutter floors set to match the installed toolchain (`sdk: ^3.9.0`, `flutter: '>=3.41.0'`), so that the declared constraints reflect reality and unlock newer APIs.
4. As a developer, I want all runtime packages moved from `dev_dependencies` into `dependencies`, so that the dependency graph is correct and not just incidentally working.
5. As a developer, I want only build/test tooling (`build_runner`, `freezed`, `json_serializable`, `mocktail`, `flutter_lints`, `flutter_test`) to remain in `dev_dependencies`, so that the split is semantically right.
6. As a developer, I want `freezed` and `freezed_annotation` upgraded to v3, so that code generation runs on the current major and stays supported.
7. As a developer, I want the three existing freezed-annotated files updated to the v3 class-modifier syntax, so that generation succeeds after the bump.
8. As a developer, I want `json_serializable` and `json_annotation` upgraded to their latest majors, so that model serialization keeps generating cleanly.
9. As a developer, I want `build_runner` upgraded and a clean `--delete-conflicting-outputs` run to pass, so that all generated files are regenerated against the new majors.
10. As a developer, I want `flutter_bloc` upgraded from 8 to 9 across all 31 bloc/cubit files, so that state management runs on the current major.
11. As a developer, I want every removed/renamed bloc 9 API in the existing cubits and widgets adjusted, so that the app compiles and behaves identically after the bump.
12. As a developer, I want `intl` localization (ARB + gen-l10n) verified after the bump, so that the three locales (en/es/ru) still generate and render.
13. As a developer, I want `get_it` upgraded to v8 and DI registration adjusted for any breaking changes, so that the container still wires up at startup.
14. As a developer, I want `dio` and its cache interceptor kept current, so that networking continues to work.
15. As a developer, I want `flutter_secure_storage` upgraded and verified on web, so that secret storage keeps working on the verification target.
16. As a developer, I want the `localstorage` package's major bump evaluated before adoption, so that the shopping-cart local adapter is rewritten deliberately rather than silently broken.
17. As a developer, I want the `carousel_slider` major bump evaluated before adoption, so that the movies and movie-session views keep rendering their carousels.
18. As a developer, I want the `flutter_web_auth_2` major bump evaluated before adoption, so that the web auth flow keeps working on the web target.
19. As a developer, I want `signalr_netcore` evaluated before adoption, so that real-time messaging is not silently broken.
20. As a developer, I want a pause-and-decide checkpoint on any package whose latest major requires a disproportionate rewrite or breaks web, so that I can choose between pushing through or pinning one major below.
21. As a developer, I want `flutter_lints` upgraded to v6 with only blocking issues fixed, so that I get the newer ruleset without a massive style-only diff.
22. As a developer, I want non-blocking new lint `info` findings left untouched, so that the migration diff stays focused and reviewable.
23. As a developer, I want minimal smoke/widget tests for the Movies/sessions screens written before migrating, so that carousel regressions are caught.
24. As a developer, I want a smoke test around the shopping cart (and the existing `shopping_cart_model_test` kept green), so that `localstorage`/freezed/json regressions are caught.
25. As a developer, I want a smoke test for the auth flow, so that `flutter_web_auth_2` / `jwt_decoder` / secure-storage regressions are caught.
26. As a developer, I want the whole migration done on a dedicated branch with incremental commits per module, so that any single step can be rolled back.
27. As a developer, I want `dart analyze` to report no errors after migration, so that the codebase is in a known-good state.
28. As a developer, I want `flutter build web` to succeed and a manual web smoke run to pass, so that I have confidence the app works end-to-end.
29. As a developer, I want the existing layered `lib/src/<feature>/{data,domain,presentation}` structure left untouched, so that this stays a version migration and not an architecture rewrite.
30. As a developer, I want no new packages added without explicit approval, so that the dependency surface does not grow during a maintenance task.

## Implementation Decisions

**Scope boundary.** Same stack, same architecture — only versions change.
Explicitly excluded: `slang`, `auto_route`, `retrofit`, `injectable`,
`very_good_analysis`, and any port/adapter restructuring. `intl`, hand-written
`dio` repos, `get_it` without `injectable`, `flutter_bloc`, and `flutter_lints`
are all retained.

**Major-version policy.** Every retained library goes to its latest major.
Forced bumps (must happen for resolution): `intl` → `0.20.2`. High-surface
bumps: `flutter_bloc` 8 → 9 (31 files), `freezed`/`freezed_annotation` 2 → 3
(3 files + class-modifier syntax change). Other bumps: `json_annotation`,
`json_serializable`, `build_runner`, `get_it` 7 → 8, `flutter_lints` 3 → 6,
plus current releases of `dio`, `dio_cache_interceptor`, `flutter_secure_storage`,
`logger`, `jwt_decoder`, `flutter_dotenv`, `cupertino_icons`, `equatable`,
`dartz`, `mocktail`, `flutter_guid`.

**Migration modules (and intended commit boundaries):**

- **A. Toolchain & pubspec** — set `environment` floors to the installed
  toolchain; move runtime packages into `dependencies`; keep only build/test
  tooling in `dev_dependencies`.
- **B. Codegen pipeline** — `freezed` 3, `json_*`, `build_runner`; migrate the
  three freezed files to v3 syntax; clean regeneration with
  `--delete-conflicting-outputs`.
- **C. State management** — `flutter_bloc` 8 → 9; adjust removed/renamed APIs
  across the 31 bloc/cubit files and their widgets.
- **D. Localization** — `intl` 0.20.2; confirm gen-l10n/ARB still generate for
  en/es/ru.
- **E. Risky adapters** — `localstorage` (shopping-cart local adapter),
  `carousel_slider` (movies + movie-session views), `flutter_web_auth_2` (auth),
  `signalr_netcore`. Each is a pause-and-decide checkpoint: if the latest major
  needs a disproportionate rewrite or breaks web, surface the API diff and
  choose push-through vs. pin-one-major-below before committing.
- **F. Lints** — `flutter_lints` 6; fix only blocking errors/warnings, leave
  non-blocking `info` findings.

**Verification target.** Web (`flutter run -d chrome` / `flutter build web`).
The installed dev machine is Windows, so iOS is not buildable here.

**Branching.** A dedicated branch off `development` with incremental,
per-module commits.

**Definition of done.** Smoke tests green · `dart analyze` clean ·
`build_runner build --delete-conflicting-outputs` clean · `flutter build web`
passes · manual web smoke run passes.

## Testing Decisions

**What makes a good test here.** Tests assert externally observable behavior of
the migrated surfaces — a screen renders its key widgets, a cart round-trips an
item through storage, the auth flow reaches its expected state — not the
internal wiring of any package. They exist as a regression net for the bumps,
so they should keep passing unchanged before and after the migration.

**Modules to be tested (safety net, written before migrating):**

- **Movies / movie-session screens** — widget smoke tests, because they exercise
  the `carousel_slider` major bump.
- **Shopping cart** — a widget/unit smoke around the cart, and the existing
  `shopping_cart_model_test` kept green (it depends on freezed/json codegen).
- **Auth flow** — a smoke test covering the `flutter_web_auth_2` / `jwt_decoder`
  / secure-storage path.

**Not in the net (by decision):** dedicated `bloc_test` coverage for the
flutter_bloc 8 → 9 bump — that surface is verified via the screen smoke tests,
`dart analyze`, and the manual web run rather than new bloc unit tests.

**Prior art.** `test/lib/shopping_cart/data/models/shopping_cart_model_test.dart`
is the one existing test and the reference for model-level tests. New widget
smoke tests follow standard `flutter_test` patterns with mocked cubits where a
screen has one (per `CLAUDE.md` rules), using `mocktail` for mocks.

## Out of Scope

- The target-stack architectural migration: `slang`, `auto_route`, `retrofit`,
  `injectable`, `very_good_analysis`, and any port/adapter (`*Repo` → port)
  restructuring or vertical-slice reorganization.
- Changing the existing layered `lib/src/<feature>/{data,domain,presentation}`
  folder structure.
- Adding any new dependency.
- A full test suite across all four layers for every slice — only the three
  agreed smoke-test surfaces are written here.
- Mass `dart fix` / style-only cleanup of new `flutter_lints` 6 `info` findings.
- iOS/Android/desktop build verification — web is the single verification target
  on this machine.
- Publishing this PRD to a remote issue tracker (`gh` is unavailable; saved
  locally instead).

## Further Notes

- The project currently does not `pub get` at all, so this migration is a
  prerequisite for any other work, not an optional cleanup.
- Highest-risk items: `freezed` 2 → 3 (syntax change, codegen) and
  `flutter_bloc` 8 → 9 (largest surface, 31 files). The pause-and-decide
  checkpoints concentrate on `localstorage`, `carousel_slider`,
  `flutter_web_auth_2`, and `signalr_netcore`.
- `CLAUDE.md` marks this repo as legacy mid-migration; this slice deliberately
  stays within the "modifying existing legacy code — minimal change" lane and
  does not re-architect.
- No issue-tracker/triage-label vocabulary was configured and `gh` is not
  installed, so the `needs-triage` step could not run; this PRD is stored under
  `specs/features/platform/0001_flutter_dart_deps_migration/`.
