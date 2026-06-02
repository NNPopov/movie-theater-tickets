---
name: feature-tests
description: This skill should be used when the user wants to generate tests.md for a slice in this Flutter project. Trigger when the user invokes /feature-tests, says "write outside-in test spec", "generate tests.md", or asks to describe the slice-level integration test in markdown form before any code is written. Use this skill only after prd.md, plan.md, requirements.md, and validation.md all exist for the target slice — it reads all four as input. This skill writes a markdown specification only; it does NOT write Dart code.
disable-model-invocation: false
---

# feature-tests

Generate `tests.md` for a slice. The output is a **markdown specification** of one
outside-in integration test that will exercise the slice end-to-end through its
public surface (typically the Cubit), with mocks only at system boundaries
(typically Dio).

The Dart implementation of this test is produced separately by `/slice-test-red`.
This skill writes prose, not code.

## Process

### 1. Find the target slice

The slice is the one currently being worked on. Determine it in this order:

1. If the user explicitly named a slice ("tests.md for delete_post"), use that.
2. Else look for the most recently modified `validation.md` under
   `specs/features/*/*/`. That folder is the target slice.
3. If multiple recent candidates exist, ask the user which one.

The slice folder path is `specs/features/<feature>/<NNNN>_<slice>/`. The output
goes to `<that folder>/tests.md`.

### 2. Read the inputs

Read **all four** existing spec files before writing:

- `prd.md` — overall behavior and user-visible effects.
- `plan.md` — public surface of the slice (Cubit methods, port signatures, event
  publications). Determine the slice's "outside" from here.
- `requirements.md` — functional requirements that the outside-in test must cover.
- `validation.md` — manual scenarios. The outside-in test typically covers the
  happy-path manual scenario plus the most important failure path.

If any of the four is missing, stop and ask the user to create it first.

Also load architectural context per `agent_docs/spec_workflow.md`:
`CLAUDE.md`, `agent_docs/architecture.md`, `agent_docs/error_handling.md`,
`agent_docs/testing.md`. The boundary decisions (what to mock, what to wire real)
depend on the project's testing conventions, which live there.

### 3. Decide the test boundary

The outside-in test wires **real** code for the slice's domain, data, and
application layers, and mocks only what crosses a system boundary.

For a typical CRUD slice in this project:

- **Mock:** `Dio` (the HTTP client). Optionally `AuthCubit` if the slice's logic
  depends on the current user. Optionally `PostEventBus` (or similar) if it is the
  way the slice communicates with the rest of the app.
- **Wire real:** the slice's `Adapter`, `Port`, `UseCase`, `Cubit`, and any
  domain entities. They all participate in the test as production code.

Widget tests are **not** part of the outside-in scope at this level — those are
covered separately, after the implementation lands, as per `agent_docs/testing.md`.
The outside-in test is at the Cubit-to-network level.

### 4. Write the file

Use this exact structure. Keep section names unchanged. Keep prose tight — this
is a contract, not an essay.

```markdown
# NNNN · slice_name — Outside-in test spec

## Goal

One sentence: what slice-level behavior does this test prove?

## Entry point

The Cubit method (or other public-surface call) the test invokes.

Example: `cubit.confirmAndDelete(username: 'alice', id: 42)`

## Wired real (production code in the test)

- <module-name> (the slice's Adapter)
- <module-name> (the slice's Port — bound to the adapter via DI)
- <module-name> (the slice's UseCase)
- <module-name> (the slice's Cubit, the system under test)

## Mocked (system boundaries only)

- **Dio**: returns `<status code>` for `<HTTP method> <path pattern>`.
- **AuthCubit** (if applicable): `currentUser` returns `<user fixture>`.
- **<other boundary>**: <what the mock returns>.

## Test scenarios

### Scenario 1: <name of the happy-path scenario>

**Setup:**
- <mock configuration>

**Act:**
- `<the entry-point call>`

**Expect:**
- States emitted by the Cubit: `[<list of expected states in order>]`
- Side effects observed: `<e.g. PostEventBus received PostDeleted(42)>`
- Mocks verified: `<e.g. Dio.delete called once with /api/v1/alice/post/42>`

### Scenario 2: <name of the most important failure-path scenario>

**Setup:**
- <mock configuration that triggers the failure>

**Act:**
- `<the entry-point call>`

**Expect:**
- States emitted by the Cubit: `[<list>]`
- Side effects observed: `<e.g. PostEventBus received nothing>`
- Mocks verified: `<e.g. logger.error called once>`

## Out of scope for this test

- Widget rendering (covered by widget tests separately).
- Route navigation (covered by widget tests separately).
- Manual UX scenarios from validation.md that do not change observable state
  through this Cubit.
```

Replace `NNNN`, `slice_name`, and all bracketed placeholders with concrete content.

### 5. Save and confirm

Save to `specs/features/<feature>/<NNNN>_<slice>/tests.md`. Tell the user the file
was created, list the scenarios it contains, and suggest the next step:

> Next step: `/slice-test-red` to generate the failing Dart test from this spec.

## Style rules

- **Always English**, regardless of conversation language.
- **Two scenarios is the default**: one happy path, one most-important failure
  path. Add a third only if a critical edge case cannot be covered by widget tests
  or unit tests later (rare).
- **Concrete fixtures**, not placeholders: write `username: 'alice'` and `id: 42`,
  not `username: <some username>` and `id: <some id>`.
- **State sequences are exact**: list every expected state in order, not "starts
  with deleting and ends with success".
- This file describes **observable behavior**, not internal calls between use-case
  and port. Those are not visible from outside the Cubit.

## What this file is NOT

- Not a Dart file. It contains no `dart` code blocks, no `test('...')`, no
  `expect(...)`. Those belong in the Dart file produced by `/slice-test-red`.
- Not a replacement for `validation.md`. validation.md is for manual scenarios
  and code review checklist. tests.md is for one executable outside-in scenario.
- Not a list of unit tests. Unit tests (use-case, adapter, cubit, widget) are
  decided per `agent_docs/testing.md` and written after the outside-in test
  passes green.

## Common mistakes

- ❌ Writing Dart code inside tests.md. The Dart code goes in `*_outside_in_test.dart`,
  produced by `/slice-test-red`. tests.md is a markdown contract.
- ❌ Listing every possible failure scenario. Pick the most important one. The rest
  are covered by adapter unit tests, which are written later from `plan.md`.
- ❌ Including widget assertions ("button is visible") in the outside-in test.
  Widgets are tested separately. The outside-in test stops at the Cubit's
  observable surface.
- ❌ Mocking the slice's own Port, UseCase, or Cubit. Those are wired real —
  that's the whole point of "outside-in". If you find yourself mocking them, this
  is no longer an outside-in test.
- ❌ Saving to a path other than `specs/features/<feature>/<NNNN>_<slice>/tests.md`.
