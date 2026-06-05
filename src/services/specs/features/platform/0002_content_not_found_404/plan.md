# 0002 · ContentNotFound404 — Implementation plan

## 1. Header

- **Aggregate / Module:** `platform` (cross-cutting HTTP-contract correction at
  `CustomExceptionHandler` + two `ShoppingCarts` / `MovieSessions` carve-outs — not a single
  DDD aggregate)
- **Slice:** `0002_content_not_found_404`
- **PRD:** ./prd.md
- **ADR:** `docs/adr/ADR-002-error-handling-model-result-vs-exceptions.md` (step 2; ADR stays
  **Proposed**)
- **Reference slice:** `../0001_error_model_result_infrastructure/plan.md` — same `platform`
  module and the same "ADR-gated change to stable infrastructure, run through the full spec
  chain, pinned by a focused unit spec, no `WebApplicationFactory`" shape. There is **no**
  command/query use-case slice to shape-match (this slice adds no new use-case folder); 0001
  is the structural precedent.
- **HTTP paths affected (no new route added):**
  - `GET /api/shoppingcarts/{ShoppingCartId}` — flips `204 → 404` via the central change.
  - `GET /api/moviesessions/{movieSessionId}` — flips `204 → 404` via the central change.
  - `GET /api/movies/{movieId}` — flips `204 → 404` via the central change (already declares
    `.Produces(404)`).
  - `GET /api/shoppingcarts/current` — **carve-out A:** no active cart ⇒ `204`; inconsistent
    (active-cart id recorded, record missing) ⇒ `404`.
  - `GET /api/movies/{movieId}/moviesessions` — **carve-out B:** no upcoming sessions ⇒
    `200 []`.
  - The cart commands and the seat service that throw `ContentNotFoundException` for an
    addressed-but-absent resource (select, unselect, reserve, expired, `assignclient` bridge)
    flip `204 → 404` automatically; no per-handler edit.
- **STABLE files touched (ADR-gated — see note):**
  - `BookingManagement/BookingManagementService.API/Infrastructure/CustomExceptionHandler.cs`
    — rewrite the **one** `HandleContentNotFoundException` writer from "set `204`, complete
    with no body" to "set `404`, write `ProblemDetails`". No change to the dictionary wiring,
    the dispatch mechanism, or any other writer.

  > **Why this is allowed despite touching a stable file.** `CustomExceptionHandler` is listed
  > as *stable infrastructure* in `agent_docs/stable_vs_feature.md`, and
  > `agent_docs/spec_workflow.md` § "When to stop and ask" flags a change to its mapping as ADR
  > territory. That stop-and-ask has already happened: this is **ADR-002 step 2**, settled in a
  > grill-me interview and an approved `prd.md`, and deliberately run through the full spec
  > chain. **No mechanism is invented** — the dispatch dictionary, the `IExceptionHandler`
  > contract, the exception hierarchy, the MediatR pipeline, and every other writer are
  > unchanged; only the body of the existing `ContentNotFoundException` writer changes, and the
  > two carve-outs are ordinary feature edits to a handler and an endpoint. If anything beyond
  > the edits in §5 proves necessary, **stop and ask** — that would exceed the ADR's step-2
  > scope.

- **NEW test project (per user decision):** `BookingManagementService.API.UnitTests` —
  references the API project + `FrameworkReference Microsoft.AspNetCore.App`, hosts the
  `CustomExceptionHandler` gate test. Adding a test project and its solution entry is feature
  work (tests are feature code), not an ADR. Created by `/slice-test-red` in step 5.

## 2. Context summary

This slice corrects the service's "not-found" HTTP contract as ADR-002 step 2. At the single
central translation point (`CustomExceptionHandler`), `ContentNotFoundException` stops mapping
to an empty `204 No Content` and starts mapping to `404 Not Found` with a `ProblemDetails`
body identical in shape to the one `NotFoundException` already produces. Because the mapping
is central, every addressed-resource read path that throws `ContentNotFoundException` (movie
by id, movie session by id, shopping cart by id, the seat service, the cart commands) flips to
`404` with **no per-handler edit**. Two genuine empty-state paths are explicitly carved out so
the blunt central flip does not turn a normal empty state into a `404`: the get-current-cart
query returns an empty (`null`) result when the customer has no active cart (endpoint ⇒ `204`),
and the get-movie-sessions query returns an empty collection when there are no upcoming
sessions (endpoint ⇒ `200 []`). The OpenAPI `.Produces(...)` declarations on the affected read
paths are corrected, and the project's own documentation/skills that teach the stale
`→ 204` mapping are updated to `→ 404`. The acceptance gate is a `CustomExceptionHandler` unit
test (status `404` + `ProblemDetails`, with the `NotFoundException → 404` regression) going
green, plus the full suite (including the architecture tests) staying green.

## 3. API contract

This slice changes **status codes and the not-found response body**, not request shapes. No new
request/response model is introduced. One response type is made nullable (carve-out A).

### Central flip — `ContentNotFoundException`

- **Before:** `204 No Content`, empty body (`Response.CompleteAsync()`).
- **After:** `404 Not Found` with a `ProblemDetails`:
  - `Status = 404`
  - `Type = "https://tools.ietf.org/html/rfc7231#section-6.5.4"`
  - `Title = "The specified resource was not found."`
  - `Detail = exception.Message`
  - Logged at `Warning` (unchanged log level/behaviour).
  - **Byte-identical in shape to the existing `HandleNotFoundException` writer** (user
    stories 4, 5). `NotFoundException → 404` is left untouched (user story 19).

### Carve-out A — `GET /api/shoppingcarts/current`

- Query result type becomes **`CreateShoppingCartResponse?`** (nullable) to express "no current
  cart".
- **No active cart** (repository returns `Guid.Empty`): handler returns `null` (does **not**
  throw). Endpoint maps `null` ⇒ `204 No Content` (user stories 7, 8).
- **Inconsistent** (active-cart id recorded but the cart record is missing): handler **keeps
  throwing `ContentNotFoundException`** ⇒ `404` via the central flip (user story 9).
- **Cart found:** `200 OK` with `CreateShoppingCartResponse` body.
- **Status codes:** `200` (found), `204` (no active cart), `404` (inconsistent).

### Carve-out B — `GET /api/movies/{movieId}/moviesessions`

- **No upcoming sessions:** handler returns the **empty collection** (does **not** throw).
  Endpoint returns `200 OK` with `[]` (user stories 10, 11).
- **Sessions present:** `200 OK` with the mapped collection.
- **Status codes:** `200` only (no `404`/`204` for emptiness).

### Status-code mapping reference

Per `agent_docs/error_handling.md` (mapping table is corrected by this slice):
`ContentNotFoundException → 404`, `NotFoundException → 404`, `ValidationException` /
`DomainValidationException → 400`, `ConflictException → 409`, `LockedException → 423`,
`DuplicateRequestException → 200`, `UnauthorizedAccessException → 401`,
`ForbiddenAccessException → 403`, anything else `→ 500`. No new status code is invented and no
new exception/`Error` type is added.

## 4. File structure

```
BookingManagement/
├── BookingManagementService.API/
│   ├── Infrastructure/
│   │   └── CustomExceptionHandler.cs                              # EDIT (STABLE): HandleContentNotFoundException → 404 + ProblemDetails
│   └── Endpoints/
│       ├── ShoppingCartEndpointApplicationBuilderExtensions.cs    # EDIT: `current` null⇒204; .Produces corrections (current, GetShoppingCartById)
│       └── MovieSessionEndpointApplicationBuilderExtensions.cs    # EDIT: .Produces corrections (GetMovieSessionsById, GetActiveMovieSessionsByMovieId)
├── BookingManagementService.Application/
│   ├── ShoppingCarts/Queries/
│   │   └── GetCurrentShoppingCartQueryHandler.cs                  # EDIT: IRequest<CreateShoppingCartResponse?>; no-active-cart ⇒ return null
│   └── MovieSessions/Queries/
│       └── GetMovieSessionsQueryHandler.cs                        # EDIT: empty ⇒ return empty collection (do not throw)
│
├── tests/
│   └── BookingManagementService.Domain.UnitTests/                 # (references Application; hosts the two query-handler tests)
│       ├── ShoppingCarts/GetCurrentShoppingCartQueryHandlerTests.cs   # NEW (written after green, per testing.md)
│       └── MovieSessions/GetMovieSessionsQueryHandlerTests.cs         # NEW (written after green, per testing.md)
│
└── (src/services)/tests/
    └── BookingManagementService.API.UnitTests/                    # NEW PROJECT (step 5 / slice-test-red)
        ├── BookingManagementService.API.UnitTests.csproj          # xUnit + FluentAssertions + FrameworkReference Microsoft.AspNetCore.App + ProjectReference API
        └── Infrastructure/
            └── CustomExceptionHandlerContentNotFound404OutsideInTests.cs   # the RED acceptance gate (step 5)

docs / skills (documentation, not stable mechanism):
├── agent_docs/error_handling.md                                   # EDIT: mapping table ContentNotFoundException 204 → 404
└── .claude/skills/
    ├── feature-tests/...                                          # EDIT: remove "ContentNotFoundException → 204" guidance
    ├── slice-test-red/...                                         # EDIT: same
    ├── feature-validation/...                                     # EDIT: same
    ├── feature-requirements/...                                   # EDIT: same
    └── spec-workflow/...                                          # EDIT: same
```

No EF Core entity is added or altered → **no migration**.

> Note: the existing `BookingManagementService.Domain.UnitTests` project is named "Domain" but
> its `RootNamespace` is `CinemaTicketBooking.Application.UnitTests` and it references the
> **Application** project (with NSubstitute available). The two query-handler tests belong there
> by precedent. The `CustomExceptionHandler` test cannot live there (it would drag the web host
> into a domain/application test project); hence the dedicated `API.UnitTests` project.

## 5. Implementation steps

1. **API (STABLE) — flip the central writer.** In `CustomExceptionHandler.cs`, replace the body
   of `HandleContentNotFoundException`:
   ```csharp
   private async Task HandleContentNotFoundException(HttpContext httpContext, Exception ex)
   {
       _logger.Warning(ex, "Not Found");

       httpContext.Response.StatusCode = StatusCodes.Status404NotFound;

       await httpContext.Response.WriteAsJsonAsync(new ProblemDetails
       {
           Status = StatusCodes.Status404NotFound,
           Type = "https://tools.ietf.org/html/rfc7231#section-6.5.4",
           Title = "The specified resource was not found.",
           Detail = ex.Message
       });
   }
   ```
   Do **not** touch the dictionary registration, `TryHandleAsync`, or any other writer. This is
   the single point that flips every addressed-resource not-found path to `404` (user stories
   1–6, 12, 13).

2. **Application — carve-out A (current cart).** In `GetCurrentShoppingCartQueryHandler.cs`:
   - Change the query/handler contract to `IRequest<CreateShoppingCartResponse?>` /
     `IRequestHandler<GetCurrentShoppingCartQuery, CreateShoppingCartResponse?>`.
   - When `existingShoppingCartId == Guid.Empty` (no active cart): `return null;` instead of
     throwing `ContentNotFoundException` (user stories 7, 8, 14).
   - **Keep** the second guard: when `shoppingCart is null` (id recorded but record missing),
     still `throw new ContentNotFoundException(...)` ⇒ `404` (user story 9).
   - The success branch is unchanged: `return new CreateShoppingCartResponse(...)`.

3. **Application — carve-out B (movie sessions list).** In `GetMovieSessionsQueryHandler.cs`,
   delete the `if (movieSessions == null || !movieSessions.Any()) throw new
   ContentNotFoundException(...)` block. Return the projected collection directly; when the
   source is empty the result is an empty `List<MovieSessionsDto>` (`200 []`) (user stories 10,
   11, 14). The `TimeProvider.System` filter is unchanged (out of scope for this slice).

4. **API — endpoint: `current` maps null ⇒ 204.** In
   `ShoppingCartEndpointApplicationBuilderExtensions.cs`, the `current` delegate:
   ```csharp
   var result = await sender.Send(command, cancellationToken);
   return result is null ? Results.NoContent() : Results.Ok(result);
   ```
   Correct its `.Produces(...)`: drop the stale `.Produces(201)` and `.Produces(409)`; declare
   `.Produces<CreateShoppingCartResponse>(200, "application/json")`, keep `.Produces(204)` (the
   real empty state), and **add `.Produces(404)`** (the inconsistent branch) (user stories 8, 9,
   15, 16).

5. **API — endpoint: `.Produces(404)` on addressed-resource read paths.**
   - `GetShoppingCartById` (`GET {BaseRoute}/{{ShoppingCartId}}`): its `.Produces(204)` was the
     not-found mapping; replace it with `.Produces(404)` (keep `.Produces<ShoppingCartDto>(200)`)
     (user story 15).
   - `GetMovieSessionsById` (`GET api/moviesessions/{{movieSessionId}}`): same — replace the
     not-found `.Produces(204)` with `.Produces(404)`.
   - `GetActiveMovieSessionsByMovieId` (`GET api/movies/{{movieId}}/moviesessions`, carve-out B):
     the not-found `.Produces(204)` is no longer reachable (empty ⇒ `200 []`); remove it, keep
     `.Produces<IReadOnlyCollection<MovieSessionsDto>>(200)`.
   - `GetMovieById` (`GET api/movies/{{movieId}}`): already declares `.Produces(404)` and no
     stray `204` — **no change** (it simply becomes correct at runtime after the flip).
   - Each remaining `.Produces(204)` elsewhere (create cart, select/unselect/reserve/purchase,
     `activemovies`, `seats`) represents "no body on success" or an unrelated path; **leave
     untouched** (user story 16). Do not add `.Produces(404)` to write endpoints in this slice.

6. **Docs — correct the canonical reference.** In `agent_docs/error_handling.md`, change the
   mapping-table row `ContentNotFoundException | 204 No Content` to
   `ContentNotFoundException | 404` (user story 17). Do **not** flip ADR-002 to Accepted.

7. **Skills — stop teaching the stale contract.** Search the spec-chain skills for the
   hard-coded "`ContentNotFoundException → 204`" / "`204`, not `404`" guidance and change it to
   `→ 404`: `feature-tests`, `slice-test-red`, `feature-validation`, `feature-requirements`,
   `spec-workflow` (user story 18). Documentation/skill edits only — no skill mechanism changes.

8. **Verify (pre-test).** From `src/services`:
   ```
   dotnet format CinemaBookingManagement.sln
   dotnet build  CinemaBookingManagement.sln -warnaserror
   ```
   Resolve any warnings. The accepted AutoMapper **NU1903** NuGet-audit advisory trips
   `-warnaserror` at restore time — a known, accepted project constraint (see MEMORY:
   `dotnet10-migration`); handle the NuGet audit so the real build/warnings are what is
   validated, not the advisory.

## 6. Tests planned

The externally observable behaviour is the HTTP status (and, for the central flip, the body
shape). There is **no integration-test project** (no `WebApplicationFactory<Program>`); the
change is pinned by focused unit tests of the changed units, consistent with how slice 0001
closed.

- **Outside-in / RED acceptance gate (the gate) — NEW project
  `BookingManagementService.API.UnitTests`,
  `Infrastructure/CustomExceptionHandlerContentNotFound404OutsideInTests.cs`.** xUnit +
  FluentAssertions + `DefaultHttpContext`. Drives `CustomExceptionHandler.TryHandleAsync`
  directly:
  1. `ContentNotFoundException` ⇒ `Response.StatusCode == 404` and a `ProblemDetails` body
     (asserts `Status`, `Type`, `Title`, `Detail` from the message).
  2. **Regression:** `NotFoundException` ⇒ still `404` with its `ProblemDetails` shape.
  Produced and verified RED by `/slice-test-red` in step 5 (RED because the current writer sets
  `204`/empty body). Created together with the `.csproj` (`FrameworkReference
  Microsoft.AspNetCore.App`, `ProjectReference` to the API) and its solution entry.

- **Handler unit test — current cart** —
  `BookingManagement/tests/BookingManagementService.Domain.UnitTests/ShoppingCarts/GetCurrentShoppingCartQueryHandlerTests.cs`.
  NSubstitute mocks `IActiveShoppingCartRepository` + `IMapper`. Facts: no active cart
  (`Guid.Empty`) ⇒ returns `null`, does not throw; active-cart id present but `GetByIdAsync`
  returns `null` ⇒ throws `ContentNotFoundException`; cart present ⇒ returns the response.

- **Handler unit test — movie sessions list** —
  `BookingManagement/tests/BookingManagementService.Domain.UnitTests/MovieSessions/GetMovieSessionsQueryHandlerTests.cs`.
  NSubstitute mocks `IMovieSessionsRepository` + `IMapper`. Facts: repository returns empty ⇒
  handler returns an empty collection, does not throw; repository returns sessions ⇒ returns the
  mapped collection.

**Opt-outs (explicit, per `agent_docs/testing.md` and the PRD's Testing Decisions):**
- **`WebApplicationFactory` end-to-end / endpoint integration test — skipped:** no HTTP harness
  exists and standing one up is disproportionate for a one-method central change plus two
  carve-outs. The endpoint `.Produces`/null-mapping edits are verified by compilation and the
  unit tests.
- **Repository / adapter unit test — skipped:** no repository or adapter logic changes (the
  carve-outs change handler control flow only; no new business-meaningful infrastructure
  exception translation).
- **Unit tests for the addressed-resource handlers that flip purely via the central mapping —
  skipped:** their behaviour is covered by the `CustomExceptionHandler` gate test (PRD decision).

**Full-suite gate:** the entire `dotnet test` run, **including
`BookingManagementService.Domain.ArchitectureTests`**, must stay green (user story 21).

## 7. Out of scope for this slice

- Removing the `assignclient` endpoint `Result → exception` bridge or converting any endpoint to
  `Match`-to-HTTP (ADR step 3). Its not-found path changing `204 → 404` is an **accepted side
  effect** of the central flip (user story 22).
- Adopting `Result<T>` in any handler (ADR step 3).
- Deduplicating `NotFoundException` and `ContentNotFoundException` (they now produce identical
  `404`s) — a separate exception-vocabulary ADR.
- Replacing bare `throw new Exception(...)` in the domain/handlers (ADR defect #2).
- Updating the Flutter client to treat the by-id cart not-found as `404` (separate tracked task,
  user story 23). After the carve-outs the client breaks only at `GET /shoppingcarts/{id}` and
  the `assignclient` bridge path; `current` keeps `204`, so its empty-cart UX is unaffected.
- Flipping ADR-002 to Accepted.
- Changing the `CustomExceptionHandler` mechanism (dictionary/dispatch), the MediatR pipeline,
  the validation behaviour, or any base type.
- Touching the `TimeProvider.System` "upcoming" filter logic in `GetMovieSessionsQueryHandler`
  (only the throw-on-empty is removed).

## 8. Open questions

- **Sequencing risk (must be honoured by steps 4–5).** The spec-chain skills (`feature-tests`,
  `slice-test-red`, `feature-validation`, `feature-requirements`, `spec-workflow`) **still teach
  the stale `ContentNotFoundException → 204` mapping** at the time this pipeline runs (step 7
  fixes them only at implementation). When `/feature-tests` (step 4) and `/slice-test-red`
  (step 5) generate their artifacts, they must encode the **new `404` contract from this
  PRD/plan**, not the stale `204` guidance still present in the skill text. This PRD/plan is the
  source of truth; the skill text is a known-stale input being corrected by this very slice.
- Otherwise none. The central-flip body shape (mirror `NotFoundException`), the two carve-out
  control-flow changes, the nullable `current` response, the `.Produces` corrections, the gate
  living in a new `API.UnitTests` project, and the "no `WebApplicationFactory`" decision were all
  settled in the grill-me interview / PRD and the user's test-host choice.
