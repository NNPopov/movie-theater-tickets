# Feature Spec / Plan — `0005_seat_grid_performance`

> Phase-1 ("quick win") of ADR 0005. A **scoped performance fix on legacy code**, not a
> migration and not a new vertical slice. Seat identity stays `(row, seatNumber)`; no
> domain/contract change, no new dependency, no network adapter or use-case added.
> Source of truth for intent: `prd.md` in this folder.

---

## 1. Header

Fix the seat-selection grid so it **opens immediately** and **updates without jank**, even
for a large hall (~1000 seats; largest real seeded hall is Red 28×22 = 616). Two legacy
root causes are removed:

1. **O(N²) status lookup** — each seat cell runs `seats.firstWhere(...)` (O(N)) inside its
   own `BlocSelector`, re-run for every cell on every emit (SignalR update, countdown tick).
   Replace with an **O(1)** `byId[(row, seatNumber)]` map lookup against a status index built
   **once per state instance**.
2. **Heavyweight, eagerly-built widgets** — Material `TextButton` per seat + nested
   `ListView.builder`s in `shrinkWrap`/`NeverScrollableScrollPhysics` (non-lazy anyway).
   Replace with a lightweight `GestureDetector` + `DecoratedBox` seat and plain
   `Column`/`Row` layout.

**UX result:** screen looks and behaves identically — same colours, layout, row labels,
screen bar; tap-to-select / tap-to-release unchanged; sold and empty seats stay
non-interactive. **One intentional behaviour change:** a hall wider than the viewport now
**scrolls sideways** instead of overflowing off-screen.

This is a **legacy slice** (`*Repo`, `intl`/gen-l10n, `Bloc`, manual `get_it`). Per
`CLAUDE.md` "Modifying existing legacy code", match surrounding legacy style and make the
minimal change. Do **not** re-architect to ports/adapters, `slang`, `retrofit`, or
`injectable`.

---

## 2. Context — what to read, what not to read

### READ
- `@CLAUDE.md` — fully (legacy-modification rules win here).
- `@specs/features/seats/0005_seat_grid_performance/prd.md` — the intent.
- Files that will be **modified**:
  - `@lib/src/seats/presentation/widgets/seats_movie_session_widget.dart` — the grid; O(N²)
    selector, nested lists, colour mapping, tap wiring all live here.
  - `@lib/src/seats/presentation/widgets/seat_widget.dart` — the per-seat `TextButton`.
  - `@lib/src/seats/presentation/cubit/seat_state.dart` — gains the derived `byId` field.
- Files read for **context only** (do not change):
  - `@lib/src/seats/presentation/cubit/seat_cubit.dart` — `SeatBloc`; how `seats` is emitted
    (EventBus `SeatsUpdateEvent` → `copyWith(seats:...)`). The bloc logic is **not** changed.
  - `@lib/src/seats/domain/entities/seat.dart` — `Seat` (status side) + `SeatStatus` enum.
  - `@lib/src/cinema_halls/domain/entity/cinema_seat.dart` — `CinemaSeat` (geometry side).
  - `@lib/src/cinema_halls/domain/entity/cinema_hall_info.dart` — `cinemaSeat: List<List<CinemaSeat>>`.
  - `@lib/src/cinema_halls/presentation/cubit/movie_cubit.dart` — `CinemaHallInfoBloc` (geometry).
- `_shared`-equivalent signatures (read only to confirm the tap contract, do not change):
  `ShoppingCartCubit.seatSelect({row, seatNumber, movieSessionId})`,
  `unSeatSelect({...})`, `ShoppingCartState.hashId`, `ShoppingCartStateStatus.initial`.
- `@.claude/skills/bloc/SKILL.md` and `agent_docs/testing.md` (read at test-writing time).

### DO NOT READ
- Other seat-feature concerns not on the grid path: `seats/data/**`,
  `seats/domain/usecases/**`, `seats/domain/repos/**` (no network/use-case change here).
- Other features beyond the tap contract: `shopping_carts/**` internals,
  `movie_sessions/**`, `movies/**`.
- Generated files: `*.gr.dart`, `*.g.dart`, l10n `app_localizations*.dart`.

---

## 3. "API" — there is none (internal data contract instead)

**No backend, no REST/gRPC, no new adapter or use-case.** Geometry and live status already
arrive through existing paths; this slice only changes how they are *indexed and rendered*.
The relevant internal contract:

- **Geometry (existence):** `CinemaHallInfoBloc` → `state.movie.cinemaSeat`
  (`List<List<CinemaSeat>>`), each `CinemaSeat` = `(row, seatNumber)`. Drives the grid shape.
- **Live status:** `SeatBloc` → `state.seats` (`List<Seat>`), mutated in real time by the
  SignalR path (`EventBus` → `SeatsUpdateEvent` → `emit(copyWith(seats: ...))`). Each `Seat`
  carries `blocked`, `hashId`, `seatStatus` (`blocked|available|selected|reserved|sold`).
- **Identity joining the two:** `(row, seatNumber)`. Today the join is a per-cell
  `firstWhere`; this slice makes it an O(1) map keyed by a new `SeatId` value type.
- **Tap intent:** unchanged — routes to `ShoppingCartCubit.seatSelect` / `unSeatSelect`,
  guarded by `ShoppingCartState.status != initial`, coloured by `Seat` vs `ShoppingCartState.hashId`.

**Status → colour → action mapping (must be preserved byte-for-byte):**

| Condition (on the resolved `Seat`) | Colour | Tap action |
|---|---|---|
| `blocked && hashId == cart.hashId && status == selected` | `Colors.greenAccent` (mine-selected) | unselect |
| `blocked && hashId == cart.hashId && status != selected` | `Colors.green` (mine-blocked) | unselect |
| `blocked && hashId != cart.hashId` | `Colors.blue` (taken-by-others) | unselect *(preserve legacy behaviour as-is)* |
| otherwise | `Colors.grey` (available) | select |
| no `Seat` for that `(row, seatNumber)` (index miss) | `Colors.black12` (empty) | none (non-interactive) |

The index-miss → empty-seat path replaces the current `firstWhere` `catch (_) → null`; it
must behave identically (a miss yields the empty seat, no exception).

---

## 4. Target structure (files touched)

```
lib/src/seats/
├── domain/
│   ├── entities/
│   │   ├── seat.dart                    # (unchanged) Seat + SeatStatus
│   │   └── seat_id.dart                 # NEW: typedef SeatId = (int row, int seatNumber);
│   └── seat_index.dart                  # NEW: pure seam — Map<SeatId, Seat> buildSeatIndex(List<Seat>)
└── presentation/
    ├── cubit/
    │   └── seat_state.dart              # MODIFIED: derived `byId` field (out of equality)
    └── widgets/
        ├── seats_movie_session_widget.dart  # MODIFIED: O(1) lookup, plain layout, h-scroll
        └── seat_widget.dart                  # MODIFIED: lightweight GestureDetector seat
```

- `seat_id.dart` is the single shared seat-id type the PRD requires; Phase 2/P5 inherits it
  unchanged. Placed in `seats/domain/entities` as the canonical home (legacy layout — not a
  `_shared/` move).
- `seat_index.dart` is the **deep, widget-free seam** — a top-level pure function, the
  analogue of slice 0004's extracted overlay-mode resolver. No Flutter imports.
- **No** new files under `data/`, `domain/ports/`, `domain/usecases/`. **No** `pubspec.yaml`
  change.

---

## 5. What to do — step by step

### Step 1 — `SeatId` value type (`domain/entities/seat_id.dart`)
Create the Dart record typedef:

```dart
/// Identity of a seat within a hall: its (row, seat-number) coordinate.
/// Shared by the live-status index (P1) and explicit geometry (P2/P5).
typedef SeatId = (int row, int seatNumber);
```

Structural equality + hashing come for free — no `Equatable`, no boilerplate. (Captured in
`CONTEXT.md`.)

### Step 2 — Pure status-index seam (`domain/seat_index.dart`)
A top-level pure function — **no `package:flutter`** import — building the O(1) index once:

```dart
import 'entities/seat.dart';
import 'entities/seat_id.dart';

/// Builds an O(1) lookup of live seat status keyed by (row, seatNumber).
/// A miss (no Seat for a coordinate) is the caller's "empty seat" signal,
/// matching the legacy firstWhere-catch path. On duplicate ids, last wins
/// (defensive; backend does not emit duplicates).
Map<SeatId, Seat> buildSeatIndex(List<Seat> seats) {
  final index = <SeatId, Seat>{};
  for (final seat in seats) {
    index[(seat.row, seat.seatNumber)] = seat;
  }
  return index;
}
```

### Step 3 — Derived `byId` on `SeatState` (`presentation/cubit/seat_state.dart`)
Add a derived `Map<SeatId, Seat> byId`, built **once per state instance** from `seats`, and
keep it **out of equality** (it is a pure function of `seats`; `props` stays `[seats, status]`).

- In the constructor body / initializer, set `byId = buildSeatIndex(seats)`.
- `copyWith` continues to pass `seats`/`status`; the new instance recomputes `byId` once.
- **Do not** add `byId` to `props`. Equality is unchanged, so bloc still skips no-op emits.
- `SeatBloc` (`seat_cubit.dart`) is **not** modified — it already emits via `copyWith`, which
  now rebuilds the index for free.

### Step 4 — Grid uses O(1) lookup + plain layout (`seats_movie_session_widget.dart`)
1. **Per-cell selector → O(1):** replace the `firstWhere(...)`/`catch` block in the inner
   `BlocSelector<SeatBloc, SeatState, Seat?>` with:
   ```dart
   selector: (state) {
     if (state.status != SeatStateStatus.loaded || state.seats.isEmpty) return null;
     return state.byId[(seatPlace.row, seatPlace.seatNumber)]; // null on miss → emptySeat
   }
   ```
   **Keep one `BlocSelector` per seat** — per-seat rebuild isolation is preserved (a single
   status change repaints one cell, not the hall). This is Design B; do **not** collapse to a
   single whole-grid subscription (rejected Design A).
2. **Outer rows list → `Column`:** replace the outer `ListView.builder` (rows) with
   `Column(mainAxisSize: MainAxisSize.min, children: [...])`.
3. **Inner seats list → `Row`:** replace the inner horizontal `ListView.builder` (seats per
   row) with `Row(mainAxisSize: MainAxisSize.min, children: [...])`. Same eager build, no
   sliver overhead, no visual change.
4. **Wide-hall horizontal scroll (the one intentional behaviour change):** wrap the hall
   body in a `SingleChildScrollView(scrollDirection: Axis.horizontal, ...)` so a hall wider
   than the viewport scrolls instead of overflowing. **Implementation note:** the horizontal
   scroll view sits inside the existing centred `Row` from `seats_view.dart`; give it a
   bounded width (constrain from the viewport, e.g. `MediaQuery`/`LayoutBuilder` /
   `ConstrainedBox`) to avoid an "unbounded width" error. Vertical scrolling already exists
   one level up in `seats_view.dart` — do not duplicate it.
5. Leave colour mapping (`buildSeat`), `emptySeat`, and the tap handlers
   (`onSelectSeatPress` / `onSeatUnselectPress`) **functionally unchanged** — only the seat
   widget they return changes (Step 5). The `ShoppingCartCubit.hashId` selector stays.

### Step 5 — Lightweight seat widget (`seat_widget.dart`)
Replace the Material `TextButton` with a non-Material cell, **keeping the public constructor
shape** (`text`, `foregroundColor`, `backgroundColor`, `onPressed`) so callers do not churn:

```dart
SizedBox(
  height: 19, width: 19,
  child: GestureDetector(
    behavior: HitTestBehavior.opaque, // whole cell tappable, like the old button
    onTap: onPressed,                 // null ⇒ non-interactive (sold/empty)
    child: DecoratedBox(
      decoration: BoxDecoration(color: backgroundColor),
      child: Center(
        child: Text(text,
          style: TextStyle(color: foregroundColor, fontSize: AppStyles.defaultFontSize)),
      ),
    ),
  ),
)
```

**Accepted trade-off (per PRD):** the tap ripple and desktop hover highlight are dropped.
Resting render is pixel-identical (same 19×19, same colours, same centred label); the
load-bearing feedback (grey→green recolour driven by bloc/cart) is unchanged.

### Step 6 — Integration / verification
No `core/`, routing, DI, or `_shared` change. No `pubspec.yaml` change. No `slang`/l10n
change (existing `AppLocalizations` keys `screen` / `row` are reused as-is).

Run, in order:
- `dart format .` — no diff.
- `dart analyze` — no new warnings.
- `flutter test test/features/seats/0005_seat_grid_performance/` (and the outside-in file).
- No `build_runner` needed (no freezed/injectable/retrofit/slang touched; `SeatId` is a hand
  -written typedef).

---

## 6. Tests

Per `prd.md` "Testing Decisions". Adapter / use-case layers are **N/A** (no network adapter
or use-case is added or changed) — same documented exception as slice 0004.

`test/features/seats/0005_seat_grid_performance/`:

**a) `domain/seat_index_test.dart` — deep-module unit test of the pure seam.**
- maps each `Seat` by `(row, seatNumber)`; lookup hit returns the right `Seat`.
- lookup **miss** returns `null` (the "empty seat" signal — matches legacy catch path).
- empty input list → empty map.
- (defensive) duplicate ids → last wins, no throw.

**b) `application/seat_bloc_test.dart` — `bloc_test` on `SeatBloc` (mocktail mocks).**
- on load and on a `SeatsUpdateEvent` status change, the emitted state's `byId` resolves each
  seat correctly across the transition. Locks the derived field; the bloc's own logic is
  unchanged so no new transitions are introduced.

**c) `presentation/seat_grid_widget_test.dart` — widget tests, mocked `SeatBloc` +
`CinemaHallInfoBloc` + `ShoppingCartCubit` (mocktail).** One case per observable colour:
- mine-selected → `greenAccent`; mine-blocked → `green`; taken-by-others → `blue`;
  available → `grey`; empty (index miss) → `black12`.
- tapping a **free** seat calls `ShoppingCartCubit.seatSelect` with the right `(row,
  seatNumber, movieSessionId)`.
- tapping **my selected** seat calls `unSeatSelect`.
- tapping a **sold/empty** seat dispatches **nothing**.
These enforce visual + interaction parity (the headline "no behaviour change").

**d) Outside-in acceptance gate — `seat_grid_performance_outside_in_test.dart`** (in the
test tree, generated by `/slice-test-red`). Drives a hall geometry + an initial status list
through the **real** `SeatBloc` and `CinemaHallInfoBloc` into the grid, then pushes a
status-update event (simulating the SignalR path) and asserts the affected seat **recolours**
and that a tap routes to the right cart intent — proving behavioural parity end-to-end.

**Not test-gated here (manual, recorded in `validation.md`):** the non-functional acceptance
— "a ~1000-seat hall opens without freeze and updates without jank; Red/Black/White render
pixel-identically" — is verified by **manual profiling** on a target device. It is a
performance property a widget test cannot assert directly.

Prior art to mirror: slice 0004's `connectivity_overlay_freeze_fix_outside_in_test.dart`
(one file, several red cases gating a fix slice) + its extracted pure resolver and
mocked-bloc widget tests. Use `bloc_test` / `mocktail` per project conventions.

---

## 7. Report (what the implementing agent must hand back)

- List of files created (`seat_id.dart`, `seat_index.dart`) and modified (`seat_state.dart`,
  `seats_movie_session_widget.dart`, `seat_widget.dart`).
- Confirmation that **no other slice/feature** was touched, **no `core/`** change, **no
  `_shared` / API-client / `pubspec.yaml`** change, **no `SeatBloc` logic** change.
- Confirmation that per-seat `BlocSelector` isolation is preserved (Design B, not A).
- Parity statement: colour mapping and tap→cart wiring unchanged; the only intentional
  behaviour change is wide-hall horizontal scroll.
- Test run: number of new tests across the four files, all green; the outside-in test green.
- `dart format` / `dart analyze` clean.

---

## 8. What NOT to do

- ❌ Do **not** collapse the per-seat selectors into one whole-grid subscription (rejected
  Design A) — it destroys rebuild isolation.
- ❌ Do **not** put `byId` into `SeatState.props` / equality — it is derived from `seats`.
- ❌ Do **not** change `SeatBloc` logic, the EventBus/SignalR path, `Seat`/`CinemaSeat`
  shapes, the shopping-cart contract, or the backend.
- ❌ Do **not** change seat identity away from `(row, seatNumber)`.
- ❌ Do **not** introduce `CustomPaint`, `InteractiveViewer`, zoom/pan, free-form coordinate
  rendering, zones, or variable seats-per-row — those are later ADR-0005 phases (P2–P6).
- ❌ Do **not** keep the tap ripple / hover highlight (intentionally dropped with the button).
- ❌ Do **not** migrate `seats` / `cinema_halls` to the target stack (ports/adapters,
  `slang`, `retrofit`, `injectable`, renaming `*Repo`/`*Bloc`) — they stay legacy.
- ❌ Do **not** add a dependency to `pubspec.yaml`.
- ❌ Do **not** add a vertical scroll inside the grid (it already exists in `seats_view.dart`).
- ❌ Do **not** restructure the seat-selection state model beyond adding the derived `byId`.
```
