# Error handling

Read this before writing or modifying any handler or repository/adapter.

## The decided hybrid (ADR-002, Accepted 2026-06-04)

The error model is **decided** — see
`docs/adr/ADR-002-error-handling-model-result-vs-exceptions.md`. The two mechanisms
both exist **by design**, split by the *nature* of the outcome, not by aggregate or
by per-slice habit:

| Situation | Mechanism |
|---|---|
| Expected business outcome (seat taken, cart not found, wrong status, cannot purchase) | **`Result` / `Result<T>`** carrying an `Error` |
| In-aggregate state transition that also raises a domain event | **`Result`** (event appended on the success branch only) |
| Structural input validation | **`ValidationBehaviour` + `ValidationException`** (unchanged) |
| Invariant violation / infrastructure fault / "don't know how to handle" | **exception** → `CustomExceptionHandler` → 500 |
| Repository/adapter translating a business-meaningful infra exception (e.g. unique-violation `DbUpdateException`) | **throw** a specific `*Exception` (unchanged) |

Endpoints resolve a `Result` **straight to HTTP** with
`result.Match(onSuccess, ErrorResults.ToProblem)`. The old endpoint **bridge** that
re-threw a failing `Result` as a `*Exception` so `CustomExceptionHandler` could
produce the response **is gone** — a request pays for one error pass, not two.

### Intentional exception tails (not debt)

Some paths deliberately stay exception-based and must **not** be "converted" to
`Result` on sight — they are the right-hand side of the table above, not leftovers:

- **Read/query handlers** that raise `ContentNotFoundException` for a missing
  aggregate (a `GET` returning 404). Queries return a value or throw; they do not
  carry a `Result` failure channel.
- **The shared `MovieSessionSeatService.GetMovieSessionSeat` seat-not-found** helper —
  a missing seat row is a `ContentNotFoundException` (404), not a business outcome of
  the calling command.
- **The `ClientId`-empty invariant** in `ShoppingCart.PurchaseComplete()` — a cart
  reaching completion without a client is a bug (500-class `Ensure.NotEmpty` throw),
  evaluated before the status guards, not an expected `Result` failure.

When you touch existing code, follow the table; do not refactor an intentional tail
into a `Result` (or vice versa) without an ADR.

## The exception hierarchy

Domain exceptions (`Domain/Exceptions/`):

| Exception | Meaning |
|---|---|
| `ContentNotFoundException` | the requested aggregate/row does not exist |
| `ConflictException` | a business conflict (e.g. seat already taken) |
| `DomainValidationException` | an invariant violated inside the domain |
| `LockedException` | a distributed lock could not be acquired |

Application exceptions (`Application/Exceptions/`):

| Exception | Meaning |
|---|---|
| `ValidationException` | FluentValidation failures (thrown by `ValidationBehaviour`) |
| `NotFoundException` | application-level "not found" |
| `ForbiddenAccessException` | the caller may not perform the operation |
| `DuplicateRequestException` | an idempotent request was replayed |

`UnauthorizedAccessException` (BCL) is also handled.

## Central translation: `CustomExceptionHandler`

`API/Infrastructure/CustomExceptionHandler.cs` implements `IExceptionHandler` and
holds a `FrozenDictionary<Type, Func<HttpContext, Exception, Task>>` mapping each
exception type to a writer. Current mapping:

| Exception | HTTP status |
|---|---|
| `ValidationException`, `DomainValidationException` | 400 `ValidationProblemDetails` |
| `ContentNotFoundException` | 404 `ProblemDetails` |
| `NotFoundException` | 404 |
| `UnauthorizedAccessException` | 401 |
| `ForbiddenAccessException` | 403 |
| `ConflictException` | 409 |
| `DuplicateRequestException` | 200 (replay of a completed idempotent request) |
| `LockedException` | 423 |
| anything else (`typeof(Exception)`) | 500, logged as "Internal Server Error" |

Consequences for your code:

- **To introduce a new HTTP status, register a handler here** — never set
  `Response.StatusCode` inside a use-case.
- **The handler logs.** `CustomExceptionHandler` logs every mapped exception
  (`_logger.Error/Warning`). Therefore lower layers **do not** log-and-rethrow; they
  just throw.
- **Unknown exceptions are not swallowed.** They fall through to the
  `typeof(Exception)` branch → 500. That is the desired behaviour for bugs.

## Rules for handlers (use-cases)

- Raise a **specific** domain/application exception for each business failure
  (`throw new ContentNotFoundException(nameof(ShoppingCart), id.ToString())`), or
  return a failing `Result` if that aggregate uses the monad.
- **No `try/catch (Exception)`** to convert arbitrary failures into a domain error.
  Let infrastructure faults propagate.
- A `try/catch` is acceptable only for a **compensating action** — e.g.
  `SelectSeatCommandHandler` catches around the Redis lifecycle call to return the
  seat to "available", then rethrows. The catch performs a rollback and re-throws;
  it does not hide the failure.
- Do not set HTTP status, do not write to `HttpContext`.

## Rules for repositories / adapters (Infrastructure)

- Catch **only** infrastructure exceptions that carry business meaning, and map them
  to the matching domain/application exception:

  ```csharp
  try
  {
      await _dbContext.SaveChangesAsync(cancellationToken);
  }
  catch (DbUpdateException ex) when (IsUniqueViolation(ex))   // business-meaningful
  {
      throw new ConflictException(nameof(MovieSession), ex.Message);
  }
  ```

- **Do not** wrap in `try/catch (Exception)`. A connection drop, a timeout, a
  serialization failure — these propagate unchanged to `CustomExceptionHandler`,
  which logs them and returns 500.
- **Read-only** operations (existence checks, lookups) have **no** `try/catch`. A
  missing row is represented by `null`/empty, which the handler turns into a
  `ContentNotFoundException`; an infrastructure fault propagates.
- Preserve the cause: `throw new ConflictException(...) ` — when the constructor
  allows, pass the inner exception so the stack is not lost.
- Adapters **do not log**. Logging is the exception handler's job.

## Result/Error usage (when the aggregate uses the monad)

- Return `Result.Success()` / `Result.Success(value)` for the happy path and a
  failing `Result` carrying an `Error` (see `Domain/Error/DomainErrors.cs`) for an
  expected business failure.
- The endpoint resolves it with `result.Match(() => Results.Ok(), ErrorResults.ToProblem)`
  (the shared mapper in `API/Endpoints/Common/ErrorResults.cs`: `NotFoundError ⇒ 404`,
  `ConflictError ⇒ 409`, any other `Error ⇒ 500`). The failure branch returns an HTTP
  result directly — it must **not** re-throw a typed exception to be re-caught by
  `CustomExceptionHandler` (ADR-002; the old bridge is removed).
- Do not invent new `Error` codes inside a feature folder. New `Error` definitions
  live in `Domain/Error/DomainErrors.cs` and are an ADR-level decision.

## Checklist before finishing an error path

- [ ] Each business failure raises a **specific** exception (or returns a failing
      `Result`) — never a bare `Exception`/`throw new Exception(...)` in new code.
- [ ] No `catch (Exception)` that hides infrastructure faults.
- [ ] Read-only paths have no `try/catch`.
- [ ] The use-case sets no HTTP status and touches no `HttpContext`.
- [ ] No log-and-rethrow in lower layers (the central handler logs).
- [ ] If a new status/error type is needed, it is added to `CustomExceptionHandler`
      / `DomainErrors` and flagged as an ADR, not buried in the slice.
