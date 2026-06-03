---
name: spec-workflow
description: This skill should be used when the user references slice spec work, mentions a slice folder (specs/features/...), asks "what's next" on a slice, or invokes the spec chain. It auto-loads the architectural context, inspects the current slice folder, determines which stage the slice is in, and advises which command to run next. This skill advises only; it does not write spec files itself.
disable-model-invocation: false
---

# spec-workflow

Dispatcher for the slice spec chain. Reads the architectural context, inspects
the slice folder state, and tells the user which command to run next. Does not
produce spec files or test code.

## When to trigger

- User mentions a slice by name, by folder path, or by aggregate/operation pair.
- User asks "what's next" on a slice.
- User invokes the spec chain without a specific command (e.g. "start working
  on SelectSeat").
- User references `specs/features/...`.
- User talks about implementing a slice and the slice has no `tests.md` yet.

Do not trigger on pure implementation questions ("how do I write the
repository?"). Those go to `agent_docs/architecture.md` and
`agent_docs/error_handling.md` directly.

## Process

### 1. Load context (mandatory)

Read these files before doing anything else:

- `CLAUDE.md`
- `agent_docs/architecture.md`
- `agent_docs/error_handling.md`
- `agent_docs/spec_workflow.md`
- `agent_docs/stable_vs_feature.md`

These are always read together. If any is missing, stop and ask the user.

### 2. Find the target slice

Determine the slice in this order:

1. If the user named a slice path (`specs/features/ShoppingCarts/0003_select_seat/`),
   use it.
2. If the user named an aggregate/operation pair (e.g. "select_seat under
   ShoppingCarts"), search `specs/features/*/` for a matching folder and use it.
3. If the user just said "next" or "current", use the slice whose folder has
   the most recently modified file under `specs/features/*/*/`.
4. If ambiguous, list the candidates and ask.

If no matching slice folder exists, treat the request as "new slice" — the
next step is `/to-prd`.

### 3. Inspect state

Determine which of these states the slice is in by checking which files exist
inside the slice folder and whether the outside-in C# test file is present:

| State | Files present (in slice folder) | Outside-in test file |
|---|---|---|
| Empty | none | absent |
| Started | `prd.md` only | absent |
| Planned | `prd.md`, `plan.md` | absent |
| Formalized | + `requirements.md` | absent |
| Validated | + `validation.md` | absent |
| Test-specified | + `tests.md` (all five .md) | absent |
| Red gate set | all five .md | present (assumed RED) |
| Complete | all five .md | present (assumed GREEN) |

The outside-in test file is a C# file at
`tests/...IntegrationTests/Features/<Aggregate>/<NNNN>_<slice>/<Slice>OutsideInTests.cs`.

Conditionally read `agent_docs/testing.md` if the state is Validated or beyond.
Conditionally read `agent_docs/entry_points/minimal-api.md` if the slice has
an HTTP entry point (almost always true).

### 4. Advise

Reply with three things:

1. **Current state** of the slice, named (e.g. "Planned").
2. **Recommended next command**, in backticks.
3. **One sentence** of context for the user — what the next command will produce
   and roughly what it covers.

Example replies:

> The slice `specs/features/ShoppingCarts/0003_select_seat/` is in state
> **Planned**. Next step: `/feature-requirements`. This will produce
> `requirements.md` with functional and non-functional requirement IDs that
> later phases trace back to.

> The slice `specs/features/MovieSessions/0001_create_session/` is in state
> **Test-specified**. Next step: `/slice-test-red`. This will translate
> `tests.md` into a runnable C# `CreateSessionOutsideInTests` class, run it
> via `dotnet test`, and confirm it is RED.

For state **Red gate set**, the next step is not a command but implementation:

> The slice is in state **Red gate set**. The outside-in test at
> `tests/...IntegrationTests/Features/ShoppingCarts/0003_select_seat/SelectSeatOutsideInTests.cs`
> is RED. Next step: implement the slice (domain → application → infrastructure
> → endpoint) until that test turns GREEN. Run
> `dotnet test --filter "FullyQualifiedName~SelectSeatOutsideInTests"` from
> `src/services` to check progress.

For state **Complete**, advise on the follow-up unit tests per
`agent_docs/testing.md` (handler unit test, repository/adapter unit test,
endpoint integration test), and confirm that
`dotnet test CinemaBookingManagement.sln` (including the architecture tests in
`BookingManagementService.Domain.ArchitectureTests`) passes in full.

### 5. Stop

Do not execute the recommended command. The user issues it. This skill
advises only.

## Hard limits

- No writing or modifying any file under `specs/`.
- No writing or modifying any file under `BookingManagement/` or `tests/`.
- No running shell commands (`dotnet test`, `dotnet build`, `dotnet ef`, etc.).
- No invoking other spec-stage skills (`/to-prd`, `/feature-spec`, etc.) on
  the user's behalf.

This skill is read-only and advisory.

## What this skill is NOT

- Not a producer. It does not create spec files or C# test classes.
- Not a tester. It does not run tests.
- Not a planner. It picks the next command from a fixed table based on the
  slice's state.

## Common mistakes

- Treating a slice with `prd.md` only as "Complete" because the folder exists.
  State depends on the **files inside** the folder, not on the folder itself.
- Recommending `/to-prd` for an existing slice. `/to-prd` is for **new**
  slices and creates the folder.
- Failing to mention the `<Slice>OutsideInTests.cs` file path when advising
  about a slice in state Red gate set or Complete.
- Skipping the architectural-context read in step 1. Without it, the advice
  may contradict project rules.
- Advising the wrong entry-point doc — this project uses Minimal APIs
  (`agent_docs/entry_points/minimal-api.md`), not controllers.
- Misremembering the `ContentNotFoundException` → 404 mapping. Always consult
  `agent_docs/error_handling.md` for the exception→status table before giving
  error-path advice.
