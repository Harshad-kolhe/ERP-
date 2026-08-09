# ADR 0001 — Modular monolith with compiler-enforced boundaries

- **Status**: Accepted, partly superseded by
  [ADR 0002](0002-one-dbcontext-for-the-application.md)
- **Date**: 2026-08-08

> **Amended 2026-08-09.** The "one `DbContext` and one SQL schema per module"
> decision below, and the `internal` boundary as it applied to *entities*, were
> reversed by ADR 0002: there is now one `ErpDbContext` for the application and
> entities are public, in `src/Erp.Persistence`. Everything else here still
> holds — modules are still separate assemblies, their application code is still
> `internal`, and minimal APIs one-endpoint-per-file is unchanged.

## Context

The legacy system being replaced is an ASP.NET Core 8 MVC application of
roughly 320,000 hand-written lines in a **single project**. It nominally had
layers — there were `Domain/`, `Models/BLL/` and `Models/Database/` directories —
but nothing enforced them. `Domain/` ended up containing exactly one file. The
"BLL" classes injected `DbContext` directly and therefore simply *were* the data
layer. One class reached 10,444 lines; one controller reached 4,686 lines with
249 action methods and 41 constructor dependencies. 329 of 1,169 commits were
merges, because every feature touched the same handful of files.

The failure was not a lack of intent. Someone drew those boundaries deliberately.
They eroded because nothing made crossing them cost anything.

## Decision

Build a **modular monolith**: one deployable API, with each module as its own
`.csproj`. Every type in a module is `internal` except its `IModule` entry point
and the contents of its `Integration/` folder.

The boundary is therefore the C# `internal` keyword. Another module cannot
reference a Masters entity, handler or `DbContext` — not by convention, not
discouraged by review, but because the compiler will not resolve the name.

Supporting choices that follow from this:

- ~~**One `DbContext` and one SQL schema per module.** Cross-module writes go
  through integration events, never a shared transaction.~~ Reversed by ADR 0002:
  one `ErpDbContext`, one migration history, schemas retained per area.
- **Minimal APIs, one endpoint per file.** A controller is an unbounded bucket;
  that is how one reached 249 actions. A file holding one endpoint has nowhere
  to grow, and two developers adding features touch two different files.
- **Vertical slices, not layers.** `Application/GoodsReceipts/CreateGrn/` holds
  the command, handler, validator and endpoint together.
- **Architecture tests that assert all of the above**, so an erosion attempt is
  a red build rather than a code-review argument.

## Alternatives considered

**Microservices.** Rejected. The domain is tightly coupled — BOM, Inventory and
Procurement all touch the same aggregates — so the immediate result would be
distributed transactions across a team that has never run a distributed system.
The `internal` boundary preserves the option: a module can be extracted later
without redesigning it, because nothing outside it depends on its internals.

**Classic Clean/Onion layering.** Rejected as the *primary* structure. It is what
the legacy system claimed to be. Layer-shaped folders describe where code lives,
not who may call it, and that distinction is exactly what failed. Layering still
applies *within* a module (Domain has no infrastructure dependency, asserted by
`ModuleBoundaryTests.Domain_types_do_not_reference_infrastructure`).

**Keep one project, rely on discipline.** Rejected. This is the null hypothesis,
and there is 320,000 lines of local evidence about how it ends.

## Consequences

- Adding a module means adding a project. Slightly more ceremony, deliberately.
- A module needing another's data must go through `Integration/`, which forces the
  dependency to be designed rather than discovered.
- Refactoring across a boundary is harder. That is the point: the cost is paid at
  design time, visibly, instead of accumulating silently.
- Tests reach module internals via `InternalsVisibleTo` for that module's own test
  project only — the single exception, and one no other module gets.
