# Current release addendum — 2026-11-23

The synthetic public demo is deployed at https://talhaantep.github.io/B2B/ and was opened successfully by the project owner. The public-demo branch passed `npm test` (**36/36**) and `VITE_PUBLIC_DEMO=true` production build. The local full-stack frontend had passed 35/35 and the .NET Release build passed with zero warnings/errors. The preceding compatibility pass recorded 56/56 filtered non-database backend tests and eight focused SQL API groups; those groups were not rerun during public-demo publication.

The first GitHub Actions deployment attempt returned Pages API `404 Not Found`; the repository Pages source was initialised/configured and the workflow rerun. The static page was subsequently opened. This resolves the frontend showcase deployment only. The ASP.NET Core API and SQL Server remain local-only; no full-stack public deployment is claimed.

# Test and bug report

## Final delivery check — 2026-11-22

Current commands run: frontend `npm test` passed 35/35; frontend `npm run build` passed; backend `dotnet build B2BStore.Settings.slnx --configuration Release --no-restore` passed with 0 warnings and 0 errors. Backend test suites and SQL integration groups were not rerun in this delivery pass. No database writes occurred. Prior backend and SQL integration results below remain historical, preserved evidence.
## Current consolidated release status — 2026-11-22

This status supersedes earlier discovery and milestone snapshots in this report. Those dated findings remain as historical evidence and are not current-state claims.

- The UI catalog contains 98 unique DealerSystemSetting control IDs. The authenticated Development-only settings API has a fixed allowlist of 93 setting adapters; five image controls use the separate application-owned asset service.
- Five setting controls use application-owned extension keys because no legacy destination was established: `ParameterProductDetailNotLogin2`, `ParameterMenu2`, `WhatsAppOrderNotification`, `FavoriteIconPosition`, and `FavoriteIconVerticalPosition`. Their new-app save/readback/restoration was tested; old-portal reads remain unverified. `ParameterProductSearch` is hybrid: its low 16-bit word is stored in candidate `DealerSettings.ParameterProductSearch` (`smallint`), and its high word in an application-owned extension key.
- `ParameterPayment` is persisted as the same unsigned 16-bit bit pattern in SQL `smallint` using signed representation. `ParameterModules` option `1` belongs to `ddlParameterModules` / `DealerSettings.ParameterModules` (`int`); `ParameterAdmin` option `32` belongs to the separate `ddlParameterAdmin` / `DealerSettings.ParameterAdmin` (`bigint`).
- Legacy save-handler parity remains unverified for all 98 controls because the original server-side Web Forms handler is unavailable.
- Current normal API mode is read-only; isolated test-write flags are disabled. Final SQL checks were SELECT-only against `Store5_Settings_Test` and the unique `MemberCode=store5` row. No database writes or public deployment occurred during this final delivery pass.
- Historical test and backup evidence is retained in the SQL/test reports. Backend test suites and SQL integration groups were not rerun during this delivery pass; only frontend tests/build and backend Release build are current-run results.

## Automated test results

| Area | Command | Result |
|---|---|---|
| Frontend tests | `npm test` | Latest run passed: 29 tests across 2 files, including EN/TR default/switch/persistence, bilingual search, selected option labels, all 98 catalog controls and source option integrity. |
| Frontend production build | `npm run build` | Final run passed: `vue-tsc -b` and Vite production build. |
| Backend Release build | `dotnet build .\B2BStore.Settings.slnx --configuration Release --no-restore` | Final run passed: API and tests target `net10.0`; 0 warnings and 0 errors. |
| Backend Release non-write tests | `dotnet test B2BStore.Settings.slnx --configuration Release --no-restore --filter 'FullyQualifiedName!~CatalogPersistentIntegrationTests&FullyQualifiedName!~RollbackWriteSafetyTests&FullyQualifiedName!~PersistentWriteSafetyTests'` with private local SQL configuration; write flag removed | Final run passed: 16/16, including both read-only SQL integration tests. Database mutation and rollback test classes were not rerun; prior verified groups are retained below. |
| SQL rollback validation | `RollbackWriteSafetyTests` | Passed: SiteStatus and ParameterOrder read back inside a transaction; rollback restored original values. A stale concurrency conflict after the first update also rolled back. `Store5_Analysis` was rejected before connection. |
| Persistent API test | Authenticated local Development API | Three numeric text fields were individually saved through `PUT /api/settings`, read back through the API and SQL, restored to captured originals, and verified. External backups passed `RESTORE VERIFYONLY WITH CHECKSUM`. |
| API authorization and isolation | Service checks and local Development API | Test-mode writes require Development + SQL mode, explicit process flag, bearer auth, fixed allowlist, exact Store5_Settings_Test database, and MemberCode=store5. Analysis DB requests are rejected before connection. Normal-mode authenticated `PUT /api/settings` returns 405. Values were suppressed. |
| Earlier local API smoke | .NET 10 Development API + PowerShell HTTP requests | Passed: `/health` returned `ok`; settings returned SQL mode with 98 controls. Values were suppressed. |
| Admin/dealer portal | No portal interaction | Test Edilemedi: no authorized session/runtime workflow was provided for this implementation pass. |

The frontend unit tests cover exact high-order bit display and SQL NULL distinct from zero. Component tests verify global search, category navigation, unresolved/read-only state and mixed scalar/bitmask save request construction.

Backend tests cover bit enable/disable, idempotency, preserving neighboring and unknown bits, invalid options, `NULL`, exact bigint string serialization, bigint SQL parameter conversion, catalog count/status, invalid field/value rejection, exact row selection guard, mock write rejection, test-mode/authentication checks, SQL database isolation, persistent-path stale concurrency rejection, in-transaction read-after-write, and rollback verification. Integration test output contains no setting values.

## Focused final SQL-setting expansion (2026-11-22)

These tests were run serially against only `Store5_Settings_Test` and `MemberCode=store5`; earlier successful API integration groups were not repeated.

| Field | Validation command / evidence | Result |
|---|---|---|
| UseFiltering | `dotnet test tests/B2BStore.Settings.Tests/B2BStore.Settings.Tests.csproj -c Debug --no-build --filter FullyQualifiedName~PersistsAndRestoresUseFilteringDecimalTextMaskThroughAuthenticatedApplicationApi` | Passed 1/1. Separate external backup passed checksum verification; authenticated save, API readback, SQL readback, decimal-text check, unknown/unrelated bit preservation, and exact restore passed. |
| DiscountType | `dotnet test tests/B2BStore.Settings.Tests/B2BStore.Settings.Tests.csproj -c Debug --no-build --filter FullyQualifiedName~PersistsAndRestoresDiscountTypeSourceCodeThroughAuthenticatedApplicationApi` | Passed 1/1. Separate external backup passed checksum verification; authenticated save, API readback, SQL readback, bare-code codec check, and exact restore passed. |
| Codec/unit coverage | `dotnet test tests/B2BStore.Settings.Tests/B2BStore.Settings.Tests.csproj -c Debug --no-restore --filter FullyQualifiedName~DealerSettingValueCodecTests` | Passed 7/7. |
| Backend build | `dotnet build backend/B2BStore.Settings.Api/B2BStore.Settings.Api.csproj -c Debug --no-restore` | Passed; 0 warnings and 0 errors. |
| Frontend | `npm test` | Passed 29/29 tests across two files. A placeholder-option expectation and the newly test-editable UseFiltering state were corrected before this final passing run. |
| Frontend production build | `npm run build` | Passed after the UX text was aligned with the test-only adapter status. |

Final read-only preflight reported database `Store5_Settings_Test`, exactly one row for `store5`, and `ProductCompany IS NULL`. The normal API served 98 settings with test writes disabled and no editable settings; the frontend remained available. Temporary test API on port 5081 was stopped. Neither `Store5_Analysis` nor another MemberCode was accessed. The two new codecs are application-specific test adapters; legacy save-handler parity remains unknown.

## Known defects / limitations

- **Recovered `ProductCompany` and verified explicit-NULL persistence (2026-11-21/22):** A fresh external backup passed `RESTORE VERIFYONLY WITH CHECKSUM`. Recovery set only `ProductCompany` to its captured NULL baseline for the unique `store5` row. Then the presence-aware API contract distinguished omitted scalar, explicit `null`, and empty string; contract tests passed 14/14. A separate single-field authenticated API integration test made a fresh external checksum-verified backup, saved a source option, verified API and SQL readback, restored explicit NULL through the API, and verified restoration through both API and SQL (1/1 passed). No previous persistence groups were rerun. `ProductCompany` is now test-mode-only; legacy behavior is still unknown. Test API is stopped and normal API mode reports writes disabled.

- Legacy persistence behavior remains unknown for all 98 controls because the original save handler is unavailable. The current Development test-mode allowlist has 85 fields; 49 have authenticated API round-trip evidence and 36 adapters have service-level evidence only. The standard/Production API is read-only.
- Five file upload controls and two radio option controls (one group) cannot be persisted safely from current evidence.
- Legacy portal persistence behavior remains unverified; transient SQL round trips do not prove that the original portal saves through these fields.
- No existing-portal behavior has been tested; no defect in the legacy portal is claimed or fixed.

## Verification — expanded editable settings (2026-11-20)

- Frontend: `npm test` passed, 11/11 tests; `npm run build` passed.
- Backend: .NET 10 Release build passed with 0 warnings/errors; all 33 tests passed across four backup-verified persistence groups and the remaining test suite.
- SQL persistent integration: the then-current 17 allowlisted fields (12 newly enabled, plus five previously enabled) were exercised across four groups: the original five, then groups of 3, 4, and 5 new fields. Every group created a copy-only `Store5_Settings_Test` backup outside the repository, passed `RESTORE VERIFYONLY WITH CHECKSUM`, persisted temporary values, verified read-after-write, restored the captured originals, and verified restoration. Bitmask tests checked that unrelated and unknown bits remained unchanged.
- Backup: each committed-write group had its own verified external backup. Backup paths contain no application data and are kept outside the repository.
- Database isolation: only `MemberCode=store5` in `Store5_Settings_Test` was addressed. `Store5_Analysis` was untouched.
- At that earlier milestone, the explicit test-mode count was 17 save-enabled fields and 81 blocked controls; this snapshot is superseded by the current validation section below.
- No production deployment occurred. After browser verification the API is returned to Development SQL read-only mode with the write flag absent.

## Legacy evidence and final local read validation (2026-11-21)

- Incorporated the supplied HTML export, 123-control DOM inspection, FormManagement.js, redacted browser snapshot, inventory/mapping copies and targeted SQL extracts. The 60 currently blocked settings were each reassessed; 0 gained enough linked persistence evidence for implementation. See the per-control matrix in docs/setting-editability-matrix.md.
- FormManagement.js establishes generic client transformations, including comma-joining multi-select values and Boolean checkbox values. The exported page has no FormToParameters invocation or data-dbname/data-typename attributes. The legacy C# save handler remains unavailable, so no server serialization or parity claim is made.
- The 401 was caused by the diagnostic request, not the app configuration: it sent a transformed/double-encoded token (a later diagnostic also checked a non-existent root-level property). Both API and Vite read the raw token from B2BStore.AdminToken. The local token was rotated after the diagnostic mishap and both local services restarted; the corrected proxied and direct authenticated reads succeeded.
- Fresh authenticated Vite-proxied SQL read returned mode SqlServer, 98 settings, 7 SQL NULL values, 0 editable settings and testWritesEnabled=false. Catalog count was 98. API health returned 200. SQL preflight confirmed Store5_Settings_Test and exactly one MemberCode=store5 row.
- No SQL writes occurred during this evidence review. The SQL write-test flag was absent/false. Store5_Analysis and other MemberCode rows were not accessed.
# Fixed scalar API expansion (2026-11-21)

## Implemented

- Added a Development/test-mode-only authenticated `PUT /api/settings` route, backed by the same fixed server-side allowlist as the isolated persistent test service. The route does not exist in normal or Production mode.
- Added `MinCartCount` (`int`), `MinCartTotal` (`decimal(18,2)`), and `MaximumOrderQuantity` (`int`) as typed fixed mappings. Numeric text values are invariant-culture strings; decimal precision/scale are checked against SQL metadata and explicit SQL parameters.
- Added UI editors for these three settings only when the server advertises test-write mode and a non-NULL current value fits the SQL range. Existing bigint and unrelated bit behavior is unchanged.
- Updated the 60-control matrix with primary class A–F for every control and retained the per-field blocker/evidence in each row. New current blocked count is 57.

## API integration verification

- External `COPY_ONLY,CHECKSUM` backups were created outside the repository and `RESTORE VERIFYONLY WITH CHECKSUM` passed before changes.
- `MinCartCount`, `MinCartTotal`, and `MaximumOrderQuantity` were each saved through authenticated `PUT /api/settings`, read back through the API and a read-only SQL comparison, restored to the captured original, and verified again. Result: **3/3 passed**.
- The existing 38 isolated service adapters were not re-tested through the HTTP endpoint. They are now routed through the API only when Development, SQL mode, explicit process test flag, and bearer authorization are all active.
- No values, tokens, SQL connection strings, or backup paths are recorded. Only `Store5_Settings_Test` / `MemberCode=store5` was used. `Store5_Analysis` and other MemberCode rows were not accessed.

## Automated validation

- Backend: `dotnet build B2BStore.Settings.slnx --configuration Release --no-restore` passed with zero warnings/errors. The non-repeat backend set passed **30/30** using the configured isolated SQL target. Previously successful committed-write integration groups were excluded to avoid unnecessary repeat writes; rollback-only/read-only safety tests and the new numeric validation tests ran.
- Frontend: `npm test -- --run` passed **22/22** tests; `npm run build` passed (`vue-tsc -b` and Vite production build).
- The new SQL API integration checks passed **3/3** for save, API/SQL read-back, restoration and verification.

## Known evidence limit

The original Web Forms code-behind remains unavailable. The new typed adapters are implementation behavior validated against the isolated database; exact legacy save-handler parity and portal side effects remain unknown. The 57 blocked fields remain classified A–F in the matrix, with reasons retained per row.

## Final safety state

- Normal API on `127.0.0.1:5080` returned SQL mode with 98 settings and `testWritesEnabled=false`; `PUT /api/settings` was rejected with HTTP 405 after the test-mode API process was stopped.
- Copy-only external backups passed `RESTORE VERIFYONLY WITH CHECKSUM`. All three new original values were restored and confirmed by API and SQL comparisons. No setting values are recorded.
- Only `Store5_Settings_Test` and `MemberCode=store5` were targeted. `Store5_Analysis` and other member rows were not accessed.


## Current expansion result (2026-11-21)

- Frontend unit tests and production build were rerun after the UI allowlist update; see the exact results below after the current run.
- The authenticated application API integration test passed for 40 newly enabled settings: 15 text scalars, 20 scalar selects, one integer-backed checkbox, and four bitmasks. It saved each field individually, verified API and SQL read-back, and restored the captured original. Four serialized external backups passed checksum verification.
- A SQL-bit dropdown concurrency conversion bug was discovered when a `true` baseline was flattened to `false`; the adapter now preserves a Boolean expected value and the SQL-bit scalar group passed. An integer-checkbox restore conversion bug was also found; a backup-verified one-field recovery restored the original after the failed restore, then the full update/read/restore group passed. No values were logged.
- Two smallint multi-select settings are excluded from the allowlist because their source options exceed signed smallint range.
- The current test-only allowlist is 81; 17 remain draft-only (B=3, C=5, D=5, E=1, F=3). The standard API is read-only.

## Final reconciled delivery QA (2026-11-21)

| Measure | Verified result |
|---|---:|
| Catalog controls | 98 unique IDs |
| UI coverage | 98 represented controls and editors; `ParameterModules` option `1` is confirmed by original browser DOM evidence and remains independent from `ParameterAdmin` option `32` |
| Test-only fixed persistence adapters | 81 control/column/type entries in `SqlPersistentSettingsTestService` |
| Authenticated Development test API allowlist | Same 81 fixed entries; route is absent when the explicit flag is disabled and in Production |
| HTTP/browser save, API/SQL read-back, and restoration evidence | 45 fields (40 in the latest four groups, 3 earlier numeric scalars, and 2 earlier browser-tested fields) |
| Service-level-only persistence tests | 36 fields |
| Draft-only persistence | 17 fields (B=3, C=5, D=5, E=1, F=3) |
| Normal/Production write availability | 0 fields |

Final frontend validation: `npm test` passed **25 tests across 2 files**; `npm run build` passed (`vue-tsc -b` and Vite production build). Backend validation: `dotnet build .\B2BStore.Settings.slnx --configuration Release --no-restore` passed with 0 warnings and 0 errors. The non-write Release test command excluded the three database-write test classes (`CatalogPersistentIntegrationTests`, `RollbackWriteSafetyTests`, and `PersistentWriteSafetyTests`) and passed **16/16**, including both `SqlReadOnlyIntegrationTests` against the configured local SQL connection. Prior successful write and rollback groups were retained as evidence, not unnecessarily repeated.

A normal local API/Vite-proxy check returned `/health=ok`, SQL mode, 98 settings, `testWritesEnabled=false`, and HTTP 405 for `PUT /api/settings`. The API process ran on `127.0.0.1:5080` without the write-test environment flag. The local credential source remained `%LOCALAPPDATA%\B2BStore\settings.local.json`; no credential material was printed or written to tracked files.

Prior committed test groups used separate external `COPY_ONLY,CHECKSUM` backups, each verified with `RESTORE VERIFYONLY WITH CHECKSUM`; each field's original value was restored and read back. The checkbox recovery and complete checkbox group passed after correcting conversion. No new SQL writes occurred in final QA. The API read was limited to the configured `Store5_Settings_Test` / `MemberCode=store5` target; `Store5_Analysis` and other member rows were not accessed.

English is the default UI language; Turkish is selectable and remembered in local storage. Source values, IDs, options, bigint decimal strings, and bitmask semantics are unchanged by display localization. Public deployment was not performed; no public URL exists. The original C# save handler and portal behavior remain unverified, and human approval/security review is required before any production deployment or persistence expansion.

## Legacy portal observations versus test results

- Evidence file `docs/evidence/legacy/B2B_STORE_legacy_evidence_bundle/inspection/browser-control-values.md` is a redacted browser-control snapshot captured in the `store1` page context (not `store5`). It records example UI selections and control shapes only. It is not a controlled before/after experiment; setting effects on a live portal are **Not tested**.
- Minimum Inventory Quantity (`MinStockFilter`): the snapshot identifies a text control. The SQL definition in `docs/evidence/legacy/B2B_STORE_legacy_evidence_bundle/sql-evidence/sql-module-definitions-extract.txt`, procedure `dbo._ServiceExportStockXml`, uses `product.stocknumber >= (select top 1 MinStockFilter from DealerSettings)` when `@DepotID=''`. Result: direct SQL behavior evidence for that export path only; live portal display and Lucene behavior **Not tested**.
- The same SQL extract separately contains `Product.UpdateLucene=1` in `dbo._ServiceImportPrice`. This indicates an update marker in the price-import procedure, but no evidence ties it to `MinStockFilter`. No Lucene-indexing dependency is claimed for Minimum Inventory Quantity.

## Final local application QA (2026-11-22)

- Historical reconciliation snapshot: 98 catalog controls; 97 UI editors; ParameterModules was then treated as disabled due to incomplete option evidence. Superseding original browser DOM evidence confirms option `1` for `ddlParameterModules`, separate from option `32` for `ddlParameterAdmin`. The Development-only allowlist and integration evidence are recorded in the current mapping audit; no legacy Web Forms parity is claimed.
- A fresh external copy-only backup of `Store5_Settings_Test` passed `RESTORE VERIFYONLY WITH CHECKSUM` before the UI save demonstration. SQL preflight verified the named test database, LocalDB endpoint pattern, exactly one `MemberCode=store5` target, and the `ProductCompany` NULL baseline. The scalar and bitmask settings were restored through the application API and compared with their captured baselines; the bitmask check confirmed the selected bit changed alone and unrelated/unknown bits were preserved.
- A temporary synthetic PNG was uploaded through the local Vite/API proxy into an empty app-owned logo slot. The browser rendered the preview and removed that slot. The app-owned metadata now contains zero assets; the temporary input file was deleted. This storage is application-only and does not update the legacy portal.
- Final API state: Development SQL read mode, 98 settings returned, `GET /api/settings/capabilities` reports `ReadOnly`, the normal save route returns HTTP 405, and application-asset routes are not mapped in read-only mode. The normal API is on `127.0.0.1:5080`; the frontend remains on `127.0.0.1:5173`.
- Validation: frontend **31/31** tests passed; `npm run build` passed. .NET 10 Release solution build passed with **0 warnings and 0 errors**; the non-write backend suite passed **34/34**. Previously successful database integration groups were not rerun.
- Secret scan found no workspace `.env` files or credential-like values in source/docs. The provided `source/store5.bak` remains present and untouched; the new verified test backup is outside the repository. This task performed no `Store5_Analysis` access and no access to any other `MemberCode` row.

## Final blocked-settings milestone (2026-11-22)

- SQL Development test allowlist: **83** fixed `DealerSettings` mappings. This milestone added authenticated API round-trip tests for `ProductCompany` (nullable tinyint; explicit NULL restoration) and `Schedule` (field-specific tinyint scalar 1/0; no bitwise OR). Each had a fresh external checksum-verified backup, API and SQL readback, and exact restoration. The original 81 persistence groups were not rerun.
- Five file controls now have separate application-owned storage, **not legacy-compatible**: generated filenames, persistent JSON metadata, PNG/JPEG/WebP/ICO signature and MIME checks, 5 MiB cap, staged replacement, scoped deletion and path checks. Storage unit tests passed; one authenticated test API upload/list/content-read/delete test passed. These uploads made no SQL operations.
- Historical snapshot: the then-count of draft-only fields included ParameterModules due to missing option evidence. This is superseded by the original browser DOM reconciliation documenting its option `1`; consult the current editability matrix for present status. The six unresolved controls, including `UseFiltering`, remain outside SQL writes.
- Validation: Release build passed with 0 warnings/errors; frontend tests **28/28** and production build passed; non-write backend suite **29/29**; focused Schedule/NULL tests **10/10**; ProductCompany, Schedule, and app-asset API integration tests each **1/1**. Temporary test API processes were stopped; normal API remains read-only.

**Read-scope incident:** during targeted schema work, one read-only query against `GeneralSettings` filtered by setting keys but omitted the `Tab='store5'` scope. It returned metadata rows with other dealer tab identifiers. It selected no `SettingValue`, performed no write, and did not query other `DealerSettings` member rows. No further `GeneralSettings` data query was run. This was outside the requested member scope and is disclosed here; no database contents were changed.

## Continuation QA and SQL preflight (2026-11-22)

- No previous 81 persistence groups were rerun. Frontend tests: 28/28; production build passed. .NET Release build: 0 warnings/errors. Focused `ScheduleScalarSerializerTests|SettingChangePresenceTests`: 10/10. Offline backend suite: 27/27.
- A first broad non-write test invocation produced 28 passed and one SQL read-only integration prerequisite failure because `B2BSTORE_SQL_CONNECTION` was absent. The SQL-only test was not successfully rerun.
- The private local settings file was inspected in memory without printing its content, but its database/endpoint did not pass the exact approved-target preflight. No SQL connection was opened. No database reads or writes occurred in this continuation QA.
- Process write-test environment flags were absent. Ports 5080, 5081 and 5173 had no listeners. Therefore the temporary write API is stopped; the normal API was also stopped rather than running live in read-only mode.
- Do not treat the prior restored baseline as freshly reverified in this continuation. Earlier evidence remains documented above; no restoration/write test was repeated.

## Compatibility pass test note (2026-11-22)

The 56/56 filtered non-database backend test result is separate from the SQL integration evidence. Eight new SQL-backed HTTP groups passed serially with external verified backups and exact restoration. The broad backend invocation without required SQL test environment variables failed in the integration tests at their safety preconditions; it did not exercise database writes. Frontend tests passed 34/34; Release backend build passed with no warnings/errors; frontend production build passed. Original browser DOM evidence subsequently resolved the ParameterModules/ParameterAdmin option assignment; app-owned extension values still have no demonstrated reader in the old portal.
