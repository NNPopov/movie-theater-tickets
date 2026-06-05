---
name: run-spec-workflow
description: Run full feature-spec→feature-requirements→feature-validation→feature-tests→slice-test-red pipeline for a feature folder
---

# run-spec-workflow

Thin orchestrator. Runs the spec chain from `/feature-spec` through to
`/slice-test-red` in strict order for a given slice, producing all five
markdown spec files and the red C# outside-in test class.

The `/to-prd` step is **not** run automatically — a `prd.md` must already
exist in the slice folder before this pipeline starts. If it does not, stop
and tell the user to run `/to-prd` first.

## When to trigger

- User invokes `/run-spec-workflow` with a slice name or folder argument.
- User says "run the full spec pipeline for `<slice>`".

Do not trigger on requests to run a single step. Those are invoked directly
(`/feature-spec`, `/slice-test-red`, etc.).

## Pre-flight check

Before starting:

1. Verify that `specs/features/<aggregate>/<NNNN>_<slice>/prd.md` exists.
   If absent, stop: "prd.md is missing — run /to-prd first, then re-run
   /run-spec-workflow."
2. Read `CLAUDE.md` and `agent_docs/spec_workflow.md` to confirm the slice
   path and aggregate name.

## Pipeline (run in this order, one step at a time)

| Step | Skill | Produces |
|---|---|---|
| 1 | `/feature-spec` | `plan.md` |
| 2 | `/feature-requirements` | `requirements.md` |
| 3 | `/feature-validation` | `validation.md` |
| 4 | `/feature-tests` | `tests.md` |
| 5 | `/slice-test-red` | `<Slice>OutsideInTests.cs` (verified RED) |

Each step **must complete successfully** before the next begins. If a step
fails or reports a problem, halt the pipeline and surface the issue to the
user — do not skip ahead.

## What the pipeline produces

After a successful run, the slice folder contains:

```
specs/features/<aggregate>/<NNNN>_<slice>/
├── prd.md            (pre-existing)
├── plan.md           (step 1)
├── requirements.md   (step 2)
├── validation.md     (step 3)
├── tests.md          (step 4)
└── implement.md      (written by step 5)

tests/...IntegrationTests/Features/<Aggregate>/<NNNN>_<slice>/
└── <Slice>OutsideInTests.cs   (step 5 — verified RED)
```

The final artifact is the C# `<Slice>OutsideInTests.cs` class, which is the
RED acceptance gate. Implementation begins after this pipeline completes.

## After the pipeline

Report to the user:

1. All five markdown files are present in `specs/features/<aggregate>/<NNNN>_<slice>/`.
2. `<Slice>OutsideInTests.cs` is written and verified RED (include the failure
   mode: build error / 404 / assertion).
3. `implement.md` has been written to the slice folder.
4. The next step is implementation — paste the contents of `implement.md` into
   a new session to start.

## Hard limits

- No skipping steps. The order is fixed: each file depends on the ones before it.
- No running the pipeline if `prd.md` is absent.
- No running `dotnet build` or `dotnet test` directly (that is delegated to
  `/slice-test-red` in step 5).
- No modifying `specs/roadmap.md` (owned by `/to-prd`).
