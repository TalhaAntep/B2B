# Deployment status and plan

**Last updated:** 2026-11-23  
**Repository:** https://github.com/TalhaAntep/B2B  
**Public demo:** https://talhaantep.github.io/B2B/

## Executive status

The **synthetic frontend demo is deployed and reachable** on GitHub Pages. The **complete Vue + ASP.NET Core API + SQL Server application is not deployed as a full-stack hosted service**. The deployment requirement is therefore partially met: an accessible, free, synthetic, read-only frontend showcase exists; the backend and SQL Server remain local-only.

Do not describe the public page as a live dealer administration service. It is not connected to an API or database and cannot persist settings.

## What is deployed

| Component | Current state | Notes |
|---|---|---|
| Vue 3 + TypeScript frontend | **Publicly deployed** | GitHub Pages project site at `https://talhaantep.github.io/B2B/`. |
| Settings catalogue | **Included in static build** | 98 UI controls are represented. |
| Demo data | **Synthetic, browser-generated** | Values are unset/NULL; no real dealer data is loaded. |
| Settings editing | **Draft only** | Any interaction remains in browser state; no server-side Save endpoint is available. |
| Image controls | **Local preview only** | File selection can preview in the browser; no upload or storage request is made. |
| ASP.NET Core API | **Not publicly deployed** | Runs locally for the SQL-backed application. |
| SQL Server | **Not publicly deployed** | `Store5_Settings_Test` is a local isolated database; it is not the public demo data source. |
| Production identity / user authorization | **Not implemented for public hosting** | The current local bearer-token model is not suitable for exposing the full settings API publicly. |

## Public demo architecture

- Git repository: `https://github.com/TalhaAntep/B2B` (Public).
- Deployment branch: `public-demo`.
- Workflow: `.github/workflows/deploy-public-demo.yml`.
- Pages source: GitHub Actions.
- Build flag: `VITE_PUBLIC_DEMO=true`.
- Published artifact: `frontend/dist` only.
- Public URL: `https://talhaantep.github.io/B2B/`.

The static build creates all 98 catalogue items with synthetic unset values. The public-demo code path does not call `/api`, does not contact SQL Server, does not contain connection strings or bearer tokens, and disables server-side save and asset-management capabilities. The website was opened successfully by the project owner on 2026-11-23 after the GitHub Pages workflow was corrected and rerun.

## Verification and deployment history

1. `npm test` on the public-demo branch: **36/36 tests passed**.
2. `VITE_PUBLIC_DEMO=true; npm run build`: passed.
3. The initial GitHub Actions run failed with Pages API `404 Not Found` because Pages was not yet initialised/configured for Actions.
4. GitHub Pages was configured and the workflow rerun.
5. The project owner opened `https://talhaantep.github.io/B2B/` successfully on 2026-11-23.

For the exact build and test history, see `docs/final-release-report.md` and `docs/test-bug-report.md`.

## Why the full-stack service is still local

The current ASP.NET Core Production configuration requires a server-side admin token, a SQL connection, and a fixed member context. The local token scheme is not a reviewed multi-user identity and authorization design for a public multi-tenant management API. Deploying the current API with a shared token would expose sensitive settings and would not satisfy the required user/tenant isolation.

The full-stack phase needs, at minimum:

1. A hosted SQL Server-compatible database containing synthetic data only, with schema compatibility tested against the application and migrations reviewed.
2. A hosted ASP.NET Core API, with public user authentication, per-user/per-tenant authorization, strict tenant scoping, rate limits, input validation, and audit logging.
3. Secret management outside source control, exact CORS origins, HTTPS, redacted logs, and controlled error responses.
4. A decision on asset persistence (or permanent disabling of public uploads).
5. A provider and region assessment covering current free-tier limits, sleep/cold-start behaviour, disk persistence, storage, request/runtime quotas, SQL compatibility, and possible overage charges.
6. Security review and explicit approval before connecting any hosted API/database.

No Azure subscription was available in the checked university directory. Azure is **not required** for the GitHub Pages demo. No paid resources or hosted database have been provisioned.

## Public data and secret boundaries

Never publish or upload `source/store5.bak`, `Store5_Settings_Test`, `Store5_Analysis`, real dealer records, local settings files, `.env` files, connection strings, bearer tokens, passwords, private keys, or raw legacy evidence that has not been reviewed for sensitive information. The public demo should remain synthetic and read-only.

If any credential is ever committed to a public repository, deleting the file or switching the repository to private is insufficient; revoke/rotate the exposed credential and review Git history.

## Release decision

- **Static public showcase:** deployed and verified.
- **Full-stack public deployment:** not complete; do not claim otherwise.
- **Local SQL-backed implementation:** available with the documented local setup and isolated test database; Production mode remains read-only.
