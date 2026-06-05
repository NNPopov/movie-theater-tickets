# 0008 · EndpointInputGuards — Implementation plan

## 1. Header

- **Aggregate / Module:** `platform` (cross-cutting transport/auth guards on the
  `ShoppingCarts` endpoint group + two focused tests; not a single DDD aggregate, and
  **no MediatR use-case**).
- **Slice:** `0008_endpoint_input_guards`
- **PRD:** ./prd.md
- **ADR:** `docs/adr/ADR-002-error-handling-model-result-vs-exceptions.md` (the
  endpoint-helper *tail* of the now-*Accepted* ADR; no change to the ADR).
- **Reference slice:** `../0002_content_not_found_404/plan.md` — same shape: a
  `platform` slice that corrects an HTTP error contract through the existing central
  translation, run through the full spec chain, pinned by focused unit tests in
  `BookingManagementService.API.UnitTests` with **no `WebApplicationFactory`**. There is
  **no** command/query use-case slice to shape-match (this slice adds no use-case
  folder); `0002` is the structural precedent.
- **HTTP paths affected (no new route added):**
  - `POST /api/shoppingcarts` (`CreateShoppingCart`) — malformed `X-Idempotency-Key`
    header flips `500 → 400`.
  - `DELETE /api/shoppingcarts/{ShoppingCartId}/unreserve` (`UnreserveSeats`) — malformed
    `X-Idempotency-Key` header: in-endpoint empty `400` becomes a typed-exception
    `400 + ProblemDetails`.
  - `GET /api/shoppingcarts/current` (`current`) — non-Guid/missing `nameidentifier`
    claim (via `GetClientId`) flips `500 → 401`.
  - `PUT /api/shoppingcarts/{ShoppingCartId}/assignclient` (`AssignUser`) — same
    `GetClientId` guard, flips `500 → 401`.
- **STABLE files touched:** **none.**
  `API/Infrastructure/CustomExceptionHandler.cs` is **not** changed — the
  `DomainValidationException → 400` and `UnauthorizedAccessException → 401` writers
  already exist (`agent_docs/stable_vs_feature.md`; confirmed in code). The only edited
  production file is the feature endpoint class
  `ShoppingCartEndpointApplicationBuilderExtensions.cs` (an `IEndpoints` group — feature
  code per `stable_vs_feature.md`). Adding an `InternalsVisibleTo` item to the API
  `.csproj` is a test-enablement attribute (tests are feature code), not a mechanism
  change. If anything beyond §5 proves necessary (a new exception/`Error` type, a new
  mapper arm, a `CustomExceptionHandler` change), **stop and ask** — that exceeds this
  slice.

## 2. Context summary

This slice closes the endpoint-helper tail of ADR-002: the two bare
`throw new Exception(...)` guards on the `ShoppingCarts` endpoints. A malformed
`X-Idempotency-Key` header and a token whose `nameidentifier` claim is not a `Guid`
currently fall through to `CustomExceptionHandler.HandleException` and are reported as
`500`. Per `agent_docs/error_handling.md` (no bare `Exception`) and
`agent_docs/entry_points/minimal-api.md` (failure status comes from
`CustomExceptionHandler`, keyed on the exception type — not hard-coded in the endpoint),
each guard is replaced with a **specific, already-mapped** typed exception:
`DomainValidationException` (⇒ `400`) for the idempotency key, `UnauthorizedAccessException`
(⇒ `401`) for the claim. The idempotency-key parse is extracted into one shared,
unit-testable helper used by both `CreateShoppingCart` and `UnreserveSeats` (removing the
duplication and the doc-violating in-endpoint `Results.BadRequest()`). The latent message
bug in `GetClientId` is fixed. No use-case, aggregate, repository, domain, schema, or
`CustomExceptionHandler` change. The acceptance gate is a focused unit test of the guards
plus the central mapping, with the full suite (incl. architecture tests) staying green.

## 3. API contract

This slice changes **failure status codes only**. No request or response model is added
or changed; no success contract changes.

### Guard 1 — `X-Idempotency-Key` parse (`CreateShoppingCart`, `UnreserveSeats`)

- **Input:** `[FromHeader(Name = "X-Idempotency-Key")] string requestId`.
- **Before:** `CreateShoppingCart` — `throw new Exception(...)` ⇒ `500`; `UnreserveSeats`
  — `return Results.BadRequest()` ⇒ empty `400`.
- **After:** both parse via the shared helper; on failure
  `throw new DomainValidationException($"Invalid idempotency key: {requestId}")`, mapped
  centrally to **`400`** (`ValidationProblemDetails`, `Type` rfc7231 §6.5.1) by the
  existing `CustomExceptionHandler.HandleDomainValidationException`.
- **Valid key:** unchanged — parsed `Guid` flows into the command (`CreateShoppingCartCommand`
  / `UnreserveSeatsCommand`); success contract unchanged.

### Guard 2 — `nameidentifier` claim (`GetClientId`; used by `current`, `assignclient`)

- **Input:** the authenticated `ClaimsPrincipal`'s `nameidentifier` claim.
- **Before:** non-Guid/missing claim ⇒ `throw new Exception(...)` ⇒ `500` (with a latent
  message bug: it interpolates the always-empty `out` variable and an unrelated
  `nameof(CreateShoppingCartRequest)`).
- **After:** non-Guid/missing claim ⇒
  `throw new UnauthorizedAccessException($"Invalid nameidentifier claim: {id}")` (message
  uses the raw claim value `id`), mapped centrally to **`401`** (`ProblemDetails`, `Type`
  rfc7235 §3.1) by the existing `CustomExceptionHandler.HandleUnauthorizedAccessException`.
- **Valid claim:** unchanged — returns the parsed `Guid`.

### Status-code mapping reference

Per `agent_docs/error_handling.md` (no row added/changed by this slice):
`DomainValidationException → 400`, `UnauthorizedAccessException → 401`,
`ContentNotFoundException`/`NotFoundException → 404`, `ConflictException → 409`,
`LockedException → 423`, `DuplicateRequestException → 200`,
`ForbiddenAccessException → 403`, anything else `→ 500`. **No new status code and no new
exception/`Error` type are introduced.**

### OpenAPI `.Produces` corrections

- `CreateShoppingCart` (`POST {BaseRoute}`): add `.Produces(400)`.
- `UnreserveSeats` (`DELETE {BaseRoute}/{{ShoppingCartId}}/unreserve`): add `.Produces(400)`.
- `current` (`GET {BaseRoute}/current`): add `.Produces(401)` (keeps 200/204/404).
- `AssignUser` (`PUT {BaseRoute}/{{ShoppingCartId}}/assignclient`): add `.Produces(401)`
  (keeps 200/404/409).

## 4. File structure

```
BookingManagement/
└── BookingManagementService.API/
    ├── BookingManagementService.API.csproj                         # EDIT: add <InternalsVisibleTo Include="BookingManagementService.API.UnitTests" />
    └── Endpoints/
        └── ShoppingCartEndpointApplicationBuilderExtensions.cs     # EDIT (feature):
            #   - new internal static Guid ParseIdempotencyKey(string requestId)  -> DomainValidationException
            #   - CreateShoppingCart: use ParseIdempotencyKey (was bare throw)
            #   - UnreserveSeats: use ParseIdempotencyKey (was return Results.BadRequest())
            #   - GetClientId: private static -> internal static; bare throw -> UnauthorizedAccessException; fix message
            #   - .Produces(400)/.Produces(401) on the four endpoints

(src/services)/tests/
└── BookingManagementService.API.UnitTests/                         # EXISTING project (refs API, FrameworkReference AspNetCore.App)
    ├── Endpoints/
    │   └── ShoppingCart/
    │       └── EndpointInputGuardsTests.cs                          # NEW (after green): ParseIdempotencyKey + GetClientId thrown-type facts
    └── Infrastructure/
        └── CustomExceptionHandlerInputGuardsTests.cs               # NEW: DomainValidationException->400, UnauthorizedAccessException->401 (DefaultHttpContext)

specs/features/platform/0008_endpoint_input_guards/
└── EndpointInputGuardsOutsideInTests.cs                            # NOTE: the executable RED gate is written by /slice-test-red INTO the API.UnitTests project (Endpoints/ShoppingCart/), not under specs/
```

No EF Core entity is added or altered → **no migration**.

> Note: `BookingManagementService.API.UnitTests` (RootNamespace `CinemaTicketBooking.Api.UnitTests`,
> **assembly name `BookingManagementService.API.UnitTests`**) is the correct host — the
> guards live in the API assembly and need its web types. `InternalsVisibleTo` must name
> the **assembly** (`BookingManagementService.API.UnitTests`), not the root namespace. The
> existing `Domain.UnitTests` project references Application only and cannot see the API
> guards.

## 5. Implementation steps

1. **API — extract the idempotency-key guard.** In
   `ShoppingCartEndpointApplicationBuilderExtensions.cs`, add:
   ```csharp
   internal static Guid ParseIdempotencyKey(string requestId)
   {
       if (!Guid.TryParse(requestId, out var parsedRequestId))
       {
           throw new DomainValidationException($"Invalid idempotency key: {requestId}");
       }

       return parsedRequestId;
   }
   ```
   Add `using CinemaTicketBooking.Domain.Exceptions;`. (PRD user stories 1, 2, 9, 12;
   `DomainValidationException` chosen over `ValidationException` per discovery.)

2. **API — `CreateShoppingCart` uses the helper.** Replace the inline
   `if (!Guid.TryParse(requestId, out Guid parsedRequestId)) { throw new Exception(...); }`
   with `var parsedRequestId = ParseIdempotencyKey(requestId);`. Behaviour: malformed key
   `500 → 400`; valid key unchanged (PRD user stories 1, 5, 7).

3. **API — `UnreserveSeats` uses the helper.** Replace
   `if (!Guid.TryParse(requestId, out Guid parsedRequestId)) { return Results.BadRequest(); }`
   with `var parsedRequestId = ParseIdempotencyKey(requestId);`. Behaviour: status stays
   `400` but the body becomes `ValidationProblemDetails`; both idempotency endpoints now
   behave identically (PRD user stories 6, 10, 11).

4. **API — fix the `GetClientId` guard.** Change `private static Guid GetClientId(...)`
   to `internal static Guid GetClientId(...)` and replace the bare throw:
   ```csharp
   var id = user.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;

   if (!Guid.TryParse(id, out Guid clientId))
   {
       throw new UnauthorizedAccessException($"Invalid nameidentifier claim: {id}");
   }

   return clientId;
   ```
   `UnauthorizedAccessException` is BCL (`System`) — no new using. Fixes the latent message
   bug (was the always-empty `clientId` + wrong `nameof(CreateShoppingCartRequest)`).
   Behaviour: bad/missing claim `500 → 401` for `current` and `assignclient` (PRD user
   stories 3, 4, 5, 15).

5. **API — OpenAPI `.Produces`.** Add `.Produces(400)` to `CreateShoppingCart` and
   `UnreserveSeats`; add `.Produces(401)` to `current` and `AssignUser` (PRD user story 8).

6. **API project — `InternalsVisibleTo`.** In
   `BookingManagementService.API.csproj`, add:
   ```xml
   <ItemGroup>
     <InternalsVisibleTo Include="BookingManagementService.API.UnitTests" />
   </ItemGroup>
   ```
   so the two `internal static` guards are unit-testable without an HTTP harness (PRD user
   story 16).

7. **Verify (pre-test).** From `src/services`:
   ```
   dotnet format CinemaBookingManagement.sln
   dotnet build  CinemaBookingManagement.sln -warnaserror
   ```
   Resolve any new warnings. Per MEMORY (`warnaserror-baseline-debt`, `dotnet10-migration`):
   `-warnaserror` only passes incrementally against the pre-existing nullable debt, and the
   accepted AutoMapper NuGet-audit advisory trips restore — scope the build/format so this
   slice's edits are what is validated, not the baseline debt, and do not let `dotnet format`
   rewrite unrelated files (e.g. `ReserveSeatsCommandValidatorSpecification.cs`).

## 6. Tests planned

The externally observable behaviour is (a) the exception **type** each guard throws and
(b) the HTTP status the central handler yields. There is **no `WebApplicationFactory`**
harness in the repo; the slice is pinned by focused unit tests, consistent with `0002`.

- **Outside-in / RED acceptance gate — `BookingManagementService.API.UnitTests`,
  `Endpoints/ShoppingCart/EndpointInputGuardsTests.cs`** (written + verified RED by
  `/slice-test-red`). xUnit + FluentAssertions, driving the now-`internal` guards directly:
  1. `ParseIdempotencyKey` — malformed string ⇒ throws `DomainValidationException`; empty
     string ⇒ throws `DomainValidationException`; a valid `Guid` string ⇒ returns the
     parsed `Guid`.
  2. `GetClientId` — `ClaimsPrincipal` with a non-Guid `nameidentifier` ⇒ throws
     `UnauthorizedAccessException`; with the claim absent ⇒ throws
     `UnauthorizedAccessException`; with a valid `Guid` claim ⇒ returns that `Guid`.
  RED before step 1/4 (the bare `throw new Exception` / `Results.BadRequest()` do not throw
  the typed exceptions); GREEN after.

- **Central-mapping test — `BookingManagementService.API.UnitTests`,
  `Infrastructure/CustomExceptionHandlerInputGuardsTests.cs`.** Mirrors
  `CustomExceptionHandlerContentNotFound404OutsideInTests` (`0002`): drives the real
  `CustomExceptionHandler.TryHandleAsync` against a `DefaultHttpContext` and asserts
  `DomainValidationException ⇒ 400` (`ValidationProblemDetails`, `Type` rfc7231 §6.5.1) and
  `UnauthorizedAccessException ⇒ 401` (`ProblemDetails`, `Type` rfc7235 §3.1). These two
  mappings are not currently covered by any test; this pins the end-to-end status contract
  the guards rely on.

**Opt-outs (explicit, per `agent_docs/testing.md` and the PRD's Testing Decisions):**
- **Handler unit test — skipped:** no MediatR handler is added or changed.
- **Repository / adapter unit test — skipped:** no repository or adapter code changes.
- **Domain unit test — skipped:** no domain code changes.
- **`WebApplicationFactory` end-to-end / endpoint integration test — skipped:** no HTTP
  harness exists; standing one up is disproportionate for two edge guards. The endpoint
  wiring + `.Produces` edits are verified by compilation; the guards and the central
  mapping are pinned by the two unit tests above.

**Full-suite gate:** the entire `dotnet test` run, **including
`BookingManagementService.Domain.ArchitectureTests`**, must stay green.

## 7. Out of scope for this slice

- Any MediatR handler, aggregate, repository, value object, or domain change.
- The composition-root guard `throw new Exception("identityOptions is null")` in
  `ConfigureApiServices` — a config-time startup check, not a request path.
- The query-side handlers that throw `ContentNotFoundException` — intentional exception
  tails documented in `agent_docs/error_handling.md`.
- The Flutter client follow-up to `0002`'s `204 → 404`; any client effect of this slice's
  new `400`/`401` (standard semantics, not expected to need client work).
- Any change to `CustomExceptionHandler` (its mappings already exist), the MediatR
  pipeline, `ValidationBehaviour`, `ErrorResults`, the `IEndpoints` plumbing, or any base
  type.
- Adding a new exception type, `Error` kind, mapper arm, or status code.
- Re-examining the `CreateShoppingCart`/`UnreserveSeats` `.Produces(204)`/success
  contracts beyond adding the new failure-status declarations.
- A `WebApplicationFactory` HTTP integration harness.
- Schema changes / EF Core migrations.

## 8. Open questions

None. The mechanism (typed exceptions → central handler), the two exception choices
(`DomainValidationException → 400`, `UnauthorizedAccessException → 401`), the
`UnreserveSeats` alignment, the shared `ParseIdempotencyKey` extraction, the
`InternalsVisibleTo` testability seam, and the "no `WebApplicationFactory`, focused unit
tests like `0002`" strategy were all settled in the grill-me interview and the approved
`prd.md`.
