# 0007 · CreateMovieSession Result→HTTP — PRD

> ADR-002 step 3 applied to the **MovieSessions write path** — the fifth end-to-end
> conversion, and the first to use the **generic `Result<Guid>`** at an endpoint (the
> generic was built in slice `0001` but never exercised by `0003`–`0006`, which all used
> the non-generic `Result`). Closes the last outstanding **named bare-`Exception`
> defect** from ADR-002 (defect #2) on this aggregate.

## Problem Statement

As a maintainer of the BookingManagement service, when a scheduling client tries to
create a movie session (a showtime) for a **cinema hall (auditorium) that does not
exist** or a **movie that does not exist**, the `CreateMovieSession` command handler
raises a **bare `throw new Exception()`** (no message). `CustomExceptionHandler` maps any
unrecognised exception to **`500 Internal Server Error`** — so a perfectly ordinary
"you referenced an id that isn't there" mistake is reported to the caller as a *server
bug*. The client cannot distinguish "I sent a bad reference" from "the service crashed,"
and the response carries no useful `ProblemDetails`.

This is also the **last named bare-`throw new Exception(...)` defect** that ADR-002
(now *Accepted*) flagged for adoption. The ADR's error model is decided, but this
handler on the MovieSessions write path was never converted: it neither raises a
specific exception nor returns a `Result`. It is, additionally, the natural first place
to exercise the generic `Result<Guid>` the platform built in slice `0001`.

## Solution

As a scheduling client, when I post a create-movie-session request that references a
**non-existent auditorium or movie**, I want a clean **`404 Not Found`** with a
`ProblemDetails` body — not a misleading `500` — so I can tell I sent a bad reference and
fix my request; and when the references are valid I still get **`201 Created`** with the
new movie-session id exactly as before.

Convert `CreateMovieSession` end-to-end to the ADR-002 `Result` model, reusing the
existing shared infrastructure unchanged:

- The handler returns **`Result<Guid>`** — `Result.Success(<new session id>)` on success,
  a **`NotFoundError`** when the referenced cinema hall or movie is absent.
- The endpoint resolves it with **`result.Match(id => CreatedAtRoute(...), ErrorResults.ToProblem)`**,
  so a missing reference becomes a `404` `ProblemDetails` via the shared mapper and
  success stays `201 Created` pointing at `GetShowtimeById` with the id as the body.

No domain change, no new `Error` kind, no mapper arm, no base-type or
`CustomExceptionHandler` change, no schema/migration.

## User Stories

1. As a scheduling client, I want to create a movie session by posting a movie id, an
   auditorium id, and a session date, so that the showtime becomes bookable.
2. As a scheduling client, when both the movie and the auditorium exist, I want to
   receive `201 Created` with the new movie-session id, so that I can immediately
   reference the created session.
3. As a scheduling client, when I reference an **auditorium (cinema hall) that does not
   exist**, I want a `404 Not Found`, so that I know my auditorium id is wrong rather
   than thinking the service crashed.
4. As a scheduling client, when I reference a **movie that does not exist**, I want a
   `404 Not Found`, so that I know my movie id is wrong.
5. As a scheduling client, I want the `404` response to carry a `ProblemDetails` body in
   the same shape the rest of the service emits for not-found, so that my error handling
   is uniform across endpoints.
6. As a scheduling client, I want the previously-returned **`500`** for these two cases
   to **become `404`**, so that genuine server errors and "bad reference" are no longer
   conflated.
7. As a scheduling client, I want the **success contract unchanged** — `201 Created`,
   `CreatedAtRoute` to `GetShowtimeById`, body = the new id — so that existing callers
   keep working.
8. As a scheduling client, I want the OpenAPI metadata to advertise `201` and `404`
   (and drop the stale `204`), so that the documented contract matches reality.
9. As a service maintainer, I want the two **bare `throw new Exception()`** calls in the
   create-movie-session handler replaced by an explicit `NotFoundError`, so that the last
   named ADR-002 bare-exception defect on this aggregate is closed.
10. As a service maintainer, I want this conversion to **reuse the existing shared
    `Error → IResult` mapper** (`ErrorResults.ToProblem`) and the existing generic
    `Match`, so that no new cross-cutting type or mechanism is introduced.
11. As a service maintainer, I want this to be the **first endpoint use of the generic
    `Result<Guid>`**, proving the `Result<T>` infrastructure built in slice `0001` works
    end to end, so that future value-carrying use-cases have a worked example.
12. As a service maintainer, I want **no domain change** to `MovieSession.Create` or
    `MovieSessionSeat.Create`, so that the conversion stays a thin application/endpoint
    change with no behavioural risk to the aggregate.
13. As a service maintainer, I want the not-found check to **short-circuit before** the
    movie session and its seats are created and persisted, so that nothing is written
    when a referenced resource is missing (the atomicity the thrown path provided).
14. As a service maintainer, I want the **error codes** to be aggregate-specific
    (`CinemaHall.NotFound`, `Movie.NotFound`), so that the not-found reason is
    identifiable, consistent with the `DomainErrors<T>` convention.
15. As a service maintainer, I want **structural input validation unchanged** — malformed
    input still surfaces as `400` via `ValidationBehaviour`; only *reference existence*
    (a business precondition) becomes a `Result`/`404`.
16. As a service maintainer, I want the create handler still to **create one
    `MovieSessionSeat` per auditorium seat and persist the session** on the success path
    exactly as today, so that the conversion changes only the error path and the return
    type, not the creation logic.
17. As a developer, I want a **handler unit test** pinning the three outcomes
    (auditorium-missing ⇒ `NotFoundError`; movie-missing ⇒ `NotFoundError`; both present
    ⇒ `Result<Guid>` success with the new id and the seats/session persisted), so that
    the conversion is the acceptance gate and cannot silently regress.
18. As a developer, I want the test to assert **atomicity on failure** (no session saved,
    no seats added when a reference is missing), so that the short-circuit is verified.
19. As a service maintainer, I want this slice to **not** touch the still-open endpoint
    helper bare-throws (`GetClientId`/`CreateShoppingCart`) or the Flutter client, so that
    its scope stays one use-case.
20. As a product owner, I want the ADR-002 adoption to be **measurably closer to fully
    migrated** after this slice (only the endpoint-helper slice and the Flutter follow-up
    remaining), so that the error-model unification has a clear finish line.

## Implementation Decisions

- **Aggregate / module:** `MovieSessions` (the `CreateMovieSession` write path). Filed
  under the `platform` slice folder to keep the ADR-002 series (`0001`–`0007`) together,
  consistent with prior slices.
- **Single use-case modified:** the `CreateMovieSession` command and its handler. No new
  use-case, no other MovieSessions use-case touched.
- **Command result type:** `CreateMovieSessionCommand` changes from
  `IRequest<Guid>` to **`IRequest<Result<Guid>>`**.
- **Handler outcomes:**
  - referenced **cinema hall (auditorium) not found** ⇒ return
    `DomainErrors<CinemaHall>.NotFound(...)` (a `NotFoundError`, code `CinemaHall.NotFound`);
  - referenced **movie not found** ⇒ return `DomainErrors<Movie>.NotFound(...)` (a
    `NotFoundError`, code `Movie.NotFound`);
  - both present ⇒ create the `MovieSession`, create one `MovieSessionSeat` per auditorium
    seat, persist, and return `Result.Success(<new session id>)` as `Result<Guid>`.
  - The two not-found checks **short-circuit before** any creation/persistence
    (atomicity). Current check order (auditorium, then movie) is preserved.
- **Endpoint (`POST {BaseRoute}`, name `CreateMovieSessions`):** replace the direct
  `Results.CreatedAtRoute(...)` with
  `result.Match(id => Results.CreatedAtRoute("GetShowtimeById", new { id }, id), ErrorResults.ToProblem)`.
  Success stays `201 Created` to `GetShowtimeById` with the id body. Update `.Produces` to
  declare `201` and `404`; drop the stale `.Produces(204)`.
- **Reused unchanged (no edits):** the generic `Match<TValue, TOut>` extension
  (`ResultExtensions`), the shared `ErrorResults.ToProblem` (`NotFoundError ⇒ 404`,
  `ConflictError ⇒ 409`, else `500`), and `DomainErrors<T>.NotFound`. **No** new `Error`
  kind, **no** new mapper arm, **no** base-type / `CustomExceptionHandler` /
  MediatR-pipeline / validation-behaviour change.
- **No domain change:** `MovieSession.Create`, `MovieSessionSeat.Create`, and the
  hardcoded seat price remain as-is.
- **No schema change ⇒ no EF Core migration.**
- **ADR guardrail:** if the conversion appears to need a new `Error` type, a mapper arm, a
  base-type change, or a domain-method change, **stop and ask** — that exceeds this slice.

## Testing Decisions

- **What makes a good test here:** assert only externally observable behaviour — *which
  `Result` the handler returns for each outcome* (and therefore the HTTP status the shared
  mapper yields) and the persistence side-effects — never internal control flow.
- **Handler unit test — the acceptance gate (the only test added).** xUnit +
  FluentAssertions + NSubstitute, mirroring `ReserveTicketsCommandHandlerTests` (`0005`)
  and `PurchaseTicketsCommandHandlerTests` (`0006`). Substitute `ICinemaHallRepository`,
  `IMoviesRepository`, `IMovieSessionSeatRepository`, `IMovieSessionsRepository`. Facts:
  1. auditorium (cinema hall) missing ⇒ result is failure, `Error` is `NotFoundError`; no
     `MovieSessionSeat` added, no session saved (atomicity).
  2. movie missing ⇒ result is failure, `Error` is `NotFoundError`; nothing persisted.
  3. both present ⇒ `Result<Guid>` success whose `Value` is the created session id; one
     seat created per auditorium seat and the session saved.
- **No `WebApplicationFactory` end-to-end test** — there is no HTTP harness in this repo
  (consistent with `0003`–`0006`); the endpoint's `Match` wiring is covered by compilation
  and the shared mapper is already pinned by slice `0003`'s `ErrorResultsOutsideInTests`.
- **No domain unit test** — the domain (`MovieSession.Create`, `MovieSessionSeat.Create`)
  is unchanged by this slice.
- **Prior art:** `ReserveTicketsCommandHandlerTests`, `PurchaseTicketsCommandHandlerTests`
  (same project `BookingManagementService.Domain.UnitTests`, RootNamespace
  `CinemaTicketBooking.Application.UnitTests`).

## Out of Scope

- The endpoint-helper bare throws `GetClientId` / `CreateShoppingCart` in the ShoppingCart
  endpoints — a **separate, later slice** (the next ADR-002 tail).
- The Flutter client follow-up to slice `0002`'s `204 → 404` contract change, and any
  client effect of this slice's `500 → 404`.
- Converting query/read handlers that throw `ContentNotFoundException` — intentional
  exception tails, documented in `agent_docs/error_handling.md`.
- Any other MovieSessions use-case (update, delete, list, get-by-id).
- The composition-root guard `throw new Exception("identityOptions is null")` in
  `ConfigureApiServices` — a config-time startup check, not a business path.
- Changing `MovieSession.Create`, the seat-creation loop, or the hardcoded seat price.
- Adopting `Result<T>` on any other path, or adding a `400`-class mapper arm.
- A `WebApplicationFactory` HTTP integration harness.
- Schema changes / EF Core migrations.

## Further Notes

- **Behaviour change:** posting a non-existent auditorium or movie id moves from
  **`500 → 404`**. The success contract (`201 Created`, `CreatedAtRoute` to
  `GetShowtimeById`, body = the new id) is **preserved**.
- **Open question (resolved here):** are "auditorium not found" / "movie not found" a
  `404` (not-found business outcome) or a `400` (invalid input)? **Chosen: `404`
  (`NotFoundError`)** — a missing referenced resource is a not-found outcome, consistent
  with the rest of ADR-002; structural/malformed input remains `400` via
  `ValidationBehaviour`. Confirm in `requirements.md` before red.
- **Significance:** this is the first endpoint to exercise the generic `Result<Guid>`,
  validating end-to-end the `Result<T>` infrastructure from slice `0001`.
- After this slice, the only remaining ADR-002 adoption tails are the endpoint-helper
  bare-throws slice and the Flutter client follow-up.
- **Publishing:** `gh` CLI is not installed in this environment, so this PRD is stored
  locally only (as for `0001`–`0006`); publishing to the issue tracker with the
  `needs-triage` label is deferred until a tracker is available.
