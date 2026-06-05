---
name: feature-spec
description: This skill should be used when the user wants to generate plan.md for a slice. Trigger when the user invokes /feature-spec, says "write the plan", "spec out this slice", or asks for the implementation plan after prd.md exists. Reads prd.md plus architectural context plus the nearest reference slice. Produces plan.md only.
disable-model-invocation: false
---

# feature-spec

Generate `plan.md` for a slice. The plan translates the PRD into a concrete
implementation blueprint: file paths, C# class names, method signatures, and
the sequence of steps to implement.

## Process

### 1. Find the target slice

The slice is the one currently being worked on:

1. If the user named a slice path, use it.
2. Else find the most recently modified `prd.md` under `specs/features/*/*/`.
3. If ambiguous, ask.

The output path is
`specs/features/<aggregate>/<NNNN>_<slice>/plan.md`. The folder already exists
because `/to-prd` created it.

### 2. Read the inputs

Mandatory reads:

- The slice's `prd.md`.
- `CLAUDE.md`.
- `agent_docs/architecture.md`.
- `agent_docs/error_handling.md`.
- `agent_docs/stable_vs_feature.md`.
- `agent_docs/entry_points/minimal-api.md` (if the slice has an HTTP entry
  point — almost always).

Reference slice: search `specs/roadmap.md` for the most recent existing slice
with the **same operation shape** (e.g. for a `CancelBooking` command, look for
another command slice in the same aggregate). Read its `plan.md`. If no
shape-match exists, state this in the new `plan.md` and proceed.

### 3. Write `plan.md`

Use this structure. Section names are fixed.

```markdown
# NNNN · SliceName — Implementation plan

## 1. Header

- **Aggregate:** <e.g. ShoppingCarts>
- **Slice:** <NNNN>_<SliceName>
- **PRD:** ./prd.md
- **Reference slice (if any):** ../<reference>/plan.md
- **HTTP path:** `<METHOD> /api/<resource>/...`
- **STABLE files touched:** list any stable files that must be touched —
  typically only a one-line DI registration in the infrastructure extension
  method (e.g. `services.AddScoped<IFooRepository, FooRepository>()`).
  Anything beyond adding one registration line is an ADR — stop and ask.

## 2. Context summary

One paragraph: what does this slice do, who calls it, what does it return?
Drawn from the PRD; no new product decisions.

## 3. API contract

- **Request model** (`<UseCase>Request`): list of fields with C# types and
  validation constraints (FluentValidation rules).
- **Path / query params:** list with types and constraints.
- **Response model** (`<UseCase>Response`): list of fields with C# types.
- **Status codes:**
  - 2xx success codes (chosen by the endpoint delegate).
  - 4xx / 5xx failure codes, each mapped to the specific `*Exception` or
    `Error` that triggers it via `CustomExceptionHandler`. Reference
    `agent_docs/error_handling.md` for the full mapping table. Do not invent
    new status codes — if one is missing from the table, flag it as an open
    question.

## 4. File structure

```
BookingManagement/
├── BookingManagementService.Domain/
│   └── <Aggregate>/
│       ├── Abstractions/
│       │   └── I<Aggregate>Repository.cs      # add method if not present
│       └── Events/
│           └── <Event>DomainEvent.cs          # if slice raises a domain event
├── BookingManagementService.Application/
│   └── <Aggregate>/
│       └── Command/<UseCase>/                 # (or Queries/<UseCase>/)
│           ├── <UseCase>Command.cs            # record : IRequest<TResult>
│           ├── <UseCase>CommandHandler.cs     # IRequestHandler<TCommand, TResult>
│           ├── <UseCase>CommandValidator.cs   # AbstractValidator<TCommand>
│           └── <UseCase>Response.cs           # response DTO (if query / if needed)
├── BookingManagementService.Infrastructure/
│   ├── Repositories/
│   │   └── <Aggregate>Repository.cs          # add method if not present
│   └── Data/Configurations/
│       └── <Entity>Configuration.cs          # add/update if model changes
└── BookingManagementService.API/
    └── Endpoints/
        └── <Aggregate>Endpoints.cs           # add endpoint to IEndpoints impl
```

If the slice introduces a new EF Core entity or alters an existing one, list
the migration command:

```
dotnet ef migrations add <Name> \
  -p BookingManagement/BookingManagementService.Infrastructure \
  -s BookingManagement/BookingManagementService.API
```

## 5. Implementation steps

Numbered list, layer by layer. For each step: what file, what to put in it,
what to verify.

1. **Domain — Repository interface.** Add (or verify the existence of) the
   method signature `Task<Foo?> GetByIdAsync(Guid id, CancellationToken ct)`
   to `I<Aggregate>Repository` in `Domain/<Aggregate>/Abstractions/`. No EF
   Core types; the interface belongs to the domain.
2. **Domain — Domain event (if needed).** Create `<Event>DomainEvent.cs` as a
   `sealed record` whose name ends in `DomainEvent`. Raise it from the
   aggregate method.
3. **Application — Command / Query.** Create the `record <UseCase>Command(...)
   : IRequest<TResult>` in `Application/<Aggregate>/Command/<UseCase>/`.
   Command and handler may live in the same file.
4. **Application — Validator.** Create `<UseCase>CommandValidator :
   AbstractValidator<<UseCase>Command>`. Structural rules only (not empty,
   ranges, formats). Business existence/state rules belong in the handler.
5. **Application — Handler.** Create `<UseCase>CommandHandler :
   IRequestHandler<<UseCase>Command, TResult>`. Inject repository interfaces
   and domain services via the primary constructor. Follow the pattern:
   acquire lock (if needed) → load aggregate (throw `ContentNotFoundException`
   if null) → enforce invariants via aggregate methods → call domain services
   → persist → return `Result.Success()` / response DTO. Error handling per
   `agent_docs/error_handling.md`.
6. **Application — Response DTO (if needed).** Create `<UseCase>Response.cs`
   as an immutable `record` carrying only the fields the client needs.
7. **Infrastructure — Repository implementation.** Add the method body to the
   EF Core `<Aggregate>Repository`. Catch only business-meaningful
   `DbUpdateException` variants (per `agent_docs/error_handling.md`); all
   other exceptions propagate.
8. **Infrastructure — EF Core configuration (if model changes).** Update or
   create `<Entity>Configuration : IEntityTypeConfiguration<T>`. Run the
   migration.
9. **API — Endpoint.** In the `IEndpoints` implementation for the resource,
   add the route delegate: bind request → build command → `sender.Send` →
   `.Match(...)` / `Results.Ok(...)`. No business logic. Per
   `agent_docs/entry_points/minimal-api.md`.
10. **DI — Repository registration (if new).** Add one
    `services.AddScoped<I<Aggregate>Repository, <Aggregate>Repository>()` line
    to the infrastructure DI extension. This is the only stable-file touch
    permitted without an ADR.
11. **Verify.** Run:
    ```
    dotnet format CinemaBookingManagement.sln
    dotnet build  CinemaBookingManagement.sln -warnaserror
    ```
    Resolve any warnings before moving to tests.

## 6. Tests planned

Four levels, per `agent_docs/testing.md`:

- **Handler unit test** —
  `tests/BookingManagementService.Domain.UnitTests/<Aggregate>/Command/<UseCase>/<UseCase>HandlerTests.cs`.
  Mocks repository interfaces. Validates the happy path and each business
  failure (`ContentNotFoundException`, `ConflictException`, etc.).
- **Repository / adapter unit test** —
  `tests/BookingManagementService.Infrastructure.UnitTests/Repositories/<Aggregate>RepositoryTests.cs`.
  Validates that `DbUpdateException` on a unique violation is translated to
  `ConflictException` / `DuplicateRequestException`, and that other
  infrastructure exceptions propagate unchanged.
- **Endpoint integration test** — in the `WebApplicationFactory<Program>` test
  project, `Features/<Aggregate>/<NNNN>_<slice>/<UseCase>EndpointTests.cs`.
  Validates routing, request/response shapes, status codes, and
  `CustomExceptionHandler` translation.
- **Outside-in test** —
  `tests/.../Features/<Aggregate>/<NNNN>_<slice>/<Slice>OutsideInTests.cs`.
  Full HTTP stack with real handler, real repository, test database. Mock only
  true external boundaries (third-party HTTP, sometimes Redis, the clock).

**Opt-outs (if any):** state explicitly which level is skipped and why. Default
is no opt-outs. See `agent_docs/testing.md` § "When a level may be skipped".

## 7. Out of scope for this slice

Bullet list of things this slice does **not** do. Examples:

- No Redis caching (handled by a follow-up).
- No bulk variant (separate slice if needed).
- No rate limiting.

## 8. Open questions

Anything unresolved at planning time. If none, write "None."
```

### 4. Save and confirm

Write to `specs/features/<aggregate>/<NNNN>_<slice>/plan.md`. Tell the user the
file was created, summarize the eight sections, and suggest the next step:

> Next step: `/feature-requirements` to produce requirements.md.

## Style rules

- **English only** in spec files.
- **Concrete file paths and C# class names**, not placeholders. Write
  `CreateMovieSessionCommand`, not `<UseCase>Command`, in the actual plan.
- **Reference the relevant agent_docs section** when a rule is invoked
  (e.g. "error handling per `agent_docs/error_handling.md`").
- **No new architectural patterns invented inside the plan.** If you find
  yourself needing one, stop and ask the user. Adding a new pattern is an ADR,
  not a plan.

## Hard limits

- Do not write any file other than `plan.md`.
- Do not write source code or test code.
- Do not modify `prd.md` or `roadmap.md`.
- Do not run shell commands.

## Common mistakes

- Skipping the reference-slice read. Without it, naming and structure drift
  from the rest of the codebase.
- Producing a plan that touches STABLE files beyond adding one DI registration
  line. If the plan needs to change `CustomExceptionHandler`, a base type, or
  the MediatR pipeline, stop and ask — that is an ADR.
- Inventing a new `*Exception` or `Error` type inside the slice. New
  cross-cutting exception/error types are ADR-level decisions. Flag this as an
  open question.
- A plan with more than ~12 implementation steps. The slice is likely two
  slices; re-decompose per the `slice-decomposition` skill.
- A plan that includes business decisions ("we will allow soft delete here").
  Business decisions belong in the PRD; the plan references them.
- Using EF Core types (`DbContext`, `DbSet`) in the `Domain` or `Application`
  layers. EF Core lives only in `Infrastructure`.
