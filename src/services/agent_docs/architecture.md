# Architecture reference

Read this when designing a new use-case, modifying layer boundaries, deciding
whether something is layered or a vertical slice, or working with cross-cutting
concerns. For the decision of "one slice or two", see the `slice-decomposition`
skill. For HTTP wiring, see `entry_points/minimal-api.md`. For errors, see
`error_handling.md`.

## The two architectures (hybrid)

This service deliberately runs **two** structural styles side by side.

### 1. Layered Clean Architecture + DDD + CQRS — the default

This is the style of the whole `BookingManagement` service today and the **only**
style allowed for the core, behaviour-rich aggregates: **`MovieSessions` and
`ShoppingCarts`** (and their satellites: `MovieSessionSeats`, `Seats`,
`CinemaHalls`, `Movies`, pricing). Use it unless you have explicit agreement to do
otherwise.

Four projects, dependencies pointing strictly inward:

```
BookingManagement/
├── BookingManagementService.Domain/          # innermost, framework-free
│   ├── <Aggregate>/                           # e.g. ShoppingCarts/, MovieSessions/
│   │   ├── <Aggregate>.cs                     # aggregate root (ShoppingCart, MovieSession)
│   │   ├── <Aggregate>Status.cs               # owned enums / value objects
│   │   ├── Abstractions/                      # I…Repository, domain service interfaces
│   │   └── Events/                            # *DomainEvent (sealed)
│   ├── Common/                                # AggregateRoot, Entity, Ensure, base events
│   ├── Error/                                 # Result, Error, DomainErrors, ResultExtensions
│   ├── Exceptions/                            # ContentNotFoundException, ConflictException, …
│   ├── Services/                              # domain services (e.g. MovieSessionSeatService)
│   └── Shared/                                # shared value objects
├── BookingManagementService.Application/      # use-cases (CQRS)
│   ├── <Aggregate>/
│   │   ├── Command/<UseCase>/                 # one folder per command use-case
│   │   │   ├── <UseCase>Command.cs            # record : IRequest<TResult>  (often inlined with handler)
│   │   │   ├── <UseCase>CommandHandler.cs     # IRequestHandler<TCommand, TResult>
│   │   │   ├── <UseCase>CommandValidator.cs   # AbstractValidator<TCommand>
│   │   │   └── <UseCase>Response.cs           # response DTO (if any)
│   │   ├── Queries/<UseCase>/                 # one folder per query use-case
│   │   ├── DTOs/                              # shared read DTOs for the aggregate
│   │   ├── Base/                              # shared handler base classes for the aggregate
│   │   └── Events/                            # application-level event handlers
│   ├── Abstractions/                          # application-only ports (Repositories, Services)
│   ├── Common/Behaviours/                     # MediatR pipeline behaviours (Validation, Idempotency)
│   ├── Exceptions/                            # application exceptions (ValidationException, NotFound…)
│   └── ConfigureServices.cs                   # AddApplicationServices (MediatR/FV/AutoMapper wiring)
├── BookingManagementService.Infrastructure/   # adapters out to the world
│   ├── Repositories/                          # EF Core implementations of the I…Repository ports
│   ├── Data/Configurations/                   # IEntityTypeConfiguration<T>
│   ├── Migrations/                            # EF Core migrations
│   ├── EventBus/                              # RabbitMQ
│   └── Services/                              # Redis lock/cache adapters, hashers, etc.
└── BookingManagementService.API/              # composition root + transport
    ├── Endpoints/                             # IEndpoints implementations (Minimal API)
    ├── Infrastructure/CustomExceptionHandler.cs
    ├── IntegrationEvents/                     # inbound/outbound integration events
    └── Program.cs                             # builds the host, wires everything
```

The CQRS feature-folders inside `Application` **are already a form of vertical
slicing within a layered shell**: one folder per operation, holding its command,
handler, validator, and response. A "slice" in the spec workflow maps to exactly one
of these folders.

### 2. Vertical Slice — secondary, for auxiliary entities

Some smaller entities play only a supporting role and today live as parts of bigger
modules. Over time, selected ones may be peeled out into self-contained vertical
slices instead of being spread across the four projects. **This has not happened
yet.** Do not convert anything to VSA on your own initiative:

- The list of entities that move to VSA, and the timing, is decided per-case with
  the user.
- When the first VSA module is agreed, this document gets a concrete layout for it
  and a reference example. Until then, treat "vertical slice" as a planned option,
  not an existing pattern.
- Even under VSA, the inward Dependency Rule and the framework-free domain still
  hold; the difference is packaging (one self-contained module per use-case rather
  than spreading a use-case across shared layer projects).

If a task tempts you toward VSA for an entity, **stop and ask** which style applies.

## Hard dependency rules

- `Domain` references nothing but the BCL. No EF Core, ASP.NET, MediatR, Serilog,
  AutoMapper. Enforced by `Domain_Should_NotToHaveDependencyOnApplication` and the
  project's package list.
- `Application` references `Domain` only (plus MediatR/FluentValidation/AutoMapper
  abstractions). It defines **ports** (repository/service interfaces) it needs —
  either in `Domain/.../Abstractions/` (when the port is a domain concept) or in
  `Application/Abstractions/` (when it is an application concern like idempotency or
  distributed locking).
- `Infrastructure` references `Application` + `Domain`. It implements the ports with
  EF Core, Redis, RabbitMQ, etc. It is the **only** place those libraries appear.
- `API` references everything and is the composition root: it registers services,
  the MediatR pipeline, the `CustomExceptionHandler`, and the endpoints.
- A handler imports its aggregate, the repository **interfaces**, and domain
  services — never an EF Core type, never another aggregate's internal handler.

## DDD building blocks (already in the codebase)

- **Aggregate root** — inherits `AggregateRoot`, implements `IAggregateRoot`. Has a
  **private parameterless constructor** (enforced by an architecture test, for EF
  Core materialisation) and **static factory methods** (`MovieSession.Create(...)`,
  `ShoppingCart.Create(...)`). State changes go through methods on the aggregate
  (`cart.AddSeats(...)`, `cart.SetShowTime(...)`); callers do not mutate fields.
- **Domain event** — a `sealed record`/class whose name ends in `DomainEvent`
  (both enforced by architecture tests). Raised by the aggregate, dispatched after
  persistence, handled by `INotificationHandler<BaseApplicationEvent<TEvent>>` in
  `Application/.../Events/`.
- **Domain service** — behaviour that does not belong to a single aggregate
  (`MovieSessionSeatService`). Lives in `Domain/Services/`, framework-free.
- **Value object** — in `Domain/Shared/` or owned by an aggregate. Compared by
  value.
- **Invariant guards** — `Domain/Common/Ensure` and the `Ensure…` methods on
  aggregates (`cart.EnsureSeatCanBeAdded(...)`) protect invariants and throw
  domain exceptions when violated.

## The use-case (MediatR) anatomy

A layered use-case is a MediatR request + handler. The command and handler often
live in the same file:

```csharp
// Application/ShoppingCarts/Command/SelectSeats/SelectSeatCommandHandler.cs
public record SelectSeatCommand(Guid MovieSessionId, short SeatRow, short SeatNumber, Guid ShoppingCartId)
    : IRequest<Result>;

public class SelectSeatCommandHandler(
    IActiveShoppingCartRepository activeShoppingCartRepository,
    IDistributedLock distributedLock,
    MovieSessionSeatService movieSessionSeatService,
    ILogger logger)
    : IRequestHandler<SelectSeatCommand, Result>
{
    public async Task<Result> Handle(SelectSeatCommand request, CancellationToken cancellationToken)
    {
        // 1. acquire infrastructure preconditions (locks)
        // 2. load the aggregate via its repository (throw ContentNotFoundException if missing)
        // 3. enforce invariants through aggregate methods (cart.EnsureSeatCanBeAdded, cart.AddSeats)
        // 4. call domain services for cross-aggregate work
        // 5. persist via the repository
        // 6. return Result.Success() (or a response DTO)
    }
}
```

Rules:

- Constructor injection (primary constructors are the prevalent style). The handler
  receives **interfaces**, never concrete EF Core types.
- The handler **assumes structural validity** — the `ValidationBehaviour<,>` already
  ran the `AbstractValidator`. The handler enforces only business rules
  (existence, state machine, conflicts).
- The handler **does not set HTTP status codes** and does not touch `HttpContext`.
- Shared logic across an aggregate's handlers goes into a base class under
  `Application/<Aggregate>/Base/` (e.g. `ActiveShoppingCartHandler`), not copied.

## Validation

`AbstractValidator<TCommand>` in the use-case folder, discovered by
`AddValidatorsFromAssembly` in `ConfigureServices`, executed by
`ValidationBehaviour<,>` before the handler. A validation failure raises the
application `ValidationException`, which `CustomExceptionHandler` turns into a 400
`ValidationProblemDetails`. Keep field-shape rules (not empty, ranges, formats) in
the validator; keep "does this row exist / is this state legal" in the handler.

## Bounded contexts: one concept ≠ one class

The same word in different layers/aggregates is a **different type**. A
`ShoppingCart` aggregate in `Domain` is not the `ShoppingCartDto` returned by a
query, which is not the request body the endpoint accepts. Do not collapse them into
one shared class to "avoid duplication":

- The aggregate carries behaviour and invariants.
- The DTO carries only the fields a reader needs, shaped for the wire.
- The request model carries only what the client sends.

A reliable smell that you wrongly merged two contexts: you want to add a nullable
field to a "shared" type that only makes sense in one of them. That means two types
are needed.

## Cross-cutting and DI

- The composition root is `API/Program.cs` + the per-layer
  `Add…Services(...)` extension methods (`AddApplicationServices`,
  infrastructure registration). New feature dependencies are registered there.
- MediatR pipeline behaviours (`ValidationBehaviour<,>`, the idempotency behaviour)
  apply to every request; do not re-implement validation or idempotency inside a
  handler.
- Infrastructure concerns that must not create DI cycles (the exception handler, the
  event bus, the distributed lock) depend on **interfaces**, not on handlers.
- Background work (worker services, integration-event consumers) reuses the same
  MediatR handlers; it does not duplicate business logic.

## Terminology

- **Repository** — the persistence port. Interface in `Domain/.../Abstractions/`
  (`IActiveShoppingCartRepository`, `IMovieSessionsRepository`), EF Core
  implementation in `Infrastructure/Repositories/`. The term is embraced here.
- **Adapter** — any Infrastructure implementation of an Application/Domain port
  (a repository, a Redis lock, a hasher). "Adapter" and "repository" are used
  interchangeably for the persistence case.
- **Slice / use-case** — one MediatR command or query and its folder. The unit the
  spec workflow operates on.
