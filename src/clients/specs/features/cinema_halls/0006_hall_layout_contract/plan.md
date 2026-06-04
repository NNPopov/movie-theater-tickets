# Feature Spec / Plan — `0006_hall_layout_contract`

> Phase-2 ("P2") of ADR 0005 — introduces the **hall-layout geometry contract** on the
> client: a concrete, consumable `SeatLayout` value type plus a **temporary bootstrap source**
> that synthesizes one from the legacy `List<List<CinemaSeat>>` grid while the backend is still
> mocked. **Net-additive on legacy code:** no UI, no new bloc, no user-visible change, no
> backend change. Seat identity stays `SeatId = (row, seatNumber)`. Source of truth for intent:
> `prd.md` in this folder; design pinned in `.claude/decisions/0005_seat_map_rendering.md`
> §6 + Phase-2.

---

## 1. Header

Introduce the **hall-layout contract** as a concrete client shape and a throwaway way to
obtain one, **changing nothing the moviegoer sees**. Three deep, isolatable modules:

1. **`SeatLayout` model + parts** (`SeatPlacement`, `Zone`, `Screen`, `LayoutBounds`,
   `LayoutPoint`) — immutable **layout-space** value types, keyed per seat by
   `SeatId = (row, seatNumber)`. This is the contract the P5 renderer will consume and the P6
   editor will author into. Net-new code, so it uses the **target** immutability convention
   (`freezed` + `json_serializable`, exactly like `movies`/`movie_sessions`); it does **not**
   retrofit the surrounding legacy `cinema_halls` entities.
2. **Legacy-geometry synthesizer** — the centre of gravity: a pure, **Flutter-free**
   top-level function `List<List<CinemaSeat>> (+ hall id/description) → SeatLayout` implementing
   the one legacy default-geometry rule. The single testable seam behind the headline promise
   "reproduces today's grid **1:1**".
3. **`SeatLayoutSource` port + temporary bootstrap adapter** — the **one** path to a hall's
   geometry, shaped as `GET …/halls/{id}/layout → SeatLayout`. The adapter wraps the
   synthesizer over existing cinema-hall data and is **explicit throwaway scaffolding**: when
   the backend serves real `SeatLayout`, the adapter is **deleted** and the client deserializes
   the backend JSON through the same port (`SeatLayout.fromJson` already exists).

This is **additive on a legacy feature** (`cinema_halls`/`seats` stay `*Repo`, `intl`, `Bloc`,
manual `get_it`). Per `CLAUDE.md` we do **not** migrate them. The *new* contract code follows
target conventions because it is net-new and downstream P5/P6/backend all branch off it.

**UX result:** none. The existing index-driven grid still renders off `CinemaSeat`; booking,
reservation, shopping-cart, SignalR status and price are all untouched. The acceptance gate for
*visible* free-form rendering belongs to **P5**; this slice's gate is the synthesizer's
**mapping-parity unit tests**.

---

## 2. Context — what to read, what not to read

### READ
- `@CLAUDE.md` — fully (migration rules + "new code follows target fully" both apply here).
- `@specs/features/cinema_halls/0006_hall_layout_contract/prd.md` — the intent.
- `@.claude/decisions/0005_seat_map_rendering.md` — §6 "Hall-layout contract — pinned details"
  and "Phase 2 — P2" (the pinned design this slice operationalizes).
- `@CONTEXT.md` — the vocabulary (`SeatId`, layout space, canvas space, `SeatPlacement`,
  `SeatLayout`, zone / zone polygon, screen, layout bounds, price layer).
- Legacy types this slice maps **from** (read for shape, do **not** change):
  - `@lib/src/cinema_halls/domain/entity/cinema_hall_info.dart` —
    `CinemaHallInfo(String id, String description, List<List<CinemaSeat>> cinemaSeat)`.
  - `@lib/src/cinema_halls/domain/entity/cinema_seat.dart` —
    `CinemaSeat({required int row, required int seatNumber})`.
  - `@lib/src/cinema_halls/domain/repo/cinema_hall_repo.dart` and
    `@lib/src/cinema_halls/data/repo/cinema_hall_repo_impl.dart` — `getCinemaHallInfoById` is
    what the bootstrap adapter delegates to for hall data; mirror its `try/catch → Left(Failure)`
    style.
- The seat-identity type to **inherit unchanged**:
  - `@lib/src/seats/domain/entities/seat_id.dart` — `typedef SeatId = (int row, int seatNumber);`.
- Net-new conventions to mirror (read as patterns, not to change):
  - `@lib/src/movies/domain/entities/movie.dart` — the `@freezed` + `fromJson` shape this
    project already uses for value types.
  - `@lib/src/seats/domain/seat_index.dart` — slice 0005's pure, widget-free seam; the closest
    analogue for "a pure derivation tested without pumping widgets".
  - `@lib/core/utils/typedefs.dart` (`ResultFuture<T>`), `@lib/core/errors/failures.dart`
    (`ServerFailure`), `@lib/core/common/app_logger.dart` (`getLogger`, `AppLogger.e(...)`).
- `@lib/injection_container.dart` — `_initCinemaHall()` is where the new port is registered.
- `@.claude/skills/bloc/SKILL.md` and `@agent_docs/testing.md` (read at test-writing time;
  note: this slice has **no** bloc/widget tests — see §6).

### DO NOT READ
- The seat-map widgets and blocs (`seats/presentation/**`, `seats/presentation/cubit/**`,
  `cinema_halls/presentation/**`) — there is **no UI/bloc change**; reading them invites scope
  creep into P5.
- `shopping_carts/**`, `movie_sessions/**`, `movies/**` internals (beyond the `Movie` freezed
  pattern noted above), the SignalR hub, pricing — all out of scope.
- Generated files: `*.gr.dart`, `*.g.dart`, `*.freezed.dart`, `app_localizations*.dart`.

---

## 3. "API" — the contract shape and the one legacy mapping rule

**No live backend call in this slice.** The eventual endpoint is
`GET …/halls/{id}/layout → SeatLayout` (backend JSON, layout-space). This slice **fakes** that
response by synthesizing a `SeatLayout` from existing hall data; the port is shaped for the
real endpoint so consumers never see the swap.

### 3.1 The contract — `SeatLayout` (layout space; no pixels, no money)

Layout space = logical **seat-pitch units** (1 unit ≈ one nominal seat), origin **top-left**,
**y-down**. The contract **never mentions pixels** and **never carries money**.

```
SeatLayout {
  String        hallId,
  LayoutBounds  bounds,      // explicit canvas extent — NOT the seat bbox
  Screen        screen,      // the cinema screen seats face
  List<Zone>    zones,       // draw-only regions (may be empty)
  List<SeatPlacement> seats, // flat list — the single source of "which seats exist"
}

SeatPlacement {
  int      row,              // identity/label, NOT a structural container
  int      number,          // per-seat number — the primary label
  double   x, double y,      // top-left of the seat cell, in layout space
  double   w = 1, double h = 1,
  double   rotation = 0,     // degrees, optional
  String?  zoneId,           // explicit membership; NEVER derived from a polygon
  // derived, NOT serialized:
  SeatId get seatId => (row, number);
}

LayoutBounds { double x, double y, double width, double height }  // top-left + size
LayoutPoint  { double x, double y }
Zone   { String id, String label, String colour, List<LayoutPoint> polygon }  // colour = hex, draw-only
Screen { ScreenSide side, LayoutPoint start, LayoutPoint end }   // a segment + which edge
enum ScreenSide { top, bottom, left, right }
```

Pinned semantics (from the PRD, locked here):
- **Identity** is `SeatId = (row, number)` — the **same** record type from slice 0005,
  inherited unchanged. Status (Seats service) and price (BookingManagement) are future overlays
  keyed by it: an overlay value for an **unknown `SeatId` is ignored**; a seat with no
  status/price renders at baseline and stays selectable (absence of price is display-only,
  never an error or a block). Geometry is the **single source of truth for which seats exist**.
- **`row` is a label, not a container** → variable seats-per-row, gaps, stagger and curves are
  just different `(x, y)` entries; the contract needs no structural special cases.
- **`bounds` is explicit** (authoring canvas), not the seat bounding box, so margins and screen
  placement are honored when a client fits the hall to its viewport.
- **Zones:** membership = `SeatPlacement.zoneId` (authored), **never** the polygon. `polygon`
  + `colour` are **draw-only**; render order is canvas background → zone polygons → seats on
  top. Membership and polygon are allowed to disagree; the contract models this without
  resolving it.
- **No `package:flutter` in these types** (domain hard rule): `colour` is a **hex string**
  (e.g. `"#9C27B0"`), not a Flutter `Color`; P5 parses it. `rotation` is `double` degrees.

### 3.2 The one legacy default-geometry rule (what the synthesizer implements)

Given a `CinemaHallInfo(id, description, cinemaSeat)` where `cinemaSeat` is
`List<List<CinemaSeat>>` (outer = rows, inner = seats in that row):

| Output | Rule |
|---|---|
| one `SeatPlacement` per `CinemaSeat` | `row = seat.row`, `number = seat.seatNumber` |
| `x` | `columnIndex` (0-based position **within its inner row list**) |
| `y` | `rowIndex` (0-based position of the inner list in the outer list) |
| `w`, `h` | `1`, `1` |
| `rotation` | `0` |
| `zoneId` | `null` |
| `zones` | `[]` (empty) |
| `hallId` | `hall.id` |

Let `R` = number of rows, `C` = max seats across rows (ragged-safe; each seat keeps its own
column index). Layout margin constant `m = 1.0` (one seat pitch). Then:

- **Screen** (top, spanning the seat width): `side = ScreenSide.top`,
  `start = (0, -m)`, `end = (C, -m)`.
- **`bounds`** (seat bbox **+** screen **+** uniform margin), exact:
  `x = -m`, `y = -2m`, `width = C + 2m`, `height = R + 3m`.
  (Seat bbox is `0..C` × `0..R` because a 1×1 seat at `(x,y)` occupies `[x,x+1]×[y,y+1]`; the
  screen line sits at `y = -m`; one margin `m` surrounds the whole thing.)

These constants and formulas are the **locked "1:1" definition** — the synthesizer tests assert
them verbatim. Defensive edges: an **empty** hall (`cinemaSeat == []`) → `seats: []`,
`C = 0`, `R = 0`, bounds/screen computed from those; a **ragged** inner list maps each seat by
its own indices without padding.

---

## 4. Target structure (files created / touched)

New contract code lives under the **legacy `lib/src/cinema_halls/` tree** (matching slice 0005,
which placed its net-new `SeatId`/`seat_index.dart` in the legacy `lib/src/seats/` tree rather
than a parallel `lib/features/` slice). Inside that tree the net-new code uses **target
vocabulary** — `domain/layout/` value types and a `domain/ports/` port — because it is net-new
and the PRD mandates a "port", never a `*Repo`/`*Repository`.

```
lib/src/cinema_halls/
├── domain/
│   ├── entity/                              # (unchanged) CinemaHallInfo, CinemaSeat, …
│   ├── layout/                              # NEW: the geometry contract (freezed value types)
│   │   ├── seat_layout.dart                 #   @freezed SeatLayout {hallId, bounds, screen, zones, seats}
│   │   ├── seat_placement.dart              #   @freezed SeatPlacement {row, number, x, y, w, h, rotation, zoneId} + SeatId getter
│   │   ├── zone.dart                        #   @freezed Zone {id, label, colour, polygon}
│   │   ├── layout_screen.dart               #   @freezed Screen {side, start, end} + enum ScreenSide
│   │   ├── layout_bounds.dart               #   @freezed LayoutBounds {x, y, width, height}
│   │   └── layout_point.dart                #   @freezed LayoutPoint {x, y}
│   └── ports/
│       └── seat_layout_source.dart          # NEW: abstract SeatLayoutSource { ResultFuture<SeatLayout> getLayout(String hallId); }
└── data/
    └── layout/
        ├── legacy_seat_layout_synthesizer.dart   # NEW pure, Flutter-free: SeatLayout synthesizeLegacyLayout(CinemaHallInfo)
        └── bootstrap_seat_layout_source.dart      # NEW THROWAWAY adapter implements SeatLayoutSource (wraps the synthesizer)
```

Plus one **wiring** change:

```
lib/injection_container.dart                  # MODIFIED: register SeatLayoutSource in _initCinemaHall()
```

Notes:
- **`SeatId` is imported from `lib/src/seats/domain/entities/seat_id.dart`, not duplicated.**
  The PRD requires "the same record type … inherited unchanged"; a second `typedef` would be a
  distinct symbol (even if structurally compatible) and split the canonical id. In the legacy
  tree, `seats ↔ cinema_halls` already cross-reference (the seat grid widget consumes
  `cinema_halls`' `CinemaSeat`); reusing `SeatId` here is the same **pre-existing, accepted**
  legacy cross-reference, not a target-slice "feature importing another feature" violation.
  *(Rejected alternative: re-declare `SeatId` in `cinema_halls` — duplicates the contract.)*
- **No new `pubspec.yaml` dependency.** `freezed`, `freezed_annotation`, `json_serializable`,
  `json_annotation`, `logger`, `dartz` are all already present. (Per `CLAUDE.md`, no dependency
  is added without asking — none is needed.)
- **No `_shared/` move, no `lib/features/` slice, no `core/` logic change** beyond the DI
  registration line. **No legacy migration** of `cinema_halls`/`seats`.

---

## 5. What to do — step by step

### Step 1 — Inherit `SeatId` (no new file)
Import `SeatId` from `lib/src/seats/domain/entities/seat_id.dart` wherever a seat key is needed
(only `SeatPlacement` exposes it, as a getter). Do **not** redeclare it.

### Step 2 — Value types (`domain/layout/*.dart`), `@freezed` + JSON
Create each value type as a `freezed` class mirroring `movie.dart` (`@freezed abstract class X
with _$X`, `factory X(...) = _X;`, `factory X.fromJson(...) => _$XFromJson(...)`, with
`part 'x.freezed.dart';` and `part 'x.g.dart';`). JSON is forward-useful: it is exactly what the
real backend endpoint will return, so deleting the adapter later needs **no** model change.

- `LayoutPoint { double x; double y; }`
- `LayoutBounds { double x; double y; double width; double height; }`
- `enum ScreenSide { top, bottom, left, right }` (json_serializable serializes enums by name)
  and `Screen { ScreenSide side; LayoutPoint start; LayoutPoint end; }` in `layout_screen.dart`.
- `Zone { String id; String label; String colour; List<LayoutPoint> polygon; }`
  — `colour` is a **hex string**, draw-only; `polygon` may be empty.
- `SeatPlacement` — fields `int row, int number, double x, double y,
  @Default(1.0) double w, @Default(1.0) double h, @Default(0.0) double rotation,
  String? zoneId`. Add a **private constructor** `const SeatPlacement._();` so a custom getter
  is allowed, then `SeatId get seatId => (row, number);`. The getter is **derived, not a JSON
  field** (sidesteps record-type serialization entirely).
- `SeatLayout { String hallId; LayoutBounds bounds; Screen screen;
  @Default(<Zone>[]) List<Zone> zones; required List<SeatPlacement> seats; }`.

**CRITICAL — domain stays Flutter-free.** None of these files may import `package:flutter/*`
or `package:dio/*`. `colour` is a string and `rotation`/coordinates are `double`; there is no
Flutter `Color`, `Offset`, or `Rect` in the contract. (P5 converts layout → canvas and parses
the hex colour.)

### Step 3 — The synthesizer (`data/layout/legacy_seat_layout_synthesizer.dart`)
A single pure, **Flutter-free** top-level function — the slice's centre of gravity:

```dart
SeatLayout synthesizeLegacyLayout(CinemaHallInfo hall) { … }
```

Implement exactly the rule in §3.2. Pin the magic numbers as named top-level constants in this
file so the tests reference them, not literals:

```dart
const double kLegacyLayoutMargin = 1.0; // one seat-pitch margin around the hall + screen
```

Behaviour:
- Walk `hall.cinemaSeat` with both indices; emit one `SeatPlacement` per `CinemaSeat`
  (`x = columnIndex`, `y = rowIndex`, `w = h = 1`, `rotation = 0`, `zoneId = null`,
  `row`/`number` from the seat).
- Compute `R = rows`, `C = max inner length (0 if empty)`.
- Build `screen` (top, `start (0,-m)`, `end (C,-m)`) and `bounds`
  (`x:-m, y:-2m, width:C+2m, height:R+3m`).
- `zones: const []`, `hallId: hall.id`.
- No I/O, no `getIt`, no logging here — it is a pure mapping. (Its purity is what makes the
  "1:1" promise a one-line testable seam.)

### Step 4 — The port (`domain/ports/seat_layout_source.dart`)
```dart
abstract class SeatLayoutSource {
  const SeatLayoutSource();
  ResultFuture<SeatLayout> getLayout(String hallId); // GET …/halls/{id}/layout → SeatLayout
}
```
`ResultFuture<SeatLayout>` from `core/utils/typedefs.dart`. **One** path to geometry — no
parallel legacy/new endpoints leak to consumers.

### Step 5 — The throwaway bootstrap adapter (`data/layout/bootstrap_seat_layout_source.dart`)
Wraps the synthesizer over existing hall data. **Mark it loudly as throwaway scaffolding** with
a file-top doc comment naming the deletion trigger (backend serving real `SeatLayout`).

```dart
/// TEMPORARY BOOTSTRAP — delete when the backend serves GET …/halls/{id}/layout.
/// Fakes that endpoint by synthesizing a SeatLayout from the legacy hall grid.
class BootstrapSeatLayoutSource implements SeatLayoutSource {
  BootstrapSeatLayoutSource(this._halls, {AppLogger? logger})
      : _logger = logger ?? getLogger(BootstrapSeatLayoutSource);

  final CinemaHallRepo _halls;
  final AppLogger _logger;

  @override
  ResultFuture<SeatLayout> getLayout(String hallId) async {
    try {
      final info = await _halls.getCinemaHallInfoById(hallId);
      return info.fold(
        Left.new,                                   // pass legacy failure through unchanged
        (hall) => Right(synthesizeLegacyLayout(hall)),
      );
    } catch (e, st) {
      _logger.e('Failed to synthesize SeatLayout for hall $hallId',
          error: e, stackTrace: st);
      return Left(ServerFailure(message: e.toString(), statusCode: 500));
    }
  }
}
```

**CRITICAL — catch-all + logging (CLAUDE.md adapter rule).** The `catch (e, st)` with
`_logger.e(...)` is mandatory: an *unexpected* failure is logged and returned as a `Left`, never
thrown raw. The logger is **injectable** (`AppLogger? logger`) purely so the test can verify the
log call; production callers use the default `getLogger(...)`. A *known* failure from
`getCinemaHallInfoById` (its `Left`) is passed through untouched — it is already a `Failure`.

### Step 6 — DI wiring (`lib/injection_container.dart`)
In `_initCinemaHall()`, register the port (legacy manual `get_it` style — match the surrounding
registrations):

```dart
getIt.registerLazySingleton<SeatLayoutSource>(
  () => BootstrapSeatLayoutSource(getIt.get<CinemaHallRepo>()),
);
```

No other DI change. No consumer is wired yet (P5 will `getIt.get<SeatLayoutSource>()`); this
slice only makes the port resolvable.

### Step 7 — Codegen & verification
Codegen-affecting files changed (freezed/json), so run `build_runner`:

- `dart run build_runner build --delete-conflicting-outputs` — generates the `*.freezed.dart`
  / `*.g.dart` parts for the new value types.
- `dart format .` — no diff.
- `dart analyze` — no new warnings.
- `flutter test test/features/cinema_halls/0006_hall_layout_contract/` — green (see §6).
- No `slang`/l10n change (no UI strings). No `pubspec.yaml` change.

---

## 6. Tests

Per `prd.md` "Testing Decisions". **Cubit, Widget, and outside-in layers are deliberately N/A**
— this slice adds **no bloc, no UI, no user-visible behaviour** (the live grid is unchanged and
nothing new is rendered). The acceptance gate for *visible* free-form rendering is **P5**, which
consumes this contract; this slice's gate is the **synthesizer's mapping-parity unit tests**.
This is the same documented carve-out slice 0005 made for its absent layers, inverted. Mocks use
`mocktail` (the project has no `bloc_test`).

`test/features/cinema_halls/0006_hall_layout_contract/`:

**a) `domain/legacy_seat_layout_synthesizer_test.dart` — the deep-module parity tests (centre
of gravity).** Build `CinemaHallInfo` fixtures in-test (the seeded halls are not in the client;
they are backend-served — construct equivalents):
- a **rectangular** hall maps to the expected flat `SeatPlacement` list with
  `x = columnIndex`, `y = rowIndex`, `w = h = 1`, `rotation = 0`, `zoneId = null`;
- **`SeatId` identity round-trips** for every seat (`placement.seatId == (seat.row,
  seat.seatNumber)`);
- **`bounds`** equals `(-m, -2m, C+2m, R+3m)` with `m = kLegacyLayoutMargin` — explicit, not
  the bare seat bbox;
- **`screen`** is `side: top`, `start: (0,-m)`, `end: (C,-m)` — top, spanning the width;
- a **ragged** inner list (variable seats-per-row) and an **empty** hall (`cinemaSeat: []`) map
  sanely (no throw; empty → `seats: []`, `C = 0`, `R = 0`);
- the **three seeded shapes** — Red **28×22 = 616**, Black **21×18 = 378**, White
  **15×12 = 180** — produce the expected seat counts and extents (`bounds.width`/`height`). The
  concrete "1:1" lock. *(Convention check: confirm whether seeded dims are rows×cols or
  cols×rows against a real fixture before asserting extents; assert the **count** unconditionally
  and the extents per the confirmed orientation.)*

**b) `domain/seat_layout_test.dart` — model invariant tests.** A handful of value-type checks:
- `SeatPlacement.seatId` equals `(row, number)`;
- `w`/`h` default to `1`, `rotation` defaults to `0` when omitted;
- a seat's zone reads from `zoneId` and is **independent of any `Zone.polygon`** (set a
  `zoneId` that no polygon contains, and a polygon that does not match the `zoneId` — membership
  follows `zoneId`);
- `freezed` value equality holds for `SeatLayout`/`SeatPlacement`/`Zone` (equal fields ⇒ equal);
- `fromJson`/`toJson` round-trips a representative `SeatLayout` (locks the forward backend
  contract; `seatId` is **absent** from JSON — it is derived).

**c) `data/bootstrap_seat_layout_source_test.dart` — the throwaway adapter** (`mocktail` mock of
`CinemaHallRepo`, injected `MockAppLogger`):
- given a hall id and a `Right(CinemaHallInfo)` from `getCinemaHallInfoById`, returns
  `Right(SeatLayout)` whose seats match `synthesizeLegacyLayout` for that fixture (delegation);
- a `Left(ServerFailure)` from the repo is **passed through** as the same `Left` (no synthesis);
- an **unexpected exception** (stub `getCinemaHallInfoById` to throw) is caught in the catch-all:
  result is `Left(ServerFailure)` **and `logger.e(...)` is verified called** — never re-thrown.
  Mirrors the `CLAUDE.md` adapter rule and the existing `*Repo` coverage pattern.

No `application/` (cubit) or `presentation/` (widget) test directory is created for this slice.

**Prior art to mirror:** slice 0005's `seat_index_test.dart` (a pure derivation tested without
pumping widgets) for (a)/(b); the existing `cinema_halls` `*Repo` adapter tests for (c).

---

## 7. Report (what the implementing agent must hand back)

- Files **created**: the six `domain/layout/*.dart` value types, `domain/ports/
  seat_layout_source.dart`, `data/layout/legacy_seat_layout_synthesizer.dart`,
  `data/layout/bootstrap_seat_layout_source.dart`, plus the three test files; and the generated
  `*.freezed.dart`/`*.g.dart`.
- File **modified**: `lib/injection_container.dart` (one DI registration line only).
- Confirmation that **no other slice/feature** was touched; **no UI, no bloc, no
  `seats`/`cinema_halls` legacy migration**; **no `pubspec.yaml`** change; the live grid,
  booking, reservation, shopping-cart, SignalR status and pricing are all unchanged.
- Confirmation that `SeatId` is **imported** from `seats`, not duplicated; that the contract is
  **Flutter-free** (no `package:flutter`/`dio` import under `domain/layout/`); and that the
  adapter is marked **throwaway** with its deletion trigger named.
- Statement that the synthesizer constants/formulas match §3.2 verbatim and that the three
  seeded shapes' seat counts (616 / 378 / 180) are locked by test (a).
- Test run: number of new tests across the three files, all green; `build_runner` ran;
  `dart format` / `dart analyze` clean.

---

## 8. What NOT to do

- ❌ Do **not** build the P5 renderer — no `CustomPaint`, `InteractiveViewer`, layout→canvas
  transform, hit-testing, zoom/pan, or any pixel/canvas-space code. This slice is the contract +
  bootstrap only.
- ❌ Do **not** build a `PriceBloc`, wire money into the layout, or add price/status overlays.
  The contract carries **no money**; `SeatId`/`zoneId` are the only seams.
- ❌ Do **not** build the P6 editor — only provide the target shape it authors into.
- ❌ Do **not** change the backend, add a real `GET …/halls/{id}/layout`, or introduce a second
  parallel endpoint. The backend stays mocked; the adapter fakes the response.
- ❌ Do **not** import `package:flutter/*` or `package:dio/*` anywhere under `domain/layout/`
  (use a hex `String` colour, not `Color`; `double` coordinates, not `Offset`/`Rect`).
- ❌ Do **not** derive a seat's zone from a polygon — membership is the explicit `zoneId` only.
- ❌ Do **not** derive `bounds` from the seat bounding box alone — it is explicit and includes
  the screen + margins per §3.2.
- ❌ Do **not** redeclare `SeatId`, change seat identity away from `(row, seatNumber)`, or
  serialize the `seatId` getter.
- ❌ Do **not** migrate `cinema_halls`/`seats` to the target stack (rename `*Repo`/`*Bloc`,
  `slang`, `retrofit`, `injectable`) — they stay legacy; this is an additive contract slice.
- ❌ Do **not** add a `pubspec.yaml` dependency (all needed packages already exist) — and if you
  somehow think one is needed, ask the user first.
- ❌ Do **not** add a Cubit, widget, or outside-in test (no bloc/UI/visible behaviour here).
- ❌ Do **not** leave the bootstrap adapter unmarked — it must announce itself as throwaway with
  the deletion trigger, so the future swap to the real backend layout is an obvious one-file
  delete.
```
