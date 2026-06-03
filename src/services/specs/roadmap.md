# Roadmap — BookingManagement service slices

Global index of slices for the `.NET` BookingManagement service. `NNNN` is a global
zero-padded counter (max existing + 1), not per-aggregate. This file is **owned by
`/to-prd`**; other skills read it but never write to it.

States: Started · Planned · Formalized · Validated · Test-specified · Red gate set ·
Complete. (See `agent_docs/spec_workflow.md`.)

| NNNN | Module / Aggregate | Slice | State | Notes |
|---|---|---|---|---|
| 0001 | platform | error_model_result_infrastructure | Complete | ADR-002 step 1: introduce `Result<T>`, fix `DomainErrors<T>` codes, rename `NotFoundError`. Infrastructure only — no use-case, no HTTP, no contract change. |
| 0002 | platform | content_not_found_404 | Complete | ADR-002 step 2: flip `ContentNotFoundException` `204 → 404` centrally, with empty-state carve-outs (`current` cart ⇒ `204`, movie sessions list ⇒ `200 []`). Server-only; Flutter client deferred. |
| 0003 | platform | assign_client_cart_result_http | Complete | ADR-002 step 3 (first, canonical): convert `AssignClientCart` end-to-end — remove the endpoint `Result → exception` bridge, add a shared `Error → IResult` mapper (`NotFoundError`⇒404, `ConflictError`⇒409, else 500), make `ShoppingCart.AssignClientId` return `ConflictError` (event on success), fix the wrong-owner bug. Status-preserving (200/404/409); template for later conversions. |
| 0004 | platform | select_seats_result_http | Complete | ADR-002 step 3 (second): convert `SelectSeats` end-to-end — remove the endpoint `Match→BadRequest` dead branch and the hidden `Result→ConflictException` bridge inside `MovieSessionSeatService.SelectSeat` (retyped `Task<Result>`), make `MovieSessionSeat.Select` return `ConflictError` (not base `Error`) for the "another cart" case to avoid a silent 409→500, reuse 0003's `ErrorResults.ToProblem`, preserve the "cart not saved on failed claim" atomicity, fix OpenAPI `200/404/409`. Status-preserving; non-generic `Result`. Defers shared-helper defects (`CheckSeatSaleAvailability` bare `Exception`) and `Reserve`/`Purchase`. |
| 0005 | platform | reserve_tickets_result_http | Started | ADR-002 step 3 (third): convert `ReserveTickets` end-to-end — endpoint `return result;` → `Match(Ok, ErrorResults.ToProblem)`; delete the handler's bare `throw new Exception("Couldn't Reserve")` bridge; handler cart-not-found → `NotFoundError`; retype shared `MovieSessionSeatService.CheckSeatSaleAvailability` `void → Task<Result>` (session-not-found → `NotFoundError`, sales-terminated → `ConflictError`), threaded through `SelSeats`/`ReserveSeats`/`SelectSeat` with 0004's handler test as regression gate; convert `ShoppingCart.SeatsReserve` `void → Result` (event on genuine transition only, already-`SeatsReserved` idempotent no-event, `PurchaseCompleted` → `ConflictError`), fixing the unconditional-event bug; short-circuit before `SaveAsync`/lifecycle (atomicity); OpenAPI `200/404/409`. **Behaviour-changing**: seat-not-reservable & terminated `500 → 409`, success body now empty. Non-generic `Result`. Defers `GetMovieSessionSeat`, `Purchase` (0006), ADR-acceptance. |
