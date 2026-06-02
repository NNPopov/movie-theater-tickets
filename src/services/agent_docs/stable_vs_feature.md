# Stable vs feature

This project does **not** use per-file header comments. "Stable" vs "feature" is a
matter of *where the file lives and what it does*, not a marker on line 1. (The
Python kit this was adapted from required `# STABLE:` / `# FEATURE:` headers — drop
that idea entirely; it is not idiomatic C# and is not used here.)

## Stable infrastructure — change only with explicit approval

These define *mechanisms*. Changing the mechanism affects every slice and is an ADR,
not a slice detail. Touch them only when the infrastructure itself must change, and
flag it.

- **Composition roots / DI:** `API/Program.cs`, `Application/ConfigureServices.cs`,
  and the infrastructure registration extensions. *(Adding one registration line for
  a new feature is fine — see below.)*
- **Cross-cutting pipeline:** the MediatR behaviours
  (`Common/Behaviours/ValidationBehaviour`, the idempotency behaviour) and their
  registration.
- **Error translation:** `API/Infrastructure/CustomExceptionHandler.cs` and the
  exception/`Error` type hierarchies (`Domain/Exceptions`, `Application/Exceptions`,
  `Domain/Error/*`).
- **Domain base types:** `Domain/Common/` (`AggregateRoot`, `Entity`, base events,
  `Ensure`), `Domain/Shared/`, value-object bases.
- **HTTP plumbing:** `API/Endpoints/Common/` (`IEndpoints`, `EndpointExtensions`).
- **Persistence plumbing:** the `DbContext` itself, the migration mechanism, the
  base configuration conventions.

## Feature code — change freely within the rules

These are added/edited per slice without special approval, as long as the hard rules
in `CLAUDE.md` and `architecture.md` hold:

- A use-case folder under `Application/<Aggregate>/Command|Queries/<UseCase>/`
  (command, handler, validator, response).
- A new aggregate, value object, domain event, or domain service in `Domain/`.
- A new repository **interface** in `Domain/.../Abstractions/` and its EF Core
  implementation in `Infrastructure/Repositories/`.
- A new `IEntityTypeConfiguration<T>` and the migration it produces.
- A new endpoint **group** (`IEndpoints` class) or a new endpoint in an existing
  group.
- The corresponding tests.

## The allowed touches to stable files

Some stable files legitimately grow by one line per feature. These are *not* ADRs:

- **Registering a new handler/validator** — usually automatic via
  `AddMediatR(...RegisterServicesFromAssembly...)` and
  `AddValidatorsFromAssembly(...)`; nothing to edit.
- **Registering a new repository/service** in the infrastructure DI extension (one
  `services.AddScoped<IFoo, Foo>()` line).
- **Registering a new endpoint group** through the existing `IEndpoints` discovery
  (no edit to the mechanism).
- **A new `DbSet`/configuration** discovered by the `DbContext`.

Anything beyond "add one registration for a new feature" — changing *how* discovery,
validation, mapping, or error translation works — is a stable-mechanism change:
**stop and ask**, and capture it as an ADR.

## When in doubt

Ask: *"Am I adding a new feature using the existing mechanism, or changing the
mechanism?"* The first is feature work. The second is an ADR.
