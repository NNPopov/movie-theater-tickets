---
name: spec-workflow
description: This skill should be used at the start of any work on a slice specification in this Flutter project — when the user wants to create a new spec, continue an in-progress spec, or is unsure which step comes next. Trigger on phrases like "let's work on a spec", "start a new slice", "next spec step", "help with the spec", "what's next on this slice", "I want to add functionality X", or any phrase indicating spec work without naming a specific spec command. Also trigger when the user invokes /spec-workflow explicitly. This skill is a dispatcher — it loads architecture context, inspects the slice folder, and tells the user which of the four spec commands to run next. It does not produce any of the four spec files itself.
disable-model-invocation: false
---

# spec-workflow

The dispatcher for the four-step spec generation chain. Loads project architecture
context, inspects the slice folder state, and tells the user which command to run
next. Never runs commands itself — always advises.

## Process

### 1. Load architectural context

Before doing anything else, read these unconditionally:

- `CLAUDE.md`
- `agent_docs/spec_workflow.md`
- `agent_docs/architecture.md`
- `agent_docs/error_handling.md`

If `agent_docs/spec_workflow.md` is missing, stop and tell the user the workflow
reference is not in the repo — they need to add it before this skill can guide them.

These four files are the architectural baseline. Any advice this skill gives must
be consistent with them.

### 2. Identify the target slice

Determine which slice the user is working on, in this order:

1. If the user explicitly named a slice ("spec for delete_post", "the user_posts
   slice"), use that.
2. If the user mentioned a feature or behavior without naming an existing slice
   ("I want to add post deletion"), treat it as a **new** slice. Determine the next
   slice number by reading `specs/roadmap.md` and taking `max(existing) + 1`,
   zero-padded to four digits. Do not yet create any folder — that is `/to-prd`'s job.
3. If the user said only "let's work on the spec" without context, ask them which
   feature they want to work on. Do not guess.

### 3. Conditionally read more context

Skim the user's intent (or the existing PRD if it exists) to decide which
additional `agent_docs/*.md` files are relevant:

- Mentions of permissions, owners, roles, "only the author can…" → `agent_docs/rbac.md`.
- Mentions of UI strings, dialogs, error messages, locales → `agent_docs/localization.md`.
- Mentions of new screens, routes, deep links, guards → `agent_docs/navigation.md`.
- Mentions of test patterns or coverage beyond the project default → `agent_docs/testing.md`.

Reading these is cheap; do not skip when in doubt.

### 4. Inspect the slice folder

Look at `specs/features/<feature>/<NNNN>_<slice>/` for the target slice. Determine
its state by which files exist:

| Files present | State | Next command |
|---|---|---|
| Folder does not exist | Not started | `/to-prd` |
| `prd.md` only | PRD written, no plan | `/feature-spec` |
| `prd.md` + `plan.md` | Plan written, not formalized | `/feature-requirements` |
| `prd.md` + `plan.md` + `requirements.md` | Requirements written, no validation | `/feature-validation` |
| All four | Spec complete | Ready for implementation |

### 5. Architectural review (only if proceeding to /feature-spec)

If the next step is `/feature-spec` (i.e., PRD exists but plan does not), perform a
brief architectural review **before** advising:

1. Find the closest completed analogous slice in `specs/features/*/` (look for
   similar verb prefixes: `delete_*`, `create_*`, `edit_*`, etc., or similar shapes:
   "list with pagination", "form with validation", "detail screen with action").
2. Read its `plan.md` to calibrate the expected detail level and conventions.
3. Check whether the new feature fits an existing pattern.

If the new feature does **not match any existing pattern** — for example, a new UI
behavior, a new integration shape, a new way of handling errors — **stop and ask**:

> "I don't see a precedent for X in the existing slices. The closest is Y, but Y
> handles this differently because [...]. Before we write the plan, we need to
> decide:
> - propose a new pattern (and you'll review),
> - use a workaround based on Y, or
> - pause and define how this should look.
>
> What would you like?"

Wait for the user's answer. Do not advise running `/feature-spec` until this is
resolved. If a new pattern is established, suggest the user record it as an ADR
in `.claude/decisions/` after the spec work is done.

### 6. Advise the user

Output a short message that contains:

- The slice name and number being worked on.
- The current state of its folder (what's done, what's missing).
- The next command the user should run.
- Any conditional readings the user (or the next skill) should be aware of (e.g.
  "this slice involves permissions — `/feature-requirements` should reference
  `agent_docs/rbac.md`").
- If a new pattern came up in step 5, surface that conversation now.

**Stop after advising.** Do not run the next command. The user issues every command
themselves.

## Output format

Use this template for the advice message:

```
Slice: <NNNN>_<slice_name> (feature: <feature>)
State: <one of: not started / PRD written / plan written / requirements written / complete>

Next step: <command>

Notes:
- <any relevant context from the conditional readings>
- <any pattern questions raised in step 5>
```

Keep it terse. The user does not need a lecture; they need to know what to do next.

## Style rules

- **Always advise, never act.** This skill loads context and answers "what's next?"
  — it does not write spec files. The four producer skills handle that.
- **Always English** in advice output and in any spec content the next command will
  produce. Project-wide convention.
- **Never modify roadmap.md.** That is `/to-prd`'s exclusive job.
- **Never create the slice folder.** That is `/to-prd`'s job too.
- If multiple slices look like candidates, ask which one rather than guessing.

## Common mistakes

- ❌ Running `/to-prd` (or any other producer skill) from inside this skill. This
  skill is read-only — it advises, the user runs the commands.
- ❌ Skipping the architectural-context read in step 1. Advice without that context
  ends up steering the user into anti-patterns.
- ❌ Skipping step 5 (architectural review) when the next step is `/feature-spec`.
  Step 5 is the only place where new patterns get caught before they are baked into
  the plan.
- ❌ Modifying any spec file or any roadmap entry. This skill is dispatch-only.
- ❌ Producing the advice message in Russian because the conversation is in Russian.
  Advice and all generated specs are in English.
- ❌ Continuing to advise running `/feature-spec` when step 5 raised a pattern
  question that the user has not yet answered. Wait for the answer.
