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

**Hall layout / geometry**:
Where each seat physically sits. Today it is *implicit* — derived from array indices
in the `CinemaSeat` grid (`column × 19px`, `row × 22px`). A future phase makes it an
explicit per-seat `(x, y, …)` `SeatPlacement` layer keyed by `SeatId`.
_Avoid_: grid (a grid is only one possible layout).

## Flagged ambiguities

- **"Seat"** is overloaded between the geometry record (`CinemaSeat`) and the status
  record (`Seat`). They share a `SeatId` but live in different slices
  (`cinema_halls` vs `seats`) and update on different cadences. When unqualified,
  "seat" means the thing the customer sees; name the layer when it matters.

## Example dialogue

> **Dev:** When a SignalR event says seat (row 3, number 12) is now blocked, what
> actually changes on screen?
> **Domain:** Its **status** changes — that's a `Seat` update in `SeatBloc`. The seat's
> **identity** (`SeatId` 3/12) and its place in the hall don't move.
> **Dev:** So the grid of `CinemaSeat`s stays put, and we just recolour the one cell
> whose `SeatId` matches?
> **Domain:** Right. Geometry is static, status is live. Never rebuild the hall to
> recolour a seat.
