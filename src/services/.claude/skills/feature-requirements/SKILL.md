---
name: feature-requirements
description: This skill should be used when the user wants to generate requirements.md for a slice. Trigger when the user invokes /feature-requirements, says "formalize the requirements", or asks for the F/N traceability after plan.md exists. Reads prd.md and plan.md. Produces requirements.md only — formal functional (F) and non-functional (N) requirements with IDs that the validation and tests phases trace back to.
disable-model-invocation: false
---

# feature-requirements

Generate `requirements.md` for a slice. Translates the PRD and plan into
formal, ID-bearing requirements that downstream documents (`validation.md`,
`tests.md`, and code review) can reference.

## Process

### 1. Find the target slice

Same determination as in `/feature-spec`:

1. User-named path.
2. Most recently modified `plan.md` under `specs/features/*/*/`.
3. If ambiguous, ask.

Output: `specs/features/<aggregate>/<NNNN>_<slice>/requirements.md`.

### 2. Read the inputs

Mandatory:

- The slice's `prd.md`.
- The slice's `plan.md`.
- `CLAUDE.md`.
- `agent_docs/architecture.md`.
- `agent_docs/error_handling.md`.

If `plan.md` is missing, stop and ask the user to run `/feature-spec` first.

### 3. Write `requirements.md`

Use this structure:

```markdown
# NNNN · SliceName — Requirements

## Inputs

- PRD: ./prd.md
- Plan: ./plan.md

## Functional requirements

Numbered as F1, F2, F3... Each is a single testable statement.

- **F1.** The endpoint `<METHOD> /api/<resource>/...` accepts `<UseCase>Request`
  with fields `<a, b, c>` and returns `<UseCase>Response` with HTTP status
  `<code>` on success.
- **F2.** The validator `<UseCase>CommandValidator` rejects the command with a
  `ValidationException` (HTTP 400 `ValidationProblemDetails`) when `<field>`
  violates `<constraint>`.
- **F3.** The handler throws `ContentNotFoundException` (HTTP 204) when the
  aggregate with id `<X>` does not exist.
- **F4.** The handler throws `ConflictException` (HTTP 409) when `<business
  conflict condition>`.
- **F5.** The handler throws `ForbiddenAccessException` (HTTP 403) when the
  caller does not satisfy `<auth predicate>`.
- **F6.** The repository catches `DbUpdateException` on a unique-index
  violation and rethrows it as `ConflictException` / `DuplicateRequestException`.
- ...

Cover every branch in the handler, every infrastructure exception the repository
maps, every authorization rule, every status code the endpoint can return. Use
the `CustomExceptionHandler` mapping table in `agent_docs/error_handling.md`
to derive the HTTP status for each exception.

## Non-functional requirements

Numbered N1, N2, N3...

- **N1.** The use-case is a MediatR `IRequestHandler<TCommand, TResult>`;
  the command is a `record` implementing `IRequest<TResult>`. Per
  `agent_docs/architecture.md`.
- **N2.** The repository interface (`I<Aggregate>Repository`) lives in
  `Domain/<Aggregate>/Abstractions/`; the EF Core implementation lives in
  `Infrastructure/Repositories/`. Per `agent_docs/architecture.md`.
- **N3.** The repository catches **only** business-meaningful infrastructure
  exceptions (e.g. `DbUpdateException` on a unique violation →
  `ConflictException`). Other infrastructure exceptions propagate to
  `CustomExceptionHandler`. The repository does not log. Per
  `agent_docs/error_handling.md`.
- **N4.** `Domain` and `Application` contain no EF Core types or `DbContext`
  references. EF Core lives only in `Infrastructure`. Per
  `agent_docs/architecture.md` (Dependency Rule).
- **N5.** The handler raises no HTTP-related exceptions and does not touch
  `HttpContext`. HTTP translation happens once, in `CustomExceptionHandler`. Per
  `CLAUDE.md` rule 5.
- **N6.** The endpoint delegate contains no business logic: it binds the
  request, builds the command, calls `ISender.Send`, and shapes the HTTP result.
  Per `agent_docs/entry_points/minimal-api.md`.
- **N7.** Validation lives in `AbstractValidator<TCommand>`, discovered by
  `AddValidatorsFromAssembly` and executed by `ValidationBehaviour<,>`. The
  handler enforces only business rules the validator cannot express. Per
  `agent_docs/architecture.md` § Validation.
- **N8.** All I/O is `async`/`await` with `CancellationToken` threaded through.
  No synchronous database or I/O calls. Per `CLAUDE.md`.
- **N9.** The build produces no new warnings (`dotnet build -warnaserror`). Per
  `CLAUDE.md` § Verifying changes.
- **N10.** Architecture tests (`BookingManagementService.Domain.ArchitectureTests`)
  pass without new failures. Per `agent_docs/testing.md` § Architecture tests.

The N list is mostly stable across slices. Copy it from the previous slice's
requirements and adjust only what is slice-specific. Do not invent new N
requirements — those belong in `agent_docs/`.

## Out of scope

Bullet list, copied or adapted from `plan.md` section 7.

## Traceability

Mapping from requirement ID to where it is verified.

| Requirement | Verified by |
|---|---|
| F1 | endpoint integration test |
| F2 | endpoint integration test (400 response) |
| F3 | handler unit test + endpoint integration test (204) |
| F4 | handler unit test + endpoint integration test (409) |
| F5 | endpoint integration test (403) |
| F6 | repository unit test |
| F1 (happy path) | outside-in test |
| N1–N10 | code review checklist in validation.md + architecture tests |
```

### 4. Save and confirm

Write to `specs/features/<aggregate>/<NNNN>_<slice>/requirements.md`. Tell the
user the file was created, list the count of F and N requirements, and suggest:

> Next step: `/feature-validation` to produce validation.md.

## Style rules

- **English only**.
- **One sentence per requirement.** A requirement that needs two sentences is
  two requirements.
- **Concrete identifiers** (C# class names, namespaces, HTTP paths, status
  codes), not placeholders.
- **Reference agent_docs** for each N requirement that comes from a project
  rule. The reference is the source of truth; the N entry is a pointer.
- **Stable N list.** If a non-functional requirement is project-wide, it
  appears in every slice's requirements.md with the same wording. Drift
  between slices is a smell.

## Hard limits

- Do not write any file other than `requirements.md`.
- Do not modify `prd.md`, `plan.md`, or `roadmap.md`.
- Do not invent new architectural rules. If a requirement does not have a
  basis in `agent_docs/` or `CLAUDE.md`, it does not belong in N. Ask the user.
- Do not run shell commands.

## Common mistakes

- A functional requirement that does not name the operation observably: "the
  handler handles errors properly." Specify which error, which input, which
  HTTP status.
- A non-functional requirement that is actually an implementation detail: "the
  repository uses `_dbContext.Set<T>().FindAsync()`." That is implementation,
  not a requirement.
- Skipping the traceability table. Without it, downstream skills (and human
  reviewers) cannot see what verifies what.
- Requirements that contradict the plan. If `plan.md` names `ConflictException`,
  requirements.md must name the same exception. If you find a contradiction,
  surface it; do not silently choose.
- Deriving HTTP status codes from memory instead of consulting the
  `CustomExceptionHandler` mapping table in `agent_docs/error_handling.md`.
  Always cross-check there.
