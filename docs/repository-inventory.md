# Repository inventory

**Updated:** 2026-11-23  
**Repository:** https://github.com/TalhaAntep/B2B

## Project structure

| Path | Purpose |
|---|---|
| `frontend/` | Vue 3 + TypeScript + Vite user interface and Vitest tests. |
| `backend/B2BStore.Settings.Api/` | ASP.NET Core .NET 10 minimal Web API, SQL Server access, local auth/safety guards, synthetic Mock store, setting codecs and application-owned asset handling. |
| `tests/B2BStore.Settings.Tests/` | .NET unit and integration test project. |
| `docs/` | Technical analysis, data inventory, mapping audit, test/defect reports, SQL validation, architecture, deployment and evidence. |
| `scripts/` | Catalogue generation and focused isolated API verification scripts. |
| `.github/workflows/` | GitHub Pages static synthetic-demo deployment workflow. |
| `B2BStore.Settings.slnx` | .NET solution for API and test project. |

## Verified technology and tool versions

- Vue 3.5.45
- TypeScript 5.7.3
- Vite 6.4.6
- ASP.NET Core / .NET target `net10.0`
- Microsoft.Data.SqlClient 7.1.2
- .NET SDK 10.0.404 (local validation)
- Node.js 22.11.0 and npm 10.9.0 (local validation)
- Vitest 3.2.7 (from the reported public-demo test run)

## Main application endpoints and modes

- Frontend dev server: `http://127.0.0.1:5173/`
- Local API: `http://127.0.0.1:5080/`
- Public static demo: `https://talhaantep.github.io/B2B/`
- Public demo build flag: `VITE_PUBLIC_DEMO=true`
- Local database target for approved isolated testing: `(localdb)\MSSQLLocalDB`, database `Store5_Settings_Test`, exact `MemberCode=store5`.

## Test projects and commands

Frontend commands are run from `frontend/`:

```powershell
npm test
npm run build
$env:VITE_PUBLIC_DEMO = "true"
npm run build
Remove-Item Env:VITE_PUBLIC_DEMO
```

Backend Release build from the repository root:

```powershell
dotnet build .\B2BStore.Settings.slnx -c Release --no-restore
```

SQL write/integration commands are deliberately not part of routine release validation. They must only be run using the documented isolated test procedure and exact target.

## Git and deployment

- Public repository: `TalhaAntep/B2B`.
- Main source baseline commit: `263bb0b`.
- Public synthetic demo branch: `public-demo`.
- GitHub Pages deploys the frontend-only `frontend/dist` artifact via GitHub Actions.

## Known source limitations

The original ASP.NET Web Forms server-side save handler/code-behind was not present in the inspected repository. Legacy save-path behaviour and exact old-portal parity therefore remain unverified for all 98 controls. The candidate inventory and mapping audit preserve evidence and unknowns rather than asserting unsupported mappings. The SQL backup `source/store5.bak` is excluded from Git; it must never be added to the public repository.
