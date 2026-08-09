# Build status — Phase 0

Last updated: 2026-08-09

## Repository layout

```
backend/        .NET 10 solution (Erp.slnx)
frontend/       Next.js 16 app; pnpm package name `web`
db/erd/         generated mermaid ERDs, one per schema
docs/           architecture, ADRs, data model, runbooks
docker-compose.yml
```

## Verified working

Everything here was built and run, not just written.

| | Evidence |
|---|---|
| Backend Release build | `dotnet build backend/Erp.slnx -c Release` — succeeds |
| Domain unit tests | **47/47** pass |
| Architecture tests | **19/19** pass, and verified to fail against a deliberately non-compliant endpoint |
| Integration tests | **40/40** pass against a real SQL Server in a container |
| Banned symbols | Probed: `DateTime.Now`, `DateTime.UtcNow`, `Console`, `Task.Result`, `Task.Wait()`, `decimal.Parse(string)` all fail the build with their custom messages |
| Format gate | `dotnet format whitespace` and `dotnet format style` clean |
| Schema drift gate | Both contexts report "No changes have been made to the model since the last migration" |
| Schema documentation | `db/erd/` regenerates from the EF model — `masters` (10 tables, 5 foreign keys) and `identity` (7 tables, 6 foreign keys). Byte-identical on a second run, and both diagrams parse as mermaid `erDiagram` |
| Frontend | `tsc --noEmit` clean; `next build` succeeds, `/api/[...path]` emitted as dynamic |
| Infrastructure | `docker compose up -d` — sqlserver, seq, minio, mailpit all healthy |

Resolved versions: .NET **10.0.10** · EF Core **10.0.10** · Next.js **16.3.0** ·
React **19.2** · Node **24.18** · pnpm **10.34** · Docker **29.6.2**.

## What the integration tests actually prove

Not that the code compiles — that a real database enforces the guarantees:

- An unauthenticated request is rejected (fallback authorization policy).
- A user without the permission gets 403, server-side, with no exception detail.
- A part created in business unit 1 is **invisible** to business unit 2, on both
  the detail and list endpoints — via the global query filter, which no handler asks for.
- The same part number may exist in two business units; the unique index is tenant-scoped.
- A list of 30 rows returns 10; `pageSize=100000` is clamped to 200.
- Paging is stable across pages (the mandatory tie-breaker).
- Sorting or filtering on an undeclared field is rejected with 400, not passed to SQL.
- Audit columns are stamped with no handler setting them.
- A stale `rowVersion` yields 409 and the first writer's value survives.
- A part cannot be edited while awaiting approval.
- Sign-in failures do not distinguish an unknown user from a wrong password.
- A sub-assembly cannot be filed under a section, and a section cannot be given a
  parent — the level rules hold over HTTP, not only in the aggregate.
- Holding `masters.section.*` does **not** grant `masters.assembly.*`, even though
  both levels live in one table; and a node is 404 through another level's route.
- A parent part's weight and amount totals are computed from its component lines,
  and a client-supplied `amount` is ignored rather than stored.
- A part cannot be a component of itself, cannot appear twice on one build, and a
  build cannot name a part that does not exist.

## The four conditional masters

Section, Assembly, Sub-assembly and Parent part, ported from the legacy
`Assembly` and `AssemblyMaster` screens. What changed, and why:

| Legacy | Here |
|---|---|
| `Assembly` table, level as `'S'`/`'A'`/`'SA'` free text | One `AssemblyNode` table, level stored as its enum name — `'SA'` starts with `'S'`, which is why the legacy code that counted sections with `StartsWith("S")` counted sub-assemblies too |
| Parent held as the parent's **code**, no foreign key; a section pointed at the literal `"000"` | Real self-referencing FK, `null` at the top of the tree |
| Level rules in three save methods that had already drifted | One table of allowed parents (`AssemblyLevels`), applied in one place |
| Codes generated as `"S" + (max(parsed tail) + 1)`, evaluated in memory over every row | Codes entered by the user, unique per business unit across all three levels. **No numbering logic is reproduced** — the allocator is a deferred decision, and `AssemblyNode.Create` documents the seam it plugs into |
| `AssemblyMaster` header and lines in one table, told apart by a null child column, mirrored into a second `AssemblyPartMaster` table nothing kept in step | `ParentPart` header with `ParentPartComponent` children, one aggregate, replaced and saved in one transaction |
| Both sides of the build stored as part **numbers** in free text | Real foreign keys onto `Part` |
| Totals recalculated onto whichever row carried the parent's number in its *child* column — usually no row | Rolled up by the aggregate whenever the lines change; the only writer of the two total columns |
| Line `amount` taken from the browser and then summed into the header | Computed server-side from quantity × rate; the posted value is ignored |
| No duplicate-child, self-reference or quantity check | All three enforced, with the offending part named in the error |

These are the first foreign keys in the `masters` schema — see the note below on
what the ERD found. Every list is server-paged through the same `QueryMap`
allow-list as the other masters, every dropdown reads from `LookupValue`, and the
parent and part pickers search the server rather than loading a master into a
`<select>`.

## Bugs found by the tests during this build

Four, each of which would have shipped silently:

1. **Module discovery returned nothing.** `Assembly.GetReferencedAssemblies()` omits
   project references the host never names in code — which is the entire point of the
   design — so the compiler elided them. The API would have booted with zero endpoints
   and no error. Caught by the two "guard the guard" tests. Discovery now probes the
   output directory.
2. **`[AsParameters]` could not bind the `PageRequest` record** — every list request
   returned an empty 400. List endpoints now declare explicit query parameters, which
   also document individually in OpenAPI.
3. **`p.Id.Value` on a strongly-typed ID is untranslatable.** EF maps `PartId` to a
   column, but `.Value` is a member of the CLR struct and not part of the mapping, so
   every list and detail query threw. Projections now select the id and unwrap after
   materialisation.
4. **EF did not discover the interceptors from DI.** Every row was written with
   `BusinessUnitId = 0` and no audit stamp — rows invisible to the tenant filter that
   had just created them. Interceptors are now attached explicitly per module, which
   also fixes their order.

A fifth finding was a UX one: the validator rejected a part number with surrounding
whitespace before the domain could trim it. Format rules now run against the trimmed
value, matching what is actually stored.

## Found by generating the ERD

**The `masters` schema declared no foreign keys at all** — no `HasOne`/`WithMany`, no
navigation properties, none in any migration. `Part.CategoryId`, `Employee.RoleId`,
`Employee.SiteId` and `Role.RoleId` are unconstrained columns, and two of them point at
tables that do not exist. The generated diagram rendered six disconnected boxes, which is
the diagram doing its job: a hand-drawn one would have shown the relationships someone
intended and hidden this entirely.

**Partly fixed.** The four conditional masters added the schema's first five real
constraints — `AssemblyNode → AssemblyNode`, `ParentPart → Part`,
`ParentPart → AssemblyNode`, `ParentPartComponent → ParentPart` (cascade) and
`ParentPartComponent → Part`. They are declared without navigation properties, so
a list handler cannot accidentally pull a graph per row. The four columns named
above are still unconstrained and still tracked in `docs/data-model.md` §6.

Each diagram therefore ends with a generated list of columns that name a relationship the
database does not enforce. Some entries are deliberate and explained in
`db/erd/README.md` — audit columns and anything crossing into another module's schema
cannot be constrained. The rest are tracked in `docs/data-model.md` §6 and are worth
fixing before Phase 2 adds data that references masters.

## Deliberate deviations from the plan

**Roslyn analyzers ERP0001–ERP0006 replaced by architecture tests.** Inspecting the
real `EndpointDataSource` beats analysing syntax — a syntax analyzer is defeated by an
endpoint mapped through a helper or a loop — and carries no Roslyn version coupling.

**The `T:System.Single` banned symbol was removed.** The analyzer does not match
predefined type keywords, so it looked like a guardrail while never firing — the same
"inert flag" pattern the legacy audit found in grids declaring `remoteOperations: true`
over an in-memory array. Enforced instead against the real EF model.

**Suppressed rules**, each documented at the point of suppression: CA1716, CA1000
(library-author API-shape rules, irrelevant to a closed application); CA1707, CA1711,
xUnit1051 in test projects only; `RS0030` downgraded to a warning in tests. EF
migrations are marked `generated_code` and exempt from style and boundary rules.
Nothing describing a correctness or security risk is suppressed.

## Not yet built — deferred to Phase 1

Named here so they are not mistaken for oversights:

- **Document numbering allocator** — `NumberSeries`, atomic `UPDATE … OUTPUT`, and the
  200-way concurrency test proving zero duplicates. Parts use user-entered numbers.
- **Transactional outbox** — one module exists, so there is nothing to publish to.
- **Approvals module** — the shared engine. Part approval is a state machine on the
  aggregate, which is the right shape to generalise from.
- **Document store** (`IDocumentStore` over MinIO) and the Category master.
- **orval OpenAPI → TypeScript generation.** `frontend/src/lib/api/types.ts` is
  hand-written and marked as such; the `Client contract drift` CI job fails until
  `orval.config.ts` exists.
- **Login page, app shell, navigation.** The BFF proxy, `data-table` kit and parts list
  exist; the surrounding chrome does not.
- **Playwright end-to-end tests.**

## Outstanding, independent of this work

**Rotate the legacy Azure SQL administrator password.** It is in the legacy
repository's git history permanently; deleting the line did not fix it. The server
and account are not named here on purpose — see `docs/architecture.md` §Context.
