# 0005 · ReserveTickets Result→HTTP — Requirements

## Inputs

- PRD: ./prd.md
- Plan: ./plan.md

> Contract note: HTTP statuses below are derived from the `CustomExceptionHandler` mapping table in
> `agent_docs/error_handling.md` (`ContentNotFoundException → 404`, `ConflictException → 409`,
> anything else `→ 500`) and, for the converted paths, the shared `ErrorResults.ToProblem` mapper
> introduced by slice `0003` (`NotFoundError → 404`, `ConflictError → 409`, any other `Error → 500`).
> This slice changes **neither** the exception table **nor** the mapper. Unlike `0004`, it is
> **deliberately behaviour-changing**: it routes two outcomes that today reach `500` (via bare
> `throw new Exception(...)`) through the existing mapper as `ConflictError → 409`, and empties a
> success body that today serializes a `Result` object — while preserving every other status.

## Functional requirements

### Endpoint

- **F1.** The endpoint `POST /api/shoppingcarts/{ShoppingCartId}/reservations` resolves the handler's
  `Result` with `result.Match(() => Results.Ok(), ErrorResults.ToProblem)`, returning HTTP `200 OK`
  with an **empty body** on success and an `IResult` from the shared mapper on failure — the failure
  branch never throws.
- **F2.** The `reservations` endpoint's previous `return result;` (which serialized the `Result`
  object as a `200` JSON body) is removed; a successful reservation now returns an empty `200`.
- **F3.** The `reservations` endpoint reuses the existing shared mapper `ErrorResults.ToProblem(Error)`
  from `API/Endpoints/Common/` unchanged; no new mapping module is created and `ErrorResults.cs` is
  not edited.
- **F4.** The `reservations` endpoint's OpenAPI metadata declares `.Produces(200)`, `.Produces(404)`,
  `.Produces(409)` and drops the stale `.Produces<bool>(201)` and `.Produces(204)`.
- **F5.** The `reservations` endpoint retains `.WithName("ReserveSeats")` and `.WithTags(Tag)`, takes
  `[FromRoute] Guid ShoppingCartId` with no request body, and maps it to
  `ReserveTicketsCommand(ShoppingCartId)` unchanged.

### Handler — `ReserveTicketsCommandHandler`

- **F6.** The handler returns a `NotFoundError` (`DomainErrors<ShoppingCart>.NotFound`) when the
  shopping cart does not exist (`IActiveShoppingCartRepository.GetByIdAsync` returns `null`),
  surfacing as HTTP `404` via the mapper, instead of throwing `ContentNotFoundException`; the
  `GetShoppingCartOrThrow` helper is removed.
- **F7.** The handler consumes the `Result` returned by `cart.SeatsReserve()` and, when it is a
  failure (`PurchaseCompleted` ⇒ `ConflictError`, surfacing as HTTP `409`), short-circuits and
  returns it before any seat reservation or persistence.
- **F8.** The handler propagates the failing `Result` returned by
  `MovieSessionSeatService.ReserveSeats` (movie-session-not-found ⇒ `NotFoundError → 404`;
  sales-terminated ⇒ `ConflictError → 409`; a seat not reservable ⇒ `ConflictError → 409`) unchanged
  to the endpoint.
- **F9.** The bare `throw new Exception($"Couldn't Reserve …")` in the handler is deleted; the
  failing `Result` from `ReserveSeats` is returned (`return result;`) rather than re-thrown, so a
  real domain `Error` is no longer discarded and downgraded to `500`.
- **F10.** The handler short-circuits and returns any failing `Result` **before**
  `IActiveShoppingCartRepository.SaveAsync(cart)`, `IShoppingCartLifecycleManager.SetAsync(cart.Id)`,
  and the per-seat `IShoppingCartSeatLifecycleManager.DeleteAsync(...)` calls, so a cart is never
  persisted as `SeatsReserved` and no lifecycle side-effect runs when a seat could not be reserved
  (atomicity invariant).
- **F11.** On the happy path the handler returns `Result.Success()` after `SaveAsync`, `SetAsync`,
  and deleting each held seat's selection-lifecycle entry, surfacing as HTTP `200`.
- **F12.** Genuinely unexpected faults on the path (repository failures, the Redis
  `IShoppingCartLifecycleManager` / `IShoppingCartSeatLifecycleManager` managers) continue to
  propagate as exceptions to `CustomExceptionHandler` (HTTP `500`); they are not converted to
  `Result`s.

### Aggregate — `ShoppingCart.SeatsReserve`

- **F13.** `ShoppingCart.SeatsReserve()` is retyped from `void` to `Result`.
- **F14.** `SeatsReserve()` on a cart in status `InWork` transitions it to `SeatsReserved`, appends
  exactly one `ShoppingCartReservedDomainEvent`, and returns `Result.Success()`.
- **F15.** `SeatsReserve()` on a cart already in status `SeatsReserved` returns `Result.Success()`
  and appends **no** `ShoppingCartReservedDomainEvent` (idempotent; fixes the prior unconditional-event
  bug).
- **F16.** `SeatsReserve()` on a cart in status `PurchaseCompleted` returns a `ConflictError`
  (`DomainErrors<ShoppingCart>.ConflictException`, surfacing as HTTP `409`) and appends no event,
  replacing the previous `EnsurePurchaseIsNotCompleted()` thrown `ConflictException`.
- **F17.** `SeatsReserve()` on any other status (e.g. `Deleted`) returns a `ConflictError` and appends
  no event (resolved decision; the prior `void` code's latent "event without transition" path is
  removed).
- **F18.** `SeatsReserve()` no longer calls the shared `EnsurePurchaseIsNotCompleted()` guard, which
  is left unchanged for its other callers (`PurchaseComplete`, `CalculateCartAmount`, …).

### Shared domain service — `MovieSessionSeatService`

- **F19.** `MovieSessionSeatService.CheckSeatSaleAvailability` is retyped from `Task` (void) to
  `Task<Result>`: movie-session-not-found returns `NotFoundError`
  (`DomainErrors<MovieSession>.NotFound`); sales-terminated returns `ConflictError`
  (`DomainErrors<MovieSession>.ConflictException`), replacing the bare `throw new Exception(...)`;
  success returns `Result.Success()`.
- **F20.** All three callers of `CheckSeatSaleAvailability` — `SelSeats`, `ReserveSeats`, and
  `SelectSeat` — consume its `Result` and short-circuit (`return availability;`) on `IsFailure`
  before any further work.
- **F21.** `MovieSessionSeat.Reserve` is unchanged and already returns a `ConflictError`
  (`DomainErrors<MovieSessionSeat>.ConflictException`) for the "status should be selected or
  available" case, so the seat-not-reservable path maps to HTTP `409` with no `InvalidOperation → 500`
  trap on the reserve path.
- **F22.** The shared `GetMovieSessionSeat` helper is unchanged: a missing seat record for a valid
  session continues to throw `ContentNotFoundException` (HTTP `404`).

### Observable status contract

- **F23.** Status is **preserved** for: success (`200`), shopping-cart-not-found (`404`),
  movie-session-not-found (`404`), and already-purchased (`409`).
- **F24.** Status is **corrected** (intentional behaviour change): seat-not-reservable
  `500 → 409`, sales-terminated `500 → 409`, and the success response body changes from a serialized
  `Result` object to empty (status stays `200`).
- **F25.** The retype of the shared `CheckSeatSaleAvailability` has a known, accepted interim
  side-effect on the not-yet-converted purchase path (`PurchaseTickets`): because its endpoint still
  does `return result;`, movie-session-not-found and sales-terminated on the purchase path serialize
  as `200`-with-body until slice `0006` converts that endpoint; the `reservations` path is unaffected.

## Non-functional requirements

- **N1.** The converted use-case remains a MediatR `IRequestHandler<ReserveTicketsCommand, Result>`
  with the command a `record` implementing `IRequest<Result>`; it is not converted to another style.
  Per `agent_docs/architecture.md`.
- **N2.** No use-case sets `HttpContext.Response.StatusCode`; the success status comes from the
  endpoint (`Results.Ok`) and every converted failure status comes from the shared mapper's
  `IResult`. Per `CLAUDE.md` rule 5 and `agent_docs/error_handling.md`.
- **N3.** No new cross-cutting `*Exception` or `Error` type is introduced; the conversion reuses the
  existing `DomainErrors<T>.ConflictException(...)` / `.NotFound(...)` factories and the
  `ConflictError` / `NotFoundError` kinds from `Domain/Error`. Per `CLAUDE.md` § Forbidden.
- **N4.** `Domain` and `Application` contain no EF Core types, `DbContext`, ASP.NET, or
  `IResult`/`HttpContext` references; the `Error → IResult` mapping lives only in the `API` layer.
  Per `agent_docs/architecture.md` (Dependency Rule).
- **N5.** No handler raises an HTTP-transport exception or writes to `HttpContext`; the aggregate
  method and shared domain service express their business conflicts as `Result`s, and HTTP shaping
  happens only in the API layer. Per `CLAUDE.md` rule 5.
- **N6.** The endpoint delegate contains no business logic: it binds the route value, builds the
  command, calls `ISender.Send`, and shapes the HTTP result via `Match`. Per
  `agent_docs/entry_points/minimal-api.md`.
- **N7.** The `CustomExceptionHandler` mechanism and all its writers, and the shared `ErrorResults`
  mapper, are unchanged; this slice only re-routes outcomes through the existing policy objects. Per
  `agent_docs/stable_vs_feature.md`.
- **N8.** All I/O remains `async`/`await` with `CancellationToken` threaded through; no synchronous
  database or I/O call is introduced. Per `CLAUDE.md`.
- **N9.** The build produces no new warnings (`dotnet build -warnaserror`), accounting for the
  accepted AutoMapper `NU1903` NuGet-audit advisory. Per `CLAUDE.md` § Verifying changes and MEMORY
  `dotnet10-migration`.
- **N10.** Architecture tests (`BookingManagementService.Domain.ArchitectureTests`) and the full
  `dotnet test` suite pass without new failures. Per `CLAUDE.md` § Verifying changes.
- **N11.** ADR-002 remains **Proposed**; this slice does not flip it to Accepted nor update
  `agent_docs/error_handling.md` (deferred to slice `0006`). Per the PRD.
- **N12.** `ReserveTickets` is the **only** converted use-case in this slice; `PurchaseTickets` and
  the shared `GetMovieSessionSeat` / `EnsurePurchaseIsNotCompleted` helpers are left unchanged for
  later slices. The shared `CheckSeatSaleAvailability` is touched only because `ReserveTickets`
  requires it (with the accepted interim side-effect of F25). Per the PRD.

## Out of scope

- Converting any use-case other than `ReserveTickets` (`PurchaseTickets` is slice `0006`, which must
  also handle `MovieSessionSeat.Sell`'s `InvalidOperation` `409 → 500` trap).
- Converting the shared `GetMovieSessionSeat` (seat-not-found) helper to `Result`.
- Modifying the shared `EnsurePurchaseIsNotCompleted` guard (still used by `PurchaseComplete` /
  `CalculateCartAmount`).
- Adopting `Result<T>` (the generic) on this path; `ReserveTickets` uses the non-generic `Result`.
- Adding a `400`-class arm to `ErrorResults.ToProblem` or otherwise changing the shared mapper /
  introducing a new `Error` kind.
- The bare `throw` in the endpoints' `GetClientId` / `CreateShoppingCart` helpers.
- Fixing the pre-existing seat-then-cart transactional ordering gap inside `ReserveSeats`.
- Changing `CustomExceptionHandler`, the MediatR pipeline, the validation behaviour, or any base
  type.
- Standing up a `WebApplicationFactory` HTTP integration harness, or a real-concurrency seat-race
  integration test.
- Flipping ADR-002 to Accepted and updating `agent_docs/error_handling.md`.
- The Flutter client follow-up to the `0002` `204 → 404` contract change.

## Traceability

| Requirement | Verified by |
|---|---|
| F1 | code review checklist (validation.md); endpoint `Match` wiring verified by compilation + handler gate |
| F2 | code review checklist (`return result;` removed; empty `200`) |
| F3 | code review checklist (mapper reused, `ErrorResults.cs` unchanged) |
| F4 | code review checklist (`.Produces` declarations) |
| F5 | code review checklist (endpoint metadata + command mapping) |
| F6 | `ReserveTicketsCommandHandler` unit test (cart missing ⇒ `NotFoundError`; `SaveAsync` not received) |
| F7 | `ReserveTicketsCommandHandler` unit test (already-purchased ⇒ `ConflictError`; `SaveAsync` not received) |
| F8 | `ReserveTicketsCommandHandler` unit test (session missing ⇒ `NotFoundError`; terminated ⇒ `ConflictError`; seat not reservable ⇒ `ConflictError`) |
| F9 | `ReserveTicketsCommandHandler` unit test (reserve conflict returned, not thrown) + code review (bare `throw` deleted) |
| F10 | `ReserveTicketsCommandHandler` unit test (any failure ⇒ `SaveAsync`/`SetAsync`/`DeleteAsync` not received) |
| F11 | `ReserveTicketsCommandHandler` unit test (success ⇒ `Result.Success()`, cart saved, lifecycle set, per-seat deletes) |
| F12 | code review checklist (repository/Redis faults still propagate as exceptions) |
| F13 | compilation (`Result` signature) + `ShoppingCart.SeatsReserve` domain unit test |
| F14 | `ShoppingCart.SeatsReserve` domain unit test (`InWork` ⇒ `SeatsReserved` + event) |
| F15 | `ShoppingCart.SeatsReserve` domain unit test (`SeatsReserved` ⇒ `Result.Success()`, no event) |
| F16 | `ShoppingCart.SeatsReserve` domain unit test (`PurchaseCompleted` ⇒ `ConflictError`, no event) |
| F17 | code review checklist (non-listed status ⇒ `ConflictError`, no event) |
| F18 | code review checklist (`EnsurePurchaseIsNotCompleted` no longer called from `SeatsReserve`, unchanged) |
| F19 | `ReserveTicketsCommandHandler` unit test (session missing ⇒ `NotFoundError`; terminated ⇒ `ConflictError`) + code review (`Task<Result>` signature, bare `throw` deleted) |
| F20 | compilation (all three callers consume `Result`) + `0004` `SelectSeatCommandHandlerTests` regression + code review |
| F21 | code review checklist (`MovieSessionSeat.Reserve` unchanged, returns `ConflictError`) |
| F22 | code review checklist (`GetMovieSessionSeat` unchanged) |
| F23 | handler + domain unit tests (per-outcome `Result`) + code review (preserved statuses) |
| F24 | `ReserveTicketsCommandHandler` unit test (terminated / seat-not-reservable ⇒ `ConflictError`) + code review (empty success body) |
| F25 | code review checklist (purchase-path interim side-effect acknowledged; `0006` follow-up flagged) |
| N1–N12 | code review checklist in validation.md + architecture tests + full suite |
