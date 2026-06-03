---
name: slice-test-red
description: This skill should be used when the user wants to generate the executable C# outside-in test for a slice from its tests.md specification. Trigger when the user invokes /slice-test-red, says "generate the red test", "write the outside-in C# test", or asks to translate tests.md into a runnable xUnit test class. Use only after tests.md exists. Reads tests.md plus architectural context, writes <Slice>OutsideInTests.cs, runs dotnet test, and verifies the test is RED.
disable-model-invocation: false
---

# slice-test-red

Translate a slice's `tests.md` markdown specification into an executable
C# xUnit outside-in test class, place it under
`tests/...IntegrationTests/Features/<Aggregate>/<NNNN>_<slice>/`,
run `dotnet test --filter "FullyQualifiedName~<Slice>OutsideInTests"` from
`src/services`, and verify the test is **red** — failing as expected because
the slice's implementation does not yet exist.

This is the "red" phase of outside-in TDD. The next step after this skill is
implementation, which proceeds until the same test turns green.

## Process

### 1. Find the target slice

Same determination as in `/feature-tests`:

1. User-named slice.
2. Most recently modified `tests.md` under `specs/features/*/*/`.
3. If ambiguous, ask.

### 2. Read the inputs

- `specs/features/<aggregate>/<NNNN>_<slice>/tests.md` — primary input.
- `specs/features/<aggregate>/<NNNN>_<slice>/plan.md` — for class names, route
  paths, request/response shapes, and the aggregate name.
- `agent_docs/testing.md` — for `WebApplicationFactory<Program>` conventions,
  fixture patterns, DB assertion approach.
- `agent_docs/entry_points/minimal-api.md` — for route patterns and
  `IEndpoints` structure.
- `agent_docs/error_handling.md` — for the exception→HTTP status table; assert
  on documented statuses, never invent them.
- Existing `*OutsideInTests.cs` files in the same aggregate's test folder (if
  any) — for the factory class name, namespace, and seeding patterns the team
  uses.

If `tests.md` is missing, stop and tell the user to run `/feature-tests` first.

### 3. Decide the test file path

```
tests/...IntegrationTests/Features/<Aggregate>/<NNNN>_<slice>/<Slice>OutsideInTests.cs
```

`<Slice>` is the PascalCase version of the slice folder name (e.g.
`0003_select_seat` → `SelectSeat`, class `SelectSeatOutsideInTests`).

If the integration-test project does not yet exist, note it clearly and
instruct the user to create it (xUnit + FluentAssertions +
`Microsoft.AspNetCore.Mvc.Testing`) before running the skill again.

### 4. Generate the C# code

Structure of the produced file:

1. A `using` section with only the packages the test needs. Do **not** import
   the slice's command, handler, repository, or any `Application`/
   `Infrastructure` type. The test is black-box over HTTP.
2. The namespace: mirror the test project's existing namespace pattern (e.g.
   `CinemaTicketBooking.IntegrationTests.Features.<Aggregate>`).
3. One `public class <Slice>OutsideInTests : IClassFixture<BookingApiFactory>`
   (or the factory class already used in the project).
4. A constructor that accepts `BookingApiFactory factory` and stores it.
5. One `[Fact] public async Task …` per scenario in `tests.md`, in the same
   order. Method names read as a sentence in snake_PascalCase or the style
   already used by sibling tests.
6. For each scenario: arrange (seed via raw SQL or the factory's seeding
   helpers), act (`await client.PostAsJsonAsync(...)` / `GetAsync` / etc.),
   assert (HTTP status + response body via FluentAssertions; then DB-state
   assertion for writes via raw SQL through the factory's connection string —
   do not import EF entity types).

Example skeleton (adapt names, route, and request shape from `plan.md`):

```csharp
using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace CinemaTicketBooking.IntegrationTests.Features.ShoppingCarts;

public class SelectSeatOutsideInTests : IClassFixture<BookingApiFactory>
{
    private readonly BookingApiFactory _factory;

    public SelectSeatOutsideInTests(BookingApiFactory factory)
        => _factory = factory;

    [Fact]
    public async Task Selecting_a_free_seat_returns_200_and_persists_reservation()
    {
        // Arrange: seed a movie session and a shopping cart via raw SQL or
        // the factory's seed helpers so the slice has valid preconditions.
        var client = _factory.CreateClient();
        var cartId = Guid.NewGuid();   // replace with seeded value
        var sessionId = Guid.NewGuid(); // replace with seeded value

        // Act
        var response = await client.PostAsJsonAsync(
            $"/api/shoppingcarts/{cartId}/seats/select",
            new { Row = (short)1, Number = (short)1, ShowtimeId = sessionId });

        // Assert — HTTP status
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Assert — DB side effect (raw SQL, no EF entity import)
        await using var conn = _factory.OpenDbConnection();
        var row = await conn.QuerySingleOrDefaultAsync(
            "SELECT * FROM shopping_cart_seats WHERE cart_id = @cartId AND row = 1 AND number = 1",
            new { cartId });
        row.Should().NotBeNull();
    }

    [Fact]
    public async Task Selecting_an_already_taken_seat_returns_409()
    {
        // Arrange: seed the seat as already reserved.
        var client = _factory.CreateClient();
        var cartId = Guid.NewGuid();   // replace with seeded value
        var sessionId = Guid.NewGuid(); // replace with seeded value

        // Act
        var response = await client.PostAsJsonAsync(
            $"/api/shoppingcarts/{cartId}/seats/select",
            new { Row = (short)1, Number = (short)1, ShowtimeId = sessionId });

        // Assert — CustomExceptionHandler maps ConflictException → 409
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }
}
```

Notes on the code:

- Use the factory class already present in the integration-test project (e.g.
  `BookingApiFactory`). Check sibling tests before declaring a class name.
- DB assertions use raw SQL (Dapper or `NpgsqlCommand`) through a connection
  opened from the factory's connection string. Do **not** reference EF Core
  `DbContext` or entity types in the test — that would couple the test to
  internal ORM details.
- Error-path assertions use the statuses from the `CustomExceptionHandler`
  table in `agent_docs/error_handling.md`:
  - `ContentNotFoundException` → 404 Not Found (`ProblemDetails`)
  - `ConflictException` → 409 Conflict
  - `ValidationException` / `DomainValidationException` → 400 ValidationProblemDetails
  - `NotFoundException` → 404
  - `LockedException` → 423
  - `DuplicateRequestException` → 200
  Do **not** assert on invented status codes or response shapes.
- For endpoints protected by `[RequireAuthorization]`, configure a test JWT
  scheme in the factory and include the bearer token in the request. Follow
  the pattern used by sibling integration tests.
- The test does **not** reference `ISender`, the command record, the handler
  class, or any repository interface.

### 5. Verify the test is RED

After writing the C# file:

1. Run `dotnet test --filter "FullyQualifiedName~<Slice>OutsideInTests"` from
   `src/services` and capture the output.
2. Verify the test **fails**. Acceptable kinds of failure:
   - **Build failure** because the slice's command/handler/endpoint type
     does not exist yet. This is the expected red state at the start of TDD.
   - **404 or 405** because the route is not registered. Also expected.
   - **AssertionError** (FluentAssertions) because the response is wrong
     (e.g. a stub exists but logic is incomplete). Expected when the slice
     has skeletons.

If the test **passes**: something is wrong. Either the slice is already
implemented (and this is not a fresh red), or the test does not actually check
what it should. Stop and tell the user — do not declare red.

If the test fails with a **fixture error**, **DI configuration error**,
**DB connection error**, or any **environment problem**: this is not an
acceptable red. Fix the test or the factory setup; do not ship it. The red
must be from missing **implementation**, not from a broken test harness.

### 6. Report

Output to the user, in this exact order:

1. **Path of the new test file.**
2. **Each scenario from tests.md**, mapped to its `[Fact]` method name.
3. **Exact failure mode** (build error / 404 / assertion / etc.) with a
   one-line excerpt from the `dotnet test` output.
4. **Confirmation** that the test is in the expected red state.
5. **The implementation prompt block** described below.

### The implementation prompt block

The next session — implementation — is a separate conversation. To save the
user from re-typing the same prompt every slice, output a ready-to-copy block
at the end of the report and **also write it as a file**.

**File to write:** `specs/features/<aggregate>/<NNNN>_<slice>/implement.md`
Write only the prompt text (no separator lines, no Markdown headers). The file
content is the plain text between the separator lines — the same text the user
would paste into the next chat.

Then output the block **verbatim** in this shape in the chat, replacing
`<aggregate>`, `<NNNN>`, `<slice>`, and `<Slice>` with concrete values for
the slice you just produced the test for:

```
─────────────────────────────────────────────────────────────────
COPY THIS PROMPT FOR THE IMPLEMENTATION SESSION
─────────────────────────────────────────────────────────────────

Implement slice <slice>. All specs and the red outside-in test are ready.

Sources (read in this order):
- specs/features/<aggregate>/<NNNN>_<slice>/plan.md
- specs/features/<aggregate>/<NNNN>_<slice>/requirements.md
- specs/features/<aggregate>/<NNNN>_<slice>/tests.md
- specs/features/<aggregate>/<NNNN>_<slice>/validation.md

Acceptance gate:
- tests/...IntegrationTests/Features/<Aggregate>/<NNNN>_<slice>/<Slice>OutsideInTests.cs
  must turn GREEN (dotnet test --filter "FullyQualifiedName~<Slice>OutsideInTests").
- Do not touch the test file. If the test fails due to a bug in the
  implementation — fix the implementation, not the test.
- If the test fails due to a defect in the test itself — stop and ask,
  do not silently fix it.

Once the outside-in test is green — write the missing unit tests per the
plan (see plan.md section "Tests planned" and agent_docs/testing.md).

Quality gates before completion (run from src/services):
- dotnet format CinemaBookingManagement.sln
- dotnet build  CinemaBookingManagement.sln -warnaserror
- dotnet test   CinemaBookingManagement.sln
  (includes BookingManagementService.Domain.ArchitectureTests)

If schema changed, also run:
- dotnet ef migrations add <Name> -p BookingManagement/BookingManagementService.Infrastructure -s BookingManagement/BookingManagementService.API
- dotnet ef database update      -p BookingManagement/BookingManagementService.Infrastructure -s BookingManagement/BookingManagementService.API

All must pass with no new warnings or architecture-test failures.

─────────────────────────────────────────────────────────────────
```

Rules for the block:

- **Exact paths**, not placeholders. By the time you produce this block you
  already know `<aggregate>`, `<NNNN>`, `<slice>`, `<Slice>` — substitute them.
- **No Markdown formatting** inside the block (no headers, no bold). The user
  pastes it raw into the next chat.
- **One copy of the block per run.** Do not output it twice.
- **English wording** throughout the block.
- **Write `implement.md` before outputting the block in the chat.** If the
  file write fails, report the error but still output the block.
- The block goes **last** in your reply. Nothing after it.

If for any reason the test is **not** in a verified red state (passing, or
failing on a fixture/environment error), do **not** output the implementation
prompt block and do **not** write `implement.md`. The block presupposes a valid
red state; emitting it on a broken test would mislead the user into starting
implementation against a test that does not represent the contract.

## Style rules

- **English only** in test names, XML doc comments, and inline comments.
- **One `[Fact]` per scenario** in `tests.md`, method name reads as a sentence.
- **No imports of the slice's internals** (command, handler, validator,
  repository, EF entity). The test exercises HTTP only.
- **DB assertions through raw SQL**, not through EF Core entity imports.
- **FluentAssertions** for all assertions; do not mix in raw `Assert.*`.
- **`async Task`** for every test method that touches async code; await all
  async calls.

## Hard limits

- No generating a test that passes. The whole point is red.
- No calling the handler, command, or repository directly instead of going
  through HTTP. Outside-in means the boundary is HTTP.
- No mocking the slice's repository, handler, or command. These must be wired
  real in the `WebApplicationFactory`.
- No skipping the `dotnet test` run. The skill is not complete until the red
  state is observed and reported.
- No saving the test outside the slice's designated test folder.

## Common mistakes

- Forgetting `IClassFixture<BookingApiFactory>` on the test class — the factory
  is not shared and the test DB is reset between runs.
- Using the wrong factory class name — check sibling tests before inventing one.
- Asserting on `response.IsSuccessStatusCode` instead of `.StatusCode.Should().Be(HttpStatusCode.X)` — the former hides which code was returned.
- DB assertion that imports an EF Core entity type — use raw SQL.
- Asserting the wrong status for a missing aggregate: the project maps
  `ContentNotFoundException` → 404 with a `ProblemDetails` body. Always consult the
  `CustomExceptionHandler` table in `agent_docs/error_handling.md`.
- Reporting "test is red" without showing the failure mode. The kind of failure
  (build error vs 404 vs assertion) tells the user where to start implementing.
- Using `Task.Result` or `.GetAwaiter().GetResult()` instead of `await`.
- Opening a direct `NpgsqlConnection` using a hard-coded connection string
  instead of obtaining it from the factory's configuration.
