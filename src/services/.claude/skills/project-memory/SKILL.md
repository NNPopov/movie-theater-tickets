---
name: project-memory
description: This skill should be used when the user asks about the project's history, asks where a decision is recorded, or wants to know how to record a new one. It points to the roadmap, ADRs, and per-slice spec folders as the project's three forms of memory, and explains who writes to each.
disable-model-invocation: false
---

# project-memory

Reference for the project's persistent memory: roadmap, ADRs, and per-slice
spec folders. These three together are the single source of truth for "what
exists, why, and when."

## When to trigger

- User asks "where do we record this decision?"
- User asks "what was decided about X?"
- User asks "how do I see the list of slices?"
- User mentions the roadmap or an ADR.

## The three layers

### Layer 1: `specs/roadmap.md`

A flat list of every slice in the project, with state.

- **Owned by `/to-prd`.** No other skill writes to it.
- Format: a markdown table, one row per slice, columns `NNNN | aggregate |
  slice | state | created | notes`.
- `NNNN` is global (max+1 across the whole project), zero-padded.
- States match the spec workflow: Started, Planned, Formalized, Validated,
  Test-specified, Red gate set, Complete.

Read it to:

- Find the next `NNNN`.
- Find a reference slice of the same operation shape.
- See what is in flight.

Do not edit it directly. Run `/to-prd` for new slices; later `/to-prd`
invocations update the row state too (see that skill).

### Layer 2: `specs/features/<aggregate>/<NNNN>_<slice>/`

A folder per slice with five markdown files: `prd.md`, `plan.md`,
`requirements.md`, `validation.md`, `tests.md`. Plus, outside `specs/`, the
outside-in test file `<Slice>OutsideInTests.cs` in the integration-test
project — typically at
`tests/...IntegrationTests/Features/<Aggregate>/<NNNN>_<slice>/<Slice>OutsideInTests.cs`.
The test class uses `WebApplicationFactory<Program>` and runs via xUnit.

`<aggregate>` matches the Application feature folder (`ShoppingCarts`,
`MovieSessions`, …). For a brand-new auxiliary entity, use its planned
aggregate/module name and confirm with the user.

Each file is owned by exactly one skill:

| File | Owner |
|---|---|
| `prd.md` | `/to-prd` |
| `plan.md` | `/feature-spec` |
| `requirements.md` | `/feature-requirements` |
| `validation.md` | `/feature-validation` |
| `tests.md` | `/feature-tests` |
| `<Slice>OutsideInTests.cs` | `/slice-test-red` |

Read these to:

- Reconstruct why a slice was built the way it was.
- Find the requirements traceability for a bug or a regression.
- Use a recent slice as a reference for a new similar slice.

### Layer 3: `docs/adr/`

Architecture Decision Records (create on first ADR). One file per
cross-cutting decision that affects multiple slices or stable infrastructure.

ADRs are written when:

- A stable mechanism is changed: `CustomExceptionHandler`, the MediatR
  pipeline behaviours (`ValidationBehaviour<,>`, idempotency behaviour), base
  types (`AggregateRoot`, `Entity`, `Result`, `ValueObject`), the `IEndpoints`
  plumbing, or the `DbContext` itself. See `agent_docs/stable_vs_feature.md`.
- Vertical Slice packaging is adopted for an entity that is currently part of
  the layered Clean Architecture structure.
- A new library is added outside the locked technology stack in `CLAUDE.md`.
- A new cross-cutting `*Exception` / `Error` type is introduced.

Format: short, plain English. Standard ADR template (context, decision,
consequences). Numbered `ADR-NNN-short-name.md`.

ADRs are written by the user, optionally with Claude's help. There is no skill
that owns ADRs because they are rare and irregular.

## How to find something

| Question | Where to look |
|---|---|
| What slices exist in the project? | `specs/roadmap.md` |
| Why was slice X built? | `specs/features/<aggregate>/<NNNN>_<slice>/prd.md` |
| How was slice X implemented? | `specs/features/<aggregate>/<NNNN>_<slice>/plan.md` |
| What does slice X promise to do? | `specs/features/<aggregate>/<NNNN>_<slice>/requirements.md` |
| How do we verify slice X manually? | `specs/features/<aggregate>/<NNNN>_<slice>/validation.md` |
| What is the acceptance test for slice X? | `tests/...IntegrationTests/Features/<Aggregate>/<NNNN>_<slice>/<Slice>OutsideInTests.cs` |
| Where are the feature-folder use-cases? | `BookingManagement/BookingManagementService.Application/<Aggregate>/Command/<UseCase>/` |
| Why was a library or pattern chosen? | `docs/adr/` |
| What counts as stable infrastructure? | `agent_docs/stable_vs_feature.md` |
| What are the universal project rules? | `CLAUDE.md` (root); architecture detail in `agent_docs/architecture.md` |

## Hard rules

- Writing to `specs/roadmap.md` from any skill other than `/to-prd` is forbidden.
- Skipping a spec layer for a slice ("just plan and code, skip requirements")
  is forbidden. Every slice has all five markdown files. Optionality is decided
  per slice in `plan.md`, not by skipping files.
- Recording cross-cutting decisions inside a slice folder is forbidden.
  Cross-cutting decisions go in an ADR; the slice references the ADR.
- Editing past spec files to retroactively change history is forbidden. Past
  spec files are immutable once the slice is Complete. If something needs to
  change, the slice is reopened (new modification flow per `CLAUDE.md`).

## Common mistakes

- Hunting for "the architecture document" in the project root. The architecture
  detail lives in `agent_docs/architecture.md`; the project root has `CLAUDE.md`
  (universal rules).
- Looking for a slice in the source tree to understand "why." The source code
  answers "how," not "why." Read `prd.md` for "why."
- Creating an ADR for a single slice's decision. Slice-local decisions go in
  `plan.md`. ADRs are for decisions that affect multiple slices or change a
  stable mechanism.
- Looking for `# STABLE:` / `# FEATURE:` file-header comments. This project
  does not use them. "Stable" vs "feature" is determined by where a file lives
  and what it does — see `agent_docs/stable_vs_feature.md`.
