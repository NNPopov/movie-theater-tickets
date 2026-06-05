# 0007 · free_form_seat_renderer — Outside-in test spec

> **Boundary note.** Like slice 0005, this slice's contract is *behavioural parity of the
> rendered hall under live updates* — not a Cubit→Dio round-trip. The `CustomPaint` renderer
> has **no per-seat widgets**, and per `prd.md` its pixels are **not** asserted. So the
> outside-in test drives a known `SeatLayout` + a live status list through the **real**
> `SeatLayoutCubit` and `SeatBloc` **into the real `SeatMapView`**, then aims `tester.tapAt`
> at each seat's **canvas centre** — computed with the **public, pure** `SeatLayoutTransform`
> at the initial (identity) zoom — and asserts the routed shopping-cart intent. Recolour is
> proven **render-agnostically**: after a live status event, the same tapped seat routes a
> *different* intent (status reached the renderer) while an untouched seat routes the same as
> before (isolation). The "outside" is the pumped `SeatMapView` + the `EventBus`, not a Cubit
> method. CLAUDE.md/PRD win over the generic Cubit-to-network template.

## Goal

Prove end-to-end, render-agnostic behaviour: the hall renders from explicit `SeatLayout`
geometry; a tap at a seat's canvas position routes the correct select/unselect intent; a
position with no live status (or no seat) routes nothing; a live status update changes
**only** the affected seat's interaction; and a layout-load failure shows an error instead of
crashing — all surviving any future swap of the painter.

## Entry point

A `flutter_test` widget pump of `SeatMapView(movieSession: ms)`, wrapped in
`MultiBlocProvider` with the **real** `SeatLayoutCubit` and `SeatBloc` (and a mocked
`ShoppingCartCubit`). Layout load is triggered by the screen's provider
(`SeatLayoutCubit()..load('hall-1')`); seat status is delivered by pushing
`SeatsUpdateEvent`s onto the real `EventBus` (simulating SignalR). Taps are delivered with
`tester.tapAt(<canvas centre of a seat>)`, the centre computed via
`SeatLayoutTransform.fit(boundsRect, surfaceSize).layoutToCanvas(x + 0.5, y + 0.5)`.

Fixtures:
- `movieSession`: id `'ms-1'`, cinemaHallId `'hall-1'`.
- Surface: `tester.binding.setSurfaceSize(const Size(400, 600))`, `SeatMapView` pumped as the
  full body so the painter's canvas size equals the surface size.
- Geometry: `synthesizeLegacyLayout(CinemaHallInfo('hall-1', '', grid2x2))` where
  `grid2x2 = [[(1,1),(1,2)],[(2,1),(2,2)]]` — a faithful legacy `SeatLayout`
  (`bounds = LTWH(-1, -2, 4, 5)`, `screen` top; seats at `(x,y)` = `(0,0),(1,0),(0,1),(1,1)`,
  `w = h = 1`, `rotation = 0`). This also exercises the F18 legacy-render path.
- Cart: `hashId = 'my-hash'`, status ≠ `initial`.

## Wired real (production code in the test)

- `SeatId` typedef, `buildSeatIndex`, and `SeatState.byId` (the pure status-index seam).
- `SeatLayoutTransform` and `resolveSeatAt` (the pure transform + hit resolver) and
  `colorForSeat` / `tapIntentFor` (the pure palette + intent classifier).
- `SeatLayoutCubit` (loads/holds the `SeatLayout`, system-under-test geometry path) and
  `SeatBloc` (status path), `SeatState`.
- `EventBus` (real instance — the in-app SignalR transport seam; the test pushes
  `SeatsUpdateEvent`s onto it).
- `synthesizeLegacyLayout` + `SeatLayout`/`SeatPlacement`/`LayoutBounds`/`Screen` (the 0006
  geometry contract), `SeatMapView` and `SeatMapPainter` (the renderer under test).

## Mocked (system boundaries only)

- **`SeatLayoutSource`** (the geometry port, mocktail): `getLayout('hall-1')` returns
  `Right(synthesizeLegacyLayout(CinemaHallInfo('hall-1', '', grid2x2)))`.
- **`GetSeatsByMovieSessionId`** (use-case): returns `Right(())` — a no-op success; seat
  status arrives via `SeatsUpdateEvent` on the `EventBus`, matching production.
- **`ShoppingCartCubit`** (mocktail): `state.hashId` = `'my-hash'`, `state.status` ≠
  `initial`; `seatSelect(...)` and `unSeatSelect(...)` stubbed to no-op and verified.

Initial status list pushed via `SeatsUpdateEvent`:
- `(1,1)` available, `blocked: false`, `hashId: ''` → **grey** (available) → tap routes select.
- `(1,2)` reserved, `blocked: true`, `hashId: 'other'` → **blue** (taken-by-others).
- `(2,1)` selected, `blocked: true`, `hashId: 'my-hash'` → **greenAccent** (mine-selected) →
  tap routes unselect.
- `(2,2)` — **absent** from the list → status index miss → **black12** (empty,
  non-interactive) even though geometry has the placement.

## Test scenarios

### Scenario 1: taps route the right intent; a live update changes only the affected seat

**Setup:**
- Mocked `SeatLayoutSource.getLayout('hall-1')` returns the synthesised 2×2 `SeatLayout`;
  `GetSeatsByMovieSessionId` returns `Right(())`.
- Mocked `ShoppingCartCubit` with `hashId: 'my-hash'`, status ≠ `initial`.
- Set the surface to `400×600`; pump `SeatMapView`; let the provider's `load('hall-1')`
  resolve and `initState` fire the `SeatEvent`; push the initial `SeatsUpdateEvent(seats)`
  onto the `EventBus`; settle.

**Act:**
- Tap at the canvas centre of `(1,1)` (grey / available).
- Tap at the canvas centre of `(2,1)` (greenAccent / mine-selected).
- Tap at the canvas centre of `(2,2)` (black12 / status-miss, geometry present).
- Tap at a canvas point in the top screen/margin band where **no** seat placement exists
  (a geometry gap, e.g. layout `(0.5, -1)`).
- Push a second `SeatsUpdateEvent` in which `(1,1)` becomes **mine-selected**
  (`blocked: true`, `hashId: 'my-hash'`, `status: selected`); settle.
- Tap the canvas centre of `(1,1)` again, then tap `(2,1)` again.

**Expect:**
- `ShoppingCartCubit.seatSelect` called once with `row: 1, seatNumber: 1,
  movieSessionId: 'ms-1'` (the first `(1,1)` tap, while available).
- `ShoppingCartCubit.unSeatSelect` called once with `row: 2, seatNumber: 1,
  movieSessionId: 'ms-1'` (the first `(2,1)` tap, mine-selected).
- The `(2,2)` tap and the gap tap route **nothing** (`seatSelect`/`unSeatSelect` not called
  for those coordinates / not called again at that point).
- After the second event, the second `(1,1)` tap routes **`unSeatSelect(row: 1, seatNumber: 1,
  …)`** — not `seatSelect` — proving the live status reached the renderer and `(1,1)`'s
  interaction changed; the second `(2,1)` tap still routes `unSeatSelect(row: 2, seatNumber:
  1, …)`, proving the untouched seat is unchanged (recolour isolation).
- `SeatLayoutSource.getLayout('hall-1')` called once; no exception
  (`tester.takeException()` is null) at any point.

### Scenario 2: a layout-load failure shows an error and routes nothing

**Setup:**
- Mocked `SeatLayoutSource.getLayout('hall-1')` returns `Left(ServerFailure(...))`.
- Mocked `ShoppingCartCubit` as in Scenario 1.
- Pump `SeatMapView`; let `load('hall-1')` resolve; settle.

**Act:**
- Attempt a tap anywhere in the hall area.

**Expect:**
- `SeatLayoutCubit` ends in the `error` state; the renderer (`CustomPaint`/`SeatMapView`
  painted hall) is **not** shown — an error indicator is shown instead.
- `ShoppingCartCubit.seatSelect` and `unSeatSelect` are **never** called.
- No exception is thrown (`tester.takeException()` is null); the screen does not crash or hang.

## Out of scope for this test

- The pure-module unit cases — transform round-trips at several zoom/canvas states, hit-test
  hits/misses/gaps/rotation/variable-size, the exhaustive `colorForSeat`/`tapIntentFor`
  palette — covered by their own unit tests written after green.
- `SeatLayoutCubit` `[loading, loaded]` / `[loading, error]` emission coverage in isolation
  (covered by the cubit unit test, driven directly with `mocktail`).
- Pinch-zoom / drag-pan gesture mechanics and `InteractiveViewer` internals (delegated to the
  SDK; the transform's inverse is unit-tested).
- Non-functional acceptance — open-without-freeze at ~1000 seats, no-jank, crisp numbers when
  zoomed, pixel-equivalent legacy render, tablet/desktop extra-space fit — verified by manual
  profiling/device walkthrough per `validation.md`, not by this test.
- Route navigation and the outer seats-screen scroll/chrome behaviour.
