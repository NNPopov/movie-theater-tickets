# Project: Flutter Cross-Platform App (iOS-first)

This file is **the single source of truth** for the AI assistant. It contains only
universally applicable rules. Topic-specific rules live in `agent_docs/` and
`.claude/skills/` and must be read on demand — not preloaded.

## Project at a glance

- **Platform priority:** iOS/macOS → Android → Windows → Linux.
- **Backend:** Python and .NET. REST + gRPC. DTOs shared via OpenAPI/protobuf.
- **Architecture:** Vertical Slice on top of Hexagonal. Each feature is split into
  slices; inside each slice — ports (narrow interfaces in `domain/ports/`) and adapters
  (implementations in `data/`). The word "Repository" is not used.
- **Goal:** reusable template for new applications. Minimum boilerplate in features,
  maximum in `core/`.

## Migration status (read this before touching any existing code)

This is a **legacy project being migrated** to the architecture and stack described
in this file. The rules below describe the **target**, not the current reality. The
skills in `.claude/skills/` were brought over from a project that already follows the
target fully; here they are the north star, not a description of what exists.

The existing code under `lib/src/` **predates these rules and does not follow them yet**:

- Layout is layered feature-first (`lib/src/<feature>/{data,domain,presentation}`),
  not vertical slices with `domain/ports/` + `_shared/`.
- It uses `intl` + ARB/gen-l10n (not `slang`), plain `Navigator` (not `auto_route`),
  `dio` with hand-written repos (not `retrofit`), `flutter_bloc` 8.x, `get_it` without
  `injectable`, `flutter_lints` (not `very_good_analysis`), and classes named `*Repo`.
- The official `flutter-*` reference skills suggest `provider`/`go_router`/`intl`; those
  are intentionally **not** the locked stack — see "Conflict resolution".

**How to act:**

- **Modifying existing legacy code** (a bug fix or small change under `lib/src/`):
  match the surrounding legacy style. Make the **minimal** change. Do **not** silently
  re-architect, rename `*Repo` → port/adapter, swap `intl` → `slang`, or reformat to
  the target rules. A migration is a deliberate, tracked step — never a side effect of
  an unrelated task.
- **New features, or code already migrated to the target:** follow this file and the
  skills **fully**.
- **When unsure** whether a file is already migrated: ask before applying target rules.

This section is temporary scaffolding for the migration and should be removed once the
codebase fully follows the target.

## Locked technology stack

| Area | Choice |
|---|---|
| State | `flutter_bloc` 9.x — Cubit by default, Bloc when event traceability is needed |
| Navigation | `auto_route` 9.x |
| Localization | `slang` + `slang_flutter` |
| DI | `get_it` + `injectable` |
| HTTP | `dio` + `retrofit` |
| gRPC | `grpc` + `protobuf` |
| Serialization | `freezed` + `json_serializable` |
| Storage | `flutter_secure_storage` (secrets) + `hive`/`isar` (cache) |
| Tests | `bloc_test` + `mocktail` + `flutter_test` |
| Lints | `very_good_analysis` |

**Forbidden without explicit user approval:** `Provider`, `Riverpod`, `GetX`, `MobX`,
`go_router`, `mockito`, `json_annotation` without `freezed`, any global singleton
outside `get_it`, any class named `*Repository`.

## Universal hard rules (apply to every task)

- ❌ Logic in widget `build()`.
- ❌ Calling Dio directly from presentation.
- ❌ Hardcoded UI strings — always via `slang`.
- ❌ A feature importing another feature.
- ❌ A slice importing another slice of the same feature (only `_shared/` is allowed).
- ❌ `domain/` importing anything from `package:flutter/*`, `package:dio/*`, or any
  package outside `dartz`/`freezed`/pure Dart.
- ❌ `core/` knowing about `features/`.
- ❌ `setState` in a widget that has a Cubit.
- ❌ Permission check only in UI without duplication in the use-case.
- ❌ Adapter without a catch-all `catch (e, st)` with logging.
- ❌ Adding a new dependency to `pubspec.yaml` without asking the user first.

## Where to look when working on a task

Read **only** the documents that match the current task. Do not preload everything.

| If the task involves… | Read first |
|---|---|
| Creating a new slice / feature, deciding folder layout, modifying layer boundaries | `agent_docs/architecture.md` |
| Writing or modifying an adapter, DTO, or anything that talks to the network | `agent_docs/error_handling.md` |
| Permissions, roles, route guards, or hiding UI by role | `agent_docs/rbac.md` |
| Any UI string or locale work | `agent_docs/localization.md` |
| Routing, deep links, navigation guards | `agent_docs/navigation.md` |
| Writing or modifying tests | `agent_docs/testing.md` |
| Understanding what's done, what's planned, where to write specs and ADRs | `agent_docs/project_memory.md` |
| Working on a slice spec (PRD, plan, requirements, validation) | `agent_docs/spec_workflow.md` |

The following skills auto-trigger when relevant — do not request them manually:

- `bloc-state-management` — Cubit/Bloc, sealed states, use-case orchestration.
- `slice-decomposition` — deciding whether something is a new slice or extends an existing one.
- `project-memory` — wiring up specs, roadmap, and ADRs when starting/finishing a slice.
- `spec-workflow` — entry point for any work on a slice spec; loads architectural context, inspects the slice folder, advises which `/feature-*` command to run next.

The following skills are invoked **explicitly** by the user via slash commands —
do not trigger them automatically:

- `/to-prd` — generate prd.md (entry point: also creates the slice folder and adds the roadmap row).
- `/feature-spec` — generate plan.md.
- `/feature-requirements` — generate requirements.md.
- `/feature-validation` — generate validation.md.
- `/feature-tests` — generate tests.md (outside-in test specification, markdown).
- `/slice-test-red` — generate `<slice>_outside_in_test.dart` from tests.md and verify it is RED.

See "Slice spec workflow" below for how the six chain together.

## Slice spec workflow

**Entry point:** the `spec-workflow` skill. When the user mentions spec work, that
skill auto-triggers, loads architectural context, inspects the slice folder, and
advises which command to run next. The user then runs the suggested command.

A new slice gets a complete spec folder with **five files** at
`specs/features/<feature>/<NNNN>_<slice>/`:

| File | Generated by | Sources |
|---|---|---|
| `prd.md` | `/to-prd` | the conversation; user stories and product decisions |
| `plan.md` | `/feature-spec` | prd.md + reading existing slices for context |
| `requirements.md` | `/feature-requirements` | prd.md + plan.md |
| `validation.md` | `/feature-validation` | prd.md + plan.md + requirements.md |
| `tests.md` | `/feature-tests` | prd.md + plan.md + requirements.md + validation.md |

Plus one file outside `specs/`, in the test tree:

| File | Generated by | Sources |
|---|---|---|
| `<slice>_outside_in_test.dart` (RED) | `/slice-test-red` | tests.md + plan.md + agent_docs/testing.md |

**Run the commands in this order**, one at a time. Each command produces its
file and returns. Do not skip steps — every later file depends on the earlier
ones being accurate.

```
/grill-me              (optional discovery interview)
/to-prd                → prd.md
/feature-spec          → plan.md
/feature-requirements  → requirements.md
/feature-validation    → validation.md
/feature-tests         → tests.md
/slice-test-red        → <slice>_outside_in_test.dart (verified RED)
implementation         → until the outside-in test turns GREEN
```

The outside-in test is the **acceptance gate**. The slice is not done until that
single Dart test passes. Other tests (unit tests on use-case, adapter, cubit,
widget) are written after the green is reached, per the rules in
`agent_docs/testing.md`.

### Modifying an existing slice

When a behavior change is requested on a slice that is already implemented and
green, follow the same chain in reverse-then-forward order:

1. Update `tests.md` first to describe the new expected behavior. If the change
   affects requirements (e.g. a new failure mode), update `requirements.md` too,
   and only then `tests.md`.
2. Update `<slice>_outside_in_test.dart` to match the new `tests.md`. Run it and
   confirm it is now **red** against the current (pre-change) implementation.
3. Change the implementation until the outside-in test is green again.
4. Update affected unit tests, if any, as a final step.

This order is non-negotiable for behavior changes: the test changes first, the
code changes second. The outside-in test always represents the **current
contract**, never the past one.

For pure refactors (no behavior change), the outside-in test should stay
**unchanged** and **green** throughout. If it goes red during a refactor, you
are not refactoring — you are changing behavior, and the change needs a tests.md
update first.

### Optional zeroth step: `/grill-me`

The user may invoke `/grill-me` **before** `/to-prd` to stress-test a plan through
a question-and-answer interview. This is a discovery aid, not a producer.

`grill-me` produces **only conversation in the chat** — a series of questions, the
user's answers, and a short final summary. It does **not**:

- write or edit any source code, JSON, or generated files;
- run `build_runner`, `slang`, or any other command;
- create any spec file (`prd.md`, `plan.md`, `requirements.md`, `validation.md`);
- modify `specs/roadmap.md`.

When all questions in the interview are resolved, `grill-me` ends with a one-screen
summary of the decisions reached, and a single line: **"Next step: /to-prd"**. It
does not invoke `/to-prd` itself. The user runs the next command when they choose.

This boundary is mandatory regardless of how the `grill-me` skill itself is worded.

### Roadmap ownership

`/to-prd` is the **only** skill that writes to `specs/roadmap.md`. When invoked
for a new slice, it:

1. Reads `specs/roadmap.md` to find the maximum existing slice number.
2. Uses `max + 1` (zero-padded to four digits) as the new `<NNNN>`.
3. Creates the slice folder `specs/features/<feature>/<NNNN>_<slice>/`.
4. Adds a row to `specs/roadmap.md` with the new slice and status `📋`.
5. Writes prd.md inside the new folder.

The other three skills (`/feature-spec`, `/feature-requirements`,
`/feature-validation`) **never** modify roadmap.md. They only write their
respective file inside the existing slice folder.

If `to-prd` is designed to publish to an external issue tracker and no remote is
configured, it must still create the slice folder and save prd.md locally — not in
`agent_docs/` or any other fallback.

### Hard rules for spec files

- **Never** save generated specs to `agent_docs/`. That folder is for hand-written
  reference material that does not change per slice.
- Numbers in roadmap are **global** across the project, never per-feature.
- All spec files are written in **English**, regardless of the language used in the
  conversation that produced them.
- The slice spec folder is "complete" only when all four files exist. If a skill
  has produced one file and others are missing, run the next skill in the chain;
  do not declare the slice ready for implementation until all four are in place.

## Default test coverage for new slices

Every new slice ships with tests on **all four** layers, by default, without asking:

- **Use-case** — unit tests in `test/features/<f>/<s>/domain/usecases/`.
- **Adapter** — unit tests in `test/features/<f>/<s>/data/`, covering success +
  every HTTP failure code + unexpected exception (with `logger.error` verified).
- **Cubit/Bloc** — `bloc_test` in `test/features/<f>/<s>/application/`, covering
  every state transition.
- **Widget** — widget tests in `test/features/<f>/<s>/presentation/` with a mocked
  Cubit, covering each observable UI state.

When a skill (e.g. `to-prd`, `feature-spec`) reaches the step "which modules do you
want tested?", **apply this default and proceed without asking the user**. Ask only
if the user has explicitly overridden it earlier in the same session
(e.g. "skip widget tests this time").

For test patterns, mocktail conventions, and folder structure, see
`agent_docs/testing.md`.

## Working with context during modifications

When a task is local to a single slice (extending behaviour, internal refactor, bug fix):

- Read **only** the files of the slice being modified.
- Read `_shared/` only when strictly necessary (verifying an API client signature or an
  entity type).
- Do **not** read other slices. If the task requires reading them — that is a signal the
  task is not local; stop and ask before continuing.
- Do **not** read `core/` if the task is not about infrastructure.
- If a file outside the slice must be changed (e.g. `_shared/data/`) — stop and ask
  for confirmation. Do not change silently.

This keeps changes atomic, prevents regressions in neighbouring slices, and makes
code review trivial.

## Conflict resolution

1. **This file wins** over any general Flutter guidance or any skill.
2. If two skills contradict each other on layout or layering — follow this file.
3. Never propose alternative state-management packages "for variety". The stack is locked.
4. If the task does not fit the current architecture — say so explicitly. Do not silently
   bend the layers.

## Verifying changes

Before declaring a task done:

- `dart format .` — no diff.
- `dart analyze` — no warnings.
- `dart run build_runner build --delete-conflicting-outputs` — if codegen-affecting files
  changed (freezed, injectable, retrofit, slang).
- Affected tests pass: `flutter test` (or a narrower path).
- Slang regenerated if any `*.json` under `lib/core/i18n/i18n/` changed.
