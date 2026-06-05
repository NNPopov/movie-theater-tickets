# 0001 · ErrorModelResultInfrastructure — Requirements

## Inputs

- PRD: ./prd.md
- Plan: ./plan.md

> **Note on shape.** This slice is a cross-cutting `Domain/Error` shared-kernel change with
> **no HTTP endpoint, no MediatR use-case, no validator, and no repository.** The functional
> requirements therefore describe the public contract of the `Result<T>` type and the two
> vocabulary fixes, not request/response/status behavior. The non-functional list is the
> project-wide set, with items that presuppose a handler/endpoint/repository marked **N/A
> (no such component in this slice)** rather than dropped, to keep the list stable across
> slices.

## Functional requirements

- **F1.** A generic type `CinemaTicketBooking.Domain.Error.Result<TValue>` exists in
  `Domain/Error/` alongside the non-generic `Result`, and derives from `Result` (`Result<TValue> : Result`),
  so a `Result<TValue>` is usable anywhere a `Result` is accepted. (PRD US1–2; plan §5 step 2.)
- **F2.** `Result<TValue>.Success(value)` produces a result for which `IsSuccess` is `true`,
  `IsFailure` is `false`, and `Value` equals the supplied value. (PRD US1; plan §5 step 2.)
- **F3.** `Result<TValue>.Failure(error)` produces a result for which `IsFailure` is `true`,
  `IsSuccess` is `false`, and `Error` equals the supplied `Error`. (PRD US3; plan §5 step 2.)
- **F4.** Reading `Value` on a failed `Result<TValue>` throws `InvalidOperationException`
  (fail-fast on misuse), rather than returning `default`/`null`. (PRD US3; plan §5 step 2.)
- **F5.** An implicit conversion from `TValue` to `Result<TValue>` yields a success carrying
  that value. (PRD US4; plan §5 step 2.)
- **F6.** An implicit conversion from `Error` to `Result<TValue>` yields a failure carrying
  that error, and this conversion does not regress the existing `Error → Result` implicit
  conversion on the base type. (PRD US4; plan §5 step 2.)
- **F7.** A generic extension `Match<TValue, TOut>(this Result<TValue>, Func<TValue, TOut> onSuccess, Func<Error, TOut> onFailure)`
  exists in `ResultExtensions`, invokes `onSuccess` with the carried value when the result is a
  success and `onFailure` with the `Error` when it is a failure, and coexists unambiguously with
  the existing non-generic `Match` (resolution distinguished by success-delegate arity). (PRD US5; plan §5 step 3.)
- **F8.** `Result<TValue>` exposes **no** `Bind`, `Map`, or LINQ query-syntax combinators; its
  surface is limited to `IsSuccess`/`IsFailure`/`Error` (inherited), `Value`, `Success`,
  `Failure`, the two implicit conversions, and the `Match` overload. (PRD US6; plan §5 step 2.)
- **F9.** The base `Result` constructor visibility changes from `private` to `protected` to
  permit inheritance, and **no other behavior of the non-generic `Result` changes** — the
  error/success invariant guard, `IsSuccess`/`IsFailure`/`Error`, and `Success()`/`Failure(error)`
  remain identical. (PRD US13; plan §5 step 1.)
- **F10.** All four `DomainErrors<T>` factories (`ConflictException`, `NotFound`,
  `DomainValidation`, `InvalidOperation`) build their error code from `typeof(T).Name` instead
  of `nameof(T)`, so the emitted code names the real aggregate/type (e.g.
  `"MovieSessionSeat.ConflictException"`, `"ShoppingCart.NotFound"`) rather than the literal
  `"T.*"`. (PRD US7–8; plan §5 step 4.)
- **F11.** The public error record `NotFountError` is renamed to `NotFoundError` (same
  signature, still `sealed record … : Error`), and the `DomainErrors<T>.NotFound` factory
  constructs `NotFoundError`. (PRD US9; plan §5 steps 4–5.)
- **F12.** The single pattern-match call site in
  `ShoppingCartEndpointApplicationBuilderExtensions` is updated from `failure is NotFountError`
  to `failure is NotFoundError`, and the surrounding `Result → exception` bridge is otherwise
  unchanged. (PRD US10, US14; plan §5 step 6.)
- **F13.** No existing `Result`-returning call site is migrated to `Result<TValue>`, and no
  endpoint, client contract, or integration test changes as part of this slice. (PRD US12–14; plan §7.)

## Non-functional requirements

- **N1.** *(MediatR use-case)* **N/A — no MediatR handler or command in this slice.** Per
  `agent_docs/architecture.md`; reinstated for slices that have a use-case.
- **N2.** *(Repository placement)* **N/A — no repository in this slice.** Per
  `agent_docs/architecture.md`.
- **N3.** *(Repository exception mapping)* **N/A — no repository/adapter in this slice.** Per
  `agent_docs/error_handling.md`.
- **N4.** `Domain` remains framework-free: `Result<TValue>`, the `Match` overload, and the
  `Error`/`DomainErrors` changes use only the BCL — no EF Core, ASP.NET, MediatR, Serilog, or
  AutoMapper types. Per `agent_docs/architecture.md` (Dependency Rule) and `CLAUDE.md` rule 2.
- **N5.** No use-case decides HTTP status and no code in this slice touches `HttpContext`; the
  endpoint edit is a pure type-name rename inside the existing bridge. Per `CLAUDE.md` rule 5.
- **N6.** *(Endpoint delegate has no business logic)* Satisfied vacuously — no endpoint logic
  is added or changed beyond the `NotFoundError` rename. Per `agent_docs/entry_points/minimal-api.md`.
- **N7.** *(FluentValidation)* **N/A — no validator in this slice.** Per
  `agent_docs/architecture.md` § Validation.
- **N8.** *(Async/CancellationToken)* **N/A — `Result<TValue>` and the vocabulary fixes
  introduce no I/O.** Per `CLAUDE.md`.
- **N9.** The build produces no new warnings under `dotnet build CinemaBookingManagement.sln -warnaserror`
  (accounting for the accepted AutoMapper **NU1903** NuGet-audit advisory, a known project
  constraint). Per `CLAUDE.md` § Verifying changes.
- **N10.** The architecture tests (`BookingManagementService.Domain.ArchitectureTests`) pass
  without new failures — `Result<TValue>` honors the framework-free `Domain` rule and the
  structural invariants. Per `agent_docs/testing.md` § Architecture tests and PRD US16.
- **N11.** No new cross-cutting `*Exception` or `Error` *type* is invented and no error-handling
  *mechanism* changes: `CustomExceptionHandler`, the exception hierarchy, and the MediatR
  pipeline are untouched; the only base-type change is the documented `Result` ctor-visibility
  line. Per `agent_docs/stable_vs_feature.md` and `agent_docs/error_handling.md`. (PRD
  Implementation Decisions; plan §1 STABLE note.)
- **N12.** The change is behavior-preserving at the HTTP boundary: the only runtime-observable
  difference is the `DomainErrors<T>` error-code strings, which no existing test or HTTP
  response asserts. Per PRD US12 and PRD "Further Notes".

## Out of scope

- Migrating any existing `Result`-returning method to `Result<TValue>` (no call site converted).
- Removing the endpoint `Result → exception` bridge or converting any endpoint to
  `Match`-to-HTTP (ADR steps 3–4).
- The `ContentNotFoundException` 204 → 404 contract change and any Flutter coordination
  (ADR step 2 / defect #4).
- Replacing bare `throw new Exception(...)` in the domain/handlers (ADR defect #2, per-slice).
- Unifying the two error models, changing `CustomExceptionHandler`, or altering the MediatR
  pipeline.
- Flipping ADR-002 to Accepted or updating `agent_docs/error_handling.md`.
- A dedicated test for the changed `DomainErrors<T>` code strings.

## Traceability

| Requirement | Verified by |
|---|---|
| F1 | `Result<T>` unit spec (subtype usable as `Result`) + compilation |
| F2 | `Result<T>` unit spec (success carries value) |
| F3 | `Result<T>` unit spec (failure exposes error) |
| F4 | `Result<T>` unit spec (`Value` on failure throws) |
| F5 | `Result<T>` unit spec (implicit from value → success) |
| F6 | `Result<T>` unit spec (implicit from `Error` → failure) |
| F7 | `Result<T>` unit spec (`Match` success/failure branches) |
| F8 | code review checklist (no `Bind`/`Map`/LINQ on the surface) |
| F9 | full suite green (existing `Result` call sites unaffected) + code review |
| F10 | code review (`typeof(T).Name`) + full suite green (no asserting test) |
| F11 | compilation + code review (rename) |
| F12 | compilation + code review (call-site update; bridge unchanged) |
| F13 | full suite green (no contract/integration test changes) + code review |
| N4, N10, N11 | architecture tests + code review checklist in validation.md |
| N5, N6, N12 | full suite green + code review checklist in validation.md |
| N9 | `dotnet build -warnaserror` |
| N1–N3, N7, N8 | N/A this slice (no such component) |
