# Roadmap — BookingManagement service slices

Global index of slices for the `.NET` BookingManagement service. `NNNN` is a global
zero-padded counter (max existing + 1), not per-aggregate. This file is **owned by
`/to-prd`**; other skills read it but never write to it.

States: Started · Planned · Formalized · Validated · Test-specified · Red gate set ·
Complete. (See `agent_docs/spec_workflow.md`.)

| NNNN | Module / Aggregate | Slice | State | Notes |
|---|---|---|---|---|
| 0001 | platform | error_model_result_infrastructure | Started | ADR-002 step 1: introduce `Result<T>`, fix `DomainErrors<T>` codes, rename `NotFoundError`. Infrastructure only — no use-case, no HTTP, no contract change. |
