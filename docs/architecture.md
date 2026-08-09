# New ERP — Stack, Architecture & Repository Structure

## Context

The legacy system is **not** an old WebForms app — it is already ASP.NET Core 8 MVC. The runtime is modern; the *structure* is what failed. An audit of it found ~320,000 hand-written lines (109k C#, 171k JS, 17k Razor) in a **single project** with:

- **Zero automated tests** — no test project exists, so there is no executable spec of any behaviour.
- **The database as an undocumented source of truth** — 284 `DbSet<>` (188 keyless), ~144 stored procedures holding real business rules, **2** `.sql` files in git, and **no migrations folder ever created**. The schema exists only in the live SQL Server.
- **God classes** — `BomBLL.cs` is 10,444 lines in one class; `MastersController.cs` has 249 actions and 41 injected dependencies. 329 of 1,169 commits are merges, because everyone edits the same files.
- **Security failures** — plaintext password comparison, a second endpoint that accepts a user's *email address as their password*, zero CSRF protection across 61 controllers, three controllers with no `[Authorize]` at all, authorization enforced **only in JavaScript**, 158 string-interpolated `EXEC` SQL-injection sites, and an `ExecuteQuery` screen in the UI that runs arbitrary SQL for any logged-in user.
- **Performance** — of ~180 list grids, **12** are genuinely server-paged; ~149 download the entire table to the browser.
- **Ops** — live Azure SQL credentials committed to git, environments switched by commenting out lines in `appsettings.json`, no CI/CD, `<Optimize>False</Optimize>` on Release builds, and production running a branch that diverged from `main`.

**This plan is a greenfield build on a new clean schema.** It is not a port. Its organising principle: *every one of those failures must be structurally impossible in the new repo* — prevented by the compiler, an analyzer, an architecture test, or a CI gate. Not by discipline.

### Decisions already made
| | |
|---|---|
| Relationship to legacy | **Greenfield, no parity.** Build what the business needs now; legacy runs untouched alongside until each area is replaced. |
| Database | **New clean schema, EF Core code-first.** Migrations in git from commit #1. No stored procedures without an ADR. |
| UI kit | **TanStack Table + shadcn/ui + Tailwind.** Free, headless, owned in-repo. |
| Identity | **ASP.NET Core Identity inside the API.** HttpOnly cookie held by Next.js. |

### Immediate action, independent of this plan
The legacy Azure SQL administrator account — the one the old application connects with — has its password in that repository's git history permanently. **Rotate it now.** It is not fixed by deleting the line. The server and account are deliberately not named here; whoever owns that subscription already knows which one this is.

---

## 1. Stack (versions verified 2026-08-08)

| Layer | Choice | Version | Note |
|---|---|---|---|
| Runtime | .NET | **10.0.10 LTS** | Supported to 2028-11-14 |
| ORM | EF Core | **10** | Ships with .NET 10 |
| API | ASP.NET Core Minimal APIs | 10 | Not MVC controllers — see §3.2 |
| DB | SQL Server | **2025** (2022 acceptable) | Temporal tables, RLS, `SEQUENCE` |
| Frontend | Next.js | **16.2.7** | App Router, Turbopack default, `proxy.ts` |
| UI runtime | React | **19.2.8** | |
| Node | Node.js | **24 LTS** | |
| Package mgr | pnpm | 10.x | Workspaces |

Pin exact versions at scaffold time (`global.json` for the SDK, `Directory.Packages.props` for NuGet, `pnpm-lock.yaml` for npm). Verify with `dotnet --list-sdks` and `npm view next version` before the first commit.

---

## 2. Architecture decisions

**2.1 — Modular monolith with vertical slices. One deployable API.**
Not microservices: the domain is tightly coupled (BOM ↔ Inventory ↔ Procurement all touch the same aggregates), and distributed transactions would cost more than they buy at this team size. Not classic Clean/Onion layering either — the legacy already *had* `Domain/`, `Models/BLL/`, `Models/Database/` folders and they meant nothing, because a folder is not a boundary.

The boundary here is **the C# `internal` keyword**. Each module is its own `.csproj`; its application code — handlers, endpoints, validators — is `internal` apart from a small `Integration/` folder, and therefore *invisible to the compiler* from other modules. `BomBLL.cs` cannot happen again because nothing outside Engineering can call into it.

Entities are the exception, and a deliberate one: since [ADR 0002](adr/0002-one-dbcontext-for-the-application.md) they are public and live in `src/Erp.Persistence` beneath the modules, so that one `DbContext` can map all of them. That trades away some of the extraction story in §2.5's original form — it is the cost the ADR accepts.

**2.2 — Minimal APIs, one endpoint per file, not controllers.**
`MastersController.cs` reached 249 actions because a controller is an unbounded bucket. A file that holds exactly one endpoint has nowhere to grow, and two developers adding features touch two different files instead of one — which directly attacks the 329-merge-commit problem.

**2.3 — Next.js as a BFF; the browser never sees a token.**
All browser→API traffic goes through a catch-all Route Handler at `/api/[...path]` in Next.js, which attaches the access token from an HttpOnly `SameSite=Strict` cookie and forwards to .NET. Consequences: no CORS configuration at all (same origin), no token in JavaScript, no XSS token theft, and CSRF handled by SameSite + an origin check. Server Components call the .NET API directly server-to-server. The extra hop costs ~5–15ms on a LAN — irrelevant for an internal LOB app, and it removes an entire class of vulnerability.

**2.4 — Monorepo.**
One repo, one PR, one CI run. The OpenAPI→TypeScript codegen gate (§8) only works if a schema change and its client update land in the same commit — two repos make contract drift inevitable.

**2.5 — One `DbContext`, schema-per-area in one database.** *(revised 2026-08-09 — see [ADR 0002](adr/0002-one-dbcontext-for-the-application.md))*
`masters.*`, `identity.*`, and later `procurement.*`, `inventory.*`, `engineering.*` … all mapped by a single `ErpDbContext` in `src/Erp.Persistence`, with one `__EFMigrationsHistory`. Entities and their `IEntityTypeConfiguration` classes live there too — public, beneath the modules, because a context with typed `DbSet<T>` must see every entity at compile time and every handler must see the context. Modules keep their vertical slices; they no longer own their tables. A cross-module write is one `SaveChangesAsync` in one transaction; the outbox remains for integration, not for consistency.

**2.6 — Static navigation in code, permissions from the database.**
Legacy drove the menu from `ApplicationMaster`/`UMScreenMaster`/`UMControlMaster` rows that existed nowhere in source, which is a large part of why the feature surface was unknowable. New screens require a deploy anyway, so the nav tree is a typed config file; only *permissions* are data.

---

## 3. Repository structure — top level

```
erp/
├── .github/workflows/          # pr.yml, main.yml, nightly.yml
├── .editorconfig               # single style source for C# + TS
├── global.json                 # pins the .NET SDK
├── pnpm-workspace.yaml
├── docker-compose.yml          # sql-server, seq, azurite/minio, mailpit
├── backend/                    # .NET 10 solution
├── frontend/                   # Next.js 16 (pnpm package name: `web`)
├── packages/
│   ├── api-client/             # generated TS client + TanStack Query hooks
│   ├── ui/                     # shadcn primitives shared beyond `web` (optional)
│   └── tsconfig/               # shared tsconfig bases
├── db/
│   ├── seed/                   # idempotent reference-data scripts
│   ├── erd/                    # mermaid, GENERATED from the EF model — the schema, readable
│   └── programmability/        # the few functions/views that earn an ADR
├── tools/
│   └── DataMigration/          # legacy → new ETL console app (§10)
└── docs/
    ├── architecture.md
    ├── data-model.md           # module ownership + the target schema (intent, not truth)
    ├── adr/                    # 0001-modular-monolith.md, …
    └── runbooks/               # per-module jobs and failure modes
```

---

## 4. Backend structure

```
backend/
├── Erp.sln
├── Directory.Build.props           # analyzers, TreatWarningsAsErrors, nullable, LangVersion
├── Directory.Packages.props        # central package versions — no version in any .csproj
├── BannedSymbols.txt               # §7 — the banned API list
├── .editorconfig
│
├── src/
│   ├── Erp.Api/                            # the ONLY executable
│   │   ├── Program.cs                      # ~80 lines: discover modules, build, run
│   │   ├── Extensions/
│   │   │   ├── ModuleRegistration.cs       # assembly-scans IModule — no 105 AddScoped lines
│   │   │   ├── AuthenticationSetup.cs
│   │   │   ├── ObservabilitySetup.cs       # Serilog + OpenTelemetry
│   │   │   └── OpenApiSetup.cs
│   │   ├── Middleware/
│   │   │   ├── ProblemDetailsExceptionHandler.cs
│   │   │   ├── CorrelationIdMiddleware.cs
│   │   │   └── IdempotencyMiddleware.cs
│   │   ├── appsettings.json                # NON-SECRET values only
│   │   └── Dockerfile
│   │
│   ├── Erp.Contracts/                      # the public wire shape → OpenAPI → TS client
│   │   ├── Common/       ProblemTypes.cs, PagedResult.cs, CursorPage.cs, SortSpec.cs
│   │   ├── Masters/      PartDto.cs, SupplierDto.cs, …
│   │   ├── Procurement/  PurchaseOrderDto.cs, …
│   │   └── Inventory/    GoodsReceiptDto.cs, StockBalanceDto.cs, …
│   │
│   ├── shared/
│   │   ├── Erp.SharedKernel/               # zero dependencies
│   │   │   ├── Results/         Result.cs, Result{T}.cs, Error.cs
│   │   │   ├── Primitives/      Entity.cs, AggregateRoot.cs, ValueObject.cs, StronglyTypedId.cs
│   │   │   ├── ValueObjects/    Money.cs, Quantity.cs, Gstin.cs, HsnCode.cs, DocumentNumber.cs
│   │   │   ├── Events/          IDomainEvent.cs, IIntegrationEvent.cs
│   │   │   └── Time/            IClock.cs (TimeProvider wrapper)
│   │   ├── Erp.BuildingBlocks.Application/
│   │   │   ├── Cqrs/            ICommand.cs, IQuery.cs, IHandler.cs, dispatcher
│   │   │   ├── Behaviors/       ValidationBehavior, LoggingBehavior, TransactionBehavior
│   │   │   ├── Querying/        QueryMap.cs, PageRequest.cs, QueryableExtensions.cs
│   │   │   └── Abstractions/    ICurrentUser.cs, IBusinessUnitContext.cs
│   │   ├── Erp.BuildingBlocks.Persistence/
│   │   │   ├── DependencyInjection/  AddErpPersistence, AddErpInterceptors
│   │   │   ├── Interceptors/    AuditStampInterceptor, BusinessUnitStampInterceptor,
│   │   │   │                    SoftDeleteInterceptor, DomainEventDispatchInterceptor,
│   │   │   │                    UnboundedQueryGuardInterceptor
│   │   │   ├── Conventions/     SoftDeleteConvention, TenantFilterConvention,
│   │   │   │                    RowVersionConvention, DecimalPrecisionConvention
│   │   │   ├── Numbering/       INumberSeriesAllocator.cs, SqlSequenceAllocator.cs
│   │   │   └── Outbox/          OutboxMessage.cs, OutboxDispatcher.cs
│   │   └── Erp.BuildingBlocks.Web/
│   │       ├── IModule.cs                   # RegisterServices + MapEndpoints
│   │       ├── IEndpoint.cs
│   │       ├── ResultExtensions.cs          # Result<T> → ProblemDetails / 2xx
│   │       ├── PermissionFilter.cs
│   │       ├── ValidationFilter.cs          # auto-applied — cannot be forgotten
│   │       └── Export/  IExcelExporter.cs, IPdfRenderer.cs
│   │
│   ├── modules/
│   │   ├── Erp.Modules.Identity/            # users, roles, permissions, sessions, presence
│   │   ├── Erp.Modules.Masters/             # part, supplier, customer, BU, location, UoM, FY
│   │   ├── Erp.Modules.Engineering/         # BOM, machine → section → assembly hierarchy
│   │   ├── Erp.Modules.Procurement/         # PR, enquiry, negotiation, PO + revisions
│   │   ├── Erp.Modules.Inventory/           # ← fully expanded below
│   │   ├── Erp.Modules.Manufacturing/       # work order, job card, requisition, issue
│   │   ├── Erp.Modules.Planning/            # planner, SCM, stock comparison
│   │   ├── Erp.Modules.Quality/             # DCR, NCR, inspection, issue register
│   │   ├── Erp.Modules.Dispatch/            # packaging, box, vehicle
│   │   ├── Erp.Modules.Sales/               # enquiry, quotation, service
│   │   ├── Erp.Modules.Approvals/           # ONE workflow engine for all ~10 approval flows
│   │   ├── Erp.Modules.Documents/           # file storage, attachments, drawings
│   │   ├── Erp.Modules.Notifications/       # email, bell, SignalR
│   │   └── Erp.Modules.Reporting/           # PDF/Excel documents
│   │
│   └── Erp.Analyzers/                       # custom Roslyn rules ERP0001–ERP0006 (§7)
│
└── tests/
    ├── Erp.ArchitectureTests/               # the structure IS a test
    │   ├── ModuleBoundaryTests.cs
    │   ├── LayerDependencyTests.cs
    │   ├── EndpointConventionTests.cs
    │   └── PersistenceConventionTests.cs
    ├── modules/
    │   ├── Erp.Modules.Inventory.Tests/
    │   ├── Erp.Modules.Procurement.Tests/
    │   └── …                                # one per module, mirrors its structure
    ├── Erp.IntegrationTests/                # Testcontainers SQL Server + Respawn
    │   ├── ErpApiFactory.cs
    │   ├── DatabaseFixture.cs
    │   └── Scenarios/
    └── Erp.PerformanceTests/                # BenchmarkDotNet: BOM explosion, ledger posting
```

### 4.1 One module, fully expanded — `Erp.Modules.Inventory`

This is the pattern for every module. Note there is no `BLL/` folder: the unit of organisation is **the feature**, not the layer.

```
Erp.Modules.Inventory/
├── Erp.Modules.Inventory.csproj
├── InventoryModule.cs                  # IModule — self-registers services + endpoints
│
├── Domain/                             # pure C#. No EF, no ASP.NET, no I/O.
│   ├── StockLedger/
│   │   ├── StockLedgerEntry.cs         # append-only; corrections are reversing entries
│   │   ├── StockLedgerEntryId.cs
│   │   ├── MovementDirection.cs
│   │   ├── TxnType.cs
│   │   └── StockLedgerErrors.cs        # typed errors, not thrown strings
│   ├── Balances/
│   │   ├── StockBalance.cs             # derived, updated in the same transaction
│   │   └── Reservation.cs
│   ├── GoodsReceipts/
│   │   ├── GoodsReceipt.cs             # aggregate root
│   │   ├── GoodsReceiptLine.cs
│   │   ├── GoodsReceiptStatus.cs
│   │   └── Events/GoodsReceiptPosted.cs
│   ├── Challans/
│   ├── Adjustments/
│   └── Locations/
│
├── Application/                        # ONE FOLDER PER FEATURE SLICE
│   ├── GoodsReceipts/
│   │   ├── CreateGrn/
│   │   │   ├── CreateGrnCommand.cs
│   │   │   ├── CreateGrnHandler.cs
│   │   │   ├── CreateGrnValidator.cs   # FluentValidation — the only server truth
│   │   │   └── CreateGrnEndpoint.cs    # ONE endpoint. Declares its permission.
│   │   ├── PostGrn/                    # same 4 files
│   │   ├── GetGrnById/
│   │   ├── ListGrns/
│   │   │   ├── ListGrnsQuery.cs
│   │   │   ├── ListGrnsHandler.cs      # returns PagedResult<GrnListItemDto>
│   │   │   ├── ListGrnsQueryMap.cs     # allow-listed sort/filter columns (§7 #6)
│   │   │   └── ListGrnsEndpoint.cs
│   │   └── ExportGrns/                 # reuses ListGrnsQueryMap — export == screen
│   ├── StockLedger/
│   │   ├── PostMovement/
│   │   ├── ReverseEntry/
│   │   └── GetPartLedger/              # cursor-paged, not offset — it's unbounded
│   ├── Adjustments/
│   ├── Transfers/
│   └── Abstractions/IStockPostingService.cs
│
├── Infrastructure/
│   │                                   # no DbContext — see ADR 0002; entities and
│   │                                   # configurations live in src/Erp.Persistence
│   │   ├── StockLedgerEntryConfiguration.cs
│   │   ├── StockBalanceConfiguration.cs
│   │   └── GoodsReceiptConfiguration.cs
│   ├── Migrations/                     # committed from commit #1, expand-only
│   ├── Services/StockPostingService.cs
│   └── Jobs/DailyStockSnapshotJob.cs   # Hangfire, [DisableConcurrentExecution], idempotent
│
└── Integration/                        # the ONLY public types in this assembly
    ├── IInventoryApi.cs                # how Procurement/Manufacturing ask for stock
    ├── Events/
    │   ├── GoodsReceivedIntegrationEvent.cs
    │   └── StockReservedIntegrationEvent.cs
    └── Dtos/StockAvailabilityDto.cs
```

**The stock ledger is the highest-risk design in the system.** Get it right first:

```
inventory.StockLedgerEntry   -- APPEND ONLY. No UPDATE grant, no DELETE grant.
  Id bigint identity, BusinessUnitId, FinancialYearId, PartId, LocationId, BinId, LotNo?,
  TxnTypeCode, DirectionSign (+1/-1), Quantity decimal(18,6), Rate decimal(18,4),
  SourceDocumentType, SourceDocumentId, SourceLineId,
  PostedAtUtc, PostedByUserId, ReversalOfEntryId?, CorrelationId
  -- clustered (BusinessUnitId, PartId, LocationId, Id); columnstore secondary for reporting

inventory.StockBalance       -- derived; updated in the SAME transaction, UPDLOCK on the row
  (BusinessUnitId, PartId, LocationId, BinId) → QtyOnHand, QtyReserved, QtyAvailable,
   LastEntryId, RowVersion
```

Invariant, asserted as a **property-based test in CI**: for any random sequence of postings, `SUM(Quantity * DirectionSign)` from the ledger equals `StockBalance.QtyOnHand`. Always.

---

## 5. Frontend structure

```
frontend/
├── package.json
├── next.config.ts
├── tsconfig.json                   # "strict": true, no implicit any
├── proxy.ts                        # Next 16 (was middleware.ts) — auth guard on (app)/*
├── components.json                 # shadcn config
├── orval.config.ts                 # OpenAPI → typed client + TanStack Query hooks
├── playwright.config.ts
├── vitest.config.ts
│
├── src/
│   ├── app/
│   │   ├── layout.tsx
│   │   ├── globals.css
│   │   ├── (auth)/
│   │   │   ├── layout.tsx
│   │   │   └── login/page.tsx
│   │   ├── (app)/
│   │   │   ├── layout.tsx           # shell: sidebar, topbar, BU switcher, FY indicator
│   │   │   ├── page.tsx             # dashboard
│   │   │   ├── masters/
│   │   │   │   ├── parts/
│   │   │   │   │   ├── page.tsx             # list
│   │   │   │   │   ├── new/page.tsx
│   │   │   │   │   ├── [id]/page.tsx        # detail
│   │   │   │   │   ├── [id]/edit/page.tsx
│   │   │   │   │   └── import/page.tsx      # staged Excel import
│   │   │   │   ├── suppliers/ customers/ locations/ uom/
│   │   │   ├── procurement/         # purchase-requests/ purchase-orders/
│   │   │   ├── inventory/           # goods-receipts/ stock/ adjustments/ transfers/
│   │   │   ├── engineering/ manufacturing/ quality/ dispatch/ sales/
│   │   │   ├── reports/
│   │   │   └── admin/               # users/ roles/ permissions/ sessions/ numbering/
│   │   └── api/
│   │       ├── auth/[action]/route.ts    # login/logout/refresh → sets HttpOnly cookie
│   │       └── [...path]/route.ts        # BFF catch-all proxy to .NET (§2.3)
│   │
│   ├── features/                    # colocated per domain feature
│   │   └── inventory/
│   │       └── goods-receipts/      # ← fully expanded below
│   │
│   ├── components/
│   │   ├── ui/                      # shadcn primitives — owned, in-repo, editable
│   │   ├── data-table/              # THE shared server-paged table kit — build once
│   │   │   ├── data-table.tsx
│   │   │   ├── use-server-table.ts  # URL search-params ↔ server query. Shareable links.
│   │   │   ├── data-table-toolbar.tsx
│   │   │   ├── data-table-pagination.tsx
│   │   │   ├── data-table-column-header.tsx
│   │   │   ├── data-table-faceted-filter.tsx
│   │   │   ├── data-table-view-options.tsx
│   │   │   └── data-table-export.tsx    # calls the server export endpoint, never the client
│   │   ├── form/                    # react-hook-form + Zod field kit
│   │   ├── layout/                  # app-shell, sidebar, breadcrumbs, bu-switcher
│   │   └── permission/
│   │       ├── can.tsx              # <Can permission="inventory.grn.create">
│   │       └── use-permissions.ts   # DISPLAY ONLY — the server always re-checks
│   │
│   ├── lib/
│   │   ├── api/
│   │   │   ├── generated/           # orval output — committed, CI dirty-checks it
│   │   │   ├── fetcher.ts
│   │   │   └── problem-details.ts   # RFC 9457 → toast / field errors
│   │   ├── auth/session.ts          # server-only cookie read
│   │   ├── query-client.ts
│   │   └── format.ts                # en-IN money, qty, dates — one place
│   │
│   ├── config/
│   │   ├── nav.ts                   # typed nav tree, each item declares a permission
│   │   └── site.ts
│   └── messages/en-IN.json          # next-intl from day 1, even though English-only
│
├── e2e/                             # Playwright
└── tests/                           # Vitest + Testing Library
```

### 5.1 One feature, fully expanded

```
src/features/inventory/goods-receipts/
├── components/
│   ├── grn-table.tsx            # composes <DataTable> + useServerTable
│   ├── grn-form.tsx             # react-hook-form + zodResolver
│   ├── grn-lines-editor.tsx
│   ├── grn-status-badge.tsx
│   └── grn-detail-header.tsx
├── hooks/
│   ├── use-grn-columns.tsx      # ColumnDef<GrnListItemDto>[]
│   └── use-post-grn.ts
├── schemas/
│   └── grn-form-schema.ts       # Zod — mirrors CreateGrnValidator.cs
└── api.ts                       # thin re-export of generated hooks + query keys
```

**The `data-table` kit is the single highest-leverage frontend component.** Legacy shipped 149 grids that download the whole table. Here, `useServerTable` is the *only* sanctioned way to build a list: it reads paging/sort/filter from the URL, sends them to a `PagedResult` endpoint, and there is no code path that fetches an unbounded array. Budget 3–4 weeks to build it properly before any feature screen is written.

---

## 6. Database conventions

Applied **by convention in `ErpDbContext.OnModelCreating`** (`src/Erp.Persistence`), iterating all entity types — never per-entity, so they cannot be forgotten:

| Concern | Mechanism |
|---|---|
| Tenancy (`BusinessUnitId`) | `IBusinessUnitScoped` → global query filter auto-applied + a stamping interceptor + **SQL Server Row-Level Security** as defence in depth for anything bypassing EF |
| Soft delete | `ISoftDeletable` → global query filter |
| Audit | `IAuditable` → interceptor stamps Created/Modified By+AtUtc from `ICurrentUser` |
| Concurrency | `byte[] RowVersion` on every aggregate root → 409 + ProblemDetails |
| History | SQL Server **temporal tables** (`IsTemporal()`) on masters and transaction headers |
| Money / Qty | `decimal(18,4)` / `decimal(18,6)` by convention. `float`/`double` are **banned symbols** |
| Time | UTC only, `datetime2(7)`. `DateTime.Now`/`UtcNow` banned → inject `TimeProvider` |
| Doc numbering | SQL `SEQUENCE` per series per FY, or atomic `UPDATE … OUTPUT`. **200-way concurrency test in CI proves zero duplicates** — this is the legacy bug users notice most |
| Financial year | A real `FinancialYear` entity with Open/Closed state and period locking. April–March is *configuration*, not a hard-coded constant |
| Stored procedures | **None** without an ADR. If one is justified, its `.sql` lives in `db/programmability/` and is applied by a migration |
| Schema documentation | `db/erd/` is **generated** from the EF model by the architecture tests — one mermaid diagram per schema, discovered by type so a new module appears without anyone editing a diagram. The target model, which no generator can know, is `docs/data-model.md` |

---

## 7. The 16 legacy failures → the mechanism that prevents each

Every row is enforceable by a machine. None is "we'll be careful."

| # | Legacy failure | Mechanism in the new repo |
|---|---|---|
| 1 | Zero tests | xUnit + **Testcontainers** (real SQL Server) + **Respawn** + **Verify** snapshots + Playwright. CI coverage gate: module ≥70%, `Domain/` ≥90%. ≥1 integration test per write endpoint. |
| 2 | DB is undocumented truth | EF migrations from commit #1 + CI gate `dotnet ef migrations has-pending-model-changes`. `db/erd/` regenerated on every schema change. Stored procs require an ADR. |
| 3 | God classes | Analyzer **ERP0001**: file >400 lines warns, >800 errors. One endpoint per file. One feature per folder. |
| 4 | Fake layering | **NetArchTest** suite: `Domain` may not reference EF or ASP.NET; module A may not reference module B outside `Integration/`. Module internals are `internal` — the *compiler* enforces it. |
| 5 | `dynamic` + entities on the wire | `dynamic` in `BannedSymbols.txt`. Arch test: no `Domain` type may appear in any endpoint signature — only `Erp.Contracts`. |
| 6 | SQL injection + arbitrary-SQL screen | `FromSqlRaw`, `ExecuteSqlRaw`, `SqlCommand` **banned**. Dynamic filtering goes through `QueryMap` — an allow-list of column→expression, which also guarantees every sortable column has an index. No `ExecuteQuery` screen, ever. |
| 7 | Plaintext passwords, JS-only authz | ASP.NET Core Identity (PBKDF2/Argon2). `FallbackPolicy = RequireAuthenticatedUser` — a forgotten attribute now fails closed. Analyzer **ERP0002**: every endpoint must declare `.RequirePermission(...)` **at compile time**. Permissions are C# constants → seeded → returned to the UI for display only. |
| 8 | Envelope + HTTP 200 on error | **RFC 9457 ProblemDetails** everywhere. Handlers return `Result<T>`; one filter maps it to the correct status. Global handler logs `ex` with a correlation id and returns **the id, never the message**. |
| 9 | 149 client-side grids, N+1, sync-over-async | Analyzer **ERP0005**: list endpoints must return `PagedResult<T>`/`CursorPage<T>`. `.Result`, `.Wait()`, `SaveChanges()` banned. `UnboundedQueryGuardInterceptor` throws above N rows without paging. `NoTracking` default. |
| 10 | Validation triplicated, authoritative nowhere | **FluentValidation** as the single server truth, auto-wired via `ValidationFilter` on every endpoint by convention. Zod on the client mirrors it. DB constraints are the last line. |
| 11 | Credentials in git | Zero secrets in the repo. `dotnet user-secrets` in dev; env vars / Key Vault in prod. **gitleaks** runs on every PR. `appsettings.json` holds non-secret values only. |
| 12 | No CI/CD, divergent branches | GitHub Actions from commit #1 (§8). Trunk-based, short-lived branches, required checks, linear history. `<Optimize>` never touched. |
| 13 | No frontend toolchain | pnpm + committed lockfile, Turbopack, TS strict, ESLint + Prettier, Renovate. **No CDN scripts.** Hashed immutable assets. |
| 14 | Duplicate implementations (`GRN` vs `GRNV2` vs Areas) | One module owns one concept; arch test rejects duplicate feature-slice names. **No `V2` class suffixes** — versioning lives in the route (`/api/v1/…`). PR template asks: *"deleted the thing this replaces?"* |
| 15 | Logs into the OLTP database | **Serilog + OpenTelemetry** (traces/metrics/logs) → Seq locally, Grafana/App Insights in prod. Correlation id on every request. `Console.WriteLine` banned. |
| 16 | No contract, no type sharing | API emits OpenAPI at build → **orval** generates the TS client + TanStack Query hooks → CI fails if the generated output is dirty. `Asp.Versioning` for API versions. |

### Cross-cutting choices these imply
- **Background jobs** — Hangfire (SQL Server storage, dashboard the ops team can actually look at, `[DisableConcurrentExecution]`, retries) for the nightly stock snapshot and email scheduler.
- **Approvals** — one `Approvals` module (`ApprovalRequest` + `ApprovalPolicy` with value bands and role rules). Replaces ~10 hand-rolled flows. Prove it on Part/Supplier/Customer approval before BOM depends on it.
- **Files** — `IDocumentStore` → Azure Blob / MinIO in prod, filesystem in dev. Content-addressed keys, original filename as metadata only, extension allow-list, size cap, short-lived signed URLs. **Never under `wwwroot`.**
- **PDF/Excel** — QuestPDF (verify the licence threshold) with Verify snapshot tests; ClosedXML behind one `IExcelExporter<T>` that consumes the same `QueryMap` as the grid, so export always matches the screen and always streams server-side.
- **Real-time** — SignalR for bell notifications and the session-presence subsystem (the one part of legacy worth reproducing behaviourally).
- **Idempotency** — `Idempotency-Key` header required on POST, backed by a table. Kills double-submit duplicates.
- **Caching** — .NET `HybridCache` with tag invalidation for reference data.

---

## 8. CI/CD and quality gates

`.github/workflows/pr.yml` — every one of these blocks merge:

1. `dotnet format --verify-no-changes` + `pnpm lint`
2. `dotnet build -warnaserror` (analyzers ERP0001–ERP0006 active)
3. `Erp.ArchitectureTests` — the structure is a test
4. Unit tests + coverage thresholds
5. Integration tests (Testcontainers SQL Server, Respawn between tests)
6. **Migration drift**: `dotnet ef migrations has-pending-model-changes` must be clean
7. **Contract drift**: regenerate OpenAPI + orval client, `git diff --exit-code`
8. `pnpm build` + `pnpm test` + Playwright smoke
9. **gitleaks**

`Directory.Build.props` sets `TreatWarningsAsErrors`, `Nullable=enable`, `EnforceCodeStyleInBuild`, `AnalysisLevel=latest-all`, and wires `Erp.Analyzers` + `BannedSymbols.txt` into every project. A rule that is not in CI is not a rule.

---

## 9. Delivery sequence

Because this is greenfield-no-parity, sequence by **business value**, not by legacy screen count — but platform first, because everything inherits from it.

**Phase 0 — Platform (4–6 weeks).** Repo skeleton, `Directory.Build.props`, analyzers, arch tests, CI, Docker compose, Identity, `ErpDbContext` + all conventions, `QueryMap`, `PagedResult`, `ProblemDetails`, numbering allocator, outbox, `data-table` kit, form kit, BFF proxy, orval pipeline.

**Phase 0.5 — Vertical proof (1–2 weeks).** *Before* scaling out: one complete slice end-to-end — login → parts list (server-paged, filtered, exported) → create part → approve part. It exercises every layer, every guardrail, and every generated artefact. Fix the structure here, while it costs nothing.

**Phase 1 — Identity + Masters.** Parts, suppliers, customers, business units, locations, UoM, financial year, numbering series, roles/permissions, approvals engine, document store, staged Excel import. Nothing else can be built until these exist.

**Phase 2 — Procurement + Goods Receipt + Stock Ledger.** Highest transaction volume and clearest ROI. The ledger design in §4.1 is the riskiest thing in the program — assign your two strongest engineers and write the property-based invariant test before the feature code.

**Phase 3 — Engineering / BOM.** Model `BomRevision` as **immutable once approved**; an amendment creates revision N+1 with a computed diff, so *"which BOM was this machine built to?"* becomes answerable — it currently is not. Cycle detection is a domain invariant with a unit test, not a runtime surprise.

**Phase 4+ — Planning/SCM, Manufacturing, Quality, Dispatch, Sales, Reporting** — resequence by business priority.

**Phase 9 — the real gaps.** Legacy has no Sales Order, no invoicing, no Finance, no HR. Sales Order is the biggest missing link (it closes Quotation → SO → Dispatch → Invoice). For Finance, integrate with Tally/Zoho/Busy behind an anti-corruption layer before considering building a GL — double-entry accounting is a year-long project on its own.

**Data:** even greenfield needs opening master data. `tools/DataMigration/` is an idempotent, re-runnable console ETL (legacy → new) for parts, suppliers, customers, locations and opening stock balances. Run it repeatedly against a staging DB throughout Phases 1–2; do not treat it as a one-shot cutover script.

---

## 10. Verification

The plan is working if these hold at the end of Phase 0.5 — check them explicitly:

1. **`docker compose up` then `dotnet run`** brings up SQL Server, Seq, and MinIO, applies migrations automatically, and serves the API. **`pnpm dev`** serves the web app against it. A new developer is productive in under 30 minutes with no manual DB steps.
2. **`dotnet test`** runs unit + architecture + integration tests green, with Testcontainers provisioning a real SQL Server.
3. **Deliberately break each guardrail and confirm the build fails.** Add a 900-line file (ERP0001). Add an endpoint without `.RequirePermission` (ERP0002). Call `FromSqlRaw` (banned symbol). Return `List<T>` from a list endpoint (ERP0005). Reference `Erp.Modules.Masters.Domain` from Inventory (arch test). Change an entity without a migration (drift gate). Commit a fake AWS key (gitleaks). **If any of these passes CI, the guardrail is decorative** — that is exactly how the legacy `Domain/` folder ended up with one file in it.
4. **Run the numbering concurrency test** — 200 parallel allocations, assert zero duplicate document numbers.
5. **Exercise the parts list in the browser**, confirm via the network tab that filtering and paging issue *server* requests and that no response contains the full table, and confirm the URL is shareable and restores grid state.
6. **Confirm the browser never holds a token** — check `document.cookie` and `localStorage` are empty of credentials; the session cookie must be HttpOnly.
7. **Call a protected endpoint directly** (bypassing the UI) without permission and confirm a 403 with a ProblemDetails body containing a correlation id and **no exception message**.
