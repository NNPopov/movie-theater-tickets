# Error handling

Read this before writing or modifying any handler or repository/adapter.

## Two models coexist (on purpose, not yet unified)

This service uses **both** of the following, and the canonical choice has **not**
been decided. Do not refactor one into the other without explicit user approval.

1. **Exceptions translated centrally.** Domain and application code raise typed
   `*Exception`s; a single `IExceptionHandler` maps each to an HTTP `ProblemDetails`.
2. **The `Result`/`Error` monad** (`Domain/Error/Result.cs`, `Error.cs`,
   `DomainErrors.cs`, `ResultExtensions.cs`) for expected business outcomes. A
   handler may return `Result` / `Result<T>`, and the endpoint resolves it with
   `.Match(onSuccess, onFailure)`.

The same handler may use both (e.g. `return Result.Success();` for the happy path
while `throw new ContentNotFoundException(...)` for a missing aggregate). **When you
touch existing code, match the style already used by that aggregate's handlers.**
When you genuinely have a free choice on new code, prefer the dominant style of the
nearest sibling use-case, and leave a note in `plan.md` rather than introducing a
third pattern.

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
| `ContentNotFoundException` | 204 No Content |
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
- The endpoint resolves it: `result.Match(onSuccess, onFailure)`. In existing code
  the failure branch sometimes **re-raises** a typed exception
  (`if (failure is ConflictError) throw new ConflictException(...)`) so the central
  handler still produces the response — that bridge between the two models is
  current reality, not an ideal; keep it consistent with siblings.
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
