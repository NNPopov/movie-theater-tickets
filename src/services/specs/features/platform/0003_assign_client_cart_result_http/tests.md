# 0003 · AssignClientCart Result→HTTP — Outside-in test spec

> **Deviation from the default template (intentional, per PRD — same as slice `0002`).** The
> standard outside-in test for this project goes through `WebApplicationFactory<Program>`. **No
> such harness exists** in this repository (the suites are `Domain.UnitTests`,
> `Domain.ArchitectureTests`, `Infrastructure.UnitTests`, `Application.LoadTests`, and the
> `API.UnitTests` project created by `0002`). The PRD's Testing Decisions explicitly choose **a
> focused unit spec of the new shared `Error → IResult` mapper (`ErrorResults`) as the
> acceptance/RED gate** instead of standing one up — the direct analogue of `0002`'s
> `CustomExceptionHandler` gate. The "entry point" below is therefore the mapper's returned
> `IResult` executed against a `DefaultHttpContext`, exercising the **whole `Result`-to-HTTP
> translation unit** (the `Error`-subtype switch + `Results.Problem` + `ProblemDetails`
> serialization). This is the slice's single load-bearing, externally-observable, reusable
> behaviour. The endpoint's `Match` wiring is covered by compilation; the handler and the domain
> transition are pinned by unit tests (plan §6), not by this gate.

## Goal

When the shared mapper translates a failing `Result`'s `Error`, the produced HTTP response is the
correct status with a `ProblemDetails` body identical in shape to what `CustomExceptionHandler`
emits for the same status: `NotFoundError ⇒ 404`, `ConflictError ⇒ 409`, any unrecognised
`Error ⇒ 500` — so the `Result` path and the exception path are indistinguishable to clients.

## Entry point

Not an HTTP route via `WebApplicationFactory`. The test invokes the mapper directly and executes
its result:

- **Method under test:** `ErrorResults.ToProblem(Error)` (in `API/Endpoints/Common/`), returning
  an `IResult`.
- **Execution:** `await result.ExecuteAsync(httpContext)` against a `DefaultHttpContext` whose
  `Response.Body` is a seekable `MemoryStream`, so the written JSON status + body can be read back
  (same technique as `0002`'s gate).
- **Error inputs:** a `NotFoundError` (scenario 1), a `ConflictError` (scenario 2), and a plain
  unrecognised `Error` (scenario 3), each constructed with a recognizable `Code`/`Description`.
- **Headers / auth / idempotency:** none — this is a translation-unit test, not a routed call.

## Wired real

- `ErrorResults.ToProblem` (the real mapper, real `Error`-subtype switch).
- `Results.Problem(...)` `IResult` and its real `ExecuteAsync` against a `DefaultHttpContext`
  (real `HttpResponse`, real status-code and body writing).
- Real `System.Text.Json` serialization of the `ProblemDetails` payload.
- The real `Error` / `NotFoundError` / `ConflictError` types from `Domain/Error`.

This is the full `Result`-to-HTTP translation unit; it is **not** mocked.

## Mocked

- None — the mapper has no dependencies (no logger, no DB, no Redis, no RabbitMQ, no clock). The
  test runs entirely in-process against a `DefaultHttpContext`.

## Fixtures / setup

- A helper that builds a `DefaultHttpContext` with `Response.Body = new MemoryStream()`.
- After `await ErrorResults.ToProblem(error).ExecuteAsync(httpContext)`, rewind the stream
  (`Position = 0`) and deserialize the body to a `ProblemDetails` (Web `JsonSerializerOptions`,
  as in `0002`) to assert its fields.
- Auth: none — the unit under test has no authentication.

## Test scenarios

### Scenario 1: NotFoundError ⇒ 404 + ProblemDetails (part of the RED gate)

**Setup:**
- `httpContext` with a `MemoryStream` response body.
- `error = new NotFoundError("ShoppingCart.NotFound", "Shopping cart not found")`.

**Act:**
- `await ErrorResults.ToProblem(error).ExecuteAsync(httpContext);`

**Expect:**
- `httpContext.Response.StatusCode == 404`.
- The body deserializes to a `ProblemDetails` with:
  - `Status == 404`,
  - `Type == "https://tools.ietf.org/html/rfc7231#section-6.5.4"`,
  - `Title == "The specified resource was not found."`,
  - `Detail == error.Description` (`"Shopping cart not found"`).
- Shape identical to `CustomExceptionHandler`'s `ContentNotFoundException`/`NotFoundException`
  `404` body.

**Covers requirement(s):** F4, F7.

### Scenario 2: ConflictError ⇒ 409 + ProblemDetails (part of the RED gate)

**Setup:**
- Fresh `httpContext` with a `MemoryStream` response body.
- `error = new ConflictError("ShoppingCart.ConflictException", "Active Shopping cart already exists")`.

**Act:**
- `await ErrorResults.ToProblem(error).ExecuteAsync(httpContext);`

**Expect:**
- `httpContext.Response.StatusCode == 409`.
- The body deserializes to a `ProblemDetails` with:
  - `Status == 409`,
  - `Type == "https://tools.ietf.org/html/rfc7231#section-6.5.8"`,
  - `Title == "Conflict"`,
  - `Detail` is **null/absent** (mirrors `HandleConflictException`, which sets no `Detail`).
- Shape identical to `CustomExceptionHandler`'s `ConflictException` `409` body.

**Covers requirement(s):** F5, F7.

### Scenario 3: unrecognised Error ⇒ 500 + ProblemDetails (part of the RED gate)

**Setup:**
- Fresh `httpContext` with a `MemoryStream` response body.
- `error = new Error("Some.Unmapped", "boom")` (a base `Error`, neither `NotFoundError` nor
  `ConflictError`).

**Act:**
- `await ErrorResults.ToProblem(error).ExecuteAsync(httpContext);`

**Expect:**
- `httpContext.Response.StatusCode == 500`.
- The body deserializes to a `ProblemDetails` with:
  - `Status == 500`,
  - `Type == "https://tools.ietf.org/html/rfc7231#section-6.6.1"`,
  - `Title == "Internal Server Error"`.
- Shape identical to `CustomExceptionHandler`'s `HandleException` `500` body; preserves the
  former bare-`throw new Exception(...)` collapse-to-500 behaviour.

**Covers requirement(s):** F6, F7.

> All three scenarios are **RED** against the current code: `ErrorResults` does not exist yet, so
> the test fails to compile until the mapper is created, then the assertions pin its shape.

## Out of scope for this test

- The handler behaviour — cart missing ⇒ `NotFoundError`; other active cart ⇒ `ConflictError`;
  success ⇒ `Result.Success()` and owner equals the client id (bug fix); domain `IsFailure`
  propagated — covered by `AssignClientCartCommandHandlerTests` (plan §6).
- The domain transition — `ShoppingCart.AssignClientId` already-assigned ⇒ `ConflictError` with
  no event; success ⇒ owner assigned + event raised — covered by the `ShoppingCart` domain unit
  test (plan §6).
- The endpoint's `Match` wiring and the deletion of the bridge / bare throw (F1, F2, F3) — covered
  by compilation + code review (validation.md).
- The `.Produces(...)` OpenAPI declarations (F15, F16) — verified by code review.
- The end-to-end routing of `PUT .../assignclient` to `200`/`404`/`409` — no `WebApplicationFactory`
  harness; verified manually (validation.md scenarios) and by the mapper gate.
- Field-level validation, `DbUpdateException` translation, performance/load/concurrency.
