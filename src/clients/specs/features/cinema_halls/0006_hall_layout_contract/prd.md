# PRD — Hall-layout contract: `SeatLayout` client model + bootstrap adapter (ADR 0005 / P2)

- **Slice:** `0006_hall_layout_contract`
- **Feature:** cinema_halls
- **Type:** New net-additive client data slice on legacy code (introduces the geometry
  contract; no UI change, no behaviour change to the existing grid)
- **Source:** `.claude/decisions/0005_seat_map_rendering.md` "§6. Hall-layout contract —
  pinned details" + "Phase 2 — P2" (design pinned via `/grill-with-docs`, 2026-06-04)
- **Status:** Planned

## Problem Statement

Today a cinema hall has **no explicit geometry**. Where each seat sits is *implicit* —
derived from array indices in the `CinemaSeat` grid (`column × 19px`, `row × 22px`), and
the model `List<List<CinemaSeat>>` can only ever express a rectangular grid. There is
nowhere to put variable seats-per-row, gaps, curved rows, staggered (checkerboard)
arrangements, zones, or a cinema **screen** that the seats face. The pixel offsets are
baked into the widget, so two clients of different sizes cannot render the same hall to fit
their own viewport.

This blocks everything in Phase 2/3 of ADR 0005: the free-form `CustomPaint` renderer (P5)
has nothing to consume, and the future admin "draw a hall" editor (P6) has nothing to
author into. The renderer and the editor are both **downstream of a contract that does not
yet exist**. Until that contract is pinned as a concrete, consumable shape — and until the
client can obtain one even while the backend still only stores *rows × seats-per-row* — no
free-form work can start.

The constraint that makes this safe (and the reason it can ship before any backend change):
a seat's domain **identity is `SeatId` = `(row, seatNumber)`**, and booking, reservation,
shopping-cart, and the modular backend all key on it. Geometry must therefore be an
**additive presentation layer on top of the stable `SeatId`**, never a replacement for it.

## Solution

Introduce the **hall-layout contract** as a concrete client-side shape, and a temporary way
to obtain one, without changing anything the moviegoer sees yet.

From each downstream consumer's perspective:

- **The P5 renderer** can ask for a hall's geometry as a single `SeatLayout` — a flat list
  of `SeatPlacement`s in **layout space** (device-independent seat-pitch units), plus the
  hall's `bounds`, its `screen`, and its `zones` — and draw the hall from coordinates alone,
  with full geometric freedom.
- **The future editor (P6)** has a target shape to author into: explicit per-seat
  coordinates, explicit zone membership, an explicit authoring canvas.
- **The client today** keeps working unchanged: the existing index-driven grid still renders
  off `CinemaSeat`. In parallel, a **temporary client-side adapter** synthesizes a
  `SeatLayout` from the current `List<List<CinemaSeat>>` using a legacy default-geometry
  mapping, so the new shape is real and consumable **while the backend is still mocked**.
  When the backend later serves real `SeatLayout` over `GET …/halls/{id}/layout`, the
  adapter is **deleted** and the client consumes the backend shape directly — the permanent
  owner of geometry stays the backend.

Crucially this slice ships **no visible change**: it is the data contract and a bootstrap
source, parallel to the live grid. Seat **identity stays `(row, seatNumber)`**, so booking,
reservation, shopping-cart, status (SignalR), and price are all untouched.

## User Stories

1. As the P5 free-form renderer, I want to obtain a hall's geometry as a single
   `SeatLayout`, so that I can draw the whole hall from coordinates without knowing how it
   was stored.
2. As the P5 renderer, I want every seat as a flat `SeatPlacement` with its own `(x, y)`, so
   that variable seats-per-row, gaps, stagger, and curves are just different coordinates and
   need no special structural cases.
3. As the P5 renderer, I want geometry in **layout space** (seat-pitch units, origin
   top-left, y-down), so that I can apply my own layout→canvas transform to fit my viewport
   and zoom/pan independently of any other client.
4. As the P5 renderer, I want each `SeatPlacement` keyed by `SeatId = (row, seatNumber)`, so
   that I can overlay live status and price by the same key the rest of the app already uses.
5. As the P5 renderer, I want explicit `bounds` for the hall (not derived from the seat
   bounding box), so that empty margins and screen placement are honored when I fit the hall
   to canvas.
6. As the P5 renderer, I want an explicit `screen` element (position + side/orientation in
   layout space), so that I can draw the cinema screen and orient seats toward it in curved
   or side-facing halls.
7. As the P5 renderer, I want `zones` as draw-only regions (polygon + colour + label), so
   that I can paint a VIP/stalls/balcony region behind the seats in the correct render order
   (background → zone polygons → seats on top).
8. As the P5 renderer, I want a seat's zone to come from its **explicit `zoneId`**, never
   from the polygon it visually sits in, so that geometry never silently drives membership.
9. As the P5 renderer, I want default geometry to exist for legacy halls, so that the three
   seeded halls (Red, Black, White) still render the instant P5 ships, with no backend work.
10. As the moviegoer, I want the seat screen to look and behave **exactly as today** while
    this slice lands, so that introducing the contract changes nothing I can see.
11. As the moviegoer, I want a legacy rectangular hall, once expressed as a `SeatLayout`, to
    reproduce today's grid **1:1**, so that the migration to coordinates is visually
    invisible.
12. As the future hall designer (P6 editor), I want a target shape with per-seat coordinates,
    sizes, optional rotation, and explicit zone membership, so that I have something concrete
    to author a hand-drawn hall into.
13. As the future hall designer, I want row to be an **identity/label attribute, not a
    structural container**, so that I can stagger a row, split it by an aisle, or curve it
    without breaking seat identity.
14. As the future hall designer, I want row captions to be **optional** (the per-seat number
    is the primary label), so that non-rectangular halls are not forced into a straight-line
    row model.
15. As BookingManagement (pricing context), I want the layout contract to carry **no money**
    and to expose `SeatId` and `zoneId` as the only seam, so that I can key per-seat prices
    and zone-default tiers on it without the geometry context owning price.
16. As the live `PriceBloc` (downstream sibling slice), I want geometry to be the single
    source of truth for *which seats exist*, keyed by `SeatId`, so that I can overlay live
    price as a third layer by the same key, ignoring any price for an unknown `SeatId`.
17. As a developer, I want a pure, Flutter-free **synthesizer** that maps the legacy
    `List<List<CinemaSeat>>` to a `SeatLayout`, so that the default-geometry rule is one
    testable seam rather than logic buried in a widget.
18. As a developer, I want the legacy mapping to be exact (`x = columnIndex`,
    `y = rowIndex`, `w = h = 1`, `rotation = 0`, `zoneId = null`, uniform pitch, screen at
    top spanning the width, `bounds` = seat bbox + margins), so that "reproduces today's grid"
    is verifiable, not aspirational.
19. As a developer, I want a single `SeatLayoutSource` port shaped as
    `GET …/halls/{id}/layout → SeatLayout`, so that the client has **one** path to geometry
    and no parallel legacy/new endpoints leak into consumers.
20. As a developer, I want the bootstrap adapter that fakes this endpoint to be **explicit
    throwaway scaffolding**, so that when the backend serves real layouts the deletion site
    is obvious and the client returns to a single path.
21. As a developer, I want the `SeatLayout` model and the synthesizer unit-tested in
    isolation, so that the contract's invariants are locked before any renderer depends on
    them.
22. As a developer, I want status/price absence handled defensively at the contract level —
    a status/price for an unknown `SeatId` is ignored; a seat with no price simply shows none
    and stays selectable — so that the three-feed overlay model is consistent from the start.
23. As a maintainer, I want this tracked as its own slice that introduces the contract and
    bootstrap only (no renderer, no editor, no backend change), so that P5/P6 and the backend
    work all branch cleanly off a pinned, shipped shape.
24. As a maintainer, I want the contract to **collapse old P3 and P4** (variable rows + store
    geometry) into one flat geometry shape, so that there is no intermediate "variable rows,
    no geometry" contract version to maintain.
25. As a maintainer, I want this slice to **not migrate** the legacy `cinema_halls`/`seats`
    code to the target stack, so that introducing the new shape stays a narrow, reviewable
    additive step.

## Implementation Decisions

**Coordinate model — abstract layout space.** Geometry is expressed in logical
**seat-pitch units** (1 unit ≈ one nominal seat), origin top-left, y-down. The contract
**never mentions pixels**. The backend owns and returns layout-space geometry; the **client
only transforms layout → canvas** (that transform is P5's concern, not this slice's). Two
clients of different sizes render the same hall differently in canvas space.

**Payload shape — flat list of `SeatPlacement`.** `SeatLayout` carries
`{ hallId, bounds, screen, zones[], seats[] }`, where each seat is
`{ row, number, x, y, w?, h?, rotation?, zoneId? }`. `row` is an **identity/label**
attribute, not a structural container, so variable seats-per-row, gaps, stagger, and curves
are merely different entries/coordinates. `w,h` default `1×1`; `rotation` defaults `0`.
Each seat is keyed by `SeatId = (row, seatNumber)` — the **same** record type slice 0005
introduced (`typedef SeatId = (int row, int seatNumber)`), inherited unchanged.

**Chrome carried by the contract.** Beyond seats:
- `bounds` — the layout-space canvas extent (the editor's authoring canvas), **explicit**,
  not derived from the seat bbox, so margins and screen placement are honored.
- `screen` — the **cinema screen** (position + side/orientation in layout space);
  load-bearing for curved/side-facing halls and seat orientation.
- `zones[]` — each `{ id, label, colour, polygon:[points] }`: a named grouping **plus** a
  draw-only visual region.
- Row labels are **optional**; the per-seat number is the primary label; the client may
  derive a row caption for simple rectangular halls.

**Zones — membership vs polygon.** A seat's zone is the **explicit per-seat `zoneId`**,
authored by the designer; it is **never** derived from the polygon. The polygon/colour are
**draw-only**. Render order is canvas background → zone polygons → seats on top. Membership
and polygon are allowed to disagree (a VIP-priced seat drawn outside the VIP polygon); the
contract models this without resolving it (the future editor may warn, never auto-assigns).

**Pricing boundary (cross-context).** The contract carries **no money**. Price is per-seat,
owned by **BookingManagement**; `SeatId`/`zoneId` are the only seam it keys on. A zone gives
a *default* tier, not a determinant. Price arrives as a **separate third live layer**
(a future `PriceBloc`, pushed over SignalR) — explicitly out of this slice, but the contract
is shaped so it slots in by `SeatId` without change.

**Three-feed consistency rule (baked into the contract's meaning).** Geometry is the single
source of truth for *which seats exist*. Status (Seats service) and price (BookingManagement)
are overlays keyed by `SeatId`: an overlay value for an unknown `SeatId` is **ignored**
(defensive, like slice 0005's index miss); a seat with no status renders at baseline; a seat
with no price shows none (or the zone default) and stays selectable — absence of price is
display-only, never an error or a block.

**Legacy default geometry (grid → layout space).** For a current `List<List<CinemaSeat>>`:
`x = columnIndex`, `y = rowIndex`, `w = h = 1`, `rotation = 0`, `zoneId = null`, uniform
pitch; `screen` at top spanning the width; `bounds` = seat bbox + margins. This reproduces
today's grid **1:1** and is the single rule the synthesizer implements.

**Migration / bootstrap strategy.** Build the client against the **new** `SeatLayout` shape
now, while the backend is mocked: a **temporary client-side adapter** synthesizes a
`SeatLayout` from the current cinema-hall data via the legacy mapping above. The adapter is
explicit throwaway scaffolding (per `CLAUDE.md` migration rules). Once the backend serves
real `SeatLayout`, the adapter is **deleted** and the client consumes the new shape directly;
the permanent owner of geometry — including default geometry for legacy halls — remains the
**backend**.

**Endpoint.** A single `SeatLayoutSource` port shaped as `GET …/halls/{id}/layout →
SeatLayout`; no parallel legacy/new endpoints. During bootstrap the temporary adapter fakes
this response from existing hall data.

**Modules (deep, isolatable).**
- **`SeatLayout` model + parts** (`SeatPlacement`, `Zone`, `Screen`, `LayoutBounds`) —
  immutable layout-space value types keyed by `SeatId`; the contract shape. Net-new code, so
  it follows the target's immutability conventions (it does **not** retrofit the surrounding
  legacy `cinema_halls` entities). Exact serialization/value-type mechanics are a `plan.md`
  decision.
- **Legacy-geometry synthesizer** — the deep, Flutter-free module: a pure
  `List<List<CinemaSeat>> (+ hall id/description) → SeatLayout` mapping implementing the
  legacy rule above. The single testable seam for "reproduces today's grid 1:1".
- **`SeatLayoutSource` port + temporary bootstrap adapter** — the one path to a hall's
  geometry; the adapter wraps the synthesizer over existing cinema-hall data and is marked
  throwaway.

**No domain/contract change to live behaviour, no new runtime dependency.** Seat identity
stays `(row, seatNumber)`. The existing grid, booking, reservation, shopping-cart, SignalR
status, and the backend are untouched. `cinema_halls`/`seats` stay **legacy** (`*Repo`,
`intl`, `Bloc`, manual `get_it`); this slice adds the new shape alongside them and
deliberately does **not** migrate them. Any new package needed for serialization is raised
with the user before being added (per `CLAUDE.md`).

## Testing Decisions

**What a good test is here.** Tests assert externally observable contract behaviour — what
`SeatLayout` a given hall maps to, that identity and geometry come out correct, that the
defaults and chrome are right — never the internal field plumbing or serialization codegen.
Because this slice's headline promise is "the legacy grid reproduces 1:1", the tests are
primarily **mapping-parity** tests on the synthesizer.

**Deep-module unit tests (the synthesizer — the slice's centre of gravity).** The pure
`List<List<CinemaSeat>> → SeatLayout` mapping is unit-tested exhaustively:
- a rectangular hall maps to the expected flat `SeatPlacement` list with
  `x = columnIndex`, `y = rowIndex`, `w = h = 1`, `rotation = 0`, `zoneId = null`;
- `SeatId` identity is preserved for every seat (`(row, seatNumber)` round-trips);
- `bounds` equals the seat bbox plus the defined margins (explicit, not just the bbox);
- `screen` is placed at top spanning the width;
- a ragged/variable-length inner list (defensive) and an **empty** hall map sanely;
- the three seeded shapes (Red 28×22, Black 21×18, White 15×12) produce the expected seat
  counts and extents — the concrete "1:1" lock.

**Model invariant tests.** A handful of value-type tests: `SeatPlacement` keys by `SeatId`;
`w/h`/`rotation` defaults apply; zone membership reads from `zoneId` and is independent of
any polygon; equality/identity behave as expected for the value types.

**Source/adapter tests.** The temporary `SeatLayoutSource` adapter: given a hall id it
returns a `SeatLayout` (delegating to the synthesizer), and — per the `CLAUDE.md` adapter
rule — an unexpected failure is caught in a catch-all with logging rather than thrown raw.
This mirrors how the existing `*Repo` adapters are covered.

**No cubit/widget/outside-in test in this slice.** There is **no new bloc, no UI, and no
user-visible behaviour** here — the live grid is unchanged and nothing new is rendered — so
the default Cubit and Widget layers do **not** apply (the same carve-out slice 0005 made for
its absent adapter/use-case layers, inverted). The acceptance gate for *visible* free-form
rendering belongs to **P5**, which consumes this contract; this slice's gate is the
synthesizer's parity unit tests. If `plan.md` finds a thin use-case worth extracting around
the source, it gets the standard use-case unit tests.

**Prior art.** Slice 0005's extracted pure `byId` index seam and its widget-free unit tests
are the closest analogue (a pure derivation tested without pumping widgets). Existing
`cinema_halls` `*Repo` adapter tests are the pattern for the source/adapter coverage. Use
`mocktail` per project conventions.

## Out of Scope

Deferred to later ADR-0005 phases or deliberately not done in this slice:

- **The free-form renderer** (`CustomPaint` + `InteractiveViewer`, the layout→canvas
  transform, manual hit-testing, drawing seats/zones/screen) — that is **P5**, which consumes
  this contract. No pixels, no painting, no canvas-space code here.
- **The live `PriceBloc`** (per-`SeatId` price pushed over SignalR from BookingManagement) —
  a sibling downstream slice; the contract is shaped to admit it but does not build it.
- **The admin hall editor** (P6a–c: authoring canvas, add/move seats, save/validate) — this
  slice only provides the target shape it will author into.
- **Any backend change** — variable seats-per-row storage, persisted per-seat geometry, the
  real `GET …/halls/{id}/layout` endpoint. The backend stays mocked; this slice fakes the
  response with the temporary adapter. (The backend later **deletes** the adapter by serving
  real layouts; old P3/P4 survive only as optional backend rollout sub-steps.)
- **Zoom/pan, zone rendering, seat orientation, curved/staggered drawing** — these are
  *expressible* in the contract but *rendered* by P5.
- **Migrating `cinema_halls`/`seats` to the target stack** (ports/adapters everywhere,
  `slang`, `retrofit`, `injectable`, renaming `*Repo`/`*Bloc`) — they stay legacy; this is an
  additive contract slice, not a migration.
- **Wiring real money/pricing** into the layout — the contract carries none by design.

## Further Notes

- This slice operationalizes ADR 0005 **§6 "Hall-layout contract — pinned details"** on the
  client. The *design* is already pinned (2026-06-04, `/grill-with-docs`); this slice turns it
  into a concrete, consumable client shape plus a bootstrap source — the **linchpin** both the
  P5 renderer and the future editor depend on.
- It **collapses old P3 and P4**: because geometry is explicit and the payload is flat, there
  is no separate "variable rows, no geometry" contract version. The legacy halls get
  synthesized geometry directly.
- Numbering is the **client** roadmap's global counter (next after 0005), independent of the
  .NET services roadmap (which has its own `0006`). Placed under the `cinema_halls` feature
  because geometry is the existence layer (`CinemaSeat`/`CinemaHallInfo`), per `CONTEXT.md`.
- The vocabulary used throughout (`SeatId`, layout space, canvas space, `SeatPlacement`,
  `SeatLayout`, zone / zone polygon, screen, layout bounds, price layer) is defined in
  `CONTEXT.md`; this PRD uses it deliberately.
- No external issue tracker is reachable (`gh` CLI unavailable; only an `https` origin remote
  exists). Per project rules this PRD is saved locally in the slice folder rather than
  published to a remote tracker, and tagged `needs-triage` here in lieu of a tracker label.
