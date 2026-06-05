---
name: to-prd
description: Turn the current conversation context into a PRD and publish it to the project issue tracker. Use when user wants to create a PRD from the current context.
---

This skill takes the current conversation context and codebase understanding and produces a PRD. Do NOT interview the user — just synthesize what you already know.

The issue tracker and triage label vocabulary should have been provided to you — run `/setup-matt-pocock-skills` if not.

## Process

1. Explore the repo to understand the current state of the codebase, if you haven't already. Use the project's domain glossary throughout the PRD (cinema booking domain: movie sessions, shopping carts, seats, reservations, cinema halls). Respect any ADRs in the area you're touching.

2. Sketch out the major MediatR use-cases (commands and queries) and aggregates you will need to build or modify to complete the implementation. Each use-case is an `IRequestHandler<TRequest, TResult>` tested via xUnit + `WebApplicationFactory<Program>`. Actively look for opportunities to extract deep modules that can be tested in isolation.

   A deep module (as opposed to a shallow module) is one which encapsulates a lot of functionality in a simple, testable interface which rarely changes.

   Check with the user that these use-cases and aggregates match their expectations. Check with the user which ones they want tests written for.

3. Write the PRD using the template below, then publish it to the project issue tracker. Apply the `needs-triage` triage label so it enters the normal triage flow.

4. Create the slice folder at `specs/features/<aggregate>/<NNNN>_<slice>/` and write `prd.md` there. Update `specs/roadmap.md` (add a new row, state = Started). This skill owns both files.

<prd-template>

## Problem Statement

The problem that the user is facing, from the user's perspective.

## Solution

The solution to the problem, from the user's perspective.

## User Stories

A LONG, numbered list of user stories. Each user story should be in the format of:

1. As an <actor>, I want a <feature>, so that <benefit>

<user-story-example>
1. As a cinema customer, I want to reserve a seat in a movie session, so that I can guarantee my preferred spot before paying
</user-story-example>

This list of user stories should be extremely extensive and cover all aspects of the feature.

## Implementation Decisions

A list of implementation decisions that were made. This can include:

- The MediatR use-cases (commands/queries) and aggregates that will be built or modified
- The interfaces of those use-cases and aggregates that will be modified
- Technical clarifications from the developer
- Architectural decisions
- Schema changes
- API contracts
- Specific interactions

Do NOT include specific file paths or code snippets. They may end up being outdated very quickly.

## Testing Decisions

A list of testing decisions that were made. Include:

- A description of what makes a good test (only test external behavior, not implementation details)
- Which use-cases and aggregates will be tested
- Prior art for the tests (i.e. similar types of tests in the codebase)

## Out of Scope

A description of the things that are out of scope for this PRD.

## Further Notes

Any further notes about the feature.

</prd-template>
