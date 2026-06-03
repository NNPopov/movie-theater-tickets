# 0001 · ErrorModelResultInfrastructure — Implementation plan

## 1. Header

- **Aggregate / Module:** `platform` (cross-cutting `Domain/Error` shared kernel — not a DDD aggregate)
- **Slice:** `0001_error_model_result_infrastructure`
- **PRD:** ./prd.md
- **ADR:** `docs/adr/ADR-002-error-handling-model-result-vs-exceptions.md` (step 1; ADR stays **Proposed**)
- **Reference slice (if any):** None. This is the first slice in the `platform` module and the
  first slice of any kind in the spec chain. There is **no shape-matching prior slice**
  (this slice has no MediatR use-case and no HTTP entry point, unlike every command/query
  slice). Naming/structure for the shared kernel follows the existing `Domain/Error/*` files;
  the unit spec follows the existing `BookingManagementService.Domain.UnitTests` conventions.
- **HTTP path:** None. This slice introduces no endpoint and changes no route.
- **STABLE files touched (ADR-gated — see note):**
  - `BookingManagement/BookingManagementService.Domain/Error/Result.cs` — base ctor
    `private` → `protected` (one line) **plus** the new `Result<T>` type.
  - `BookingManagement/BookingManagementService.Domain/Error/Error.cs` — rename
    `NotFountError` → `NotFoundError`.
  - `BookingManagement/BookingManagementService.Domain/Error/DomainErrors.cs` —
    `nameof(T)` → `typeof(T).Name` in all four factories.
  - `BookingManagement/BookingManagementService.Domain/Error/ResultExtensions.cs` — add the
    generic `Match` overload.
  - `BookingManagement/BookingManagementService.API/Endpoints/ShoppingCartEndpointApplicationBuilderExtensions.cs`
    — update the single `NotFountError` pattern-match call site to `NotFoundError`.

  > **Why this is allowed despite touching stable files.** `Domain/Error/*` and the base
  > `Result` type are listed as *stable infrastructure* in `agent_docs/stable_vs_feature.md`,
  > and `agent_docs/spec_workflow.md` § "When to stop and ask" flags a change to a base type
  > (`Result`) or the cross-cutting `Error` vocabulary as **ADR territory**. That stop-and-ask
  > has already happened: this work is ADR-002 step 1, captured via a grill-me interview and an
  > approved `prd.md`, and is *deliberately* run through the full spec chain rather than as an
  > ad-hoc edit. No new mechanism is invented (no change to `CustomExceptionHandler`, the
  > MediatR pipeline, or the exception hierarchy). If, during implementation, anything beyond
  > the five edits above proves necessary, **stop and ask** — that would exceed the ADR's
  > step-1 scope.

## 2. Context summary

This slice lays the ADR-002 step-1 foundation in the `Domain/Error/` shared kernel with
**zero change to runtime HTTP behavior**. It (a) introduces a new generic `Result<T>` that
carries a value on success, alongside the existing non-generic `Result`; (b) fixes
`DomainErrors<T>` so error codes name the real aggregate (`typeof(T).Name`) instead of the
literal `"T"` (`nameof(T)`); and (c) renames the mistyped public error record `NotFountError`
to `NotFoundError`, updating its single call site. Nothing **uses** `Result<T>` yet — it is
introduced, not adopted. Callers are domain/application code that already use `Result`; no
existing call site is migrated. The acceptance gate is a focused `Result<T>` unit
specification going green plus the full suite (including the architecture tests) staying
green.

## 3. API contract

**Not applicable — this slice has no HTTP entry point, no request/response model, and no
status codes.** It is a shared-kernel type/refactor change. The relevant "contract" is the
public surface of `Result<T>` and the corrected `Error`/`DomainErrors` vocabulary, specified
in §5.

The only runtime-observable change is the **error-code strings** emitted by `DomainErrors<T>`
at three existing call sites (seat domain, assign-cart handler, purchase handler): they change
from `"T.NotFound"` / `"T.ConflictException"` / … to the real type name
(`"MovieSessionSeat.ConflictException"`, `"ShoppingCart.NotFound"`,
`"AssignClientCartCommandHandler.NotFound"`, …). Per the PRD and confirmed by inspection, no
existing test or HTTP response asserts these strings, so no contract or test changes.

## 4. File structure

```
BookingManagement/
├── BookingManagementService.Domain/
│   └── Error/
│       ├── Result.cs                 # EDIT: ctor private → protected
│       ├── Result{TValue}.cs         # NEW: generic Result<TValue> : Result
│       ├── Error.cs                  # EDIT: NotFountError → NotFoundError
│       ├── DomainErrors.cs           # EDIT: nameof(T) → typeof(T).Name (×4)
│       └── ResultExtensions.cs       # EDIT: add generic Match<TValue,TOut> overload
└── BookingManagementService.API/
    └── Endpoints/
        └── ShoppingCartEndpointApplicationBuilderExtensions.cs   # EDIT: NotFountError → NotFoundError (line ~92)

BookingManagement/tests/
└── BookingManagementService.Domain.UnitTests/
    └── Error/
        └── ResultOfTSpecification.cs   # NEW (produced by /slice-test-red, not here)
```

No new EF Core entity and no model change → **no migration**.

> Note on `Result<T>` placement: a separate file `Result{TValue}.cs` keeps the diff to the
> stable `Result.cs` minimal (only the `private`→`protected` line). Putting `Result<T>` in the
> same `Result.cs` file is acceptable too; the plan prefers the separate file for a cleaner
> review. Decide at implementation time, but keep `Result.cs`'s behavioral change to the one
> ctor-visibility line.

## 5. Implementation steps

1. **Domain — open the base `Result` for inheritance.**
   In `Domain/Error/Result.cs`, change the constructor signature
   `private Result(bool isSuccess, Error error)` to
   `protected Result(bool isSuccess, Error error)`. **Nothing else in this file changes** —
   the validation guard, `IsSuccess`/`IsFailure`/`Error`, and `Success()`/`Failure(error)`
   stay identical. This preserves all existing non-generic `Result` behavior.

2. **Domain — add the generic `Result<TValue>`.**
   Create `Domain/Error/Result{TValue}.cs`:
   ```csharp
   namespace CinemaTicketBooking.Domain.Error;

   public class Result<TValue> : Result
   {
       private readonly TValue? _value;

       private Result(TValue value) : base(true, Error.None) => _value = value;

       private Result(Error error) : base(false, error) => _value = default;

       public TValue Value => IsSuccess
           ? _value!
           : throw new InvalidOperationException("The value of a failure result cannot be accessed.");

       public static Result<TValue> Success(TValue value) => new(value);

       public static new Result<TValue> Failure(Error error) => new(error);

       public static implicit operator Result<TValue>(TValue value) => Success(value);

       public static implicit operator Result<TValue>(Error error) => Failure(error);
   }
   ```
   Notes:
   - Subtype of `Result` (user stories 1–2): usable anywhere a `Result` is accepted; inherits
     `IsSuccess`/`IsFailure`/`Error`.
   - `Value` on a failure throws `InvalidOperationException` (user story 3, fail-fast).
   - Two implicit conversions (`TValue` → success, `Error` → failure) for natural call-site
     reads (user story 4). These do **not** conflict with the existing
     `Error → Result` implicit operator on `Error`: that one targets the base `Result`; this
     one targets `Result<TValue>` and is the better match when the target is `Result<TValue>`.
   - `Failure` is declared `new` because the base `Result.Failure(Error)` exists with a
     different return type; this hides it for `Result<TValue>` without changing the base.
   - **No** `Bind`/`Map`/LINQ combinators (user story 6, ADR-002).
   - Pure BCL only — honors the framework-free `Domain` rule (architecture test stays green).

3. **Domain — add the generic `Match` overload.**
   In `Domain/Error/ResultExtensions.cs`, add alongside the existing non-generic `Match`:
   ```csharp
   public static TOut Match<TValue, TOut>(
       this Result<TValue> result,
       Func<TValue, TOut> onSuccess,
       Func<Error, TOut> onFailure)
   {
       return result.IsSuccess ? onSuccess(result.Value) : onFailure(result.Error);
   }
   ```
   The success delegate takes one argument (the carried value), so overload resolution against
   the existing `Match<T>(this Result, Func<T> onSuccess, …)` (no-arg success) is unambiguous
   by arity (user story 5).

4. **Domain — fix `DomainErrors<T>` codes.**
   In `Domain/Error/DomainErrors.cs`, replace `nameof(T)` with `typeof(T).Name` in **all four**
   factories (`ConflictException`, `NotFound`, `DomainValidation`, `InvalidOperation`). Also
   update the `NotFound` factory body to construct `NotFoundError` (the renamed type from
   step 5). No call-site code changes; only the emitted string changes (user stories 7–8).

5. **Domain — rename `NotFountError` → `NotFoundError`.**
   In `Domain/Error/Error.cs`, rename the `sealed record NotFountError` to `NotFoundError`
   (signature otherwise unchanged: `sealed record NotFoundError(string Code, string? Description = null) : Error(Code, Description)`).
   (user story 9.)

6. **API — update the single call site.**
   In `API/Endpoints/ShoppingCartEndpointApplicationBuilderExtensions.cs` (~line 92), change
   `if (failure is NotFountError)` to `if (failure is NotFoundError)`. The bridge logic
   (`Result → exception` in `.Match(...)`) is **otherwise unchanged** (user story 10; bridge
   stays per PRD Out of Scope).

7. **Verify (pre-test).** From `src/services`:
   ```
   dotnet format CinemaBookingManagement.sln
   dotnet build  CinemaBookingManagement.sln -warnaserror
   ```
   Resolve any warnings. Note the accepted AutoMapper **NU1903** NuGet-audit advisory trips
   `-warnaserror` at restore time — a known, accepted project constraint (see MEMORY:
   dotnet10-migration); handle NuGet audit accordingly so the real build/warnings are what is
   validated.

## 6. Tests planned

This slice has **no handler, no repository, and no endpoint**, so three of the four default
levels do not apply. The single applicable level is the unit specification.

- **Unit specification (the acceptance gate)** —
  `BookingManagement/tests/BookingManagementService.Domain.UnitTests/Error/ResultOfTSpecification.cs`.
  xUnit + FluentAssertions, `*Specification` naming, AAA layout (per
  `ShoppingCarts/ShoppingCartSpecification.cs`). Covers the public `Result<T>` contract
  (~7 facts):
  1. `Success(value)` → `IsSuccess` true and `Value` equals the value.
  2. `Failure(error)` → `IsFailure` true and `Error` equals the error.
  3. Accessing `Value` on a failure throws `InvalidOperationException`.
  4. Implicit conversion from a value yields a success carrying that value.
  5. Implicit conversion from an `Error` yields a failure carrying that error.
  6. `Match` invokes the **success** branch with the carried value.
  7. `Match` invokes the **failure** branch with the error.
  Produced as the RED gate by `/slice-test-red` (it will not compile until `Result<T>` and the
  generic `Match` exist), then turned green by the implementation.

**Opt-outs (explicit, per `agent_docs/testing.md`):**
- **Handler unit test — skipped:** there is no MediatR handler in this slice.
- **Repository/adapter unit test — skipped:** there is no repository or adapter in this slice.
- **Endpoint integration / outside-in HTTP test — skipped:** there is no HTTP entry point;
  the one endpoint edit (the `NotFoundError` rename) is verified by compilation and changes no
  behavior. Per PRD, the corrected `DomainErrors<T>` code strings are **not** asserted by a new
  test — no consumer depends on them and adding such an assertion would only pin an unused
  value.

**Full-suite gate:** the entire `dotnet test` run, **including
`BookingManagementService.Domain.ArchitectureTests`**, must stay green (user story 16).

## 7. Out of scope for this slice

- Migrating any existing `Result`-returning method to `Result<T>` (no call site converted).
- Removing the endpoint `Result → exception` bridge (`AssignClientCart`, `SelectSeats`,
  `ReserveSeats`) or converting any endpoint to `Match`-to-HTTP (ADR steps 3–4).
- The `ContentNotFoundException` 204 → 404 contract change and any Flutter coordination
  (ADR step 2 / defect #4).
- Replacing bare `throw new Exception(...)` in the domain/handlers (ADR defect #2, per-slice).
- Unifying the two error models, changing `CustomExceptionHandler`, or altering the MediatR
  pipeline.
- Flipping ADR-002 to Accepted or updating `agent_docs/error_handling.md`.
- A dedicated test for the changed `DomainErrors<T>` code strings.

## 8. Open questions

None. The `Result<T>` shape, the inheritance approach (base ctor `protected`), the overload
strategy, and the explicit no-`Bind`/`Map`/LINQ decision were all settled in the grill-me
interview and recorded in the PRD's Implementation Decisions.
