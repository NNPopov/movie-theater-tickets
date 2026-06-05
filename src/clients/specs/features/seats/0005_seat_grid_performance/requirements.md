# 0005 · seat_grid_performance — Requirements

> Phase-1 ("quick win") of ADR 0005: a scoped performance fix on **legacy** seat code.
> The headline contract is **behavioural parity** — every functional requirement below
> except F12 asserts "unchanged from before". F12 is the single intentional change.

## Functional Requirements

| ID | Requirement |
|---|---|
| F1 | The seat-selection screen opens without freezing, even for a large hall (up to ~1000 seats). |
| F2 | A large hall (e.g. Red 28×22 = 616 seats) is as responsive to interaction as a small hall. |
| F3 | When a seat's status changes on the server (reserved, released, expired, sold), exactly that seat recolours, without stutter. |
| F4 | The reservation countdown and live status updates remain smooth (no per-tick jank). |
| F5 | Tapping a free (available) seat selects it, routing the select intent for its `(row, seatNumber)` and the current movie session. |
| F6 | Tapping a seat the current user has selected releases it, routing the unselect intent for its `(row, seatNumber)` and the current movie session. |
| F7 | Sold and empty seats are non-interactive — tapping them dispatches no cart intent. |
| F8 | Each seat shows the same colour as before for its status: mine-selected (greenAccent), mine-blocked (green), taken-by-others (blue), available (grey), empty (black12). |
| F9 | A coordinate with no live seat status renders as the empty (non-interactive) seat, with no error. |
| F10 | Row labels, the screen bar, and the overall hall layout look identical to before. |
| F11 | Selecting or unselecting a seat is gated on the shopping cart being initialised (cart status not `initial`), exactly as before. |
| F12 | A hall wider than the viewport scrolls sideways instead of overflowing off-screen (the one intentional behaviour change; vertical scrolling continues to work as before). |

## Non-functional Requirements

| ID | Requirement |
|---|---|
| N1 | Per-seat status is resolved by an O(1) lookup keyed on `SeatId = (int row, int seatNumber)`, not a per-cell linear `firstWhere` scan. |
| N2 | The status index (`Map<SeatId, Seat>`) is built once per `SeatState` instance and shared by all seat cells, not rebuilt per cell. |
| N3 | The status index (`byId`) is a derived field excluded from `SeatState` equality (`props` stays `[seats, status]`), so adding it changes no equality semantics. |
| N4 | Per-seat rebuild isolation is preserved (one `BlocSelector` per seat); a single status change repaints one cell, not the whole hall (Design B, not whole-grid Design A). |
| N5 | The status-index derivation is a pure, Flutter-free, top-level function (`buildSeatIndex`) unit-testable without pumping widgets. |
| N6 | An index miss returns `null`, reproducing the legacy `firstWhere`-catch "empty seat" path with identical behaviour. |
| N7 | Each seat is rendered with a lightweight non-Material widget (`GestureDetector` + `DecoratedBox` + centred text), not a Material `TextButton`. |
| N8 | The seat widget keeps the whole 19×19 cell tappable via opaque hit behaviour, and a null tap handler renders a non-interactive seat. |
| N9 | The seat widget's public constructor shape (`text`, `foregroundColor`, `backgroundColor`, `onPressed`) is preserved so callers do not churn. |
| N10 | The grid uses plain `Column`/`Row` (with `mainAxisSize: .min`) instead of nested `ListView.builder`s in `shrinkWrap`/`NeverScrollableScrollPhysics` mode. |
| N11 | The horizontal scroll wrapper has a bounded width (constrained from the viewport) to avoid an unbounded-width error. |
| N12 | Seat identity remains `(row, seatNumber)`; booking, reservation, shopping-cart logic, SignalR payloads, and the backend are untouched. |
| N13 | `SeatId` is expressed as a single shared Dart record typedef, with no `Equatable`/boilerplate, reusable unchanged by ADR-0005 Phase 2. |
| N14 | `SeatBloc` logic and the EventBus/SignalR update path are not modified; the bloc keeps emitting via `copyWith`, which recomputes `byId` once. |
| N15 | No new `pubspec.yaml` dependency is added. |
| N16 | As legacy-code modification, the change stays in legacy style — no migration to ports/adapters, `slang`, `retrofit`, `injectable`, or renaming `*Repo`/`*Bloc`; no `core/`, routing, DI, or `_shared`/API-client change. |
| N17 | The slice ships with tests on the pure seam (unit), `SeatBloc` (`bloc_test`), the grid widget (mocked blocs, mocktail), and one outside-in acceptance test; adapter and use-case layers are N/A (none added or changed). |

## Out of scope

- Free-form coordinate rendering (`CustomPaint` + explicit `SeatPlacement`, `InteractiveViewer` zoom/pan) — ADR-0005 Phase 2/P5.
- Zones (balcony/stalls sectioning) — Phase 3/P6.
- Backend / layout-contract changes (variable seats-per-row, per-seat coordinates) — Phase 2 (P3/P4).
- True scaling to thousands of seats beyond the ~1000 target — guaranteed by P5.
- Migrating `seats` / `cinema_halls` to the target stack — they stay legacy.
- Keeping the tap ripple / hover highlight — intentionally dropped with the Material button.
- Restructuring the seat-selection state model beyond adding the derived `byId` field.
