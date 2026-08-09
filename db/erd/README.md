# Entity-relationship diagrams

Every `*.md` beside this file is **generated** from the EF Core model and overwritten on
the next test run. Editing one accomplishes nothing. One file per database schema, named
for the schema.

```
dotnet test backend/tests/Erp.ArchitectureTests --filter FullyQualifiedName~SchemaDiagram
```

A plain `dotnet test` regenerates them too — the generator is an ordinary test, so anyone
running the suite leaves with current diagrams. GitHub renders `erDiagram` blocks natively,
so the files are readable in the browser without any tooling.

## Why generated

An ERD maintained by hand documents what someone believed the schema was on the day they
drew it. Given 14 planned modules, it is wrong within one phase — and a wrong ERD is worse
than none, because people trust it. These are a projection of the same metadata EF uses to
emit SQL, so a diagram that disagrees with the database is not expressible.

Discovery is by type across all `Erp.*` assemblies, the same way
`DevelopmentBootstrap` finds contexts to migrate. No module is named anywhere in the
generator, so a new module's tables appear here by existing — nobody has to remember.

Source: [`backend/tests/Erp.ArchitectureTests/Documentation/`](../../backend/tests/Erp.ArchitectureTests/Documentation).

## What these files are not

They describe **what exists**. For the model being built toward — module ownership, the
aggregates each of the 14 modules will own, and the rules for referencing across
modules — see [`docs/data-model.md`](../../docs/data-model.md).

## The unenforced-reference table

Each diagram ends with a list of columns that name a relationship the database does not
enforce: `*Id`, of a key-shaped type, not a primary key, and not covered by any foreign
key. It exists because the `masters` schema currently declares **no foreign keys at all**,
so the diagram alone would render disconnected boxes and quietly imply that was intended.

The list is a prompt to look, not a defect list. Three groups appear in it, and only one
is a problem:

| Columns | Status |
|---|---|
| `CreatedByUserId`, `ModifiedByUserId`, `DeletedByUserId` | **Deliberate.** They point at `identity.AspNetUsers`, which belongs to a different `DbContext` and schema. A foreign key across that boundary would couple the modules in the database and defeat the isolation the boundary exists to create. Never "fix" these. |
| `BusinessUnitId` | **Deliberate for now.** Tenancy is enforced by the global query filter and stamping interceptor in `ErpDbContextBase`, not by a constraint. Revisit alongside Row-Level Security (`architecture.md` §6). |
| `Part.CategoryId`, `Employee.RoleId`, `Employee.SiteId`, `Role.RoleId` | **Real gaps.** Legacy grouping keys carried over with no constraint behind them; two of them point at tables that do not exist yet. Tracked in [`docs/data-model.md`](../../docs/data-model.md). |

Rows leave this list as real constraints land, and become lines in the diagram above it.
It shrinks as the schema improves rather than needing to be maintained.
