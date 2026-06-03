# PRD — `AssignClientCart` canonical `Result → HTTP` conversion (ADR-002, step 3)

Slice: `0003_assign_client_cart_result_http` · Module: `platform` · Status: 📋 Started

## Problem Statement

As a client developer (and as a service developer reasoning about the error path) of the
BookingManagement service, I cannot trust how an *expected business outcome* of the
"assign the signed-in client to a shopping cart" operation reaches me, because that one
use-case currently runs **both** error models at once and contradicts itself internally:

- The `AssignClientCart` command handler carefully returns a functional `Result` for its
  expected failures ("cart not found" ⇒ `NotFoundError`, "this client already owns a
  different active cart" ⇒ `ConflictError`). The HTTP endpoint then **immediately re-throws
  that `Result` as an exception** (`ConflictError → ConflictException`,
  `NotFoundError → ContentNotFoundException`, anything else → a **bare `Exception`**) so that
  the central `CustomExceptionHandler` can produce the response. The request pays for the
  `Result` mechanism *and* the exception mechanism and gets the benefit of neither — this is
  the exact "bridge" ADR-002 calls out as the main source of the "two unreconciled models"
  problem.
- Worse, the *same* use-case raises one of its conflicts a **third** way: the aggregate
  method `ShoppingCart.AssignClientId` is typed to return `Result` but **throws**
  `ConflictException` on the already-assigned case and never actually returns a failure. So
  the handler's `IsFailure` branch is **dead code**, and "this cart already has an owner"
  surfaces as a thrown exception while "this client already has another active cart"
  surfaces as a `Result` — two identical `409`s produced by two different mechanisms inside
  one command.
- The endpoint's `else` branch throws a **bare `Exception`** for any unmapped `Error`, which
  the project's own checklist forbids and which collapses to an opaque `500`.
- There is a latent functional **bug** on this path: the handler assigns the cart's owner
  from the wrong value (it passes the *shopping-cart id* where the *client id* is expected),
  so a successful assignment records the wrong owner.

ADR-002 ("`Result` for expected outcomes, exceptions for the unexpected") resolves the
error-model question and is explicitly **incremental**. Its **step 3** is "per touched
slice: remove the endpoint `Result → exception` bridge, convert expected-failure paths to
`Result` + `Match`-to-HTTP, replace bare `throw new Exception(...)`". Step 1 (the `Result<T>`
infrastructure, slice `0001`) and step 2 (the `ContentNotFoundException` `204 → 404` contract,
slice `0002`) are already **done**. This slice is the **first** step-3 conversion, deliberately
chosen as the **canonical reference** that every later conversion copies.

## Solution

As a client developer, after this slice the `assign-client-to-cart` operation reports its
expected outcomes through **one** model — `Result` matched straight to HTTP — and stops
contradicting itself:

- The endpoint resolves the handler's `Result` with `Match(onSuccess, onFailure)` where the
  **failure branch returns an HTTP result directly** — it never throws to be re-caught by
  `CustomExceptionHandler`. The `ConflictError → ConflictException` /
  `NotFoundError → ContentNotFoundException` re-throw and the bare `throw new Exception(...)`
  are deleted.
- The failure branch maps each `Error` to its HTTP response through a **single shared
  `Error → IResult` translator** introduced by this slice. It is the `Result`-side analogue
  of `CustomExceptionHandler`: one place that decides the status code and the `ProblemDetails`
  body for a failed `Result`. `NotFoundError ⇒ 404`, `ConflictError ⇒ 409`, any unrecognised
  `Error ⇒ 500` — each with a `ProblemDetails` body **identical in shape** to the one
  `CustomExceptionHandler` already emits for the same status, so a `404`/`409` looks the same
  to a client whether it came from a thrown exception or a matched `Result`.
- The aggregate method `ShoppingCart.AssignClientId` is converted to **return `ConflictError`
  instead of throwing** on the already-assigned case, and to append the
  `ShoppingCartAssignedToClientDomainEvent` **only on the success branch**. This realises the
  one place ADR-002 says `Result` genuinely fits — an in-aggregate state transition that also
  raises a domain event — and makes the handler's `IsFailure` branch live instead of dead.
- The functional **bug is fixed**: a successful assignment records the **signed-in client**
  as the cart's owner (not the cart's own id).
- The OpenAPI surface for this one endpoint is made honest: it declares the statuses it now
  actually returns (`200` / `404` / `409`) and drops the stale ones.

The observable HTTP **status codes are unchanged** by this conversion (success `200`,
not-found `404`, conflict `409` — in and out): it is a *mechanism* swap (exception + bridge ⇒
`Result` + shared mapper). The **only** intentional behaviour change is the owner-assignment
bug fix. That keeps this a tightly-scoped "modify an existing slice" change (update tests,
red, green) rather than a broad contract change.

ADR-002 stays **Proposed**; this slice implements its step 3 for one use-case, per the ADR's
own incremental migration plan. Flipping the ADR to Accepted is reserved to the Decider and is
not part of this slice.

## User Stories

1. As a client developer, I want a request to assign myself to a non-existent cart to return `404 Not Found`, so that a bad cart id is reported as missing rather than as an opaque error.
2. As a client developer, I want a request to assign myself to a cart when I already own a *different* active cart to return `409 Conflict`, so that the conflicting state is unambiguous.
3. As a client developer, I want a request to assign a client to a cart that already has an owner to return `409 Conflict`, so that re-assignment of an owned cart is reported consistently with the other conflict on this path.
4. As a client developer, I want both `409`s on this path produced by the *same* mechanism and carrying the *same* body shape, so that I handle "conflict" uniformly regardless of which rule was violated.
5. As a client developer, I want a `404`/`409` from this endpoint to carry a `ProblemDetails` body (title, type, detail) identical in shape to every other `404`/`409` in the service, so that my client parses one response shape.
6. As a client developer, I want a successful assignment to return `200 OK`, so that the success case is explicit.
7. As a cinema customer, I want being assigned to my cart to actually record *me* as the owner, so that the cart I later read back belongs to me (this fixes the wrong-owner bug).
8. As a service developer, I want the endpoint to resolve the handler's `Result` by matching it straight to an HTTP result, so that the request runs exactly one error pass, not two.
9. As a service developer, I want the `Result → exception` bridge (the `ConflictError`/`NotFoundError` re-throw) on this endpoint removed, so that the canonical reference no longer demonstrates the anti-pattern.
10. As a service developer, I want the bare `throw new Exception(...)` in the endpoint's failure branch removed, so that no unmapped outcome collapses to an opaque, checklist-violating `500`.
11. As a service developer, I want a single shared `Error → IResult` translator that maps `NotFoundError ⇒ 404`, `ConflictError ⇒ 409`, and any unrecognised `Error ⇒ 500`, so that every future step-3 conversion reuses one mapping point.
12. As a service developer, I want that translator to emit `ProblemDetails` bodies matching `CustomExceptionHandler`'s shapes, so that the exception path and the `Result` path are indistinguishable to clients.
13. As a service developer, I want the unknown-`Error` fallback to be `500` (`Results.Problem`), so that an `Error` kind the mapper does not recognise is treated as a programming gap and preserves today's bare-throw behaviour.
14. As a domain developer, I want `ShoppingCart.AssignClientId` to *return* `ConflictError` on the already-assigned case instead of throwing, so that the in-aggregate transition is expressed as a `Result`.
15. As a domain developer, I want `AssignClientId` to append `ShoppingCartAssignedToClientDomainEvent` only on the success branch, so that the event is raised exactly when the state actually changed.
16. As a domain developer, I want the structural guard `Ensure.NotEmpty(clientId, …)` to remain an exception, so that a missing client id (a bug, not a business outcome) is not modelled as a `Result`.
17. As a service developer, I want the handler's now-live `IsFailure` short-circuit to propagate the domain `ConflictError` to the endpoint, so that the domain conflict flows through the same `Match`-to-HTTP path as the handler's own conflict.
18. As an API consumer reading the OpenAPI document, I want the `assign-client` endpoint to declare `200`, `404`, and `409`, so that the published contract matches runtime behaviour.
19. As an API consumer, I want the stale `201`/`204` declarations on that endpoint removed, so that the OpenAPI document is not wrong in the other direction.
20. As a maintainer, I want the externally observable status codes (`200`/`404`/`409`) to stay the same across this conversion, so that the change is low-risk and the only behaviour change is the explicit bug fix.
21. As a reviewer, I want this conversion to follow the spec chain (PRD → plan → requirements → validation → tests → red gate → implementation), so that even a refactor has a traceable rationale and an acceptance gate.
22. As a maintainer, I want the shared `Error → IResult` mapper pinned by a focused unit spec as the acceptance gate, so that the mapping is verified without standing up an HTTP harness.
23. As a maintainer, I want the `AssignClientCart` handler pinned by a unit test (not-found ⇒ `NotFoundError`; other active cart ⇒ `ConflictError`; success ⇒ owner is the client id), so that the conversion and the bug fix are both covered.
24. As a maintainer, I want `ShoppingCart.AssignClientId` pinned by a domain unit test (already-assigned ⇒ returns `ConflictError`, no event; success ⇒ assigns owner and raises the event), so that the throw-to-return change is covered.
25. As a maintainer, I want the architecture tests (`Domain.ArchitectureTests`) and the full suite to stay green, so that the conversion honours the structural rules.
26. As a service developer, I want this slice to be the *only* converted use-case, with `ReserveTickets`/`PurchaseTickets`/`SelectSeats` left for later slices, so that the canonical reference lands small and reviewable.

## Implementation Decisions

**Nature of the slice.** ADR-002 **step 3**, first conversion, chosen as the **canonical
reference**. It is a `ShoppingCarts` use-case conversion (`AssignClientCart`) that *also*
produces a cross-cutting platform artifact (the shared `Error → IResult` mapper) reused by all
later conversions — hence filed under the `platform` module alongside `0001`/`0002` to keep the
ADR-002 series together. Settled in a grill-me interview; runs the full spec chain.

**Use-cases / aggregates touched.**

- **`AssignClientCart` command (use-case, modified).** The command/handler contract stays
  `IRequest<Result>`. Internally: the handler keeps returning `NotFoundError` (cart missing)
  and `ConflictError` (client already owns a different active cart); the now-live `IsFailure`
  branch propagates the domain `Result`; the success path is unchanged except for the bug fix.
- **`ShoppingCart` aggregate — `AssignClientId` (modified).** Returns `ConflictError` instead
  of throwing `ConflictException` on the already-assigned case; appends the domain event only
  on success; `Ensure.NotEmpty` structural guard unchanged.
- **`assign-client` endpoint (modified).** Replaces the `Match`→re-throw bridge with
  `Match(() => Results.Ok(), failure => <shared mapper>(failure))`. `.Produces` corrected to
  `200`/`404`/`409`.
- **Shared `Error → IResult` translator (new, deep module).** A small, framework-thin mapping
  from `Error` to an HTTP `IResult` + `ProblemDetails`, living in the API endpoints' common
  area. Simple, stable, testable interface (one input `Error`, one output `IResult`); it
  encapsulates the entire `Result`-to-HTTP policy behind one call. No new `Error` types are
  introduced; `Error` definitions stay centralised in `Domain/Error`.

**Mapping policy (the mapper).** `NotFoundError ⇒ 404`, `ConflictError ⇒ 409`, unrecognised
`Error ⇒ 500` via `Results.Problem`. Each `ProblemDetails` mirrors the title/type/detail shape
`CustomExceptionHandler` already emits for that status, so the exception path and the `Result`
path are byte-compatible for clients.

**Bug fix.** A successful assignment records the **signed-in client id** as the cart owner.
Pinned by the handler unit test (user stories 7, 23).

**Status-preservation.** The conversion is mechanism-only at the HTTP layer: success `200`,
not-found `404`, conflict `409` are unchanged in and out. The single intentional behaviour
change is the bug fix. (The not-found contract is already `404` after slice `0002`.)

**ADR status.** ADR-002 stays **Proposed**; this slice implements its step 3 for one use-case.

**Explicitly deferred (NOT in this slice).**
- Converting `ReserveTickets` / `PurchaseTickets` (which currently leak a serialized `Result`
  and would exercise `Result<T>`) and `SelectSeats` (which collapses every failure to `400`) —
  later slices that reuse this slice's mapper.
- Deduplicating the two not-found exception *types* (`NotFoundException` vs
  `ContentNotFoundException`, now byte-identical `404`s) and relocating the misplaced
  `ContentNotFoundException` file (Domain project under an Application namespace) — its own
  ADR-gated slice.
- Replacing the bare `throw new Exception(...)` in `CreateMovieSessionCommandHandler`,
  `ReserveTicketsCommandHandler`, `MovieSessionSeatService` — fixed by whichever slice converts
  those use-cases.
- The bare `throw` in the endpoints' `GetClientId` helper (a malformed-identity auth concern,
  not part of this error-model conversion).
- Standing up a `WebApplicationFactory` HTTP harness.

## Testing Decisions

**What makes a good test here.** The externally observable behaviour is the HTTP status and
`ProblemDetails` body for each outcome of this use-case, plus the domain transition's outcome
and event. Because the repository has **no integration-test project**
(`WebApplicationFactory<Program>` is not established; the suites are `Domain.UnitTests`,
`Domain.ArchitectureTests`, `Infrastructure.UnitTests`, `Application.LoadTests`, and the
`API.UnitTests` project created by slice `0002`), the change is pinned with **focused unit
tests of the changed units**, consistent with how `0001` and `0002` closed. Tests assert
external behaviour (status, body shape, returned `Result`, raised event), not internal wiring.

**Units under test.**
- **Shared `Error → IResult` mapper (the acceptance gate / RED gate):** `NotFoundError ⇒ 404`
  + `ProblemDetails`; `ConflictError ⇒ 409` + `ProblemDetails`; an unrecognised `Error ⇒ 500`.
  Lives in `BookingManagementService.API.UnitTests`. This is the direct analogue of slice
  `0002`'s `CustomExceptionHandler` gate and is **RED** until the mapper exists.
- **`AssignClientCart` handler:** cart missing ⇒ returns `NotFoundError`; client already owns a
  *different* active cart ⇒ returns `ConflictError`; success ⇒ `Result.Success()` and the cart
  owner equals the **client id** (pins the bug fix); the domain `IsFailure` is propagated.
- **`ShoppingCart.AssignClientId` (domain):** already-assigned cart ⇒ returns `ConflictError`
  and raises **no** domain event; success ⇒ assigns the owner and raises
  `ShoppingCartAssignedToClientDomainEvent`.

**Prior art.** Slice `0002`'s `CustomExceptionHandlerContentNotFound404OutsideInTests`
(`DefaultHttpContext`, `ProblemDetails` body assertions) for the mapper gate;
`BookingManagementService.Domain.UnitTests` (`ShoppingCarts/ShoppingCartSpecification.cs`,
`Error/ResultOfTSpecification.cs`) for the AAA / `*Specification` domain conventions; existing
mocked-repository application handler tests (NSubstitute) for the handler test.

**Out of the net (by decision):** no `WebApplicationFactory` end-to-end test (no harness exists;
standing one up for one endpoint is disproportionate — the endpoint's `Match` wiring is covered
by compilation + the mapper gate); no tests for the use-cases left out of scope.

## Out of Scope

- Converting any use-case other than `AssignClientCart` (`ReserveTickets`, `PurchaseTickets`,
  `SelectSeats` — later slices that reuse this slice's mapper).
- Adopting `Result<T>` (the generic) in any handler — `AssignClientCart` uses the non-generic
  `Result`; the generic is exercised by a later seats-command conversion.
- Deduplicating `NotFoundException` and `ContentNotFoundException`, or relocating the misplaced
  `ContentNotFoundException` file.
- Replacing the bare `throw new Exception(...)` in `CreateMovieSessionCommandHandler`,
  `ReserveTicketsCommandHandler`, `MovieSessionSeatService`, or the `GetClientId` endpoint helper.
- Standing up a `WebApplicationFactory` HTTP integration harness.
- Changing the `CustomExceptionHandler` mechanism, the MediatR pipeline, the validation
  behaviour, or any base type.
- Flipping ADR-002 to Accepted.
- Publishing this PRD to a remote issue tracker — `gh` is not installed and no triage-label
  vocabulary was provided, so the `needs-triage` step could not run; the PRD is stored locally
  instead (same as slices `0001`/`0002`).

## Further Notes

- The diff is small but load-bearing: this is the **template** for every remaining ADR-002
  step-3 conversion, so its shape (handler returns `Result`; aggregate transition returns
  `Result` and raises its event on success; endpoint `Match`es straight to HTTP via the shared
  mapper; no bridge; no bare `throw`) matters more than its size.
- The shared `Error → IResult` mapper is deliberately a **deep module**: a one-method interface
  hiding the whole `Result`-to-HTTP policy, mirroring how `CustomExceptionHandler` hides the
  exception-to-HTTP policy. Keeping both policies each in exactly one place is the structural
  win of this slice.
- Status-preservation (`200`/`404`/`409` unchanged) means the "modifying an existing slice"
  red→green is driven by the new mapper gate and the bug-fix assertion, not by a contract flip.
- Verification per `CLAUDE.md`: `dotnet format`, `dotnet build -warnaserror`, `dotnet test`.
  As recorded in MEMORY (`dotnet10-migration`), the build trips the accepted AutoMapper
  `NU1903` NuGet-audit advisory under `-warnaserror` at restore time; handle NuGet audit
  accordingly so the real build/warnings are what is validated.
