---
name: feature-validation
description: This skill should be used when the user wants to generate validation.md for a slice in this Flutter project. Trigger when the user invokes /feature-validation, says "write validation checklist", "generate validation doc", "fill in validation for the slice", or asks to produce a manual test plan and code review checklist after PRD, plan, and requirements already exist. Use this skill only after prd.md, plan.md, and requirements.md all exist for the target slice — it reads all three as input.
disable-model-invocation: false
---

# feature-validation

Generate `validation.md` for a slice. The output is a two-part acceptance checklist:
manual UX test scenarios and a code-level review checklist. Each item should be
directly verifiable — either by performing an action and observing a result, or by
inspecting the code.

## Process

### 1. Find the target slice

The slice is the one currently being worked on. Determine it in this order:

1. If the user explicitly named a slice ("validation for delete_post"), use that.
2. Else look for the most recently modified `requirements.md` under
   `specs/features/*/*/`. That folder is the target slice.
3. If multiple recent candidates exist, ask the user which one.

The slice folder path is `specs/features/<feature>/<NNNN>_<slice>/`. The output goes
to `<that folder>/validation.md`.

### 2. Read the inputs

Read **all three** files before writing anything:

- `prd.md` — source of UX flows and user stories that drive manual scenarios.
- `plan.md` — source of technical invariants for the code-review checklist
  (file moves, refactors, structural changes).
- `requirements.md` — source of F and N requirement IDs to trace back to.

If any of the three is missing, stop and tell the user which one to create first
(`/to-prd`, `/feature-spec`, or `/feature-requirements`).

Also skim `CLAUDE.md` and the relevant `agent_docs/*.md` for project-wide build and
verification commands (slang, build_runner, dart analyze, tests). These belong in
the universal tail of the code checklist.

### 3. Derive the two parts

**Part A — Manual testing (M).** A numbered table of concrete user actions and
their expected results. One row per scenario. Each scenario should be performable
by hand in under a minute.

How to derive: walk through every F-requirement and ask "what action proves this is
working?". One F can yield multiple M (e.g. "button shown for owner" yields one M
for owner, one for non-owner, one for unauthenticated). Also include the obvious
edge cases: empty state, error state, retry, in-flight disable, very long input,
very short input, bypass attempts.

**Part B — Code checklist.** A bulleted list of `- [ ]` items, each one
inspectable in the diff or in the running tree. Two sub-groups, in order:

- **Slice-specific items** — translate each N-requirement into something
  inspectable. "All UI strings via slang" → `[ ] No hardcoded strings in UI — all
  via context.t.<feature>.<slice>.*`. "Adapter has double catch" → `[ ] adapter
  contains both inner DioException catch and outer catch-all with logger.error`.
- **Universal tail** — every slice ends with the same closing items:
  - `[ ] dart run build_runner build --delete-conflicting-outputs` — no errors
  - `[ ] dart run slang` — no errors
  - `[ ] dart analyze` — no warnings
  - `[ ] All tests green`

### 4. Write the file

Use this exact template. Keep section names, table structure, and ID format
unchanged. Numbering starts at M1 — sequential, no gaps.

```markdown
# NNNN · slice_name — Validation Checklist

## Manual testing

| # | Step | Expected result |
|---|---|---|
| M1 | <user action> | <observable outcome> |
| M2 | … | … |

## Code review

- [ ] <slice-specific inspectable item derived from N-requirements>
- [ ] …
- [ ] `dart run build_runner build --delete-conflicting-outputs` — no errors
- [ ] `dart run slang` — no errors
- [ ] `dart analyze` — no warnings
- [ ] All tests green
```

Replace `NNNN` and `slice_name` with the actual values from the slice folder name.

### 5. Save and confirm

Save to `specs/features/<feature>/<NNNN>_<slice>/validation.md`. Tell the user
the file was created, how many M scenarios were generated, and how many code-checklist
items it contains. The slice's spec folder is now complete (4 files: prd, plan,
requirements, validation) — note this in the confirmation.

## Style rules

- **Always English**, regardless of the language used in source files. Project-wide
  convention.
- Manual scenarios are **specific actions**, not generalizations. "Open
  /user/testuser/posts not logged in" — yes. "Test that anyone can view posts" — no,
  that is a requirement, not a scenario.
- Each M row is **one action and one observable result**. If the result has multiple
  observable parts, that is one scenario; if the action has multiple steps, split it.
- Code checklist items are **diff-inspectable** or **command-runnable**. "Slice does
  not import other slices" — yes (grep-able). "Code is well-structured" — no
  (subjective).
- Universal tail items are always present, exactly as shown, in the same order.

## Common mistakes

- ❌ Writing scenarios that match requirements 1:1 instead of one-to-many. The
  point of validation is to test each requirement under multiple conditions.
- ❌ Skipping the "bypass attempt" scenario when there is an ownership or permission
  rule. Always test the negative case (non-owner can't see the button, server
  rejects bypass).
- ❌ Forgetting the universal tail (build_runner, slang, dart analyze, tests).
  These four lines are non-negotiable on every slice.
- ❌ Writing items in the code checklist that no human can verify by reading the
  diff. If you cannot grep for it or run a command, it is not a checklist item.
- ❌ Creating the file outside the slice folder. The path is fixed:
  `specs/features/<feature>/<NNNN>_<slice>/validation.md`.
- ❌ Numbering scenarios with gaps after deleting one during drafting. Renumber
  sequentially before saving.
