# Movie Theater Tickets "**Come and Watch**"

A small demo application for browsing sessions and selecting, reserving and
ordering cinema tickets in real time.

Built with DDD + Clean Architecture on the backend and a Flutter client migrating
toward Vertical-Slice / Hexagonal architecture. Stack: ASP.NET 10, Flutter 3.41 /
Dart 3.9, SignalR, NGINX, Redis, PostgreSQL, Keycloak, RabbitMQ, and Stripe as the
payment system.

## Highlights

- **Interactive seat map.** The auditorium is drawn from explicit geometry on a single
  `CustomPaint` canvas inside a pinch-zoom / drag-pan viewport, so on a phone you can
  zoom in until a seat is large enough to tap accurately — and a tap still resolves to
  the correct seat at any zoom. Seat numbers stay crisp when zoomed in.
- **Real-time seat status.** Seat availability updates live over SignalR — a seat
  blocked, reserved, sold or released by another customer recolours immediately, with
  no reload.
- **Shopping cart & reservation countdown.** Selected seats are held in a cart with a
  live reservation timer.
- **Arbitrary hall layouts.** Geometry is coordinate-based (per-seat position, size,
  rotation), so non-rectangular halls — gaps, staggered rows, curved/angled seating —
  are supported; legacy rectangular halls render unchanged.

## Documentation

General business and custom requirements that we may receive from businesses
[Business Requirement](docs/BusinessRequirement.md)

General architectural vision of the system
[IT Architecture Vision](docs/ITArchitectureVision.md)

Detailed application architecture
[Movie Theater Tickets Architecture](docs/MovieTheaterTicketsArchitecture.md)

[Deployment](docs/Deployment.md)

## Project status & roadmap

The project is under active development and delivered slice-by-slice. Recent client
work reworked the seat-selection screen for performance (an O(1) live status index)
and then re-rendered it as a coordinate-driven, zoomable seat map (see ADR 0005). A
dependency/architecture migration of the Flutter client is tracked as roadmap slice
0001.

>### DISCLAIMER
>
>The application is in the process of being written, code will be added as soon as it is ready
>
