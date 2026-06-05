# 0007 · free_form_seat_renderer — Requirements

## Functional Requirements

| ID | Requirement |
|---|---|
| F1 | The system shall draw the hall from explicit `SeatLayout` geometry, placing each seat at its layout-space position (coordinate-driven, not index-driven). |
| F2 | The customer can pinch out to zoom in and pinch in to zoom out within the seat viewport. |
| F3 | The customer can drag to pan the hall while zoomed in. |
| F4 | A tap at any zoom level resolves to the seat whose placement rect contains the tapped point. |
| F5 | Seat numbers shall stay crisp and legible at any zoom level. |
| F6 | Tapping an available (free) seat routes the select intent (`seatSelect`) for that `(row, seatNumber, movieSessionId)`. |
| F7 | Tapping a seat the customer has selected routes the unselect intent (`unSeatSelect`) for that seat. |
| F8 | Tapping a seat blocked by the customer's own cart (not yet selected) routes the unselect intent. |
| F9 | A seat blocked by another customer is shown in a distinct colour and routes the same intent as today (unselect), preserving the legacy behaviour. |
| F10 | A tap that resolves to no seat — a gap between seats, a point outside `bounds`, or a position with no live status — routes nothing (non-interactive). |
| F11 | Each seat is filled with the palette colour for its live status relative to the customer's cart `hashId` (mine-selected → greenAccent, mine-blocked → green, other-blocked → blue, available → grey, empty/index-miss → black12), reproducing the legacy palette exactly. |
| F12 | A seat's colour updates immediately when its status changes on the live status feed (SignalR / `SeatsUpdateEvent`). |
| F13 | A live status change recolours only the affected seat(s), leaving every other seat visually unchanged. |
| F14 | The cinema screen indicator is drawn at the side and position given by `SeatLayout.screen`. |
| F15 | The hall fits its `bounds` into the viewport on open, so the whole hall is visible before the customer zooms. |
| F16 | On a larger canvas (tablet/desktop) the hall uses the extra space (larger initial scale), reducing the need to zoom. |
| F17 | A hall wider than the viewport is fully reachable via pan/zoom, so no seat is unreachable. |
| F18 | A legacy (grid) hall renders the same as before, with its seats at the synthesised grid positions. |
| F19 | Tap intents are dispatched only when a shopping cart exists (cart status != `initial`); otherwise the tap is ignored. |
| F20 | The reservation countdown and the shopping cart continue to work exactly as before, undisturbed by the new rendering. |

## Non-functional Requirements

| ID | Requirement |
|---|---|
| N1 | The render core is a single `CustomPaint` painter inside an `InteractiveViewer` (ADR 0005 P5); no scaled `Stack`+`Positioned` widget layer is used. |
| N2 | Geometry is consumed only through the `SeatLayoutSource` port; the renderer does not depend on the backend or on the legacy `List<List<CinemaSeat>>` grid directly. |
| N3 | The layout↔canvas transform is a pure, widget-free module (imports `dart:ui` only, no `package:flutter`) with a tested round-trip inverse. |
| N4 | The hit-test resolver is a pure, widget-free function mapping a layout-space point to `SeatId?`, honouring each seat's rect, size (`w`/`h`), and `rotation`. |
| N5 | `colorForSeat` is a pure function reproducing the five-way palette, and colour and tap intent are derived from one shared classifier so they never diverge. |
| N6 | No seat geometry or hit-testing math lives in widgets; all tap→seat resolution goes through the pure transform + resolver. |
| N7 | Live status is read from `SeatState.byId` (the O(1) index from slice 0005), preserving O(1)-per-seat lookup and the single map-level subscription. |
| N8 | The painter's `shouldRepaint` triggers only on a change of status (`byId`), cart `hashId`, or transform — the documented deviation from ADR §6's per-cell selector, recorded in `.claude/decisions/`. |
| N9 | Seat identity stays `SeatId = (row, seatNumber)`; the renderer keys all lookups and tap routing by it. |
| N10 | A new legacy `SeatLayoutCubit` loads and holds the `SeatLayout`, depending only on `SeatLayoutSource`; no new use-case or network adapter is added. |
| N11 | The slice stays in the legacy idiom (`flutter_bloc` + `get_it` without `injectable`, per-route bloc providers); no migration to `slang`/`retrofit`/`injectable`/ports-everywhere. |
| N12 | No new `pubspec.yaml` dependency is added and no `build_runner`/`slang` regeneration is required. |
| N13 | `SeatBloc`, `SeatState`/`byId`, `seat_index`, the EventBus/SignalR path, the `ShoppingCartCubit` contract, the `SeatLayout`/`SeatPlacement` models, the `SeatLayoutSource` port, and `BootstrapSeatLayoutSource` are not modified. |
| N14 | `SeatLayout.zones` and any per-seat price are ignored; the renderer draws status only. |
| N15 | The surrounding seats-screen chrome is untouched; the slice swaps only the hall body and provides `SeatLayoutCubit`, dropping the now-unused `CinemaHallInfoBloc` from that screen. |
| N16 | No hardcoded UI strings; any error shown reuses the existing localized idiom (seat numbers are numeric, not localized copy). |
| N17 | The outside-in acceptance test asserts render-agnostic behaviour (taps, routed intents, recolour), never `CustomPaint` pixels or painter internals. |
| N18 | Cubit tests drive the real cubit directly with `mocktail` mocks (no `bloc_test`, which the client does not depend on). |
| N19 | The legacy grid widgets (`seats_movie_session_widget.dart`, `seat_widget.dart`) are left in place but un-mounted; they are not deleted or edited in this slice. |
| N20 | The renderer meets the scale target: up to ~1000 seats render without a freeze (the largest seeded hall is 616). |

## Out of scope

- Zone rendering — drawing zone polygons or per-seat zone tints (deferred to the slice that first produces real zone data).
- Price layer — the live per-seat price overlay (a separate slice; status only here).
- Backend geometry — serving a real `SeatLayout` from the backend and deleting the temporary client synthesiser (later, separate steps).
- Admin hall editor — authoring/editing layouts (ADR Phase 3).
- Accessibility — per-seat screen-reader `Semantics` over the painter (accepted risk for now, its own follow-up).
- DI migration — adopting `injectable` (a separate tracked slice; this slice uses legacy `get_it`).
