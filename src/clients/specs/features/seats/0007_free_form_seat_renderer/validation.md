# 0007 · free_form_seat_renderer — Validation Checklist

## Manual testing

| # | Step | Expected result |
|---|---|---|
| M1 | Open the seats screen for a seeded hall (e.g. Red, 616 seats) on a phone | The whole hall fits in the viewport on open; every seat is drawn at its layout position; the screen indicator is visible. (F1, F14, F15) |
| M2 | Pinch out on the hall | The hall zooms in; seats and numbers get larger. (F2) |
| M3 | Pinch in on the zoomed hall | The hall zooms back out smoothly. (F2) |
| M4 | While zoomed in, drag across the hall | The hall pans; seats that were off the edge become reachable. (F3) |
| M5 | Zoom in and read a seat number | The number stays crisp and legible — not blurry or pixelated. (F5) |
| M6 | Tap an available (grey) seat | It becomes selected (greenAccent) and appears in the shopping cart. (F6, F11) |
| M7 | Tap the now-selected (greenAccent) seat again | It deselects (returns to grey) and is removed from the cart. (F7, F11) |
| M8 | Zoom in fully, then tap one seat among close neighbours | The exact tapped seat responds — no neighbour misfires. (F4) |
| M9 | Observe a seat blocked by another customer | It is shown in the distinct blue colour. (F9, F11) |
| M10 | Tap an empty position / a gap between seats | Nothing happens; no selection is made. (F10) |
| M11 | From a second client, block one seat | Only that seat recolours (to blue) almost immediately; all other seats are unchanged. (F12, F13) |
| M12 | Select a single seat | Only that seat recolours; the rest of the hall does not flicker, jump, or redraw visibly. (F13) |
| M13 | Open a hall wider than the screen | The far side is reachable by pan/zoom; no seat is unreachable; no RenderFlex/overflow error. (F17) |
| M14 | Open the same hall in a large tablet/desktop window | The hall uses the extra space (larger initial scale); little or no zoom is needed. (F16) |
| M15 | Open a legacy (grid) hall | It looks the same as before — seat positions, screen, and colours unchanged. (F18) |
| M16 | Before any cart exists (cart status initial), tap an available seat | Nothing is routed; no seat is selected. (F19) |
| M17 | During seat selection, watch the reservation countdown and the shopping cart panel | Both continue to work exactly as before, undisturbed by the new renderer. (F20) |
| M18 | Force the layout load to fail (hall/backend error) | An error state is shown; the screen does not crash or hang. (F1 negative path) |
| M19 | Open the largest seeded hall (616 seats) and interact | The screen opens immediately with no freeze; live updates and taps stay responsive (no jank). (F12, F20, scale target) |
| M20 | Inspect the screen indicator on a hall whose `Screen.side` is not `top` (or the legacy `top`) | The screen is drawn on the correct edge per `SeatLayout.screen`. (F14) |
| M21 | Tap a seat the user's own cart has blocked but not selected (green) | It routes the unselect intent (deselects). (F8) |

## Code review

- [ ] Render core is a single `CustomPaint` painter inside an `InteractiveViewer`; no `Stack`+`Positioned` scaled-widget layer is used. (N1)
- [ ] The renderer obtains geometry only via the `SeatLayoutSource` port (`SeatLayoutCubit`); no direct dependency on the backend or the legacy `List<List<CinemaSeat>>` grid. (N2)
- [ ] `seat_layout_transform.dart` imports only `dart:ui`/`dart:math` — no `package:flutter`; it exposes `layoutToCanvas`/`canvasToLayout` and is round-trip tested. (N3)
- [ ] `seat_hit_tester.dart` is a pure top-level function returning `SeatId?`, honouring rect, `w`/`h`, and `rotation`; no widget imports. (N4)
- [ ] `colorForSeat` is a pure function reproducing the five-way palette byte-for-byte; the tap-intent classifier (`tapIntentFor`) is derived from the same logic. (N5, F11)
- [ ] No seat geometry or hit-testing math appears in widgets — `seat_map_view.dart` resolves taps only through the transform + `resolveSeatAt`. (N6)
- [ ] The painter reads live status from `SeatState.byId` (the slice-0005 index), not by scanning `seats`. (N7)
- [ ] `SeatMapPainter.shouldRepaint` returns true only on a change of `byId`, `cartHashId`, or `transform`. (N8)
- [ ] All lookups and tap routing are keyed by `SeatId = (row, seatNumber)`. (N9)
- [ ] `SeatLayoutCubit` depends only on `SeatLayoutSource`; no new use-case or network adapter was added. (N10)
- [ ] The slice stays legacy: `flutter_bloc` + `get_it` without `injectable`; no `slang`/`retrofit`/`injectable`/ports-everywhere migration. (N11)
- [ ] `git diff pubspec.yaml` is empty; no new dependency added. (N12)
- [ ] `SeatBloc`, `SeatState`/`byId`, `seat_index`, the EventBus path, `ShoppingCartCubit`, the `SeatLayout`/`SeatPlacement` models, the `SeatLayoutSource` port, and `BootstrapSeatLayoutSource` are unchanged in the diff. (N13)
- [ ] `SeatLayout.zones` and price are not read or drawn — status only. (N14)
- [ ] `seats_view.dart` swaps only the hall body (grid → `SeatMapView`), adds the `SeatLayoutCubit` provider, drops the unused `CinemaHallInfoBloc` provider, and leaves the movie/cart/countdown chrome untouched. (N15)
- [ ] No hardcoded UI strings; any error message reuses the existing localized idiom. (N16)
- [ ] The outside-in test asserts taps/intents/recolour behaviour, never `CustomPaint` pixels or painter internals. (N17)
- [ ] `SeatLayoutCubit` tests drive the real cubit with `mocktail` mocks (no `bloc_test`). (N18)
- [ ] The legacy grid widgets (`seats_movie_session_widget.dart`, `seat_widget.dart`) are unchanged in the diff and left un-mounted (not deleted). (N19)
- [ ] The ADR §6 deviation (painter reads whole `byId`; whole-canvas repaint) is recorded in `.claude/decisions/`. (N8)
- [ ] `dart run build_runner build --delete-conflicting-outputs` — no errors (N/A unless generated files changed; this slice touches none). (N12)
- [ ] `dart run slang` — N/A: the client localizes with `intl`/gen-l10n, not `slang`; run only if `*.json` under `lib/core/i18n/` changes (it does not here).
- [ ] `dart analyze` — no warnings.
- [ ] All tests green (`flutter test test/features/seats/0007_free_form_seat_renderer/`, incl. the outside-in gate).
