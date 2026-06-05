---
name: project-memory
description: This skill should be used at the start and at the end of work on any feature or slice in this project. Trigger when the user says "new feature", "new slice", "start a feature", "start working on", "spec for X", "let's plan X", "I want to add X functionality", or anything that signals the beginning of a non-trivial change. Also trigger at the end — phrases like "I'm done with X", "finished implementing", "wrap up the slice", "this slice is ready", or when a slice's tests pass and the conversation is winding down. This skill walks the user through the procedure for `specs/`, `roadmap.md`, and `.claude/decisions/`. Make sure to use this skill even if the user does not explicitly ask for it — they often forget the bookkeeping, and missing it breaks the project's memory.
---

# Project memory procedure

This skill is the operational counterpart to `agent_docs/project_memory.md`. Read that
reference first if you need the rationale; this skill is the checklist.

## At the start of a new slice

Walk through these steps in order. Each one depends on the previous.

**Step 1.** Open `specs/roadmap.md` and find the largest existing four-digit slice
number. The next slice gets that number + 1. Numbers are global across the project,
not per-feature.

**Step 2.** Create the slice folder:
`specs/features/<feature_name>/<NNNN>_<slice_name>/`. Inside it, create four empty
stubs: `prd.md`, `plan.md`, `requirements.md`, `validation.md`.

**Step 3.** Add a row to `specs/roadmap.md` in the appropriate section, with status
`📋` (planned) and a relative link to the new folder. Keep the table sorted by number.

**Step 4.** Run a discovery conversation using the `grill-me` skill if it is
available. The point of grill-me is to challenge assumptions before a single line of
code is written; treat the result as raw input, not as the spec itself.

**Step 5.** Generate `prd.md` from the grill-me conversation using the `to-prd` skill.

**Step 6.** Generate `plan.md`, `requirements.md`, and `validation.md` from the PRD
using the `feature-spec` skill.

**Step 7.** Before writing any code, check `.claude/decisions/` for ADRs whose titles
match keywords in the new slice. A previous similar slice may have already established
a pattern that this one should follow.

**Step 8.** Implement the slice. Layout follows `agent_docs/architecture.md`. State
patterns follow the `bloc-state-management` skill. Adapter patterns follow
`agent_docs/error_handling.md`. Tests follow `agent_docs/testing.md`.

## At the end of a slice

Two records may need to be created. Do not skip either without thinking.

**Decision 1: was a non-trivial problem solved during implementation?**

If a future developer (or future Claude session) is plausibly going to hit the same
problem, write a short note in `.claude/decisions/issues/resolved/<NNNN>_<title>.md`.
Format is loose: one paragraph for the problem, one for the resolution, one or two
file references. The goal is searchability when someone hits the same wall later.

If the problem was a one-off that nobody else will encounter — a typo in the API
schema, a flaky test that turned out to be local — skip this. The resolved-issues
folder is not a debug log; signal-to-noise matters.

**Decision 2: was an architectural decision made?**

If the slice established a rule or pattern that other slices will follow — a new way
to handle a class of cases, a new boundary, a new convention — write an ADR in
`.claude/decisions/<NNNN>_<title>.md`. Use the standard four-section shape:

- **Context**: what the situation was and what forces were at play.
- **Decision**: what was decided.
- **Consequences**: what becomes easier and what becomes harder.
- **Status**: proposed / accepted / superseded.

Keep ADRs short. If yours runs longer than two pages, the decision is too large and
needs to be split into smaller decisions.

If the ADR establishes a rule that should apply **everywhere** — not just inside one
feature — also update `agent_docs/` (or `CLAUDE.md` for truly universal rules) and
reference the ADR from the updated section. The ADR is the historical record; the
agent docs are the live operational guide. Both are needed; they serve different
audiences.

**Step 3.** Update the row in `specs/roadmap.md` to status `✅` (done).

**Step 4.** If user-facing behaviour changed, append a one-line entry to
`CHANGELOG.md`. The roadmap tracks slices; the changelog tracks shipped behaviour.
They are not the same.

## Quick visual reminder of where each thing goes

| Information type | File location | When written |
|---|---|---|
| What is being built and why | `specs/features/<f>/<NNNN>_<s>/prd.md` | Before implementation |
| Implementation plan | `specs/features/<f>/<NNNN>_<s>/plan.md` | Before implementation |
| Acceptance criteria | `specs/features/<f>/<NNNN>_<s>/validation.md` | Before implementation |
| Status of all slices | `specs/roadmap.md` | Updated at each phase transition |
| Why a non-trivial choice was made | `.claude/decisions/<NNNN>_<title>.md` | After implementation, if applicable |
| Recurring problem and its fix | `.claude/decisions/issues/resolved/<NNNN>_<title>.md` | After implementation, if applicable |
| What shipped and when | `CHANGELOG.md` | After implementation, if user-facing |
| A rule that applies everywhere | `agent_docs/*.md` or `CLAUDE.md` | When the rule is established |

## Common mistakes

- ❌ Skipping the spec because "this slice is small". Small slices that skip the spec
  accumulate into a feature with no design record. The spec is cheap; write it.
- ❌ Writing the spec retroactively to match the implementation. The PRD is a
  point-in-time contract. If implementation diverged, the divergence is the ADR, and
  the PRD stays as it was.
- ❌ Numbering slices per-feature (`users/0001`, `orders/0001`). Numbers are global.
- ❌ Treating `.claude/decisions/issues/resolved/` as a personal debug log. Most bugs
  do not belong there. Only those a future developer will plausibly hit again do.
- ❌ Updating the roadmap status to ✅ before tests pass and validation criteria are
  met. The status reflects reality, not aspiration.
- ❌ Writing an ADR but not updating the agent docs when the rule is universal. The
  ADR alone is a tombstone; future slices will not find it. Both must change.
