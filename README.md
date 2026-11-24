# B2B STORE — Technical Settings

A modern replacement UI for the B2B STORE `DealerSystemSetting` module. The project includes a Vue 3 + TypeScript frontend, an ASP.NET Core .NET 10 Web API, SQL Server persistence adapters, a 98-control catalogue, technical analysis, tests, and deployment documentation.

## Current deployments

- **MonsterASP full-stack application:** http://b2b-store-settings.runasp.net/  
  Owner-observed deployment on 2026-11-23: the ASP.NET Core API started in Production, `GET /health/database` returned `{"status":"ready"}`, the admin-token connection loaded settings, one Save operation reported success/readback, and the later performance build was reported as responsive. The endpoint is currently HTTP; HTTPS was not confirmed. This deployment is connected to the synthetic MonsterASP database `db82141` for `MemberCode=store5`.
- **GitHub Pages showcase:** https://talhaantep.github.io/B2B/  
  Static, synthetic, read-only UI. It does not call the API or SQL Server and is safe to share as a visual showcase.

The MonsterASP app and GitHub Pages showcase are different deployment targets. Do not describe GitHub Pages as the SQL-backed application.

## Stack

- Frontend: Vue 3, TypeScript, Vite
- Backend: C# / ASP.NET Core Web API, .NET 10
- Database: Microsoft SQL Server
- Tests: Vitest and .NET test project

## Main capabilities

- 98 setting/control IDs are represented in the UI catalogue.
- Category navigation, global search, EN/TR labels, context/help text, and typed controls.
- Bitmask editor preserves unrelated and unknown bits according to the documented masks.
- API reads settings from the server-configured `MemberCode` scope.
- The hosted writable mode uses fixed database/member guards, typed value validation, fixed allowlists, transactional updates, concurrency checks, and SQL readback verification.
- Five unresolved legacy settings use application-owned extension storage; five image slots use application-owned asset storage. These are not claimed to be consumed by the legacy portal.

## Runtime modes

| Mode | Configuration | Behaviour |
|---|---|---|
| Public GitHub Pages demo | `VITE_PUBLIC_DEMO=true` | Synthetic browser-generated values; no API/SQL; read-only. |
| Local normal mode | Development / standard configuration | API reads settings; writes disabled. |
| Local isolated test mode | Development + `B2BSTORE_SETTINGS_WRITE_MODE=IsolatedLocalDemo` + SQL Server | Explicit, guarded local test writes only to `Store5_Settings_Test` / `store5`. |
| Hosted standard mode | Production + `B2BSTORE_SETTINGS_WRITE_MODE=HostedStore` | Hosted SQL access with `B2BSTORE_ADMIN_TOKEN`; authenticated reads/writes. |
| Hosted public-write mode | Hosted standard mode + `B2BSTORE_PUBLIC_WRITE_ENABLED=true` | No token prompt; any internet visitor can edit shared settings. Temporary evaluation-only mode; not production-safe. |

## Local setup

Requirements: Node.js/npm and .NET 10 SDK. SQL mode additionally requires an approved local SQL Server test database.

Frontend:

```powershell
Set-Location .\frontend
npm ci
npm test
npm run build
```

Backend:

```powershell
dotnet restore .\B2BStore.Settings.slnx
dotnet build .\B2BStore.Settings.slnx --configuration Release
dotnet test .\B2BStore.Settings.slnx --configuration Release
```

See `README_START_HERE.md` and `docs/deployment.md` for the complete local configuration, safe database setup, and test instructions.

## MonsterASP deployment

The Windows publishing script builds Vue, copies the frontend into the ASP.NET Core `wwwroot`, runs `dotnet publish`, and creates `artifacts/B2BStore-MonsterASP-Publish.zip`:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File ".\scripts\publish-monsterasp.ps1"
```

Required server-side variables (never commit their values):

- `ASPNETCORE_ENVIRONMENT=Production`
- `B2BSTORE_SETTINGS_WRITE_MODE=HostedStore`
- `B2BSTORE_MEMBER_CODE=store5`
- `B2BSTORE_SQL_CONNECTION` — private connection string for the hosted `db82141` SQL database
- `B2BSTORE_ADMIN_TOKEN` — required for the standard authenticated hosted mode

For the owner-requested, anonymous public-write mode only, additionally set `B2BSTORE_PUBLIC_WRITE_ENABLED=true`. This exposes the shared writable settings and application-owned asset routes to every visitor. See `PUBLIC-WRITE-ENABLEMENT-TR.md` and `docs/deployment.md` before enabling it.

## Important known limitations

1. The original ASP.NET Web Forms server-side save handler was not available. Exact legacy save-handler behaviour and old-portal parity are therefore unverified for all 98 controls.
2. 93 fixed setting adapters are implemented; five unresolved legacy destinations use application-owned extension keys. Five image slots use application-owned asset storage. These do not prove compatibility with the old portal.
3. Portal effects have not been exhaustively tested in a controlled before/after experiment. Unavailable tests are marked as unverified or not tested.
4. The MonsterASP full-stack deployment was smoke-tested by the project owner, not subjected to a full 98-setting production acceptance suite. The database is synthetic and scoped to `db82141` / `store5`.
5. The hosted endpoint was tested over HTTP. HTTPS activation was not confirmed. Do not use real customer data or credentials on this deployment until HTTPS and an appropriate authentication model are in place.
6. Anonymous public-write mode knowingly bypasses the assignment's authentication requirement. Use it only for a temporary synthetic evaluation; any visitor may change shared settings.

## Assignment deliverables

- **A — Technical analysis:** `docs/technical-analysis-report.md`, `docs/technical-analysis.md`, parameter catalogue, mapping audit, database inventory, Bitwise and legacy persistence notes.
- **B — Test and defect report:** `docs/test-and-defect-report.md`, `docs/test-bug-report.md`, `docs/sql-validation-report.md`, and `docs/evidence/delivery/`.
- **C — Working software:** this Vue/TypeScript frontend, .NET API, SQL persistence layer, tests, setup instructions, and demo recording.
- **D — AI usage summary:** `docs/ai-usage-report.md`.

## Repository safety

Never commit `.bak` files, SQL backups, real dealer/customer data, `settings.local.json`, `.env` files, connection strings, passwords, bearer tokens, or hosting secret values. Public demo screenshots and video use synthetic values only.
