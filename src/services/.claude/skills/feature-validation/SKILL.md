---
name: feature-validation
description: This skill should be used when the user wants to generate validation.md for a slice. Trigger when the user invokes /feature-validation, says "write the validation checklist", or asks for manual scenarios after requirements.md exists. Reads prd.md, plan.md, requirements.md. Produces validation.md with manual scenarios and code review checklist.
disable-model-invocation: false
---

# feature-validation

Generate `validation.md` for a slice. It contains two things:

1. **Manual test scenarios** — what a human (or curl) tries against a running
   service to confirm the slice works.
2. **Code review checklist** — what a reviewer looks for in the PR before
   approving.

This is **not** an automated test specification. The automated outside-in test
lives in `tests.md` and `<Slice>OutsideInTests.cs`.

## Process

### 1. Find the target slice

Same determination as in `/feature-requirements`.

Output: `specs/features/<aggregate>/<NNNN>_<slice>/validation.md`.

### 2. Read the inputs

Mandatory:

- The slice's `prd.md`, `plan.md`, `requirements.md`.
- `CLAUDE.md`.
- `agent_docs/architecture.md`, `agent_docs/error_handling.md`,
  `agent_docs/stable_vs_feature.md`, `agent_docs/testing.md`,
  `agent_docs/entry_points/minimal-api.md`.

If `requirements.md` is missing, stop and ask the user to run
`/feature-requirements` first.

### 3. Write `validation.md`

Use this structure:

```markdown
# NNNN · SliceName — Validation

## Inputs

- PRD: ./prd.md
- Plan: ./plan.md
- Requirements: ./requirements.md

## Prerequisites

- Service running locally:
  ```
  dotnet run --project BookingManagement/BookingManagementService.API
  ```
  (default port: check `launchSettings.json`; examples below use
  `http://localhost:<port>`).
- Test database provisioned and migrations applied:
  ```
  dotnet ef database update \
    -p BookingManagement/BookingManagementService.Infrastructure \
    -s BookingManagement/BookingManagementService.API
  ```
- Prerequisite seed data in place (e.g. a movie session, a shopping cart)
  as required by the slice.
- For authenticated endpoints: a valid Bearer token obtained from Keycloak
  (or a test JWT if a dev-auth bypass is configured).

## Manual scenarios

Numbered scenarios. Each lists steps, expected result, and the requirement ID
it covers.

### S1 — Happy path

**Steps:**

1. ```
   curl -s -X POST http://localhost:<port>/api/shoppingcarts/{cartId}/seats/select \
     -H "Content-Type: application/json" \
     -H "Authorization: Bearer <token>" \
     -d '{"showtimeId": "<sessionId>", "row": 1, "number": 1}'
   ```

**Expected:**

- HTTP 200.
- Response body matches the `<UseCase>Response` shape from `plan.md`.
- The seat row in the database is marked as reserved for the cart (verify with
  a direct DB query or a `GET` endpoint if available).

**Covers:** F1.

### S2 — Validation failure

**Steps:**

1. Submit the request with a missing or out-of-range required field.

**Expected:**

- HTTP 400.
- Response is a `ValidationProblemDetails` JSON body with `errors` listing
  the offending field.

**Covers:** F2.

### S3 — Not found

**Steps:**

1. Submit the request with an id that does not exist in the database.

**Expected:**

- HTTP 204 (No Content) — `ContentNotFoundException` mapped by
  `CustomExceptionHandler`.

**Covers:** F3.

### S4 — Conflict (if applicable)

**Steps:**

1. Perform the operation successfully (S1).
2. Repeat the same operation.

**Expected:**

- HTTP 409 — `ConflictException` mapped by `CustomExceptionHandler`.

**Covers:** F4.

### S5 — Unauthorized (if endpoint requires auth)

**Steps:**

1. Submit the request without the `Authorization` header.

**Expected:**

- HTTP 401.

### S6 — Forbidden (if applicable)

**Steps:**

1. Submit the request with a valid token for a user who does not own the
   resource.

**Expected:**

- HTTP 403.

**Covers:** F5.

(Add as many scenarios as needed. Cover at minimum: happy path, every distinct
error status code in the requirements, and any boundary condition easy to miss.)

## Code review checklist

For the reviewer (human or AI) to verify on the PR. Each line is a yes/no
question. Reject the PR until all are yes.

### Architecture

- [ ] Use-case folder exists at
      `Application/<Aggregate>/Command/<UseCase>/` (or `…/Queries/<UseCase>/`)
      and contains command, handler, validator, and response DTO (where needed).
- [ ] The handler is a MediatR `IRequestHandler<TCommand, TResult>` class with
      constructor injection of interfaces — no concrete EF Core types in its
      constructor or body.
- [ ] The command / query is a `record` implementing `IRequest<TResult>`.
- [ ] The validator is an `AbstractValidator<TCommand>` in the same use-case
      folder; it contains only structural rules (field presence, ranges, format).
      Business existence/state rules are in the handler.
- [ ] The repository interface (`I<Aggregate>Repository`) lives in
      `Domain/<Aggregate>/Abstractions/`; the EF Core implementation lives in
      `Infrastructure/Repositories/`. No EF Core type appears in `Domain` or
      `Application`.
- [ ] The endpoint delegate is in an `IEndpoints` implementation, contains no
      business logic, and follows the pattern: bind → build command →
      `sender.Send` → shape result. Per `agent_docs/entry_points/minimal-api.md`.
- [ ] No new aggregate, domain event, or repository interface was introduced
      in `Application/` or `Infrastructure/` — domain concepts stay in
      `Domain/`.

### Error handling

- [ ] The handler raises specific domain/application exceptions
      (`ContentNotFoundException`, `ConflictException`, `DomainValidationException`,
      etc.) or returns a failing `Result` — never a bare `new Exception(...)`.
- [ ] The handler contains no `catch (Exception)` that hides infrastructure
      faults.
- [ ] Read-only repository paths have no `try/catch` — infrastructure faults
      propagate to `CustomExceptionHandler`.
- [ ] The repository catches **only** business-meaningful `DbUpdateException`
      variants (e.g. unique-index violation → `ConflictException` or
      `DuplicateRequestException`) and nothing else.
- [ ] No new cross-cutting `*Exception` or `Error` type was introduced inside
      the slice folder. New types require registration in `CustomExceptionHandler`
      and an ADR.
- [ ] The handler sets no HTTP status code and does not reference `HttpContext`.
- [ ] No layer below `CustomExceptionHandler` logs-and-rethrows; each exception
      is thrown once and handled centrally.

### Stable infrastructure

- [ ] No stable file was modified beyond adding one
      `services.AddScoped<I<Aggregate>Repository, <Aggregate>Repository>()`
      line to the infrastructure DI extension method.
- [ ] `CustomExceptionHandler`, base types (`AggregateRoot`, `Entity`,
      `Result`, `Error`), `IEndpoints` / `EndpointExtensions`, and `Program.cs`
      were not changed. If they were, flag as ADR.
- [ ] MediatR/FluentValidation handler and validator discovery (`AddMediatR`,
      `AddValidatorsFromAssembly`) was not changed — new handlers and validators
      are auto-discovered.

### DI and wiring

- [ ] If a new repository implementation was added, one
      `services.AddScoped<I…Repository, …Repository>()` line exists in the
      infrastructure DI extension.
- [ ] No new MediatR handler or validator registration was written by hand —
      they are discovered from the assembly automatically.
- [ ] No new library outside the locked stack (see `CLAUDE.md`) was referenced.

### Tests

- [ ] Handler unit test exists and covers the happy path and each business
      failure branch.
- [ ] Repository unit test exists and covers the error-translation contract
      (unique-violation → specific exception) and the pass-through of unknown
      exceptions.
- [ ] Endpoint integration test exists, uses `WebApplicationFactory<Program>`,
      and validates routing, status codes, and `CustomExceptionHandler`
      translations.
- [ ] Outside-in test (`<Slice>OutsideInTests.cs`) exists, is the acceptance
      gate, and is GREEN.
- [ ] Each test resets DB state between runs (respawn/truncate or a transaction
      per test); no test leaves rows in shared state.

### Quality gates

Run from `src/services`:

```
dotnet format CinemaBookingManagement.sln
dotnet build  CinemaBookingManagement.sln -warnaserror
dotnet test   CinemaBookingManagement.sln
dotnet test --filter "FullyQualifiedName~<Slice>OutsideInTests"
```

All must pass, including:
- `BookingManagementService.Domain.ArchitectureTests` (enforces domain has no
  dependency on application, aggregate roots have a private parameterless
  constructor, domain events are `sealed` and named `*DomainEvent`).
- The slice's outside-in test.

If the architecture tests fail, the slice is **not done** even if every other
test is green.

For any EF Core model changes, also run:
```
dotnet ef migrations add <Name> \
  -p BookingManagement/BookingManagementService.Infrastructure \
  -s BookingManagement/BookingManagementService.API
dotnet ef database update \
  -p BookingManagement/BookingManagementService.Infrastructure \
  -s BookingManagement/BookingManagementService.API
```
```

### 4. Save and confirm

Write to `specs/features/<aggregate>/<NNNN>_<slice>/validation.md`. Tell the
user the file was created, list the scenario count and checklist groups, and
suggest:

> Next step: `/feature-tests` to produce tests.md (outside-in test spec).

## Style rules

- **English only**.
- **Concrete curl commands** in manual scenarios — copyable, runnable against
  `http://localhost:<port>` with the real route shapes from `plan.md`.
- **Yes/no checklist items.** Anything that needs a paragraph to explain is a
  scenario, not a checklist item.
- **Coverage:** every functional requirement has at least one manual scenario.
  Cross-check by listing requirement IDs under "Covers."
- **Code review checklist is mostly stable.** Copy from the previous slice's
  checklist and adjust only slice-specific entries.

## Hard limits

- Do not write any file other than `validation.md`.
- Do not include automated test code. Manual scenarios are curl commands;
  automated tests live in `tests.md` and the `.cs` test files.
- Do not invent new code review rules. The checklist mirrors `agent_docs/`; any
  novel rule belongs in `agent_docs/` first.
- Do not modify earlier spec files.

## Common mistakes

- Manual scenarios that require running `dotnet test`. Manual scenarios are for
  humans poking the running service with curl or a browser.
- Checklist items that overlap functional requirements. The checklist is about
  **how the code is shaped**, not about **whether features work**. Feature
  correctness is verified by tests.
- Curl commands that reference Python-style routes (`/api/v1/users`) instead of
  the actual routes from `plan.md`. Use the real route shapes.
- Skipping the "Covers" line under a scenario. The traceability matters during
  review.
- Listing a new `*Exception` type in the error-handling checklist without
  noting that it requires an ADR and a `CustomExceptionHandler` entry.
