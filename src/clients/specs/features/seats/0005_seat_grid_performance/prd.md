# PRD — Seat grid performance fix (ADR 0005 / Phase 1)

- **Slice:** `0005_seat_grid_performance`
- **Feature:** seats
- **Type:** Tracked performance/defect-fix slice on legacy code (no domain/contract change)
- **Source:** `.claude/decisions/0005_seat_map_rendering.md` "Phase 1 — P1" + grilling
  session 2026-06-04 (`/grill-with-docs`)
- **Status:** Planned

## Problem Statement

When a moviegoer opens the seat-selection screen for a session, the screen **freezes** for
a noticeable time on open, and then **janks** (stutters) every time a seat's status changes.
The larger the hall, the worse it is — the largest seeded hall (Red, 28×22 = 616 seats) is
where it is most visible. The freeze makes the screen feel broken before the user can pick a
seat, and the jank makes real-time updates — another customer reserving or releasing a seat,
a reservation expiring, the countdown ticking — feel laggy.

The freeze is not a network wait (a loading screen would not help); it is in the build/layout
pass *after* the seat data has arrived. Two root causes, both in the seat grid:

1. **O(N²) status lookup.** The grid iterates the `CinemaSeat` geometry and, for every cell,
   wraps it in its own `BlocSelector` whose selector runs `seats.firstWhere(...)` — an O(N)
   linear scan over the `Seat` status list. For the Red hall that is 616 cells × ~616-scan ≈
   **380 000 comparisons**, executed synchronously in the first frame (the freeze) and **again
   on every emit** — i.e. on every real-time status update and every reservation-countdown
   tick (the ongoing jank).
2. **Heavyweight, eagerly-built widgets.** Each seat is a Material `TextButton` (ink splash,
   `Material` ancestor lookup, theme resolution), and the rows/seats are nested
   `ListView.builder`s in `shrinkWrap` + `NeverScrollableScrollPhysics` mode — which disables
   laziness and builds **all** children eagerly anyway, with sliver overhead on top.

## Solution

From the moviegoer's perspective:

- The seat screen **opens immediately**, even for a large hall (up to ~1000 seats).
- A seat that changes status on the server (reserved, released, expired, sold) **recolours
  instantly**, with no stutter, and the reservation countdown stays smooth.
- The screen looks and behaves **exactly as before**: same seat colours, layout, row labels,
  screen bar; tapping a free seat selects it, tapping my selected seat releases it; sold and
  empty seats stay non-interactive.
- A hall too wide for the window **scrolls sideways** instead of overflowing off-screen
  (vertical scrolling already works).

Internally this is the Phase-1 ("quick win") of ADR 0005: it exploits the geometry-vs-status
split that already exists in embryo (`CinemaSeat` = existence/geometry, `Seat` = live status)
without inventing the explicit-geometry layer (that is Phase 2/P5). Seat **identity stays
`(row, seatNumber)`**, so booking, reservation, and shopping-cart logic are untouched.

## User Stories

1. As a moviegoer, I want the seat screen to open without freezing, so that I can start
   picking seats immediately instead of waiting through a lock-up.
2. As a moviegoer choosing seats in a large hall (hundreds of seats), I want the screen to be
   as responsive as a small hall, so that big auditoriums are not painful to use.
3. As a moviegoer, I want a seat that someone else just reserved to recolour instantly, so
   that I always see live availability without the screen stuttering.
4. As a moviegoer, I want a seat that was released or whose reservation expired to recolour
   instantly, so that I can grab a freed-up seat without lag.
5. As a moviegoer, I want the reservation countdown and live updates to stay smooth, so that
   the screen does not jank on every tick.
6. As a moviegoer, I want tapping a free seat to select it exactly as before, so that the fix
   does not change how I pick seats.
7. As a moviegoer, I want tapping a seat I selected to release it exactly as before, so that
   my selection behaviour is unchanged.
8. As a moviegoer, I want sold and empty seats to stay non-interactive, so that I cannot tap
   something I should not.
9. As a moviegoer, I want the seat colours (mine-selected, mine-blocked, taken-by-others,
   available, empty) to look identical to before, so that nothing about reading the hall
   changes.
10. As a moviegoer, I want the row labels, the screen bar, and the hall layout to look
    identical to before, so that the screen is visually unchanged.
11. As a moviegoer using a narrow window or wide hall, I want the hall to scroll sideways
    rather than spill off-screen, so that I can still reach every seat.
12. As a developer, I want per-seat status resolved by an O(1) map lookup keyed on
    `(row, seatNumber)` instead of a per-cell `firstWhere`, so that the per-emit cost is O(N)
    instead of O(N²).
13. As a developer, I want the status index built once per state instance and shared by all
    seat cells, so that one emit does not rebuild the index per cell.
14. As a developer, I want per-seat rebuild isolation preserved, so that a status change on
    one seat repaints one cell, not the whole hall.
15. As a developer, I want each seat rendered with a lightweight, non-Material widget, so that
    a thousand-seat hall does not carry a thousand ink-capable Material widgets.
16. As a developer, I want the eager, sliver-overhead nested lists replaced with plain layout
    widgets, so that the grid builds with no list machinery it never uses.
17. As a developer, I want the `(row, seatNumber)` identity expressed as a single shared type,
    so that Phase 2 (explicit geometry) inherits the same seat-id shape unchanged.
18. As a developer, I want the status-index derivation isolated as a pure, unit-testable seam,
    so that I can verify the indexing without pumping widgets.
19. As a developer, I want widget tests that lock each observable seat colour and the tap →
    cart-intent wiring, so that "no behaviour change" is enforced, not just claimed.
20. As a developer, I want an outside-in test that drives a hall + live status updates through
    the real blocs into the grid, so that the slice has an acceptance gate proving behavioural
    parity.
21. As a maintainer, I want this tracked as its own slice scoped to the seat grid, so that it
    is a deliberate Phase-1 step and not a silent re-architecture of legacy code.
22. As a maintainer, I want the free-form renderer, zoom/pan, zones, and the backend layout
    contract explicitly deferred to later ADR-0005 phases, so that this slice stays a narrow
    quick win.

## Implementation Decisions

**Design B — memoized status index, per-seat isolation preserved.** Add a derived
`Map<SeatId, Seat>` ("byId") to the seat bloc state, built **once per state instance** from
the `Seat` status list. It is a derived field and stays **out of the state's equality** (it is
a function of the existing `seats` list; including it changes nothing about equality). Each
per-seat selector resolves its status by an **O(1)** `byId[(row, seatNumber)]` lookup instead
of `seats.firstWhere(...)`. This removes the O(N²) freeze while keeping per-seat rebuild
isolation — a status change on one seat repaints one cell, not the whole hall.

- *Rejected alternative — Design A (single subscription, whole-grid rebuild on every emit).*
  Structurally simpler but throws away rebuild isolation for no gain at this scale; on a
  single-seat update it would rebuild every cell.

**`SeatId` value type.** Identity is expressed as the Dart record
`typedef SeatId = (int row, int seatNumber)` — structural equality and hashing for free, no
boilerplate, and the same shape Phase 2/P5 uses for explicit placements, so it is inherited
unchanged. (Captured in `CONTEXT.md`.)

**Lightweight seat widget.** Replace the Material `TextButton` with
`GestureDetector(behavior: opaque)` + `DecoratedBox` + a centred text, keeping the seat
widget's public constructor shape so callers do not churn. **Accepted trade-off:** the tap
ripple and desktop hover highlight are lost; the resting render is pixel-identical and the
load-bearing feedback (the grey→green recolour driven by the bloc/cart) is unchanged. The
opaque hit behaviour keeps the whole cell tappable, matching today's button.

**Plain layout instead of nested lists.** Replace the outer (rows) and inner (seats-per-row)
`ListView.builder`s — which are already non-lazy in `shrinkWrap`/`NeverScrollableScrollPhysics`
mode — with `Column(mainAxisSize: .min)` / `Row(mainAxisSize: .min)`. Same eager build,
without sliver overhead. No visual change.

**Wide-hall layout.** Wrap the hall container in a horizontal scroll view so a hall wider than
the viewport scrolls instead of overflowing the screen (vertical scrolling already exists one
level up). *This is the one intentional behaviour change* — today a too-wide hall overflows;
it will scroll. Implementation note for the plan: a horizontal scroll view nested inside the
existing centred row needs a bounded width (e.g. constrained from the viewport) to avoid an
"unbounded width" error.

**Scale target.** Support **up to ~1000 seats**. The largest real seeded hall is **Red,
28×22 = 616** (Black 21×18 = 378, White 15×12 = 180). At 1000 seats the update path is O(N)
per emit with single-cell repaint; the only residual cost is a possible **1–2 dropped frames
during the initial eager build** of ~1000 lightweight widgets — a brief hitch on open, not a
freeze. If that hitch proves visible on a target device, it is the trigger to bring Phase 2/P5
(`CustomPaint` + `InteractiveViewer`) forward — **not** to embed `CustomPaint` in this slice.

**No domain/contract change.** Seat identity stays `(row, seatNumber)`; booking, reservation,
shopping-cart, SignalR payloads, and the backend are untouched. No new dependencies. These are
**legacy** slices (`*Repo`, `intl`, `Bloc`, manual `get_it`); this slice is a scoped
performance fix and deliberately does **not** migrate them to the target stack.

## Testing Decisions

**What a good test is here.** Tests assert externally observable behaviour — which colour a
seat shows for a given status, that tapping a seat dispatches the correct cart intent, that a
live status change recolours the right cell — never the internal map mechanics, selector
wiring, or layout-widget choice. Because the headline promise is "no behaviour change", the
tests are primarily **parity** tests.

**Deep-module unit tests (the `byId` seam).** The pure derivation `List<Seat> → Map<SeatId,
Seat>` is unit-tested in isolation: correct mapping by `(row, seatNumber)`, O(1) lookup hits
and misses (a miss returns the "empty seat", matching today's caught-exception path), empty
list, and (defensively) duplicate ids. This is the slice's deep, widget-free seam — the
analogue of slice 0004's extracted overlay-mode resolver.

**Cubit/Bloc tests.** `bloc_test` on the seat bloc confirms that emitting a new status list
still produces state whose `byId` resolves each seat correctly across transitions (load,
status update). The bloc's *logic* is not changed by this slice; the coverage locks the
derived field.

**Widget tests (mocked blocs, mocktail).** With mocked seat + cinema-hall blocs, one case per
observable seat colour (mine-selected / mine-blocked / taken-by-others / available / empty),
plus: tapping a free seat calls the cart's select intent, tapping my selected seat calls the
unselect intent, sold/empty seats dispatch nothing. These enforce visual + interaction parity.

**Outside-in acceptance gate.** One file
(`seat_grid_performance_outside_in_test.dart`) drives a hall geometry + an initial status list
through the **real** seat and cinema-hall blocs into the grid, then pushes a status-update
event (simulating the SignalR path) and asserts the affected seat recolours and a tap routes
to the right cart intent — proving behavioural parity end-to-end. The non-functional
acceptance ("a ~1000-seat hall opens without freeze and updates without jank; Red/Black/White
render pixel-identically") is verified by **manual profiling** on a target device, recorded in
`validation.md`; it is a performance property, not something a widget test can assert directly.

**Prior art.** Slice 0004's `connectivity_overlay_freeze_fix_outside_in_test.dart` (one file,
several red cases gating a migration/fix slice) and its extracted pure resolver + mocked-bloc
widget tests. Use `bloc_test` / `mocktail` per project conventions.

**Adapter / use-case layers — N/A.** No network adapter or use-case is added or changed
(geometry and status already arrive through existing paths), so those default layers do not
apply — same exception the 0004 PRD made.

## Out of Scope

Deferred to later ADR-0005 phases or deliberately not done:

- **Free-form coordinate rendering** (`CustomPaint` + explicit `SeatPlacement` geometry,
  `InteractiveViewer` zoom/pan) — Phase 2/P5. This slice keeps the index-driven grid.
- **Zones** (balcony/stalls sectioning) — a Phase-3/P6 editor UX concept, **never** a
  performance mechanism; not introduced here.
- **Backend / layout contract changes** (variable seats-per-row, per-seat coordinates) —
  Phase 2 (P3/P4). No backend change in this slice.
- **True scaling to thousands** beyond the ~1000 target — guaranteed by P5, not P1.
- **Migrating `seats` / `cinema_halls` to the target stack** (ports/adapters, `slang`,
  `retrofit`, `injectable`, renaming `*Repo`/`*Bloc`) — these stay legacy; this is a scoped
  perf fix, not a migration.
- **Keeping the tap ripple / hover highlight** — intentionally dropped with the Material
  button (resting render unchanged).
- **Restructuring the seat-selection state model** beyond adding the derived `byId` field.

## Further Notes

- This is the standalone **quick win** of ADR 0005, independent of all later phases; Phases 1
  and 2 are contract-independent and can ship separately.
- The geometry-vs-status split this slice relies on already exists in embryo: `CinemaSeat`
  (from the cinema-hall bloc) is the existence/geometry layer, `Seat` (from the seat bloc,
  mutated by SignalR) is the live-status layer. P1 exploits it; P5 makes geometry explicit.
- Numbering is the **client** roadmap's global counter, independent of the .NET services
  roadmap (which has its own `0005`).
- No external issue tracker is reachable (`gh` CLI unavailable); per project rules this PRD is
  saved locally in the slice folder rather than published to a remote tracker, and tagged
  `needs-triage` here in lieu of a tracker label.
