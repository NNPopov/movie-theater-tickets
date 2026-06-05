# PRD — Content-not-found `204 → 404` contract (ADR-002, step 2)

Slice: `0002_content_not_found_404` · Module: `platform` · Status: 📋 Started

## Problem Statement

As a client developer (and as an operator reasoning about the HTTP contract) of the
BookingManagement service, I cannot trust "not found" to mean what HTTP says it means,
because the service maps `ContentNotFoundException` to **`204 No Content`** instead of
`404 Not Found`. The pain is concrete:

- A request for a resource that genuinely does not exist (a movie by id, a movie session
  by id, a shopping cart by id) comes back as `204` — a **success with no body**. A client
  cannot distinguish "the thing you asked for is missing" from "the operation succeeded and
  there is simply nothing to return". The Flutter client has had to special-case the
  status code `204` as "not found" in several layers to compensate.
- The `204` is produced in **one** central place — `CustomExceptionHandler` — but it fans
  out to **every** read path that throws `ContentNotFoundException` (movies, movie sessions,
  seats, shopping carts). The wrong contract is therefore uniform and load-bearing.
- The same blunt mapping conflates two genuinely different situations under one status:
  an **addressed resource that is absent** (correctly `404`) and a **normal empty state** —
  "this customer has no active shopping cart yet", "this movie has no upcoming sessions yet".
  Treating an empty state as "not found" is itself a contract smell, regardless of the
  status code chosen.

ADR-002 ("`Result` for expected outcomes, exceptions for the unexpected") resolves the
larger error-model question and is explicitly **incremental**. Its **step 2** is the
"decide and apply the `204 → 404` contract change for `ContentNotFoundException`" item.
This slice is exactly that step 2 — a server-side contract correction — and nothing more.
Step 1 (the `Result<T>` infrastructure, slice `0001`) is already done.

This slice does **not** remove the endpoint `Result → exception` bridge, does **not**
introduce or adopt `Result<T>` in any handler, does **not** deduplicate the two not-found
exception types, and does **not** modify the Flutter client. Those are later, separately
tracked steps.

## Solution

As a client developer, after this slice "not found" behaves the way HTTP promises, and an
empty state is no longer disguised as "not found":

- A request for an **addressed resource that does not exist** returns **`404 Not Found`**
  with a proper `ProblemDetails` body (title, type, and a human-readable detail), identical
  in shape to the `404` already produced by `NotFoundException`. This is achieved by a
  single change at the central translation point (`CustomExceptionHandler`): the
  `ContentNotFoundException` writer now emits `404` + `ProblemDetails` instead of an empty
  `204`.
- The two **genuine empty-state** read paths are taken **out from under** the not-found
  mapping, by the principle *"an addressed resource that is absent ⇒ `404`; an empty
  collection or a normal absent-but-expected state ⇒ not `404`"*:
  - **"No current shopping cart for this customer"** returns **`204 No Content`** (a real
    "nothing to return" success), not `404`. The query handler returns an empty result for
    the no-active-cart case; the endpoint maps that to `204`. The *inconsistent* case (an
    active-cart id exists but its record is missing) keeps throwing `ContentNotFoundException`
    and so becomes `404`.
  - **"No upcoming movie sessions for this movie"** returns **`200` with an empty list**,
    not `404`. The query handler returns the empty collection instead of throwing.

- The HTTP `ProblemDetails`/OpenAPI surface is made honest: `.Produces(404)` is declared on
  the addressed-resource read paths that can now answer `404`; the genuinely-empty paths keep
  their `204`/`200` declarations. Endpoints where `204` legitimately means "no body on
  success" are left untouched.

- The project's own documentation stops teaching the old contract: `agent_docs/error_handling.md`
  and the spec-chain skills that hard-code "`ContentNotFoundException → 204`" are updated to
  "`→ 404`", so future slices do not regenerate the stale mapping.

ADR-002 stays **Proposed**; `agent_docs/error_handling.md`'s mapping table is corrected (it
is the canonical reference and would otherwise actively mislead), but flipping the ADR to
Accepted is reserved to the Decider and is not part of this slice.

## User Stories

1. As a client developer, I want a request for a non-existent movie (by id) to return `404 Not Found`, so that I can distinguish a missing resource from an empty success.
2. As a client developer, I want a request for a non-existent movie session (by id) to return `404`, so that "this showtime does not exist" is unambiguous.
3. As a client developer, I want a request for a non-existent shopping cart (by id) to return `404`, so that a bad cart id is reported as missing, not as success.
4. As a client developer, I want a `404` response to carry a `ProblemDetails` body (title, type, detail), so that I have a machine-readable and human-readable reason, consistent with every other `404` in the service.
5. As a client developer, I want the `404` produced by `ContentNotFoundException` to be indistinguishable in shape from the one produced by `NotFoundException`, so that I can handle "not found" uniformly regardless of which internal exception was raised.
6. As an operator, I want "not found" to be logged and returned as `404` rather than silently completed as `204`, so that absent-resource access is visible and correctly categorised.
7. As a cinema customer using the app, I want "I have no active shopping cart yet" to be a normal empty state (`204 No Content`), so that opening the app without a cart is not treated as an error.
8. As a client developer, I want `GET /shoppingcarts/current` to return `204` when the customer has no active cart, so that the existing empty-cart UX keeps working after the not-found contract changes.
9. As a client developer, I want `GET /shoppingcarts/current` to return `404` only in the genuinely inconsistent case (an active-cart id exists but its record is gone), so that a data inconsistency is surfaced rather than hidden.
10. As a cinema customer, I want a movie with no upcoming sessions to show an empty list (`200` with `[]`), so that "no sessions scheduled" reads as empty, not as "movie not found".
11. As a client developer, I want `GET` of a movie's sessions to return `200` with an empty array when there are none, so that I can render an empty list without treating it as an error.
12. As a service developer, I want the `204 → 404` change made at the single central point (`CustomExceptionHandler`), so that the contract is corrected uniformly and there is exactly one place that decides the status.
13. As a service developer, I want the addressed-resource read paths (movie by id, movie session by id, shopping cart by id, the seat service, and the cart commands that load a cart/session by id) to flip to `404` automatically via the central change, so that no per-handler edits are needed for the genuinely-not-found cases.
14. As a service developer, I want the empty-state paths (`current` cart, movie sessions list) explicitly carved out of the not-found mapping, so that the central flip does not silently turn a normal empty state into a `404`.
15. As an API consumer reading the OpenAPI document, I want `.Produces(404)` declared on the read paths that can now return `404`, so that the published contract matches runtime behaviour.
16. As an API consumer, I want endpoints where `204` legitimately means "success, no body" to keep their `204` declaration, so that the OpenAPI document is not made wrong in the other direction.
17. As a service developer, I want `agent_docs/error_handling.md` to state `ContentNotFoundException → 404`, so that the canonical reference is correct.
18. As a service developer using the spec-chain skills, I want the skills that hard-code "`ContentNotFoundException → 204`" updated to `404`, so that newly generated slices do not reintroduce the stale contract.
19. As a service developer, I want the existing `NotFoundException → 404` mapping left unchanged, so that the corrected `ContentNotFoundException` simply joins it rather than altering it.
20. As a reviewer, I want this change to follow the spec chain (PRD → plan → requirements → validation → tests → red gate → implementation), so that even a contract correction has a traceable rationale and an acceptance gate.
21. As a maintainer, I want the architecture tests (`Domain.ArchitectureTests`) and the full suite to stay green, so that the contract change honours the structural rules.
22. As a service developer, I want the endpoint `Result → exception` bridge in `assignclient` left in place for this slice, so that step 2 carries no extra behaviour-change risk; I accept that its not-found path changes `204 → 404` as a side effect of the central flip.
23. As a maintainer, I want the Flutter client update tracked as a separate task, so that the server contract can land independently and the client follows deliberately.

## Implementation Decisions

**Nature of the slice.** This is a **cross-cutting HTTP-contract correction** at stable
infrastructure (`CustomExceptionHandler`), plus two small, deliberate handler/endpoint
behaviour carve-outs. It is ADR-gated (ADR-002 step 2) and runs the full spec chain under
the `platform` module, mirroring slice `0001`.

**The central flip (the core change).** `CustomExceptionHandler`'s `ContentNotFoundException`
writer changes from "set `204`, complete with no body" to "set `404`, write a
`ProblemDetails`" — title, `type` (`rfc7231 §6.5.4`), and `detail` from the exception
message — matching the existing `NotFoundException` writer's shape. This is the single point
that flips every addressed-resource not-found path to `404`.

**Principle for what becomes `404` vs not.** *Addressed resource absent ⇒ `404`; empty
collection or normal absent-but-expected state ⇒ not `404`.* Applied:

- **Genuine `404` (no handler edits; flips via the central change):** get movie by id, get
  movie session by id, get shopping cart by id, the seat service's missing-session lookups,
  and the shopping-cart commands that load a cart/session by id (select, unselect, reserve,
  expired). These already throw `ContentNotFoundException` for an addressed-but-absent
  resource and are correct under the new mapping.
- **Carve-out A — current cart (empty state ⇒ `204`):** the get-current-cart query handler
  returns an **empty/`null` result** when the customer has no active cart (instead of
  throwing), and the endpoint maps that to `204 No Content`. The query response is made
  nullable to express "no current cart". The **inconsistent** branch (an active-cart id is
  recorded but the cart record is missing) **keeps throwing `ContentNotFoundException`** and
  therefore returns `404`.
- **Carve-out B — movie sessions list (empty list ⇒ `200 []`):** the get-movie-sessions
  query handler returns the **empty collection** when there are no upcoming sessions, instead
  of throwing. The endpoint returns `200` with `[]`.

**OpenAPI declarations.** Add `.Produces(404)` to the addressed-resource read paths that can
now answer `404`. Keep `204` on `current` (empty state) and `200` on the movie-sessions list.
Each `.Produces(204)` is reviewed individually; declarations where `204` legitimately means
"no body on success" are left as-is — only not-found declarations are corrected.

**Documentation.** Update `agent_docs/error_handling.md`'s mapping table
(`ContentNotFoundException → 404`) and the spec-chain skills that encode the old contract
(`feature-tests`, `slice-test-red`, `feature-validation`, `feature-requirements`,
`spec-workflow`), removing the "`→ 204`, not `404`" guidance.

**Explicitly deferred (NOT in this slice).**
- Removing the endpoint `Result → exception` bridge (`assignclient`) and converting
  expected-failure paths to `Result<T>` + `Match`-to-HTTP — ADR step 3.
- Adopting `Result<T>` in any handler — ADR step 3.
- Deduplicating `NotFoundException` and `ContentNotFoundException` (which will now produce
  byte-identical `404`s) — a separate exception-vocabulary decision (ADR territory).
- Replacing bare `throw new Exception(...)` in domain/handlers — ADR defect #2, per-slice.
- Updating the Flutter client (`shopping_cart_repo_impl`, `get_shopping_cart`, the cubit) to
  treat the by-id cart not-found as `404` — a separate, tracked client task.

**Accepted side effect.** The `assignclient` bridge re-throws `NotFoundError` as
`ContentNotFoundException`; via the central flip its not-found path returns `404` instead of
`204`. This is an improvement, not a regression, and is accepted.

**ADR status.** ADR-002 stays **Proposed**. `agent_docs/error_handling.md`'s mapping table is
corrected because it is the canonical reference; flipping the ADR to Accepted is reserved to
the Decider.

## Testing Decisions

**What makes a good test here.** The externally observable behaviour is the HTTP status (and,
for the central flip, the response body shape) for "not found" and the two empty-state paths.
Because the repository has **no integration-test project** (`WebApplicationFactory<Program>`
is not established; the suites are `Domain.UnitTests`, `Domain.ArchitectureTests`,
`Infrastructure.UnitTests`, `Application.LoadTests`), the change is pinned with **focused unit
tests of the changed units** — proportional to a one-method central change plus two handler
carve-outs, and consistent with how slice `0001` closed (a Domain unit spec, no HTTP harness).

**Units under test.**
- **`CustomExceptionHandler` (the acceptance gate / RED gate):** given a
  `ContentNotFoundException` and a fake `HttpContext`, the handler sets status `404` and writes
  a `ProblemDetails`. Regression: `NotFoundException` still maps to `404`. (xUnit +
  FluentAssertions, `DefaultHttpContext`.)
- **Get-current-cart query handler:** no active cart ⇒ returns the empty/`null` result (does
  not throw); active-cart id present but record missing ⇒ throws `ContentNotFoundException`.
- **Get-movie-sessions query handler:** no upcoming sessions ⇒ returns an empty collection
  (does not throw); sessions present ⇒ returns the mapped collection.

**Prior art.** `BookingManagementService.Domain.UnitTests` (e.g.
`Error/ResultOfTSpecification.cs`, `ShoppingCarts/ShoppingCartSpecification.cs`) for the AAA /
`[Fact]` / `*Specification` conventions; existing application handler unit tests for the
mocked-repository handler style.

**Out of the net (by decision):** no `WebApplicationFactory` end-to-end test (no harness
exists and standing one up is disproportionate for this change); no test for the unchanged
addressed-resource handlers that flip purely via the central mapping (their behaviour is
covered by the `CustomExceptionHandler` test).

## Out of Scope

- Removing the `assignclient` endpoint `Result → exception` bridge or converting any endpoint
  to `Match`-to-HTTP (ADR step 3).
- Adopting `Result<T>` in any handler (ADR step 3).
- Deduplicating `NotFoundException` and `ContentNotFoundException` into a single not-found type.
- Replacing bare `throw new Exception(...)` in the domain/handlers (ADR defect #2).
- Updating the Flutter client to handle the by-id cart not-found as `404` (separate tracked
  task). After the carve-outs, the client breaks only at `GET /shoppingcarts/{id}` and the
  `assignclient` bridge path; `current` keeps `204`, so its empty-cart UX is unaffected.
- Flipping ADR-002 to Accepted.
- Changing the `CustomExceptionHandler` mechanism itself, the MediatR pipeline, or the
  validation behaviour.
- Publishing this PRD to a remote issue tracker — `gh` is not installed and no triage-label
  vocabulary was provided, so the `needs-triage` step could not run; the PRD is stored locally
  instead.

## Further Notes

- The change is small at the centre (one writer method) but the *contract* blast radius is
  every not-found read path; that is exactly why it is an ADR-gated, full-chain slice rather
  than an ad-hoc edit. The two carve-outs (`current`, movie sessions list) are what keep the
  blunt central flip from silently turning normal empty states into `404`s.
- The grill-me interview settled: scope is **step 2 only**; the flip is **global at the
  server** with the client deferred; the `404` body is a **full `ProblemDetails`**; the gate
  is a **`CustomExceptionHandler` unit test** (no `WebApplicationFactory`); docs **and**
  skills are updated; `.Produces` corrected for not-found paths; the two not-found exception
  types are **left un-deduplicated**; and ADR-002 stays **Proposed**.
- Verification per `CLAUDE.md`: `dotnet format`, `dotnet build -warnaserror`, `dotnet test`.
  As recorded in MEMORY (`dotnet10-migration`), the build trips the accepted AutoMapper
  `NU1903` NuGet-audit advisory under `-warnaserror` at restore time; handle NuGet audit
  accordingly so the real build/warnings are what is validated.
