---
name: slice-test-red
description: This skill should be used when the user wants to generate the executable Dart outside-in test for a slice from its tests.md specification. Trigger when the user invokes /slice-test-red, says "generate the red test", "write the outside-in Dart test", "make the failing test", or asks to translate tests.md into runnable Dart code. Use this skill only after tests.md exists for the target slice — it reads tests.md plus architectural context as input, produces a Dart test file, and runs `flutter test` to verify the test is RED (fails as expected because the implementation is not yet written).
disable-model-invocation: false
---

# slice-test-red

Translate a slice's `tests.md` markdown specification into an executable Dart
outside-in test file, place it under `test/features/<f>/<NNNN>_<slice>/`, run
`flutter test` against it, and verify that it is **red** — failing as expected
because the slice's implementation does not yet exist.

This is the "red" phase of outside-in TDD. The next step after this skill is
implementation, which proceeds until the same test turns green.

## Process

### 1. Find the target slice

Same determination as in `/feature-tests`:

1. If the user explicitly named a slice, use that.
2. Else look for the most recently modified `tests.md` under `specs/features/*/*/`.
3. If ambiguous, ask.

### 2. Read the inputs

- `specs/features/<feature>/<NNNN>_<slice>/tests.md` — the test specification.
  This is the **primary** input. The Dart file produced must implement what is
  written here, scenario by scenario.
- `specs/features/<feature>/<NNNN>_<slice>/plan.md` — for the actual class names,
  method signatures, and DI bindings the test must reference.
- `agent_docs/testing.md` — for mocktail conventions, fallback values, and the
  reference adapter/widget tests.
- The reference adapter test pointed to from `agent_docs/testing.md` (currently
  `test/features/posts/erase_db_post/data/erase_db_post_adapter_test.dart`) —
  for the canonical pattern of mocking Dio and verifying logger calls.

If `tests.md` is missing, stop and tell the user to run `/feature-tests` first.

### 3. Decide the test file path

The file goes to:
`test/features/<feature>/<NNNN>_<slice>/<slice>_outside_in_test.dart`.

Filename suffix is always `_outside_in_test.dart` to distinguish it from later
unit tests on the same slice (`<slice>_cubit_test.dart`, etc.).

### 4. Generate the Dart code

Structure of the produced file:

1. Imports (mocktail, flutter_test, bloc_test, dartz, the slice's Cubit/state,
   the slice's Adapter (real), Port (real), UseCase (real), Dio for mocking, and
   any other system-boundary classes from the "Mocked" section of tests.md).
2. Mock class declarations: `class _MockDio extends Mock implements Dio {}`, etc.
3. `setUpAll` registering fallback values for any custom types used in `when`/`verify`.
4. `setUp` constructing the **real** Adapter, Port, UseCase, Cubit using the mocks.
   Wire dependencies by hand — do NOT use `getIt` here, the test must be
   self-contained.
5. One `test(...)` block per scenario from tests.md, in the same order.

For each scenario, follow the markdown structure literally:

- **Setup** in tests.md → `when(...).thenAnswer(...)` / `.thenReturn(...)` /
  `.thenThrow(...)` calls.
- **Act** in tests.md → the actual call, awaited.
- **Expect** in tests.md → `expect`/`expectLater` for state sequence,
  `verify(...)` for boundary calls and side effects.

Use `expectLater`/`emitsInOrder` to capture state sequences, **not**
`bloc_test` and **not** `stream.listen` + `await cancel()`. `bloc_test` is for
unit-testing cubits in isolation. `stream.listen` + `cancel` has a race condition:
`BlocBase._stateController` is an async broadcast stream (`sync: false`), so each
`emit()` schedules delivery via `scheduleMicrotask` — the method returns before the
microtask fires, and `cancel()` removes the listener before the event is delivered.
`expectLater` avoids this because it awaits the stream events directly:

```dart
// ❌ race condition — last state silently dropped
final emitted = <MyState>[];
final sub = cubit.stream.listen(emitted.add);
await cubit.load();
await sub.cancel();
expect(emitted, [...]); // may fail with Actual: []

// ✅ correct pattern — set up expectation BEFORE the action
final expectation = expectLater(
  cubit.stream,
  emitsInOrder([
    const MyState.loading(),
    isA<MyStateLoaded>(),
  ]),
);
await cubit.load();
await expectation;
// field-level assertions go on cubit.state
final loaded = cubit.state as MyStateLoaded;
expect(loaded.items.length, 1);
```

### 5. Verify the test is RED

This is the critical step that distinguishes this skill from "just write code".

After writing the Dart file:

1. Run `dart run slang` if any localization keys are referenced by the test
   (rare, but possible). Then `dart run build_runner build --delete-conflicting-outputs`
   if any new types in the test require codegen.
2. Run `flutter test <path-to-the-outside-in-test-file>` and capture the output.
3. Verify that the test **fails**. There are two acceptable kinds of failure:
   - **Compilation failure** with a clear message that the slice's Cubit/Adapter
     class does not exist yet (because we haven't written it). This is the expected
     red state at the very start of TDD on a new slice.
   - **Assertion failure** with a clear message that the expected state sequence
     was not emitted (because we have skeletons but no real logic yet). This is
     the expected red state when the slice has stubs.

If the test **passes**, something is wrong: either the slice is already
implemented (and this is not a fresh red phase), or the test is not actually
checking what it should. Stop and tell the user — do not declare the red phase
complete on a passing test.

If the test fails with a **runtime exception other than the above** — e.g. a
missing mock setup, a type mismatch, a registration error — that is **not** an
acceptable red. Fix the test, do not ship it. The red must be from missing
**implementation**, not from a broken test.

### 6. Report

Output to the user, in this exact order:

1. **Path of the new Dart test file.**
2. **Each scenario from tests.md**, mapped to its `test('...')` block.
3. **Exact failure mode** (compilation error / assertion failure / etc.)
   with a one-line excerpt from the flutter test output.
4. **Confirmation** that the test is in the expected red state.
5. **The implementation prompt block** described below.

### The implementation prompt block

The next session — implementation — is a separate conversation. To save the
user from re-typing the same prompt every slice, output a ready-to-copy
block at the end of the report. The user copies the text between the
separator lines, clears context, opens a new chat, and pastes.

Output the block **verbatim** in this shape, replacing `<feature>`,
`<NNNN>`, `<slice>` with concrete values for the slice you just produced
the test for:

```
─────────────────────────────────────────────────────────────────
COPY THIS PROMPT FOR THE IMPLEMENTATION SESSION
─────────────────────────────────────────────────────────────────

Implement the <slice> slice. All specs and the test are already ready.

Sources (read in this order):
- specs/features/<feature>/<NNNN>_<slice>/plan.md
- specs/features/<feature>/<NNNN>_<slice>/requirements.md
- specs/features/<feature>/<NNNN>_<slice>/tests.md
- specs/features/<feature>/<NNNN>_<slice>/validation.md

Acceptance gate:
- test/features/<feature>/<NNNN>_<slice>/<slice>_outside_in_test.dart
  must turn GREEN.
- Do NOT touch the test file. If the test fails due to a bug in the
  implementation — fix the implementation, not the test.
- If the test fails due to a defect in the test itself — stop and ask,
  do not fix silently.

Once the outside-in test is green — write the missing unit tests per the
plan (see plan.md section "Tests planned" and agent_docs/testing.md):
use-case, adapter, cubit, widget.

Before running tests with new i18n keys or JSON:
- dart run slang
- dart run build_runner build --delete-conflicting-outputs

Quality gates before finishing:
- dart analyze
- flutter test
- bash scripts/check_arch.sh

All must pass with no new warnings.

─────────────────────────────────────────────────────────────────
```

Rules for the block:

- **Exact paths**, not placeholders. By the time you produce this block
  you already know `<feature>`, `<NNNN>`, `<slice>` — substitute them.
- **No Markdown formatting** inside the block (no headers, no bold). The
  user pastes it raw into the next chat; Markdown would render
  inconsistently and might be misinterpreted.
- **One copy of the block per run.** Do not output it twice. Do not
  output it partially.
- **English only** in the block.
- The block goes **last** in your reply. Nothing after it.

If for any reason the test is **not** in a verified red state (passing, or
failing on a broken-test runtime error), do **not** output the
implementation prompt block. The block presupposes a valid red state;
emitting it on a broken test would mislead the user into starting
implementation against a test that does not actually represent the
contract.

## Style rules

- **English only** in test names and comments.
- **One `test(...)` per scenario in tests.md**, named with a short string derived
  from the scenario heading.
- **Imports sorted** in standard Dart convention (dart:, package:, then relative).
- **No `getIt`** in the test. The test wires everything manually, by hand,
  through constructors. This makes the test self-contained and the dependency
  graph visible.
- **No `bloc_test`** in the outside-in test. `bloc_test` is reserved for the later
  unit-test phase.
- **No `stream.listen` + `await cancel()`** to collect states. Use
  `expectLater`/`emitsInOrder` set up before the action — see step 4 for the full
  pattern and the reason (`sync: false` async delivery race).
- **No try-catch around the act phase** unless the spec explicitly requires
  asserting an exception type. Failures should propagate to the test runner.

## What this skill is NOT

- Not a code generator that produces a "green" implementation. The output is
  intentionally a failing test. The implementation is the user's next step.
- Not a unit-test generator. Unit tests for use-case, adapter, cubit, widget are
  written **after** the outside-in test passes green, per `agent_docs/testing.md`.
- Not for modifying `tests.md`. If the spec is wrong, fix it in `tests.md` first,
  then re-run this skill.

## Common mistakes

- ❌ Generating a passing test. The whole point is **red**. If the test passes,
  the skill has either generated a stub assertion or there is real implementation
  already — stop and check.
- ❌ Using `bloc_test` in the outside-in test. Outside-in tests need the raw
  stream because they exercise the full chain, not the cubit in isolation.
- ❌ Using `stream.listen(emitted.add)` + `await cancel()` to collect states.
  Due to `BlocBase`'s async broadcast stream, `cancel()` races with the delivery
  microtask and silently drops the last emitted state. Use `expectLater`/`emitsInOrder`
  instead (see step 4).
- ❌ Mocking the slice's own Adapter, Port, UseCase, or Cubit. Those are wired
  real. Mocking them defeats the purpose of "outside-in".
- ❌ Using `getIt` to construct the cubit. Wire everything manually in `setUp`
  so the dependency graph is visible inside the test.
- ❌ Suppressing a compilation error to make the test "work". If the slice's
  Cubit class does not exist, the compilation error **is** the red signal —
  accept it and report it as the expected failure.
- ❌ Skipping the `flutter test` run. The skill is not complete until the red
  state has been observed and reported. A Dart file that has not been executed
  is not a verified red.
- ❌ Saving the test outside the slice-specific folder
  `test/features/<feature>/<NNNN>_<slice>/`.
