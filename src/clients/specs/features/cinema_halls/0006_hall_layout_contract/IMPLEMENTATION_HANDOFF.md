# 0006 hall_layout_contract — Implementation Handoff

> Status checkpoint written before a planned machine restart (to clear hung
> Dart processes). **All source is on disk and committed.** Nothing is lost by
> restarting. After reboot, the slice needs only a clean `build_runner` run plus
> verification — see "Resume after restart".

## TL;DR state

- **Implementation: COMPLETE.** All slice source files are written and committed.
- **Acceptance gate: PASSED.** The outside-in test
  `test/features/cinema_halls/0006_hall_layout_contract/hall_layout_contract_outside_in_test.dart`
  passed **both scenarios** green on the very first `flutter test` run.
- **One unit test was failing**, then fixed in source (not yet re-verified):
  `seat_layout_test.dart` → "fromJson/toJson round-trips a representative
  SeatLayout". Root cause: nested `freezed` objects serialize as objects, not
  maps. Fix applied: added `build.yaml` with `explicit_to_json: true`.
- **Why we stopped:** the Dart toolchain got into a stuck state on THIS project
  (every `dart`-on-project command hangs at startup, ~47 MB / CPU 0) after a
  `build_runner` run was killed mid-`--delete-conflicting-outputs`. The SDK
  itself is fine (`dart --version` works instantly). A reboot clears the
  zombie processes holding the project's `.dart_tool` lock.

## Files created (all committed; `*.g.dart`/`*.freezed.dart` are gitignored)

Source under `lib/src/cinema_halls/`:
- `domain/layout/layout_point.dart` — `@freezed LayoutPoint {x, y}`
- `domain/layout/layout_bounds.dart` — `@freezed LayoutBounds {x, y, width, height}`
- `domain/layout/layout_screen.dart` — `enum ScreenSide {top,bottom,left,right}` + `@freezed Screen {side, start, end}`
- `domain/layout/zone.dart` — `@freezed Zone {id, label, colour, polygon=[]}`
- `domain/layout/seat_placement.dart` — `@freezed SeatPlacement {row, number, x, y, w=1, h=1, rotation=0, zoneId?}` with `const SeatPlacement._();` + `SeatId get seatId => (row, number);`
- `domain/layout/seat_layout.dart` — `@freezed SeatLayout {hallId, bounds, screen, seats, zones=[]}`
- `domain/ports/seat_layout_source.dart` — `abstract SeatLayoutSource { ResultFuture<SeatLayout> getLayout(String hallId); }`
- `data/layout/legacy_seat_layout_synthesizer.dart` — pure `SeatLayout synthesizeLegacyLayout(CinemaHallInfo)` + `const double kLegacyLayoutMargin = 1.0;`
- `data/layout/bootstrap_seat_layout_source.dart` — throwaway adapter (marked with deletion trigger), catch-all + injectable `AppLogger`.

Tests under `test/features/cinema_halls/0006_hall_layout_contract/`:
- `domain/legacy_seat_layout_synthesizer_test.dart` — parity tests incl. seeded 616/378/180.
- `domain/seat_layout_test.dart` — model invariants incl. the JSON round-trip (the one that needed `explicit_to_json`).
- `data/bootstrap_seat_layout_source_test.dart` — success / pass-through Left / unexpected-exception-with-logging.

Modified:
- `lib/injection_container.dart` — one `SeatLayoutSource` registration in `_initCinemaHall()` (+ 2 imports).
- `build.yaml` — **NEW** (project root). `json_serializable: explicit_to_json: true`. Required so nested freezed objects round-trip through `toJson`/`fromJson`. Flat models are unaffected.

`SeatId` is **imported** from `lib/src/seats/domain/entities/seat_id.dart`, not redeclared.

## Resume after restart (clean, single pass — do NOT parallelize or kill mid-run)

Run each via the **PowerShell** tool (Dart is NOT on the Bash tool's PATH — Bash
gives exit 127), one at a time, letting each finish:

1. `dart run build_runner build --delete-conflicting-outputs`
   - Regenerates all the gitignored `*.g.dart`/`*.freezed.dart` that were
     deleted, clearing the ~163 "missing part" analyzer errors.
   - **Let it run to completion.** It can take ~30–60 s on a cold cache (the
     `build.yaml` change invalidates the json_serializable cache once).
2. `flutter test test/features/cinema_halls/0006_hall_layout_contract/`
   - Expect ALL green now (the outside-in already passed; the JSON round-trip is
     fixed by `build.yaml`).
3. `dart analyze lib/src/cinema_halls test/features/cinema_halls/0006_hall_layout_contract lib/injection_container.dart`
   - My new code is clean. Pre-existing legacy warnings in
     `cinema_hall_info_dto.dart`, `movie_cubit.dart`, `cinema_hall_cubit.dart`
     are NOT from this slice (they appear only because the dir scope is broad).
   - One expected note: `unused_import: seat_layout.dart` in the **outside-in
     test file** — that file must NOT be edited (acceptance gate). The import
     reads as unused because the test uses type inference. Leave it.
4. `dart format .` (no diff expected on new files) and the arch check
   `bash scripts/check_arch.sh`.

## Hard-won gotchas (do not repeat)

- **Never** run a native exe with `2>&1 | Select-Object`/pipe in PowerShell 5.1:
  each stderr progress line becomes an ErrorRecord and chokes the pipeline (a
  10 s build took 6 min). Run plainly; stderr is captured anyway.
- **Never kill `build_runner` mid-run.** With `--delete-conflicting-outputs` it
  deletes all generated outputs first, then regenerates; killing in between
  leaves the project uncompilable (the ~163 errors).
- `TaskStop` kills the shell wrapper but NOT the child `dart` process — orphans
  pile up and hold the project's `.dart_tool` lock, making every later
  `dart`/`pub get`/`build_runner` hang at startup. If it happens again, kill ALL
  `dart`/`dartaotruntime` processes (or reboot) before retrying.
- Dart is on PATH only in the **PowerShell** tool, not the **Bash** tool.

## Per spec (plan.md §6): this slice has NO cubit/widget/use-case layer.
Only synthesizer + model + adapter unit tests, plus the outside-in gate.
