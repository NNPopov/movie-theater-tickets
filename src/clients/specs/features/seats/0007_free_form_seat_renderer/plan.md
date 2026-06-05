# Feature Spec / Plan — `0007_free_form_seat_renderer`

> Phase-2 ("P5") of ADR 0005. Replaces the index-driven seat grid with a
> **coordinate-driven renderer** (`CustomPaint` + `InteractiveViewer`) that draws the
> whole hall from explicit `SeatLayout` geometry (slice 0006) inside a pan-and-zoom
> viewport. Seat identity stays `(row, seatNumber)`; status and cart feeds are reused
> unchanged. Source of truth for intent: `prd.md` in this folder.
>
> This is a **legacy slice** (`flutter_bloc` + `get_it` without `injectable`, per-route
> bloc providers). Per `CLAUDE.md` "Modifying existing legacy code", match the
> surrounding legacy idiom of the `seats`/`cinema_halls` features. Do **not** migrate to
> ports/adapters-everywhere, `slang`, `retrofit`, or `injectable` — those are separate
> tracked slices. (The one already-target seam — the `SeatLayoutSource` port from 0006 —
> is consumed as-is.)

---

## 1. Header

Draw the seat-selection hall from explicit geometry inside a **pinch-zoom / drag-pan
viewport**, so a customer on a phone can zoom in until a seat is big enough to tap
accurately and still hit the right seat. A single `CustomPaint` painter draws every seat
where the `SeatLayout` says it sits (layout space → canvas space), the screen indicator,
and crisp seat numbers; `InteractiveViewer` provides zoom and pan. Tapping a seat — even
while zoomed in — selects or deselects it through the **unchanged** shopping-cart contract.
Seat colours update live over the existing SignalR/`SeatBloc` status feed.

**UX result:** the hall opens fitted to the screen, can be pinched/dragged, and taps
resolve to the correct seat at any zoom. Legacy (grid) halls look exactly as before,
because their geometry is synthesised into the same `SeatLayout` shape (slice 0006). The
customer's selection behaviour, the reservation countdown, and the shopping cart are
**unchanged** — only how the hall is drawn and how a tap is resolved to a seat changes.

This slice **adds** one legacy state holder (a `SeatLayoutCubit` that loads/holds the
`SeatLayout`) and three **pure, widget-free** render modules (a layout↔canvas transform, a
hit-test resolver, a status→colour palette). It **reuses** `SeatBloc` (status, incl. the
`byId` index from 0005) and `ShoppingCartCubit` (cart) untouched, and **swaps** the grid
body in the seats screen for the new renderer.

---

## 2. Context — what to read, what not to read

### READ
- `@CLAUDE.md` — fully (legacy-modification rules win here).
- `@specs/features/seats/0007_free_form_seat_renderer/prd.md` — the intent.
- The geometry contract from slice 0006 (read-only, do **not** change):
  - `@lib/src/cinema_halls/domain/layout/seat_layout.dart` — `SeatLayout {hallId, bounds, screen, seats, zones}`.
  - `@lib/src/cinema_halls/domain/layout/seat_placement.dart` — `SeatPlacement {row, number, x, y, w, h, rotation, zoneId}` + `SeatId get seatId`.
  - `@lib/src/cinema_halls/domain/layout/layout_bounds.dart` — `LayoutBounds {x, y, width, height}` (the fit-to-viewport canvas).
  - `@lib/src/cinema_halls/domain/layout/layout_screen.dart` — `enum ScreenSide`, `Screen {side, start, end}`.
  - `@lib/src/cinema_halls/domain/layout/layout_point.dart` — `LayoutPoint {x, y}`.
  - `@lib/src/cinema_halls/domain/layout/zone.dart` — `Zone` (read only to confirm it is **ignored** here).
  - `@lib/src/cinema_halls/domain/ports/seat_layout_source.dart` — `SeatLayoutSource.getLayout(hallId)` (the loader's only dependency).
- The live-status + identity seam from slice 0005 (read-only):
  - `@lib/src/seats/domain/entities/seat_id.dart` — `typedef SeatId = (int row, int seatNumber);`
  - `@lib/src/seats/domain/entities/seat.dart` — `Seat` (status side) + `SeatStatus` enum.
  - `@lib/src/seats/domain/seat_index.dart` — `buildSeatIndex` (already used by `SeatState.byId`).
  - `@lib/src/seats/presentation/cubit/seat_cubit.dart` + `seat_state.dart` — `SeatBloc`, `SeatState.byId`, the EventBus/SignalR path. **Not** changed by this slice.
- Files that will be **modified / added** in `seats`:
  - `@lib/src/seats/presentation/views/seats_view.dart` — swap the grid for the renderer; provide `SeatLayoutCubit`.
  - `@lib/src/seats/presentation/widgets/seats_movie_session_widget.dart` — the **current** grid (read to preserve the colour/tap contract; it is replaced by the new renderer widget, not edited in place).
- The tap contract (read-only, do **not** change):
  - `@lib/src/shopping_carts/presentation/cubit/shopping_cart_cubit.dart` — `seatSelect({row, seatNumber, movieSessionId})`, `unSeatSelect({...})`, `ShoppingCartState.hashId`, `ShoppingCartStateStatus.initial`.
- DI + prior art:
  - `@lib/injection_container.dart` — the existing `SeatLayoutSource` registration (line ~173); add the loader's wiring near the seats registrations.
  - `@lib/core/utils/typedefs.dart` — `ResultFuture<T> = Future<Either<Failure, T>>`.
  - `@specs/features/seats/0005_seat_grid_performance/plan.md` + its test files — the **nearest analogue** for the pure-seam + mocked-bloc widget-test + outside-in patterns (opt on, do **not** copy).
- `@.claude/skills/bloc/SKILL.md` and `agent_docs/testing.md` (read at test-writing time).

### DO NOT READ
- Other `cinema_halls` concerns off the layout path: `data/` DTOs, `movie_cubit.dart`
  geometry internals beyond confirming it is dropped from the seats screen.
- Other features beyond the tap contract: `shopping_carts/**` internals, `movie_sessions/**`,
  `movies/**`, `hub/**` beyond `SeatsUpdateEvent`.
- Generated files: `*.gr.dart`, `*.g.dart`, `*.freezed.dart`, l10n `app_localizations*.dart`.
- ADR phases that are **out of scope**: zone rendering, price overlay, backend geometry,
  admin editor, DI migration (see §8).

---

## 3. "API" — there is none new (internal data contract instead)

**No backend, no REST/gRPC, no new adapter or use-case.** Geometry and live status already
arrive through existing paths; this slice changes how geometry is *consumed and drawn* and
how a tap is *resolved* to a seat.

- **Geometry (existence + position):** `SeatLayoutSource.getLayout(hallId)`
  → `ResultFuture<SeatLayout>`. Today served by `BootstrapSeatLayoutSource` (synthesises a
  `SeatLayout` from the legacy `List<List<CinemaSeat>>` via `synthesizeLegacyLayout`); later
  by the backend. The renderer does **not** know or care which. `SeatLayout.seats` is the
  single source of truth for which seats exist and where (`x, y` in seat-pitch layout units,
  `w, h` size, `rotation`); `SeatLayout.bounds` is the authoring canvas to fit; `screen`
  is the drawn screen segment; `zones` is **ignored** here.
- **Live status:** `SeatBloc` → `SeatState.seats` (`List<Seat>`) + `SeatState.byId`
  (`Map<SeatId, Seat>`, the O(1) index from slice 0005), mutated in real time by the SignalR
  path (`EventBus` → `SeatsUpdateEvent` → `emit(copyWith(seats: ...))`, which rebuilds
  `byId`). Each `Seat` carries `blocked`, `hashId`, `seatStatus`.
- **Identity joining geometry ↔ status:** `SeatId = (row, seatNumber)`. The painter looks up
  each `SeatPlacement.seatId` in `byId` (O(1)); a miss is an empty/non-interactive position.
- **Tap intent (unchanged):** a resolved `SeatId` routes to `ShoppingCartCubit.seatSelect` /
  `unSeatSelect`, guarded by `ShoppingCartState.status != initial`, coloured by `Seat` vs
  `ShoppingCartState.hashId`.

**Status → colour → action mapping (must be reproduced exactly from the legacy grid):**

| Condition (on the resolved `Seat` for a `SeatPlacement`) | Colour | Tap action |
|---|---|---|
| `blocked && hashId == cart.hashId && status == selected` | `Colors.greenAccent` (mine-selected) | unselect |
| `blocked && hashId == cart.hashId && status != selected` | `Colors.green` (mine-blocked) | unselect |
| `blocked && hashId != cart.hashId` | `Colors.blue` (taken-by-others) | unselect *(preserve legacy behaviour as-is)* |
| otherwise (a `Seat` exists, not blocked) | `Colors.grey` (available) | select |
| no `Seat` for that `SeatId` (index miss) | `Colors.black12` (empty) | **none** (non-interactive) |

This is the **same** five-way mapping the legacy `buildSeat`/`emptySeat` produces; it is
extracted verbatim into the pure `colorForSeat` so it can be unit-tested exhaustively and so
the painter and the tap resolver agree on which seats are interactive.

---

## 4. Target structure (files added / modified)

```
lib/src/seats/
├── domain/
│   └── render/
│       ├── seat_layout_transform.dart   # NEW: pure fit-to-viewport transform (layout↔canvas);
│       │                                 #      dart:ui only, NO package:flutter import.
│       └── seat_hit_tester.dart          # NEW: pure SeatId? resolveSeatAt(Offset layoutPoint,
│                                         #      List<SeatPlacement>) incl. w/h + rotation.
└── presentation/
    ├── cubit/
    │   ├── seat_layout_cubit.dart        # NEW: legacy Cubit; loads/holds SeatLayout via the port.
    │   └── seat_layout_state.dart        # NEW: status enum + SeatLayout? + errorMessage.
    ├── render/
    │   ├── seat_palette.dart             # NEW: pure Color colorForSeat(Seat?, String cartHashId)
    │   │                                 #      — the 5-way palette above (material Colors).
    │   └── seat_map_painter.dart         # NEW: CustomPainter — draws screen + seats + numbers;
    │                                     #      shouldRepaint on status/hashId/transform change.
    └── widgets/
        └── seat_map_view.dart            # NEW: the renderer widget (InteractiveViewer + tap
                                          #      GestureDetector + CustomPaint); replaces the grid.

MODIFIED:
- lib/src/seats/presentation/views/seats_view.dart
    • add BlocProvider<SeatLayoutCubit>(create: (_) => SeatLayoutCubit(getIt.get())
        ..load(movieSession.cinemaHallId));
    • render SeatMapView instead of SeatsMovieSessionWidget;
    • drop the now-unused CinemaHallInfoBloc provider from THIS screen (geometry now comes
      from the layout port; the bootstrap source still reads the same hall info internally).
- lib/injection_container.dart
    • register SeatLayoutCubit as a factory (or leave un-registered and build inline in the
      provider via getIt.get<SeatLayoutSource>()) — follow whichever the seats screen already
      does for SeatBloc/CinemaHallInfoBloc (they are built inline with getIt.get()).

UNCHANGED (reused as-is):
- seats_movie_session_widget.dart / seat_widget.dart   (legacy grid — left in place, no
  longer mounted by the seats screen; do not delete in this slice).
- seat_cubit.dart / seat_state.dart / seat_index.dart  (status feed + byId index).
- All of cinema_halls/domain/layout/** and the SeatLayoutSource port + bootstrap adapter.
```

Notes:
- The pure render math lives under `domain/render/` to mirror slice 0005's
  `domain/seat_index.dart` pure seam. `seat_layout_transform.dart` and `seat_hit_tester.dart`
  import **only** `dart:ui` (`Offset`/`Size`/`Rect`) and `dart:math` — **no `package:flutter`**,
  so they stay unit-testable without pumping a widget (the hard rule the PRD's maintainer
  stories ask for). `colorForSeat` needs `material` `Colors`, so it sits under
  `presentation/render/` instead of `domain/`.
- **No** new files under `data/` or `domain/ports/` and **no `pubspec.yaml` change**:
  `CustomPaint`, `InteractiveViewer`, `Matrix4`/`MatrixUtils`, `TextPainter` are all in the
  Flutter SDK; the geometry port and adapter already exist (slice 0006).

---

## 5. What to do — step by step

### Step 1 — Pure fit-to-viewport transform (`domain/render/seat_layout_transform.dart`)
A small immutable value object mapping **layout space ↔ canvas-base space**, built once per
`(bounds, canvasSize)`. Uniform scale (preserve aspect ratio) + centering, so the whole
`bounds` fits and is centred; a bigger canvas (tablet/desktop) yields a bigger scale, which
is what "use the extra space" means (F17 / N16-17).

```dart
import 'dart:ui';

/// Fit-to-viewport mapping between layout space (seat-pitch units, from
/// SeatLayout.bounds) and canvas-base space (logical px of the CustomPaint, BEFORE
/// the InteractiveViewer zoom/pan matrix). Pure: dart:ui only, no package:flutter.
class SeatLayoutTransform {
  const SeatLayoutTransform({required this.scale, required this.offset});
  final double scale;     // canvas px per layout unit
  final Offset offset;    // canvas px added after scaling (centering + bounds origin)

  /// Fits [bounds] into [canvasSize], preserving aspect ratio and centering.
  factory SeatLayoutTransform.fit(Rect bounds, Size canvasSize) { ... }

  Offset layoutToCanvas(double x, double y) =>
      Offset(x * scale + offset.dx, y * scale + offset.dy);

  Offset canvasToLayout(Offset c) =>
      Offset((c.dx - offset.dx) / scale, (c.dy - offset.dy) / scale);
}
```
- `bounds` comes from `SeatLayout.bounds` as `Rect.fromLTWH(b.x, b.y, b.width, b.height)`.
- Round-trip: `canvasToLayout(layoutToCanvas(p)) == p` (within epsilon) for any scale/canvas.
- This object is **public** so tests can construct it with the same inputs the widget uses
  and compute a seat's canvas centre to aim taps (see §6) — no production-only test seam.

### Step 2 — Pure hit-test resolver (`domain/render/seat_hit_tester.dart`)
A top-level pure function mapping a point **in layout space** to a `SeatId?`, honouring each
seat's rect (`x, y, w, h`) and `rotation`. A point in a gap between seats resolves to `null`.

```dart
import 'dart:math';
import 'dart:ui';
import 'package:.../cinema_halls/domain/layout/seat_placement.dart';
import 'package:.../seats/domain/entities/seat_id.dart';

/// Returns the SeatId whose placement rect contains [layoutPoint], or null.
/// Each seat occupies [x, x+w] × [y, y+h] in layout space; rotation rotates the
/// hit rect about the seat centre. Last match wins on overlap (defensive).
SeatId? resolveSeatAt(Offset layoutPoint, List<SeatPlacement> seats) { ... }
```
- For `rotation == 0` (the legacy default and the common case) this is a plain
  point-in-rect test. For non-zero rotation, rotate `layoutPoint` by `-rotation` about the
  seat centre `(x + w/2, y + h/2)` before the rect test.
- Linear scan is fine at the scale target (≤ ~1000 seats); do **not** prematurely add a
  spatial index. Document the scan as the deliberate simple choice.

### Step 3 — Pure status→colour palette (`presentation/render/seat_palette.dart`)
Extract the legacy `buildSeat`/`emptySeat` colour decision into one pure function, plus a
companion "is this seat interactive / which intent" helper the renderer and the painter share.

```dart
import 'package:flutter/material.dart';
import 'package:.../seats/domain/entities/seat.dart';

/// The exact 5-way palette from the legacy grid (see plan §3 table).
/// [seat] == null ⇒ empty (index miss) ⇒ Colors.black12 (non-interactive).
Color colorForSeat(Seat? seat, String cartHashId) { ... }
```
- Reproduce the table **byte-for-byte**: greenAccent / green / blue / grey / black12.
- Add a tiny intent classifier used by the tap path so colour and action never diverge,
  e.g. `enum SeatTapIntent { select, unselect, none }` +
  `SeatTapIntent tapIntentFor(Seat? seat, String cartHashId)` mirroring the table's last
  column (mine/other-blocked → unselect, available → select, empty → none).

### Step 4 — Layout loader (`presentation/cubit/seat_layout_cubit.dart` + `_state.dart`)
A render-agnostic **legacy Cubit** that fetches and holds the `SeatLayout`. Depends **only**
on `SeatLayoutSource` (the geometry port) — the seam that keeps the future backend cutover
free (N22).

```dart
// seat_layout_state.dart
enum SeatLayoutStatus { initial, loading, loaded, error }

@immutable
class SeatLayoutState extends Equatable {
  const SeatLayoutState({this.status = SeatLayoutStatus.initial, this.layout, this.errorMessage});
  final SeatLayoutStatus status;
  final SeatLayout? layout;
  final String? errorMessage;
  SeatLayoutState copyWith({...});
  @override List<Object?> get props => [status, layout, errorMessage];
}

// seat_layout_cubit.dart
class SeatLayoutCubit extends Cubit<SeatLayoutState> {
  SeatLayoutCubit(this._source) : super(const SeatLayoutState());
  final SeatLayoutSource _source;

  Future<void> load(String hallId) async {
    emit(state.copyWith(status: SeatLayoutStatus.loading));
    final result = await _source.getLayout(hallId);
    result.fold(
      (failure) => emit(state.copyWith(status: SeatLayoutStatus.error, errorMessage: failure.errorMessage)),
      (layout) => emit(state.copyWith(status: SeatLayoutStatus.loaded, layout: layout)),
    );
  }
}
```
- Use a Cubit (not Bloc): a single `load` is naturally a Cubit, and other legacy loaders on
  this screen (`MovieCubit`, `CinemaHallCubit`) are Cubits too.
- `errorMessage` from `Failure.errorMessage` (the legacy getter used by `SeatBloc`/`CinemaHallInfoBloc`).

### Step 5 — The painter (`presentation/render/seat_map_painter.dart`)
A `CustomPainter` that draws the **whole hall** from `SeatLayout` + the live status index.

```dart
class SeatMapPainter extends CustomPainter {
  SeatMapPainter({
    required this.layout,
    required this.transform,     // SeatLayoutTransform.fit(bounds, size)
    required this.byId,          // SeatState.byId — live status, O(1)
    required this.cartHashId,
  });
  ...
  @override
  void paint(Canvas canvas, Size size) {
    // 1) draw the screen indicator (layout.screen.start/end → transform → a thick line/bar).
    // 2) for each SeatPlacement p:
    //      final rect = transform applied to (p.x, p.y, p.w, p.h);  (rotation via canvas.save/rotate)
    //      final fill = colorForSeat(byId[p.seatId], cartHashId);
    //      canvas.drawRect(rect, Paint()..color = fill);
    //      draw p.number centred via a TextPainter (crisp at any zoom — redrawn, not scaled).
    // (Optional polish: per-row label near each row's min-x seat — F14.)
  }

  @override
  bool shouldRepaint(SeatMapPainter old) =>
      !identical(byId, old.byId) ||            // new status emit ⇒ new byId instance (0005)
      cartHashId != old.cartHashId ||          // my-cart recolour
      transform != old.transform ||            // resize / fit change
      !identical(layout, old.layout);          // (layout is loaded once; cheap guard)
}
```
- **Status stays live + per-seat-isolated — the documented ADR §6 deviation (from the PRD):**
  a painter has no per-seat widgets, so it reads the whole `byId` map directly. The O(1)
  per-seat lookup and the single map-level subscription (ADR §4) are preserved; "only the
  affected seat changes" is satisfied **visually/at the data level** (only that seat's fill
  differs between frames) rather than via per-widget rebuild isolation. A whole-canvas
  redraw on a status emit is cheap and culled at the scale target — record this deviation in
  `.claude/decisions/` per the project-memory skill.
- Seat numbers use `TextPainter` re-laid-out each paint, so they stay **crisp** when zoomed
  (the core reason `CustomPaint` beat `Stack`+`Positioned` in ADR 0005 / the PRD).
- **Ignore `layout.zones`** (out of scope). Draw status only — no price layer.

### Step 6 — The renderer widget (`presentation/widgets/seat_map_view.dart`)
A `StatefulWidget` that owns the viewport and the tap→seat resolution.

1. `initState`: kick off the **status** load — `context.read<SeatBloc>().add(SeatEvent(movieSessionId: widget.movieSession.id))` (the layout load is started by the provider's `..load(hallId)` in Step 7). Hold a `TransformationController` for the `InteractiveViewer`.
2. `build`: `BlocBuilder<SeatLayoutCubit, SeatLayoutState>`:
   - `loading`/`initial` → `LoadingView()` (reuse the existing core loading widget).
   - `error` → a simple error text (reuse the legacy error presentation idiom).
   - `loaded` → a `LayoutBuilder` giving the canvas `Size`; build
     `transform = SeatLayoutTransform.fit(boundsRect, canvasSize)` and render:
     ```
     GestureDetector(                       // OUTER: captures discrete taps in viewport space
       behavior: HitTestBehavior.opaque,
       onTapUp: (d) => _handleTap(d.localPosition, transform),
       child: InteractiveViewer(            // owns pan (drag) + zoom (pinch); taps fall through
         transformationController: _controller,
         minScale: ..., maxScale: ..., boundaryMargin: ...,
         child: BlocBuilder<SeatBloc, SeatState>(            // live status
           builder: (_, seatState) => BlocSelector<ShoppingCartCubit, ShoppingCartState, String>(
             selector: (c) => c.hashId,                       // my-cart colour key (as the legacy grid)
             builder: (_, hashId) => CustomPaint(
               size: canvasSize,
               painter: SeatMapPainter(
                 layout: layout, transform: transform,
                 byId: seatState.byId, cartHashId: hashId,
               ),
             ),
           ),
         ),
       ),
     )
     ```
3. `_handleTap(Offset viewportPoint, SeatLayoutTransform transform)` — the **single** tap→seat
   path, all geometry through the pure modules (no seat math in widgets):
   ```
   // viewport → canvas-base: invert the live InteractiveViewer matrix.
   final canvasPoint = MatrixUtils.transformPoint(
       Matrix4.inverted(_controller.value), viewportPoint);
   final layoutPoint = transform.canvasToLayout(canvasPoint);     // canvas-base → layout
   final seatId = resolveSeatAt(layoutPoint, layout.seats);       // layout → SeatId?
   if (seatId == null) return;                                    // gap / outside → nothing
   final seat = context.read<SeatBloc>().state.byId[seatId];      // live status for that seat
   switch (tapIntentFor(seat, cart.hashId)) {                     // same classifier as the colour
     case SeatTapIntent.select:   _select(seatId);                // → cart.seatSelect(row, number, msId)
     case SeatTapIntent.unselect: _unselect(seatId);              // → cart.unSeatSelect(...)
     case SeatTapIntent.none:     break;                          // sold/empty/other-blocked-as-legacy
   }
   ```
   - `_select`/`_unselect` reproduce the legacy guard exactly: only dispatch when
     `ShoppingCartCubit.state.status != ShoppingCartStateStatus.initial`, calling
     `seatSelect`/`unSeatSelect` with `(row, seatNumber, movieSessionId)` from the `SeatId`.
   - **Capturing taps OUTSIDE the `InteractiveViewer`** lets the renderer own the full
     composed inverse (viewer matrix ∘ fit) per the PRD's seam, while `InteractiveViewer`
     still owns pan/zoom (drag/pinch win the gesture arena; discrete taps fall through to the
     outer detector). At the initial fit the viewer matrix is identity, so a tap maps straight
     through the fit transform.

### Step 7 — Integrate into the seats screen (`presentation/views/seats_view.dart`)
- Add `BlocProvider<SeatLayoutCubit>(create: (_) => SeatLayoutCubit(getIt.get())..load(widget.movieSession.cinemaHallId))` to the screen's `MultiBlocProvider` (alongside the existing `SeatBloc`).
- Render `SeatMapView(movieSession: widget.movieSession)` where `SeatsMovieSessionWidget` was.
- **Remove** the `CinemaHallInfoBloc` provider from this screen: the renderer no longer needs
  the `List<List<CinemaSeat>>` geometry (it consumes `SeatLayout`). The bootstrap source still
  reads the same hall info internally via `CinemaHallRepo`, so nothing about the data origin
  changes. (Confirm no other widget on the seats screen reads `CinemaHallInfoBloc` — the
  `AuditoriumDetailView`/movie panels use `CinemaHallCubit`/`MovieCubit`, not this bloc.)
- The surrounding chrome (movie-session info panel, `ShoppingCartWidget`, countdown) is
  **untouched** (F18).

### Step 8 — Localization / DI / verification
- No new UI strings beyond what already exists (`screen`, `row` keys reused if row labels are
  drawn; seat numbers are numeric). **No `slang`/l10n/ARB change.** If an error message is
  shown, reuse the existing localized error idiom — do not hardcode.
- No `pubspec.yaml` change; no `build_runner` needed (no freezed/injectable/retrofit/slang
  touched — the layout models from 0006 already have their generated parts).
- Run, in order (PowerShell tool; Dart is not on the Bash PATH):
  - `dart format .` — no diff.
  - `dart analyze` — no new warnings.
  - `flutter test test/features/seats/0007_free_form_seat_renderer/` (+ the outside-in file).

---

## 6. Tests

Per `prd.md` "Testing Decisions": **test external behaviour, not painter internals**;
CustomPaint pixels are **not** asserted directly — drawing correctness is guaranteed through
the **pure modules** plus **behavioural taps**. Adapter / use-case layers are **N/A new** (the
`SeatLayoutSource` port + `BootstrapSeatLayoutSource` adapter are already covered by slice
0006); the new **loader Cubit** takes the "application" coverage. Per project convention the
client has **no `bloc_test`** — drive the real `SeatLayoutCubit` directly with `mocktail`
mocks of the port (mirror `0005_seat_grid_performance/application/seat_bloc_test.dart`).

`test/features/seats/0007_free_form_seat_renderer/`:

**a) `domain/render/seat_layout_transform_test.dart` — pure transform (the bulk of value).**
- `fit` centres and uniformly scales `bounds` into the canvas for several canvas sizes
  (square, wide, tall) — the whole bounds fits, aspect ratio preserved.
- **Round-trip**: `canvasToLayout(layoutToCanvas(p)) ≈ p` across several scales/offsets
  (simulating different zoom levels and canvas sizes).
- a bigger canvas yields a bigger `scale` (the "use the extra space" property).

**b) `domain/render/seat_hit_tester_test.dart` — pure hit resolver.**
- a point inside a seat's rect resolves to that `SeatId`; the seat's centre resolves to it.
- a point in the **gap** between seats resolves to `null`.
- a point outside `bounds` resolves to `null`.
- **rotated** seat: a point inside the rotated rect hits; the same point outside (that would
  hit the un-rotated rect) misses — proving rotation is honoured.
- **variable-size** seat (`w`/`h` ≠ 1) is hit across its full extent.

**c) `presentation/render/seat_palette_test.dart` — pure palette + intent.**
- every status/`hashId` combination → expected colour: mine-selected→greenAccent,
  mine-blocked→green, taken-by-others→blue, available→grey, `null`→black12.
- `tapIntentFor` agrees with the table: mine/other-blocked→unselect, available→select,
  `null`/empty→none (colour and action never diverge).

**d) `application/seat_layout_cubit_test.dart` — loader Cubit (mocktail mock of the port).**
- `load` success → emits `[loading, loaded(layout)]`; the held `layout` is the port's value.
- `load` failure → emits `[loading, error]` with the failure's message; `layout` stays null.
- (drive the cubit directly and assert `emit.stream`/successive `state`s — no `bloc_test`.)

**e) Outside-in acceptance gate — `free_form_seat_renderer_outside_in_test.dart`** (in the
test tree, generated by `/slice-test-red`). Drives the **real** slice end to end with only
the boundaries mocked — mirror the 0005 harness: real `SeatBloc` fed by a mocked
`GetSeatsByMovieSessionId` + real `EventBus`; a **mocked `SeatLayoutSource`** returning a
known legacy-shaped `SeatLayout` (or the real `synthesizeLegacyLayout` over a seeded grid);
a `MockCubit` `ShoppingCartCubit` to verify routed intents. Render the real `SeatMapView`.
Assert **render-agnostic behaviour** (survives any future render-core swap), aiming taps by
computing a seat's canvas centre with the **public** `SeatLayoutTransform.fit(...)` for the
test's surface size at the initial (identity) zoom:
- tapping the canvas point of a **free** seat → `cart.seatSelect(row, number, movieSessionId)`.
- tapping the canvas point of **my selected** seat → `cart.unSeatSelect(...)`.
- tapping an **empty / gap** point (a `SeatId` with no `Seat`, or between seats) → **nothing**.
- a live `SeatsUpdateEvent` that flips a free seat to mine-selected changes that seat's
  **routed intent** (the next tap routes `unSeatSelect` instead of `seatSelect`), while an
  untouched neighbour still routes as before — proving the live status reached the renderer
  and recoloured **only** the affected seat, **without asserting pixels**.
- a legacy (grid) hall renders all its seats at the expected canvas positions (a tap at each
  synthesised seat centre resolves to the matching `SeatId`).

**Not test-gated here (manual, recorded in `validation.md`):** the non-functional acceptance —
a ~1000-seat hall opens without freeze; pinch-zoom/drag feel natural; seat numbers stay crisp
when zoomed; legacy halls look pixel-equivalent — is verified by **manual profiling / device
walkthrough** on a target phone + a tablet/desktop. These are perceptual/performance
properties a widget test cannot assert directly.

Prior art to mirror: slice 0005's `seat_grid_performance_outside_in_test.dart` (one file,
several behavioural cases over the real blocs + mocked boundaries) and its pure-seam unit
test; slice 0006's synthesizer/model tests (seeded halls Red 616 / Black 378 / White 180,
the `(row, number)` identity). Use `mocktail` per project conventions; **no `bloc_test`**.

---

## 7. Report (what the implementing agent must hand back)

- Files **created** (`seat_layout_transform.dart`, `seat_hit_tester.dart`, `seat_palette.dart`,
  `seat_map_painter.dart`, `seat_map_view.dart`, `seat_layout_cubit.dart`,
  `seat_layout_state.dart`) and **modified** (`seats_view.dart`, `injection_container.dart`).
- Confirmation that the **status feed** (`SeatBloc`/`SeatState`/`seat_index`), the **cart
  contract** (`ShoppingCartCubit`), the **geometry contract** (`SeatLayout`, `SeatPlacement`,
  the `SeatLayoutSource` port + bootstrap adapter), and the legacy grid widgets were **not
  changed**; **no other slice/feature**, **no `core/`** beyond reused widgets, **no
  `pubspec.yaml`**, **no `build_runner`/`slang`** change.
- Confirmation the colour mapping and tap→cart wiring are reproduced **exactly** (greenAccent
  / green / blue / grey / black12; select / unselect / none) via the shared `colorForSeat` /
  `tapIntentFor`.
- Confirmation taps resolve through the pure transform + resolver (no seat geometry math in
  widgets), and `zones`/price are ignored.
- The documented ADR §6 deviation (painter reads whole `byId`; whole-canvas repaint) recorded
  in `.claude/decisions/`.
- Test run: counts across the four unit files + the outside-in gate, all green;
  `dart format` / `dart analyze` clean.

---

## 8. What NOT to do

- ❌ Do **not** assert `CustomPaint` pixels or `SeatMapPainter` private paint calls — assert
  the pure modules + behavioural taps (the render-agnostic contract).
- ❌ Do **not** put seat geometry / hit-testing math inside widgets — all of it goes through
  `SeatLayoutTransform` + `resolveSeatAt` so a future render-core swap needs no test change.
- ❌ Do **not** change seat identity away from `(row, seatNumber)` / `SeatId`.
- ❌ Do **not** change `SeatBloc`, `SeatState`/`byId`, `seat_index`, the EventBus/SignalR path,
  the `ShoppingCartCubit` contract, the `SeatLayout`/`SeatPlacement` models, the
  `SeatLayoutSource` port, or `BootstrapSeatLayoutSource`.
- ❌ Do **not** render `zones` (zone polygons/tints) or any **price** overlay — both are later,
  separate slices.
- ❌ Do **not** add per-seat `Semantics` over the painter (screen-reader support is explicitly
  out of scope, decided 2026-06-04; the accepted risk is recorded).
- ❌ Do **not** introduce the backend layout endpoint or delete the bootstrap synthesiser —
  this slice consumes whatever the port returns (ADR P3/P4 are later steps).
- ❌ Do **not** migrate `seats`/`cinema_halls` to the target stack (`injectable`, `slang`,
  `retrofit`, ports-everywhere, renaming `*Bloc`/`*Repo`) — they stay legacy; DI migration is
  its own tracked slice.
- ❌ Do **not** add a dependency to `pubspec.yaml` (everything needed is in the Flutter SDK +
  the 0006 contract).
- ❌ Do **not** delete or edit the legacy `seats_movie_session_widget.dart` / `seat_widget.dart`
  in this slice — leave them in place (un-mounted) to keep the change atomic and reviewable.
- ❌ Do **not** use `bloc_test` (the client doesn't depend on it) — drive cubits directly with
  `mocktail`.
```