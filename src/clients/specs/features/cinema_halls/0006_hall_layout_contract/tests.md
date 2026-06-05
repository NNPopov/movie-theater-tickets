# 0006 · hall_layout_contract — Outside-in test spec

> **Note on boundary.** This slice has **no Cubit, no UI, and no use-case** (the documented
> carve-out in `plan.md` §6). Its public surface is the `SeatLayoutSource` port. The
> outside-in test therefore enters at `SeatLayoutSource.getLayout` and wires the whole slice
> (adapter → synthesizer → freezed value types) real, mocking only `CinemaHallRepo` — the
> legacy seam that stands in for the eventual `GET …/halls/{id}/layout` network boundary
> (the bootstrap adapter delegates to it rather than calling Dio directly).

## Goal

Prove that obtaining a hall's geometry through `SeatLayoutSource` synthesizes a `SeatLayout`
that reproduces the legacy grid 1:1, and that an unexpected failure is logged and returned as
a `Left(ServerFailure)` rather than thrown.

## Entry point

`source.getLayout('hall-red')` where `source` is a real `BootstrapSeatLayoutSource`.

## Wired real (production code in the test)

- `BootstrapSeatLayoutSource` (the slice's throwaway adapter — system under test)
- `SeatLayoutSource` (the slice's port — bound to the adapter)
- `synthesizeLegacyLayout` (the pure legacy-geometry synthesizer)
- `SeatLayout`, `SeatPlacement`, `Zone`, `Screen`, `LayoutBounds`, `LayoutPoint` (the freezed contract value types)

## Mocked (system boundaries only)

- **`CinemaHallRepo`** (`mocktail`): `getCinemaHallInfoById(id)` returns a `Right(CinemaHallInfo)` fixture for the happy path, or is stubbed to throw / return `Left` for the failure paths.
- **`AppLogger`** (`mocktail`, injected into the adapter): captures `e(...)` calls so logging can be verified; no `bloc_test` is used.

## Test scenarios

### Scenario 1: synthesizing a legacy hall reproduces the grid 1:1

**Setup:**
- `CinemaHallRepo.getCinemaHallInfoById('hall-red')` returns `Right(CinemaHallInfo('hall-red', 'Red', grid))` where `grid` is a rectangular `28×22` fixture (28 rows, 22 seats per row, `row`/`seatNumber` set per cell).

**Act:**
- `final result = await source.getLayout('hall-red');`

**Expect:**
- `result` is `Right(SeatLayout)`.
- `layout.hallId == 'hall-red'`.
- `layout.seats.length == 616` (28 × 22).
- Every seat: `x == columnIndex`, `y == rowIndex`, `w == 1`, `h == 1`, `rotation == 0`, `zoneId == null`, and `seatId == (row, number)`.
- `layout.zones` is empty.
- `layout.screen` is `side: top`, `start: (0, -1)`, `end: (22, -1)`.
- `layout.bounds` is `(x: -1, y: -2, width: 24, height: 31)` (i.e. `-m, -2m, C+2m, R+3m` with `m = kLegacyLayoutMargin`, `C = 22`, `R = 28`).
- Mocks verified: `CinemaHallRepo.getCinemaHallInfoById('hall-red')` called once; `AppLogger.e` never called.

### Scenario 2: an unexpected repo failure is logged and returned as Left

**Setup:**
- `CinemaHallRepo.getCinemaHallInfoById('hall-red')` is stubbed to throw an unexpected `Exception('boom')`.

**Act:**
- `final result = await source.getLayout('hall-red');`

**Expect:**
- `result` is `Left(ServerFailure)` (not thrown).
- Side effects observed: `AppLogger.e(...)` called once with the error and stack trace.
- Mocks verified: `CinemaHallRepo.getCinemaHallInfoById('hall-red')` called once; no `SeatLayout` produced.

## Out of scope for this test

- Widget rendering — there is no UI in this slice (no widget tests apply).
- Cubit/Bloc state sequences — there is no bloc in this slice.
- Route navigation — no routes are added.
- The pass-through of a *known* `Left(Failure)` from the repo, the empty/ragged-hall synthesis edges, the Black/White seeded shapes, the model invariants (defaults, zone-vs-polygon independence, equality), and the JSON round-trip — all covered by the synthesizer, model, and adapter **unit** tests written from `plan.md` §6, not by this outside-in scenario.
