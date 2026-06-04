# 0005 · seat_grid_performance — Validation Checklist

> Behavioural-parity fix on legacy seat code. The **non-functional acceptance**
> (open-without-freeze, update-without-jank, pixel-identical render) is verified here by
> **manual profiling**, since a widget test cannot assert it (per `prd.md`). Profile on a
> real target device, not just a fast desktop build.

## Manual testing

| # | Step | Expected result |
|---|---|---|
| M1 | Open the seats screen for a session in the **Red** hall (28×22 = 616). | Screen appears immediately; no visible freeze/lock-up before seats are interactive. (F1, F2) |
| M2 | While on M1's screen, watch a DevTools performance/frame chart on open. | The initial build is at most a 1–2 dropped-frame hitch, not a multi-hundred-ms synchronous freeze. (F1) |
| M3 | Open the seats screen for the **Black** (21×18) and **White** (15×12) halls. | Both render with the same colours, row labels, screen bar, and layout as before this slice. (F8, F10) |
| M4 | From a second client/browser, reserve a seat in the hall you are viewing. | Exactly that one seat recolours to taken-by-others (blue); no full-grid flash, no stutter. (F3, F8) |
| M5 | From the second client, release / let a reservation expire on a seat. | That seat recolours back to available (grey) instantly; surrounding seats do not repaint. (F3) |
| M6 | Keep the screen open while a reservation countdown is ticking and updates arrive. | Countdown and recolours stay smooth — no per-tick jank. (F4) |
| M7 | Tap a **grey (available)** seat (cart initialised). | Seat is selected and recolours to mine-selected (greenAccent); a select intent is sent for that `(row, seatNumber)`. (F5, F8) |
| M8 | Tap a seat you just selected (greenAccent / green). | Seat is released and recolours to available; an unselect intent is sent. (F6) |
| M9 | Tap a **sold** seat and an **empty** (black12) cell. | Nothing happens — no selection, no intent dispatched. (F7, F9) |
| M10 | Open the screen before the shopping cart is initialised, then tap a free seat. | Tap is ignored (no intent) until the cart leaves the `initial` state. (F11) |
| M11 | Narrow the window (or use a small device) so the hall is wider than the viewport. | The hall **scrolls sideways**; no off-screen overflow, no "RenderFlex overflowed" / unbounded-width error. (F12, N11) |
| M12 | With the same narrow window, scroll vertically. | Vertical scrolling still works as before (owned by the outer screen, not duplicated). (F12) |
| M13 | Compare the resting render (a screenshot) of Red/Black/White against the pre-slice build. | Pixel-identical seats (19×19, same colours, same centred numbers); only the lost tap-ripple/hover differs. (F8, F10) |
| M14 | (Synthetic, if available) Render a ~1000-seat hall and profile open + a status update. | Opens without freeze; a single update repaints one cell at O(1) cost — no jank. If a visible hitch remains on open, that is the trigger to bring ADR-0005 Phase 2/P5 forward (not to change this slice). (F1, F2, N1, N4) |

## Code review

- [ ] `SeatId` is a single shared record typedef `(int row, int seatNumber)` in `seats/domain/entities/seat_id.dart`, with no `Equatable`/boilerplate. (N1, N13)
- [ ] `buildSeatIndex` is a top-level pure function with **no `package:flutter` import**; returns `Map<SeatId, Seat>`. (N5)
- [ ] An index miss returns `null` and the grid maps that to the empty (black12, non-interactive) seat — matching the old `firstWhere`-catch path. (N6, F9)
- [ ] `SeatState` builds `byId` once per instance from `seats`; `byId` is **not** in `props` (`props` stays `[seats, status]`). (N2, N3)
- [ ] The per-seat selector reads `state.byId[(row, seatNumber)]` — no `firstWhere`/linear scan remains in the grid. (N1)
- [ ] One `BlocSelector` per seat is preserved (Design B); no single whole-grid subscription was introduced. (N4)
- [ ] `SeatBloc` (`seat_cubit.dart`) and the EventBus/`SeatsUpdateEvent` path are unchanged. (N14)
- [ ] `SeatWidget` uses `GestureDetector(behavior: opaque)` + `DecoratedBox` + centred `Text`; no Material `TextButton`. (N7, N8)
- [ ] `SeatWidget` keeps its public constructor (`text`, `foregroundColor`, `backgroundColor`, `onPressed`); a null `onPressed` renders non-interactive. (N8, N9)
- [ ] Grid uses `Column`/`Row` with `mainAxisSize: .min`; no `ListView.builder` in `shrinkWrap`/`NeverScrollableScrollPhysics` remains in the grid. (N10)
- [ ] Horizontal scroll wrapper has a bounded width (constrained from the viewport). (N11)
- [ ] Colour/action mapping in `buildSeat` is unchanged (greenAccent/green/blue/grey + select/unselect wiring). (F8, F5, F6)
- [ ] Seat identity stays `(row, seatNumber)`; no change to shopping-cart calls, SignalR payloads, or backend. (N12)
- [ ] No `pubspec.yaml` dependency added; no `core/`, routing, DI, or `_shared`/API-client change; no `*Repo`/`*Bloc` rename. (N15, N16)
- [ ] Tests present on all applicable layers: pure-seam unit, `SeatBloc` `bloc_test`, grid widget (mocked blocs/mocktail), one outside-in acceptance test; adapter/use-case correctly omitted (N/A). (N17)
- [ ] `dart format .` — no diff.
- [ ] `dart run build_runner build --delete-conflicting-outputs` — **N/A** for this slice (no freezed/injectable/retrofit/slang touched; `SeatId` is a hand-written typedef). Confirm none of those files changed.
- [ ] `dart run slang` — **N/A** (legacy slice uses gen-l10n; no `*.json` i18n added; existing `screen`/`row` keys reused).
- [ ] `dart analyze` — no new warnings.
- [ ] All tests green (including the outside-in acceptance test).
