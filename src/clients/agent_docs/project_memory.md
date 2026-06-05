# Project memory reference

Read this at the start of any work that involves a new feature, a new slice, or a
non-trivial change to an existing one. This document explains where context lives and
how to keep it updated as the project grows.

The paired `project-memory` skill auto-triggers on phrases like "new feature", "new
slice", "start a feature", or "spec for X" — it walks you through the procedure that
this reference describes statically.

## The three memory layers of the project

The project has three places where decisions are recorded, each with a different
purpose. Mixing them up leads to context that is hard to find later.

The **`specs/` tree** describes *what is being built and why*. It contains the roadmap
of all slices and the per-slice product/technical specifications. This is forward-
looking material: a slice gets a spec **before** implementation starts, and the spec
is the contract that implementation follows.

The **`.claude/decisions/` tree** describes *why a non-trivial decision was made*. It
contains Architecture Decision Records (ADRs) and resolved-issue notes. This is
backward-looking material: an ADR is written **after** a decision is made and needs to
outlive the conversation in which it was made.

The **`CHANGELOG.md`** describes *what shipped and when*. It is a flat user-facing
record, not a place for design discussion.

If you remember nothing else from this document, remember this distinction. `specs/`
is about intent, `.claude/decisions/` is about reasoning, `CHANGELOG.md` is about
delivery.

## Where to look before starting work

| What you need to understand | Where to look |
|---|---|
| Architecture and universally applicable rules | `CLAUDE.md`, then the relevant `agent_docs/*.md` |
| Order and status of all slices | `specs/roadmap.md` |
| What was implemented and how it was originally planned | `specs/features/<feature>/<NNNN>_<slice>/` |
| Why a non-trivial decision was made | `.claude/decisions/` |
| What changed and when | `CHANGELOG.md` |

## `specs/` directory layout

```
specs/
├── roadmap.md
└── features/
    └── <feature_name>/
        └── <NNNN>_<slice_name>/
            ├── prd.md            # product requirements
            ├── plan.md           # implementation plan
            ├── requirements.md   # functional requirements
            └── validation.md     # how we know it works
```

The four-digit number `NNNN` is **globally unique across the entire project**, not
per-feature. This is intentional: it gives every slice a stable, searchable identifier
regardless of how features get reorganized later. `roadmap.md` is the authoritative
list.

## Procedure: starting a new slice

Read these steps as a checklist. Each step depends on the previous one.

1. **Open `specs/roadmap.md`** and pick the next available number. Do not skip
   numbers, do not reuse retired ones. Reusing a retired number breaks the
   one-number-one-spec contract that lets you find a slice by ID.
2. **Create the folder** `specs/features/<feature>/<NNNN>_<slice_name>/` with the four
   files listed above (empty stubs are fine at this stage).
3. **Add a row to `roadmap.md`** with a link to the new folder and status `📋`
   (planned). The roadmap is the single index — if it is out of date, the project
   memory is broken.
4. **Run a discovery conversation** using the `grill-me` skill. The point of grill-me
   is to challenge assumptions before any code is written; treat the resulting
   conversation as raw input to the spec.
5. **Generate `prd.md`** from the grill-me conversation using the `to-prd` skill.
6. **Generate `plan.md`, `requirements.md`, and `validation.md`** from the PRD using
   the `feature-spec` skill.
7. **Implement** the slice, following `agent_docs/architecture.md` for layout and the
   `bloc-state-management` skill for application-layer patterns.
8. **Update the row in `roadmap.md`** to status `✅` (done) when implementation,
   tests, and the validation criteria all pass.

## Procedure: finishing a slice

Two records may need to be created at the end of a slice. Skip neither.

If a **non-trivial problem** was solved during implementation — something a future
developer (or future Claude session) is likely to hit again — write a short note in
`.claude/decisions/issues/resolved/<NNNN>_<short_title>.md`. The format is loose: the
problem in one paragraph, the resolution in another, a code reference if useful. The
goal is searchability, not completeness.

If an **architectural decision** was made — something that changes how future slices
will be built, not just this one — write an ADR in `.claude/decisions/<NNNN>_<title>.md`.
ADRs follow the standard four-section shape: Context, Decision, Consequences,
Status. Keep them short; an ADR longer than a page usually needs splitting.

If the ADR establishes a rule that should apply **everywhere** in the project — not
just inside one feature — also update the relevant document under `agent_docs/` (or
`CLAUDE.md` if it is truly universal) and reference the ADR from there. The ADR is the
historical record; the agent docs are the operational guide.

## Common mistakes

- ❌ Starting implementation without writing the spec first. The spec is cheap when
  written before code and expensive when reverse-engineered after.
- ❌ Numbering a slice with a per-feature counter (e.g. `users/0001_list`,
  `orders/0001_list`). Numbers are global. Open `roadmap.md` to find the next one.
- ❌ Updating `prd.md` after implementation to match what was built. The PRD is a
  contract from a point in time; if implementation diverged, write an ADR explaining
  why.
- ❌ Putting an architectural rule into a single feature's spec instead of the agent
  docs. If it applies everywhere, it must live somewhere that future features will
  naturally read.
- ❌ Treating `.claude/decisions/issues/resolved/` as a debug log. It is for problems
  that will repeat, not for one-off bugs. Most bugs do not need a record there.
- ❌ Writing an ADR longer than two pages. If it does not fit, the decision is too big
  and should be split.
