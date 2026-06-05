---
name: slice-decomposition
description: This skill should be used when the user is splitting a new feature into slices, when a planned slice feels too large, or when the user asks "should this be one slice or two?". It helps decide the right grain of decomposition — each slice is one MediatR command or query use-case, one repository, one HTTP entry point.
disable-model-invocation: false
---

# slice-decomposition

Reference for how to split a feature into slices. The unit of decomposition is
the **use-case**, not the resource. A REST `POST /shoppingcarts` and a
`GET /shoppingcarts/{id}` are two slices.

## When to trigger

- User is starting a new feature and asks "how do I structure this?"
- User has a `plan.md` that touches multiple operations.
- User asks "should `CreateMovieSession` and `UpdateMovieSession` be the same
  slice?"
- User asks "how do I name this slice?"
- User is unsure whether to use the layered style or Vertical Slice Architecture
  for a given entity.

## Layered vs Vertical Slice — decide first

This service runs **two architectural styles** (see `agent_docs/architecture.md`
"The two architectures"):

1. **Layered Clean Architecture + DDD + CQRS (the default, primary style)** for
   the core, behaviour-rich aggregates — **`MovieSessions` and `ShoppingCarts`**
   and everything that hangs off them (`MovieSessionSeats`, `Seats`,
   `CinemaHalls`, `Movies`, pricing). This is the style used throughout the
   codebase today. Each use-case lives under
   `Application/<Aggregate>/Command/<UseCase>/` or
   `Application/<Aggregate>/Queries/<UseCase>/` and consists of a command/query
   record, a handler, a validator, and a response DTO.

2. **Vertical Slice (secondary style)** for smaller, auxiliary entities that play
   a supporting role. **This has not been adopted yet.** Do not convert anything
   to VSA on your own initiative. Which entities move to VSA, and when, is decided
   per-case with the user.

**If a task tempts you toward VSA for an entity, stop and ask which style
applies.** Do not pre-emptively restructure anything into self-contained vertical
slices. Only after the user confirms VSA is the chosen style for that entity
should you proceed with VSA packaging.

For all decomposition work on `MovieSessions`, `ShoppingCarts`, and their
satellites, assume the layered style throughout.

## Process

### 1. Load context

Read these:

- `CLAUDE.md`
- `agent_docs/architecture.md`
- The relevant existing use-case folders under
  `BookingManagement/BookingManagementService.Application/<Aggregate>/` if any
  (for reference patterns).

### 2. Apply the heuristics

A slice corresponds to exactly **one use-case** — one MediatR command or query
and its folder. To decide whether two operations are one slice or two:

**Two slices when any of these holds:**

- The operations have different **repositories or domain services**: one writes
  to `IActiveShoppingCartRepository`, one reads from `IMovieSessionsRepository`.
- The operations have different **authorization rules**: any authenticated user
  can `GetShoppingCart`; only the cart owner can `ReserveSeats`.
- The operations have different **cache policies**: a query result is cached;
  the corresponding command invalidates it.
- The operations model different **failure modes** worth distinguishing in the
  domain (e.g. `ContentNotFoundException` vs `ConflictException`).
- The operations are exposed at **different HTTP paths or methods**.
- The handler would depend on a large set of repositories or services and branch
  heavily — a sign it is really two use-cases forced into one.

**One slice when all of these hold:**

- Both operations depend on the same small set of repositories/services.
- Both operations have the same authorization rule.
- One is a strict subset of the other (e.g. a dry-run variant of the same
  command logic).
- The two share no useful distinction at the domain layer.

When in doubt, **two slices**. Merging later is cheap; splitting an overgrown
slice is expensive.

### 3. Name the slice

Slices are named in **PascalCase** using a **Verb + Noun** business operation.
Never use the HTTP verb as the name:

- `CreateMovieSession`, `UpdateMovieSession`, `GetMovieSession`,
  `CancelMovieSession`
- `CreateShoppingCart`, `SelectSeat`, `UnselectSeat`, `ReserveSeats`,
  `PurchaseSeats`, `GetShoppingCart`
- `CreateCinemaHall`, `GetCinemaHallSeats`

For query variants with meaningfully different shapes, each is its own slice:

- `GetShoppingCart` — single cart by ID
- `ListMovieSessions` — paginated, with filters
- `SearchMovieSessions` — full-text / multi-field query

Three slices, not one.

For batch operations, prefix with `Bulk`:
`BulkCancelMovieSessions`, `BulkReleaseSeats`.

### 4. Sanity-check the result

For each proposed slice, confirm:

- It represents one operation that is meaningful to a human user.
- Its handler depends on a **small, focused set** of repositories and domain
  services. If the handler would need many repositories and many conditional
  branches, it is probably two use-cases — split it.
- It can be tested by one outside-in scenario (`<Slice>OutsideInTests`).
- Its name reads as `<Verb><Noun>` in PascalCase and reflects the business
  operation, not the HTTP verb.

### 5. Document the decisions

In the feature's first PRD (or in an ADR), record:

- The list of slices that compose the feature.
- The architectural style (layered vs VSA) and the reason if VSA was chosen.
- The reason for any non-obvious split or merge decision.
- Which slices are in scope for the current iteration and which are deferred.

## Worked example: ShoppingCarts feature

The `ShoppingCarts` aggregate with its full lifecycle. All slices use the
**layered style** (this is a core, behaviour-rich aggregate):

| Slice | Kind | Use-case folder | Notes |
|---|---|---|---|
| `CreateShoppingCart` | Command | `ShoppingCarts/Command/CreateShoppingCart/` | Creates a new cart for a session |
| `SelectSeat` | Command | `ShoppingCarts/Command/SelectSeat/` | Adds a seat to the cart; acquires a distributed lock |
| `UnselectSeat` | Command | `ShoppingCarts/Command/UnselectSeat/` | Removes a selected seat; releases the lock |
| `ReserveSeats` | Command | `ShoppingCarts/Command/ReserveSeats/` | Transitions cart to Reserved; different auth + state machine |
| `PurchaseSeats` | Command | `ShoppingCarts/Command/PurchaseSeats/` | Transitions cart to Purchased; triggers domain events |
| `GetShoppingCart` | Query | `ShoppingCarts/Queries/GetShoppingCart/` | Read-only; different repository interface, cached |

Six slices for one aggregate. Each has its own folder with command/query record,
handler, validator, and response DTO. The aggregate root `ShoppingCart` and its
repository interface `IActiveShoppingCartRepository` are shared across slices
through `Domain/ShoppingCarts/`.

## Anti-patterns

- Naming a slice `ShoppingCartCrud` containing all operations. Multiple
  repository interfaces, multiple authorization rules, multiple state-machine
  transitions — impossible to maintain cleanly.
- Splitting `SelectSeat` further into `ValidateSeatInput` and `PersistSeat`.
  That is internal handler structure, not a slice boundary.
- Grouping `SelectSeat`, `UnselectSeat`, and `ReserveSeats` into
  `SeatManagement` because they "feel related." Each transitions the aggregate
  through a different state and has distinct failure modes; split them.
- Combining `GetShoppingCart` with `ReserveSeats` because both touch the same
  aggregate. Read and write are different slices: different repository interface,
  different HTTP method, different caching concerns.
- Adopting VSA packaging for `ShoppingCarts` or `MovieSessions` without explicit
  user approval — these are core aggregates; layered is the required style.

## Common mistakes

- Naming a slice after the URL: `PostShoppingcarts`, `DeleteSeatsId`. The slice
  is named after the business operation, not the route.
- Sharing a repository interface across two slices "because the signature is the
  same." If they are two operations, each handler takes the interface it needs
  independently; shared dependency does not mean shared slice.
- Placing a new handler directly in `Application/<Aggregate>/` rather than in
  its own `Command/<UseCase>/` or `Queries/<UseCase>/` subfolder — the unit of
  work is the use-case folder, not the aggregate folder.
- Introducing VSA structure for a new auxiliary entity without stopping to ask —
  VSA has not been adopted yet; the conversation must happen first.
