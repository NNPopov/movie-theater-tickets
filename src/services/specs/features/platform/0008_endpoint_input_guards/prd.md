# 0008 · Endpoint input guards — typed exceptions for the ShoppingCart edge — PRD

> The **endpoint-helper tail** of ADR-002 (now *Accepted*): the two bare
> `throw new Exception(...)` guards on the ShoppingCart endpoints that slices
> `0005`–`0007` explicitly deferred as "endpoint bare-throws (a later slice)". Replaces
> them with **specific typed exceptions** that `CustomExceptionHandler` already maps,
> per `agent_docs/error_handling.md` and `agent_docs/entry_points/minimal-api.md`. No
> new exception/`Error` type, no `CustomExceptionHandler` change, no domain change, no
> schema change.

## Problem Statement

As a maintainer of the BookingManagement service, two request-edge guards on the
ShoppingCart endpoints raise a **bare `throw new Exception(...)`**:

1. **`CreateShoppingCart`** — when the caller's `X-Idempotency-Key` header is not a
   valid `Guid`.
2. **`GetClientId`** (the shared helper used by `current` and `assignclient`) — when the
   authenticated principal's `nameidentifier` claim is not a valid `Guid`.

`CustomExceptionHandler` maps any unrecognised exception to **`500 Internal Server
Error`**. So two perfectly ordinary client-side mistakes — a malformed idempotency
header, and a token whose subject id is not a Guid — are both reported to the caller as
a *server crash*. The caller cannot tell "I sent a bad request / a bad token" from "the
service broke", and the responses carry no useful `ProblemDetails`.

These are the **last documented bare-`throw new Exception(...)` defects on the endpoint
layer**. `agent_docs/error_handling.md` (the error-path checklist) forbids a bare
`Exception` in new code, and `agent_docs/entry_points/minimal-api.md` requires failure
status codes to come from `CustomExceptionHandler` keyed on the exception **type** —
not be hard-coded in the endpoint. A third site, **`UnreserveSeats`**, already parses
the same `X-Idempotency-Key` but resolves a parse failure with an in-endpoint
`return Results.BadRequest()`, which violates that same rule and behaves inconsistently
with `CreateShoppingCart`.

(Note: ADR-002 itself enumerates only the *domain* bare-`Exception` defects; these
endpoint guards are tracked separately in the `0005`–`0007` roadmap notes as a deferred
tail. This slice closes that tail.)

## Solution

As an API client, when I send a **malformed `X-Idempotency-Key`** I want a clean
**`400 Bad Request`** with a `ProblemDetails` body — not a misleading `500` — so I can
tell I sent a bad header and fix it; and when my **authenticated token's subject claim
is not a usable id** I want a **`401 Unauthorized`**, so I can tell the problem is my
token, not the server.

Follow the documented error model: replace each bare `throw new Exception(...)` with a
**specific, already-mapped typed exception**, and let `CustomExceptionHandler` choose
the status:

- **Malformed `X-Idempotency-Key`** ⇒ `throw new DomainValidationException(...)`, which
  `CustomExceptionHandler` already maps to **`400`** (`ValidationProblemDetails`).
- **Malformed / missing `nameidentifier` claim** ⇒ `throw new
  UnauthorizedAccessException(...)`, which `CustomExceptionHandler` already maps to
  **`401`** (`ProblemDetails`).

To make the idempotency-key guard testable and to remove the duplication between
`CreateShoppingCart` and `UnreserveSeats`, extract a shared parse helper. Align
`UnreserveSeats` onto the same helper so both idempotency-key parses behave identically
and become doc-compliant. Fix the latent message defect in `GetClientId` (it
interpolates the always-empty `out` variable and the wrong type name).

No new exception type, no new `Error` kind, no `CustomExceptionHandler` change, no
domain change, no schema/migration. The `DomainValidationException → 400` and
`UnauthorizedAccessException → 401` mappings already exist.

## User Stories

1. As an API client, when I create a shopping cart with a **malformed
   `X-Idempotency-Key`** header, I want a `400 Bad Request`, so that I know my header is
   wrong rather than thinking the service crashed.
2. As an API client, I want that `400` to carry a `ProblemDetails`/`ValidationProblemDetails`
   body naming the offending input, so that my error handling is uniform with the rest
   of the service.
3. As an API client, when I call an authenticated endpoint (`current`, `assignclient`)
   with a token whose **`nameidentifier` claim is not a valid `Guid`**, I want a
   `401 Unauthorized`, so that I know the problem is my token's identity, not a server
   fault.
4. As an API client, when the `nameidentifier` claim is **entirely missing**, I want the
   same `401`, so that "no usable subject id" is handled consistently with "malformed
   subject id".
5. As an API client, I want the previously-returned **`500`** for both of these cases to
   **become `400` / `401`** respectively, so that genuine server errors and bad-input /
   bad-token are no longer conflated.
6. As an API client, when I unreserve seats with a **malformed `X-Idempotency-Key`**, I
   want the **same `400` + `ProblemDetails`** as `CreateShoppingCart`, so that the two
   idempotency-keyed endpoints behave identically.
7. As an API client, I want the **success contracts unchanged** — a valid idempotency
   key still creates the cart (`201`/replay `200`), a valid token still resolves the
   client id — so that existing callers keep working.
8. As an API client, I want the OpenAPI metadata for the affected endpoints to advertise
   the new failure statuses (`400` for the idempotency-keyed endpoints, `401` for the
   authenticated ones), so that the documented contract matches reality.
9. As a service maintainer, I want the two **bare `throw new Exception(...)`** guards
   (`CreateShoppingCart` idempotency key, `GetClientId` claim) replaced by specific
   typed exceptions, so that the last documented endpoint bare-exception defects are
   closed and the `error_handling.md` checklist holds.
10. As a service maintainer, I want failure statuses to come from **`CustomExceptionHandler`
    keyed on the exception type**, not be hard-coded in the endpoint, so that the change
    complies with `minimal-api.md`.
11. As a service maintainer, I want **`UnreserveSeats`'s in-endpoint
    `return Results.BadRequest()`** converted to the same typed-exception path, so that
    the whole file is doc-compliant and the two idempotency-key parses share one
    behaviour.
12. As a service maintainer, I want the idempotency-key parse **extracted into a single
    shared helper** used by both `CreateShoppingCart` and `UnreserveSeats`, so that the
    duplication is removed and the guard is unit-testable.
13. As a service maintainer, I want **no new exception type and no new `Error` kind**
    introduced (which would require an ADR), reusing the existing
    `DomainValidationException`/`UnauthorizedAccessException` mappings, so that the slice
    stays a thin endpoint-layer change.
14. As a service maintainer, I want **no change to `CustomExceptionHandler`**, since the
    `400` and `401` mappings already exist, so that stable infrastructure is untouched.
15. As a service maintainer, I want the **latent message bug** in `GetClientId` fixed (it
    logs the always-empty parsed `out` variable and an unrelated request-type name), so
    that the diagnostic log line is actually useful.
16. As a service maintainer, I want the guards extracted as **internal helpers exposed to
    the API unit-test project via `InternalsVisibleTo`**, so that their thrown-type
    behaviour can be pinned without an HTTP harness.
17. As a developer, I want **unit tests** for the idempotency-key parse helper
    (malformed/empty ⇒ `DomainValidationException`; valid ⇒ the parsed `Guid`) and for
    `GetClientId` (malformed/missing claim ⇒ `UnauthorizedAccessException`; valid claim ⇒
    the `Guid`), so that the typed-exception behaviour is the acceptance gate and cannot
    silently regress.
18. As a developer, I want tests confirming `CustomExceptionHandler` maps
    `DomainValidationException → 400` and `UnauthorizedAccessException → 401` (added only
    if not already covered), so that the end-to-end status contract is pinned at the
    translation point.
19. As a service maintainer, I want this slice to **not** touch any MediatR handler,
    aggregate, repository, or the Flutter client, so that its scope stays the ShoppingCart
    endpoint edge.
20. As a product owner, I want the ADR-002 adoption to be **fully closed on the server
    side** after this slice (only the optional Flutter `204→404` follow-up and the
    intentional query-side exception tails remaining), so that the error-model
    unification reaches its finish line.

## Implementation Decisions

- **Aggregate / module:** filed under the **`platform`** slice folder to keep the ADR-002
  series (`0001`–`0007`) together; the edits live in the ShoppingCart endpoint class but
  the change is cross-cutting transport/auth hygiene, not ShoppingCart business
  behaviour.
- **No MediatR use-case is added or modified.** This is an endpoint-layer + central-handler
  slice. No command, query, handler, validator, aggregate, or repository changes.
- **Guard 1 — idempotency key (`CreateShoppingCart`, `UnreserveSeats`):** extract a shared
  helper that parses the `X-Idempotency-Key` string and, on failure, throws
  **`DomainValidationException`** (message references the raw header value). Both endpoints
  call it; `UnreserveSeats`'s `return Results.BadRequest()` is removed in favour of the
  helper. Status becomes **`400`** via the existing
  `CustomExceptionHandler.HandleDomainValidationException`.
- **Guard 2 — client id (`GetClientId`):** replace the bare throw with
  **`throw new UnauthorizedAccessException(...)`** (message references the raw claim
  value, fixing the current always-empty / wrong-`nameof` interpolation). Status becomes
  **`401`** via the existing `CustomExceptionHandler.HandleUnauthorizedAccessException`.
  Helper signature (`Guid GetClientId(ClaimsPrincipal)`) is preserved, so both call sites
  (`current`, `assignclient`) are covered without change.
- **Chosen exception types (both pre-existing, no ADR):**
  `DomainValidationException` (Domain) ⇒ `400`; `UnauthorizedAccessException` (BCL) ⇒
  `401`. The API may throw a Domain exception — the dependency rule permits API → Domain.
  `DomainValidationException` was chosen over `ValidationException` per the developer's
  decision during discovery.
- **Testability seam:** make the two guards `internal static` and add `InternalsVisibleTo`
  for `BookingManagementService.API.UnitTests` (if not already present), so the helpers
  are unit-testable without a `WebApplicationFactory` harness (none exists in the repo).
- **OpenAPI:** add the relevant `.Produces(400)` / `.Produces(401)` declarations to the
  affected endpoint registrations so the advertised contract matches the new behaviour.
- **Reused unchanged (no edits):** `CustomExceptionHandler` (both mappings already
  registered), the `IEndpoints` plumbing, the MediatR pipeline, `ValidationBehaviour`,
  the `Result`/`ErrorResults` machinery (not involved — these are transport/auth guards,
  not business outcomes).
- **No domain change, no schema change ⇒ no EF Core migration.**
- **ADR guardrail:** if the conversion appears to need a new exception type, a new `Error`
  kind, a new `CustomExceptionHandler` mapping, or any domain change, **stop and ask** —
  that exceeds this slice.

## Testing Decisions

- **What makes a good test here:** assert only externally observable behaviour — *which
  exception type each guard throws for a given input* (and therefore the HTTP status the
  central handler yields), and the happy-path return value — never internal control flow.
- **Guard unit tests — the acceptance gate.** xUnit + FluentAssertions, in
  `BookingManagementService.API.UnitTests`:
  1. idempotency-key helper: malformed string ⇒ throws `DomainValidationException`; empty
     string ⇒ throws `DomainValidationException`; a valid `Guid` string ⇒ returns the
     parsed `Guid`.
  2. `GetClientId`: a `ClaimsPrincipal` with a non-Guid `nameidentifier` ⇒ throws
     `UnauthorizedAccessException`; with the claim absent ⇒ throws
     `UnauthorizedAccessException`; with a valid `Guid` claim ⇒ returns the `Guid`.
- **`CustomExceptionHandler` mapping tests** (added only if not already present), mirroring
  `CustomExceptionHandlerContentNotFound404OutsideInTests` (slice `0002`): drive the real
  handler against a `DefaultHttpContext` and assert `DomainValidationException → 400` and
  `UnauthorizedAccessException → 401` with the expected `ProblemDetails`/`ValidationProblemDetails`
  shape.
- **Outside-in acceptance test** (`EndpointInputGuardsOutsideInTests`) framed at the same
  altitude as `0002`'s outside-in gate (focused unit-level, no HTTP harness): exercises the
  guards + the central translation as the single RED→GREEN gate.
- **No `WebApplicationFactory` end-to-end test** — there is no HTTP harness in this repo
  (consistent with `0002`–`0007`).
- **No handler / repository / domain unit tests** — no such layer is touched (opt-out per
  `agent_docs/testing.md`).
- **Prior art:** `CustomExceptionHandlerContentNotFound404OutsideInTests` and
  `ErrorResultsOutsideInTests` (`BookingManagementService.API.UnitTests`).

## Out of Scope

- Any MediatR handler, aggregate, repository, or domain change — this is an endpoint /
  central-handler slice only.
- The Flutter client follow-up to slice `0002`'s `204 → 404` contract (the affected client
  paths already handle `404`); any client effect of this slice's new `400`/`401` for
  malformed input / bad tokens — standard semantics, not expected to need client work.
- Converting query/read handlers that throw `ContentNotFoundException` — intentional
  exception tails, documented in `agent_docs/error_handling.md`.
- The composition-root guard `throw new Exception("identityOptions is null")` in
  `ConfigureApiServices` — a config-time startup check, not a business/request path.
- Introducing a `WebApplicationFactory` HTTP integration harness.
- Adding any new exception type, `Error` kind, or `CustomExceptionHandler` mapping.
- Schema changes / EF Core migrations.
- Re-examining the `CreateShoppingCart` `.Produces(204)` success contract beyond adding the
  new failure-status declarations.

## Further Notes

- **Behaviour changes:**
  - `CreateShoppingCart` with a malformed `X-Idempotency-Key`: **`500 → 400`**.
  - `current` / `assignclient` with a non-Guid or missing `nameidentifier` claim:
    **`500 → 401`**.
  - `UnreserveSeats` with a malformed `X-Idempotency-Key`: status stays `400` but the body
    becomes a `ProblemDetails`/`ValidationProblemDetails` (was an empty `400`).
  - All success contracts are preserved.
- **Resolved during discovery:**
  - Mechanism = **typed exceptions → `CustomExceptionHandler`** (follow the docs), not
    in-endpoint `Results.*`.
  - `requestId`/idempotency key ⇒ **`DomainValidationException` → 400** (developer's choice
    over `ValidationException`).
  - `clientId`/claim ⇒ **`UnauthorizedAccessException` → 401**.
  - `UnreserveSeats` is **in scope** and aligned onto the shared helper.
  - Tests = focused unit tests + central-handler mapping tests (no WAF), matching `0002`.
- **Significance:** closes the server-side endpoint tail of ADR-002. After this slice the
  only remaining ADR-002 items are the optional Flutter `204→404` follow-up and the
  intentionally-retained query-side exception tails.
- **Publishing:** `gh` CLI is not installed in this environment, so this PRD is stored
  locally only (as for `0001`–`0007`); publishing to the issue tracker with the
  `needs-triage` label is deferred until a tracker is available.
