# Plan — Flutter/Dart & dependency migration (gentle)

Slice: `0001_flutter_dart_deps_migration` · Feature: `platform` · Status: 📋

> This is a **migration** slice, not a feature slice. There is no new port /
> use-case / adapter / cubit. The "plan" is the ordered, per-module migration
> recipe with explicit pause-and-decide checkpoints, derived from `prd.md`.
> Structure follows the six modules **A–F** from the PRD, each a commit boundary.

---

## 1. Goal

Get the project to **resolve, generate, and build web** on the installed
toolchain (Flutter 3.41.7 / Dart 3.11.5) by bumping every retained library to
its latest major — same stack, same layered `lib/src/<feature>/{data,domain,presentation}`
architecture. No `slang` / `auto_route` / `retrofit` / `injectable` /
`very_good_analysis`, no port/adapter restructuring, no new dependencies.

**Acceptance gate:** the outside-in test (`/slice-test-red` →
`flutter test`) is green, `dart analyze` is clean, `build_runner build
--delete-conflicting-outputs` is clean, `flutter build web` passes, manual web
smoke run passes.

---

## 2. Context

### READ
- `@CLAUDE.md` — fully. **Migration lane:** "modifying existing legacy code →
  minimal change, do not re-architect."
- `@pubspec.yaml` — the file being corrected (root of the slice).
- `@specs/features/platform/0001_flutter_dart_deps_migration/prd.md` — the contract.
- The freezed-annotated files (Module B):
  - `@lib/src/movies/domain/entities/movie.dart`
  - `@lib/src/movie_sessions/domain/entities/movie_session.dart`
- Risky-adapter call sites (Module E), only the file that uses each package:
  - `localstorage` → `@lib/src/shopping_carts/data/repos/shopping_cart_local_repo_impl.dart`
  - `carousel_slider` → `@lib/src/movies/presentation/views/movie_view.dart`,
    `@lib/src/movie_sessions/presentation/views/movie_session_view.dart`
  - `flutter_web_auth_2` → `@lib/src/auth/data/services/flutter_web_auth_2_authenticator.dart`
  - `signalr_netcore` → `@lib/src/hub/data/signalr_event_hub.dart`
  - `flutter_secure_storage` → `@lib/src/auth/data/services/auth_service_impl.dart`,
    `@lib/injection_container.dart`
- The existing test (the reference for new tests):
  `@test/lib/shopping_cart/data/models/shopping_cart_model_test.dart`
- DI wiring: `@lib/injection_container.dart` (Module C/E breaking-change surface).

### DO NOT READ (now)
- Any slice not listed above; the 31 bloc/cubit files are migrated mechanically
  in Module C — read each only when its compile error points there, not up front.
- Generated files (`*.freezed.dart`, `*.g.dart`) — they are regenerated, never
  hand-edited.

### Codebase facts established before planning
- `pubspec.yaml` is **structurally inverted**: every runtime package
  (`dio`, `get_it`, `flutter_bloc`, `flutter_secure_storage`, `signalr_netcore`,
  `carousel_slider`, `localstorage`, `flutter_web_auth_2`, `jwt_decoder`,
  `logger`, `equatable`, `dartz`, `flutter_guid`, `flutter_dotenv`,
  `dio_cache_interceptor`) is under `dev_dependencies`. Only `intl`,
  `cupertino_icons`, `freezed_annotation`, `json_annotation` are correctly in
  `dependencies`.
- **Live `@freezed` classes: two, not three.**
  `lib/src/movie_sessions/data/models/movie_session_dto.dart` is **entirely
  commented out** — it has no active code. The PRD's "three freezed files" maps
  to two live files (`movie.dart`, `movie_session.dart`) plus this dead file.
  Decide in Module B whether to delete the dead file or leave it; do not waste a
  v3 migration on commented code.
- One existing test only: `shopping_cart_model_test.dart`. It must stay green.

---

## 3. Target version policy

Every retained library → latest major. Forced / high-surface bumps called out;
everything else "current release of its latest major".

| Package | From | To (target major) | Risk |
|---|---|---|---|
| `intl` | `0.18.1` | `0.20.2` (SDK-pinned) | **Forced** — resolution blocker |
| `flutter_bloc` | `^8.1.3` | `9.x` | **High** — 31 files |
| `freezed` / `freezed_annotation` | `^2.4.6` / `^2.4.1` | `3.x` | **High** — syntax change |
| `json_serializable` / `json_annotation` | `^6.7.1` / `^4.8.1` | latest major | codegen |
| `build_runner` | `^2.4.7` | latest | codegen |
| `get_it` | `^7.6.4` | `8.x` | DI wiring |
| `flutter_lints` | `^3.0.1` | `6.x` | lint ruleset |
| `localstorage` | `^4.0.1+4` | latest major | **Checkpoint** (web) |
| `carousel_slider` | `^4.2.1` | latest major | **Checkpoint** |
| `flutter_web_auth_2` | `^3.1.0` | latest major | **Checkpoint** (web) |
| `signalr_netcore` | `^1.3.6` | latest | **Checkpoint** |
| `dio` / `dio_cache_interceptor` | `^5.4.0` / `^3.5.0` | latest | networking |
| `flutter_secure_storage` | `^9.0.0` | latest major | web verify |
| `logger`, `jwt_decoder`, `flutter_dotenv`, `cupertino_icons`, `equatable`, `dartz`, `mocktail`, `flutter_guid` | various | latest major | low |

`environment`: `sdk: ^3.9.0`, `flutter: '>=3.41.0'`.

> Exact resolved versions are decided at migration time from `pub`, not pinned
> in this plan — the policy is "latest major that resolves", and the lockfile is
> the record of truth.

---

## 4. What to do — ordered by module (each = one commit)

Work on a dedicated branch off `development`. Commit per module so any single
step rolls back cleanly. Run `dart analyze` after every module.

### Pre-flight (before Module A)
- Create the branch off `development`.
- Write the **safety-net tests first** (Module G below) — they must exist and
  pass against the *pre-migration* state where the toolchain allows, so
  regressions surface immediately. (If the project cannot `pub get` at all yet,
  the safety net is authored now and first *run* right after Module A unblocks
  resolution; note this ordering explicitly in the commit message.)

### A. Toolchain & pubspec
1. Set `environment.sdk: ^3.9.0`, `environment.flutter: '>=3.41.0'`.
2. Move every runtime package from `dev_dependencies` → `dependencies`.
3. Leave **only** build/test tooling in `dev_dependencies`:
   `flutter_test`, `flutter_lints`, `freezed`, `json_serializable`,
   `build_runner`, `mocktail`. (`freezed_annotation`, `json_annotation`,
   `equatable`, `dartz` are runtime → `dependencies`.)
4. Set `intl: 0.20.2`. Bump the low-risk packages to latest major in the same
   edit.
5. `flutter pub get` must succeed. **This is User Story 1 — the unblock.**

### B. Codegen pipeline
1. Bump `freezed`/`freezed_annotation` → 3, `json_*` → latest, `build_runner` → latest.
2. Migrate the two live freezed classes to **freezed 3 syntax**: a class using
   `with _$X` must be declared `abstract class` (or `sealed`). So
   `class Movie with _$Movie` → `abstract class Movie with _$Movie`, and the
   same for `MovieSession`. Keep factories / `fromJson` / `Movie.empty()` as-is.
3. Decide the dead `movie_session_dto.dart`: delete it, or leave commented
   (do **not** un-comment and migrate it — out of scope). Recommend deletion to
   reduce confusion; flag for the user if unsure.
4. `dart run build_runner build --delete-conflicting-outputs` — clean.

### C. State management — `flutter_bloc` 8 → 9
1. With 9 resolved, run `dart analyze` and let the compiler enumerate the
   removed/renamed APIs across the ~31 bloc/cubit files + their widgets.
2. Fix each call site mechanically (no behavior change). Common 9.x deltas to
   watch: any reliance on removed `mapEventToState`, `BlocProvider` API tweaks,
   `Emitter`/`on<Event>` signatures. Read a bloc file **only** when analyze
   points at it.
3. No new `bloc_test` units here — this surface is verified via the screen smoke
   tests + analyze + manual web run (PRD "Testing Decisions").

### D. Localization — `intl` 0.20.2
1. Confirm gen-l10n / ARB still generate for **en/es/ru**.
2. Verify rendering of localized strings in the web run. No code rewrite
   expected beyond what `intl 0.20` API changes force.

### E. Risky adapters — pause-and-decide checkpoints
For **each** of `localstorage`, `carousel_slider`, `flutter_web_auth_2`,
`signalr_netcore` (and verify `flutter_secure_storage` on web):
1. Read the package changelog / migration notes for the major jump.
2. Look at the single call site (listed in §2 READ).
3. If the latest major is a **drop-in or small** change → push through, adapt
   the call site, commit.
4. If it requires a **disproportionate rewrite or breaks web** → **STOP**,
   surface the API diff and the two options to the user
   (push-through vs. pin one major below), and let them choose before
   committing. Do not silently rewrite an adapter.

### F. Lints — `flutter_lints` 6
1. Bump to 6.
2. Fix **only** blocking errors/warnings.
3. Leave new non-blocking `info` findings untouched — no mass `dart fix`,
   keep the diff reviewable.

### G. Safety-net tests (authored pre-migration, kept green throughout)
Default four-layer coverage is **explicitly overridden** by the PRD for this
slice. Only these three surfaces get tests:
- **Movies / movie-session screens** — widget smoke tests (guards
  `carousel_slider`). Mock the cubit per CLAUDE.md.
- **Shopping cart** — a widget/unit smoke around the cart **and** keep the
  existing `shopping_cart_model_test.dart` green (guards
  `localstorage`/freezed/json).
- **Auth flow** — a smoke test over the
  `flutter_web_auth_2` / `jwt_decoder` / secure-storage path.

These assert externally observable behavior (a screen shows its key widgets, a
cart round-trips an item, auth reaches its expected state) and must pass
**unchanged** before and after the migration.

---

## 5. Verification (definition of done)

Run, in order, after all modules:
- `dart format .` — apply (migration touches many files; format is expected).
- `dart analyze` — **no errors**.
- `dart run build_runner build --delete-conflicting-outputs` — clean.
- `flutter test` — safety-net + existing test green; the slice outside-in test green.
- `flutter build web` — succeeds.
- Manual web smoke run (`flutter run -d chrome`): movies/sessions carousels
  render, cart round-trips, auth flow reaches expected state, locales render.

---

## 6. Report (on completion)

- pubspec diff summary: what moved dep↔dev-dep, every version bump old→new.
- `pubspec.lock` resolved versions for the headline packages.
- Per-module commit list on the migration branch.
- Freezed: which files migrated to v3, decision taken on the dead `*_dto.dart`.
- flutter_bloc 9: list of files touched and the API deltas applied.
- Each Module-E checkpoint: decision taken (push-through / pinned-below) + why.
- Lints: count of blocking fixes; confirmation `info` findings left alone.
- Confirmation that **no** target-stack package was introduced and **no** folder
  structure changed.
- Verification results: analyze / build_runner / tests / build web / web smoke.

---

## 7. What NOT to do

- ❌ Introduce `slang`, `auto_route`, `retrofit`, `injectable`,
  `very_good_analysis`, or any new dependency.
- ❌ Restructure `lib/src/<feature>/{data,domain,presentation}` into vertical
  slices, or rename `*Repo` → port/adapter.
- ❌ Silently rewrite a Module-E adapter for a major bump — checkpoint first.
- ❌ Mass `dart fix` / style-only churn for new `flutter_lints` 6 `info` findings.
- ❌ Add `bloc_test` units for the 8→9 bump (verified via smoke + analyze + web).
- ❌ Un-comment and migrate the dead `movie_session_dto.dart`.
- ❌ Hand-edit generated `*.freezed.dart` / `*.g.dart`.
- ❌ Verify iOS/Android/desktop — **web is the only verification target** here.
- ❌ Pin exact versions in this plan; "latest major that resolves" + lockfile.
