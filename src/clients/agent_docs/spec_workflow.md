# Spec workflow reference

This document describes **how this project works on slice specifications**. It is the
authoritative process guide for the four-step spec generation chain. The `/spec-workflow`
skill reads this file and uses it to dispatch the user to the correct step.

You should also read this file by hand if you want to understand how spec work is
expected to flow in this project — it is not just for the agent.

## Mindset: why specs are written in five steps

A slice spec is built up in five passes, each with its own focus and own input set.
Mixing the focuses produces sloppy specs, so the order matters.

**Pass 1 — Requirements gathering (PRD).** What the system should do, from the
user's perspective. No technical details. No mention of layers, ports, or adapters.
Output: `prd.md`. Tool: `/to-prd`.

**Pass 2 — Architectural review (the human/Claude step between PRD and plan).**
Read the existing architecture and check: does the new behavior fit existing
patterns? Are there similar slices to use as references? Is anything genuinely new?
This is the **mental** step before writing the plan; it does not produce a file.
What it produces is **confidence** that the plan can be written, or a **stop signal**
that the team needs to discuss something new.

**Pass 3 — Implementation plan.** How the requirements get implemented inside the
existing architecture. Specific files, specific signatures, specific decisions.
Output: `plan.md`. Tool: `/feature-spec`.

**Pass 4 — Formalization (requirements + validation).** Turn the PRD and plan into
formal, traceable artifacts. Functional and non-functional requirements with IDs.
Manual test scenarios and code review checklist. Outputs: `requirements.md`,
`validation.md`. Tools: `/feature-requirements`, `/feature-validation`.

**Pass 5 — Outside-in test specification and red.** Define the single integration
test that proves the slice works end-to-end, first as markdown (`tests.md`), then
as failing Dart code (`<slice>_outside_in_test.dart`). The red test is the
**acceptance gate** for the implementation that follows. Outputs: `tests.md`,
`<slice>_outside_in_test.dart` (RED). Tools: `/feature-tests`, `/slice-test-red`.

The reason this is five steps and not one is the **mindset shift between them**.
You cannot write requirements while thinking about implementation. You cannot
write a plan while thinking about user stories. You cannot write the red test
without knowing the public surface from the plan. The five-step rhythm forces
each mindset to do its work cleanly.

## Always-read context for any spec work

Before any pass that produces a file, the architectural baseline must be in context.
Read these unconditionally at the start of any spec-related session:

- `CLAUDE.md` — universal rules and defaults that apply to every slice.
- `agent_docs/architecture.md` — layers, ports/adapters, `_shared/`, bounded contexts,
  DI rules. This document defines the shape every slice must respect.
- `agent_docs/error_handling.md` — Failure types, soft DTO contract, double catch in
  adapters. Every slice that talks to the network must satisfy these.

These three are the **non-negotiable baseline**. Without them, any generated spec
risks contradicting the project's own architecture.

## Conditionally-read context

Read these **only if** the slice in question touches the corresponding area. Decide
by skimming the user's intent or the existing PRD:

- `agent_docs/rbac.md` — if the slice involves permissions, roles, or guards
  (any user story mentioning "owner", "admin", "only logged-in", etc.).
- `agent_docs/localization.md` — if the slice involves UI strings, dialogs, error
  messages visible to the user, or new locale support.
- `agent_docs/navigation.md` — if the slice introduces a new screen, modifies
  routing, deep links, or guards on routes.
- `agent_docs/testing.md` — if the slice modifies test conventions or the user is
  asking about test coverage explicitly. The default coverage rules in CLAUDE.md
  cover the common case; this file is for deeper work on test patterns.

Reading conditionally is cheaper than reading everything, but only if the
conditions are honestly checked. If in doubt, read.

## Reference slice: always find one

Before writing `plan.md` for a new slice, find the **closest completed analogous
slice** in `specs/features/*/`. For example:

- A new `delete_*` slice — look at `delete_user`.
- A new `create_*` slice — look at `create_post` or `create_user`.
- A new list-with-pagination slice — look at `user_posts`, `list_users`.

Read its `plan.md`, `requirements.md`, and `validation.md`. Do not copy them.
Use them to calibrate the level of detail, the section structure, and the technical
conventions specific to this project.

If no analogous slice exists — that is itself a signal. See the next section.

## When you encounter a pattern that does not yet exist

This is the most important rule in this whole document.

If during the architectural review (pass 2) you discover that the new feature does
**not match any existing pattern** — for example, the project has only ever shown
confirmation dialogs at the bottom, but this feature needs a popup at the top —
**stop**. Do not invent the pattern unilaterally.

Tell the user: "I don't see a precedent for X in the existing slices. The closest is
Y in slice Z, but Z does it differently because [...]. Should I:

- propose a new pattern (and you'll review it), or
- use a workaround based on Y, or
- pause and you'll define how this should look?"

Wait for the user's answer. After the answer, if a new pattern was established,
record it as an ADR in `.claude/decisions/<NNNN>_<title>.md` so the next slice can
find it.

The reason this rule exists: silently inventing patterns is the single biggest source
of architectural drift. One slice introduces a quiet new convention, the next slice
copies it as if it were the standard, and within a few slices the original convention
is gone. Stop-and-ask is the only reliable defense.

## State of the slice folder: how to read it

A slice folder lives at `specs/features/<feature>/<NNNN>_<slice>/`. Its state at any
moment is one of these:

| State | Files present (in slice folder) | Outside-in test file | What's next |
|---|---|---|---|
| Empty / not created | none | absent | Run `/to-prd` to start |
| Started | `prd.md` only | absent | Run `/feature-spec` |
| Planned | `prd.md`, `plan.md` | absent | Run `/feature-requirements` |
| Formalized | `prd.md`, `plan.md`, `requirements.md` | absent | Run `/feature-validation` |
| Validated | `prd.md`, `plan.md`, `requirements.md`, `validation.md` | absent | Run `/feature-tests` |
| Test-specified | all five `.md` files | absent | Run `/slice-test-red` |
| Red gate set | all five `.md` files | present and RED | Implement the slice |
| Complete | all five `.md` files | present and GREEN | Ready for unit-test pass and merge |

The `/spec-workflow` skill checks this state and tells the user the next step.
It does not run the next step itself — the user always issues the command.

## Hard limits for `/grill-me`

`/grill-me` is the optional discovery interview that a user may run **before**
`/to-prd`. Its only legitimate output is **the conversation itself** — a series of
questions, the user's answers, and a final summary in the chat.

The skill description (which may come from a third-party source) might be vague
about boundaries. The boundaries below are mandatory regardless of how that
description is worded; they apply whenever `/grill-me` is in use within this
project:

- ❌ Editing source files in `lib/` — including widgets, screens, cubits, adapters,
  routes, anything.
- ❌ Editing localisation files (`*.json`) under `lib/core/i18n/`.
- ❌ Running `dart run build_runner`, `dart run slang`, `flutter test`, or any
  other shell command.
- ❌ Creating, modifying, or deleting any file under `specs/` — including
  `prd.md`, `plan.md`, `requirements.md`, `validation.md`, or `roadmap.md`.
- ❌ Creating any folder anywhere in the repo.

What `/grill-me` **may** do:

- Read existing source files to inform the questions it asks (read-only exploration).
- Read existing spec files (`prd.md`, `plan.md`, etc.) for the same reason.
- Ask the user clarifying questions, one at a time, with a recommended answer.
- After all branches of the decision tree are resolved, output a one-screen
  summary of decisions reached, ending with a single line: **"Next step: /to-prd"**.

The skill must not invoke `/to-prd` itself. The user runs the next command when
they choose. This preserves the "advise, don't act" principle that all spec-stage
skills follow in this project.

If at any point during a `/grill-me` session the user's answer triggers an instinct
to "just do it now" — stop. Finish the interview with a summary. The user's "Ok" to
a proposed answer is consent for **the answer**, not consent to start implementing.

## Roadmap interaction

`specs/roadmap.md` is updated **only by `/to-prd`**, when the slice folder is first
created. The other three skills (`/feature-spec`, `/feature-requirements`,
`/feature-validation`) do not modify roadmap.md.

Numbers in roadmap are global across the project, not per-feature. The next slice
gets `max(existing) + 1`, zero-padded to four digits.

## Language

All four spec files are written in **English**, regardless of the language used in
the conversation. This is a project-wide convention to keep specs consistent and
international-team-ready.

## Common workflow mistakes

- ❌ Calling `/feature-spec` without an existing PRD. The plan needs the PRD as
  input. If there is no PRD, run `/to-prd` first.
- ❌ Skipping the architectural review (pass 2). Without checking existing slices,
  the plan ends up reinventing patterns the project already has.
- ❌ Inventing a new pattern silently because "the user is busy". The user is not
  too busy for a 30-second answer. They are too busy for a week of refactoring later.
- ❌ Calling `/feature-validation` before `/feature-requirements`. Validation traces
  back to requirement IDs — without requirements there is nothing to trace.
- ❌ Editing `roadmap.md` from any skill other than `/to-prd`. This produces
  duplicate or conflicting rows.
- ❌ Treating the four files as independent. They are a chain — each downstream
  file depends on the upstream files being accurate.
- ❌ Writing specs in Russian because the conversation is in Russian. Always English.
