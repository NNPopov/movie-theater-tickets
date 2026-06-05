# PRD — 0007 Free-form seat-map renderer (CustomPaint)

> Feature: `seats` · Slice: `0007_free_form_seat_renderer` · ADR 0005 / P5
> Status: needs-triage

## Problem Statement

On a phone, the seat-selection screen is hard to use. A real hall (the largest seeded
hall has 616 seats) is drawn so small that, verified on a 5" device, a customer cannot
reliably tap the seat they want — neighbouring seats are a finger-width apart. There is
no way to zoom in to aim at a specific seat, and a hall wider than the screen is awkward
to navigate. The current screen can also only ever draw a plain rectangular grid: seat
position is implied by array indices, so it cannot show variable rows, gaps, staggered
seats, curved rows, or a screen on any side but the top — even though the hall-layout
contract (slice 0006) can already describe all of that.

## Solution

Replace the index-driven grid with a coordinate-driven renderer that draws the whole
hall from explicit geometry (the `SeatLayout` from slice 0006), inside a pan-and-zoom
viewport. The customer sees the hall laid out exactly where each seat sits in layout
space, can pinch-zoom and drag to focus on any area, and taps a seat — even while zoomed
in — to select or deselect it. Seat colours still update live as statuses change over
SignalR, and seats still recolour individually without redrawing the whole hall. Halls
that the backend still serves in the legacy grid form keep rendering exactly as before,
because their geometry is synthesised into the same `SeatLayout` shape. The customer's
selection behaviour is unchanged — only how the hall is drawn and how a tap is resolved
to a seat changes.

## User Stories

1. As a moviegoer on a phone, I want to zoom into the hall, so that the seat I want is
   big enough to tap accurately.
2. As a moviegoer on a phone, I want to pinch out to zoom in and pinch in to zoom out,
   so that zooming feels natural.
3. As a moviegoer on a phone, I want to drag (pan) the hall while zoomed in, so that I
   can reach seats that are off the edge of the screen.
4. As a moviegoer, I want to tap a seat while zoomed in and have the correct seat
   respond, so that zooming actually helps me aim instead of misfiring on a neighbour.
5. As a moviegoer, I want seat numbers to stay crisp and readable when I zoom in, so
   that I can confirm I am picking the right seat.
6. As a moviegoer, I want a free (available) seat to become selected when I tap it, so
   that I can build my order.
7. As a moviegoer, I want a seat I have selected to deselect when I tap it again, so
   that I can change my mind.
8. As a moviegoer, I want seats blocked by other customers to look distinct and not
   respond to my taps, so that I do not try to take a seat that is gone.
9. As a moviegoer, I want sold/empty positions to be non-interactive, so that I only
   ever act on real, selectable seats.
10. As a moviegoer, I want a seat's colour to update the instant its status changes on
    the server, so that I always see an accurate, live picture of the hall.
11. As a moviegoer, I want only the affected seat to change when one seat's status
    changes, so that the hall does not flicker or jump on every update.
12. As a moviegoer, I want the screen opening to be smooth with no freeze, even for the
    largest halls, so that I am not stuck staring at a frozen screen.
13. As a moviegoer, I want to see where the cinema screen is relative to the seats, so
    that I can judge which seats face it best.
14. As a moviegoer, I want to see which row and number a seat is, so that I can match it
    to my expectations.
15. As a moviegoer viewing a hall that is wider than my screen, I want to pan to its
    far side, so that no seats are unreachable.
16. As a moviegoer, I want the hall to fit sensibly into my screen when it first opens,
    so that I see the whole hall before deciding where to zoom.
17. As a moviegoer on a large tablet or desktop, I want the same hall to use the extra
    space, so that I rarely need to zoom at all on a big screen.
18. As a moviegoer, I want the reservation countdown and shopping cart to keep working
    exactly as before, so that the new rendering does not disturb my booking flow.
19. As a returning customer, I want a legacy (grid) hall to look the same as it always
    did, so that nothing visibly regresses while the backend catches up.
20. As a product owner, I want the renderer to already understand non-rectangular layouts
    (variable rows, gaps, staggered seats, a screen on any side), so that when the backend
    and editor ship real geometry, no further rendering work is needed.
21. As a maintainer, I want the coordinate math and tap resolution to be pure, isolated,
    and unit-tested, so that the zoom/pan hit-testing — historically a bug source — is
    trustworthy.
22. As a maintainer, I want the renderer to depend only on the geometry port from slice
    0006, so that swapping the temporary client synthesiser for the real backend later
    requires no renderer change.
23. As a maintainer, I want status to be read from the existing whole-hall index, so that
    the O(1)-per-seat lookup and per-seat repaint isolation from slice 0005 are preserved.

## Implementation Decisions

- **Render core: `CustomPaint` + `InteractiveViewer`.** Confirmed in ADR 0005 (P5
  decision pinned 2026-06-04): a single painter draws the whole hall; `InteractiveViewer`
  provides zoom and pan. `Stack`+`Positioned` was considered and rejected because, with
  zoom-to-select as a core phone interaction, a scaled widget layer rasterises seat
  numbers blurry, whereas the painter redraws crisply at any zoom. Performance was not the
  deciding factor (at ~616–1000 seats either approach is fast enough).
- **Geometry source is slice 0006, unchanged.** The renderer consumes `SeatLayout`
  through the existing `SeatLayoutSource` port; today that is served by the temporary
  bootstrap synthesiser, later by the backend. The renderer does not know or care which.
  Legacy-hall default geometry is therefore already provided and needs no new code here.
- **Coordinate model: layout space → canvas space.** All geometry arrives in layout-space
  seat-pitch units. The renderer owns the transform that fits the layout `bounds` into the
  available canvas, combined with the live `InteractiveViewer` zoom/pan matrix, and owns
  the **inverse** transform used to resolve taps. The backend never reasons about canvas
  pixels.
- **Deep, pure modules (testable without pumping a widget):**
  - a **layout↔canvas transform** (fit-to-viewport + pitch + viewer matrix, plus inverse);
  - a **hit-test resolver** mapping a canvas point to a `SeatId` (or none), accounting for
    each seat's rect, size, and rotation;
  - a **`colorFor`** mapping a seat's live status (relative to my cart's `hashId`) to a
    fill colour, reproducing the existing palette (my-selected / my-blocked / blocked-by-
    other / available / empty).
- **Status stays live and per-seat-isolated.** The painter reads the whole-hall status
  index (`byId`) introduced in slice 0005 and recolours via a status-snapshot/version
  comparison in `shouldRepaint`. This is a **conscious, documented deviation** from ADR
  §6's "per-cell O(1) selector" wording: a painter has no per-seat widgets, so it reads the
  whole map directly — O(1) lookup and the single map-level subscription (ADR §4) are
  preserved.
- **Tap intent is unchanged.** Once a tap resolves to a `SeatId`, the renderer dispatches
  the same select / unselect intents to the existing shopping-cart cubit, keyed by
  `(row, seatNumber)`. Sold/empty and other-blocked seats ignore taps per current rules.
- **Code style: legacy.** This slice stays in the legacy idiom of its neighbours
  (`flutter_bloc` + `get_it` without `injectable`, the existing per-route bloc providers).
  Introducing `injectable` is deliberately deferred to a separate, tracked DI-migration
  slice so the feature and the migration are not entangled.
- **New state holder.** A render-agnostic loader (legacy bloc/cubit) fetches and holds the
  `SeatLayout` for the hall; the existing status feed (`SeatBloc`) and cart feed
  (`ShoppingCartCubit`) are reused as-is. Surrounding screen chrome (movie info, shopping
  cart, countdown) is untouched.
- **Initial fit + responsiveness.** The hall fits its `bounds` into the viewport on open;
  larger canvases (tablet/desktop) use the extra space, reducing the need to zoom. A hall
  wider than the viewport is handled by pan/zoom inside the viewer rather than by
  overflow.

## Testing Decisions

- **Test external behaviour, not implementation.** Tests assert what the customer or a
  caller observes — a tap on a given point routes the right select/unselect intent; a
  status update recolours only the affected seat; the whole hall fits and can be panned —
  never private fields or paint-call internals. CustomPaint pixels are not asserted
  directly; drawing correctness is guaranteed through the pure modules plus behavioural
  taps.
- **Pure modules get heavy unit coverage** (the bulk of the testing value):
  - the transform: layout→canvas and canvas→layout round-trips at several zoom/pan
    states, fit-to-viewport for different canvas sizes;
  - the hit-test resolver: hits and misses across pan/zoom, including rotated and
    variable-size seats, and the gaps between seats resolving to "no seat";
  - `colorFor`: every status/`hashId` combination maps to the expected palette colour,
    and an unknown/empty seat to the empty rendering.
- **Loader state holder** is tested with `mocktail` mocks of the geometry port, covering
  load-success and load-failure states. Per project conventions the client has no
  `bloc_test`; states are driven and asserted with plain mocks.
- **Outside-in acceptance test (render-agnostic).** Drives the real slice end to end with
  only the network/feeds mocked, and asserts behaviour that survives any future render
  swap: tapping a free seat routes select; tapping a selected seat routes unselect;
  tapping an empty/index-miss point routes nothing; a live status update recolours one
  seat; a legacy grid hall renders its seats at the expected positions.
- **Prior art.** Slice 0005 (`seat_grid_performance`) outside-in and widget tests for the
  tap-routing and recolour behaviour and the `byId` index; slice 0006
  (`hall_layout_contract`) synthesiser and model tests for layout-space geometry and the
  `(row, number)` identity. Reuse their mocking patterns (mocked `SeatBloc`/cart feeds,
  mocked port, seeded hall shapes Red 616 / Black 378 / White 180).

## Out of Scope

- **Zone rendering.** Drawing zone polygons and per-seat zone tints is deferred to the
  slice that first produces real zone data (backend/editor). The renderer ignores
  `zones[]` for now.
- **Price layer.** The live per-seat price overlay (`PriceBloc`, pushed from
  BookingManagement over SignalR) is a separate slice; this slice renders status only.
- **Backend geometry (ADR P3/P4).** Serving real `SeatLayout` from the backend and
  deleting the temporary client synthesiser are later, separate steps; this slice consumes
  whatever the port returns.
- **Admin hall editor (ADR P6).** Authoring/editing layouts is Phase 3.
- **Accessibility.** Per-seat screen-reader `Semantics` over the painter is explicitly out
  of scope (decided 2026-06-04); the "CustomPaint loses widget semantics" risk is accepted
  for now and becomes its own follow-up if required.
- **DI migration.** Adopting `injectable` is a separate tracked slice; this slice uses the
  legacy `get_it` registration.

## Further Notes

- Anchored in ADR 0005 ("Seat map rendering and free-form hall layout"), specifically the
  P5 item and its 2026-06-04 pinned rendering decision; vocabulary follows `CONTEXT.md`
  (SeatId, layout space, canvas space, SeatPlacement, Screen, Zone).
- Scale target carried from P1: render up to ~1000 seats without a freeze; the real
  seeded maximum is 616. CustomPaint's culling head-room means thousands remain feasible
  later without re-architecture.
- The seam to keep the future render-core swap (and the eventual backend cutover) cheap:
  the transform and `colorFor` stay pure and independently tested, and the outside-in test
  is written against behaviour, not the painter.
