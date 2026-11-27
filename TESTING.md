# Testing – Dragon's Hoard API

[Back to README](README.md)

## Table of contents

* [Automated testing](#automated-testing)
    * [Setup](#setup)
    * [Unit tests](#unit-tests)
    * [Integration tests](#integration-tests)
    * [Test results](#test-results)
    * [Static checks](#static-checks)
* [Manual testing](#manual-testing)
    * [Offices](#offices)
    * [Asset list: search, filter, sort and paging](#asset-list-search-filter-sort-and-paging)
    * [Create, update and delete](#create-update-and-delete)
    * [Report figures](#report-figures)
    * [Input validation](#input-validation)
    * [Deployed API](#deployed-api)
* [Performance](#performance)
* [Security testing](#security-testing)
* [Bugs](#bugs)


## Automated testing

### Setup

Tests are written with [xUnit](https://xunit.net/) 2.9.3. Unit tests are in
`tests/AssetTracker.Core.Tests` and reference only `AssetTracker.Core`. HTTP
tests are in `tests/AssetTracker.Api.Tests` and start the whole API in memory
with `WebApplicationFactory` from `Microsoft.AspNetCore.Mvc.Testing`, with the
repositories replaced by in-memory fakes, so no database is needed. Both run on
every push in GitHub Actions.

```bash
dotnet test
```

| Helper file | What it is |
|-------------|------------|
| `tests/AssetTracker.Core.Tests/FakeTimeProvider.cs` | A `TimeProvider` with a fixed date, so "today" never changes during a test |
| `tests/AssetTracker.Core.Tests/Fakes/` | In-memory `IAssetRepository` and `IOfficeRepository` |
| `tests/AssetTracker.Api.Tests/ApiFactory.cs` | `WebApplicationFactory` with the fakes, a test API key and small rate limits |
| `tests/*/Builders/` | Builders for test assets and requests |

TODO: correct the file names when the test projects exist (build step 3).

### Unit tests

Counts are the number of tests in each file.

| File | What is tested | Tests | Result |
|------|----------------|:-----:|--------|
| `AssetLifecycleTests.cs` | Each status cutoff exactly on, one day before and one day after; leap day | – | Not tested yet |
| `AssetTests.cs` | Blank and too-long text, price limits and rounding, future purchase date, warranty before purchase, blank "assigned to" becomes none, a failed update changes nothing | – | Not tested yet |
| `AssetServiceTests.cs` | Currency comes from the office and survives a move, duplicate serial numbers on create and update, unknown asset or office, lifetime filters become the right date bounds | – | Not tested yet |
| `ReportServiceTests.cs` | Office totals and per-currency top list passed through, `count` limits | – | Not tested yet |

### Integration tests

| File | Flow | Tests | Result |
|------|------|:-----:|--------|
| `AuthenticationTests.cs` | Every endpoint answers 401 without a key and with a wrong key, and works with the right one | – | Not tested yet |
| `ErrorHandlingTests.cs` | 400 / 404 / 409 / 503 / 500 as ProblemDetails, no stack trace or exception message | – | Not tested yet |
| `OfficesEndpointTests.cs` | The four offices with currencies | – | Not tested yet |
| `AssetsEndpointTests.cs` | Query parameters and limits, create (201 + `Location`), update, delete (204), unknown fields refused | – | Not tested yet |
| `ReportsEndpointTests.cs` | Office totals per currency, most expensive per currency | – | Not tested yet |
| `RateLimitingTests.cs` | Too many requests give 429 with `Retry-After` | – | Not tested yet |
| `ProductionSettingsTests.cs` | In Production: no OpenAPI document, no developer exception page | – | Not tested yet |

### Test results

TODO: fill in after the tests have run (`dotnet test`, with the date).

![Test run result](./readme-assets/test-result.png)
<!-- TODO: take screenshot -->

### Static checks

| Check | Command | What it finds | Result |
|-------|---------|---------------|--------|
| Build | `dotnet build` | Compiler and analyzer warnings, which fail the build | Not tested yet |
| Formatting | `dotnet format --verify-no-changes` | Files not formatted to the project's style | Not tested yet |
| Vulnerable packages | `dotnet list package --vulnerable --include-transitive` | Packages with known security advisories | Not tested yet |


## Manual testing

Tested with the requests in `src/AssetTracker.Api/AssetTracker.Api.http`,
locally against SQL Server Express with the 25 sample assets, and again on the
deployed API ([Deployed API](#deployed-api)). These checks cover what the
automated tests can't: the real SQL queries.

### Offices

| Action | Expected result | Result |
|--------|-----------------|--------|
| `GET /api/offices` | Germany EUR, Sweden SEK, USA USD, Turkey TRY | Not tested yet |

### Asset list: search, filter, sort and paging

| Action | Expected result | Result |
|--------|-----------------|--------|
| `GET /api/assets` | Page 1 of 3, 10 assets, 25 in total, sorted by office then purchase date | Not tested yet |
| `?page=3` and `?page=99` | Page 3 has 5 assets; page 99 returns page 3 | Not tested yet |
| `?brand=dell` / `?brand=DELL` | Same Dell assets for both (case-insensitive) | Not tested yet |
| `?model=pro` | Every asset whose model contains "pro" | Not tested yet |
| `?officeId=2` | Only Sweden's assets | Not tested yet |
| `?purchaseYear=<a year in the sample data>` | Only assets bought that year | Not tested yet |
| `?category=Mobile` / `?category=Computer` | Only phones and tablets / only laptops and desktops | Not tested yet |
| `?lifecycle=ExpiringSoon` | Only assets with less than 6 months left that aren't past end of life | Not tested yet |
| `?lifecycle=Expired` | Only assets past their 3-year lifetime | Not tested yet |
| Criteria combined, e.g. `?officeId=2&category=Computer` | Only assets matching both | Not tested yet |
| `?sortBy=Category` / `?sortBy=EndOfLife` | Computers before mobile, grouped by type / oldest purchase first | Not tested yet |
| A search with no matches | 200 with an empty list and `totalCount` 0 | Not tested yet |

### Create, update and delete

| Action | Expected result | Result |
|--------|-----------------|--------|
| `POST` a laptop for Sweden, price 14 990 | 201, `Location` header, currency `SEK` | Not tested yet |
| `GET` the `Location` URL | The same asset | Not tested yet |
| `PUT` it with `officeId` Germany | 200, office Germany, price still 14 990 `SEK` | Not tested yet |
| Restart the API, `GET` it again | Still there with the changes | Not tested yet |
| `DELETE` it, then `GET` it | 204, then 404 | Not tested yet |

### Report figures

| Action | Expected result | Result |
|--------|-----------------|--------|
| `GET /api/reports/office-totals` | One row per office and currency; counts add up to 25; one office's total checked by hand against the asset list | Not tested yet |
| `GET /api/reports/most-expensive?count=3` | Up to 3 assets per currency, highest price first within each | Not tested yet |

### Input validation

Every field of `POST /api/assets` and `PUT /api/assets/{id}` is tried with bad
input.

| Field | Input | Expected result | Result |
|-------|-------|-----------------|--------|
| `brand`, `model`, `serialNumber` | Empty, or only spaces | 400 naming the field, nothing saved | Not tested yet |
| `brand`, `model`, `serialNumber`, `assignedTo` | 51 characters | 400 with the limit | Not tested yet |
| `purchasePrice` | `0`, `-5`, `150000000.01`, `"abc"` | 400 | Not tested yet |
| `purchaseDate` | Tomorrow, `2026-13-01`, `27/11/2026` | 400 | Not tested yet |
| `warrantyExpirationDate` | Before the purchase date | 400 | Not tested yet |
| `officeId` | `99` | 400 naming `officeId` | Not tested yet |
| `type` | `Printer` | 400 | Not tested yet |
| `serialNumber` | An existing serial number, and the same one in lower case | 409 for both (compared without letter case) | Not tested yet |
| Extra field | `"currency": "USD"` or `"id": 1` in the body | 400, nothing saved | Not tested yet |
| Query | `pageSize=101`, `page=0`, `count=51` | 400 | Not tested yet |
| Valid input | A complete phone for Turkey | 201, currency `TRY` | Not tested yet |

### Deployed API

Run after each deploy, against `https://asset-tracker-api.niclashugdahl.dev`.

| Action | Expected result | Result |
|--------|-----------------|--------|
| `GET /api/assets` without a key | 401 | Not tested yet |
| `GET /api/assets` with the key | 200 with the sample assets | Not tested yet |
| Create, update and delete one asset | Same results as locally | Not tested yet |
| Open the URL with `http://` | Redirected to HTTPS | Not tested yet |
| Check the certificate in the browser | Valid, issued for the custom domain | Not tested yet |


## Performance

| Check | Result |
|-------|--------|
| First request after the app and database have paused (cold start), time in seconds | Not tested yet |
| A warm `GET /api/assets?pageSize=100` stays under 1 second | Not tested yet |

What changed because of the testing:
- TODO: fill in after testing.


## Security testing

Each risk listed in `docs/PROJECT_CONTEXT.md` §8 gets a row here.

| # | Check | How it was tested | Expected result | Result |
|---|-------|-------------------|-----------------|--------|
| X1 | No per-object access rules (accepted risk) | — | Documented as accepted | Not tested yet |
| X2 | Every endpoint needs a valid key | Automated test over all endpoints; manual request without and with a wrong key on the live API | 401, same response for both | Not tested yet |
| X3 | Clients can't set fields they don't own | `POST`/`PUT` with `currency`, `id` or `category` in the body | 400, nothing saved | Not tested yet |
| X4 | Requests can't be made too large or too many | `pageSize=10000`; a body over 64 KB; requests in a loop | 400; 413; 429 with `Retry-After` | Not tested yet |
| X5 | Production settings | Open `/openapi/v1.json` and cause a 500 on the live API; look at the response headers | 404 for the document, no stack trace, HSTS header, no `Server` header | Not tested yet |
| X6 | Search text is never run as SQL | `?brand=' OR 1=1 --` and `?model=%` | Treated as plain text: no extra rows, no database error | Not tested yet |
| X7 | No secrets in the repo or the image | Search the repo history for keys and connection strings; inspect the published image's `appsettings*.json` | Nothing found | Not tested yet |
| X8 | Errors don't leak internals | Unknown id, bad JSON, database stopped locally | ProblemDetails only, no stack trace, query or file path | Not tested yet |
| X9 | The database refuses SQL logins | Try to connect to the Azure SQL server with a SQL login | Refused (Entra-only authentication) | Not tested yet |
| X10 | No known vulnerable dependencies | `dotnet list package --vulnerable --include-transitive` on <date>; NuGet audit in CI | No known vulnerabilities | Not tested yet |
| X11 | A faked `X-Forwarded-For` doesn't bypass the IP limit | Requests without a key that send a different `X-Forwarded-For` each time | Still limited after 10 per minute | Not tested yet |
| X12 | The pipeline has only the rights it needs | Read the workflow `permissions` and the Azure role assignment | Only what §8 lists | Not tested yet |
| X13 | No real personal data is published | Read the sample data, tests, `.http` file, README and screenshots | Only invented names | Not tested yet |
| X14 | No surprise bill | Azure cost analysis after the first month; database auto-pause setting | 0 cost, auto-pause until next month on | Not tested yet |


## Bugs

### Fixed bugs

- None yet.

### Unfixed bugs

- None known
