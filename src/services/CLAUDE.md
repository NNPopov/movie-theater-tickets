# CLAUDE.md — universal rules for this .NET service

This file is loaded into every session. Anything written here is mandatory and
overrides anything in `agent_docs/`, in individual skills, or in source code.

Topical references live in `agent_docs/`. Read them by relevance to the task.
Reusable procedures live in `.claude/skills/` and are invoked by name.

## Project at a glance

- Async ASP.NET Core service, **.NET 10**, C# with nullable reference types on.
- Solution: `CinemaBookingManagement.sln`. Service root:
  `BookingManagement/`. Four projects per the Clean Architecture onion:
  `*.Domain`, `*.Application`, `*.Infrastructure`, `*.API`. Assemblies are named
  `BookingManagementService.*`; the root namespace is `CinemaTicketBooking.*`.
- **Two architectures coexist by design (hybrid):**
  - **Layered Clean Architecture + DDD + CQRS (the default, primary style)** for
    the core, behaviour-rich aggregates — **`MovieSessions` and `ShoppingCarts`**
    and everything that hangs off them. Rich domain models, domain events,
    repositories, MediatR command/query handlers organised by feature folders.
  - **Vertical Slice (secondary style)** for smaller, auxiliary entities that play
    a supporting role and currently live as parts of larger modules. Which entities
    move to VSA, and when, is **decided per-case later** — do not pre-emptively
    convert anything. See `agent_docs/architecture.md`.
- **CQRS via MediatR.** A use-case is a `record` command/query implementing
  `IRequest<TResult>`, handled by an `IRequestHandler<TRequest, TResult>`.
  Cross-cutting concerns (validation, idempotency) run as MediatR pipeline
  behaviours.
- **Validation** is **FluentValidation** (`AbstractValidator<TCommand>`), run by the
  `ValidationBehaviour<,>` pipeline before the handler.
- **Error handling:** native exceptions translated centrally. Handlers/domain raise
  `*Exception` types (`ContentNotFoundException`, `ConflictException`,
  `DomainValidationException`, `LockedException`, …); the global
  `CustomExceptionHandler : IExceptionHandler` maps each to an HTTP `ProblemDetails`.
  A `Result`/`Error` monad (`Domain/Error/`) is **also** used for some expected
  business outcomes. Both styles coexist **by design** — the split is **decided**
  (ADR-002, Accepted 2026-06-04): expected business outcomes ⇒ `Result`, the
  unexpected ⇒ exception. See `agent_docs/error_handling.md`. **Do not unify them on
  your own.**
- **Persistence:** PostgreSQL via EF Core (async), repositories in
  `*.Infrastructure/Repositories/`, configurations in
  `*.Infrastructure/Data/Configurations/`, migrations in
  `*.Infrastructure/Migrations/`.
- **HTTP:** Minimal APIs registered through the `IEndpoints.DefineEndpoints`
  convention; handlers are invoked via MediatR `ISender`/`IMediator`.
- Other infrastructure already present: Serilog, Redis (distributed lock + cache),
  RabbitMQ event bus, AutoMapper, Keycloak/JWT auth, OpenTelemetry.

## Locked technology stack

| Concern | Choice |
|---|---|
| Language / runtime | C# / .NET 10, nullable reference types enabled, full type usage |
| Web framework | ASP.NET Core Minimal APIs (`IEndpoints` convention) |
| Application mediation | MediatR (`IRequest<T>`, `IRequestHandler<,>`, `IPipelineBehavior<,>`) |
| Validation | FluentValidation (`AbstractValidator<T>`) via `ValidationBehaviour<,>` |
| Mapping | AutoMapper |
| ORM | EF Core (async) on PostgreSQL (Npgsql) |
| Migrations | EF Core migrations |
| Cache / locks | Redis (distributed cache + distributed lock) |
| Messaging | RabbitMQ event bus |
| Logging | Serilog (`Serilog.ILogger`) |
| Testing | xUnit + FluentAssertions; NetArchTest for architecture rules; `WebApplicationFactory<Program>` for HTTP-level tests |
| Format / lint | `dotnet format` + Roslyn analyzers; build warnings treated as defects |

Anything not on this list requires explicit approval before use.

## Forbidden without explicit user approval

- A generic CRUD wrapper / repository base that hides EF Core behind reflection.
  Repositories are written by hand against the aggregate they serve.
- **Synchronous database or I/O calls.** All I/O is `async`/`await` with a
  `CancellationToken` threaded through.
- **EF Core entity types or `DbContext` usage inside `*.Domain` or `*.Application`.**
  EF Core lives only in `*.Infrastructure` (and the composition root).
- **Throwing `HttpException`-style transport errors, or writing to
  `HttpContext.Response`, inside a handler.** Handlers raise domain/application
  exceptions (or return a `Result`); HTTP translation happens once in
  `CustomExceptionHandler`.
- A use-case implemented as anything other than a MediatR
  `IRequestHandler<TRequest, TResult>` for the layered style. (A VSA auxiliary
  module may host its handler differently — but only after the VSA approach is
  agreed for that entity.)
- A new aggregate root without a private parameterless constructor, or a domain
  event whose name does not end in `DomainEvent` and is not `sealed`. These are
  enforced by `BookingManagementService.Domain.ArchitectureTests`.
- `*.Domain` taking a dependency on `*.Application`, `*.Infrastructure`, or any
  framework package (also enforced by an architecture test).
- Inventing a new architectural pattern or a new cross-cutting `*Exception` /
  `Error` type inside a single feature folder. New cross-cutting types are an ADR,
  not a slice detail — stop and ask.

## Universal hard rules

These apply to every change, every file, every PR. Violations are non-negotiable.

1. **The Dependency Rule points inward.** `Domain` depends on nothing. `Application`
   depends on `Domain` only. `Infrastructure` and `API` depend inward on
   `Application`/`Domain`. Nothing inward depends on a framework.
2. **`Domain` is framework-free.** It contains entities, value objects, aggregate
   roots, domain events, domain services, the `Result`/`Error` types, and the
   **abstractions (interfaces) for repositories and domain services** under
   `Abstractions/`. No EF Core, no ASP.NET, no MediatR, no Serilog.
3. **A use-case is a MediatR handler** (layered style). The command/query is a
   `record` implementing `IRequest<TResult>`; the handler implements
   `IRequestHandler<TRequest, TResult>`. The HTTP endpoint converts the request
   model into the command and sends it via `ISender` — it contains no business
   logic. See `agent_docs/entry_points/minimal-api.md`.
4. **Validation lives in a `AbstractValidator<TCommand>`**, discovered by
   `AddValidatorsFromAssembly` and executed by `ValidationBehaviour<,>`. Handlers
   assume their command is already structurally valid; they enforce only the
   business rules a validator cannot express.
5. **Use-case never decides the HTTP status.** It raises an application/domain
   `*Exception` or returns a `Result`. Translation to HTTP `ProblemDetails` happens
   once, in `CustomExceptionHandler`. To surface a new HTTP status you register a
   handler there, not in the use-case.
6. **Repositories are the persistence port.** Their interfaces live in
   `*.Domain/.../Abstractions/` (named `I…Repository`); their EF Core
   implementations live in `*.Infrastructure/Repositories/`. The word "Repository"
   is the project's chosen term — use it.
7. **The unit of work is the use-case folder.** Under
   `*.Application/<Aggregate>/Command/<UseCase>/` (or `…/Queries/<UseCase>/`) live
   the command/query, its handler, its validator, and its response DTO. Keep them
   together.
8. **Adapters catch only business-meaningful infrastructure exceptions** (e.g.
   `DbUpdateException` on a unique index → `ConflictException` /
   `DuplicateRequestException`). They do **not** wrap work in
   `try/catch (Exception)`. Unknown failures propagate to `CustomExceptionHandler`,
   which logs and returns 500. See `agent_docs/error_handling.md`.
9. **The error model is decided — see ADR-002 (Accepted 2026-06-04).** `Result`/`Error`
   and `*Exception` both exist on purpose, split by the *nature* of the outcome:
   expected business outcomes (and in-aggregate transitions that raise a domain event)
   ⇒ `Result`; structural validation ⇒ `ValidationBehaviour`/`ValidationException`;
   the unexpected/infrastructure ⇒ exception ⇒ `CustomExceptionHandler`. Follow the
   pattern that split dictates for the aggregate you are touching; do not refactor one
   into the other without an ADR.
10. **Stable infrastructure is not changed casually.** Composition roots
    (`Program.cs`, `ConfigureServices`, DI registration), `CustomExceptionHandler`,
    base types (`AggregateRoot`, `Entity`, `ValueObject`, `Result`), and the
    `IEndpoints` plumbing are stable. Adding a new endpoint registration or a new
    DI line for a new feature is fine; changing the mechanism itself is an ADR. See
    `agent_docs/stable_vs_feature.md`.

## Where to look when working on a task

Read by relevance. Always read `architecture.md`, `error_handling.md`, and
`spec_workflow.md` for any non-trivial change.

| You are doing | Read this |
|---|---|
| Designing a new use-case / slice | `agent_docs/architecture.md` |
| Deciding layered vs vertical slice for an entity | `agent_docs/architecture.md`, skill `slice-decomposition` |
| Splitting a feature into slices | skill `slice-decomposition` |
| Writing or modifying a repository/adapter | `agent_docs/error_handling.md` |
| Writing a Minimal API endpoint | `agent_docs/entry_points/minimal-api.md` |
| Writing tests | `agent_docs/testing.md` |
| Working with the spec chain (PRD/plan/etc) | `agent_docs/spec_workflow.md`, skill `spec-workflow` |
| Deciding whether a file/area is stable | `agent_docs/stable_vs_feature.md` |
| Adding RabbitMQ / Redis / event-bus wiring | will be documented when the first such slice lands |

## Slice spec workflow

A new use-case ("slice") gets a complete spec folder with **five markdown files** at
`specs/features/<aggregate>/<NNNN>_<slice>/`:

| File | Generated by | Sources |
|---|---|---|
| `prd.md` | `/to-prd` | the conversation; user stories and product decisions |
| `plan.md` | `/feature-spec` | prd.md + reading existing slices for context |
| `requirements.md` | `/feature-requirements` | prd.md + plan.md |
| `validation.md` | `/feature-validation` | prd.md + plan.md + requirements.md |
| `tests.md` | `/feature-tests` | all four above |

Plus one executable counterpart — here, a **C# test** — outside `specs/`:

| File | Generated by | Sources |
|---|---|---|
| `<Slice>OutsideInTests.cs` (RED) | `/slice-test-red` | tests.md + plan.md + agent_docs/testing.md |

**Run the commands in this order**, one at a time. Each command produces its file
and returns. Do not skip steps — later files depend on earlier ones.

```
/grill-me              (optional discovery interview)
/to-prd                → prd.md
/feature-spec          → plan.md
/feature-requirements  → requirements.md
/feature-validation    → validation.md
/feature-tests         → tests.md
/slice-test-red        → <Slice>OutsideInTests.cs (verified RED)
implementation         → until the outside-in test turns GREEN
```

The outside-in test is the **acceptance gate**. The slice is not done until that
single test passes. Other tests (handler unit test, repository test, endpoint
integration test) are written after green is reached, per `agent_docs/testing.md`.

### Optional zeroth step: `/grill-me`

The user may invoke `/grill-me` before `/to-prd` to stress-test a plan through an
interview. `grill-me` produces **only conversation in the chat** — questions,
answers, and a final summary. It does **not** write files, run commands, or create
spec files. When the interview ends it outputs a summary and the line
"Next step: /to-prd". The user issues the next command.

### Modifying an existing slice

When behaviour changes on a slice that is already green:

1. Update `tests.md` first to describe the new expected behaviour. If requirements
   change, update `requirements.md` first, then `tests.md`.
2. Update `<Slice>OutsideInTests.cs` to match the new tests.md. Run it and confirm
   it is **red** against the current implementation.
3. Change the implementation until the outside-in test is green again.
4. Update affected unit tests as a final step.

For pure refactors with no behaviour change, the outside-in test stays green
throughout. If it goes red during a refactor, the change is not a refactor — it is
a behaviour change, and tests.md must be updated first.

## Default test coverage for a new slice

Every new slice has all four levels by default:

- **Handler unit test** with the repositories/services mocked. Validates business
  logic and branches. xUnit + FluentAssertions + a mocking library.
- **Repository / adapter unit test** validating that business-meaningful
  infrastructure exceptions (e.g. a unique-violation `DbUpdateException`) are
  translated into the right `*Exception`, and that other infrastructure exceptions
  propagate **unchanged**. See `agent_docs/error_handling.md`.
- **Endpoint integration test** through `WebApplicationFactory<Program>` against the
  test database. Validates routing, request/response shapes, status codes, and
  `CustomExceptionHandler` translation.
- **Outside-in test** as the acceptance gate (see workflow above).

Tests may be opted out **only** when the relevant layer is trivial and adds no
behaviour — see `agent_docs/testing.md` for the exact criteria.

## Where generated specs go

- `specs/roadmap.md` — global index of slices. **Owned by `/to-prd`.** Other skills
  read it but never write to it.
- `specs/features/<aggregate>/<NNNN>_<slice>/` — per-slice folder with the five
  markdown files. `NNNN` is global (max+1 from roadmap), not per-aggregate.

## Working with context during modifications

When changing an existing slice, **read only the files of that use-case folder plus
its direct dependencies** (the aggregate, the repository interface, the endpoint
registration). Do not load the whole solution. If the change touches a cross-cutting
concern (DI, `DbContext`, auth), read just that file in isolation.

## Conflict resolution

If two documents conflict, this file wins. If a skill conflicts with an
`agent_docs/` file, the skill wins for its scope and the agent_docs entry should be
updated to match.

## Verifying changes

After any code change, run from `src/services`:

```
dotnet format CinemaBookingManagement.sln                  # apply formatting
dotnet build  CinemaBookingManagement.sln -warnaserror     # compile, no new warnings
dotnet test   CinemaBookingManagement.sln                  # full suite incl. architecture tests
```

The `BookingManagementService.Domain.ArchitectureTests` project encodes structural
rules (domain has no dependency on application, aggregate roots have a private
parameterless ctor, domain events are sealed and `*DomainEvent`). If an architecture
test fails, the change is **not done**, even if every other test is green.

For schema or model changes (run against the `Infrastructure` project, started by
`API`):

```
dotnet ef migrations add <Name> -p BookingManagement/BookingManagementService.Infrastructure -s BookingManagement/BookingManagementService.API
dotnet ef database update     -p BookingManagement/BookingManagementService.Infrastructure -s BookingManagement/BookingManagementService.API
```

Outside-in test for the slice you are working on:

```
dotnet test --filter "FullyQualifiedName~<Slice>OutsideInTests"
```

A change is not complete until build, the full test suite, and the slice's
outside-in test all pass.

## Language and tone

All generated artifacts (PRDs, plans, requirements, validations, tests, markdown
specs, code comments, XML doc comments, log messages) are written in **English**.
The conversation with the user may be in another language.
