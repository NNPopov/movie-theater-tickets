---
name: db-only-slice
description: Use this skill when implementing a DB-only slice — one that adds or changes EF Core entities, IEntityTypeConfiguration<T>, and an EF Core migration, but has no MediatR command/query handler or HTTP endpoint. Trigger when plan.md says "no use-case" and "no HTTP entry point", or when the user says "implement the DB foundation slice".
disable-model-invocation: false
---

# db-only-slice

Implement a DB-only slice: EF Core entity changes, `IEntityTypeConfiguration<T>`
configuration changes, and an EF Core migration.
No MediatR command/query, no handler, no validator, no endpoint.

## When to use

- The slice's `plan.md` says there is no HTTP entry point.
- The slice only touches `Domain/<Aggregate>/` entities and
  `Infrastructure/Data/Configurations/` (and possibly a new `DbSet` on the
  `DbContext`).
- The acceptance gate is `dotnet ef database update` succeeding + the app
  building and booting + architecture tests passing — **not** an outside-in HTTP
  test.

## Process

### 1. Read the inputs

Read in this order:

- `specs/features/<aggregate>/<NNNN>_<slice>/plan.md`
- `specs/features/<aggregate>/<NNNN>_<slice>/requirements.md`
- The entity class(es) to be changed (full file, not excerpts)
- The matching `IEntityTypeConfiguration<T>` in
  `Infrastructure/Data/Configurations/` (full file)
- The `DbContext` file to see what `DbSet<T>` properties and
  `ApplyConfigurationsFromAssembly` / explicit `ApplyConfiguration` calls exist

### 2. Implement entity changes in Domain

Apply changes in the order listed in `plan.md`. Rules that must hold (see
`agent_docs/architecture.md` and `agent_docs/stable_vs_feature.md`):

**Aggregate roots:**
- Must have a **private parameterless constructor** (enforced by
  `Domain.ArchitectureTests`; EF Core uses it for materialisation).
- Expose state changes through methods on the aggregate, not by mutating
  properties from outside.
- Use **static factory methods** (`MovieSession.Create(...)`) for construction.

**Domain types:**
- No EF Core attributes (`[Column]`, `[Key]`, `[ForeignKey]`, etc.) inside
  `Domain/`. All mapping goes in the `IEntityTypeConfiguration<T>`.
- No `DbContext`, no `DbSet`, no EF Core namespaces inside `*.Domain` — this is
  enforced by architecture tests.
- Value objects live in `Domain/Shared/` or owned by the aggregate; they are
  compared by value and carry no EF Core mapping themselves.
- Domain events are `sealed record`/class and end in `DomainEvent` (also
  enforced).

**Adding a property to an existing entity:**
- Non-nullable property with a C# default → add it to the entity and map it in
  the configuration.
- Non-nullable property for a table that already has rows → also provide a
  `defaultValueSql:` in the migration (see Step 5 below); EF Core autogenerate
  does **not** carry a C# default into SQL.
- Nullable property → map it with `.IsRequired(false)` (or leave the default
  for reference types in a nullable-enabled project).

**Creating a new entity/aggregate root file:**
- Place in `Domain/<Aggregate>/`.
- Private parameterless constructor, static factory method.
- No EF Core annotations.

### 3. Implement configuration changes in Infrastructure

Create or update the `IEntityTypeConfiguration<T>` in
`Infrastructure/Data/Configurations/`:

- Use the Fluent API exclusively — no data annotations in Domain.
- Map the table name, column names/types, required/optional, max lengths,
  indexes, unique constraints, and foreign keys here.
- Owned entity types (value objects) use `.OwnsOne(...)` / `.OwnsMany(...)`.
- All `HasIndex(...).IsUnique()` / `HasForeignKey(...)` that the domain requires
  must appear explicitly.

### 4. Register the entity in the DbContext

Check which discovery mechanism the `DbContext` uses:

- **`ApplyConfigurationsFromAssembly`** (typical): add a `DbSet<TEntity>`
  property on the context so EF Core includes the entity in migrations. No
  explicit `ApplyConfiguration<T>` call needed.
- **Explicit `ApplyConfiguration<T>` calls**: add a call for the new
  configuration class as well.

A missing `DbSet` or missing configuration call causes EF Core to think the
table was removed and generates a destructive `DropTable` — the check in Step 5
will catch this, but prevent it here.

### 5. Generate and review the migration

Run from `src/services`:

```
dotnet ef migrations add <Name> \
  -p BookingManagement/BookingManagementService.Infrastructure \
  -s BookingManagement/BookingManagementService.API
```

Open the generated file **immediately** and verify every line in `Up()` and
`Down()` before proceeding.

**Drop-table check (critical — data loss):** If `Up()` contains
`migrationBuilder.DropTable(...)` for a table you did not intend to remove,
stop. Either the `DbSet` is missing from the context, the configuration was not
discovered, or a namespace changed. Fix the registration and regenerate. Do not
apply a migration that drops tables you did not intend to drop.

**Drop-column check:** Similarly, `migrationBuilder.DropColumn(...)` for a
column you did not remove from the entity means the property mapping is missing
from the configuration. Fix and regenerate.

**`defaultValueSql` check (critical — NOT NULL violation on live data):** For
every `migrationBuilder.AddColumn<T>(nullable: false, ...)` being added to a
table that already has rows, confirm the call includes a sensible
`defaultValue:` or `defaultValueSql:`. EF Core autogenerate **never** carries a
C# property initialiser into SQL. Add it manually if missing:

```csharp
// autogenerated (wrong for a table with existing rows):
migrationBuilder.AddColumn<string>(
    name: "Status",
    table: "MovieSessions",
    nullable: false);

// corrected — provide a default for existing rows:
migrationBuilder.AddColumn<string>(
    name: "Status",
    table: "MovieSessions",
    nullable: false,
    defaultValue: "Scheduled");
```

**Index check:** Confirm `migrationBuilder.CreateIndex(...)` entries exist for
every property you mapped with `.HasIndex(...)` in the configuration.

**Unique-constraint check:** Confirm `.IsUnique()` constraints appear as
`migrationBuilder.CreateIndex(..., unique: true)`.

**Foreign-key check:** Confirm `migrationBuilder.AddForeignKey(...)` entries
match every `.HasForeignKey(...)` in the configuration.

**`Down()` reversal check:** Confirm `Down()` reverses every operation in
`Up()` in reverse order: drop foreign keys → drop indexes → drop columns → drop
tables. An incomplete `Down()` makes rollback unsafe.

### 6. Apply the migration

```
dotnet ef database update \
  -p BookingManagement/BookingManagementService.Infrastructure \
  -s BookingManagement/BookingManagementService.API
```

Confirm it prints the migration name with no errors. If the output shows an
unexpected error about a missing column or an existing table, stop and diagnose
— do not re-run until the cause is understood.

### 7. Quality gates

Run from `src/services` in this order — all must pass:

```
dotnet format CinemaBookingManagement.sln
dotnet build  CinemaBookingManagement.sln -warnaserror
dotnet test   CinemaBookingManagement.sln
```

The `BookingManagementService.Domain.ArchitectureTests` suite runs as part of
`dotnet test` and enforces:

- `Domain` has no dependency on `Application`, `Infrastructure`, or any
  framework package.
- Aggregate roots have a private parameterless constructor.
- Domain events are `sealed` and named `*DomainEvent`.

If any architecture test fails, the slice is **not done** even if every other
test is green. Investigate and fix before declaring completion.

The slice is complete when:

1. `dotnet ef database update` applied the migration cleanly.
2. `dotnet build -warnaserror` passes with no new warnings.
3. `dotnet test` passes, including all architecture tests.

There is no outside-in HTTP test for a DB-only slice.

## Common mistakes

- Running `dotnet ef migrations add` before verifying the `DbSet` and
  configuration are wired up — produces a migration that drops existing tables.
- Adding a non-nullable column to a populated table without `defaultValue` /
  `defaultValueSql` — causes a `NOT NULL` constraint violation on
  `database update`.
- Placing EF Core attributes (`[Column]`, `[Key]`, `[Required]`) directly on
  the Domain entity — violates the Dependency Rule and will be caught by
  architecture tests.
- Forgetting the **private parameterless constructor** on a new aggregate root —
  caught by `Domain.ArchitectureTests`, but better to add it from the start.
- Accepting a `DropTable` in the generated migration without investigating — this
  is almost always a registration/discovery bug, not an intentional change.
- Accepting a `DropColumn` for a column that still exists in the entity — the
  Fluent mapping is missing from the configuration.
- Skipping the `Down()` review — an incomplete `Down()` means the migration
  cannot be safely rolled back.
- Changing EF Core's `DbContext`, base configuration conventions, or the
  migration mechanism itself without treating it as a stable-infrastructure
  change — see `agent_docs/stable_vs_feature.md`.

## Reference

- Architecture rules: `agent_docs/architecture.md`
- Stable vs feature: `agent_docs/stable_vs_feature.md`
- Error handling (repositories): `agent_docs/error_handling.md`
