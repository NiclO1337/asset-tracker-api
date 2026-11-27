# Dragon's Hoard API — Asset Tracker API

A REST API for tracking company IT assets across offices and currencies, built
with ASP.NET Core 10, Entity Framework Core and Azure SQL, and deployed on
Azure Container Apps.

> Self-study extension of the weekly mini project, week 46 — Full Stack C# .NET
> Web Development with AI-powered development
> Author: Niclas Hugdahl

> **Status: in progress.** The design is finished and the API is being built
> step by step from the
> [build plan](docs/PROJECT_CONTEXT.md#11-build-plan-one-step--one-commit).
> This README describes the finished API; [Progress](#progress) shows what
> works today.

Live API: `https://asset-tracker-api.niclashugdahl.dev` (requires an API key)
<!-- TODO: confirm the live URL after the first deploy (build step 19) -->

---

## Table of contents

- [About](#about)
- [Progress](#progress)
- [Features](#features)
- [Getting started](#getting-started)
- [Usage](#usage)
- [Project structure](#project-structure)
- [Design decisions](#design-decisions)
- [Testing](#testing)
- [Planning and documentation](#planning-and-documentation)
- [Future features](#future-features)
- [Technologies used](#technologies-used)
- [Credits](#credits)

---

## About

The API is the database behind the
[Week 46 asset tracker](https://github.com/NiclO1337/weekly-projects-lexicon/tree/main/Week46),
a console app that keeps track of laptops, desktops, phones and tablets in four
offices (Germany, Sweden, USA and Turkey). It stores what each office paid, in
its own currency, and enforces the asset rules: a three-year lifetime with
warnings near the end, unique serial numbers, and dates and prices that make
sense. The console app will be rebuilt as a client of this API and keeps the
parts that belong to a client: exchange rates, reports and file export.

It is built in three layers, and dependencies point one way only:
`AssetTracker.Core` holds the domain rules and services and references nothing;
`AssetTracker.Infrastructure` stores data with EF Core; `AssetTracker.Api` is
the thin HTTP layer on top.

---

## Progress

| Phase | Content | Status |
|-------|---------|--------|
| 0 | Docs, solution, tests, CI | In progress |
| 1 | Domain and services | Not started |
| 2 | Database | Not started |
| 3 | Error handling and API key | Not started |
| 4 | Endpoints | Not started |
| 5 | Hardening and deploy to Azure | Not started |
| 6 | Final test and docs | Not started |

---

## Features

- **Assets:** create, read, update and delete laptops, desktops, phones and
  tablets.
- **Search and filter:** by brand, model, office, purchase year, category
  (computers / mobile devices) and lifetime (expiring soon / expired), combined
  freely, sorted by office, category or end of life, one page at a time.
- **Lifetime status:** every asset shows its end-of-life date and a status:
  more than 6 months left, less than 6, less than 3, or past its 3-year
  lifetime.
- **Local prices:** each asset keeps the price and currency its office paid;
  the API never mixes currencies.
- **Report figures:** asset count and total price per office and currency, and
  the most expensive assets within each currency.
- **Security:** every endpoint needs an API key; rate limiting; clear error
  responses (ProblemDetails) without internal details.

TODO: update at the end.

---

## Getting started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) (10.0.404 or
  a later 10.0 version). The repo's `global.json` selects it even when a newer
  SDK is installed
- [SQL Server Express](https://www.microsoft.com/sql-server/sql-server-downloads)
  running as `localhost\SQLEXPRESS`

Check what you have:

```bash
dotnet --version
```

### Build and run

```bash
git clone https://github.com/NiclO1337/asset-tracker-api.git
cd asset-tracker-api

dotnet tool restore
dotnet build
dotnet run --project src/AssetTracker.Api
```

The first start in Development creates the `asset_tracker_api` database,
applies the migrations and adds 25 sample assets.

**API key (required).** Every request needs a key in the `X-Api-Key` header.
Create one for local use and store it in user secrets, never in a file in the
repo (PowerShell):

```powershell
$key = [Convert]::ToHexString([Security.Cryptography.RandomNumberGenerator]::GetBytes(32))
dotnet user-secrets set "ApiKeys:Week46Console" $key --project src/AssetTracker.Api
```

To use another database server, set `APP_DB_CONNECTION` in user secrets or as
an environment variable.
<!-- TODO: check the commands against the finished code (build step 21) -->

### Run the tests

```bash
dotnet test
```

---

## Usage

Every route is under `/api` and needs the `X-Api-Key` header. Requests and
responses are JSON (camelCase, dates as `YYYY-MM-DD`, enums as text).

| Method | Route | What it does |
|--------|-------|--------------|
| `GET` | `/api/offices` | The four offices with their currencies |
| `GET` | `/api/assets` | One page of assets. Query: `brand`, `model`, `officeId`, `purchaseYear`, `category`, `lifecycle`, `sortBy`, `page`, `pageSize` (max 100) |
| `GET` | `/api/assets/{id}` | One asset |
| `POST` | `/api/assets` | Create an asset; its currency comes from the office |
| `PUT` | `/api/assets/{id}` | Change an asset's details (not its type or currency) |
| `DELETE` | `/api/assets/{id}` | Remove an asset |
| `GET` | `/api/reports/office-totals` | Asset count and total price per office and currency |
| `GET` | `/api/reports/most-expensive?count=10` | The most expensive assets within each currency |

Example:

```http
GET https://localhost:7194/api/assets?officeId=2&lifecycle=ExpiringSoon&sortBy=EndOfLife
X-Api-Key: <your key>
```

Errors come back as ProblemDetails: `400` bad input (with the field name),
`401` missing or wrong key, `404` unknown asset, `409` duplicate serial number,
`429` too many requests, `503` database unavailable.

All requests, including the error cases, are in
[`src/AssetTracker.Api/AssetTracker.Api.http`](src/AssetTracker.Api/AssetTracker.Api.http)
for Visual Studio or VS Code.

![A request in the .http file and the API's response with one page of assets](readme-assets/screenshots/http-request-assets.png)
<!-- TODO: take screenshot -->

---

## Project structure

```
asset-tracker-api/
├── src/
│   ├── AssetTracker.Core/            # domain rules, services, interfaces (no dependencies)
│   ├── AssetTracker.Infrastructure/  # EF Core: database context, migrations, repositories
│   └── AssetTracker.Api/             # controllers, request/response types, auth, Program.cs
├── tests/
│   ├── AssetTracker.Core.Tests/      # unit tests for the rules and services
│   └── AssetTracker.Api.Tests/       # HTTP tests with WebApplicationFactory
├── docs/PROJECT_CONTEXT.md           # design document and build plan
├── .github/workflows/                # CI and deploy to Azure
├── TESTING.md                        # how it is tested, and the results
└── CLAUDE.md                         # rules for Claude Code in this repo
```

---

## Design decisions

| Decision | Reasoning |
|----------|-----------|
| The API stores facts; the client converts currencies and builds reports | Exchange rates are an outside service with their own fallbacks, already handled by the Week 46 app. Keeping them out keeps the API small, testable and stable |
| Prices stored in the currency that was paid | What an office paid never changes. A converted price would change every time the exchange rate moved |
| Totals per office **and** currency | Prices in different currencies can't be added or compared directly (40 000 TRY is less than 1 500 EUR) |
| API key instead of user accounts | The only client is a program. User login comes in a later project |
| Managed identity for the database | The app proves who it is to Azure SQL; there is no database password to store or leak |
| Azure Container Apps + Azure SQL free offer | Free for a small API, custom domain with a free certificate, same SQL Server as local development |
| Image on GitHub Container Registry | Free for public images; Azure's registry has no free tier |

📄 **[Full design document →](docs/PROJECT_CONTEXT.md)**

---

## Testing

Unit tests (xUnit) cover the domain rules and services; HTTP tests with
`WebApplicationFactory` cover every endpoint's status codes, validation, the
API key and rate limiting. Both run on every push in GitHub Actions. Database
queries and the deployed API are tested by hand.

More detail in [TESTING.md](TESTING.md).

---

## Planning and documentation

| Document | Contents |
|----------|----------|
| [`docs/PROJECT_CONTEXT.md`](docs/PROJECT_CONTEXT.md) | Goals, decisions, domain rules, data model, endpoints, security, build plan, deployment |
| [`TESTING.md`](TESTING.md) | How the project is tested, and the results |
| [`CLAUDE.md`](CLAUDE.md) | Rules for Claude Code in this repo: architecture boundaries, conventions, scope |

---

## Future features

Planned, designed for, but not yet built:

- [ ] **Week 46 client:** the console app rebuilt to call this API instead of
      the database, keeping its menus, exchange rates, reports and exports.
- [ ] **Employees and assignment (Level 5):** the optional "assigned to" text
      becomes a link to an employee in one migration.
- [ ] **Concurrency check:** refuse an update based on an outdated copy of an
      asset (409) instead of overwriting it.
- [ ] **Published OpenAPI document** linked from this README.

---

## Technologies used

- C# 14 on .NET 10
- ASP.NET Core 10 Web API (controllers, built-in rate limiting, authentication
  and ProblemDetails)
- Entity Framework Core 10 with SQL Server Express (local) and Azure SQL
  Database (production)
- xUnit and `Microsoft.AspNetCore.Mvc.Testing`
- Azure Container Apps, Microsoft Entra managed identity
- GitHub Actions and GitHub Container Registry
- Git and GitHub
- Visual Studio
- Claude (planning)
- Claude Code (implementation)

---

## Credits

- Domain model, rules, EF Core configuration and sample data reused from my
  [Week 46 asset tracker](https://github.com/NiclO1337/weekly-projects-lexicon/tree/main/Week46),
  which builds on Week 38 of the same repo.
- Project layout and `Directory.Build.props` from my C# Milestone project.
- [ASP.NET Core documentation](https://learn.microsoft.com/aspnet/core/),
  [Azure Container Apps documentation](https://learn.microsoft.com/azure/container-apps/)
  and the [OWASP API Security Top 10](https://owasp.org/API-Security/editions/2023/en/0x11-t10/).
