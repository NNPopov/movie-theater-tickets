# 0001 · ErrorModelResultInfrastructure — Outside-in test spec

> **Adaptation note (read first).** The standard outside-in test in this project drives a
> slice end-to-end **through HTTP** via `WebApplicationFactory<Program>`. **This slice has no
> HTTP entry point, no MediatR use-case, and no repository** — it is a `Domain/Error`
> shared-kernel change. Per the PRD (§ Testing Decisions) and `plan.md` §6, the acceptance gate
> is therefore an in-process **`Result<T>` unit specification** plus the full suite (including
> the architecture tests) staying green. The HTTP outside-in level is not skipped to dodge
> work — there is simply no endpoint to exercise; the equivalent "black-box over the public
> surface" assertion is made directly against `Result<T>`. The C# class is produced by
> `/slice-test-red`.

## Goal

Prove the public contract of `CinemaTicketBooking.Domain.Error.Result<TValue>`: a success
carries its value, a failure exposes its error and forbids `Value` access, the two implicit
conversions land on the correct branch, and `Match` invokes the correct branch with the
correct argument — all through the public surface only, without asserting internal field
layout.

## Entry point

In-process public API (not HTTP):

- **Type under test:** `CinemaTicketBooking.Domain.Error.Result<TValue>` and the
  `ResultExtensions.Match<TValue, TOut>` extension.
- **Surface exercised:** `Result<TValue>.Success(value)`, `Result<TValue>.Failure(error)`,
  implicit conversion from `TValue`, implicit conversion from `Error`, the inherited
  `IsSuccess`/`IsFailure`/`Error`, the `Value` property, and the generic `Match` overload.
- **No HTTP, no route, no auth** — the assembly under test is `Domain`, reached transitively
  through the `Domain.UnitTests` project's reference to `Application`.

## Wired real

- `CinemaTicketBooking.Domain.Error.Result<TValue>` (the real type, once implemented).
- `CinemaTicketBooking.Domain.Error.ResultExtensions.Match<TValue, TOut>` (real extension).
- `CinemaTicketBooking.Domain.Error.Error` and the base `Result` (real, unchanged behavior).
- Nothing else — pure in-memory, no DbContext, no host, no middleware.

## Mocked

None — the test runs entirely in memory against the real domain types. No database, no Redis,
no clock, no event bus is involved.

## Fixtures / setup

- **Test project:** `BookingManagement/tests/BookingManagementService.Domain.UnitTests`
  (net10.0; xUnit + FluentAssertions + NSubstitute; root namespace
  `CinemaTicketBooking.Application.UnitTests`).
- **File:** `Error/ResultOfTSpecification.cs`, namespace
  `CinemaTicketBooking.Application.UnitTests.Error`.
- **Class:** `ResultOfTSpecification` (matches the `*Specification` + AAA + `[Fact]`
  conventions of the sibling `ShoppingCarts/ShoppingCartSpecification.cs`).
- **Sample types/values used by the facts:** a reference value type for the carried value
  (`string`, e.g. `"payload"`), and a concrete `Error` (e.g.
  `new Error("Test.Code", "test description")`). No seeding or external state.
- **Auth:** none — not an HTTP test.

## Test scenarios

Because this is the only test level for the slice, the scenario set covers the **full ~7-fact
contract** rather than just one happy + one failure path.

### Scenario 1: success carries the value

**Setup:** a value `"payload"`.

**Act:** `var result = Result<string>.Success("payload");`

**Expect:**

- `result.IsSuccess` is `true`; `result.IsFailure` is `false`.
- `result.Error` equals `Error.None`.
- `result.Value` equals `"payload"`.

**Covers requirement(s):** F2 (and F1 — a `Result<string>` is a `Result`).

### Scenario 2: failure exposes the error

**Setup:** an error `new Error("Test.Code", "test description")`.

**Act:** `var result = Result<string>.Failure(error);`

**Expect:**

- `result.IsFailure` is `true`; `result.IsSuccess` is `false`.
- `result.Error` equals the supplied `error`.

**Covers requirement(s):** F3.

### Scenario 3: accessing `Value` on a failure throws

**Setup:** `var result = Result<string>.Failure(new Error("Test.Code"));`

**Act:** read `result.Value`.

**Expect:**

- Accessing `Value` throws `InvalidOperationException` (fail-fast on misuse), rather than
  returning `null`/`default`.

**Covers requirement(s):** F4.

### Scenario 4: implicit conversion from a value yields success

**Setup:** a value `"payload"`.

**Act:** `Result<string> result = "payload";`

**Expect:**

- `result.IsSuccess` is `true`.
- `result.Value` equals `"payload"`.

**Covers requirement(s):** F5.

### Scenario 5: implicit conversion from an `Error` yields failure

**Setup:** an error `new Error("Test.Code", "test description")`.

**Act:** `Result<string> result = error;`

**Expect:**

- `result.IsFailure` is `true`.
- `result.Error` equals the supplied `error`.

**Covers requirement(s):** F6.

### Scenario 6: `Match` runs the success branch with the carried value

**Setup:** `var result = Result<string>.Success("payload");`

**Act:** `var output = result.Match(value => $"ok:{value}", error => $"err:{error.Code}");`

**Expect:**

- `output` equals `"ok:payload"` — the success delegate ran and received the carried value.

**Covers requirement(s):** F7.

### Scenario 7: `Match` runs the failure branch with the error

**Setup:** `var result = Result<string>.Failure(new Error("Test.Code", "d"));`

**Act:** `var output = result.Match(value => $"ok:{value}", error => $"err:{error.Code}");`

**Expect:**

- `output` equals `"err:Test.Code"` — the failure delegate ran and received the error.

**Covers requirement(s):** F7.

## Out of scope for this test

- The `DomainErrors<T>` `typeof(T).Name` code strings (F10) — deliberately not asserted; no
  consumer depends on them and the rename/typeof fixes are verified by compilation and the full
  suite staying green (PRD § Testing Decisions).
- The `NotFountError → NotFoundError` rename (F11) and its call-site update (F12) — verified by
  compilation, not by a runtime assertion.
- That existing `Result` call sites are unaffected (F9, F13) — verified by the full suite
  staying green, not by this spec.
- Any HTTP status, routing, validation (400), or `CustomExceptionHandler` behavior — there is
  no endpoint in this slice.
- `Bind`/`Map`/LINQ behavior — those members do not exist by design (F8); absence is checked in
  the code review checklist, not by a test.
