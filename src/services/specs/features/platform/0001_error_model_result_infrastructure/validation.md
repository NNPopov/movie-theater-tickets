# 0001 · ErrorModelResultInfrastructure — Validation

## Inputs

- PRD: ./prd.md
- Plan: ./plan.md
- Requirements: ./requirements.md

> **Note on shape.** This slice has **no HTTP endpoint and no use-case**, so there are no curl
> scenarios against a running service. The manual scenarios below are therefore (a) build and
> test observations and (b) one optional "no HTTP behavior change" spot-check against the
> existing assign-cart endpoint, since the only runtime-observable change is the
> `DomainErrors<T>` error-code strings (which no test or response asserts). The code review
> checklist is adapted to a shared-kernel change.

## Prerequisites

- .NET 10 SDK installed; working directory `src/services`.
- For the optional HTTP spot-check (S5 only): the service runnable locally —
  ```
  dotnet run --project BookingManagement/BookingManagementService.API
  ```
  and a valid Bearer token for the authenticated ShoppingCart endpoint. The DB/migration
  setup is unchanged by this slice (no migration is added).

## Manual scenarios

### S1 — Build is clean

**Steps:**

1. ```
   dotnet format CinemaBookingManagement.sln
   dotnet build  CinemaBookingManagement.sln -warnaserror
   ```
   (Account for the accepted AutoMapper **NU1903** NuGet-audit advisory — a known project
   constraint — so the real build/warnings are what is validated.)

**Expected:**

- Solution compiles with no new warnings. The `NotFountError → NotFoundError` rename and its
  call-site update compile cleanly (proves F11, F12).

**Covers:** F9, F11, F12, N9.

### S2 — `Result<T>` specification is green

**Steps:**

1. ```
   dotnet test --filter "FullyQualifiedName~ResultOfTSpecification"
   ```

**Expected:**

- All `Result<T>` facts pass: success carries the value; failure exposes the error; `Value` on
  failure throws `InvalidOperationException`; implicit conversion from a value yields success;
  implicit conversion from an `Error` yields failure; `Match` runs the success branch with the
  value and the failure branch with the error.

**Covers:** F1–F8 (acceptance gate).

### S3 — Full suite and architecture tests are green

**Steps:**

1. ```
   dotnet test CinemaBookingManagement.sln
   ```

**Expected:**

- Every test passes, including `BookingManagementService.Domain.ArchitectureTests`. The
  pre-existing `Result` call sites (six seat operations, the cart commands) are unaffected;
  no contract/integration test changed.

**Covers:** F9, F13, N4, N10, N12.

### S4 — `Result<T>` surface is minimal (inspection)

**Steps:**

1. Inspect the public members of `Result<TValue>` in `Domain/Error/Result{TValue}.cs`.

**Expected:**

- Only `IsSuccess`/`IsFailure`/`Error` (inherited), `Value`, `Success`, `Failure`, the two
  implicit conversions, and the `Match` overload (in `ResultExtensions`) are present. No
  `Bind`, `Map`, or LINQ query-syntax members exist.

**Covers:** F8.

### S5 — (Optional) No HTTP behavior change on the assign-cart endpoint

**Steps:**

1. With the service running, call the assign-cart endpoint for a **non-existent** cart id
   (the path that returns a `NotFoundError`/`ContentNotFoundException`):
   ```
   curl -s -o /dev/null -w "%{http_code}\n" -X PUT \
     http://localhost:<port>/api/shoppingcarts/00000000-0000-0000-0000-000000000000/assign \
     -H "Authorization: Bearer <token>"
   ```
   (Confirm the exact route/verb in `ShoppingCartEndpointApplicationBuilderExtensions`.)

**Expected:**

- The HTTP status is **unchanged** from before this slice (the `Result → exception` bridge and
  `CustomExceptionHandler` mapping are untouched). The only difference is the
  `Error.Code`/log string now naming the real type instead of `"T.*"` — not surfaced as a
  status change.

**Covers:** F12, F13, N5, N12.

## Code review checklist

Each line is a yes/no question. Reject the PR until all applicable items are yes. Items marked
**N/A** have no corresponding component in this slice.

### Shared-kernel change (scope)

- [ ] Only the six files named in `plan.md` §1/§4 are modified:
      `Domain/Error/Result.cs`, `Domain/Error/Result{TValue}.cs` (new),
      `Domain/Error/Error.cs`, `Domain/Error/DomainErrors.cs`,
      `Domain/Error/ResultExtensions.cs`, and the ShoppingCart endpoint file.
- [ ] `Result<TValue> : Result`; the base `Result` constructor changed from `private` to
      `protected` and **nothing else** in `Result.cs` changed (guard, properties, factories
      identical).
- [ ] `Result<TValue>` exposes only `Value`, `Success`, `Failure`, two implicit conversions,
      and inherits `IsSuccess`/`IsFailure`/`Error`; the `Match` overload lives in
      `ResultExtensions`. No `Bind`/`Map`/LINQ.
- [ ] `Value` on a failed `Result<TValue>` throws `InvalidOperationException`.
- [ ] The generic `Match<TValue,TOut>` coexists with the existing non-generic `Match` and
      resolves unambiguously (success-delegate arity differs).
- [ ] `DomainErrors<T>` uses `typeof(T).Name` (not `nameof(T)`) in all four factories.
- [ ] `NotFountError` is renamed to `NotFoundError` everywhere (type, `DomainErrors.NotFound`
      body, endpoint pattern match); no occurrence of the old spelling remains.

### Architecture

- [ ] `Domain` stays framework-free: the new/changed `Domain/Error/*` code uses only the BCL —
      no EF Core, ASP.NET, MediatR, Serilog, or AutoMapper. (N4)
- [ ] No EF Core type appears in `Domain` or `Application`.
- [ ] N/A — no use-case folder, handler, command, validator, repository, aggregate, or domain
      event is introduced.

### Error handling

- [ ] No new cross-cutting `*Exception` or `Error` **type** is invented (the `NotFoundError`
      rename is not a new type) and no error-handling **mechanism** changes. (N11)
- [ ] `CustomExceptionHandler`, the exception hierarchy, and the MediatR pipeline are
      untouched. (N11)
- [ ] The endpoint `Result → exception` bridge is unchanged apart from the `NotFoundError`
      rename; no handler sets an HTTP status or touches `HttpContext`. (N5, F12)
- [ ] No bare `throw new Exception(...)` is added (the existing one in the endpoint bridge is
      left as-is per Out of Scope).

### Stable infrastructure

- [ ] The base-type change is limited to the one documented `Result` ctor-visibility line; this
      is the agreed, ADR-002-step-1 exception, not an undocumented stable-mechanism change.
- [ ] No change to `IEndpoints`/`EndpointExtensions`, `Program.cs`, `ConfigureServices`, base
      types other than `Result`, the `DbContext`, or migrations.
- [ ] `agent_docs/error_handling.md` is **not** updated and ADR-002 stays **Proposed** (per
      PRD — reserved to the Decider).

### DI and wiring

- [ ] No DI registration is added or changed (no new repository/service/handler to register).
- [ ] No new library outside the locked stack (see `CLAUDE.md`) is referenced.

### Tests

- [ ] `ResultOfTSpecification.cs` exists in
      `BookingManagement/tests/BookingManagementService.Domain.UnitTests/Error/`, follows the
      `*Specification` + AAA + xUnit/FluentAssertions conventions, and is GREEN (the acceptance
      gate).
- [ ] No new handler/repository/endpoint test is added — those levels are opted out (no such
      component), as recorded in `plan.md` §6.
- [ ] No existing test was modified to accommodate this change (proves the behavior-preserving
      claim). (F13, N12)

### Quality gates

Run from `src/services`:

```
dotnet format CinemaBookingManagement.sln
dotnet build  CinemaBookingManagement.sln -warnaserror
dotnet test   CinemaBookingManagement.sln
dotnet test --filter "FullyQualifiedName~ResultOfTSpecification"
```

All must pass, including `BookingManagementService.Domain.ArchitectureTests`. If the
architecture tests fail, the slice is **not done** even if every other test is green.

No EF Core model change in this slice → **no migration** to add or apply.
