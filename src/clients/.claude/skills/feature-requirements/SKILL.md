---
name: feature-requirements
description: This skill should be used when the user wants to generate requirements.md for a slice in this Flutter project. Trigger when the user invokes /feature-requirements, says "write requirements", "generate requirements doc", "fill in requirements for the slice", or asks to formalize functional and non-functional requirements after a PRD and plan already exist. Use this skill only after both prd.md and plan.md exist for the target slice — it reads both as input.
disable-model-invocation: false
---

# feature-requirements

Generate `requirements.md` for a slice. The output formalizes the user-language PRD
and the technical plan into numbered functional (F) and non-functional (N)
requirements that can be referenced individually in code review and validation.

## Process

### 1. Find the target slice

The slice is the one currently being worked on. Determine it in this order:

1. If the user explicitly named a slice ("requirements for delete_post"), use that.
2. Else look for the most recently modified `prd.md` under
   `specs/features/*/*/`. That folder is the target slice.
3. If multiple recent candidates exist, ask the user which one.

The slice folder path is `specs/features/<feature>/<NNNN>_<slice>/`. The output goes
to `<that folder>/requirements.md`.

### 2. Read the inputs

Read **both** files before writing anything:

- `specs/features/<feature>/<NNNN>_<slice>/prd.md` — source of user stories,
  business rules, and out-of-scope.
- `specs/features/<feature>/<NNNN>_<slice>/plan.md` — source of technical
  invariants, structural constraints, and architectural rules specific to the slice.

If either file is missing, stop and tell the user which one to create first
(`/to-prd` for prd.md, `/feature-spec` for plan.md). Do not improvise content.

Also skim `CLAUDE.md` and the relevant `agent_docs/*.md` for project-wide invariants
that apply to every slice (no hardcoded strings, double catch in adapters, no
cross-slice imports, etc.). These belong in the N-requirements section.

### 3. Derive requirements

**Functional requirements (F)** — what the system does, observable from the outside.
Source: PRD user stories + implementation decisions that describe behavior.
Phrasing: "The system shall…", "Route X opens…", "Button Y is shown when…".
One requirement per row.

**Non-functional requirements (N)** — technical invariants, architectural rules,
code-level constraints. Source: plan.md technical decisions + project-wide rules
from CLAUDE.md and agent_docs that apply to this slice.
Examples: "All UI strings via slang", "Adapter implements double catch per
agent_docs/error_handling.md", "Slice does not import other slices of the same
feature", "State is sealed class via freezed".

**Out of scope** — short bulleted list. Pull directly from the PRD's out-of-scope
section. Do not invent new exclusions.

### 4. Write the file

Use this exact template. Keep the section names, table structure, and ID format
unchanged. Numbering starts at F1, N1 — sequential, no gaps.

```markdown
# NNNN · slice_name — Requirements

## Functional Requirements

| ID | Requirement |
|---|---|
| F1 | <one-sentence behavior, observable from outside the system> |
| F2 | … |

## Non-functional Requirements

| ID | Requirement |
|---|---|
| N1 | <technical invariant or architectural rule> |
| N2 | … |

## Out of scope

- <item from PRD's out-of-scope section>
- …
```

Replace `NNNN` and `slice_name` with the actual values from the slice folder name.

### 5. Save and confirm

Save to `specs/features/<feature>/<NNNN>_<slice>/requirements.md`. Tell the user
the file was created and how many F and N requirements were generated. Suggest
running `/feature-validation` next to produce the matching validation checklist.

## Style rules

- **Always English**, regardless of the language used in PRD or plan source files.
  This is a project-wide convention.
- Each requirement is **one sentence**. If you need two sentences, it is two
  requirements.
- F-requirements describe **what**, not **how**. The "how" lives in plan.md.
  "User can delete their own post" — yes. "DeletePostCubit emits success state" — no,
  that is a plan detail.
- N-requirements describe **invariants the code must satisfy**, not actions taken.
  "Adapter has double catch" — yes. "Add a try/catch to the adapter" — no, that is
  a plan instruction.
- Do not duplicate what is already in plan.md word for word. Requirements are a
  formalization, not a copy.
- Out-of-scope items are short and absolute. "No bulk operations." Not "Bulk
  operations may be added later if needed."

## Common mistakes

- ❌ Skipping reading plan.md and writing only from PRD. Half the N-requirements
  come from the plan.
- ❌ Writing requirements in Russian because PRD is in Russian. Always English.
- ❌ Inventing F-requirements that have no basis in PRD. If a behavior is not in the
  PRD, it does not belong in requirements either; it belongs in a PRD revision.
- ❌ Writing N-requirements like "use freezed" without saying for what. Be precise:
  "State class is a sealed class generated by freezed".
- ❌ Numbering with gaps (F1, F3, F4) because a requirement was deleted during
  drafting. Renumber sequentially before saving.
- ❌ Creating the file outside the slice folder (e.g. in `agent_docs/` or in the
  repo root). The path is fixed: `specs/features/<feature>/<NNNN>_<slice>/requirements.md`.
