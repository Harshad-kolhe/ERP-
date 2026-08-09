# Local setup — running the app and recreating the database

For a developer who has just cloned the repository onto a machine that has
nothing on it yet. Covers macOS and Windows, because the database story differs
between them.

The short version: **the database schema is rebuilt by `dotnet run`.** There is
no SQL script to execute and no manual migration step. Everything below is
detail around that one fact.

---

## 1. Prerequisites

| Tool | Version | Check |
|---|---|---|
| .NET SDK | 10.0.302+ | `dotnet --list-sdks` |
| Node.js | 24 LTS | `node --version` |
| pnpm | 10.x | `corepack enable pnpm` |
| EF Core tools | 10.x | `dotnet tool install --global dotnet-ef` |
| Docker Desktop | latest | `docker --version` |

The SDK version is pinned in [`global.json`](../global.json); a newer patch is
fine, an older one is not.

**Docker is mandatory on macOS** — Microsoft has never shipped SQL Server for
macOS, so a local instance means a container. It is *optional* on Windows, where
SQL Server Developer or Express installs natively.

On both platforms Docker is required for the integration test suite, which uses
Testcontainers to start a throwaway SQL Server per run. Unit and architecture
tests run without it.

---

## 2. Start the database

### macOS

```bash
docker compose up -d sqlserver
```

That is the only service the application needs today. The other three in
[`docker-compose.yml`](../docker-compose.yml) are optional:

| Service | Status | If it is down |
|---|---|---|
| `seq` | wired as a Serilog sink | logs still go to the console; the sink fails silently |
| `minio` | not referenced by any code yet | nothing |
| `mailpit` | not referenced by any code yet | nothing |

Use `docker compose up -d` to start all four once documents and outbound email
are actually implemented.

The SA password in that file is a committed local-only throwaway. It exists so a
new developer starts with one command. Nothing else in the repository contains a
credential, and `gitleaks` runs on every pull request to keep it that way.

### Windows

Install SQL Server Developer or Express, then create an **empty** database in
SSMS:

```sql
CREATE DATABASE Erp;
```

Do not create tables, schemas, or anything else. The application builds all of
it.

This step is in fact optional — EF Core issues `CREATE DATABASE` itself when the
database is missing. Pre-creating it in SSMS is the right move only when your SQL
login lacks `CREATE DATABASE` permission, which is normal on a shared or
corporate server. Whichever way the database is created, the login the
application connects with needs `CREATE SCHEMA` and `CREATE TABLE` inside it —
`db_owner` on `Erp` covers it.

---

## 3. Give the API the connection string

Nothing in this repository contains one. This is the only manual configuration
step, and it is per-machine — it does not travel with a `git clone`.

Pick the connection string that matches how the database is running:

```
# Docker (macOS, or Windows with Docker)
Server=localhost,1433;Database=Erp;User Id=sa;Password=Local_Dev_Only_P@ssw0rd!;TrustServerCertificate=True

# Windows auth, default instance — what SSMS uses by default
Server=localhost;Database=Erp;Trusted_Connection=True;TrustServerCertificate=True

# SQL Express or a named instance
Server=localhost\SQLEXPRESS;Database=Erp;Trusted_Connection=True;TrustServerCertificate=True
```

### `.env` — the one to use

```bash
cp .env.example .env
```

Then edit the `ConnectionStrings__Erp` line to match. On macOS with the container
running, the value already in the example file is correct and there is nothing to
edit at all.

That is the whole step. Same command on macOS, Linux and Windows, in any shell —
no `source`, no `direnv`, no per-key command.

`.env` is gitignored, and `.env.example` is the committed list of every variable
a developer needs. When a new one is introduced, it goes into `.env.example` in
the same pull request, and everyone picks it up with a diff instead of a message.

**How it works.** [`Program.cs`](../backend/src/Erp.Api/Program.cs) calls
`Env.TraversePath().NoClobber().Load()` before `WebApplication.CreateBuilder`,
which reads `.env` into environment variables. Environment variables are a
first-class configuration provider in .NET, and `__` (double underscore) is the
section separator — so `ConnectionStrings__Erp` *is* the configuration key
`ConnectionStrings:Erp`. The same file therefore configures the frontend, docker
compose and the API, because all three read environment variables.

Two properties of that call matter:

- **`TraversePath`** — the file is found by walking up from the working
  directory, so one `.env` at the repository root serves `dotnet run` from
  `backend/` and `dotnet ef` from `backend/src/Erp.Api/`.
- **`NoClobber`** — a real environment variable always wins over a line in the
  file. CI and every deployed environment behave exactly as if no `.env` existed,
  and a stale file left on a server cannot override what the platform supplies.
  A missing file is not an error; that is the normal case everywhere but a
  laptop.

**Never put `ASPNETCORE_ENVIRONMENT` in `.env`.** `dotnet run` and `dotnet ef`
both take it from
[`launchSettings.json`](../backend/src/Erp.Api/Properties/launchSettings.json),
so nothing needs it there — and setting it breaks the build. `dotnet build`
starts the host to emit `contracts/openapi.json`, deliberately outside
Development so that reading a schema never applies a migration or seeds a user.
A value in `.env` reaches that process too, and the build starts writing to your
database.

### user-secrets — the alternative

Still supported, and unchanged: values live in `~/.microsoft/usersecrets/`, keyed
by the `UserSecretsId` in [`Erp.Api.csproj`](../backend/src/Erp.Api/Erp.Api.csproj),
outside the repository tree entirely.

```bash
cd backend/src/Erp.Api
dotnet user-secrets set "ConnectionStrings:Erp" "<connection string>"
dotnet user-secrets list                          # they are readable plain JSON, not encrypted
dotnet user-secrets remove "ConnectionStrings:Erp"
```

Prefer it when a value must not sit inside the repository folder even
gitignored — a shared staging credential on a machine that is not yours, say.
For ordinary local development `.env` is fewer commands and one file.

**Do not use both for the same key.** Environment variables are layered above
user-secrets by the default host builder, so `.env` silently wins and you end up
debugging a value you cannot see. Pick one per machine.

### Optional: a stable administrator login

By default the bootstrap generates a random password and logs it once. Uncomment
these in `.env` to fix both instead:

```bash
Bootstrap__AdminEmail=you@erp.local
Bootstrap__AdminPassword=Local!DevOnly123
```

The password must satisfy the Identity policy: 12+ characters, upper, lower,
digit, symbol.

---

## 4. Run it

```bash
# Backend — creates the schema on first run
cd backend
dotnet run --project src/Erp.Api

# Frontend, in a second terminal
pnpm install
pnpm --filter web dev
```

The pnpm package is named `web` and lives in `frontend/`, so workspace commands
are `pnpm --filter web <script>`.

| Service | URL |
|---|---|
| Web app | http://localhost:3000 |
| API | http://localhost:5080 (http) / https://localhost:5081 |
| OpenAPI UI | https://localhost:5081/scalar |
| Seq (logs) | http://localhost:8081 |

**The seeded administrator password is printed to the backend console as a
warning on first run, and never again.** Scroll back and copy it before doing
anything else, or set `Bootstrap:AdminPassword` as above.

The frontend reads exactly one variable and already defaults to the right value —
see [`server.ts`](../frontend/src/lib/api/server.ts). Next.js loads env files from
its own project directory rather than the repository root, so if you do need to
override `ERP_API_BASE_URL`, put it in `frontend/.env.local`. The root `.env` is
read by the API and by docker compose.

If `https://localhost:5081` complains about the certificate, either trust the
development certificate with `dotnet dev-certs https --trust`, or skip https
entirely with `dotnet run --project src/Erp.Api --launch-profile http`. The
frontend BFF talks to port 5080 over http regardless, so nothing is lost.

---

## 5. How the schema gets built

[`DevelopmentBootstrap.cs`](../backend/src/Erp.Api/Authentication/DevelopmentBootstrap.cs),
called at the end of [`Program.cs`](../backend/src/Erp.Api/Program.cs), does two
things on startup:

1. **Migrate.** It resolves `ErpDbContext` and calls `MigrateAsync()`. One
   context maps the whole application, so this is one call rather than a scan
   for every module's own context — see
   [ADR 0002](adr/0002-one-dbcontext-for-the-application.md).
2. **Seed one way in.** If no user matches `admin@erp.local` it creates the
   `Super Administrator` role and that account, logging the generated password.
   If the account already exists it does nothing, and in particular does not
   re-sync the role's permissions — an administrator may have changed them
   deliberately.

There is one context and one history table. Tables still sit in per-area schemas,
mapped in [`ErpDbContext.cs`](../backend/src/Erp.Persistence/ErpDbContext.cs) and
registered once by the host through
[`AddErpDbContext`](../backend/src/Erp.Persistence/DependencyInjection/ErpDbContextExtensions.cs):

| Context | Project | Schemas | History table |
|---|---|---|---|
| `ErpDbContext` | `src/Erp.Persistence` | `masters`, `identity` | `masters.__EFMigrationsHistory` |

Confirm a run worked:

```sql
SELECT * FROM masters.__EFMigrationsHistory;
```

That table records which migrations have been applied. The next `dotnet run`
reads it, finds nothing pending, and does nothing. The operation is idempotent.

> **The bootstrap only runs when `ASPNETCORE_ENVIRONMENT` is `Development`.**
> [`launchSettings.json`](../backend/src/Erp.Api/Properties/launchSettings.json)
> sets it for `dotnet run`, but a published binary or a different launch profile
> will not. Outside Development you get an app that starts against an empty
> database and cannot be signed into. This is deliberate: a production schema
> change should be a reviewed deployment step, not a side effect of a process
> starting.

---

## 6. Coming from Liquibase

EF Core migrations are the same idea with different names. The one real shift:
you do not hand-write the changelog. Edit the entity and configuration classes,
then `dotnet ef migrations add` diffs the model against the last snapshot and
generates the migration for you. Review it, edit it if needed, commit it.

| Liquibase | EF Core |
|---|---|
| changelog you write | migration classes, generated from the entity classes |
| `liquibase update` | `dotnet ef database update` — or just `dotnet run`, here |
| `DATABASECHANGELOG` | `masters.__EFMigrationsHistory`, one for the application |
| `liquibase rollback` | `dotnet ef database update <PreviousMigrationName>` |
| `liquibase diff` | `dotnet ef migrations has-pending-model-changes` (a CI gate) |

---

## 7. Recreating the database from nothing

`dotnet run` applies what is pending; it does not undo anything. To start from an
empty database:

```bash
cd backend

# Drops the whole database — both schemas, one context.
dotnet ef database drop -f \
  --context ErpDbContext \
  --project src/Erp.Persistence --startup-project src/Erp.Api

dotnet run --project src/Erp.Api
```

On Windows the SSMS equivalent is `DROP DATABASE Erp; CREATE DATABASE Erp;`
followed by the same `dotnet run`.

Wiping the Docker volume achieves the same and also resets Seq and MinIO:

```bash
docker compose down -v && docker compose up -d sqlserver
```

Whichever route, the next `dotnet run` rebuilds every table and reseeds the
administrator — with a **new** generated password unless
`Bootstrap:AdminPassword` is set.

### Migrating explicitly, without starting the app

Rarely needed, since startup does it. One command now, because there is one
context. Run from `backend/`:

```bash
dotnet ef database update --context ErpDbContext \
  --project src/Erp.Persistence --startup-project src/Erp.Api
```

Add `--no-build` when the app is already running and holding its output
assemblies — the migration then runs against the existing build rather than
failing on a locked DLL.

### Adding a migration

```bash
dotnet ef migrations add <Name> \
  --project src/Erp.Persistence \
  --startup-project src/Erp.Api \
  --context ErpDbContext \
  --output-dir Migrations
```

---

## 8. Troubleshooting

| Symptom | Cause |
|---|---|
| `ConnectionStrings:Erp is not configured…` | §3 was skipped on this machine. The message names the fix. |
| `InvalidOperationException: The ConnectionString property has not been initialized.` | The same cause, seen on a build from before the guard in `PersistenceExtensions.ErpConnectionString` existed: `UseSqlServer(null)` was accepted and failed later inside SqlClient, naming neither the setting nor the application. |
| `A network-related or instance-specific error…` | database not running, or wrong `Server=`. On macOS check `docker compose ps`. |
| `The certificate chain was issued by an authority that is not trusted` | `TrustServerCertificate=True` missing from the connection string. |
| `Login failed for user 'sa'` | password does not match `MSSQL_SA_PASSWORD` in `docker-compose.yml`. |
| App starts, no tables, cannot sign in | `ASPNETCORE_ENVIRONMENT` is not `Development` — see §5. |
| `CREATE SCHEMA permission denied` | the login is not `db_owner` on `Erp`. |
| Cannot find the admin password | it is logged once at first run only. Reset the database, or set `Bootstrap:AdminPassword` and reset. |
| Works on one machine, not another after `git pull` | `.env` is gitignored and per-machine. `cp .env.example .env` — and if a teammate added a variable, diff the two files. |
| A setting in `.env` appears to be ignored | something already exports that variable in the shell, or it is also set in user-secrets. Real environment variables win by design (`NoClobber`); check with `env \| grep <name>` and `dotnet user-secrets list`. |
| Integration tests fail, others pass | Docker Desktop is not running. Testcontainers needs it on both platforms. |
| `dotnet build` fails with `There is already an object named 'AspNetRoles'` | `ASPNETCORE_ENVIRONMENT=Development` is set in `.env`. Remove it — see §3. The build starts the host to emit the OpenAPI document, deliberately outside Development so it never migrates; a value in `.env` reaches that process and makes `dotnet build` write to your database. |
| `dotnet build` or `dotnet ef` fails on a locked DLL | the app is running. Stop it, or pass `--no-build`. |

---

## 9. Everyday commands

```bash
dotnet build backend/Erp.slnx              # analyzers + banned symbols are build errors
dotnet test  backend/Erp.slnx              # unit + architecture + integration

# Formatting — the subcommands, not bare `dotnet format`; see README for why.
dotnet format whitespace backend/Erp.slnx
dotnet format style backend/Erp.slnx

pnpm lint                                  # eslint + prettier
pnpm test                                  # vitest
pnpm --filter web exec playwright test     # e2e
pnpm --filter web generate:api             # regenerate the TS client from OpenAPI
```
