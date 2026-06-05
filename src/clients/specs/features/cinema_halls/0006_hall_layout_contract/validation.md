# 0006 · hall_layout_contract — Validation Checklist

> This slice ships **no user-visible change** and adds no UI. The only end-user-observable
> guarantee is that the existing seat grid behaves exactly as before (M1–M3). The remaining
> scenarios verify the new contract via runnable checks, since there is no screen to click.

## Manual testing

| # | Step | Expected result |
|---|---|---|
| M1 | Launch the app and open the seat-map screen for any hall | The seat grid renders pixel-identical to the pre-slice build (same layout, spacing, colours). |
| M2 | Select, deselect, and reserve a seat on the seat-map screen | Booking, reservation, shopping-cart, and live SignalR status behave exactly as before. |
| M3 | Open seat maps for the Red, Black, and White halls | All three render exactly as before; no missing or extra seats. |
| M4 | Resolve `getIt.get<SeatLayoutSource>()` after `init()` (e.g. in a scratch test) | A `BootstrapSeatLayoutSource` instance is returned; no DI registration error. |
| M5 | Call `getLayout(id)` for a seeded hall id and inspect the result | Returns `Right(SeatLayout)` with seat count 616 (Red) / 378 (Black) / 180 (White). |
| M6 | Inspect a returned `SeatPlacement` for a known seat | `x = columnIndex`, `y = rowIndex`, `w = h = 1`, `rotation = 0`, `zoneId = null`, and `seatId == (row, number)`. |
| M7 | Inspect the returned `SeatLayout.bounds` and `screen` | `bounds = (-1, -2, C+2, R+3)`; `screen.side = top`, `start = (0,-1)`, `end = (C,-1)`. |
| M8 | Call `getLayout(id)` for an empty hall fixture (`cinemaSeat: []`) | Returns `Right(SeatLayout)` with `seats: []`, no throw; bounds/screen computed from `C=0, R=0`. |
| M9 | Stub `getCinemaHallInfoById` to return `Left(ServerFailure)` and call `getLayout` | The same `Left(ServerFailure)` is returned unchanged; no layout synthesized. |
| M10 | Stub `getCinemaHallInfoById` to throw an unexpected exception and call `getLayout` | Returns `Left(ServerFailure)`; the exception is logged via `AppLogger.e`, never rethrown. |
| M11 | Round-trip a representative `SeatLayout` through `toJson`/`fromJson` | The deserialized layout equals the original; the JSON contains no `seatId` field. |

## Code review

- [ ] `SeatLayout`, `SeatPlacement`, `Zone`, `Screen`, `LayoutBounds`, `LayoutPoint` are `@freezed` value types with `fromJson`/`toJson`, mirroring `movie.dart` (N1)
- [ ] No file under `lib/src/cinema_halls/domain/layout/` imports `package:flutter/*` or `package:dio/*` — grep the directory (N2)
- [ ] `colour` is a hex `String` and coordinates/`rotation` are `double` — no `Color`, `Offset`, or `Rect` (N2)
- [ ] Contract carries no money/price field; only `SeatId`/`zoneId` are exposed as seams (N3)
- [ ] `SeatId` is imported from `lib/src/seats/domain/entities/seat_id.dart`, not redeclared in `cinema_halls` (N4)
- [ ] `seatId` is a derived getter on `SeatPlacement` (via `const SeatPlacement._();`) and is **not** a JSON field (N5)
- [ ] `w`/`h` default to `1.0` and `rotation` defaults to `0.0` via `@Default(...)` (N6)
- [ ] A `SeatLayout` `fromJson`/`toJson` round-trip test exists and passes (N7)
- [ ] `synthesizeLegacyLayout` is a pure, Flutter-free top-level function with no I/O, no `getIt`, no logging (N8)
- [ ] `kLegacyLayoutMargin = 1.0` is a named top-level constant referenced by synthesizer and tests, not inline literals (N9)
- [ ] `BootstrapSeatLayoutSource.getLayout` has a catch-all `catch (e, st)` returning `Left(ServerFailure)` with `logger.e(...)` (N10)
- [ ] The adapter accepts an injectable `AppLogger?` defaulting to `getLogger(...)` (N11)
- [ ] The adapter file has a top doc comment marking it throwaway scaffolding and naming the deletion trigger (backend serving real `SeatLayout`) (N12)
- [ ] A known `Left(Failure)` from `getCinemaHallInfoById` is passed through unchanged (no synthesis) (F18)
- [ ] No new entry added to `pubspec.yaml` — diff shows no dependency change (N13)
- [ ] No legacy migration: no `*Repo`/`*Bloc` renames, no `slang`/`retrofit`/`injectable` introduced into `cinema_halls`/`seats` (N14)
- [ ] Synthesizer parity tests assert the seeded counts 616 / 378 / 180 and the bounds/screen formulas verbatim (N15)
- [ ] Model invariant tests cover `seatId` derivation, defaults, zone-vs-polygon independence, value equality, and JSON round-trip (N16)
- [ ] Adapter tests (`mocktail`) cover success delegation, pass-through `Left`, and unexpected-exception-with-verified-logging (N17)
- [ ] No `application/` (cubit) or `presentation/` (widget) test directory was created for this slice (N18)
- [ ] The only non-new file modified is `lib/injection_container.dart` (one `SeatLayoutSource` registration line) (N19)
- [ ] `dart run build_runner build --delete-conflicting-outputs` — no errors
- [ ] `dart run slang` — no errors
- [ ] `dart analyze` — no warnings
- [ ] All tests green
