# PROJECT_CONTEXT – Asset Tracker API (ASP.NET Core Web API on Azure)

Agreed plan for the Asset Tracker API. It builds on the Week 46 design
(`WeeklyMiniProjects/Week46/docs/PROJECT_CONTEXT.md`) and the brief
(`Weekly Mini Project - Asset Tracking-2.pdf`); everything this API needs from
them is summarised here. Read this before writing code. `CLAUDE.md` is the
short rulebook for *how* we work. This file is *what* we build and *why*.

> **Status (2026-11-27):** planning done, nothing built. Open questions 1–9
> answered on 2026-11-27 (§14). Still **Proposed**: the rows marked so in §2
> and the accepted risks in §8, each needed before the step that uses it.
> Last review: 2026-11-27.

**From the brief:** the challenge ideas for students who finish early include
"Build REST API endpoints", and Level 5's bonus ideas include "Build REST API".

**The idea:** *Dragon's Hoard API*: the asset tracker's database behind a
small, secure, deployed REST API. It answers one question for its client:
"what assets do we have, where, and what state are they in?", so that the
Week 46 console app can become a client of a real web service instead of
talking to a database directly. Its second job is to be a clean, public
portfolio piece.

**How this file is kept up to date**

- Everything here is agreed with Niclas. A new or changed decision goes in as
  **Proposed** until he confirms it.
- A decision made while building is added to §14 with a date **and** worked into
  the sections it changes. It must not live only in the chat or a commit message.
- Open questions stay in §14 "Still open". **Do not invent answers.** Mark
  missing values with `TODO`.
- Sections are referred to as §N, features as M1 / S1 / C1 (§4), rules as R1
  (§5), risks as X1 (§8).

---

## 1. Goals (own extension, no separate brief)

The brief only names a REST API as an optional challenge, so these goals decide
when the project is finished:

- [ ] Every database operation the Week 46 console app uses is available as an
      endpoint: offices, asset CRUD, search / filter / sort / paging, and the
      per-currency figures its reports are built from
- [ ] The same domain rules as Week 46 (lifetime, local-currency prices,
      validation), enforced by the API, not trusted from the client
- [ ] Deployed on Azure at `https://asset-tracker-api.niclashugdahl.dev`,
      protected by an API key, with its own Azure SQL database
- [ ] Automated tests run on every push (GitHub Actions); a push to `main`
      that passes deploys
- [ ] Public repo with a README a reviewer can follow, and `TESTING.md`
- The brief's general notes still apply: meaningful names, no duplicated code,
  small reusable methods, graceful handling of bad input.

Not a goal: replacing the Week 46 console app. Rebuilding it as a client of
this API is a separate piece of work in the `WeeklyMiniProjects` repo (§16).

---

## 2. Agreed decisions

| Topic | Decision | Status |
|-------|----------|--------|
| Repo | Standalone public repo `NiclO1337/asset-tracker-api`, all work on `main`, no commit prefix | Agreed |
| Stack | .NET 10 (LTS), C# 14, ASP.NET Core 10 Web API with **controllers** | Agreed |
| SDK pinning | `global.json` pins the .NET 10 SDK (`10.0.404`, `rollForward: latestFeature`), so this repo builds with C# 14 on every machine even when a .NET 11 SDK is installed (`LangVersion latest` would otherwise mean C# 15). `dotnet-ef` is a local tool pinned to 10.0.15 | Agreed |
| Responsibility | The API owns the database and the asset rules. It does **not** convert currencies, fetch exchange rates, build report layouts or export files: the Week 46 client does. The split falls where Week 46 already split it: aggregate per currency in SQL, convert in the client | Agreed |
| Data | EF Core 10 with the `SqlServer` provider. Locally SQL Server Express (`asset_tracker_api`), in production Azure SQL Database (free offer, serverless). Code-first migrations, one set for both | Agreed |
| Week 46 database | Never touched. The API has its own database | Agreed |
| Auth | One API key per client in the `X-Api-Key` header, checked by an ASP.NET Core authentication handler; every endpoint requires it by default. No users, roles, Identity or JWT | Agreed |
| Hosting | Azure Container Apps (consumption plan, scales to zero), custom domain with a free managed certificate | Agreed |
| Image | Container image built with `dotnet publish /t:PublishContainer` (no Dockerfile), public package on GitHub Container Registry `ghcr.io` | Agreed |
| Database login | The Container App's system-assigned **managed identity**; no database password anywhere. App user: `db_datareader` + `db_datawriter` | Agreed |
| Logs | No log storage (Container Apps logs destination `none`); live log stream only | Agreed |
| Pipeline | GitHub Actions: build, test and format check on every push; on `main`, publish the image and deploy a new revision | Agreed |
| Testing | xUnit, `tests/AssetTracker.Core.Tests` and `tests/AssetTracker.Api.Tests` (`WebApplicationFactory`), plus manual checks in `TESTING.md` | Agreed |
| Quality | Warnings as errors (`Directory.Build.props` from Week 46), `dotnet format`, nullable on | Agreed |
| Docs | Simple README template, `TESTING.md`, `.http` request file | Agreed |
| Timeline | Self-study, no deadline | Agreed |
| Package versions | Move from the EF example's 10.0.14 to **10.0.15** and pin `Microsoft.Data.SqlClient` **6.1.8**, because of the November 2026 security fixes (§8, X10). Drop the explicit `System.Security.Cryptography.Pkcs` reference unless the vulnerability check in step 2 flags it | Agreed |
| Test framework | xUnit **2.9.3** with the Milestone project's runner and SDK versions (familiar setup; `xunit.v3` can wait for a later project) | Agreed |
| API tests | `AssetTracker.Api.Tests` replaces the repositories with in-memory fakes: no database in CI. EF queries are checked by hand (`TESTING.md`) | Agreed |
| Unknown office | An `officeId` in a request body that doesn't exist gives **400** with `errors.officeId` (it is a bad field, not a missing URL resource) | Agreed |
| Sample data | The live database gets the same 25 sample assets as local development, seeded once by the app (§12 step 8) | Agreed |
| Region | Azure **Sweden Central** (`swedencentral`) for every resource | Agreed |
| Rate limits | **60 requests per minute per client key**, **10 per minute per IP** for requests without a valid key | Agreed |
| Domain errors | Core throws its own `AssetValidationException` (with the field name) instead of `ArgumentException` for bad input, so the API can map it to 400 without also turning programming bugs (`ArgumentNullException`) into 400s | **Proposed** |
| Today's date | Services get the date from an injected `TimeProvider` and pass `today` into the domain (Week 46's `Asset` reads `DateTime.Today` itself). "Today" is the **UTC** date | **Proposed** |
| Asset type in the contract | One `type` field (`Laptop`, `Desktop`, `Phone`, `Tablet`); the category is derived from it | **Proposed** |
| Unknown JSON fields | Rejected with 400 (`UnmappedMemberHandling.Disallow`), so a typo or an attempt to set `currency` or `id` is visible instead of silently ignored | **Proposed** |
| Firewall | Azure SQL "Allow Azure services" on (Container Apps has no fixed outbound IP on the consumption plan) together with **Microsoft Entra-only authentication**, so SQL logins can't be used at all. Recorded as an accepted risk in §8 | **Proposed** |
| Pipeline login to Azure | GitHub Actions signs in to Azure with OpenID Connect (federated credential, no stored Azure secret), with rights on this resource group only | **Proposed** |

**Why the API stops at the database.** Exchange rates are an outside service
with its own failure modes (cache, stale cache, hard-coded fallback) that Week
46 already handles well. Moving them into the API would add an outbound HTTP
dependency, a cache file in a scale-to-zero container and more to secure,
while the client would still need the rates to display anything. Keeping the
API to stored facts (what was paid, in which currency, when) keeps it small,
fully testable and stable: its answers never change because a rate moved.

**Why an API key and not users.** The only client is a program, not a person.
An API key is the standard way to identify a calling program; real user login
(ASP.NET Core Identity, `AspNetUsers`) is the subject of the next course
project. It is still done "properly": an authentication handler and a
fallback policy, not a check copied into each controller.

**Considered and not chosen**

- **Azure App Service Free (F1):** no custom domain on the free tier.
- **Heroku:** works for .NET, but its database is Postgres; SQL Server from
  Heroku would mean opening the database firewall to the whole internet.
- **Azure Container Registry:** no free tier (Basic is about $0.167/day).
- **`az containerapp up --source`:** convenient, but creates an Azure
  Container Registry when none is given.
- **Log Analytics:** costs per GB stored and isn't needed for a hobby API.
- **SQL login with a password:** a secret to store, rotate and leak; managed
  identity has none.
- **Minimal APIs:** fine, but the scaffold and the course use controllers.

**Versions checked on 2026-11-24**

- .NET runtime **10.0.15** / SDK 10.0.404 or 10.0.115 (November 10, 2026
  servicing release; earlier 10.0 runtimes are affected by the ASP.NET Core
  CVEs in that release). .NET 11 shipped the same day; we stay on the 10 LTS.
- EF Core **10.0.15** (published 2026-11-10); its SqlServer provider requires
  `Microsoft.Data.SqlClient` >= 6.1.7 and `System.Security.Cryptography.Pkcs`
  >= 10.0.13.
- `Microsoft.Data.SqlClient` **6.1.8** (2026-11-13).
- `Microsoft.AspNetCore.OpenApi` and `Microsoft.AspNetCore.Mvc.Testing`
  **10.0.15**.
- xUnit: the Milestone project uses xunit 2.9.3; the current line is
  `xunit.v3` 4.0.3 (see §14, open question 2).

---

## 3. Audience and goals

**Users**

1. **The Week 46 console app (rebuilt later):** "Give me a page of assets
   matching these criteria; create, change or remove one; give me the totals
   per office and currency so I can convert them."
2. **Niclas, as developer and operator:** "I want to run it locally against
   SQL Express, test it from the `.http` file, and deploy by pushing to
   `main`."
3. **A reviewer looking at the portfolio:** "I want to understand what this
   API does, how it is secured and how to run it, from the README alone."

**Problem:** the Week 46 app talks straight to its database, so the data
lives on one machine and every client would need database access and
credentials.

**Goals**

- Any read with a page size up to 100 answers in under 1 second once the app
  and database are warm.
- A bad request gets a 4xx with a ProblemDetails body that says which field is
  wrong.
- Deploying is a `git push`; no manual step after the one-off Azure setup.

---

## 4. Scope tiers

Build order: every Must first, then Should. *Could* is optional.

### Must

| # | Feature |
|---|---------|
| M1 | `GET /api/offices` lists the four offices (id, name, country, currency) |
| M2 | `GET /api/assets` returns one page of assets. Query: `brand`, `model` (contains, case-insensitive), `officeId`, `purchaseYear`, `category` (`Computer` / `Mobile`), `lifecycle` (`All` / `ExpiringSoon` / `Expired`), `sortBy` (`Office` default / `Category` / `EndOfLife`), `page` (default 1), `pageSize` (default 10, max 100). Criteria combine with AND |
| M3 | `GET /api/assets/{id}` returns one asset or 404 |
| M4 | `POST /api/assets` creates an asset; the currency is taken from the office. 201 with a `Location` header |
| M5 | `PUT /api/assets/{id}` replaces the editable fields (not type, category or currency). 200 with the updated asset |
| M6 | `DELETE /api/assets/{id}` removes an asset. 204 |
| M7 | `GET /api/reports/office-totals`: asset count and summed purchase price per office **and currency** |
| M8 | `GET /api/reports/most-expensive?count=10`: the top `count` assets **within each currency** (max 50) |
| M9 | Every endpoint requires a valid API key; missing or wrong key gives 401 |
| M10 | Errors are ProblemDetails with the right status code (400, 401, 404, 409, 429, 503, 500), never a stack trace |
| M11 | Rate limiting: too many requests give 429 with `Retry-After` |
| M12 | Deployed on Azure at the custom domain, deploying on push to `main` |

### Should: if time allows

| # | Feature |
|---|---------|
| S1 | `GET /api/assets/serial-number-available?serialNumber=…&exceptId=…` so the client can re-prompt before submitting a form |
| S2 | OpenAPI document published with the repo (`openapi.json` in `docs/`) so the README can link to it |
| S3 | A health endpoint for Container Apps probes (the only anonymous endpoint) |

### Could: later phases

C1 Optimistic concurrency (`rowversion`, 409 on a stale update) · C2 Repository
tests against a real SQL Server in a container (Testcontainers) · C3 Separate
read-only and read-write keys · C4 Level 5 entities (employees, maintenance)

### Not doing

- **Currency conversion, EUR values, report layouts, export files:** the
  client's job (§2).
- **Users, roles, login endpoints, JWT:** next project.
- **CORS:** the client is not a browser.
- **Swagger UI / Scalar:** S2 covers documentation without a UI to secure.
- **Changing an asset's type:** fixed at creation, as in Week 46; remove and
  re-add.
- **Managing offices:** seeded by the migration, read-only.

---

## 5. Domain rules

### Vocabulary

| Term | Meaning |
|------|---------|
| Asset | A company device: `ComputerAsset` (Laptop, Desktop) or `MobileAsset` (Phone, Tablet) |
| Category | Computer or Mobile. Derived from the type |
| Office | Germany (EUR), Sweden (SEK), USA (USD), Turkey (TRY). Seeded, read-only |
| Purchase price | What the office paid, in the asset's currency. Never converted by the API |
| Currency | The office's currency when the asset was created. Never changes |
| Lifetime | 3 years from the purchase date |
| End-of-life status | `None`, `Yellow` (< 6 months left), `Red` (< 3 months left), `DarkRed` (past 3 years) |
| API client | A program calling the API with a valid key |

### Rules

| # | Rule |
|---|------|
| R1 | Lifetime is 3 years. Bought 2023-05-10 → end of life 2026-05-10. On 2026-11-24 that asset is `DarkRed` |
| R2 | Status thresholds are purchase-date cutoffs from `AssetLifecycle`: on 2026-11-24, bought on/before 2023-11-24 → `DarkRed`; on/before 2024-02-24 → `Red`; on/before 2024-05-24 → `Yellow`; later → `None`. The status in a response and the `lifecycle` filter use the same cutoffs |
| R3 | `lifecycle=ExpiringSoon` means `Yellow` or `Red`; `Expired` means `DarkRed` |
| R4 | The currency is set from the office on create and never changes. An asset bought in Sweden for 14 990 SEK and moved to Germany is still 14 990 SEK |
| R5 | Prices in different currencies are never compared or summed by the API. Totals are grouped by office **and** currency; "most expensive" ranks within each currency. The client converts and merges |
| R6 | Serial numbers are unique, trimmed and compared **without letter case** (SQL Server's default collation: `ABC-1` and `abc-1` are the same). A duplicate gives 409. The in-memory test fakes must compare the same way |
| R7 | Purchase date is not in the future (UTC date); warranty expiration is not before the purchase date |
| R8 | Price > 0 and ≤ 150 000 000, rounded to 2 decimals (away from zero) |
| R9 | Brand, model, serial number: trimmed, non-empty, max 50. Assigned to: optional, trimmed, max 50, blank means none |
| R10 | Type and category are fixed after creation |
| R11 | Sorting always ends with `Id`, so equal rows keep their order and never jump between pages |
| R12 | A page number past the end returns the last page (Week 46 behaviour); `page` < 1 or `pageSize` outside 1–100 is a 400 |

Why R5 looks odd: 40 000 TRY is less than 1 500 EUR, so a single "most
expensive" list sorted by price would be wrong. The overall top N is always
among the per-currency top N, so the client's merged result is exact.

---

## 6. Data model

```
Office                          Asset (abstract, table-per-hierarchy "Assets")
  Id         int  PK              Id                      int  PK identity
  Name       nvarchar(50) unique  AssetCategory           nvarchar(20)  discriminator
  Country    nvarchar(50)         Type                    nvarchar(20)  Laptop/Desktop/Phone/Tablet
  Currency   nvarchar(3)          Brand                   nvarchar(50)
                                  Model                   nvarchar(50)
                                  SerialNumber            nvarchar(50)  unique
                                  PurchaseDate            date
                                  WarrantyExpirationDate  date
                                  PurchasePrice           decimal(18,2)
                                  Currency                nvarchar(3)
                                  OfficeId                int  FK → Office (Restrict)
                                  AssignedTo              nvarchar(50) null
```

Same schema as Week 46 (copied `OnModelCreating`), in its own database.

- **Stored:** only what is listed above. **Derived, never stored:** category
  label, end-of-life date, end-of-life status, any value in another currency.
- **Validation:** R6–R9; violations give 400 (or 409 for R6) with the field
  name in the ProblemDetails `errors`.
- **Who can do what:**

| Action | API client (valid key) | No key / wrong key |
|--------|------------------------|--------------------|
| Read offices, assets, reports | ✔ | ✘ 401 |
| Create, update, delete assets | ✔ | ✘ 401 |
| Health check (S3, if built) | ✔ | ✔ |

There is no per-object ownership: every client with a key sees every asset.
That is acceptable because there is one client and one owner (accepted risk
X1, §8).

### Contracts (JSON)

```
AssetResponse   { id, category, type, brand, model, serialNumber,
                  purchaseDate, warrantyExpirationDate, purchasePrice, currency,
                  officeId, officeName, assignedTo, endOfLifeDate, endOfLifeStatus }
PagedResponse<T>{ items, page, totalPages, totalCount }
OfficeResponse  { id, name, country, currency }
OfficeTotalResponse { officeId, officeName, currency, assetCount, totalPrice }
CreateAssetRequest  { type, brand, model, serialNumber, purchaseDate,
                      warrantyExpirationDate, purchasePrice, officeId, assignedTo? }
UpdateAssetRequest  { brand, model, serialNumber, purchaseDate,
                      warrantyExpirationDate, purchasePrice, officeId, assignedTo? }
```

camelCase, enums as strings, dates `yyyy-MM-dd`, prices as JSON numbers. No
request contains `id`, `currency` or `category`.

---

## 7. Structure

### Folder structure

```
asset-tracker-api/
├── CLAUDE.md
├── README.md
├── TESTING.md
├── asset-tracker-api.slnx
├── Directory.Build.props                  # copied from Week 46
├── .github/workflows/
│   ├── ci.yml                             # build, test, format on every push
│   └── deploy.yml                         # main only: image to ghcr.io, new revision
├── docs/
│   └── PROJECT_CONTEXT.md                 # this file
├── src/
│   ├── AssetTracker.Core/                 # no project or package references
│   │   ├── Models/                        # Asset, ComputerAsset, MobileAsset, Office, AssetDetails,
│   │   │                                  # AssetQuery, PagedResult, OfficeCurrencyTotal
│   │   ├── Enums/                         # AssetCategory, ComputerType, MobileType, CurrencyCode,
│   │   │                                  # EndOfLifeStatus, LifecycleFilter, AssetSortMode
│   │   ├── Abstractions/                  # IAssetRepository, IOfficeRepository (async)
│   │   ├── Services/                      # AssetLifecycle, AssetService, ReportService
│   │   └── Exceptions/                    # AssetValidationException, AssetNotFoundException,
│   │                                      # OfficeNotFoundException, DuplicateSerialNumberException,
│   │                                      # DataStoreException
│   ├── AssetTracker.Infrastructure/       # references Core
│   │   ├── Persistence/                   # AppDbContext, EfAssetRepository, EfOfficeRepository,
│   │   │                                  # DatabaseGuard, DatabaseInitializer, SampleData
│   │   ├── Migrations/                    # generated
│   │   └── DependencyInjection.cs         # AddInfrastructure(connectionString)
│   └── AssetTracker.Api/                  # references Core + Infrastructure
│       ├── Program.cs                     # composition root and pipeline only
│       ├── Controllers/                   # OfficesController, AssetsController, ReportsController
│       ├── Contracts/                     # request/response records + mapping
│       ├── Authentication/                # ApiKeyAuthenticationHandler, ApiKeyOptions
│       ├── Errors/                        # DomainExceptionHandler (IExceptionHandler)
│       ├── appsettings.json               # no secrets
│       └── AssetTracker.Api.http          # manual requests
└── tests/
    ├── AssetTracker.Core.Tests/
    └── AssetTracker.Api.Tests/
```

### Where logic lives

- `Core`: rules and calculations (R1–R12), services. No HTTP, no EF, no clock
  (`today` is passed in). Unit-tested.
- `Infrastructure`: EF Core, SQL, migrations, translating database failures
  into `DataStoreException` / `DuplicateSerialNumberException`.
- `Api`: HTTP only. Model binding and validation of requests, mapping to and
  from Core types, auth, rate limiting, error mapping.
- `Program.cs`: registration and middleware order. Nothing else.

### Endpoints

All under `/api`, all require `X-Api-Key` (M9).

| Method and route | Success | Errors |
|------------------|---------|--------|
| `GET /api/offices` | 200 `OfficeResponse[]` | 401, 429 |
| `GET /api/assets?…` | 200 `PagedResponse<AssetResponse>` | 400 (bad query), 401, 429 |
| `GET /api/assets/{id:int}` | 200 `AssetResponse` | 401, 404, 429 |
| `POST /api/assets` | 201 `AssetResponse` + `Location` | 400 (incl. unknown office), 401, 409 (serial), 429 |
| `PUT /api/assets/{id:int}` | 200 `AssetResponse` | 400 (incl. unknown office), 401, 404 (asset), 409, 429 |
| `DELETE /api/assets/{id:int}` | 204 | 401, 404, 429 |
| `GET /api/reports/office-totals` | 200 `OfficeTotalResponse[]` | 401, 429 |
| `GET /api/reports/most-expensive?count=` | 200 `AssetResponse[]` | 400, 401, 429 |
| `GET /api/assets/serial-number-available?…` (S1) | 200 `{ available }` | 400, 401, 429 |

Any endpoint can also return 503 (database unreachable, after retries) or 500
(a bug), both as ProblemDetails without internals.

### Error mapping (one `IExceptionHandler`)

| Exception | Status |
|-----------|--------|
| Model validation (`[ApiController]`) | 400 `ValidationProblemDetails` |
| `AssetValidationException` | 400, field in `errors` |
| `AssetNotFoundException` | 404 |
| `OfficeNotFoundException` (only raised for a body field) | 400, `errors.officeId` |
| `DuplicateSerialNumberException` (service check **and** unique-index violation, SQL error 2601/2627, caught in Infrastructure) | 409 |
| `DataStoreException` | 503 |
| anything else | 500, logged with a trace id, body has only the trace id |

### Classes (draft)

- `AssetService` (Core): `GetPageAsync`, `GetByIdAsync`, `AddAsync(type, details)`,
  `UpdateAsync`, `RemoveAsync`, `IsSerialNumberAvailableAsync`. Takes
  `TimeProvider` for today.
- `ReportService` (Core): `GetOfficeTotalsAsync`, `GetMostExpensivePerCurrencyAsync`.
- `AssetLifecycle` (Core): unchanged from Week 46.
- `EfAssetRepository` (Infrastructure): Week 46 queries made async, plus the
  search and filter criteria in the database.
- `AssetsController` etc. (Api): one service call per action, mapping via
  `AssetMappings`.
- `ApiKeyAuthenticationHandler` (Api): reads `X-Api-Key`, compares with each
  configured key using `CryptographicOperations.FixedTimeEquals`, on success
  creates a principal named after the client.

### Reuse map

From `WeeklyMiniProjects/Week46/` (**committed state only**; search and filter
are still uncommitted there on 2026-11-24, so `AssetQuery` here is written from
the Week 46 plan, not copied):

| Week 46 file | Lands in | Change |
|---|---|---|
| `Directory.Build.props`, `.gitignore` | repo root | Unchanged (`.gitignore` done) |
| `Core/Models/Asset.cs`, `ComputerAsset.cs`, `MobileAsset.cs`, `Office.cs`, `AssetDetails.cs` | `Core/Models/` | `today` passed in; `AssetValidationException` instead of `ArgumentException` for bad input |
| `Core/Models/PagedResult.cs`, `OfficeCurrencyTotal.cs` | `Core/Models/` | Unchanged |
| `Core/Models/AssetQuery.cs` | `Core/Models/` | Adds brand, model, office, purchase year, category, lifecycle; `MaxPageSize = 100` |
| `Core/Enums/*` | `Core/Enums/` | Add `LifecycleFilter` |
| `Core/Services/AssetLifecycle.cs` | `Core/Services/` | Unchanged |
| `Core/Services/AssetService.cs` | `Core/Services/` | Async, `TimeProvider`, no currency provider, `AddAsync(type, …)` |
| `Core/Exceptions/*` | `Core/Exceptions/` | Add `AssetValidationException` |
| `Core/Abstractions/IAssetRepository.cs`, `IOfficeRepository.cs` | `Core/Abstractions/` | Async + `CancellationToken`; `GetAll` dropped (no export) |
| `Infrastructure/Persistence/AppDbContext.cs` | same | `DbContextOptions` constructor, no `ConfigurationBuilder`/`OnConfiguring`, `EnableRetryOnFailure` |
| `Infrastructure/Persistence/EfAssetRepository.cs`, `EfOfficeRepository.cs`, `DatabaseGuard.cs` | same | Async; filters; unique-violation → `DuplicateSerialNumberException`; `SaveOrForget` not needed (one context per request) |
| `Infrastructure/Persistence/DatabaseInitializer.cs`, `SampleData.cs` | same | Async; Development only |

**Not ported:** the whole `ConsoleApp` project, `Currency/` (providers,
`CurrencyConversion`), `ReportService`'s EUR logic, `Report`/`ReportColumn`,
`AppPaths`, the old migration (a fresh `InitialCreate` here).

---

## 8. Security

**Researched on 2026-11-24.** Before the first code, and again when a framework,
library or security-relevant feature is added, Claude searches for known
security holes and common mistakes in similar applications and in our exact
frameworks, tools and versions (CLAUDE.md, "Security"). The result goes here.

**Sources checked:**

- OWASP API Security Top 10 2023 (still the current edition):
  https://owasp.org/API-Security/editions/2023/en/0x11-t10/
- OWASP REST Security Cheat Sheet:
  https://cheatsheetseries.owasp.org/cheatsheets/REST_Security_Cheat_Sheet.html
- .NET November 2026 servicing updates (25 CVEs, mostly ASP.NET Core; fixed in
  10.0.15): https://devblogs.microsoft.com/dotnet/dotnet-and-dotnet-framework-november-2026-servicing-updates/
- CVE-2026-62903, Microsoft.Data.SqlClient information disclosure (CVSS 7.4,
  fixed in 6.1.7): https://msrc.microsoft.com/update-guide/vulnerability/CVE-2026-62903
- Microsoft.Data.SqlClient 6.1.8 release (dependency minimums raised for
  CVE-2026-84008): https://techcommunity.microsoft.com/blog/SQLServer/microsoft-data-sqlclient-7-1-2-7-0-4-and-6-1-8-are-now-available/4573321
- EF Core 10.0.15 dependencies: https://www.nuget.org/packages/Microsoft.EntityFrameworkCore.SqlServer/10.0.15
- ASP.NET Core rate limiting: https://learn.microsoft.com/aspnet/core/performance/rate-limit
- ASP.NET Core behind proxies (forwarded headers):
  https://learn.microsoft.com/aspnet/core/host-and-deploy/proxy-load-balancer
- Container Apps ingress headers: https://learn.microsoft.com/azure/container-apps/network-proxy
  and https://learn.microsoft.com/azure/container-apps/ingress-overview
- Azure SQL network access and "Allow Azure services":
  https://learn.microsoft.com/azure/azure-sql/database/network-access-controls-overview
- Microsoft Entra-only authentication:
  https://learn.microsoft.com/azure/azure-sql/database/authentication-azure-ad-only-authentication
- Passwordless (managed identity) connections from .NET:
  https://learn.microsoft.com/azure/azure-sql/database/azure-sql-passwordless-migration
- Container Apps managed identity: https://learn.microsoft.com/azure/container-apps/managed-identity

### Known risks and how we prevent them

| # | Risk (known hole or common mistake) | Where it applies here | How we prevent it | Source | Step (§11) |
|---|------|-----------------------|-------------------|--------|-----------|
| X1 | API1 Broken object level authorization: a caller reads or changes objects that aren't theirs | Every `{id}` route | Single tenant: one owner, every valid client may see every asset. No ownership to check yet. **Accepted** (below); revisit with Level 5 users | OWASP API1 | — |
| X2 | API2 Broken authentication: missing checks, guessable or leaked keys, keys in URLs, timing leaks | Every endpoint | Authentication handler + **fallback policy** (deny by default). Keys: 32 random bytes as 64 hex characters, one per client, compared with `CryptographicOperations.FixedTimeEquals`. Header only, never a query string. HTTPS only. Same 401 for missing and wrong key. Failed attempts rate-limited per client IP (X4). Test that every mapped endpoint requires auth | OWASP API2, REST cheat sheet | 10 |
| X3 | API3 Broken object property level authorization (mass assignment, excessive data exposure) | `POST`/`PUT` bodies, all responses | Requests bind to request records with only client-settable fields; `UnmappedMemberHandling.Disallow` rejects anything else (e.g. `currency`, `id`). Responses are explicit records, never entities | OWASP API3 | 9, 12 |
| X4 | API4 Unrestricted resource consumption (huge pages, huge bodies, request floods) | Lists, reports, all endpoints | `pageSize` ≤ 100, `count` ≤ 50, request body limit 64 KB, built-in rate limiter (fixed window of 1 minute: 60 requests per client key when authenticated, 10 per client IP otherwise; 429 + `Retry-After`). Container App max replicas = 1, so the in-memory limiter sees every request | OWASP API4, ASP.NET Core rate limiting | 9, 15 |
| X5 | API8 Security misconfiguration (debug pages, docs, HTTP, server banners) | `Program.cs`, Azure settings | `ASPNETCORE_ENVIRONMENT=Production` in Azure: no developer exception page, OpenAPI only in Development, HSTS. Ingress `allowInsecure=false`. Kestrel `AddServerHeader = false`. Host filtering: `AllowedHosts` is `*` from the template; in Production set it to our host names only (custom domain + the `*.azurecontainerapps.io` FQDN), via a Container Apps environment variable, after checking that Container Apps health probes still pass. No CORS. Non-root `app` user (default in .NET container images) | OWASP API8 | 15, 16 |
| X6 | Injection | Search parameters | EF Core LINQ only (parameterised). No `FromSqlRaw`; no string-built SQL | OWASP REST | 8 |
| X7 | Secrets in a public repo or public image | Repo, image, workflow, `.http` file | No secrets in `appsettings*.json`; API key as a Container Apps secret; DB has no password (managed identity); `.http` secrets in the git-ignored `http-client.env.json.user`; GitHub → Azure via OIDC (no stored Azure secret). The image is public, so nothing secret is ever baked in | CLAUDE.md rules | 2, 10, 18 |
| X8 | Error responses leak internals | Every failure | One `IExceptionHandler` mapping to ProblemDetails; 500s return only a trace id; infrastructure exception messages never reach the client | OWASP REST | 9 |
| X9 | Database reachable from the internet | Azure SQL logical server | Entra-only authentication (SQL logins can't connect at all), managed identity with `db_datareader` + `db_datawriter` only, `Encrypt=True`. "Allow Azure services" must be on because Container Apps (consumption) has no fixed outbound IP, which lets any Azure IP reach the port. **Accepted** (below) | Azure SQL network access, Entra-only auth | 17 |
| X10 | Known-vulnerable framework and packages | Runtime, EF Core, SqlClient | Runtime/SDK 10.0.15+ (Nov 2026 CVEs). EF Core 10.0.15 + direct reference `Microsoft.Data.SqlClient` 6.1.8 (CVE-2026-62903 below 6.1.7; CVE-2026-84008 dependency fix in 6.1.8). NuGet audit runs on restore and its warnings fail the build (`TreatWarningsAsErrors`); `dotnet list package --vulnerable --include-transitive` in CI. Container base image tag `10.0` so every build gets the latest patch | .NET Nov 2026 servicing, MSRC, SqlClient release | 2, 4 |
| X11 | Spoofed client IP defeats per-IP limits | Rate limiter (X4) | Forwarded Headers middleware first in the pipeline, `X-Forwarded-For` + `X-Forwarded-Proto`, `ForwardLimit = 1` (only the right-most entry, the one Container Apps sets). Known proxies are cleared because the ingress IPs aren't fixed; the app is only reachable through the ingress | ASP.NET Core proxy docs, Container Apps ingress | 15 |
| X12 | Over-privileged CI/CD | GitHub Actions | Workflow `permissions:` minimal (`contents: read`; `packages: write` and `id-token: write` only in the deploy job). The Azure identity used by the pipeline has rights on this resource group only. Actions pinned to versions | Least privilege | 4, 18 |
| X13 | Personal data | `AssignedTo` | Only invented names in sample data, tests, `.http` and README. Never logged | CLAUDE.md | 8 |
| X14 | Surprise bill ("denial of wallet") | Azure | Scale to zero, max replicas 1, logs off, free SQL offer set to **auto-pause until next month** when the free amount is used | Azure SQL free offer | 17 |

### Trust boundaries and data

- **Untrusted input:** request body, route values, query string and every
  header, including `X-Api-Key` and `X-Forwarded-For`. Validated by
  `[ApiController]` model validation (types, required, lengths, enum values,
  ranges for paging) and then by the domain (R6–R9).
- **Personal data stored:** `AssignedTo` (an employee name or username,
  optional, max 50). Needed for the brief's "employee/username" field. Visible
  to API clients only. Kept until the asset is deleted.
- **Secrets** (names only): `ApiKeys__<ClientName>` (e.g.
  `ApiKeys__Week46Console`). Locally in user secrets, in Azure as a Container
  Apps secret. `APP_DB_CONNECTION` contains no password in any environment.
  GitHub repository **variables** (not secrets, they are identifiers):
  `AZURE_CLIENT_ID`, `AZURE_TENANT_ID`, `AZURE_SUBSCRIPTION_ID`.

### Dependencies

To be checked with `dotnet list package --vulnerable --include-transitive` in
step 2 (2026-11-24 research below).

| Package | Version | Known advisories | Action |
|---------|---------|------------------|--------|
| .NET runtime / ASP.NET Core | 10.0.15 | Nov 2026 CVEs fixed in 10.0.15 | SDK 10.0.404 (or 10.0.115) on the dev machine; `aspnet:10.0` base image |
| `Microsoft.EntityFrameworkCore.SqlServer` | 10.0.15 | none listed | Use 10.0.15 |
| `Microsoft.Data.SqlClient` (transitive) | 6.1.8 | CVE-2026-62903 below 6.1.7; CVE-2026-84008 fixed by 6.1.8's dependency minimums | Direct `PackageReference` to 6.1.8 |
| `System.Security.Cryptography.Pkcs` (transitive) | ≥ 10.0.13 via EF | none found | No explicit reference unless step 2 flags it |
| `Microsoft.AspNetCore.OpenApi` | 10.0.15 | none found | Keep |

### Accepted risks

Agreed with Niclas, not decided by Claude (pending confirmation, §14).

- **X1, no per-object authorization:** one owner, one client. Fix when Level 5
  adds users and roles.
- **X9, "Allow Azure services":** any Azure-hosted machine can reach the SQL
  port, but with Entra-only authentication it cannot log in without a token
  for this tenant. The fix (VNet + private endpoint) costs money; revisit if
  the API ever holds real data.
- **No stored logs:** past incidents can't be investigated afterwards (OWASP's
  old "insufficient logging" item). Fine for a demo API without real data.
- **One shared key per client:** if the Week 46 machine is compromised, the key
  is too. Rotation: add a second key, switch the client, remove the old one.

---

## 9. Testing plan

Setup: xUnit (versions in §10), `tests/AssetTracker.Core.Tests` (references
Core only) and `tests/AssetTracker.Api.Tests` (`WebApplicationFactory<Program>`).
Time is controlled with a small `FakeTimeProvider` subclass of `TimeProvider`
in the test project (no extra package). Test data comes from small builders,
not copied from `SampleData`.

**Unit tests (written with each piece, same commit)**

| Target | What we test |
|--------|--------------|
| `AssetLifecycle` | Table-driven: each cutoff exactly on, one day before and one day after (R1–R2), leap day 2024-02-29 |
| `Asset` / `ComputerAsset` / `MobileAsset` | R7–R10: blank/whitespace/51-char text, price 0 / 0.001 / 150 000 000 / 150 000 000.01, rounding 1.005 → 1.01, future purchase date, warranty before purchase, `AssignedTo` blank → null, failed `Update` leaves the asset unchanged |
| `AssetService` | Currency comes from the office (R4), update keeps currency when the office changes, duplicate serial (R6) on add and update (excluding itself), unknown asset / office, `lifecycle` filter translated to the right purchase-date bounds |
| `ReportService` | Passes totals and per-currency top N through unchanged; `count` bounds |

**Integration tests (`AssetTracker.Api.Tests`)** with the repositories replaced
by in-memory fakes:

- Every mapped endpoint returns 401 without a key and with a wrong key (found
  by enumerating the endpoint data source, so a new endpoint can't be missed).
- Each status code in §7: 200/201 (+ `Location`)/204/400/404/409/429/503/500
  and the ProblemDetails shape (no stack trace, no exception message on 500).
- Request validation: unknown JSON field (`currency`), missing `brand`,
  `pageSize=101`, `page=0`, unknown `type`, bad date format.
- JSON shape: enums as strings, dates `yyyy-MM-dd`, camelCase.
- Rate limiting with a tiny configured limit → 429 with `Retry-After`.

**Security tests:** X2, X3, X4, X8 above are covered by the integration tests;
X5 by a test that runs the factory in `Production` and checks there is no
OpenAPI document and no developer exception page.

**Manual tests:** documented in `TESTING.md`: repository queries (search,
filters, sorting, paging, aggregates) against local SQL Express through the
`.http` file; after each deploy, a smoke test against the live URL (401
without key, list with key, create/update/delete round trip); cold-start time
after the app and database have paused.

**Not tested automatically, and why:** EF Core query translation against real
SQL Server (C2 would add it with a SQL Server container); Azure configuration
(checked by the manual smoke test).

---

## 10. Packages and scaffolding

Exactly these. Ask before adding anything else.

| Package | Version | Project | Used for |
|---------|---------|---------|----------|
| `Microsoft.AspNetCore.OpenApi` | 10.0.15 | Api | OpenAPI document (Development) |
| `Microsoft.EntityFrameworkCore.Tools` | 10.0.15 | Api (`PrivateAssets=all`) | `dotnet ef` migrations |
| `Microsoft.EntityFrameworkCore.SqlServer` | 10.0.15 | Infrastructure | EF Core provider |
| `Microsoft.Data.SqlClient` | 6.1.8 | Infrastructure | Pins the patched driver (X10) |
| `xunit` | 2.9.3 | both test projects | Tests (Milestone versions) |
| `xunit.runner.visualstudio` | 3.1.4 | both test projects | Test Explorer / `dotnet test` |
| `Microsoft.NET.Test.Sdk` | 17.14.1 | both test projects | Test host |
| `coverlet.collector` | 6.0.4 | both test projects | Coverage |
| `Microsoft.AspNetCore.Mvc.Testing` | 10.0.15 | Api.Tests | `WebApplicationFactory` |

Rate limiting, forwarded headers, authentication, ProblemDetails, health checks
and container publishing are built into ASP.NET Core / the .NET SDK: no
packages.

```
# Indicative: check the current docs first. Run from the repo root.
dotnet new globaljson --sdk-version 10.0.404 --roll-forward latestFeature
dotnet new tool-manifest
dotnet tool install dotnet-ef --version 10.0.15
dotnet new classlib -o src/AssetTracker.Core
dotnet new classlib -o src/AssetTracker.Infrastructure
# move asset-tracker-api/ to src/AssetTracker.Api, rename the .csproj, RootNamespace AssetTracker.Api
dotnet new xunit -o tests/AssetTracker.Core.Tests
dotnet new xunit -o tests/AssetTracker.Api.Tests
dotnet sln asset-tracker-api.slnx add src/*/*.csproj tests/*/*.csproj
dotnet user-secrets init --project src/AssetTracker.Api
```

- Environment variables / configuration: `APP_DB_CONNECTION`,
  `ApiKeys__<ClientName>`, `ASPNETCORE_ENVIRONMENT`. No `.env` file (.NET uses
  user secrets).
- **Gotchas found during the check:**
  - .NET container images listen on **8080** and run as the non-root `app`
    user; the Container Apps ingress target port is 8080.
  - TLS ends at the Container Apps ingress; the app receives plain HTTP. The
    Forwarded Headers middleware must run first so `Request.Scheme` is `https`
    (otherwise HTTPS redirection and HSTS misbehave).
  - Since ASP.NET Core 8.0.17 / 9.0.6 forwarded headers from unknown proxies
    are ignored, so the known networks/proxies must be cleared explicitly (X11).
  - `Microsoft.Data.SqlClient` 6.1.6+ pulls in `Microsoft.Identity.Client.Broker`
    (bigger publish output, EF Core 10 breaking-changes page). We need Entra
    authentication anyway.
  - `EnableRetryOnFailure` doesn't work with user-started transactions without
    an execution strategy; we don't use explicit transactions.
  - `WebApplicationFactory<Program>` needs `Program` to be visible to the test
    project. Checked in step 3: in .NET 10 the Web SDK's source generator adds
    `public partial class Program` for top-level `Program.cs`, so we don't
    declare it ourselves.
  - Attributes can't take `decimal` constants, so the price range is enforced
    by the domain (R8), not by `[Range]` with `Asset.MaxPurchasePrice`.
  - Pick the Azure region before creating the free SQL database: the region of
    the first free database applies to all free databases in the subscription.

---

## 11. Build plan (one step = one commit)

**Rules**

- **One step = one commit.** Tests go in the same commit as the code they test.
  If a step grows past one commit, split it. If a fix needs another fix, stop
  and rethink (CLAUDE.md).
- **Niclas makes every commit himself.** Claude finishes the step, runs the
  checks, says what to look at and suggests the commit message from the last
  column.
- Commit messages are very short, imperative and lowercase. No prefix.
- **Logic before HTTP, security before endpoints.** Domain code is written
  test-first; authentication exists before the first data endpoint.
- The *Covers* column points to features (§4), rules (§5) and risks (§8). "—"
  in *Commit* means a checkpoint by Niclas, not a commit.

### Phases

| Phase | Content | Steps |
|-------|---------|-------|
| 0 | Docs, scaffold, tests, CI | 1–4 |
| 1 | Domain and services, test-first | 5–6 |
| 2 | Persistence | 7–8 |
| 3 | HTTP foundation and auth | 9–10 |
| 4 | Endpoints | 11–14 |
| 5 | Hardening and deploy | 15–19 |
| 6 | Final test and docs | 20–21 |

### Phase 0 – Setup

| # | Step | Covers | Check before committing | Suggested commit |
|---|------|--------|-------------------------|------------------|
| 1 | `CLAUDE.md`, `.gitignore`, `docs/PROJECT_CONTEXT.md`, `TESTING.md`, first `README.md` (simple template, "in progress") | — | Docs agree with each other | `add project docs` |
| 2 | Scaffold: `global.json`, local tool manifest with `dotnet-ef` 10.0.15, move the template to `src/AssetTracker.Api`, delete WeatherForecast, `Directory.Build.props`, Core and Infrastructure class libraries, references, packages from §10 | §10, X10 | `dotnet --version` in the repo shows 10.0.x; `dotnet build` clean; Core has no references; `dotnet list package --vulnerable --include-transitive` clean | `scaffold solution` |
| 3 | Both test projects with one smoke test each (Core: trivial; Api: the app starts and an unknown route gives 404) | §9 | `dotnet test` green | `set up tests` |
| 4 | `ci.yml`: restore, build, test, `dotnet format --verify-no-changes`, vulnerable-package check, on every push and pull request; minimal `permissions` | X10, X12 | Workflow green on GitHub | `add ci workflow` |

### Phase 1 – Domain

| # | Step | Covers | Check before committing | Suggested commit |
|---|------|--------|-------------------------|------------------|
| 5 | Copy and adapt the domain: enums (+ `LifecycleFilter`), `Office`, `Asset` (`today` passed in, `AssetValidationException`), `ComputerAsset`, `MobileAsset`, `AssetDetails`, `AssetLifecycle`, exceptions. Tests first | R1–R2, R7–R10 | Tests green; Core still has no references | `add domain model` |
| 6 | `AssetQuery` (all criteria, page-size limits), `PagedResult`, `OfficeCurrencyTotal`, async repository interfaces, `AssetService` and `ReportService` with `TimeProvider`; tests with fake repositories | R3–R6, R11–R12 | Tests green | `add asset and report services` |

### Phase 2 – Persistence

| # | Step | Covers | Check before committing | Suggested commit |
|---|------|--------|-------------------------|------------------|
| 7 | `AppDbContext` (options constructor, Fluent config copied, office seed), `AddInfrastructure`, `EnableRetryOnFailure`, `InitialCreate` migration | §6 | `dotnet ef database update` (Niclas) creates `asset_tracker_api` with `Assets` and 4 `Offices` | `add database context and migration` |
| 8 | Async EF repositories: paging, sorting, search, filters, aggregates, unique-violation → `DuplicateSerialNumberException`; `DatabaseGuard`; startup: migrations in Development only, `SampleData` when `Database:SeedSampleData` is true and `Assets` is empty (true in `appsettings.Development.json`) | M2, M7, M8, R5, R11, X6, X13 | Niclas runs the API once: 25 sample assets; second start inserts none | `add ef repositories` |

### Phase 3 – HTTP foundation and auth

| # | Step | Covers | Check before committing | Suggested commit |
|---|------|--------|-------------------------|------------------|
| 9 | Error handling (`IExceptionHandler`, ProblemDetails), JSON options (string enums, `Disallow` unmapped members), request body limit, `AddServerHeader = false` | M10, X3, X4, X5, X8 | Integration tests for 400/404/409/503/500 shapes via a test-only endpoint or fakes | `add error handling` |
| 10 | API key authentication handler, `ApiKeys` options, fallback policy, `.http` environment file pattern | M9, X2, X7 | Tests: 401 missing / wrong key, 200 with key; all-endpoints-protected test | `add api key authentication` |

### Phase 4 – Endpoints

| # | Step | Covers | Check before committing | Suggested commit |
|---|------|--------|-------------------------|------------------|
| 11 | Contracts + mapping, `GET /api/offices` | M1 | Tests green; `.http` request works locally | `add offices endpoint` |
| 12 | `GET /api/assets` (all query parameters) and `GET /api/assets/{id}` | M2, M3, R12 | Tests for parameters and limits; manual search/filter/sort/paging checks from `TESTING.md` | `add asset read endpoints` |
| 13 | `POST`, `PUT`, `DELETE /api/assets` | M4–M6, R4, R6, X3 | Tests for 201 + `Location`, 409, 400 on `currency` in the body; manual round trip | `add asset write endpoints` |
| 14 | Report endpoints (+ S1 if agreed) | M7, M8, S1 | Tests; totals checked by hand against the sample data | `add report endpoints` |

### Phase 5 – Hardening and deploy

| # | Step | Covers | Check before committing | Suggested commit |
|---|------|--------|-------------------------|------------------|
| 15 | Forwarded headers, rate limiting, production pipeline settings (HSTS, no OpenAPI, no developer page, `AllowedHosts` limited to our host names, see X5) | M11, X4, X5, X11 | Tests: 429 + `Retry-After`; Production-mode test | `add rate limiting and production settings` |
| 16 | Container settings in `AssetTracker.Api.csproj` (repository, base image tag `10.0`, port) | X5, X10 | Niclas: `dotnet publish /t:PublishContainer` locally builds an image that starts | `add container settings` |
| 17 | Azure setup by Niclas following §12 (resource group, SQL, Container Apps, identity, secrets, DB user, migration script) | X9, X14 | Smoke test against the `*.azurecontainerapps.io` URL | — |
| 18 | `deploy.yml`: on `main` after tests, push image to `ghcr.io` (`GITHUB_TOKEN`), sign in to Azure with OIDC, deploy a new revision | M12, X7, X12 | A push to `main` deploys; the new revision answers | `add deploy workflow` |
| 19 | Custom domain + managed certificate (Niclas), live URL in README | M12 | `https://asset-tracker-api.niclashugdahl.dev` answers with a valid certificate | `add live url to readme` |

### Phase 6 – Finish

| # | Step | Covers | Check before committing | Suggested commit |
|---|------|--------|-------------------------|------------------|
| 20 | Full test against §15, fix what it finds | §15 | Every box ticked | `fix issues from final test` |
| 21 | Finish `README.md` and `TESTING.md` (results, screenshots of `.http` responses), drop "in progress" | — | A fresh clone runs locally by following the README | `finish readme` |

### Needs from Niclas

| Before step | What |
|-------------|------|
| 2 | .NET 10 SDK 10.0.404 or later installed next to the .NET 11 SDK (`dotnet --list-sdks`) |
| 7 | SQL Server Express running locally |
| 10 | A generated API key for local use, stored with `dotnet user-secrets` |
| 17 | An Azure **subscription** with a payment method under the Microsoft account (signing in alone doesn't create one); ability to add DNS records for `niclashugdahl.dev`; yes/no on the remaining Proposed rows in §2 and the accepted risks in §8 |
| 18 | Repository variables `AZURE_CLIENT_ID`, `AZURE_TENANT_ID`, `AZURE_SUBSCRIPTION_ID` from the step 17 setup |

---

## 12. Deployment

Claude writes the exact commands when we get here (checked against the current
docs); Niclas runs them. Order:

1. **Resource group** `asset-tracker-api` in **Sweden Central**. Check first
   that the free SQL offer and Container Apps are offered there for the
   subscription.
2. **Azure SQL logical server** with Niclas's Entra account as **Entra admin**
   and **Entra-only authentication** on. Firewall: "Allow Azure services" on;
   Niclas's own IP added only while running scripts, then removed.
3. **Database** `asset_tracker_api` with the **free offer** (serverless),
   "auto-pause the database until next month" when the free amount is used.
4. **Container Apps environment** with logs destination `none`.
5. **Container App** from the `ghcr.io` image: external ingress, target port
   8080, `allowInsecure=false`, min replicas 0, max 1, **system-assigned
   identity** on. Secret `api-key-week46console`; environment variables
   `ASPNETCORE_ENVIRONMENT=Production`,
   `ApiKeys__Week46Console=secretref:api-key-week46console`,
   `APP_DB_CONNECTION=Server=tcp:<server>.database.windows.net,1433;Database=asset_tracker_api;Authentication=Active Directory Managed Identity;Encrypt=True;TrustServerCertificate=False;`.
   If pulling from `ghcr.io` fails without credentials, add a read-only GitHub
   token as a registry secret (§14, open question 1).
6. **Database user for the app** (as Entra admin, in `asset_tracker_api`):
   `CREATE USER [<container-app-name>] FROM EXTERNAL PROVIDER;` then
   `ALTER ROLE db_datareader ADD MEMBER [...]` and `db_datawriter`.
7. **Schema:** `dotnet ef migrations script --idempotent` → run as Entra admin.
   Repeated for every later migration, before the code that needs it deploys.
8. **Sample data:** start the app once with `Database__SeedSampleData=true`
   (the app user has `db_datawriter`, enough to insert), confirm 25 assets,
   then remove the setting so an emptied table is never refilled by surprise.
   Purchase dates are relative to the seeding day, so the statuses age like
   real data afterwards.
9. **GitHub → Azure:** an Entra app registration (or user-assigned identity)
   with a federated credential for `repo:NiclO1337/asset-tracker-api:ref:refs/heads/main`,
   role on the resource group only; IDs stored as repository variables.
10. **Custom domain:** CNAME `asset-tracker-api` → the app's
   `*.azurecontainerapps.io` host, TXT `asuid.asset-tracker-api` with the
   verification id, then add the domain with a **managed certificate**.
11. **Check:** 401 without key, 200 with key, a create/update/delete round
    trip, the certificate, and the time of the first request after a pause.

The repo is public: never commit `appsettings.*.json` with values, user
secrets, `http-client.env.json.user` or any key.

---

## 13. Risks

| Risk | Mitigation |
|------|------------|
| **Scope** grows (exports, conversion, users creep in) | §2 responsibility line and §4 "Not doing" |
| **New versions** of .NET 10 / EF Core 10 differ from older tutorials | Current docs first, versions pinned (§10) |
| **Azure complexity**: many separate resources, easy to misconfigure | One ordered checklist (§12), Claude writes commands, Niclas runs them, smoke test after |
| **Cold starts**: app and database both pause; the first request can take long or fail once | EF retry on failure; client timeout and one retry in the Week 46 client; documented in README |
| **Free limits** run out (100 000 vCore seconds per month for the DB) | Auto-pause until next month instead of billing; max 1 replica |
| **Copied code drifts** from Week 46 | Reuse map (§7); the Week 46 rebuild takes the API as its source of truth |
| **GHCR pull** needs credentials despite a public image | Fallback in §12 step 5 |

---

## 14. Review decisions and open questions

### Decided

| # | Date | Decision |
|---|------|----------|
| 1 | 2026-11-24 | Own database; Week 46's `asset_tracker_ef` is never touched |
| 2 | 2026-11-24 | Deployed publicly on Azure with its own external database; custom domain `asset-tracker-api.niclashugdahl.dev` |
| 3 | 2026-11-24 | The API owns data and rules only; currency conversion, report layout and export stay in the Week 46 client, which will be rebuilt to use the API |
| 4 | 2026-11-24 | Automated tests + `TESTING.md` |
| 5 | 2026-11-24 | Azure Container Apps + Azure SQL free offer (not App Service Free, not Heroku) |
| 6 | 2026-11-24 | Simple login: API key per client. Identity/`AspNetUsers` is for the next project |
| 7 | 2026-11-24 | Managed identity for the database |
| 8 | 2026-11-24 | Image on GitHub Container Registry, not Azure Container Registry |
| 9 | 2026-11-24 | No log storage; live log stream only |
| 10 | 2026-11-24 | No deadline |
| 11 | 2026-11-27 | Packages 10.0.15 + `Microsoft.Data.SqlClient` 6.1.8 (security fixes, §8 X10) |
| 12 | 2026-11-27 | xUnit 2.9.3 with the Milestone versions |
| 13 | 2026-11-27 | API integration tests use in-memory fake repositories |
| 14 | 2026-11-27 | Unknown office in a request body → 400 |
| 15 | 2026-11-27 | The live database is seeded once with the 25 sample assets |
| 16 | 2026-11-27 | Region Sweden Central |
| 17 | 2026-11-27 | Rate limits 60/min per key, 10/min per IP without a valid key (Niclas: lower than first proposed) |
| 18 | 2026-11-27 | Stay on .NET 10 (same support end as .NET 11, Nov 14 2028; matches the course and all reused code). SDK pinned with `global.json`. .NET 11 is for the next project |

### Still open – do not invent answers

1. **GHCR pull credentials:** found during step 17 (Microsoft's docs disagree
   on whether a public `ghcr.io` image needs credentials).
2. **Proposed rows in §2** (domain errors, UTC today, single `type` field,
   unknown JSON fields, firewall, OIDC) and the **accepted risks** in §8 need
   a yes before the step that uses them: domain rows before step 5, JSON
   before step 9, firewall and accepted risks before step 17, OIDC before
   step 18.
3. **Azure subscription:** confirm one exists with a payment method before
   step 17.

---

## 15. Definition of done

- [ ] All goals in §1 are ticked
- [ ] All Must features (§4) work on the deployed API
- [ ] Create → read → update → delete works end to end against the live URL,
      including corrections (duplicate serial, bad date) giving clear 4xx
- [ ] Data is still there after the app and database have paused and resumed
- [ ] Bad input is handled everywhere: empty, wrong type, too long, out of
      range, unknown fields, bad dates
- [ ] Every risk in §8 has its prevention in place; 401/429 tested; the
      dependency check is clean; no secrets or real personal data in the repo
      or the image
- [ ] Tests pass in CI, `dotnet format` clean, zero warnings
- [ ] A push to `main` deploys without manual steps
- [ ] README and `TESTING.md` complete; a fresh clone runs locally by following
      the README
- [ ] Repo public, live URL in the README

---

## 16. Later, if there is time

Not in scope now. Listed so today's design does not block them.

- *Week 46 client rebuild* (in `WeeklyMiniProjects`): an `ApiAssetRepository`
  / `ApiOfficeRepository` implementing the Week 46 interfaces over `HttpClient`,
  the API key in user secrets, a timeout and one retry for cold starts. The
  menus barely change; currency conversion, reports and exports stay there.
- *Level 5:* employees and assignment (`AssignedTo` becomes a foreign key in
  one migration), roles (separate keys or real users in the next project),
  maintenance tracking, dashboard figures as new report endpoints.
- *C1–C4* in §4.
- *Published OpenAPI document* (S2) and a short API reference in the README.
