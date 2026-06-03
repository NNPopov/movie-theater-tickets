# PRD — Error-model `Result<T>` infrastructure (ADR-002, step 1)

Slice: `0001_error_model_result_infrastructure` · Module: `platform` · Status: 📋 Started

## Problem Statement

As a developer on the BookingManagement service, I cannot reason about the error model
with confidence, because the codebase deliberately runs **two** error models at once —
typed exceptions translated by `CustomExceptionHandler`, and a functional
`Result`/`Error` monad — and they are wired through each other. The pain is concrete:

- The functional `Result` type can only express success/failure, **not a success that
  carries a value**, so when a booking operation needs to return data on success the
  value has to travel out-of-band. Half the point of the pattern is missing.
- The shared `DomainErrors<T>` helper emits the **wrong error code for every aggregate**:
  it interpolates `nameof(T)`, which on a generic type parameter evaluates to the literal
  string `"T"`. Every seat/cart/session error code is `"T.NotFound"`, `"T.ConflictException"`,
  etc., regardless of the aggregate that raised it.
- The error vocabulary contains a **typo in a public type name** (`NotFountError`), which
  any new code must either copy or work around.

ADR-002 ("Result for expected outcomes, exceptions for the unexpected") resolves the
larger model question, but it is explicitly **incremental**. Its **step 1** is a small,
low-risk infrastructure foundation that everything else depends on. This slice is exactly
that step 1 — and nothing more.

This slice does **not** change any HTTP behavior, does not remove the endpoint
`Result → exception` bridge, and does not touch the 204→404 contract question. Those are
later, per-slice, behavior-changing steps in the ADR's migration plan.

## Solution

As a developer, after this slice the `Domain/Error/` shared kernel gives me a correct,
lightweight foundation to build on, with **zero change to runtime HTTP behavior**:

- A new generic **`Result<T>`** exists alongside the current non-generic `Result`, so a
  successful outcome can carry its value. It is intentionally lightweight (status / value /
  error / `Match`) with **no monadic `Bind`/`Map`/LINQ**, per ADR-002 and Andrew Lock's
  caveat. `Result<T>` is a subtype of `Result`, so it is usable anywhere a `Result` is
  expected, and a failed `Result<T>` throws if its value is accessed (fail-fast on misuse).
- `DomainErrors<T>` emits **correct, aggregate-specific error codes**
  (`"MovieSessionSeat.NotFound"`, `"ShoppingCart.NotFound"`, …) by using `typeof(T).Name`
  instead of `nameof(T)`.
- The mistyped public error type `NotFountError` is renamed to **`NotFoundError`**, and the
  single call site that pattern-matches it is updated.

Nothing yet **uses** `Result<T>` — it is introduced, not adopted. No existing `Result`
call site is migrated to it in this slice; that adoption happens later, per-slice, only
where a success genuinely needs to carry a value (ADR-002 steps 3–4). The full test suite,
including the architecture tests, stays green throughout.

## User Stories

1. As a service developer, I want a generic `Result<T>` that carries a value on success, so that an operation can return both "it succeeded" and the produced data through one type.
2. As a service developer, I want `Result<T>` to be a subtype of the existing `Result`, so that I can pass it anywhere a non-generic `Result` is accepted and reuse the shared `IsSuccess`/`IsFailure`/`Match` plumbing.
3. As a service developer, I want accessing `Value` on a failed `Result<T>` to throw immediately, so that a programming error surfaces at the point of misuse rather than propagating a default/null.
4. As a service developer, I want a value to implicitly convert into a successful `Result<T>` and an `Error` to implicitly convert into a failed `Result<T>`, so that handler/domain code reads naturally without explicit factory calls.
5. As a service developer, I want a `Match` overload whose success branch receives the carried value, so that I can fold a `Result<T>` into an outcome without first unpacking `Value`.
6. As a service developer, I want `Result<T>` kept lightweight (no `Bind`/`Map`/LINQ query syntax), so that the code stays idiomatic C# and free of functional-purist boilerplate, per ADR-002.
7. As a service developer, I want `DomainErrors<T>` to produce aggregate-specific error codes (e.g. `MovieSessionSeat.NotFound`), so that an error code identifies which aggregate raised it.
8. As an operator reading logs, I want error codes that name the real aggregate instead of the literal `"T"`, so that I can tell a seat conflict from a cart conflict from the code alone.
9. As a service developer, I want the misspelled `NotFountError` type renamed to `NotFoundError`, so that the error vocabulary is correct and new code does not propagate the typo.
10. As a service developer, I want the one existing call site that matches `NotFountError` updated to the corrected name, so that the project keeps compiling after the rename.
11. As a service developer, I want a focused unit specification for `Result<T>`, so that the success-carries-value, value-throws-on-failure, implicit-conversion, and `Match`-branch decisions are pinned against accidental regression.
12. As a service developer, I want this change to make no HTTP/behavioral difference, so that no endpoint, client contract, or integration test needs to change as part of step 1.
13. As a service developer, I want the existing non-generic `Result` behavior to remain identical, so that the six seat operations and the cart commands that already return `Result` are completely unaffected.
14. As a service developer, I want the endpoint `Result → exception` bridge left in place for now, so that step 1 carries no behavior-changing risk and can land as one green commit.
15. As a reviewer, I want the slice to follow the spec chain (PRD → plan → requirements → validation → tests → red gate → implementation), so that even an infrastructure change has a traceable rationale and an acceptance gate.
16. As a maintainer, I want the architecture tests (`Domain.ArchitectureTests`) to stay green, so that `Result<T>` honors the framework-free `Domain` rule and the structural invariants.

## Implementation Decisions

**Nature of the slice.** This is a **cross-cutting `Domain/Error` shared-kernel** change.
It has **no MediatR use-case** (no command/query handler), **no aggregate root** added or
modified, and **no HTTP entry point**. It is registered under the `platform` module by
convention (mirroring the cross-cutting precedent in the `src/clients` project). It is
flagged as ADR territory by `spec_workflow.md` because it changes a base type (`Result`)
and the cross-cutting `Error` vocabulary — hence it goes through the full chain rather
than being treated as a slice detail.

**`Result<T>` shape (agreed in the grill-me interview).**
- `Result<T>` derives from the existing `Result` (inheritance). This requires changing the
  base `Result` constructor from `private` to `protected` — a one-line change to a
  stable-ish base type, accepted deliberately.
- Surface: `IsSuccess` / `IsFailure` / `Error` (inherited), plus `Value`, `Success(value)`,
  `Failure(error)`, an implicit conversion from `TValue`, and an implicit conversion from
  `Error`.
- `Value` on a failed result throws `InvalidOperationException`.
- A generic `Match<TValue, TOut>(onSuccess: value → out, onFailure: error → out)` extension
  sits alongside the existing non-generic `Match`. Overload resolution is unambiguous
  because the success delegate arities differ (no-arg vs one-arg).
- Explicitly **no** `Bind`/`Map`/LINQ combinators.

**`DomainErrors<T>` fix.** Replace `nameof(T)` with `typeof(T).Name` in all four factory
methods (`ConflictException`, `NotFound`, `DomainValidation`, `InvalidOperation`). This
changes the runtime error-code strings at the existing call sites (seat domain, assign-cart
handler, purchase handler) from `"T.*"` to the real aggregate/type name. No call site code
changes; only the emitted string changes.

**`NotFountError` → `NotFoundError` rename.** Rename the `sealed record` in the error
vocabulary and update the single pattern-match call site in the ShoppingCart endpoint. The
endpoint's bridge logic itself is **not** otherwise modified in this slice.

**Defects deliberately deferred (NOT in this slice).** Per the ADR's migration plan these
belong to later per-slice steps, not step 1:
- The bare `throw new Exception(...)` in `MovieSessionSeatService` / the create-session
  handler (ADR defect #2) — replaced during the relevant slice's conversion.
- The `ContentNotFoundException` 204 → 404 mapping (ADR defect #4) — a client-contract
  change requiring Flutter coordination.
- Removing the endpoint `Result → exception` bridge and converting expected-failure paths
  to `Result<T>` + `Match`-to-HTTP (ADR steps 3–4).

**ADR status.** ADR-002 stays **Proposed**; `agent_docs/error_handling.md` is not updated
in this slice (the ADR says that file is updated once the ADR is accepted, which is a
separate decision reserved to the Decider).

**Defect already resolved.** ADR defect #3 (duplicate `ContentNotFoundException` in both
`Application.Exceptions` and `Domain.Exceptions`) is **not present in the current code** —
`ContentNotFoundException` is defined once, in `Domain.Exceptions`. No action needed; the
ADR text is out of date on this point.

## Testing Decisions

**What makes a good test here.** The only new externally observable behavior is the public
contract of `Result<T>`: that success carries the value, that failure exposes the error and
forbids `Value` access, that the implicit conversions land on the right branch, and that
`Match` invokes the correct branch with the correct argument. The test asserts that
contract through the public surface only — it does not assert internal field layout. The
two fixes (`typeof(T)`, the rename) are **not** given dedicated tests: the rename is
verified by compilation, and the corrected error codes are not asserted by any existing
test (confirmed by inspection), so changing them is safe and a new code-string assertion
would only pin a value that no consumer depends on.

**Unit under test.** `Result<T>` — a small xUnit + FluentAssertions specification covering:
success carries the value; failure is a failure and exposes the error; accessing `Value`
on failure throws; implicit conversion from a value yields success; implicit conversion
from an `Error` yields failure; `Match` runs the success branch with the value; `Match`
runs the failure branch with the error. (~7 facts.)

**Acceptance gate.** Because this slice has no HTTP entry point, there is no
`WebApplicationFactory`-based outside-in endpoint test. The acceptance gate is the
`Result<T>` unit specification going green plus the **full suite (including the
architecture tests) staying green**. The `/slice-test-red` step produces this unit spec as
the RED gate (it will not compile / will fail until `Result<T>` exists), and implementation
turns it green.

**Prior art.** `BookingManagementService.Domain.UnitTests` — e.g.
`ShoppingCarts/ShoppingCartSpecification.cs` — is the reference for domain-level xUnit +
FluentAssertions specs (AAA layout, `[Fact]`, `*Specification` naming).

**Out of the net (by decision):** no test for the changed `DomainErrors<T>` code strings,
no endpoint/integration test, no handler test — there is no handler or endpoint in this
slice.

## Out of Scope

- Removing the endpoint `Result → exception` bridge (`AssignClientCart`, `SelectSeats`,
  `ReserveSeats`) and converting any endpoint to `Match`-to-HTTP. (ADR steps 3–4.)
- The `ContentNotFoundException` 204 → 404 contract change and any Flutter-client
  coordination. (ADR step 2 / defect #4.)
- Replacing bare `throw new Exception(...)` in the domain/handlers. (ADR defect #2,
  per-slice.)
- Migrating any existing `Result`-returning method to `Result<T>`; no current call site is
  converted in this slice.
- Unifying the two error models, changing `CustomExceptionHandler`, or altering the MediatR
  pipeline.
- Flipping ADR-002 to Accepted or updating `agent_docs/error_handling.md`.
- Publishing this PRD to a remote issue tracker — `gh` is not installed and no
  triage-label vocabulary was provided, so the `needs-triage` step could not run; the PRD
  is stored locally instead.

## Further Notes

- The change is small but sits in the `Domain/Error` shared kernel, which is why it is an
  ADR-gated, full-chain slice rather than an ad-hoc edit. An earlier attempt in this
  conversation to implement it directly (during the grill-me interview, with no PRD/tests)
  was reverted; this PRD restarts it correctly.
- Risk is low and bounded: the only runtime-visible change is the **error-code strings**
  emitted by `DomainErrors<T>`, which no existing test or HTTP response surfaces. `Result`'s
  existing behavior and all current `Result` call sites are unchanged.
- The "domain-event-on-the-success-branch" fit that motivated `Result` inside aggregate
  methods (e.g. `MovieSessionSeat.Select/Reserve/Sell`) is preserved as-is and is the
  reason `Result` remains the model for in-aggregate state transitions.
- Verification per `CLAUDE.md`: `dotnet format`, `dotnet build -warnaserror`, `dotnet test`.
  Note the build currently trips the **accepted AutoMapper `NU1903`** audit advisory under
  `-warnaserror` at restore time (a known, accepted project constraint), so the suite is
  validated with NuGet audit handled accordingly.
