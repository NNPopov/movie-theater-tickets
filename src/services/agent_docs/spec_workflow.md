# Spec workflow

The shared model behind the spec-chain skills. Read this for any non-trivial change.
`CLAUDE.md` is the authority; this expands the mechanics.

## What a "slice" is here

A slice is **one use-case** = one MediatR command **or** query and its folder under
`Application/<Aggregate>/Command/<UseCase>/` (or `…/Queries/<UseCase>/`). A REST
`POST` and a `GET` on the same resource are two slices. The grain is the operation,
not the entity. See the `slice-decomposition` skill.

## The chain

```
specs/features/<aggregate>/<NNNN>_<slice>/
├── prd.md            ← /to-prd               (why: product decisions, user stories)
├── plan.md           ← /feature-spec         (how: files, types, steps)
├── requirements.md   ← /feature-requirements (F/N requirements with IDs)
├── validation.md     ← /feature-validation   (manual scenarios + review checklist)
└── tests.md          ← /feature-tests        (outside-in test spec, prose)

tests/.../<Slice>OutsideInTests.cs  ← /slice-test-red  (the RED acceptance gate)
```

Run order, one command at a time:

```
/grill-me (optional) → /to-prd → /feature-spec → /feature-requirements
→ /feature-validation → /feature-tests → /slice-test-red → implement until GREEN
```

Each file depends on the ones before it. Do not skip steps.

## Ownership

| Artifact | Owner skill | Others may |
|---|---|---|
| `specs/roadmap.md` | `/to-prd` | read only |
| `prd.md` | `/to-prd` | read |
| `plan.md` | `/feature-spec` | read |
| `requirements.md` | `/feature-requirements` | read |
| `validation.md` | `/feature-validation` | read |
| `tests.md` | `/feature-tests` | read |
| `<Slice>OutsideInTests.cs` | `/slice-test-red` | implementation makes it green |

A skill writes **only** its own artifact. It never edits an earlier one; if an
earlier file is wrong, surface it and let the user re-run that step.

## Numbering and location

- `NNNN` is a **global** zero-padded counter: max existing number across
  `specs/roadmap.md` + 1. Not per-aggregate.
- `<aggregate>` matches the Application feature folder
  (`ShoppingCarts`, `MovieSessions`, …). For a brand-new auxiliary entity that will
  be a vertical slice, use its planned aggregate/module name and confirm with the
  user.

## States (used by the `spec-workflow` dispatcher)

| State | Files present in slice folder | Outside-in test |
|---|---|---|
| Empty | none | absent |
| Started | `prd.md` | absent |
| Planned | + `plan.md` | absent |
| Formalized | + `requirements.md` | absent |
| Validated | + `validation.md` | absent |
| Test-specified | + `tests.md` (all five .md) | absent |
| Red gate set | all five .md | present, RED |
| Complete | all five .md | present, GREEN |

## The acceptance gate

The slice is **done** only when `<Slice>OutsideInTests` is GREEN and the full suite
(`dotnet test`, including the architecture tests) passes. The other three test
levels (handler, repository, endpoint) are written after green, per `testing.md`.

## Modifying a green slice

1. Update `requirements.md` (if requirements change), then `tests.md`.
2. Update `<Slice>OutsideInTests.cs`; run it; confirm it is RED against current code.
3. Implement until GREEN.
4. Update affected unit tests last.

A pure refactor keeps the gate green throughout. If it goes red, it was not a
refactor — update `tests.md` first.

## When to stop and ask (ADR territory)

Halt the chain and ask the user when a slice would require:

- a new cross-cutting `*Exception` / `Error` type, or a change to
  `CustomExceptionHandler`'s mechanism;
- a change to a base type (`AggregateRoot`, `Entity`, `Result`) or the MediatR
  pipeline;
- choosing **vertical slice** packaging for an entity that is currently layered;
- a new library outside the locked stack.

These are architecture decisions, not slice details.
