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
