# CLAUDE.md – Asset Tracker API

Guidance for Claude Code in this repo. Read this first, then
`docs/PROJECT_CONTEXT.md`. That file is the source of truth for decisions,
domain rules, the data model and the build plan. If this file and the context
doc disagree, the context doc wins (and tell me, so we can fix one of them).

## What this is

A REST API (ASP.NET Core Web API) for the database operations of the Week 46
asset tracker ("Dragon's Hoard"): offices, and create / read / update / delete,
search, filter, sort and paging for company IT assets, plus the per-currency
figures the reports are built from. It owns the asset rules and the database;
it does **not** convert currencies, build reports or export files. The Week 46
console app (`WeeklyMiniProjects/Week46/`) will be rebuilt afterwards to call
this API instead of the database, and brings those parts together.

Self-study extension of Weekly Mini Project week 46 (the brief lists "Build
REST API endpoints" as a challenge idea). Standalone repo
`NiclO1337/asset-tracker-api`, deployed publicly with its own external
database. No deadline.

## Stack

- .NET 10 (LTS), C# 14. `global.json` pins the .NET 10 SDK; my machine also
  has the .NET 11 SDK, and without the pin `LangVersion latest` would mean
  C# 15. Never remove or bump `global.json` without asking
- ASP.NET Core 10 Web API with **controllers** (as scaffolded), built-in DI,
  built-in `ILogger`, `Microsoft.AspNetCore.OpenApi` for the OpenAPI document
- Entity Framework Core 10 on SQL Server Express, code-first migrations
- Validation: data annotations on request DTOs + the domain's own guards
- Tests: xUnit, see "Testing"
- Formatter: `dotnet format`. Warnings are errors (`Directory.Build.props`)
- Auth: one API key per client, sent in a header (see Security). No users,
  no ASP.NET Core Identity, no JWT
- Hosting: **Azure Container Apps** (consumption plan, scales to zero) with a
  free managed certificate for `asset-tracker-api.niclashugdahl.dev`
- Production database: **Azure SQL Database** (free offer, serverless). Local
  development against SQL Server Express. Same EF Core provider in both
- Container image: GitHub Container Registry (`ghcr.io`), public package (free).
  Not Azure Container Registry (no free tier)
- Database login: the Container App's **managed identity** (no database
  password anywhere)
- Logs: no log storage (Container Apps logs destination `none`). Live log
  stream in the portal / `az containerapp logs show --follow` only, so log
  output must be useful as it happens and is gone afterwards
- Deploy pipeline: GitHub Actions (tests, image to `ghcr.io`, new revision);
  details in the context doc
- Package manager: `dotnet` / NuGet

Several of these may be newer than what you remember. **Check the current
official docs before writing version-specific code**, and trust the compiler and
the tests over memory. Do not copy patterns from older major versions (no
`Startup.cs`, no Swashbuckle-era setup, no `IWebHostBuilder` templates).

## Commands

Run from the repo root.

| Command | What it does | Who runs it |
|---------|--------------|-------------|
| `dotnet build` | Build everything. Warnings fail the build | You, after changing code |
| `dotnet test` | Tests, one run (once a test project exists) | You, after changing code |
| `dotnet format` | Formatter | You, after editing files |
| `dotnet format --verify-no-changes` | Check formatting only | You, before suggesting a commit |
| `dotnet list package --vulnerable --include-transitive` | Known vulnerable packages | You, when a package is added or updated |
| `dotnet ef migrations add <Name> --project src/AssetTracker.Infrastructure --startup-project src/AssetTracker.Api` | New migration | You, when the model changes (ask first) |
| `dotnet ef database update --project src/AssetTracker.Infrastructure --startup-project src/AssetTracker.Api` | Apply migrations to my local database | **Me only** |
| `dotnet run --project src/AssetTracker.Api` | Starts the API, never exits | **Me only**, see below |
| `dotnet watch` | Never exits | **Me only** |
| `az …`, `dotnet publish /t:PublishContainer`, anything that deploys or touches Azure | Deploy, cloud resources | **Me only**. You may write the commands for me to run |

Visual Studio Package Manager Console equivalent:
`Add-Migration <Name> -Project AssetTracker.Infrastructure -StartupProject AssetTracker.Api`.

Run only the checks a change can affect, and run them before you suggest a
commit, so I get a verified chunk. Report the result honestly, including
failures. Warnings count as failures.

**Manual API testing** goes through `src/AssetTracker.Api/AssetTracker.Api.http`.
When you add or change an endpoint, add or update its requests there (happy
path and the main error cases) and tell me which ones to run. I run them in
Visual Studio against my running API.

## How we work together

- **Small chunks.** Every change you suggest must be small enough to fit in
  **one commit** with one clear purpose. If a task is bigger than that, first
  give me a short numbered plan of commit-sized steps, then do **one step at a
  time**.
- **Stop after each chunk.** Say what changed, why, and what I should check.
  Wait for me before you continue with the next step.
- Do not mix unrelated things in one chunk (for example a refactor and a new
  feature, or a bug fix and a formatting pass).
- Follow the build plan in the context doc in order, one step per commit, and
  say which step you are on. Do not jump ahead.
- **Do not start the API**, a browser, or anything that never exits (watch
  mode). Tell me what to run in the `.http` file and I will check it manually.
- **Look at the whole picture first.** Before changing something, find out
  where else it is used, what depends on it and what the types and data model
  say. Many problems are symptoms of something else.
- **Clean code.** Prefer the simplest solution that works. If a fix feels
  complicated, or a second fix is needed on top of an earlier fix: stop, explain
  the root cause and suggest changing the approach instead of stacking patches.
  Ask before a larger refactor.
- **Explain why.** When you introduce a technique or concept I have not used
  before (this is my first ASP.NET Core API: DI lifetimes, model binding,
  ProblemDetails, async/await with EF Core, etc.), explain it in one or two plain
  sentences. I want to understand every line.
- **Ask before** adding a dependency, and before anything destructive
  (deleting files, dropping or resetting a database, removing a migration).
- **Never take shortcuts that hide problems**: no skipped or deleted failing
  tests, no `#pragma warning disable` or suppressed analyzers, no `--no-verify`,
  no `dynamic` or `object` to dodge a type, no `!` to silence nullability. Fix
  the cause.
- Do not leave debug output, commented-out code, unused files or unused
  dependencies behind. The scaffold's `WeatherForecast` files go in the first
  code step.
- Do not invent content, test results or data. Ask me, or leave a `TODO`.

## Where things come from

Copy and adapt; do not rewrite from scratch what already exists. The context doc
has the file-by-file reuse map.

| Need | Copy from | Adapt |
|---|---|---|
| Domain models, enums, exceptions, `AssetLifecycle`, `AssetQuery`, `PagedResult`, services, repository interfaces | `WeeklyMiniProjects/Week46/src/AssetTracker.Core/` | Namespaces stay `AssetTracker.Core.*`; signatures become async (see Hard rules) |
| `AppDbContext`, Fluent configuration, EF repositories, `SampleData`, `DatabaseInitializer` | `WeeklyMiniProjects/Week46/src/AssetTracker.Infrastructure/Persistence/` | Context gets a `DbContextOptions` constructor and is registered with DI; async queries |
| `Directory.Build.props`, `.gitignore` | `WeeklyMiniProjects/Week46/` and repo root | Unchanged |
| Package versions, connection-string pattern | `EntityFrameworkCore/` example (`AppDbContext.cs`, `ConsoleAppEF1.csproj`) | Read through ASP.NET Core configuration instead of a hand-built `ConfigurationBuilder` |

**Never modify anything in `WeeklyMiniProjects/`** from this project, unless I
ask for it in that repo explicitly. Week 46 is still being built there; copy
from its latest **committed** state (`git show HEAD:<path>`), not from
uncommitted work in progress, and tell me if the file you need has
uncommitted changes.

## Git and commits

- **I do all commits myself. Always.** Never run `git add`, `git commit`,
  `git commit --amend`, `git push`, `git merge`, `git rebase`, `git reset`,
  `git stash`, or create, switch or delete branches. Read-only git is fine
  (`git status`, `git diff`, `git log`, `git show`).
- **Always suggest a commit message** when a chunk is done. It is **very
  short** (aim for under 50 characters), imperative and lowercase, for example
  `add asset endpoints`. No prefix: this is a standalone repo.
- If I ask for something that does not fit in one commit, tell me and suggest
  how to split it.
- Branching: all work on `main`.

## Security

Security is part of every chunk, not a clean-up phase at the end. This is a
web-facing app: everything here is reachable over HTTP.

- **Research first.** Before the first code is written, and again whenever a
  framework, library or security-relevant feature is added (endpoints that
  write data, auth, CORS, file output, external APIs): **search the web for
  known security holes and common security mistakes in similar applications and
  in the exact frameworks, tools and versions this project uses.** Start with
  the OWASP Top 10, the OWASP API Security Top 10 and the relevant OWASP Cheat
  Sheets, Microsoft's ASP.NET Core security docs, and published advisories
  (CVE, GitHub Advisory Database, .NET release notes) for our versions.
- **Tell me what you found** in a few lines: which risks apply here, and how we
  prevent each of them according to current best practice. Write lasting
  decisions into the security section of the context doc.
- **Prevent it in the design**, not afterwards. Use the framework's built-in
  protection and well-known libraries. Never write your own crypto, password
  hashing, token or session handling.
- **Never trust input.** Validate on the server everything that comes from the
  request body, route, query string or headers. `[ApiController]` model
  validation first, the domain's guards always. Never trust ids, currencies,
  prices or totals sent by the client: the currency comes from the office, the
  id from the route, the EUR value is computed.
- **No over-posting / mass assignment.** Requests bind to request DTOs that
  contain only the fields a client may set. Never bind to, or return, an EF
  entity.
- **Check who is asking.** The API is public on the internet. **Nothing is
  deployed until every endpoint requires authentication.** The login is an API
  key in the `X-Api-Key` header:
  - Implemented as an ASP.NET Core authentication handler, so the normal
    `[Authorize]` pipeline is used, plus a **fallback authorization policy**
    that requires an authenticated caller. New endpoints are protected by
    default; `[AllowAnonymous]` only on a health check, if we add one.
  - Compare keys with `CryptographicOperations.FixedTimeEquals`, never `==`.
  - Missing or wrong key → 401 with no hint about which part was wrong.
  - The key lives in user secrets locally and as a Container Apps secret in
    Azure. Never in a URL or query string, never logged.
  - This is deliberately simple. Users, passwords, roles and Identity
    (`AspNetUsers`) belong to the next project, not this one.
- **Limit what one request can cost.** Page size has a server-side maximum;
  never return an unbounded list. Rate limiting (ASP.NET Core's built-in
  middleware) is on before the first deploy.
- **Production is not Development.** The deployed app runs with
  `ASPNETCORE_ENVIRONMENT=Production`: no developer exception page, no
  OpenAPI document unless we decide to publish it, HTTPS only, HSTS on.
- **Azure.** The app reaches Azure SQL with its system-assigned managed
  identity (`Authentication=Active Directory Managed Identity` in the
  connection string): no database password exists to leak. Its database user
  gets `db_datareader` + `db_datawriter` only; migrations run as my Entra
  admin account, not as the app. The image is public, so it must never contain
  secrets (no `appsettings.Production.json` with values, no `.env`). Firewall
  rules are written into the context doc.
- **No injection.** LINQ / EF Core only. No `FromSqlRaw` or SQL built from
  strings; if raw SQL is ever needed, use `FromSql` with interpolated
  parameters and tell me first.
- **Secrets** (connection strings with credentials, API keys) never go into
  code, git, `appsettings*.json`, logs, error responses or the `.http` file.
  Locally: user secrets, and `http-client.env.json.user` (git-ignored) for the
  `.http` file. Deployed: the host's environment variables / secret store.
  Only the passwordless `localhost` fallback lives in source. The production
  database user has only the rights the app needs (no `db_owner` at runtime). If a secret has ended up in the repo or in the
  chat, tell me right away so I can rotate it.
- **Dependencies.** Few, well-maintained packages; the versions in "Packages".
  Run `dotnet list package --vulnerable --include-transitive` when a package is
  added or updated and tell me about findings instead of silently ignoring or
  force-fixing them.
- **Personal data.** `AssignedTo` is a person's name or username: never log it,
  and only invented names in `SampleData`, the `.http` file and the README.
- **Errors** returned to clients are RFC 9457 ProblemDetails with no stack
  traces, SQL, file paths, connection details or exception messages from
  infrastructure. The developer exception page is for `Development` only.
- **Never weaken security to make something work** (CORS `*`, turning off
  HTTPS redirection or certificate checks, disabling validation). If there
  seems to be no other way, stop and ask.
- If you notice a security problem in existing code, **tell me**, even when it
  is outside the current task. When a chunk touches security, say so in the
  summary and tell me what to test.

## Where logic lives

Proposed layout; the scaffold in `asset-tracker-api/` is moved into `src/` in
the scaffold step. The context doc has the full tree.

```
asset-tracker-api/
├── CLAUDE.md
├── asset-tracker-api.slnx
├── Directory.Build.props
├── README.md
├── TESTING.md
├── docs/PROJECT_CONTEXT.md
├── src/
│   ├── AssetTracker.Core/            # domain + business logic, no packages
│   ├── AssetTracker.Infrastructure/  # AppDbContext, Migrations/, EF repositories, seeding
│   └── AssetTracker.Api/             # Program.cs, Controllers/, Contracts/ (DTOs), error mapping, .http
└── tests/
    ├── AssetTracker.Core.Tests/
    └── AssetTracker.Api.Tests/
```

```
Api ──► Core ◄── Infrastructure
 └──────────────────►┘
```

- `AssetTracker.Core`: models, enums, repository interfaces, services,
  exceptions. Pure rules and calculations. **References no project and no
  NuGet package.** No HTTP, no EF, no reading the clock (pass `today` in).
- `AssetTracker.Infrastructure`: database access only. Implements the `Core`
  interfaces. References `Core`.
- `AssetTracker.Api`: controllers, request/response DTOs and their mapping,
  exception → status code mapping, DI wiring. **No business logic and no
  queries in controllers**: a controller action validates, calls one service
  method, maps the result to a DTO and returns it.
- `Program.cs` is the composition root: registration and the middleware
  pipeline. Nothing else.

Dependencies point one way only. If a layer seems to need a reference in the
other direction, the design is wrong: stop and say so.

## Hard rules

- **No EF Core types outside `Infrastructure`.** No `DbContext`, `DbSet`,
  `IQueryable`, `Include` or `Microsoft.EntityFrameworkCore` usings in `Core` or
  `Api` (except `AddDbContext` in `Program.cs`). Repositories take intent (an
  `AssetQuery`) and return materialized `IReadOnlyList<T>` / `PagedResult<T>`.
- **Async all the way** for I/O: repository and service methods that touch the
  database are `async Task<T>` and take a `CancellationToken`, passed down from
  the controller action. Never `.Result`, `.Wait()` or `async void`.
- **`AppDbContext` is scoped** (one per request, registered with
  `AddDbContext`). Services and repositories that use it are scoped too. Never
  hold a context in a singleton.
- **Controllers never return entities.** Response DTOs are records in
  `Contracts/`; `Asset` → `AssetResponse` mapping lives in one place.
- **Status codes are consistent**, mapped from domain exceptions in one place
  (an `IExceptionHandler`), not with try/catch in every action:
  not found → 404, duplicate serial number → 409, domain validation → 400,
  `DataStoreException` → 503, anything unexpected → 500. Create returns 201
  with a `Location` header, delete returns 204.
- **`decimal` for money**, explicit precision on every money column.
- **Prices are stored in local currency** (`PurchasePrice` + `Currency` on the
  asset); the currency is set from the office at creation and never changes.
  The EUR value is computed, never stored. **Never compare or sum prices across
  currencies.** Same rules as Week 46; see the context doc.
- **Never branch on asset type for behaviour.** No `is ComputerAsset`, no
  `GetType()`; use the abstract members on `Asset`. Serialising the category
  and type into the DTO is fine.
- **Schema changes go through a migration.** Never `EnsureCreated()`, never
  edit the database by hand, never hand-edit `*.Designer.cs` or the model
  snapshot.
- **No `Console.*`.** Use `ILogger<T>`, and never log request bodies, personal
  data or connection strings. Do not copy the EF example's
  `LogTo(Console.WriteLine, ...)`.
- **Catch specific exceptions only**, and only where you can do something
  useful. The global handler deals with the rest.

## Code conventions

- Readable over clever: meaningful names, small methods with a single job, no
  duplicated logic, no premature abstraction (no generic repository, no
  MediatR, no AutoMapper).
- Nullable enabled. No `!` without a comment justifying it.
- File-scoped namespaces, one type per file, file name matches the type.
- `sealed` by default. `virtual`/`abstract` only where polymorphism is used.
- `PascalCase` types and members, `_camelCase` private fields,
  `s_camelCase` static readonly, `Async` suffix on async methods.
- Routes: plural nouns, lowercase (`/api/assets`, `/api/assets/{id:int}`,
  `/api/offices`). JSON is camelCase (the ASP.NET Core default); enums
  serialised as strings; dates as `yyyy-MM-dd`.
- Constants (max lengths, price limit, page size) live once in `Core` and are
  used by the domain, the Fluent configuration and the DTO annotations.
  Settings live in `appsettings.json`, never hard-coded.
- Derived values (EUR value, lifetime status) are calculated, not stored.
- XML doc comments on public members of `Core` and on controller actions (they
  feed the OpenAPI document); elsewhere comment only what isn't obvious.

## Testing

Automatic tests from the start, plus manual checks documented in `TESTING.md`
(my template; create it in the docs step).

- `tests/AssetTracker.Core.Tests`: xUnit (same versions as the Milestone
  project). Domain rules (`AssetLifecycle`, validation) and services, with
  in-memory fakes of the repository interfaces.
- `tests/AssetTracker.Api.Tests`: integration tests with
  `WebApplicationFactory` (`Microsoft.AspNetCore.Mvc.Testing`) for routes,
  status codes, validation, auth and the error mapping. The database they run
  against is decided in the context doc (never my dev database, never
  production).

Rules:

- Tests are written **in the same commit** as the code they cover.
- Test behaviour, not implementation: for endpoints, the status code and the
  response body.
- Pure functions get table-driven tests (`[Theory]`).
- Tests are deterministic: no real network, no real clock (pass `today` in),
  no randomness, no dependency on my local database's contents.

## Packages

Exactly these (the context doc, §10, says what each is for). Ask before adding
anything else.

| Package | Version | Project |
|---|---|---|
| `Microsoft.AspNetCore.OpenApi` | 10.0.15 | Api (from the scaffold) |
| `Microsoft.EntityFrameworkCore.SqlServer` | 10.0.15 | Infrastructure |
| `Microsoft.Data.SqlClient` | 6.1.8 | Infrastructure (pins the patched driver) |
| `Microsoft.EntityFrameworkCore.Tools` | 10.0.15 | Api (`PrivateAssets=all`) |
| `xunit`, `xunit.runner.visualstudio`, `Microsoft.NET.Test.Sdk`, `coverlet.collector` | 2.9.3, 3.1.4, 17.14.1, 6.0.4 | test projects (Milestone versions) |
| `Microsoft.AspNetCore.Mvc.Testing` | 10.0.15 | Api.Tests |

The EF example and Week 46 use 10.0.14; this project uses 10.0.15 and
SqlClient 6.1.8 because of the November 2026 security fixes (context doc §8,
X10). The Week 46
`Microsoft.Extensions.Configuration.*` packages are not needed: ASP.NET Core
already reads user secrets and environment variables. The explicit
`System.Security.Cryptography.Pkcs` reference is dropped unless the
vulnerability check flags it. The .NET 10 SDK must be 10.0.404 or later
(runtime 10.0.15). `dotnet-ef` is a local tool (`.config/dotnet-tools.json`)
pinned to 10.0.15: run `dotnet tool restore`, never a global `dotnet-ef`.

## Persistence

- Its own database. Locally: `asset_tracker_api` on SQL Server Express. The
  handed-in Week 46 app and its `asset_tracker_ef` are never touched.
- Connection string `APP_DB_CONNECTION` from user secrets or an environment
  variable, falling back to
  `Server=localhost\SQLEXPRESS;Database=asset_tracker_api;Integrated Security=true;Encrypt=false;`
  in Development only. Production has no fallback: a missing connection string
  stops the app at startup with a clear log message.
- Production is Azure SQL Database with the same `SqlServer` provider, so one
  set of migrations works for both. The free serverless database pauses when
  idle and the first query after a pause can fail or be slow: turn on EF
  Core's connection resiliency (`EnableRetryOnFailure`) and keep the HTTP
  client's timeout in mind.
- Locally, Development startup applies pending migrations and seeds the sample
  assets when the table is empty (copied from Week 46). In production,
  migrations are a deliberate deploy step (SQL script or migration bundle),
  not something the app does on startup. Sample data is inserted at startup
  only when `Database:SeedSampleData` is true and the table is empty (true in
  Development; set once in Azure, then removed). Offices are seeded by the
  migration.
- Read-only queries use `AsNoTracking()`. Filter, sort and page in SQL; never
  load the table and filter in memory.
- IDs are database identity columns. Never assign or reuse them in code.

## Scope

Build what the context doc describes, in that order. Do not add machinery
nobody asked for (extra frameworks, abstractions, configuration, features).

Not part of this API (they live in the Week 46 client): currency conversion
and exchange rates, EUR totals, report layout, TXT/CSV/JSON export, any UI.

Planned but **not in scope yet**, do not build until asked: users, roles,
ASP.NET Core Identity or JWT (next project), Swagger UI / Scalar, CORS (the
client is not a browser), a frontend,
switching the Week 46 console app over to call this API (that work belongs in
the `WeeklyMiniProjects` repo).

## README and docs

- `README.md` follows my README template: **simple** (APIs). Keep it accurate
  when behaviour, endpoints or setup change. It lists every endpoint.
- When a section needs an image, write the image markdown anyway, with a
  suggested alt text and file path, and add a `<!-- TODO: take screenshot -->`.
- Leave `TODO` markers for things that can only be filled in later. Do not
  invent them.
- When a decision is made during the work that is not in the context doc, add
  it there instead of letting it live only in the chat.

## Docs map

| File | What it is |
|------|------------|
| `docs/PROJECT_CONTEXT.md` | Decisions, domain rules, data model, endpoints, security notes, build plan |
| `README.md` | Public project description, setup, endpoint list |
| `TESTING.md` | How we test, test results, manual checklists (my template) |
| `src/AssetTracker.Api/AssetTracker.Api.http` | Manual request collection, one block per endpoint and error case |
| `WeeklyMiniProjects/Week46/docs/PROJECT_CONTEXT.md` | The Week 46 design this API reuses (read only) |
