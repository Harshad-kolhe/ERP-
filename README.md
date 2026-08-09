# ERP

Modular-monolith ERP for engineer-to-order machine manufacturing.
.NET 10 · EF Core 10 · Next.js 16 · React 19 · SQL Server.

Architecture and the reasoning behind it: [`docs/architecture.md`](docs/architecture.md).
Decision records: [`docs/adr/`](docs/adr/).

---

## Prerequisites

| Tool | Version | Check |
|---|---|---|
| .NET SDK | 10.0.302+ | `dotnet --list-sdks` |
| Node.js | 24 LTS | `node --version` |
| pnpm | 10.x | `corepack enable pnpm` |
| Docker Desktop | latest | `docker --version` |
| EF Core tools | 10.x | `dotnet tool install --global dotnet-ef` |

Docker is required for local SQL Server and for the integration test suite
(Testcontainers). Without it you can still build and run unit + architecture
tests, but not integration tests.

## Getting started

```bash
# 1. Infrastructure — SQL Server, Seq, MinIO, Mailpit
docker compose up -d

# 2. Backend. Migrations are applied automatically on startup in Development.
cd backend
dotnet restore
dotnet run --project src/Erp.Api          # https://localhost:5081

# 3. Frontend
cd ..
pnpm install
pnpm --filter web dev                      # http://localhost:3000
```

The pnpm package is named `web` and lives in `frontend/`, so workspace commands
are `pnpm --filter web <script>`.

A machine that has nothing on it yet, Windows and SQL Server without Docker, and
recreating the database from scratch: [`docs/local-setup.md`](docs/local-setup.md).

Default sign-in for a freshly seeded database is written to the console on
first run. There are no credentials in this repository.

| Service | URL |
|---|---|
| Web app | http://localhost:3000 |
| API | http://localhost:5080 (http) / https://localhost:5081 |
| OpenAPI UI | https://localhost:5081/scalar |
| Seq (logs) | http://localhost:8081 |
| MinIO console | http://localhost:9001 |
| Mailpit (email) | http://localhost:8025 |

## Configuration and secrets

**No secret ever enters this repository.** `appsettings.json` holds non-secret
defaults only; `gitleaks` runs on every pull request.

```bash
cp .env.example .env      # gitignored; .env.example is the committed list of variables
```

`Program.cs` reads `.env` into environment variables before the configuration
builder runs, and a real environment variable always wins over the file — so CI
and every deployed environment behave as if it were absent. `dotnet user-secrets`
remains available for values that must not sit in the repository folder at all.

Every other environment supplies configuration through environment variables
(`ConnectionStrings__Erp`) or Azure Key Vault. Environments are selected with
`ASPNETCORE_ENVIRONMENT`, never by editing a config file.

Full setup, both platforms: [`docs/local-setup.md`](docs/local-setup.md).

## Common tasks

```bash
dotnet build backend/Erp.slnx              # analyzers + banned symbols are build errors
dotnet test  backend/Erp.slnx              # unit + architecture + integration

# Formatting. Use the whitespace/style subcommands, not bare `dotnet format`:
# the latter also tries to auto-fix analyzer diagnostics and exits non-zero on
# any that have no code fix (RS0030, the banned-symbol rule, has none).
dotnet format whitespace backend/Erp.slnx
dotnet format style backend/Erp.slnx

pnpm lint                                  # eslint + prettier
pnpm test                                  # vitest
pnpm --filter web exec playwright test     # e2e

# Add a migration. One DbContext for the application — see docs/adr/0002.
# Run from backend/.
dotnet ef migrations add <Name> \
  --project src/Erp.Persistence \
  --startup-project src/Erp.Api \
  --context ErpDbContext \
  --output-dir Migrations

# Regenerate the TypeScript client from OpenAPI
pnpm --filter web generate:api

# Regenerate db/erd/ from the EF model. A plain `dotnet test` does this too.
dotnet test backend/tests/Erp.ArchitectureTests --filter FullyQualifiedName~SchemaDiagram
```

## Repository layout

```
backend/        .NET 10 solution — the only deployable backend
frontend/       Next.js 16 app; also the BFF that holds the session cookie
packages/       shared TypeScript (generated API client, tsconfig bases)
db/             seed data, generated ERDs, and the few SQL objects that earn an ADR
tools/          data-migration ETL from the legacy system
docs/           architecture, ADRs, data model, runbooks
```

The diagrams in [`db/erd/`](db/erd) are generated from the EF Core model, so they
describe the schema that exists rather than the one someone last drew. Where it is
*going* — module ownership and the target model — is
[`docs/data-model.md`](docs/data-model.md).

Inside `backend`, each module is its own project. Everything in a module is
`internal` except its `Integration/` folder — the compiler, not convention, is
what keeps modules apart. See [`docs/architecture.md`](docs/architecture.md).

## Quality gates

Every pull request must pass, and each gate maps to a specific failure in the
system this replaces:

| Gate | Prevents |
|---|---|
| `dotnet format --verify-no-changes` | style drift |
| `dotnet build -warnaserror` + banned symbols | raw SQL, `dynamic`, blocking calls |
| architecture tests | god classes, module boundary violations |
| coverage: module ≥70%, `Domain/` ≥90% | untested business rules |
| `dotnet ef migrations has-pending-model-changes` | schema drifting away from source |
| OpenAPI/client dirty check | server and client types diverging |
| gitleaks | credentials in git history |

A rule that is not in CI is not a rule.
