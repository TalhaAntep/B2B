# Deployment status and plan

**Last updated:** 2026-11-24  
**Repository:** https://github.com/TalhaAntep/B2B  
**Static synthetic showcase:** https://talhaantep.github.io/B2B/  
**MonsterASP full-stack app:** http://b2b-store-settings.runasp.net/

## Current deployment summary

The project now has two separately deployed surfaces:

1. **GitHub Pages** is a static, synthetic, read-only showcase. It contains the frontend build only; it does not connect to an API or database.
2. **MonsterASP** hosts the Vue frontend and ASP.NET Core .NET 10 API in the same website and connects to the hosted synthetic SQL database `db82141` for `MemberCode=store5`.

The project owner reported these MonsterASP checks on 2026-11-23:

- ASP.NET Core process started in Production and listened on the IIS-provided local port.
- `GET /health/database` returned `{"status":"ready"}` over HTTP.
- The admin-token UI connection succeeded and settings loaded.
- One settings Save operation returned the application's success/readback confirmation.
- A later performance build was published; the owner reported settings refresh/save interactions were responsive afterwards.

These are owner-observed smoke-test results, not a full 98-control acceptance test. They do not establish legacy portal parity.

## Deployment map

| Component | State | Notes |
|---|---|---|
| Vue 3 + TypeScript UI | Deployed to MonsterASP and GitHub Pages | Same source project; Pages uses a special synthetic build. |
| ASP.NET Core .NET 10 API | Deployed to MonsterASP | Same-origin API and UI. |
| SQL Server | Hosted on MonsterASP | Synthetic database `db82141`, member `store5`. |
| SQL readiness | Owner-verified | `/health/database` returned `ready`. |
| Save/readback | Owner-verified smoke test | One setting Save reported successful commit/readback; not all settings tested on the hosted database. |
| Performance update | Owner-reported as faster | Removes repeated SQL reads after save and combines row uniqueness/read queries. Shared-host limits can still affect latency. |
| GitHub Pages | Deployed | Synthetic, static, read-only. |
| HTTPS | Not confirmed | MonsterASP URL was tested over HTTP; do not use real customer data or credentials. |
| Anonymous public-write option | Implemented in source; deployment activation should be confirmed | Opt-in environment variable `B2BSTORE_PUBLIC_WRITE_ENABLED=true`. When active, any visitor can modify shared settings. |

## MonsterASP configuration

Required variables for the standard hosted mode:

- `ASPNETCORE_ENVIRONMENT=Production`
- `B2BSTORE_SETTINGS_WRITE_MODE=HostedStore`
- `B2BSTORE_MEMBER_CODE=store5`
- `B2BSTORE_SQL_CONNECTION` — private connection string pointing to `db82141`
- `B2BSTORE_ADMIN_TOKEN` — high-entropy secret for bearer-token access

`B2BSTORE_MODE` defaults to `SqlServer` in Production. It can be explicitly set to `SqlServer` for clarity.

The following variables must never be committed to source control. Do not put their values in README files, screenshots, demo recordings, issue comments, or public deployment artifacts.

### Owner-requested anonymous public-write option

The source contains an explicit opt-in public mode. Add this variable in MonsterASP only when the project owner deliberately wants all internet visitors to share the same writable settings:

- `B2BSTORE_PUBLIC_WRITE_ENABLED=true`

This requires Production + SqlServer + `B2BSTORE_SETTINGS_WRITE_MODE=HostedStore`. In this mode:

- Visitors do not enter an admin token; the frontend detects `HostedPublicWrite` and connects automatically.
- Every visitor can read and modify the shared `db82141` / `store5` settings.
- Application-owned extension settings and asset endpoints are also exposed.
- The backend still restricts persistence to the fixed database/member guard, typed validation, and fixed setting allowlists; it does not accept arbitrary SQL or caller-supplied table/column names.

**Security and assignment deviation:** this deliberately bypasses the brief's requirement that write endpoints require authorization. Anyone can change site status, payments, products, menus, and other shared settings. Use only for a temporary evaluation with synthetic data. It is not suitable for real customer data or production operations. To restore token authentication, remove the variable or set it to `false` and restart the website; keep `B2BSTORE_ADMIN_TOKEN` configured.

The repository source includes this mode, but its current activation in the hosting panel should be confirmed from the environment-variable list and `/api/settings/capabilities` (`environment: HostedPublicWrite`) before claiming anonymous write access is live.

## Build and publish

From the project root on Windows with Node/npm and the .NET 10 SDK installed:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File ".\scripts\publish-monsterasp.ps1"
```

The script runs `npm ci` and `npm run build`, copies `frontend/dist` to the ASP.NET Core `wwwroot`, runs `dotnet publish -c Release`, and creates `artifacts/B2BStore-MonsterASP-Publish.zip`. Run `npm test` separately as part of pre-release validation.

Upload the publish ZIP into the MonsterASP website `wwwroot` and extract/overwrite the prior build. Preserve the existing hosting environment variables and the `web.config` hosting model setting selected in the working deployment. Restart the website after the upload.

Smoke tests:

- `http://b2b-store-settings.runasp.net/health` — application health.
- `http://b2b-store-settings.runasp.net/health/database` — verifies SQL connectivity and exactly one configured `MemberCode` row; returns only a generic readiness status.
- Open the root URL and verify the UI, connection state, load, and Save/readback behaviour according to the active mode.

## Static GitHub Pages showcase

- Repository: `https://github.com/TalhaAntep/B2B`
- Deployment branch: `public-demo`
- Workflow: `.github/workflows/deploy-public-demo.yml`
- Build flag: `VITE_PUBLIC_DEMO=true`
- Published artifact: `frontend/dist` only
- URL: `https://talhaantep.github.io/B2B/`

The Pages mode builds 98 browser-generated synthetic values, does not call `/api` or SQL Server, and has no Save or server-side asset-management capability. Keep this workflow separate from the MonsterASP full-stack deployment.

## Database safety and evidence

The MonsterASP test database is synthetic and isolated as `db82141` / `store5`. The required `DealerSystemSettingExtension` table was checked through MonsterASP WebMSSQL and returned `EXISTS`; no duplicate DDL was applied. A one-row settings readiness check returned `ready`. One live Save operation was confirmed by the project owner. The complete 98-setting hosted save matrix has not been run.

The original `Store5_Analysis` and local `Store5_Settings_Test` databases are distinct from the MonsterASP database. Do not confuse the local integration evidence with hosted SQL verification.

Never publish or upload SQL backups, real dealer records, local settings files, `.env`, connection strings, bearer tokens, passwords, private keys, or raw legacy evidence that has not been reviewed for sensitive content. If a credential is ever committed, revoke/rotate it and review Git history; deleting the file alone is not enough.

## Known limitations

1. The original Web Forms C# save handler was unavailable; legacy save behaviour and old-portal parity are unverified for all 98 controls.
2. Five setting fields use application-owned extension keys because legacy destinations/readers were not established. Five image slots use app-owned asset storage. These are not claimed to be consumed by the old portal.
3. Portal effects were not exhaustively tested in controlled before/after experiments.
4. The hosted public-write mode bypasses authentication by explicit owner request and must remain temporary/synthetic-only.
5. HTTPS activation was not confirmed for the MonsterASP FreeSite deployment.
