# Seat Selection (Flutter client)

The seat-selection context covers how a cinema hall is presented to a customer for
booking: which seats exist, where they sit, their live status, and how the customer
selects them. It deliberately separates a seat's stable **identity** and **geometry**
from its frequently-changing **status**.

## Language

**SeatId**:
The stable identity of a seat within a hall: `(row, seatNumber)`, both `int`.
Expressed as the Dart record `typedef SeatId = (int row, int seatNumber)`. Booking,
reservation, and shopping-cart logic all key on it; geometry never replaces it.
_Avoid_: seat key, seat index, position.

**CinemaSeat**:
The **existence/geometry** layer of a seat — that a seat with a given `SeatId` exists
in the hall, and (today, implicitly) where it sits. Supplied by `CinemaHallInfoBloc`
as `List<List<CinemaSeat>>`. Carries no live status.
_Avoid_: seat slot, placement (placement is reserved for the future explicit-geometry
type).

**Seat**:
The **dynamic status** layer for a `SeatId` — `blocked`, `hashId`, `seatStatus`.
Supplied by `SeatBloc` as `List<Seat>` and mutated in real time by SignalR. Looked up
by `SeatId`, overlaid on the matching `CinemaSeat` to colour it.
_Avoid_: seat state (that names the bloc state, not the per-seat record).

**Price (live layer)**:
A third per-`SeatId` overlay on the seat map, alongside status — the per-seat amount for
the current session. Owned by BookingManagement, pushed **live** over SignalR (a
manager's coefficient change reprices seats for users already viewing). Looked up by
`SeatId`, like status; must stay O(1) and repaint only affected cells (per slice 0005).
_Avoid_: tariff (that is the zone-level default, not the resolved per-seat price).

**Hall layout / geometry**:
Where each seat physically sits. Today it is *implicit* — derived from array indices
in the `CinemaSeat` grid (`column × 19px`, `row × 22px`). A future phase makes it an
explicit per-seat `SeatPlacement` layer keyed by `SeatId`.
_Avoid_: grid (a grid is only one possible layout).

**SeatPlacement**:
The explicit geometry of one seat: its `SeatId` plus position and size
(`x, y, w, h`, optional rotation/zone) expressed in **layout space**. It is an additive
presentation layer — it never replaces the `SeatId` identity.
_Avoid_: seat coordinate, seat box.

**Layout space**:
The abstract, device-independent coordinate space a hall's geometry is defined in:
logical **seat-pitch units** (1 unit ≈ one nominal seat), origin top-left, y-down.
Owned and persisted by the backend; identical for every client. The contract is
expressed entirely in layout space — it never mentions pixels.
_Avoid_: world space, grid coordinates.

**Canvas space**:
The actual on-screen pixels a given client paints into. The client transforms
**layout space → canvas space** to fit its own viewport (scale, zoom, pan); the backend
never reasons about it. Two clients of different sizes render the same layout-space hall
differently in canvas space.
_Avoid_: screen space (use consistently), device coordinates.

**Screen** (the cinema screen):
The projection wall the seats face — an explicit element in the layout (position and
side/orientation in layout space) that the editor authors and the renderer draws. For a
curved or side-facing hall it determines seat orientation.
_Avoid_: using bare "screen" for a device/canvas — say "canvas" for that.

**Layout bounds / canvas**:
The extent of a hall's layout space — the editor's fixed authoring canvas. Carried
explicitly in the contract (not derived from the seat bounding box) so empty margins and
screen placement are honored. The client fits these bounds into canvas space.
_Avoid_: viewport (that is a canvas-space notion).

**Zone**:
A named grouping of seats with a visual form (e.g. VIP, stalls, balcony). Membership is
an **explicit per-seat attribute** (`seat.zoneId`) authored by the hall designer — it is
**not** derived from geometry. A zone has a visual form (a **polygon** boundary, a
colour, a description) used only for drawing; the polygon never determines membership. A
zone may suggest a **default** price tier, but it does **not** fix a seat's price — the
authoritative price is **per-seat** and owned by BookingManagement (see pricing boundary).
_Avoid_: section, category; when you mean the drawn region say "zone polygon".

## Flagged ambiguities

- **"Seat"** is overloaded between the geometry record (`CinemaSeat`) and the status
  record (`Seat`). They share a `SeatId` but live in different slices
  (`cinema_halls` vs `seats`) and update on different cadences. When unqualified,
  "seat" means the thing the customer sees; name the layer when it matters.
- **"Coordinates"** are ambiguous unless the space is named. Persisted/contract
  coordinates are always **layout space** (abstract units); anything pixel-valued is
  **canvas space** and is per-client and transient. The backend speaks only layout
  space; the client owns the layout→canvas transform.
- **"Screen"** has two meanings: the **cinema screen** (a layout element seats face)
  and the device/**canvas** a client paints into. Reserve "screen" for the cinema
  screen; call the device surface "canvas".
- **Zone membership vs zone polygon.** A seat's zone is the explicit `seat.zoneId`,
  never the polygon it visually sits in. The two are allowed to disagree (a VIP-priced
  seat drawn outside the VIP polygon) — this is the designer's responsibility, optionally
  surfaced by a per-seat zone tint. The editor may warn but never auto-assigns zone from
  geometry (that would let geometry drive price — the forbidden coupling).
- **"Row" is identity, not a shape.** A row groups seats by identity
  (`SeatId.row`); it is **not** necessarily a straight visual line — seats of one row may
  be staggered or split by an aisle. So a **row caption is optional** (the per-seat number
  is the primary label); the contract can anchor a row label anywhere but never requires
  one, and the client may derive one for simple rectangular halls.
- **Pricing boundary.** The hall-layout contract carries **no money**. Price is
  **per-seat**, owned by BookingManagement, computed from a per-hall template modulated by
  per-session / day-type / premiere / dynamic (sold-ratio) coefficients a manager can
  tune. The layout supplies only `SeatId` and `zoneId` (grouping/default); those are the
  cross-context seam BookingManagement keys its prices on. A seat in a zone may still be
  priced individually — zone is a default, not a determinant.

## Example dialogue

> **Dev:** When a SignalR event says seat (row 3, number 12) is now blocked, what
> actually changes on screen?
> **Domain:** Its **status** changes — that's a `Seat` update in `SeatBloc`. The seat's
> **identity** (`SeatId` 3/12) and its place in the hall don't move.
> **Dev:** So the grid of `CinemaSeat`s stays put, and we just recolour the one cell
> whose `SeatId` matches?
> **Domain:** Right. Geometry is static, status is live. Never rebuild the hall to
> recolour a seat.
