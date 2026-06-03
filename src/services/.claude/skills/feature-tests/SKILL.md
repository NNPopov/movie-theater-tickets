---
name: feature-tests
description: This skill should be used when the user wants to generate tests.md for a slice. Trigger when the user invokes /feature-tests, says "write outside-in test spec", or asks to describe the slice-level integration test in markdown form before any code is written. Use only after prd.md, plan.md, requirements.md, validation.md exist. Produces markdown specification only; does not write C# code.
disable-model-invocation: false
---

# feature-tests

Generate `tests.md` for a slice. The output is a **markdown specification** of
one outside-in integration test that exercises the slice end-to-end through
its HTTP entry point, with the real handler, the real repository, the test
Postgres, and mocks only at true external system boundaries.

The C# implementation is produced separately by `/slice-test-red`. This skill
writes prose, not code.

## Process

### 1. Find the target slice

Same determination as in `/feature-validation`. Output:
`specs/features/<aggregate>/<NNNN>_<slice>/tests.md`.

### 2. Read the inputs

Read **all four** existing spec files:

- `prd.md` — overall behavior.
- `plan.md` — public surface (HTTP path, request/response shapes, status codes).
- `requirements.md` — functional requirements with IDs.
- `validation.md` — manual scenarios. The outside-in test typically covers the
  happy-path scenario plus the most important failure path.

Plus:

- `agent_docs/testing.md` — test conventions, the four levels, the
  `WebApplicationFactory` pattern, and the outside-in test example.
- `agent_docs/entry_points/minimal-api.md` — for exact HTTP call shapes.
- `agent_docs/error_handling.md` — for exact HTTP status codes and response
  shapes for each exception.

If any of the four spec files is missing, stop and ask the user to create it.

### 3. Decide the test boundary

For this project, the outside-in test goes through the **full HTTP stack**:

- **Wired real:** ASP.NET Core app (with all middleware and
  `CustomExceptionHandler`), the `ValidationBehaviour<,>` pipeline, the
  slice's handler, the slice's repository (EF Core), and the test Postgres
  database.
- **Mocked:** true external boundaries — third-party HTTP APIs, Redis
  distributed lock / cache (where a real Redis is impractical or
  non-deterministic), the clock (for time-dependent tests). Seed data is
  inserted directly via fixtures or SQL before each scenario.

The test makes HTTP calls through the `WebApplicationFactory<Program>` client
(`factory.CreateClient()`). It does **not** call the handler, command, or
repository types directly — it is black-box over HTTP.

### 4. Write the file

Use this structure. Keep section names unchanged. Keep prose tight.

```markdown
# NNNN · SliceName — Outside-in test spec

## Goal

One sentence: what end-to-end behavior does this test prove?

Example: "A caller who POSTs a valid seat-selection request receives HTTP 200
and the selected seat is recorded as reserved in the database."

## Entry point

The HTTP call the test makes.

- **Method:** `POST`
- **Path:** `/api/shoppingcarts/{cartId}/seats/select`
- **Body:** `ReserveSeatsRequest` with fields `showtimeId`, `row`, `number`
- **Headers:** `Authorization: Bearer <test-token>` (if the endpoint requires
  auth; describe how to obtain it in the fixture section)
- **Idempotency header (if applicable):** `X-Idempotency-Key: <guid>`

## Wired real

- ASP.NET Core app via `WebApplicationFactory<Program>` (full middleware stack
  including `CustomExceptionHandler` and `ValidationBehaviour<,>`).
- `<UseCase>CommandHandler` / `<UseCase>QueryHandler`.
- `<UseCase>CommandValidator`.
- `I<Aggregate>Repository` → `<Aggregate>Repository` (EF Core, real Postgres).
- Test Postgres database; state is reset between tests (respawn/truncate or a
  transaction per test — match the approach already used by sibling tests in
  the project).

## Mocked

- **Redis distributed lock** (if the handler acquires one): replace with a
  no-op or an in-memory stub so the test does not depend on a Redis instance.
- **Clock** (if the handler uses `DateTimeOffset.UtcNow` or a clock
  abstraction): freeze via a test double so scenarios are deterministic.
- **<Third-party HTTP client>** (if any): patched to return a canned response.
- **RabbitMQ event bus** (if the handler publishes integration events): replace
  with a no-op stub; the domain event side-effect on the DB is still asserted.

If no external boundaries are touched: write "None — the test runs entirely
against the test database."

## Fixtures / setup

- **`BookingApiFactory`** (or the project's existing `WebApplicationFactory`
  subclass): boots the app, overrides the DB connection to the test database.
- **`<Aggregate>Seeder`** (or inline SQL): inserts the prerequisite rows needed
  for each scenario. Be explicit: list the exact rows and their key fields.
- **Auth:** describe how the test client gets a valid Bearer token or how the
  test factory configures a bypass authentication scheme for protected
  endpoints.

## Test scenarios

### Scenario 1: happy path

**Setup:**

- DB contains: `<list the exact seed rows — e.g. a MovieSession with id X,
  a ShoppingCart with id Y in Active status>`.
- Mocks configured: `<list any explicit mock returns — e.g. Redis lock returns
  acquired; or "none">`.

**Act:**

- Send `POST /api/shoppingcarts/{cartId}/seats/select` with body
  `{ "showtimeId": "<sessionId>", "row": 1, "number": 1 }` and a valid
  Bearer token.

**Expect:**

- HTTP status: `200`.
- Response body matches `<UseCase>Response` shape: `<list key fields and
  values, e.g. no body / id returned>`.
- DB state: the `MovieSessionSeats` row for (sessionId, row=1, number=1) has
  `ShoppingCartId` set to `cartId` and `Status == Reserved`.

**Covers requirement(s):** F1.

### Scenario 2: most important failure path

(Pick the failure scenario that most directly tests the slice's business logic
— typically "not found" for commands that must load an aggregate, or "conflict"
for create/reserve operations.)

**Setup:**

- DB contains: `<describe minimal seed state>`.

**Act:**

- Send the same request with `<the condition that triggers the failure — e.g.
  a cartId that does not exist>`.

**Expect:**

- HTTP status: `<e.g. 404 for ContentNotFoundException, 409 for
  ConflictException>`.
- Response body: `<describe the ProblemDetails shape if one is returned, e.g.
  the 404 ProblemDetails for ContentNotFoundException>`.
- DB state: `<assert no unintended mutation — e.g. "the seat remains
  Available">`.

**Covers requirement(s):** F3 (or the relevant F-id from requirements.md).

## Out of scope for this test

- Field-level validation errors (covered by the endpoint integration test,
  which asserts 400 + `ValidationProblemDetails`).
- Specific `DbUpdateException` variants (covered by the repository unit test).
- Handler branches beyond the two scenarios above (covered by handler unit
  tests).
- Performance, load, and concurrency.
```

### 5. Save and confirm

Write to `specs/features/<aggregate>/<NNNN>_<slice>/tests.md`. Tell the user
the file was created, list the scenarios, and suggest:

> Next step: `/slice-test-red` to generate the failing C# test from this spec.

## Style rules

- **English only**.
- **Two scenarios is the default**: one happy path, one most-important failure
  path. Add a third only if a critical scenario cannot be covered by the
  endpoint integration test or the handler unit tests.
- **Concrete fixtures**: `row = 1`, `number = 1`, not `row = <some_value>`.
- **Exact status codes**: always derive from the `CustomExceptionHandler`
  mapping table in `agent_docs/error_handling.md` — do not guess.
- **Database assertions are mandatory** for any scenario that writes. "DB
  state: ..." must name the table, the key fields, and the expected values.
- **Auth setup is explicit**: either list the fixture/factory that injects a
  test JWT, or write "Auth: none — endpoint is unauthenticated."

## What this file is NOT

- Not a C# file. No `[Fact]`, no `await client.PostAsJsonAsync(...)` as code,
  no `.Should()`. Those live in the C# file produced by `/slice-test-red`.
- Not a replacement for `validation.md`. `validation.md` is for manual
  scenarios and code review. `tests.md` is for one executable outside-in
  scenario set.
- Not a list of unit tests.

## Hard limits

- Do not write C# code inside `tests.md`. Code goes in
  `<Slice>OutsideInTests.cs`.
- Do not mock the slice's own handler, validator, or repository. Those are
  wired real. Mocking them defeats the outside-in principle.
- Do not skip the "DB state" expectation for scenarios that write. Without it,
  the test cannot prove the side effect occurred.
- Do not include UI or browser assertions. The API contract is the HTTP
  response.

## Common mistakes

- Listing many failure scenarios. Pick the most important one. Other failures
  are covered by unit tests and the endpoint integration test.
- Using a handler or command call as the entry point ("Act: `await
  handler.Handle(command, ct)`"). The entry point is HTTP. The test exercises
  the whole stack.
- Forgetting to describe auth setup for authenticated endpoints.
- Deriving status codes from memory instead of consulting `agent_docs/error_handling.md`.
  For example, `ContentNotFoundException` maps to **404** with a `ProblemDetails`
  body — always check the table.
- Listing `WebApplicationFactory` internals (handler types, repository types)
  in "Wired real" as if mocking them. Only genuine external boundaries belong
  in "Mocked."
