# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

Modular-monolith ERP replacing a legacy ASP.NET Core MVC system. .NET 10 + EF Core 10
backend, Next.js 16 frontend, SQL Server. Currently Phase 0/1: Identity + Masters.

The full reasoning behind every structural rule is `docs/architecture.md` (§7 maps each
legacy failure to the mechanism that now prevents it). Read it before proposing a
structural change; nearly every "why is this so strict" question is answered there.

## Commands

```bash
# Full local stack (docker infra + API watch + web dev)
pnpm dev:all                 # pnpm dev = frontend only; pnpm dev:down stops docker

dotnet build backend/Erp.slnx          # analyzers + banned symbols are build ERRORS
dotnet test  backend/Erp.slnx          # unit + architecture + integration (needs Docker)

# Single test / class
dotnet test backend/tests/Erp.ArchitectureTests --filter FullyQualifiedName~SourceFileTests
dotnet test backend/Erp.slnx --filter "FullyQualifiedName~Every_endpoint_declares"

# Formatting — subcommands only. Bare `dotnet format` also tries to fix analyzer
# diagnostics and exits non-zero on RS0030 (banned symbols), which has no code fix.
dotnet format whitespace backend/Erp.slnx
dotnet format style backend/Erp.slnx

pnpm lint | pnpm typecheck | pnpm test          # all --filter web
pnpm --filter web exec vitest run filter-terms   # single vitest file

# After changing any entity or IEntityTypeConfiguration (run from backend/)
dotnet ef migrations add <Name> --project src/Erp.Persistence \
  --startup-project src/Erp.Api --context ErpDbContext --output-dir Migrations

# After changing anything in Erp.Contracts — both outputs are committed and CI diffs them
dotnet build backend/src/Erp.Api      # writes contracts/openapi.json
pnpm --filter web generate:api        # → frontend/src/lib/api/generated/erp.ts

# Regenerate db/erd/ from the EF model (a plain `dotnet test` does this too)
dotnet test backend/tests/Erp.ArchitectureTests --filter FullyQualifiedName~SchemaDiagram
```

Migrations apply automatically on API startup in Development. Seeded sign-in
credentials are printed to the console on first run; none are in the repo.

## Backend architecture

**One deployable: `Erp.Api`.** Modules (`src/modules/Erp.Modules.*`) are separate
projects discovered by assembly scan — `Program.cs` names none of them. A module derives
from `ModuleBase`, which maps `/api/v1/{RoutePrefix}` and instantiates every `IEndpoint`
found *in its own assembly*. Adding an endpoint means adding one file; there is no
registration list. Handlers and validators are likewise discovered
(`AddHandlersFromAssembly`, `AddValidatorsFromAssemblyContaining`).

**Everything in a module is `internal` except `MastersModule` and `Integration/`.** The
compiler is the module boundary, not convention — `ModuleBoundaryTests` fails otherwise.
`Integration/` holds the cross-module surface (e.g. `MastersPermissions`).

**One endpoint per file**, named `<Verb><Thing>Endpoint.cs`, inside a feature-slice
folder: `Application/Parts/CreatePart/{Command,Handler,Validator,Endpoint}.cs`. The unit
of organisation is the feature, never the layer — there is no `BLL/` or `Services/` bucket.

**One `ErpDbContext` for the whole application** (ADR 0002). Every entity, every
`IEntityTypeConfiguration` and all migrations live in `src/Erp.Persistence`, *beneath* the
modules. Modules own vertical slices, not tables; a cross-module write is one
`SaveChangesAsync`. Do not add a second `DbContext`.

**Handlers return `Result<T>`**, endpoints call `.ToHttpResult()` → RFC 9457
ProblemDetails. Never throw for expected failures, never return 200 with an error body.

**Every endpoint must declare `.RequirePermission(Const)` or
`.RequireAuthenticatedUserOnly()`.** `EndpointConventionTests` walks the real endpoint
table and fails on any that declares neither — the two conventions are distinct so
"needs no permission" is distinguishable from "someone forgot". Permissions are C#
constants, seeded, and returned to the UI for display only; the server always re-checks.

**List endpoints are server-paged through a `QueryMap`** — an allow-list of
column→expression. Sorting/filtering on an undeclared field returns 400 rather than
reaching SQL. `Collection_endpoints_return_a_paged_contract` enforces `PagedResult<T>`.
Reuse the same `QueryMap` for exports so export always matches the screen.

**Conventions applied centrally in `ErpDbContext.OnModelCreating` + interceptors**, never
per-entity: `IBusinessUnitScoped` → global tenant query filter + stamping interceptor,
`ISoftDeletable` → filter, `IAuditable` → audit stamps, `IHasRowVersion` → 409 on stale
writes, decimal precision by convention.

### Things that will fail your build

- `BannedSymbols.txt` → error RS0030: raw SQL (`FromSqlRaw`, `SqlCommand`),
  `DateTime.UtcNow`/`Now` (inject `IClock`), `Console` (use `ILogger`), `.Result`/`.Wait()`/
  sync `SaveChanges`, `GetRequiredService`, culture-less `Parse`. Removing a line needs an ADR.
  Test projects downgrade RS0030 to a warning.
- `TreatWarningsAsErrors` everywhere (IDE0055 exempt — CI reports formatting instead).
- No source file over **800 lines** (`SourceFileTests`).
- Architecture tests also assert: SharedKernel and Contracts have no dependencies, no
  mapped floating-point property, every tenant-scoped entity has a query filter, every
  decimal declares precision, endpoint names unique, every master has a list endpoint.

### Known traps (each one already shipped a bug here)

- **`entity.Id.Value` is untranslatable in LINQ.** Strongly-typed IDs map to a column, but
  `.Value` is a CLR member — project the id and unwrap after materialisation.
- **`[AsParameters]` cannot bind `PageRequest`.** List endpoints declare explicit
  `page/pageSize/sort/search/filter` query parameters and call `PageRequestBinding.From`.
- **Interceptors are attached explicitly** in `AddErpDbContext`; EF's implicit DI discovery
  silently didn't pick them up, writing rows with `BusinessUnitId = 0`.
- **Module discovery probes the output directory**, not `GetReferencedAssemblies()` — the
  compiler elides project references the host never names in code.

## Frontend architecture

`frontend/` is the pnpm package **`web`** (workspace commands are `pnpm --filter web …`).
It is also the BFF: `src/app/api/[...path]/route.ts` proxies every browser call to .NET,
attaching the HttpOnly `SameSite` session cookie. Consequences to preserve — the browser
never holds a token, there is no CORS config, and request/response headers are
allow-listed rather than forwarded wholesale.

- **`apiFetch` (`src/lib/api/fetcher.ts`) is the only way to call the API.** Paths are
  relative (`/masters/parts`), prefixed with `/api/v1` so they travel through the BFF.
- **The generated client is types-only.** orval emits `src/lib/api/generated/erp.ts`; the
  fetch client it produces alongside is a by-product and unused. Import types from
  `@/lib/api/types`. Do not hand-edit generated output — CI regenerates and diffs it.
- **Layout:** `src/features/<module>/<entity>/` holds table, form, and `use-<entity>.ts`
  (TanStack Query hooks + query keys). Shared kits live in `src/components/`:
  `data-table/use-server-table.ts` (URL search-params ↔ server query — the only sanctioned
  way to build a list; nothing may fetch an unbounded array), `form/`, `permission/can.tsx`,
  `ui/` (shadcn primitives, owned in-repo and editable).
- Navigation is a typed config file (`src/config/nav.ts`), each item declaring a
  permission. Only permissions are data — screens require a deploy anyway.
- Zod schemas mirror the FluentValidation rules; the server validator is the truth.

## Plan vs. reality

`docs/architecture.md` is the plan; `docs/status.md` is what has actually been built and
verified. Referenced but not yet present: Playwright e2e in `frontend/` (no dependency,
no config, no `e2e/`, no CI step — `pnpm --filter web exec playwright test` in the README
will not run), `tools/DataMigration/`, and `packages/*`. Frontend tests today are Vitest
only. Modules other than Masters exist on paper alone.
