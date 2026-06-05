---
name: slice-decomposition
description: This skill should be used whenever the user is deciding how to organize new functionality in the project — whether something is a new slice, an extension of an existing slice, a separate feature, or a piece of `_shared/` code. Trigger on phrases like "new feature", "new slice", "where should I put", "should this be its own slice", "split this slice", "extend this slice", "add to feature X", "decompose", "organize this functionality", "list with filtering", "edit screen", "delete operation", or any planning conversation that mentions multiple operations or screens within one feature. Also trigger when the user describes a new screen and you need to decide which slice it belongs to. This is a planning/decision skill — invoke it before code is written, not while writing code.
---

# Slice decomposition

## The decision procedure

When new functionality is being designed, ask three questions in order. Stop at the
first one that gives a clear answer.

**Question 1: Does the new functionality have its own screen and an independent flow?**

If yes — different screen, distinct user journey, can be entered independently — it is
a new slice. The list-of-users screen and the create-user screen each have their own
entry points, their own state, and their own independence; they are separate slices
even though they live in the same feature.

**Question 2: Does the new functionality fit into the existing Cubit's state machine
without stretching it?**

If a small extension naturally fits — pagination is a refinement of "loading a list",
pull-to-refresh is "trigger a reload", filtering is "reload with different params" —
it stays in the existing slice. The state machine simply gains another transition or
parameter; the shape is preserved.

If the existing state class would need a fundamentally new dimension (a different
kind of "what am I doing right now") — that is a different state machine and belongs
in its own slice. The signal you can trust: you are about to add a sealed subtype to
the state that has nothing to do with the existing subtypes.

**Question 3: Can the operation be invoked from multiple, independent places?**

If the same operation is triggered from a list, from a details screen, and from a
context menu — and each invocation needs the same orchestration — it is one slice that
those screens consume. `delete_user` invoked from three places is one
`delete_user` slice with one Cubit, not three copies.

## Worked examples for this project

The `users` feature, fully decomposed:

| Slice | Scope |
|---|---|
| `list_users` | List + pagination + pull-to-refresh + filters/sorting. All these are refinements of one state machine: "load a page of users". |
| `create_user` | Creation form + validation. Its own screen, independent state, separate flow. |
| `edit_user` | Editing form. Separate from `create_user` because the state and validations differ — an edit starts populated, may have partial updates, may be cancelled with confirmation. |
| `user_details` | Single-user view. Different read shape than the list, often loaded independently (e.g. via deep link). |
| `delete_user` | Confirmation + deletion. Invoked from list and from details. |

Read the table top to bottom. Notice that pagination did not become its own slice —
it folded into `list_users` because it is the same state machine refined. Notice that
edit and create stayed split — they share an entity, not a state machine.

## Where shared code lives — and where it does not

When two slices end up needing the same code, it goes in `_shared/`. Concretely, that
applies to entities (like `User`), API clients (the Retrofit `UsersApiClient` shared
across the feature), and widgets that appear on multiple screens.

It does **not** apply to ports and abstractions. If two slices need a `loadUsers`
method, they declare two narrow ports — `ListUsersPort` in one slice and (say)
`PickUserPort` in another. Even if the methods look identical today, they are
contracts in different contexts and will evolve independently. Lifting them into a
shared interface couples slices that should be decoupled.

This is a counter-intuitive rule worth dwelling on. Most architectural advice says
"deduplicate aggressively". This project says: **deduplicate concrete code,
duplicate abstractions**. The reason is that the cost of duplicating an interface is
small (a few lines), and the cost of merging two contexts under a shared interface is
large (every change in either slice now has to consider the other).

For the full layering rules and the reasoning, read `agent_docs/architecture.md`
sections on `_shared/` and on bounded contexts.

## Signs you got the decomposition wrong

- The Cubit has more than five distinct sealed state subtypes that do not share a
  common phase (loading, success, error). The slice probably contains two state
  machines.
- A function in one slice imports from another slice, even via a workaround like
  re-exporting. This is the architecture telling you the boundary is in the wrong
  place.
- `_shared/` contains an interface used by exactly one slice. It belongs inside that
  slice's `domain/ports/`, not in `_shared/`.
- A new screen was added to an existing slice and the slice's tests now have setup
  branches for "list mode" and "detail mode". The screen needs its own slice.

## How to act on a wrong decomposition

If you discover during implementation that a slice is doing too much, **stop and
escalate**, do not silently restructure. The right sequence is:

1. Note the issue in `.claude/decisions/issues/resolved/` after the conversation.
2. Add a new slice folder (or split an existing one) per the procedure in
   `agent_docs/project_memory.md`.
3. Update `specs/roadmap.md` to reflect the split.
4. Move code in small, reviewable steps.

Silent restructuring during a single task makes the change hard to review and easy to
revert by accident. A split that is announced and tracked is cheap to land.
