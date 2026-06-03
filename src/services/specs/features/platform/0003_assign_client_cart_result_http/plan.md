# 0003 · AssignClientCart Result→HTTP — Implementation plan

## 1. Header

- **Aggregate / Module:** `platform` (an `AssignClientCart` use-case conversion in
  `ShoppingCarts` that also produces a cross-cutting platform artifact — the shared
  `Error → IResult` mapper — reused by every later ADR-002 step-3 conversion; filed under
  `platform` to keep the ADR-002 series together, like `0001`/`0002`).
- **Slice:** `0003_assign_client_cart_result_http`
- **PRD:** ./prd.md
- **ADR:** `docs/adr/ADR-002-error-handling-model-result-vs-exceptions.md` (step 3, first
  conversion; ADR stays **Proposed**).
- **Reference slice:** `../0002_content_not_found_404/plan.md` — same `platform` module, same
  "ADR-002-gated change run through the full spec chain, pinned by a focused unit spec in
  `BookingManagementService.API.UnitTests`, no `WebApplicationFactory`" shape. There is **no**
  prior `Result`→`Match`-to-HTTP endpoint to shape-match (this slice creates the canonical one);
  `0002` is the structural precedent and the `API.UnitTests` gate project already exists.
- **HTTP path (no new route; existing route, mechanism swap):**
  - `PUT /api/shoppingcarts/{ShoppingCartId}/assignclient` — the `Result → exception` bridge is
    replaced by `Match(onSuccess ⇒ 200, onFailure ⇒ shared mapper)`. Status codes unchanged in
    and out: `200` / `404` / `409`.
- **STABLE files touched:** **none.**
  - The endpoint file (`ShoppingCartEndpointApplicationBuilderExtensions.cs`, an `IEndpoints`
    implementation) and the aggregate/handler are **feature code** — editing an endpoint
    delegate and an aggregate method is ordinary feature work.
  - The shared mapper is a **new file** added under `API/Endpoints/Common/`. Adding a file to
    that folder is a feature addition; the stable *mechanism* there (`IEndpoints`,
    `EndpointExtensions`) is **not** modified.
  - `CustomExceptionHandler.cs` is **not touched** — the mapper *mirrors* its `ProblemDetails`
    shapes; it does not change them.
  - No DI line is added: the mapper is a static deep module called directly from the endpoint
    delegate (no `HttpContext`, no injected service). If anything beyond §5 proves necessary —
    a new `Error` type, a `CustomExceptionHandler` change, a base-type change — **stop and ask**;
    that exceeds ADR-002 step 3 for this use-case.

## 2. Context summary

This is ADR-002 step 3's **first conversion**, deliberately built as the **canonical reference**
every later conversion copies. The `assign-client-to-cart` use-case currently runs both error
models at once: the handler returns a `Result` for expected failures, then the endpoint
*re-throws* that `Result` as an exception (`ConflictError → ConflictException`,
`NotFoundError → ContentNotFoundException`, anything else → a bare `throw new Exception(...)`) so
`CustomExceptionHandler` can render it. This slice deletes that bridge: the endpoint resolves the
handler's `Result` with `Match(() ⇒ Results.Ok(), failure ⇒ <shared mapper>(failure))`, where the
failure branch returns an HTTP result **directly** and never throws. The failure branch maps each
`Error` through a single new shared `Error → IResult` translator — the `Result`-side analogue of
`CustomExceptionHandler` — emitting `ProblemDetails` bodies byte-identical in shape to what the
exception path already produces for the same status (`NotFoundError ⇒ 404`,
`ConflictError ⇒ 409`, unrecognised `Error ⇒ 500`). The aggregate method
`ShoppingCart.AssignClientId` is converted to **return** `ConflictError` instead of **throwing**
on the already-assigned case (and appends its domain event only on success), making the handler's
previously-dead `IsFailure` branch live. One latent functional bug is fixed: a successful
assignment now records the **signed-in client id** as the owner, not the cart's own id. Observable
status codes are unchanged; the only intentional behaviour change is the bug fix. The acceptance
gate is a focused unit spec of the new mapper in `BookingManagementService.API.UnitTests`.

## 3. API contract

Mechanism swap only — request/response shapes are unchanged. No new request/response model.

### Endpoint — `PUT /api/shoppingcarts/{ShoppingCartId}/assignclient`

- **Request:** `[FromRoute] Guid ShoppingCartId`; client id read from `ClaimsPrincipal`
  (`GetClientId(user)`). No body. `.RequireAuthorization()` (unchanged).
- **Command:** `AssignClientCartCommand(Guid ShoppingCartId, Guid ClientId)` — unchanged,
  `IRequest<Result>`.
- **Resolution:** `result.Match(() => Results.Ok(), ErrorResults.ToProblem)` — failure branch
  returns an `IResult` directly; the `ConflictError`/`NotFoundError` re-throws and the bare
  `throw new Exception(...)` are **deleted**.
- **Status codes (unchanged in/out):**
  - `200 OK` — success (`Result.Success()`).
  - `404 Not Found` — cart missing (`NotFoundError` from the handler), via the shared mapper.
  - `409 Conflict` — client already owns a *different* active cart (`ConflictError` from the
    handler) **or** the target cart already has an owner (`ConflictError` now *returned* by
    `ShoppingCart.AssignClientId`), via the shared mapper. Both `409`s are now produced by the
    **same** mechanism with the **same** body shape (user stories 3, 4).
  - `500` — unrecognised `Error` kind (programming gap), via `Results.Problem` in the mapper;
    preserves today's bare-throw-to-500 behaviour (user story 13).
- **`.Produces` corrected:** declare `200` / `404` / `409`; drop the stale `201` / `204`
  (user stories 18, 19).

### Shared mapper — `Error → IResult` (new)

`ProblemDetails` parity with `CustomExceptionHandler` (the exception path and the `Result` path
must be indistinguishable to clients — user stories 5, 12):

| Input `Error` | Status | `Type` | `Title` | `Detail` |
|---|---|---|---|---|
| `NotFoundError` | 404 | `https://tools.ietf.org/html/rfc7231#section-6.5.4` | `The specified resource was not found.` | `error.Description` |
| `ConflictError` | 409 | `https://tools.ietf.org/html/rfc7231#section-6.5.8` | `Conflict` | *(none — handler sets none)* |
| any other `Error` | 500 | `https://tools.ietf.org/html/rfc7231#section-6.6.1` | `Internal Server Error` | *(none)* |

Mirrors `HandleContentNotFoundException`/`HandleNotFoundException` (404), `HandleConflictException`
(409, title-only, **no** `Detail`), and `HandleException` (500, title-only) in
`CustomExceptionHandler.cs`. No new `Error` type is introduced; `Error` definitions stay in
`Domain/Error`.

## 4. File structure

```
BookingManagement/
├── BookingManagementService.Domain/
│   └── ShoppingCarts/
│       └── ShoppingCart.cs                                         # EDIT: AssignClientId throws → returns ConflictError; event only on success
├── BookingManagementService.Application/
│   └── ShoppingCarts/Command/AssingClientCart/
│       └── AssingClientShoppingCartCommandHandler.cs               # EDIT: bug fix — pass request.ClientId (not request.ShoppingCartId) to AssignClientId
└── BookingManagementService.API/
    └── Endpoints/
        ├── Common/
        │   └── ErrorResults.cs                                     # NEW: shared Error → IResult mapper (deep module)
        └── ShoppingCartEndpointApplicationBuilderExtensions.cs     # EDIT: assignclient Match→mapper (delete bridge + bare throw); .Produces 200/404/409

tests/
└── BookingManagementService.API.UnitTests/                        # EXISTING project (created by slice 0002)
    └── Endpoints/Common/
        └── ErrorResultsOutsideInTests.cs                          # NEW: RED acceptance gate (written by /slice-test-red, step 5)

BookingManagement/tests/
└── BookingManagementService.Domain.UnitTests/                     # EXISTING (references Application; NSubstitute) — tests written AFTER green
    └── ShoppingCarts/
        ├── AssignClientCartCommandHandlerTests.cs                  # NEW (after green): handler facts incl. bug fix
        └── ShoppingCartSpecification.cs                            # EDIT (after green): add AssignClientId return-vs-throw + event facts
```

No EF Core entity is added or altered → **no migration**.

> Note (project naming, carried from slice 0002): `BookingManagementService.Domain.UnitTests` is
> named "Domain" but its `RootNamespace` is `CinemaTicketBooking.Application.UnitTests` and it
> references the **Application** project with NSubstitute available — the correct home for the
> handler test. The mapper gate cannot live there (it would pull the web host into a
> domain/application test project); it lives in the dedicated `API.UnitTests` project, exactly as
> the `CustomExceptionHandler` gate did in `0002`.

## 5. Implementation steps

1. **Domain — convert `ShoppingCart.AssignClientId` from throw to return.** In
   `Domain/ShoppingCarts/ShoppingCart.cs`, change the already-assigned guard to **return** a
   `ConflictError` instead of throwing, and keep appending the domain event only on the success
   branch (it already does — the event line stays after the guard):
   ```csharp
   public Result AssignClientId(Guid clientId)
   {
       Ensure.NotEmpty(clientId, "The clientId is required.", nameof(clientId)); // structural guard stays an exception (US16)

       EnsurePurchaseIsNotCompleted();

       if (ClientId != Guid.Empty)
           return DomainErrors<ShoppingCart>.ConflictException(
               $"The shopping cart {Id} already has an assigned client."); // was: throw new ConflictException(...)

       ClientId = clientId;

       _domainEvents.Add(new ShoppingCartAssignedToClientDomainEvent(Id)); // success branch only

       return Result.Success();
   }
   ```
   `DomainErrors<ShoppingCart>.ConflictException(...)` returns a `ConflictError` (code
   `ShoppingCart.ConflictException`), which implicitly converts to a failing `Result` (user
   stories 14, 15). `EnsurePurchaseIsNotCompleted()` and `Ensure.NotEmpty` stay exceptions
   (out of scope; US16).

2. **Application — fix the wrong-owner bug.** In
   `Application/ShoppingCarts/Command/AssingClientCart/AssingClientShoppingCartCommandHandler.cs`,
   pass the **client id**, not the cart id, into the aggregate:
   ```csharp
   var result = cart.AssignClientId(request.ClientId); // was: cart.AssignClientId(request.ShoppingCartId)
   ```
   The rest of the handler is unchanged: cart missing ⇒ `NotFoundError`; client already owns a
   *different* active cart ⇒ `ConflictError`; the now-**live** `if (result.IsFailure) return
   result;` propagates the domain `ConflictError`; success persists and returns `Result.Success()`
   (user stories 7, 17, 23).

3. **API — new shared `Error → IResult` mapper (deep module).** Create
   `API/Endpoints/Common/ErrorResults.cs`: a small, framework-thin static class with one public
   entry hiding the whole `Result`-to-HTTP policy (the analogue of `CustomExceptionHandler`).
   It maps by `Error` subtype, emitting `ProblemDetails` shapes identical to the matching
   `CustomExceptionHandler` writers (see §3 table):
   ```csharp
   public static class ErrorResults
   {
       public static IResult ToProblem(Error error) => error switch
       {
           NotFoundError => Results.Problem(
               statusCode: StatusCodes.Status404NotFound,
               type: "https://tools.ietf.org/html/rfc7231#section-6.5.4",
               title: "The specified resource was not found.",
               detail: error.Description),
           ConflictError => Results.Problem(
               statusCode: StatusCodes.Status409Conflict,
               type: "https://tools.ietf.org/html/rfc7231#section-6.5.8",
               title: "Conflict"),
           _ => Results.Problem(
               statusCode: StatusCodes.Status500InternalServerError,
               type: "https://tools.ietf.org/html/rfc7231#section-6.6.1",
               title: "Internal Server Error"),
       };
   }
   ```
   No new `Error` type; no DI registration (called statically from the endpoint). This is the
   reuse point every later step-3 conversion calls (user stories 11, 12, 13). *(Final method
   name/signature is pinned by the step-5 gate test; `ToProblem(Error)` is the planned shape.)*

4. **API — endpoint: replace the bridge with `Match`-to-HTTP.** In
   `ShoppingCartEndpointApplicationBuilderExtensions.cs`, the `assignclient` delegate becomes:
   ```csharp
   var result = await sender.Send(assignClientCartCommand, cancellationToken);

   return result.Match(
       () => Results.Ok(),
       ErrorResults.ToProblem);
   ```
   Delete the `if (failure is ConflictError) throw new ConflictException(...)`, the
   `if (failure is NotFoundError) throw new ContentNotFoundException(...)`, and the
   `throw new Exception(failure.Description)` (user stories 8, 9, 10). Correct the metadata:
   replace `.Produces(201).Produces(204).Produces(409)` with `.Produces(200).Produces(404)
   .Produces(409)` (user stories 18, 19). `.WithName("AssignUser")`, `.WithTags(Tag)`,
   `.RequireAuthorization()` unchanged. Remove now-unused `using` imports
   (`Domain.Exceptions` / `Application.Exceptions`) only if no other delegate in the file needs
   them — verify before deleting.

5. **Verify (pre-test).** From `src/services`:
   ```
   dotnet format CinemaBookingManagement.sln
   dotnet build  CinemaBookingManagement.sln -warnaserror
   ```
   Resolve warnings. The accepted AutoMapper **NU1903** NuGet-audit advisory trips
   `-warnaserror` at restore time (known, accepted — see MEMORY `dotnet10-migration`); handle the
   NuGet audit so the real build/warnings are what is validated, not the advisory.

## 6. Tests planned

The externally observable behaviour is the HTTP status + `ProblemDetails` body per outcome, the
handler's returned `Result`, and the domain transition's outcome + event. There is **no**
`WebApplicationFactory<Program>` harness; the change is pinned by focused unit tests of the
changed units, consistent with `0001`/`0002`.

- **Outside-in / RED acceptance gate — EXISTING project `BookingManagementService.API.UnitTests`,
  `Endpoints/Common/ErrorResultsOutsideInTests.cs`.** xUnit + FluentAssertions + `DefaultHttpContext`.
  Executes the mapper's returned `IResult` against a buffered `DefaultHttpContext`
  (`await result.ExecuteAsync(context)`), then asserts status + `ProblemDetails` body — the same
  technique as `0002`'s `CustomExceptionHandlerContentNotFound404OutsideInTests`:
  1. `NotFoundError` ⇒ `404` + `ProblemDetails` (`Status`/`Type`/`Title`, `Detail == error.Description`).
  2. `ConflictError` ⇒ `409` + `ProblemDetails` (`Status`/`Type`/`Title == "Conflict"`).
  3. an unrecognised `Error` (e.g. `new Error("X.Unknown")`) ⇒ `500` + `ProblemDetails`.
  **RED** until `ErrorResults` exists (build failure, then assertions). Produced and verified RED
  by `/slice-test-red` in step 5.

- **Handler unit test (after green)** —
  `BookingManagement/tests/BookingManagementService.Domain.UnitTests/ShoppingCarts/AssignClientCartCommandHandlerTests.cs`.
  NSubstitute mocks `IActiveShoppingCartRepository` (+ `IShoppingCartLifecycleManager`, `ILogger`).
  Facts: cart missing ⇒ returns `NotFoundError`; client already owns a *different* active cart ⇒
  returns `ConflictError`; success ⇒ `Result.Success()` **and the cart owner equals the client id**
  (pins the bug fix, user stories 7, 23); a domain `IsFailure` is propagated.

- **Domain unit test (after green)** — extend
  `BookingManagement/tests/BookingManagementService.Domain.UnitTests/ShoppingCarts/ShoppingCartSpecification.cs`.
  Facts for `AssignClientId`: already-assigned cart ⇒ returns `ConflictError` and raises **no**
  `ShoppingCartAssignedToClientDomainEvent`; success ⇒ assigns the owner and raises the event
  (user story 24).

**Opt-outs (explicit, per `agent_docs/testing.md` and the PRD Testing Decisions):**
- **`WebApplicationFactory` end-to-end / endpoint integration test — skipped:** no HTTP harness
  exists; standing one up for one endpoint is disproportionate. The endpoint's `Match` wiring is
  covered by compilation + the mapper gate (user story 22; PRD "Out of the net").
- **Repository / adapter unit test — skipped:** no repository or adapter logic changes (no new
  business-meaningful infrastructure-exception translation on this path).

**Full-suite gate:** the entire `dotnet test` run, **including
`BookingManagementService.Domain.ArchitectureTests`**, must stay green (user stories 25, 26).

## 7. Out of scope for this slice

- Converting any other use-case (`ReserveTickets`, `PurchaseTickets`, `SelectSeats`) — later
  slices that **reuse this slice's mapper** (user story 26).
- Adopting `Result<T>` (the generic) in any handler — `AssignClientCart` uses non-generic `Result`.
- Deduplicating `NotFoundException` vs `ContentNotFoundException`, or relocating the misplaced
  `ContentNotFoundException` file — its own ADR-gated slice.
- Replacing the bare `throw new Exception(...)` in `CreateMovieSessionCommandHandler`,
  `ReserveTicketsCommandHandler`, `MovieSessionSeatService`, or the endpoints' `GetClientId`
  helper.
- Standing up a `WebApplicationFactory` HTTP integration harness.
- Changing the `CustomExceptionHandler` mechanism, the MediatR pipeline, the validation behaviour,
  or any base type.
- Flipping ADR-002 to Accepted.

## 8. Open questions

None. The mapper's home (`API/Endpoints/Common/`), its static deep-module shape, the
`ProblemDetails` parity with `CustomExceptionHandler` (409 is title-only with no `Detail`; 404
carries `Detail`), the throw→return conversion of `AssignClientId`, the owner-bug fix, the gate
living in the existing `API.UnitTests` project, and the "no `WebApplicationFactory`" decision are
all settled by the PRD's grill-me-derived Implementation/Testing Decisions.
