# 0006 · PurchaseTickets Result→HTTP — Requirements

## Inputs

- PRD: ./prd.md
- Plan: ./plan.md

> Contract note: HTTP statuses below are derived from the `CustomExceptionHandler` mapping table in
> `agent_docs/error_handling.md` (`ContentNotFoundException → 404`, `ConflictException → 409`,
> anything else `→ 500`) and, for the converted paths, the shared `ErrorResults.ToProblem` mapper
> introduced by slice `0003` (`NotFoundError → 404`, `ConflictError → 409`, any other `Error → 500`).
> This slice changes **neither** the exception table **nor** the mapper. Like `0005`, it is
> **deliberately behaviour-changing**: every failing `Result` the purchase handler already returns
> (cart-not-found, session-not-found, terminated, seat-already-sold, seat-held-by-another-cart) is
> today serialized as `200` because the endpoint does `return result;`; routing it through the mapper
> moves each to its correct `404`/`409`. It additionally retypes the one `MovieSessionSeat.Sell`
> "another cart" case from `InvalidOperation` to `ConflictError` so that case maps to `409` rather
> than the mapper's `_ => 500` arm. Being the final write-path conversion, it also flips ADR-002 to
> Accepted and reconciles two docs (N11–N13).
>
> Open-question resolutions assumed by these requirements (confirm before red): the product flow is
> **select → reserve → purchase**, so `PurchaseComplete()` on `InWork` is a `ConflictError` (F17, PRD
> Further Notes); and the idempotent already-purchased `Result.Success()` is a **domain-method**
> contract, while a real re-`POST /purchase` surfaces as `409` at the endpoint via `Sell`'s `Sold`
> guard (F16/F26, PRD Further Notes) — no endpoint-level `200` for re-purchase is asserted anywhere.

## Functional requirements

### Endpoint

- **F1.** The endpoint `POST /api/shoppingcarts/{ShoppingCartId}/purchase` resolves the handler's
  `Result` with `result.Match(() => Results.Ok(), ErrorResults.ToProblem)`, returning HTTP `200 OK`
  with an **empty body** on success and an `IResult` from the shared mapper on failure — the failure
  branch never throws.
- **F2.** The `purchase` endpoint's previous `return result;` (which serialized the `Result` object
  as a `200` JSON body) is removed; a successful purchase now returns an empty `200`.
- **F3.** The `purchase` endpoint reuses the existing shared mapper `ErrorResults.ToProblem(Error)`
  from `API/Endpoints/Common/` unchanged; no new mapping module is created and `ErrorResults.cs` is
  not edited.
- **F4.** The `purchase` endpoint's OpenAPI metadata declares `.Produces(200)`, `.Produces(404)`,
  `.Produces(409)` and drops the stale `.Produces<bool>(201)` and `.Produces(204)`.
- **F5.** The `purchase` endpoint retains `.WithName("PurchaseSeats")` and `.WithTags(Tag)`, takes
  `[FromRoute] Guid ShoppingCartId` with no request body, and maps it to
  `PurchaseTicketsCommand(ShoppingCartId)` unchanged.

### Handler — `PurchaseTicketsCommandHandler`

- **F6.** The handler returns a `NotFoundError` (`DomainErrors<ShoppingCart>.NotFound`) when the
  shopping cart does not exist (`IActiveShoppingCartRepository.GetByIdAsync` returns `null`),
  surfacing as HTTP `404` via the mapper (already present from prior work; preserved unchanged).
- **F7.** The handler propagates the failing `Result` returned by `MovieSessionSeatService.SelSeats`
  (movie-session-not-found ⇒ `NotFoundError → 404`; sales-terminated ⇒ `ConflictError → 409`;
  seat-already-sold ⇒ `ConflictError → 409`; seat-held-by-another-cart ⇒ `ConflictError → 409` after
  the F19 retype) unchanged to the endpoint (the `SelSeats` short-circuit is already present from
  prior work; preserved unchanged).
- **F8.** The handler consumes the `Result` returned by `cart.PurchaseComplete()` and, when it is a
  failure (`ConflictError` for a non-completable status, surfacing as HTTP `409`), short-circuits and
  returns it; the previous unconditional `void` call `cart.PurchaseComplete();` is removed.
- **F9.** The handler short-circuits and returns any failing `Result` **before**
  `IActiveShoppingCartRepository.SaveAsync(cart)`, `IShoppingCartLifecycleManager.DeleteAsync(cart.Id)`,
  and the per-seat `IShoppingCartSeatLifecycleManager.DeleteAsync(...)` calls, so a cart is never
  persisted as `PurchaseCompleted` and no lifecycle side-effect runs when the completion was not legal
  (atomicity invariant).
- **F10.** On the happy path the handler returns `Result.Success()` after `SaveAsync`, the
  cart-lifecycle `DeleteAsync(cart.Id)`, and deleting each held seat's selection-lifecycle entry,
  surfacing as HTTP `200`.
- **F11.** Genuinely unexpected faults on the path (repository failures, the Redis
  `IShoppingCartLifecycleManager` / `IShoppingCartSeatLifecycleManager` managers, and the
  `ClientId`-empty invariant thrown by `PurchaseComplete`) continue to propagate as exceptions to
  `CustomExceptionHandler` (HTTP `500`); they are not converted to `Result`s.

### Aggregate — `ShoppingCart.PurchaseComplete`

- **F12.** `ShoppingCart.PurchaseComplete()` is retyped from `void` to `Result`.
- **F13.** `PurchaseComplete()` keeps `Ensure.NotEmpty(ClientId, …)` as a **throw**: a cart reaching
  purchase without an assigned client is an invariant violation (`500`-class), evaluated first,
  independent of status.
- **F14.** `PurchaseComplete()` on a cart in status `SeatsReserved` transitions it to
  `PurchaseCompleted`, appends exactly one `ShoppingCartPurchaseDomainEvent`, and returns
  `Result.Success()`.
- **F15.** `PurchaseComplete()` no longer calls the shared `EnsurePurchaseIsNotCompleted()` guard,
  which is left unchanged for its other callers (`CalculateCartAmount`, …); the not-completed check is
  inlined as a `Result`-returning guard.
- **F16.** `PurchaseComplete()` on a cart already in status `PurchaseCompleted` returns
  `Result.Success()` and appends **no** `ShoppingCartPurchaseDomainEvent` (idempotent; a
  domain-method-level `409 → 200` change), replacing the previous `EnsurePurchaseIsNotCompleted()`
  thrown `ConflictException`.
- **F17.** `PurchaseComplete()` on any status other than `SeatsReserved` or `PurchaseCompleted` (e.g.
  `InWork`, `Deleted`) returns a `ConflictError` (`DomainErrors<ShoppingCart>.ConflictException`,
  surfacing as HTTP `409`) and appends no event (resolved decision: purchasing requires a prior
  reservation).
- **F18.** `PurchaseComplete()` appends the `ShoppingCartPurchaseDomainEvent` **only** on a genuine
  `SeatsReserved → PurchaseCompleted` transition, fixing the prior unconditional-event bug (the old
  `void` code appended the event even when the `if (Status == SeatsReserved)` guard did not fire).

### Seat transition — `MovieSessionSeat.Sell`

- **F19.** `MovieSessionSeat.Sell`'s "the place is already being processed by another shopping cart"
  case (`ShoppingCartId != shoppingCartId`) is retyped from
  `DomainErrors<MovieSessionSeat>.InvalidOperation(...)` to
  `DomainErrors<MovieSessionSeat>.ConflictException(...)`, so seat contention on the purchase path
  maps to HTTP `409` via the existing mapper instead of falling into the `_ => 500` arm.
- **F20.** `MovieSessionSeat.Sell`'s already-`Sold` case is unchanged and continues to return a
  `ConflictError` (HTTP `409`).
- **F21.** No new mapper arm is added and `InvalidOperation` keeps mapping to HTTP `500` (it continues
  to mean "genuinely unexpected"); only the `Error` kind returned by the one `Sell` case changes.

### Shared helper (unchanged)

- **F22.** The shared `GetMovieSessionSeat` helper is unchanged: a missing seat record for a valid
  session continues to throw `ContentNotFoundException` (HTTP `404`), as in `0005`.

### Observable status contract

- **F23.** Status is **preserved** for: success (`200`, body now empty) and movie-session-seat-not-found
  via `GetMovieSessionSeat` (`404`), `ClientId`-empty invariant (`500`), and repository/Redis faults
  (`500`).
- **F24.** Status is **corrected** (intentional behaviour change), each moving off the interim `200`
  that `0005` parked: shopping-cart-not-found `200 → 404`; movie-session-not-found `200 → 404`;
  sales-terminated `200 → 409`; seat-already-sold `200 → 409`; seat-held-by-another-cart `200 → 409`
  (via the F19 retype, avoiding the `InvalidOperation → 500` trap); cart-not-completable (`InWork`)
  `200`-buggy ⇒ `409`; and the success response body changes from a serialized `Result` object to
  empty (status stays `200`).
- **F25.** This slice closes the interim purchase-path regression `0005` accepted (movie-session-not-found
  and sales-terminated serializing as `200` on the purchase path); after this slice the entire
  `ShoppingCarts` write path reports expected outcomes through the `Result → mapper` model.
- **F26.** No endpoint-level `200` for a re-`POST /purchase` on a completed cart is asserted: on a
  fully-completed cart the seats are already `Sold`, so `MovieSessionSeat.Sell`'s `Sold` guard returns
  a `ConflictError` (`409`) **before** `PurchaseComplete()` is reached; the idempotent
  `Result.Success()` of F16 is a domain-method contract pinned by a domain test, not an endpoint test.

### ADR-002 adoption close-out (docs)

- **F27.** `docs/adr/ADR-002-error-handling-model-result-vs-exceptions.md` is flipped from status
  `Proposed` to `Accepted`, dated `2026-06-04`; only the status/date is changed, the ADR body is not
  rewritten.
- **F28.** `agent_docs/error_handling.md` is rewritten from "two models coexist / undecided" to the
  decided hybrid (expected business outcome ⇒ `Result`; in-aggregate transition that raises a domain
  event ⇒ `Result`; structural validation ⇒ `ValidationBehaviour`/`ValidationException`;
  unexpected/infrastructure ⇒ exception ⇒ `CustomExceptionHandler`; the endpoint `Result → exception`
  bridge is gone), naming the deliberately un-converted tails (read/query `ContentNotFoundException`,
  the shared `GetMovieSessionSeat`, the `ClientId`-empty invariant throw) as **intentional** exception
  usage.
- **F29.** `CLAUDE.md` rule #9 and the project-at-a-glance "the error model is not yet unified" line
  are amended from "not yet unified / undecided" to "decided — see ADR-002," stating the hybrid split;
  no other rule and not the locked-stack table is touched.

## Non-functional requirements

- **N1.** The converted use-case remains a MediatR `IRequestHandler<PurchaseTicketsCommand, Result>`
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
  method and the seat transition express their business conflicts as `Result`s, and HTTP shaping
  happens only in the API layer. Per `CLAUDE.md` rule 5.
- **N6.** The endpoint delegate contains no business logic: it binds the route value, builds the
  command, calls `ISender.Send`, and shapes the HTTP result via `Match`. Per
  `agent_docs/entry_points/minimal-api.md`.
- **N7.** The `CustomExceptionHandler` mechanism and all its writers, and the shared `ErrorResults`
  mapper, are unchanged; this slice only re-routes outcomes through the existing policy objects (the
  `Sell` retype changes the `Error` kind the domain returns, not the mapper). Per
  `agent_docs/stable_vs_feature.md`.
- **N8.** All I/O remains `async`/`await` with `CancellationToken` threaded through; no synchronous
  database or I/O call is introduced. Per `CLAUDE.md`.
- **N9.** The build produces no new warnings (`dotnet build -warnaserror`), accounting for the
  accepted AutoMapper `NU1903` NuGet-audit advisory and the pre-existing nullable baseline debt. Per
  `CLAUDE.md` § Verifying changes and MEMORY `dotnet10-migration` / `warnaserror-baseline-debt`.
- **N10.** Architecture tests (`BookingManagementService.Domain.ArchitectureTests`) and the full
  `dotnet test` suite pass without new failures. Per `CLAUDE.md` § Verifying changes.
- **N11.** ADR-002 is flipped to **Accepted** in this slice (dated 2026-06-04); this is the final
  step-3 conversion that completes the `ShoppingCarts` write path and therefore carries the adoption
  close-out. Per the PRD.
- **N12.** The two STABLE doc edits (`agent_docs/error_handling.md`, `CLAUDE.md` rule #9 + the
  project-at-a-glance line) change wording only — no mechanism, no base type, no pipeline. Per
  `agent_docs/stable_vs_feature.md` (docs reconciliation, not a mechanism change).
- **N13.** `PurchaseTickets` is the **only** converted use-case in this slice; `MovieSessionSeat.Sell`
  is touched only for the one mislabelled case (F19), and the shared `GetMovieSessionSeat` /
  `EnsurePurchaseIsNotCompleted` helpers are left unchanged. Per the PRD.

## Out of scope

- Converting any use-case other than `PurchaseTickets` — this is the final step-3 conversion.
- Converting any read/query handler (or the shared `GetMovieSessionSeat`) that throws
  `ContentNotFoundException` to `Result` — intentional exception usage, documented as such.
- Modifying the shared `EnsurePurchaseIsNotCompleted` guard (still used by `CalculateCartAmount` /
  others).
- Adopting `Result<T>` (the generic) on this path; `PurchaseTickets` uses the non-generic `Result`.
- Adding a `400`-class arm to `ErrorResults.ToProblem` or otherwise changing the shared mapper /
  introducing a new `Error` kind.
- The bare `throw` in the endpoints' `GetClientId` / `CreateShoppingCart` helpers (slice `0007`).
- Fixing the pre-existing seat-then-cart transactional ordering gap inside `SelSeats`.
- Changing `CustomExceptionHandler`, the MediatR pipeline, the validation behaviour, or any base
  type.
- Standing up a `WebApplicationFactory` HTTP integration harness, or a real-concurrency seat-race
  integration test.
- The Flutter client follow-up to the `0002` `204 → 404` contract change.

## Traceability

| Requirement | Verified by |
|---|---|
| F1 | code review checklist (validation.md); endpoint `Match` wiring verified by compilation + handler gate |
| F2 | code review checklist (`return result;` removed; empty `200`) |
| F3 | code review checklist (mapper reused, `ErrorResults.cs` unchanged) |
| F4 | code review checklist (`.Produces` declarations) |
| F5 | code review checklist (endpoint metadata + command mapping) |
| F6 | `PurchaseTicketsCommandHandler` unit test (cart missing ⇒ `NotFoundError`; `SaveAsync` not received) |
| F7 | `PurchaseTicketsCommandHandler` unit test (session missing ⇒ `NotFoundError`; terminated ⇒ `ConflictError`; seat held by another cart ⇒ `ConflictError`) |
| F8 | `PurchaseTicketsCommandHandler` unit test (non-completable status ⇒ `ConflictError`; `SaveAsync` not received) + code review (`void` call removed) |
| F9 | `PurchaseTicketsCommandHandler` unit test (any failure ⇒ `SaveAsync`/cart-lifecycle/per-seat `DeleteAsync` not received) |
| F10 | `PurchaseTicketsCommandHandler` unit test (success ⇒ `Result.Success()`, cart saved, cart-lifecycle removed, per-seat deletes) |
| F11 | code review checklist (repository/Redis faults and `ClientId`-empty invariant still propagate as exceptions) |
| F12 | compilation (`Result` signature) + `ShoppingCart.PurchaseComplete` domain unit test |
| F13 | code review checklist (`Ensure.NotEmpty(ClientId)` stays a throw, evaluated first) |
| F14 | `ShoppingCart.PurchaseComplete` domain unit test (`SeatsReserved` ⇒ `PurchaseCompleted` + event) |
| F15 | code review checklist (`EnsurePurchaseIsNotCompleted` no longer called from `PurchaseComplete`, unchanged) |
| F16 | `ShoppingCart.PurchaseComplete` domain unit test (`PurchaseCompleted` ⇒ `Result.Success()`, no event) |
| F17 | `ShoppingCart.PurchaseComplete` domain unit test (`InWork` ⇒ `ConflictError`, no event) |
| F18 | `ShoppingCart.PurchaseComplete` domain unit test (event only on genuine transition) + code review |
| F19 | `MovieSessionSeat.Sell` domain unit test (another cart ⇒ `ConflictError`) + `PurchaseTicketsCommandHandler` unit test (seat held by another cart ⇒ `ConflictError`) + code review |
| F20 | `MovieSessionSeat.Sell` domain unit test (already-`Sold` ⇒ `ConflictError`) |
| F21 | code review checklist (no new mapper arm; `InvalidOperation` still `500`) |
| F22 | code review checklist (`GetMovieSessionSeat` unchanged) |
| F23 | handler + domain unit tests (per-outcome `Result`) + code review (preserved statuses) |
| F24 | `PurchaseTicketsCommandHandler` unit test (each corrected outcome ⇒ `NotFoundError`/`ConflictError`) + code review (empty success body) |
| F25 | code review checklist (interim purchase-path regression closed; whole write path on `Result → mapper`) |
| F26 | `ShoppingCart.PurchaseComplete` domain unit test (idempotent `Success`) + code review (no endpoint-level `200` re-purchase asserted) |
| F27 | code review checklist (ADR-002 status `Accepted`, dated 2026-06-04) |
| F28 | code review checklist (`agent_docs/error_handling.md` rewritten; intentional tails named) |
| F29 | code review checklist (`CLAUDE.md` rule #9 + project-at-a-glance amended; no other rule touched) |
| N1–N13 | code review checklist in validation.md + architecture tests + full suite |
