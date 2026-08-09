# ADR 0002 — One DbContext for the application

- **Status**: Accepted
- **Date**: 2026-08-09
- **Amends**: [ADR 0001](0001-modular-monolith-with-compiler-enforced-boundaries.md),
  which decided one `DbContext` and one schema per module

## Context

ADR 0001 gave each module its own `DbContext`, and the host a separate
`IdentityDataContext`. With Masters and Identity built, that shape had produced:

- **Two migration histories** — `masters.__EFMigrationsHistory` and
  `identity.__EFMigrationsHistory`. `dotnet ef database update` had to be run
  once per context, and `dotnet run` migrated by scanning the loaded assemblies
  for every non-abstract `DbContext`.
- **A read model that existed only to cross the boundary.** Every master grid
  shows who created and last changed a row. The audit interceptor stores a user
  id, and the name lives in `identity.AspNetUsers` — a table the Masters context
  could not see. The workaround was `AuditUser`, a keyless entity mapped as a
  view over another context's table, plus its own configuration class, plus a
  paragraph in each list handler explaining why it was there.
- **A boundary that cost more than it returned at this size.** One module. The
  `internal` wall was doing real work in principle and very little in practice.

## Decision

**One `ErpDbContext` for the whole application.** Master data and ASP.NET
Identity are one model, one migration history, one registration.

It lives in a new project, `src/Erp.Persistence`, together with every entity and
every `IEntityTypeConfiguration`. That placement is forced rather than chosen: a
context with typed `DbSet<T>` properties must see every entity at compile time,
and every handler must see the context. Entities therefore have to sit *beneath*
the modules — otherwise the next module could not contribute an entity without
referencing Masters, and "one context" would stop being true the moment
Procurement landed.

Consequences that follow:

- **Entities are `public`.** The compiler-enforced boundary from ADR 0001 is
  withdrawn for entities. Modules keep their vertical slices — handlers,
  endpoints, validators stay `internal` to their own assembly — but they no
  longer own their tables.
- **Schemas stay.** `masters.*` and `identity.*` are unchanged. One class maps
  all of it; ownership is still legible in SSMS, and a query may now join across
  the two freely.
- **`AuditUser` is deleted.** A "created by" name is an ordinary join onto
  `Users`.
- **`ErpDbContextBase` is deleted.** Its conventions — rowversion, soft-delete
  and tenancy query filters, decimal precision — are folded into `ErpDbContext`,
  which inherits `IdentityDbContext<ErpUser, ErpRole, Guid>` and so could not
  also inherit the old base. The conventions are unchanged and still applied by
  walking the model, never per entity.
- **Migrations were squashed.** The ten migrations across the two contexts became
  one `InitialCreate` plus `SeedLookupValues`, which carries the 206 seeded
  lookup rows forward from the old `AddLookupValues`. No database held data worth
  preserving at the time.

## Alternatives considered

**One context, entities left in their modules, model assembled from each
module's `IEntityTypeConfiguration` at runtime.** The context declares no
`DbSet<T>`; handlers use `Set<Part>()`. This gives one context *and* keeps every
entity `internal` — the boundary survives intact. Rejected in favour of typed
`DbSet` properties, which read better at every call site. This remains the
option to revisit if the boundary is ever wanted back.

**Keeping Identity separate.** Defensible — `IdentityDbContext` is a framework
base class with its own opinions — but it leaves two contexts, two histories, and
the `AuditUser` view still needed to put a name in a grid column. It does not
solve the thing that prompted the change.

## Consequences

- A cross-module write is now one `SaveChangesAsync` in one transaction. The
  transactional outbox described in ADR 0001 is no longer required *for
  consistency* between modules; it remains the mechanism if a module is ever
  extracted to its own service.
- Extracting a module to its own service is materially harder than ADR 0001
  intended. That was the price of the decision and it was made deliberately.
- `ModuleBoundaryTests.Module_types_are_internal_except_the_module_entry_point_and_Integration`
  still passes, and still means something: it now guards the module's
  *application* code, since its entities have left the assembly.
