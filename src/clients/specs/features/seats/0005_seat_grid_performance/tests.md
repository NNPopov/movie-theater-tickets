# 0005 · seat_grid_performance — Outside-in test spec

> **Boundary note.** Unlike a typical CRUD slice (Cubit→Dio), this slice's contract is
> *behavioural parity of the rendered grid under live updates*. Per `prd.md`, the
> outside-in test therefore drives a hall geometry + a live status list through the
> **real** `SeatBloc` and `CinemaHallInfoBloc` **into the real grid widget**, then pushes a
> status-update event (the SignalR path) and asserts the affected seat recolours and that a
> tap routes the correct cart intent. The "outside" here is the pumped grid + the EventBus,
> not a Cubit method. CLAUDE.md/PRD win over the generic Cubit-to-network template.

## Goal

Prove end-to-end behavioural parity: a hall renders each seat with the correct colour, a
live status update recolours **exactly** the affected cell (others unchanged), tapping an
interactive seat routes the right shopping-cart intent, and a non-interactive (empty) cell
routes nothing.

## Entry point

A `flutter_test` widget pump of `SeatsMovieSessionWidget(movieSession: ms)`, wrapped in
`MultiBlocProvider` with the **real** `SeatBloc` and `CinemaHallInfoBloc` (and a mocked
`ShoppingCartCubit`). Geometry load is triggered by the widget's own `initState`
(`CinemaHallInfoEvent`); seat status is delivered by pushing `SeatsUpdateEvent`s onto the
real `EventBus` (simulating SignalR). Taps are delivered with `tester.tap` on a seat cell.

Fixtures:
- `movieSession`: id `'ms-1'`, cinemaHallId `'hall-1'`.
- Geometry (2×2): rows `[[(1,1),(1,2)],[(2,1),(2,2)]]` as `CinemaSeat(row, seatNumber)`.
- Cart: `hashId = 'my-hash'`, status ≠ `initial`.

## Wired real (production code in the test)

- `SeatId` typedef and `buildSeatIndex` (the pure status-index seam).
- `SeatState` (with derived `byId`) and `SeatBloc` (the system-under-test data path).
- `CinemaHallInfoBloc` + `CinemaHallInfo` / `CinemaSeat` (geometry path).
- `EventBus` (real instance — the in-app SignalR transport seam; the test pushes
  `SeatsUpdateEvent`s onto it).
- `SeatsMovieSessionWidget` (the grid) and `SeatWidget` (the rendered cells).

## Mocked (system boundaries only)

- **`GetCinemaHallInfo`** (use-case): returns `Right(CinemaHallInfo('hall-1', '', geometry))`
  for `'hall-1'`.
- **`GetSeatsByMovieSessionId`** (use-case): returns `Right(())` — a no-op success; seat
  status arrives via `SeatsUpdateEvent` on the `EventBus`, matching production.
- **`ShoppingCartCubit`** (mocktail): `state.hashId` = `'my-hash'`, `state.status` ≠
  `initial`; `seatSelect(...)` and `unSeatSelect(...)` stubbed to no-op and verified.

Initial status list pushed via `SeatsUpdateEvent`:
- `(1,1)` available, `blocked: false`, `hashId: ''` → **grey** (available).
- `(1,2)` reserved, `blocked: true`, `hashId: 'other'` → **blue** (taken-by-others).
- `(2,1)` selected, `blocked: true`, `hashId: 'my-hash'` → **greenAccent** (mine-selected).
- `(2,2)` — **absent** from the list → index miss → **black12** (empty, non-interactive).

## Test scenarios

### Scenario 1: live update recolours the affected seat; taps route the right intent

**Setup:**
- `GetCinemaHallInfo` returns the 2×2 geometry; `GetSeatsByMovieSessionId` returns `Right(())`.
- Mocked `ShoppingCartCubit` with `hashId: 'my-hash'`, status ≠ `initial`.
- Pump the grid; let `initState` fire `CinemaHallInfoEvent`; push the initial
  `SeatsUpdateEvent(seats)` onto the `EventBus`; `pumpAndSettle`.

**Act:**
- Tap the `(1,1)` cell (grey/available).
- Tap the `(2,1)` cell (greenAccent/mine-selected).
- Push a second `SeatsUpdateEvent` in which `(2,1)` becomes available
  (`blocked: false`, `hashId: ''`) — the seat was released — and `pumpAndSettle`.

**Expect:**
- Initial render: `(1,1)` grey, `(1,2)` blue, `(2,1)` greenAccent, `(2,2)` black12.
- `ShoppingCartCubit.seatSelect` called once with `row: 1, seatNumber: 1,
  movieSessionId: 'ms-1'`.
- `ShoppingCartCubit.unSeatSelect` called once with `row: 2, seatNumber: 1,
  movieSessionId: 'ms-1'`.
- After the second event: `(2,1)` recolours to grey; `(1,1)`, `(1,2)`, `(2,2)` colours
  unchanged (single-cell recolour, isolation preserved).
- `GetCinemaHallInfo` called once.

### Scenario 2: a non-interactive (empty/index-miss) seat dispatches nothing

**Setup:**
- Same geometry, use-case mocks, and cart mock as Scenario 1.
- Pump the grid; fire geometry; push the same initial `SeatsUpdateEvent(seats)` (no status
  for `(2,2)`); `pumpAndSettle`.

**Act:**
- Tap the `(2,2)` cell (black12 / empty — no live status).

**Expect:**
- `(2,2)` renders black12 and is non-interactive.
- `ShoppingCartCubit.seatSelect` is **never** called.
- `ShoppingCartCubit.unSeatSelect` is **never** called.
- No grid recolour occurs as a result of the tap.

## Out of scope for this test

- Per-colour exhaustive widget assertions and tap-disabled-on-`initial`-cart cases (covered
  by the mocked-bloc widget tests, written after green).
- The pure `buildSeatIndex` mapping/miss/duplicate cases (covered by the unit test).
- `SeatBloc` transition coverage in isolation (covered by `bloc_test`).
- Non-functional acceptance — open-without-freeze, no-jank, pixel-identical render — which
  is verified by manual profiling per `validation.md`, not by this test.
- Route navigation and the outer screen's scroll behaviour.
