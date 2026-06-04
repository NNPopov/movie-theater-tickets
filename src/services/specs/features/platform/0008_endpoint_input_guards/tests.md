# 0008 · EndpointInputGuards — Outside-in test spec

> **Deviation from the default template (intentional, per PRD/plan, precedent `0002`).**
> The standard outside-in test goes through `WebApplicationFactory<Program>`. **No such
> harness exists** in this repository, and the PRD's Testing Decisions choose **focused unit
> tests as the acceptance/RED gate** instead of standing one up — exactly as slice `0002`
> closed. The slice's externally-observable change (a malformed `X-Idempotency-Key` ⇒ `400`,
> a non-Guid/missing `nameidentifier` claim ⇒ `401`, both formerly `500`) is observable
> through two seams: the **two `internal static` guards** that replace the bare throws
> (`ParseIdempotencyKey`, `GetClientId`), and the **central `CustomExceptionHandler`
> translation** of the exceptions they raise. The gate exercises both. There is no MediatR
> handler, repository, domain, or DB involved.
>
> **Status note:** statuses below are derived from the **existing**
> `agent_docs/error_handling.md` mapping table (`DomainValidationException → 400`,
> `UnauthorizedAccessException → 401`). This slice adds no mapping.

## Goal

A request with a malformed `X-Idempotency-Key` header, or an authenticated request whose
`nameidentifier` claim is not a `Guid`, is rejected with a **specific typed exception**
(`DomainValidationException` / `UnauthorizedAccessException`) — not a bare `Exception` — and
that exception is translated by `CustomExceptionHandler` to **HTTP `400`** / **`401`**
respectively (was `500`), while a valid key/claim is parsed and the request proceeds.

## Entry point

Not an HTTP route via `WebApplicationFactory`. Two units under test:

- **The guards** (the new seams that replace the bare throws), called directly:
  - `ShoppingCartEndpointApplicationBuilderExtensions.ParseIdempotencyKey(string requestId)`
    → `Guid` (throws `DomainValidationException`).
  - `ShoppingCartEndpointApplicationBuilderExtensions.GetClientId(ClaimsPrincipal user)`
    → `Guid` (throws `UnauthorizedAccessException`). The test builds a `ClaimsPrincipal`
    with a `ClaimsIdentity` carrying (or omitting) the
    `http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier` claim.
- **The central translation**:
  `CustomExceptionHandler.TryHandleAsync(HttpContext, Exception, CancellationToken)` driven
  against a `DefaultHttpContext` whose `Response.Body` is a seekable `MemoryStream`, given a
  `DomainValidationException` / `UnauthorizedAccessException`.

- **Headers / auth / idempotency:** none at the test level — the guards take a raw `string` /
  a constructed `ClaimsPrincipal`; the translation test takes a constructed exception.

## Wired real

- `ShoppingCartEndpointApplicationBuilderExtensions.ParseIdempotencyKey` /
  `GetClientId` (the real `internal static` guards, visible via `InternalsVisibleTo`).
- `CustomExceptionHandler` (the real class, real `FrozenDictionary` dispatch and writers).
- `DefaultHttpContext` (real `HttpResponse`, real status-code and body writing) and real
  `System.Text.Json` / `WriteAsJsonAsync` serialization of `ProblemDetails` /
  `ValidationProblemDetails`.
- Real `ClaimsPrincipal` / `ClaimsIdentity` construction for the `GetClientId` test.

## Mocked

- **Serilog `ILogger`** (the `CustomExceptionHandler`'s only constructor dependency): an
  NSubstitute substitute. The test does not assert on logging.

No database, no Redis, no RabbitMQ, no clock — none are touched by the guards or the handler.

## Fixtures / setup

- Guards: call the static methods directly. For `GetClientId`, build
  `new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier", <value>) }))`
  — or an identity with no such claim for the "absent" case.
- Translation: `new CustomExceptionHandler(loggerSubstitute)`;
  `var httpContext = new DefaultHttpContext(); httpContext.Response.Body = new MemoryStream();`.
  After `TryHandleAsync`, rewind (`Position = 0`) and deserialize the body to assert fields.
- Auth: none — the units have no authentication; the claim is supplied directly.

## Test scenarios

### Scenario 1: malformed `X-Idempotency-Key` ⇒ `DomainValidationException` ⇒ `400` (RED gate)

**Setup:**
- No state. Inputs: `"not-a-guid"` and the empty string `""`.
- For the translation half: `loggerSubstitute`; `httpContext` with a `MemoryStream` body;
  `exception = new DomainValidationException("Invalid idempotency key: not-a-guid")`.

**Act:**
- `ParseIdempotencyKey("not-a-guid")` and `ParseIdempotencyKey("")`.
- `await handler.TryHandleAsync(httpContext, exception, CancellationToken.None);`

**Expect:**
- `ParseIdempotencyKey` **throws `DomainValidationException`** for both inputs.
- `TryHandleAsync` returns `true`; `httpContext.Response.StatusCode == 400`.
- The body deserializes to a `ValidationProblemDetails` with `Status == 400` and
  `Type == "https://tools.ietf.org/html/rfc7231#section-6.5.1"`.
- No state outside `httpContext` is mutated (no DB — n/a).

**Covers requirement(s):** F2, F5 (and F3/F4 by derivation — the endpoints call this guard).

> **RED** against current code: `ParseIdempotencyKey` does not yet exist (the parse is an
> inline bare `throw new Exception(...)` / `return Results.BadRequest()`), so the guard half
> does not compile/throw the right type until the extraction in `plan.md` §5. The
> translation half (`DomainValidationException ⇒ 400`) is **GREEN before and after** — a
> characterization guard that the mapping the slice relies on is intact.

### Scenario 2: non-Guid / missing `nameidentifier` claim ⇒ `UnauthorizedAccessException` ⇒ `401` (RED gate)

**Setup:**
- A `ClaimsPrincipal` whose `nameidentifier` claim is `"not-a-guid"`; and a second principal
  with **no** `nameidentifier` claim.
- For the translation half: fresh `httpContext` with a `MemoryStream` body;
  `exception = new UnauthorizedAccessException("Invalid nameidentifier claim: not-a-guid")`.

**Act:**
- `GetClientId(principalWithBadClaim)` and `GetClientId(principalWithNoClaim)`.
- `await handler.TryHandleAsync(httpContext, exception, CancellationToken.None);`

**Expect:**
- `GetClientId` **throws `UnauthorizedAccessException`** for both principals.
- `TryHandleAsync` returns `true`; `httpContext.Response.StatusCode == 401`.
- The body deserializes to a `ProblemDetails` with `Status == 401`,
  `Title == "Unauthorized"`, and `Type == "https://tools.ietf.org/html/rfc7235#section-3.1"`.

**Covers requirement(s):** F7, F9 (and F8 by derivation — `current`/`assignclient` call this
guard).

> **RED** against current code: `GetClientId` is currently `private` and throws a bare
> `Exception`, so it is neither visible to the test nor throws the typed exception until the
> change in `plan.md` §5. The translation half (`UnauthorizedAccessException ⇒ 401`) is
> **GREEN before and after**.

### Scenario 3: valid key / valid claim ⇒ the parsed `Guid` (happy path)

**Setup:**
- A valid `Guid` string, e.g. `"11111111-1111-1111-1111-111111111111"`.
- A `ClaimsPrincipal` whose `nameidentifier` claim is
  `"22222222-2222-2222-2222-222222222222"`.

**Act:**
- `ParseIdempotencyKey("11111111-1111-1111-1111-111111111111")`.
- `GetClientId(principalWithValidClaim)`.

**Expect:**
- `ParseIdempotencyKey` returns `Guid("11111111-1111-1111-1111-111111111111")` and does not
  throw.
- `GetClientId` returns `Guid("22222222-2222-2222-2222-222222222222")` and does not throw.

**Covers requirement(s):** F1, F6 (and F10 — the success branches are preserved).

> **GREEN after implementation** — proves the guards do not over-reject valid input.

## Out of scope for this test

- The endpoint `.Produces(...)` OpenAPI declarations (F11) and the "no bare `Exception`
  remains" structural check (F12) — verified by code review (`validation.md`).
- The end-to-end routing of each affected endpoint to `400`/`401` (F3, F4, F8) — derived from
  the guards + the central mapping; no per-route HTTP test (no `WebApplicationFactory`
  harness). Manual `curl` scenarios in `validation.md` cover the running service.
- The success-path command/handler behaviour behind `CreateShoppingCart` / `UnreserveSeats` /
  `current` / `assignclient` — unchanged and covered by their existing tests.
- Field-level FluentValidation errors, `DbUpdateException` translation,
  performance/load/concurrency.
- Assertions on logging — verified by code review.
