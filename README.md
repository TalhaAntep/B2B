# B2B STORE Technical Settings

A local-first rebuild of the DealerSystemSetting / Advanced Settings screen. It includes a Vue 3 + TypeScript UI, ASP.NET Core Web API, a generated candidate catalog, SQL Server access guarded by an exact `MemberCode` context, and a synthetic in-memory mode.

Final delivery checklist: [`docs/delivery-checklist.md`](docs/delivery-checklist.md). AI use and evidence limits: [`docs/ai-usage-report.md`](docs/ai-usage-report.md). Current free-hosting feasibility and deployment blockers: [`docs/deployment.md`](docs/deployment.md).

## Evidence boundary

The original ASP.NET save handler is unavailable, so legacy behavior and old-portal parity remain unknown unless directly demonstrated. The UI catalog contains **98 controls**. Keep two counts distinct: the supplied source-mapping inventory classifies 93 legacy mapping candidates and five unresolved legacy mappings; separately, the replacement app has a **93-setting fixed API allowlist** available only in authenticated Development isolated-test mode. Those 93 app adapters are not proof of 93 legacy portal mappings. Five of the app adapters (`ParameterProductDetailNotLogin2`, `ParameterMenu2`, `WhatsAppOrderNotification`, `FavoriteIconPosition`, and `FavoriteIconVerticalPosition`) store values in application-owned extension keys with no demonstrated legacy reader. `ParameterProductSearch` is hybrid: its low word uses the candidate `smallint` column and its high word uses an application-owned extension key. `ParameterPayment` preserves the unsigned 16-bit bit pattern in SQL `smallint`. The five file controls use a separate application-owned asset service and do not update the legacy portal. Original browser DOM evidence resolves the option IDs: `ddlParameterModules` has option `1` and maps to `DealerSettings.ParameterModules` (`int`); the separate `ddlParameterAdmin` has option `32` and maps to `DealerSettings.ParameterAdmin` (`bigint`). Normal and Production APIs remain read-only. Legacy save-handler parity remains unverified for all 98 controls. See the [reverse-engineering report](docs/legacy-persistence-reverse-engineering.md), [editability matrix](docs/setting-editability-matrix.md), and [mapping audit](docs/mapping-audit.json).

The original `Store5_Analysis` database remains unchanged. A separate `Store5_Settings_Test` copy exists in local SQL Server LocalDB. Confirmed endpoint: `(localdb)\MSSQLLocalDB`, database `Store5_Settings_Test`, Windows Integrated Security, and configured target `MemberCode=store5`. Do not point this app at production.

## Latest delivery status — 2026-11-23

The latest frontend run passed **35/35 tests**, and the production build passed. The .NET Release solution build passed with zero warnings and zero errors. The previously completed compatibility pass recorded **56/56 non-database backend tests** and eight isolated SQL API groups with backup verification, readback, and exact restoration; those backend/SQL groups were not rerun during the final delivery-capture pass. The final local SQL-backed check was read-only. Seven synthetic Mock-mode screenshots and a 10.36-second H.264 MP4 (plus the original WebM) are in [`docs/evidence/delivery/`](docs/evidence/delivery/). No public deployment exists, and no usable Git baseline commit is present in the uploaded snapshot; see [`docs/final-release-report.md`](docs/final-release-report.md).

## Requirements

- Windows PowerShell 7 or Windows PowerShell 5.1
- .NET 10 LTS SDK
- Node.js 22.11 or later and npm
- SQL Server only when using SQL mode

The backend and tests target `net10.0`. The implementation was built with .NET SDK 10.0.404 (runtime 10.0.15), Node 22.11.0 and npm 10.9.0. `npm install` reported engine warnings from transitive `nopt`/`abbrev` packages which request Node 22.22.2 or later; frontend build and tests pass with the installed Node version.

The .NET API and backend test project are grouped in [`B2BStore.Settings.slnx`](B2BStore.Settings.slnx).

## Run the local SQL read-only application

The local admin token is stored outside the repository in `%LOCALAPPDATA%\B2BStore\settings.local.json`. The API reads this private file; the Vite development server reads it only in its server process and adds the bearer header to its localhost API proxy. The token is not sent to the browser, included in frontend bundles, or printed by the application. The settings page therefore needs no token pasted into the browser: select **Connect** to read the configured SQL row.

The interface defaults to English. Use the **EN / TR** selector in the header to switch languages; the choice is saved in browser local storage and survives refresh. Search matches both original English and Turkish display labels regardless of the selected language. Display translations do not change setting IDs, option values, SQL values, or bitmask operations.

Create the local configuration once in PowerShell from the repository root. The command generates 256 random bits and does not print the token. It refuses to overwrite an existing local configuration:

```powershell
$localDirectory = Join-Path $env:LOCALAPPDATA 'B2BStore'
$localFile = Join-Path $localDirectory 'settings.local.json'
if (Test-Path $localFile) { throw 'Local settings already exist; inspect them without printing secrets.' }
if ([string]::IsNullOrWhiteSpace($env:B2BSTORE_SQL_CONNECTION)) { throw 'Load the approved Store5_Settings_Test Windows-authenticated connection from a private local source into B2BSTORE_SQL_CONNECTION.' }
New-Item -ItemType Directory -Path $localDirectory -Force | Out-Null
$rng = [System.Security.Cryptography.RandomNumberGenerator]::Create()
$tokenBytes = New-Object byte[] 32
$rng.GetBytes($tokenBytes)
$rng.Dispose()
$token = [Convert]::ToBase64String($tokenBytes)
$localSettings = [ordered]@{ B2BStore = [ordered]@{
  Mode = 'SqlServer'
  AdminToken = $token
  SqlConnection = $env:B2BSTORE_SQL_CONNECTION
  MemberCode = 'store5'
} }
$json = $localSettings | ConvertTo-Json -Depth 4
[System.IO.File]::WriteAllText($localFile, $json, [System.Text.UTF8Encoding]::new($false))
Remove-Item Env:B2BSTORE_SQL_CONNECTION -ErrorAction SilentlyContinue
Remove-Variable token, tokenBytes, localSettings, json
```

Open two PowerShell windows in the repository. Start the API in the first:

```powershell
$env:ASPNETCORE_ENVIRONMENT = 'Development'
dotnet run --project .\backend\B2BStore.Settings.Api\B2BStore.Settings.Api.csproj --urls http://127.0.0.1:5080
```

Start the frontend in the second:

```powershell
Set-Location .\frontend
npm run dev -- --host=127.0.0.1
```

Open `http://127.0.0.1:5173/` and select **Connect**. A successful read shows **Connected** and the number of loaded settings. The local API and Vite server must both be restarted after changing the local settings file. Keep the Vite server bound to `127.0.0.1`; do not expose it to the network.

The normal `GET /api/settings` and catalog API are read-only. SQL mode requires exactly one `dbo.DealerSettings` row for `MemberCode=store5`; missing or duplicate matches block access. `Store5_Analysis` is never a permitted write target. NULL MemberCode selection is not supported.

### Explicit local demonstration mode

Writes are disabled unless the API process is started in `Development` + `SqlServer` mode with `B2BSTORE_SETTINGS_WRITE_MODE=IsolatedLocalDemo`. This is a process-only opt-in and is not saved to the local settings file. Startup and every save validate the approved `(localdb)\\MSSQLLocalDB` endpoint, the exact `Store5_Settings_Test` database and `MemberCode=store5`; the API will refuse to start if the target differs. The authenticated `GET /api/settings/capabilities` endpoint tells the UI whether isolated saves and application-only asset management are available. The token remains in the local server-side settings file and Vite proxy; it is never sent in browser headers or bundled code.

For a local demonstration, stop the read-only API, start a PowerShell process from the repository root, and set the opt-in only for that API process:

```powershell
$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:B2BSTORE_MODE = 'SqlServer'
$env:B2BSTORE_MEMBER_CODE = 'store5'
$env:B2BSTORE_SETTINGS_WRITE_MODE = 'IsolatedLocalDemo'
dotnet run --configuration Release --project .\\backend\\B2BStore.Settings.Api\\B2BStore.Settings.Api.csproj --no-launch-profile --urls http://127.0.0.1:5080
```

Keep the frontend bound to `127.0.0.1:5173`. The UI displays the isolated target warning; 93 allowlisted settings can be saved in this explicit local mode. Extension-backed settings are stored only for this application and are not claimed to affect the legacy portal. Use `Ctrl+C` to stop the demo API, then restart without `B2BSTORE_SETTINGS_WRITE_MODE` to return to normal read-only mode. Never run this mode against Production, `Store5_Analysis`, or a public host.

## Isolated SQL test endpoints

The dedicated `POST /api/settings/test/rollback-only` route is registered only when all of these conditions hold: `ASPNETCORE_ENVIRONMENT=Development`, `B2BSTORE_MODE=SqlServer`, and the process environment variable `B2BSTORE_SQL_WRITE_TEST_MODE=true`. It also requires the authenticated local administrator bearer token. This flag is intentionally **not** stored in `settings.local.json`; the endpoint is absent by default and API startup rejects the flag outside Development/SqlServer mode.

`POST /api/settings/test/rollback-only` accepts only `SiteStatus` (Active `1` or Inactive `0`) and `ParameterOrder` operations from the catalog's power-of-two options. Every call uses a serializable SQL transaction, checks the exact `Store5_Settings_Test` database and `store5` row, uses parameterized updates and expected-value concurrency predicates, reads the values back, and unconditionally rolls back in `finally`. It verifies that the row matches its original values after rollback. This route never commits.

`PUT /api/settings` is registered only in Development + SQL mode when the isolated integration-test flag or `B2BSTORE_SETTINGS_WRITE_MODE=IsolatedLocalDemo` is explicitly set in the API process environment, and still requires the server-side bearer token. The application mode validates the exact `(localdb)\\MSSQLLocalDB`, `Store5_Settings_Test`, integrated-security connection and `MemberCode=store5` at startup and again on save. The browser cannot supply SQL identifiers. The fixed 93-control allowlist uses typed/parameterized persistence, expected-value concurrency checks in serializable transactions and readback checks. The `dbo.DealerSystemSettingExtension` table is application-owned and does not alter the legacy schema; extension values are not claimed to affect the old portal. Original browser DOM evidence assigns option `1` to `ddlParameterModules` / `DealerSettings.ParameterModules` (`int`) and option `32` to the independent `ddlParameterAdmin` / `DealerSettings.ParameterAdmin` (`bigint`). The original ASP.NET save handler and old-portal parity remain unavailable. UseFiltering and DiscountType use field-specific isolated adapter codecs; neither has confirmed legacy persistence semantics. The Schedule control uses a dedicated scalar 1/0 adapter, not bitwise arithmetic. Supported bitmasks preserve unrelated and unknown bits. Production and normal mode remain read-only, and `Store5_Analysis` is rejected. For committed integration tests, use a copy-only checksum backup outside the repository, restore the captured values and verify before continuing; stop if restoration is uncertain.

The extension table is created only by the explicit script [`001_DealerSystemSettingExtension.sql`](backend/B2BStore.Settings.Api/Migrations/001_DealerSystemSettingExtension.sql), which must be applied manually to the isolated test database. The API does not auto-run migrations. Do not apply it to Production. This table is application-owned storage; the legacy portal is not known to read its values.

The final targeted integration runners are [`verify-advanced-settings-api.ps1`](scripts/verify-advanced-settings-api.ps1) for Payment, ParameterModules and Product Search, and [`verify-extension-settings-api.ps1`](scripts/verify-extension-settings-api.ps1) for the five application-owned extension controls. Run them one at a time only after explicitly starting the local API in IsolatedLocalDemo mode. Each takes an external checksum-verified backup and restores its original setting state. They are not part of the normal read-only launch.

The Development test UI displays an isolated-database warning and enables only allowlisted fields with restorable loaded values. The current settings API allowlist has 93 fields. The seven final controls were tested through authenticated API save/readback, direct SQL readback, external checksum-verified backup and exact restoration; `ParameterModules` was also tested with option `1`. Original browser DOM evidence confirms that value belongs to `ddlParameterModules`; value `32` with the same label belongs to the separate `ddlParameterAdmin` control and its `ParameterAdmin` bigint column. Five controls use the application-owned extension table because their legacy destinations remain unresolved; these are editable in the new application, but the old portal is not known to consume them. `ParameterPayment` uses unsigned/signed 16-bit bit-pattern conversion; `ParameterProductSearch` splits its low SQL word from an application-owned high word. `UseFiltering` and `DiscountType` remain application-specific formats; legacy save behavior and portal parity remain unknown. `ProductCompany` and `Schedule` also passed authenticated API save/readback/restoration tests, including restoration of ProductCompany's NULL baseline. Schedule uses scalar 1/0 and its downstream import procedure was not invoked. Five upload fields use a separate application-owned local asset store; they do not update SQL or the legacy portal. All 98 settings have new-application persistence paths (93 settings API adapters plus five app-owned asset slots); all remain read-only in normal and Production modes.

The automated SQL integration tests exercise this endpoint's service with the temporary test flag set in the test process only. Run them from the repository root in PowerShell:

```powershell
$localFile = Join-Path (Join-Path $env:LOCALAPPDATA 'B2BStore') 'settings.local.json'
$localSettings = Get-Content -LiteralPath $localFile -Raw | ConvertFrom-Json
$env:B2BSTORE_SQL_CONNECTION = $localSettings.B2BStore.SqlConnection
$env:B2BSTORE_SQL_WRITE_TEST_MODE = 'true'
dotnet test .\B2BStore.Settings.slnx --no-restore
Remove-Item Env:B2BSTORE_SQL_CONNECTION, Env:B2BSTORE_SQL_WRITE_TEST_MODE -ErrorAction SilentlyContinue
Remove-Variable localSettings
```

The test harness targets only `store5`; do not run against `Store5_Analysis` or another database. Do not set the test flag in the persistent local settings file.

In Production, startup requires `B2BSTORE_ADMIN_TOKEN`, `B2BSTORE_SQL_CONNECTION`, and `B2BSTORE_MEMBER_CODE`, and rejects Mock mode. Secrets must come from the hosting secret store or process environment.

## Commands

```powershell
# Rebuild the machine-readable catalog from the supplied candidate mapping
node .\scripts\build-catalog.mjs

# Frontend build and tests
Set-Location .\frontend
npm run build
npm test
Set-Location ..

# Backend build
dotnet build .\B2BStore.Settings.slnx --configuration Release --no-restore

# Safe validation tests (no committed setting writes)
dotnet test .\tests\B2BStore.Settings.Tests\B2BStore.Settings.Tests.csproj --configuration Release --filter "FullyQualifiedName~SettingsValidationTests|FullyQualifiedName~BitmaskEditorTests|FullyQualifiedName~SqlReadOnlyIntegrationTests"

# Backend build
dotnet build .\B2BStore.Settings.slnx --no-restore
```

The authenticated application save workflow is opt-in per API process. For the local application, configure `B2BSTORE_SETTINGS_WRITE_MODE=IsolatedLocalDemo` alongside `B2BSTORE_MODE=SqlServer` and `ASPNETCORE_ENVIRONMENT=Development`; source the connection, member code and bearer token from `%LOCALAPPDATA%\B2BStore\settings.local.json` without displaying them. Vite reads the token server-side and proxies API calls; `B2BSTORE_API_URL` can select the API instance and is not embedded in the client bundle. Run the serialized `PersistsAndRestoresPreviouslyServiceOnlyFieldsThroughAuthenticatedApplicationApi` test only against an API process connected to the approved isolated database. It makes a checksum-verified external backup, tests save/API readback/SQL readback and restores captured values in `finally`. After validation, stop the write-enabled process and restart API in normal read-only mode.

Current release validation is recorded in [`docs/final-release-report.md`](docs/final-release-report.md) and [`docs/delivery-checklist.md`](docs/delivery-checklist.md); historical integration results remain in [`docs/test-bug-report.md`](docs/test-bug-report.md). The 40 fields in the current expansion were saved one at a time through authenticated `PUT /api/settings`, read back through the API and SQL store, then restored and verified. Four serialized groups used external `COPY_ONLY,CHECKSUM` backups that passed `RESTORE VERIFYONLY WITH CHECKSUM`. The prior 38 service-level integration tests were not repeated. SQL-operation and restoration evidence is in [`docs/sql-validation-report.md`](docs/sql-validation-report.md).

## API

See [`docs/openapi.yaml`](docs/openapi.yaml). `/health` is public and returns only an application health status. Settings reads require a bearer token. `PUT /api/settings` is absent unless explicit local Development test mode is enabled and then accepts only fixed-catalog allowlisted changes; Production and normal mode remain read-only. SQL bigint values use decimal strings in JSON. The UI does not send arbitrary table or column names.

## Known limits

- **Recovery and regression completed for the isolated test row.** A fresh external checksum-verified backup was made; a transaction changed only `ProductCompany` for the unique `store5` row back to NULL and verified exactly one updated row. SQL confirmed NULL. The explicit-NULL request contract distinguishes omitted, NULL, and empty string; contract tests pass. ProductCompany later passed a separate authenticated API save/readback/explicit-NULL restoration integration test and is now included in the isolated Development-only allowlist. The temporary test API is stopped.

- Portal effects are not runtime-tested; source code-behind and authorized test portal access were unavailable.
- The supplied source backup was not modified or restored. A separate backup of `Store5_Settings_Test` was created and verified outside the repository before the controlled committed-write UI test; all tested fields were restored and verified afterwards.
- Legacy save-handler behavior is unverified for all 98 controls. The authenticated Development-only `PUT` route has 93 fixed settings adapters; it is absent when test mode is disabled and in Production. Five controls use app-owned extension keys because their legacy destinations are unresolved, while `ParameterProductSearch` uses a hybrid SQL low-word plus extension high-word representation. Five image slots use app-only storage and do not affect the legacy portal.
- The UI inventory has two radio controls whose option values were not present in the supplied inventory; no values are invented.
- No public deployment was performed. Playwright screenshots and a short synthetic local Mock-mode recording are in [`docs/evidence/delivery/`](docs/evidence/delivery/); they contain no SQL customer values. The capture did not show save-validation feedback because the API was read-only and exposes no Save action in that mode.

## Historical isolated adapter milestones (superseded by the current status above)

### Additional verified isolated adapter fields (2026-11-20)

The allowlist contains 17 fields: the previous five plus ParameterTheme, ProductTableStockType, ParameterDesign, ParameterStock, ParameterProductDetail, ParameterProductDetail2, ParameterProductDetailNotLogin, ExportProductList, ParameterAccount, MainProducts, ParameterMenu, and ParameterDynamicSearch. Each new field was changed in a small isolated group, read back through SQL, restored to its captured original value, and verified after restoration. A distinct copy-only checksum-verified backup was created outside the repository before each group. Only `Store5_Settings_Test` / `MemberCode=store5` was used. `Store5_Analysis` was untouched. These checks verify the new adapter's round trip, not the unavailable legacy save handler. See [`docs/mapping-audit.md`](docs/mapping-audit.md) for per-control evidence and the remaining read-only status.


## Current persistence expansion (2026-11-21)

Historical status from 2026-11-21: the allowlist had 85 entries and Payment/ProductSearch were draft-only. This is superseded by the 2026-11-22 compatibility pass below: both now have tested application adapters, with ProductSearch's high bits stored in the application-owned extension table. Legacy behavior remains unverified.

The 40-field expansion and the 36 formerly service-level-only fields now have authenticated HTTP API save/readback/restoration evidence; earlier API-tested fields were retained. `ProductCompany`, `Schedule`, `UseFiltering`, and `DiscountType` additionally have field-specific authenticated API save/readback/restoration evidence with checksum-verified external backups. The app-only file route passed an authenticated HTTP upload/readback/delete test; it made no SQL operation. No prior persistence groups were rerun. Latest local validation: frontend 31/31 tests and production build passed; backend Release non-write suite passed 34/34; the local UI save/readback/restoration demonstration passed for `SiteStatus` and `ParameterOrder`, with an external checksum-verified backup. The app-only image preview/upload/remove flow was exercised and the temporary image removed. The test database row was restored to its captured baseline. Normal API was returned to read-only mode after the demonstration.

## Final reconciled counts (2026-11-22)

- Catalog: 98 unique setting/control IDs.
- UI: 98 controls are represented and editable in the isolated settings UI; all are read-only in normal mode. Original browser DOM evidence confirms `ParameterModules` option `1`; option `32` belongs to the separate `ParameterAdmin` control.
- Isolated test API persistence: 93 unique settings are in the authenticated Development-only API allowlist and have API save/readback/restoration evidence. Five extension-backed settings are application-only and do not claim old-portal compatibility. Five additional file slots persist only in new-app storage, verified through authenticated API upload/readback/delete. They do not write SQL or affect the legacy portal.
- Normal or Production write availability: 0 settings.
- New-application persistence: 93 settings use the fixed settings API allowlist and five image controls use the separate app-owned asset API. Five settings still lack legacy database destinations and use extension storage only. No save path is demonstrated for the old portal as a whole; the original save handler is missing.
- The supplied `store1` snapshot is an observation for that dealer and is not treated as `store5` data or a platform-wide behavioral test. The original C# save handler is unavailable, so legacy save parity is unknown for all 98.

Continuation QA note (2026-11-22): frontend tests/build passed again (28/28); .NET Release build passed (0 warnings/errors); focused serializer/null-presence tests passed (10/10); offline backend tests passed (27/27). A SQL read-only test could not be repeated: the required process connection variable was absent, and the existing private local settings file failed strict validation for the approved database and endpoint. No SQL connection was opened, no database writes occurred, and the final live SQL baseline was not rechecked. The application ports were not listening; process write flags were absent. Details and the exact test invocation are in [`docs/test-bug-report.md`](docs/test-bug-report.md).

Compatibility-pass validation (2026-11-22): the seven final controls plus `ParameterModules` passed authenticated isolated API save/readback, SQL readback, backup checksum verification and exact restoration serially. Backend Release build passed with zero warnings/errors; 56 non-SQL backend tests passed. Frontend tests passed 34/34 and the production build passed. The subsequent final delivery-capture pass added a focused Mock-versus-SQL connection-notice regression test; the latest frontend run is 35/35 and the production build passed again. The final read-only API returned 98 settings with `canSaveSettings=false`; an unauthorised write-route check returned 405. SQL checks were SELECT-only, confirming one target row, restored scalar baselines, `ProductCompany IS NULL`, and no remaining `store5` extension rows. No database writes occurred during the final delivery-capture pass.

Final compatibility pass (2026-11-22): Payment and Product Search now round-trip safely through fixed adapters; Product Search uses app-owned storage for high bits. Five extension controls are persisted only for this application. Five upload slots remain application-only because legacy retrieval conventions are not demonstrated. The original browser DOM resolves the separate `ParameterModules` option `1` and `ParameterAdmin` option `32`; the adapter test for `ParameterModules` option `1` already passed and was not repeated for this correction. See [`docs/setting-editability-matrix.md`](docs/setting-editability-matrix.md) and [`docs/sql-validation-report.md`](docs/sql-validation-report.md).

