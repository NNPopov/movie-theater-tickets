# 0002 · ContentNotFound404 — Outside-in test spec

> **Deviation from the default template (intentional, per PRD).** The standard outside-in test
> for this project goes through `WebApplicationFactory<Program>`. **No such harness exists** in
> this repository (the suites are `Domain.UnitTests`, `Domain.ArchitectureTests`,
> `Infrastructure.UnitTests`, `Application.LoadTests`), and the PRD's Testing Decisions
> explicitly choose **a focused `CustomExceptionHandler` unit test as the acceptance/RED gate**
> instead of standing one up — consistent with how slice `0001` closed. The "entry point" below
> is therefore `CustomExceptionHandler.TryHandleAsync` driven against a `DefaultHttpContext`,
> exercising the **whole central translation unit** (the dispatch dictionary + the writer +
> `ProblemDetails` serialization). This is the slice's single load-bearing externally-observable
> behaviour. The two carve-out behaviours are pinned by handler unit tests (see
> `agent_docs/testing.md` levels; specified in `plan.md` §6), not by this gate.
>
> **Status note:** statuses below use the **post-change** mapping
> (`ContentNotFoundException → 404`). The spec-chain skills still carry the stale `→ 204` text;
> this slice corrects them. The PRD/plan are the source of truth here.

## Goal

When `CustomExceptionHandler` translates a `ContentNotFoundException`, the HTTP response is
`404 Not Found` carrying a `ProblemDetails` body identical in shape to the one produced for
`NotFoundException` — and `NotFoundException` itself still maps to `404` (regression).

## Entry point

Not an HTTP route via `WebApplicationFactory`. The test invokes the central handler directly:

- **Method under test:** `CustomExceptionHandler.TryHandleAsync(HttpContext, Exception, CancellationToken)`
- **HttpContext:** a `DefaultHttpContext` whose `Response.Body` is a seekable `MemoryStream` so
  the written JSON can be read back.
- **Exception input:** an instance of `ContentNotFoundException` (scenario 1) /
  `NotFoundException` (scenario 2), constructed with a recognizable message.
- **Headers / auth / idempotency:** none — this is a translation-unit test, not a routed call.

## Wired real

- `CustomExceptionHandler` (the real class, real `FrozenDictionary` dispatch and writers).
- `DefaultHttpContext` (real `HttpResponse`, real status-code and body writing).
- Real `System.Text.Json` / `WriteAsJsonAsync` serialization of `ProblemDetails`.

This is the full central-translation unit; it is **not** mocked.

## Mocked

- **Serilog `ILogger`** (the handler's only constructor dependency): an NSubstitute substitute,
  so the test does not depend on a configured logger. The test does not assert on logging (log
  behaviour is checked by code review per F5).

No database, no Redis, no RabbitMQ, no clock — none are touched by `CustomExceptionHandler`.

## Fixtures / setup

- Construct `new CustomExceptionHandler(loggerSubstitute)`.
- Construct `var httpContext = new DefaultHttpContext();` and assign
  `httpContext.Response.Body = new MemoryStream();`.
- After `TryHandleAsync` returns, rewind the stream (`Position = 0`) and deserialize the body to
  a `ProblemDetails` (or a `JsonDocument`) to assert its fields.
- Auth: none — the unit under test has no authentication.

## Test scenarios

### Scenario 1: ContentNotFoundException ⇒ 404 + ProblemDetails (the RED gate)

**Setup:**
- `loggerSubstitute` created; `httpContext` with a `MemoryStream` response body.
- `exception = new ContentNotFoundException("Movie", "00000000-0000-0000-0000-000000000000")`
  (or equivalent; the message is what surfaces as `Detail`).

**Act:**
- `await handler.TryHandleAsync(httpContext, exception, CancellationToken.None);`

**Expect:**
- Return value: `true` (handled).
- `httpContext.Response.StatusCode == 404`.
- The response body deserializes to a `ProblemDetails` with:
  - `Status == 404`,
  - `Type == "https://tools.ietf.org/html/rfc7231#section-6.5.4"`,
  - `Title == "The specified resource was not found."`,
  - `Detail == exception.Message` (non-empty).
- No state outside `httpContext` is mutated (no DB — n/a).

**Covers requirement(s):** F1, F2, F3.

> This scenario is **RED** against the current code: `HandleContentNotFoundException` currently
> sets `204` and calls `Response.CompleteAsync()` with no body, so the status assertion (404)
> and the body deserialization both fail.

### Scenario 2: NotFoundException still ⇒ 404 (regression)

**Setup:**
- Fresh `httpContext` with a `MemoryStream` response body.
- `exception = new NotFoundException("ShoppingCart", "00000000-0000-0000-0000-000000000000")`
  (use the existing `NotFoundException` constructor shape).

**Act:**
- `await handler.TryHandleAsync(httpContext, exception, CancellationToken.None);`

**Expect:**
- `httpContext.Response.StatusCode == 404`.
- The body deserializes to a `ProblemDetails` with `Status == 404`,
  `Type == "https://tools.ietf.org/html/rfc7231#section-6.5.4"`,
  `Title == "The specified resource was not found."`, and a `Detail`.
- The shape matches Scenario 1's body (parity between the two not-found exceptions).

**Covers requirement(s):** F4 (and F3 parity).

> This scenario is **GREEN** before and after the change — it guards against the central flip
> accidentally altering the existing `NotFoundException` mapping.

## Out of scope for this test

- The two carve-out behaviours (`current` cart `null ⇒ 204` / inconsistent `⇒ 404`; movie
  sessions empty `⇒ 200 []`) — covered by `GetCurrentShoppingCartQueryHandlerTests` and
  `GetMovieSessionsQueryHandlerTests` (plan §6).
- The `.Produces(...)` OpenAPI declarations (F12, F15) — verified by code review (validation.md).
- The end-to-end routing of each addressed-resource read path to `404` — derived from the
  central mapping (F6); no per-path HTTP test (no `WebApplicationFactory` harness).
- Field-level validation, `DbUpdateException` translation, performance/load/concurrency.
- Assertions on logging (F5) — verified by code review.
