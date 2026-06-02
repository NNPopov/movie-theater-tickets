# Testing

Read this before writing any test. The stack is **xUnit + FluentAssertions**, with
**NetArchTest** for architecture rules and **`WebApplicationFactory<Program>`** for
HTTP-level tests.

## Test projects

| Project | Scope |
|---|---|
| `BookingManagement/tests/BookingManagementService.Domain.UnitTests` | aggregates, value objects, domain services, validators — pure, no I/O |
| `tests/BookingManagementService.Domain.ArchitectureTests` | NetArchTest structural rules |
| `tests/BookingManagementService.Infrastructure.UnitTests` | repositories/adapters and cache/lock services |
| `tests/CinemaTicketBooking.Application.LoadTests` | end-to-end / load (real HTTP) |

Endpoint integration tests and slice outside-in tests go in a test project that
boots the app via `WebApplicationFactory<Program>`. If no such project exists yet
for the area you are working on, create one (xUnit + FluentAssertions +
`Microsoft.AspNetCore.Mvc.Testing`) and say so in the slice's `plan.md` — do not
silently skip the level.

## Conventions

- **Naming:** existing tests use either `<Subject>Specification` or `<Subject>Test`
  classes with `[Fact]` / `[Theory]` methods. Match the sibling files in the folder
  you are adding to. Method names read as a sentence:
  `Method_Should_DoX_When_Y`.
- **Assertions:** FluentAssertions (`result.Should().BeTrue()`,
  `act.Should().ThrowAsync<ConflictException>()`). Do not mix in raw `Assert.*`
  within a file that uses FluentAssertions.
- **Mocking:** use the mocking library already referenced by the test project
  (check its `.csproj` / `GlobalUsings.cs`); do not introduce a second one.
- **Async:** every test that touches `async` code is `async Task` and awaits;
  assert on async throwing with `await act.Should().ThrowAsync<T>()`.
- **English** for all test names, comments, and messages.

## The four levels for a slice

### 1. Handler unit test (Application)

Construct the handler with **mocked** repositories/services. Drive each branch:

- happy path → the expected `Result.Success()` / response, and the expected calls
  on the aggregate and repositories (`Verify`/`Received`);
- each business failure → the specific exception or failing `Result`
  (`ContentNotFoundException` when the repository returns `null`, `ConflictException`
  on a conflicting state, etc.).

Do **not** hit a database here; the repository is a mock. Assert on returned
`Result`/exception and on interactions, not on internal fields.

### 2. Repository / adapter unit test (Infrastructure)

Validate the **error-translation contract** from `error_handling.md`:

- a business-meaningful infrastructure exception (e.g. a unique-violation
  `DbUpdateException`) is translated to the right domain/application exception;
- **other** infrastructure exceptions propagate **unchanged** (assert that the
  original type bubbles out — proving the adapter does not over-catch);
- read paths return `null`/empty for a missing row rather than throwing.

Use an in-memory/SQLite EF Core provider or a real test Postgres as the sibling
tests do; prefer a real Postgres (or Testcontainers) when the behaviour under test
is provider-specific (unique indexes, concurrency tokens).

### 3. Endpoint integration test (`WebApplicationFactory<Program>`)

Boot the app and call it over HTTP with `factory.CreateClient()`:

- routing and binding (route/query/body/header binding, the `X-Idempotency-Key`
  header where required);
- status codes and response shapes, including the `CustomExceptionHandler`
  translations (e.g. a missing cart → **204**, a conflict → **409**, a validation
  failure → **400 `ValidationProblemDetails`**);
- auth: configure a test JWT / authentication scheme for `[RequireAuthorization]`
  endpoints.

Point the factory at a **test database** and reset state between tests
(respawn/truncate or a transaction per test). Do not run integration tests against a
shared dev database.

### 4. Outside-in acceptance test (the gate)

One test class per slice, `<Slice>OutsideInTests`, exercising the slice end-to-end
**through HTTP** with the real handler, real repository, and test database; mock only
true external boundaries (third-party HTTP, sometimes Redis, the clock). This is the
RED→GREEN acceptance gate produced by `/slice-test-red`. It:

- calls the endpoint via the `WebApplicationFactory` client;
- asserts the HTTP status and response body;
- asserts the **database side effect** for any write (query the row back);
- does **not** reference the handler, command, or repository types directly — it is
  black-box over HTTP.

```csharp
public class SelectSeatOutsideInTests : IClassFixture<BookingApiFactory>
{
    private readonly BookingApiFactory _factory;
    public SelectSeatOutsideInTests(BookingApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Selecting_a_free_seat_succeeds_and_persists()
    {
        var client = _factory.CreateClient();
        // arrange: seed a movie session + cart via fixtures/SQL
        // act:
        var response = await client.PostAsJsonAsync(
            $"/api/shoppingcarts/{cartId}/seats/select",
            new { Row = (short)1, Number = (short)1, ShowtimeId = sessionId });
        // assert: status + DB state
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        // query the seat back and assert it is reserved for this cart
    }
}
```

## Architecture tests

`BookingManagementService.Domain.ArchitectureTests` already enforces:

- `Domain` has no dependency on `Application`;
- aggregate roots have a private parameterless constructor;
- domain events are `sealed` and named `*DomainEvent`.

When you add a structural rule the team wants guaranteed (e.g. "handlers live under
`Application/<Aggregate>/Command|Queries`"), add a NetArchTest `[Fact]` here rather
than relying on review.

## When a level may be skipped

A level may be opted out **only** when it would assert nothing:

- **Handler unit test** — skip only if the handler has no branches and no logic
  beyond delegating to one repository call (rare).
- **Repository test** — skip only if the repository does no error translation and is
  a straight pass-through query.
- **Endpoint integration test** — skip only if an equivalent assertion is already
  made by the outside-in test for the same endpoint.
- **Outside-in test** — **never skipped.** It is the acceptance gate.

State any opt-out explicitly in `plan.md` with the reason. The default is no
opt-outs.

## Running

```
dotnet test CinemaBookingManagement.sln                       # everything
dotnet test --filter "FullyQualifiedName~<Slice>OutsideInTests"   # one slice's gate
dotnet test tests/BookingManagementService.Domain.ArchitectureTests   # just arch rules
```
