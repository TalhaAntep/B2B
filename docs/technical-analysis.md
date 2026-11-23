# Current release context — 2026-11-23

The public GitHub Pages site is a static frontend-only synthetic showcase at https://talhaantep.github.io/B2B/. It is not connected to the ASP.NET Core API or SQL Server. The full-stack replacement remains a local implementation. The analysis and mapping limitations below are unchanged: the original Web Forms save handler is unavailable and old-portal persistence parity is unverified for all 98 controls.

# Technical analysis report

## Current consolidated release status — 2026-11-22

This status supersedes earlier discovery and milestone snapshots in this report. Those dated findings remain as historical evidence and are not current-state claims.

- The UI catalog contains 98 unique DealerSystemSetting control IDs. The authenticated Development-only settings API has a fixed allowlist of 93 setting adapters; five image controls use the separate application-owned asset service.
- Five setting controls use application-owned extension keys because no legacy destination was established: `ParameterProductDetailNotLogin2`, `ParameterMenu2`, `WhatsAppOrderNotification`, `FavoriteIconPosition`, and `FavoriteIconVerticalPosition`. Their new-app save/readback/restoration was tested; old-portal reads remain unverified. `ParameterProductSearch` is hybrid: its low 16-bit word is stored in candidate `DealerSettings.ParameterProductSearch` (`smallint`), and its high word in an application-owned extension key.
- `ParameterPayment` is persisted as the same unsigned 16-bit bit pattern in SQL `smallint` using signed representation. `ParameterModules` option `1` belongs to `ddlParameterModules` / `DealerSettings.ParameterModules` (`int`); `ParameterAdmin` option `32` belongs to the separate `ddlParameterAdmin` / `DealerSettings.ParameterAdmin` (`bigint`).
- Legacy save-handler parity remains unverified for all 98 controls because the original server-side Web Forms handler is unavailable.
- Current normal API mode is read-only; isolated test-write flags are disabled. Final SQL checks were SELECT-only against `Store5_Settings_Test` and the unique `MemberCode=store5` row. No database writes or public deployment occurred during this final delivery pass.
- Historical test and backup evidence is retained in the SQL/test reports. Backend test suites and SQL integration groups were not rerun during this delivery pass; only frontend tests/build and backend Release build are current-run results.

## Status and evidence labels

This report reflects the supplied v2 HTML-derived inventory, field-mapping candidate file, implementation prompt, and metadata-only inspection of `source/store5.bak`. `Verified` means independently confirmed against a repository artifact or read-only runtime metadata. `Found in source` means present in the prompt/inventory but not independently tested. `Candidate` and `Unresolved` retain the source mapping classifications. No runtime portal behavior is claimed.

## System and technology evidence

| Area | Finding | Status / evidence |
|---|---|---|
| Original settings page | Inventory describes a classic ASP.NET Web Forms form posting to `./DealerSystemSetting`, with Save using `__doPostBack` / `lbSaveAndExit`. | Found in source: `B2B_STORE_DealerSystemSetting_inventory_v2.json`; code-behind unavailable. |
| Existing application source | Not supplied in the repository. | Verified by repository file inventory; no original frontend/backend source. |
| New frontend | Vue 3.5.45, TypeScript 5.7.3, Vite 6.4.6. | Verified by installed package listing and successful build. |
| New backend | ASP.NET Core Web API targeting .NET 10.0; Microsoft.Data.SqlClient 7.1.2. | Verified by project file and successful backend compile. |
| Backend tests | Test project targets .NET 10.0. | Verified by project file and successful test run. |
| Local toolchain | .NET SDK 10.0.404 / runtime 10.0.15, Node 22.11.0, npm 10.9.0. | Verified from local tool output. |
| Existing API/auth | Not available for inspection. | Unverified; no original endpoint/auth code. |

**Runtime support:** The API and tests now target `net10.0` and were built with .NET SDK 10.0.404 / runtime 10.0.15. Microsoft lists .NET 10 as an active LTS release. Deployment remains unapproved and was not performed. [Microsoft .NET lifecycle](https://learn.microsoft.com/en-us/lifecycle/products/microsoft-net-and-net-core)

## Inventory and control types

The latest inventory reports 98 controls: 65 selects, of which 22 are multi-selects; 8 checkboxes; 14 text inputs; 4 text areas; 2 radio groups; and 5 file uploads. `docs/parameter-catalog.json` preserves the original labels, HTML control IDs/names, sections, option labels/values, candidate column names, and evidence notes. The catalog generator rejects a control count other than 98 and does not promote mappings to verified.

| Panel / section | Count |
|---|---:|
| User / General | 1 |
| User / Visual Settings | 7 |
| User / Color Settings | 2 |
| User / Google Analytics / Tag Manager Settings | 3 |
| Admin / Design Settings | 10 |
| Admin / Product Display Settings | 13 |
| Admin / Cart Settings | 5 |
| Admin / General Settings | 31 |
| Admin / Application Form Settings | 2 |
| Admin / Admin | 24 |
| **Total** | **98** |

The two radio controls have no option values in the supplied inventory artifact, so the UI shows them as unavailable rather than inventing options. Five file inputs are visible but disabled until storage behavior is verified.

## Candidate database mapping

The supplied mapping JSON reports 93 `semantic_control_mapping_candidate` rows, 2 `unresolved_control_has_no_clear_column`, and 3 `unresolved_no_schema_match`. These become **93 candidate / 5 unresolved / 0 verified** in the application catalog. The mapping Markdown says name/type comparisons were made against `DealerSettings`, but this checkout has no schema export; the configured local server had no `Store5_Analysis` database. Therefore the actual column/type comparison could not be reverified here.

Unresolved controls include `ddlParameterProductDetailNotLogin2` (B2C logged-out product detail parameter 2), `ddlParameterMenu2`, `chkWhatsAppOrderNotification`, and the two favorite-icon position radio controls. Exact candidate field names and per-control evidence are in [`parameter-catalog.json`](parameter-catalog.json).

The implementation keeps `DealerSettings` as the primary candidate table. The implementation prompt says `DealerSystemSettings` is a separate table with repeated login image/logo keys, `Settings` is a separate key/value table, `GeneralSettings` is separate, and `B2CSettings` is distinct; no schema query in this environment independently verified those facts. Do not substitute these tables without evidence.

## Database and backup findings

Read-only SQL connection to local SQL Server succeeded using Windows authentication. It reported SQL Server 2022 Express, build `16.0.1220.3`. `DB_ID('Store5_Analysis')` returned `NULL` on the checked local instances. `source/store5.bak` exists and is 356,638,208 bytes. Read-only backup header metadata reports database `Store5`, backup SQL version `13.0.5026`, backup date 2026-01-11, and backup size 356,629,504 bytes. The backup was not restored or modified.

Schema details (column names/types/nullability, keys, indexes, FK, constraints), `DealerSettings` row count, and any stored modules remain unverified. No stored procedure source or SQL script was present in the repository.

## Bitmask evidence

The inventory has 22 multi-select controls; every numeric option in those controls is represented as a decimal string, avoiding JavaScript floating-point conversion. Their values are preserved in the catalog. `Parameter*` names and power-of-two option values alone do not prove semantics.

The implementation prompt records partial procedure evidence for the following masks. These are **found in the supplied brief, not independently checked against SQL modules in this checkout**:

| Field / bit | Value | Description recorded in prompt |
|---|---:|---|
| `ParameterOrder` bit 16 | 65,536 | VAT-inclusive cart price handling in a procedure. |
| `ParameterOrder` bit 28 | 268,435,456 | Duplicate-item warning/check. |
| `ParameterOrder` bit 40 | 1,099,511,627,776 | Variation-related cart restriction. |
| `ParameterOrder` bit 13 | 8,192 | Group cart products by category. |
| `ParameterOrder` bit 33 | 8,589,934,592 | Changes cart item-count calculation. |
| `ParameterAdmin` bit 10 | 1,024 | Regional warehouse display. |
| `ParameterAdmin` bit 22 | 4,194,304 | Depot hiding / stock calculation. |
| `ParameterAdmin` bit 35 | 34,359,738,368 | Depot-based cart behavior. |
| `ParameterAdmin` bit 20 | 1,048,576 | Product hiding in cart SQL. |
| `ParameterAdmin` bits 19 / 31 | 524,288 / 2,147,483,648 | Company-brand visibility/discount paths. |
| `ParameterAdmin` bit 36 | 68,719,476,736 | Payment-type-specific discount filtering. |
| `ParameterIntegration` bits 1 / 2 / 4 | 1 / 2 / 4 | Customer import group updates; customer discount matching; ERP-imported sub-dealer orders. |
| `ParameterMenu` bit 20 | 1,048,576 | Sub-dealer menu behavior in customer import. |

These descriptions are partial and procedure-specific. In the normal UI, bitmask options are display-only. In isolated test mode, `ParameterOrder` edit operations are sent as strings and the backend computes the updated bigint while preserving all unrelated and unknown bits. NULL remains distinct from zero; the UI will not offer a persistent edit when the loaded original is NULL because the allowed operations cannot restore SQL NULL exactly.

## API and security

Normal API endpoints are `GET /health`, `GET /api/settings/catalog`, and `GET /api/settings`; the two settings routes require the local bearer token. There is no general-purpose settings `PUT` endpoint. SQL reads are scoped to the server-configured exact `MemberCode` and reject missing or ambiguous rows.

`POST /api/settings/test/rollback-only` is conditionally registered only in Development + SqlServer mode when `B2BSTORE_SQL_WRITE_TEST_MODE=true` is explicitly present in the process environment. It requires the authenticated local admin bearer and has no commit path: it always rolls back after transaction read-back. The separate `POST /api/settings/test/persistent-save` endpoint has the same process and bearer gates and, at that report date, a fixed 17-field allowlist. It checks the exact `(localdb)\\MSSQLLocalDB` endpoint, exact `Store5_Settings_Test` database, integrated security, and hardcoded `MemberCode=store5`. It checks catalog mappings and runtime SQL metadata, binds values as typed parameters, uses optimistic expected-value predicates and a serializable transaction, reads back before commit, and verifies again after commit. Bitmask updates accept only listed options and preserve unrelated and unknown bits. Exceptions before commit roll back in `finally`. The regular `/api/settings` route remains read-only; no public/general-purpose update endpoint exists, and `Store5_Analysis` is rejected by the connection guard.

The persistent route was exercised through the local browser UI after creating and checksum-verifying an external backup. `SiteStatus` and `ParameterOrder` were temporarily changed, refreshed/read back, then restored to their captured originals and refreshed/read back again. The two original values matched after restoration. This proves the new restricted API path only; it does **not** verify the absent legacy ASP.NET save handler or promote any fields to general-purpose editability. Mock mode and all standard SQL settings remain read-only.

## Portal behavior and unknowns

No portal settings were changed. The admin portal, dealer portal, source code-behind, database schema, stored procedure definitions, cache refresh behavior, licenses, and dependencies were not runtime-tested. Every described portal effect remains unverified except the partial SQL behavior statements supplied in the implementation prompt.

## Earlier isolated test-mode allowlist (2026-11-20; superseded by latest status below)

Seventeen settings are editable in the authenticated, explicitly enabled local Development test mode: SiteStatus, ParameterOrder, Layout, DontShowMenuAtDealers, DeliveryDateShow, ParameterTheme, ProductTableStockType, ParameterDesign, ParameterStock, ParameterProductDetail, ParameterProductDetail2, ParameterProductDetailNotLogin, ExportProductList, ParameterAccount, MainProducts, ParameterMenu, and ParameterDynamicSearch. Twelve fields were newly added to the allowlist this milestone. All 17 passed four grouped, checksum-backup-verified persistent save/read-back/restoration test runs: five scalar selects, eleven bitmasks, and one Boolean. Original values were restored and checked after every group. These tests validate the new adapter only; legacy save-handler behavior remains unknown for every field. The other 81 controls are draft-only or unsupported for persistence. All 98 controls remain read-only in normal mode. Store5_Analysis remains rejected by the server guard.

## Evidence bundle update (2026-11-21)

The evidence bundle supplies client HTML/DOM and FormManagement.js, not the original C# handler. The helper implements scalar select, comma-joined multi-select and Bool checkbox/radio transformations, but the exported settings HTML does not invoke FormToParameters and has no data-dbname/data-typename markers. During the evidence-only review, the 60 blocked controls were individually reassessed and none had a confirmed legacy server serialization format. The later scalar implementation below adds three fixed API adapters based on control, SQL type, and controlled round-trip evidence; 57 remain blocked. The six unresolved mappings and five file uploads remain blocked. The local token configuration issue was resolved by reading the nested B2BStore.AdminToken and rotating/restarting local services; the test-write flag was absent during that read-only check.

## Implementation update: fixed scalar settings API (2026-11-21)

`PUT /api/settings` is now conditionally registered only in Development + SQL mode with `B2BSTORE_SQL_WRITE_TEST_MODE=true`; server-side bearer authentication is still required. It delegates to `SqlPersistentSettingsTestService`, which enforces the fixed control-ID → column → SQL type allowlist, exact test database and `store5` member guard, one-row selection, typed parameterized SQL, expected-value concurrency predicates, serializable transaction, and read-back before/after commit. It is not a general-purpose SQL update endpoint. In normal mode and Production the route is absent and settings remain read-only.

The fixed allowlist was later expanded from 41 to 81 fields. The three numeric scalar HTTP checks remain historical evidence; the current expansion is recorded below.

Current persistence counts at the time of this earlier snapshot were 41 test-mode fields and 57 blocked controls. This snapshot is superseded by the current API expansion appended below.


## Current persistence expansion (2026-11-21)

The fixed authenticated local Development test-mode allowlist now contains **81** fields. Forty fields were newly enabled only after validating the original control shape, exact candidate column, SQL type, typed conversion, and controlled persistence behavior: 15 text scalars, 20 single-select scalars (four backed by SQL `bit`), one integer-backed checkbox, and four bitmask controls. The two other smallint multi-select candidates remain blocked because option values exceed signed `smallint` range.

All 40 new fields passed the authenticated application HTTP API test in four serialized groups. Each group used a separate external `COPY_ONLY,CHECKSUM` backup that passed `RESTORE VERIFYONLY WITH CHECKSUM`; each setting was read through API and SQL, restored to its captured value, and verified. The bitmask group confirmed unknown and unrelated bits were preserved. No downstream payment, email, import, stock or portal operation was triggered. Legacy save-handler parity remains unknown.

Persistence status is separate from business-effect status: the new adapters prove storage round trips for this application only; they do not prove the original portal's semantics. **17** settings remain blocked: B=3, C=5, D=5, E=1, F=3. The normal API remains read-only; test mode was stopped after validation. `Store5_Analysis` and all non-store5 row queries were avoided.

## Bilingual labels and current QA reconciliation

`mapping-audit.json` now carries `displayLabelEnglish` and `displayLabelTurkish` for all 98 control IDs. `setting-editability-matrix.md` shows the same English/Turkish label pair beside each current mapping row. Display labels are presentation metadata; original control IDs, option labels/values, database values and bitmask strings remain intact. The UI defaults to English and persists the EN/TR selection in browser local storage. `store1` inventory/snapshot evidence is treated only as an observation of the captured dealer, never as a `store5` value or universal portal result.

Final local QA: frontend 25/25 tests and production build passed; backend Release build passed with 0 warnings/errors; non-write backend suite passed 16/16 with the SQL read-only integration checks; normal API/Vite proxy returned 98 SQL settings and `testWritesEnabled=false`. The fixed adapters require authenticated explicit Development mode, including the new `B2BSTORE_SETTINGS_WRITE_MODE=IsolatedLocalDemo`; production and normal mode remain read-only. The 36 previously service-only fields subsequently passed authenticated API save/read-back/SQL compare/restoration, completing HTTP/browser evidence for all 81 fixed adapters. Seventeen remain draft-only. A later nullable `ProductCompany` exploratory save failed API restoration; it was then recovered to the captured NULL baseline with a single-field transactional update and checksum-verified external backup. The API request contract was made presence-aware and its regression tests passed; `ProductCompany` remains outside the allowlist. The write-test API is stopped and normal API mode is read-only. See `docs/test-bug-report.md` and `docs/sql-validation-report.md` for recovery evidence. Historical counts above are snapshots and are superseded by this section.

### Minimum inventory / indexing evidence boundary

The supplied page inventory and captured `store1` browser snapshot identify the control as **Minimum Inventory Quantity**; that is a label/observation, not proof of platform-wide effect. The SQL definition extract for `dbo._ServiceExportStockXml` contains a conditional dynamic predicate on the `DealerSettings.MinStockFilter` value when `@DepotID=''`: `product.stocknumber >= (select top 1 MinStockFilter from DealerSettings)`. This is direct evidence that this procedure applies a stock-number threshold in that export path. A separate procedure excerpt contains `Product.UpdateLucene=1` in `_ServiceImportPrice`, evidencing a Lucene update marker during that import path. The available extracts do not show that `MinStockFilter` controls Lucene indexing, nor do they establish reindexing/runtime behavior. These two facts must remain separate; no indexing dependency is inferred for this setting.

## Evidence levels and portal limits (final reconciliation)

The 98-row machine audit separates source-control evidence from SQL consumers and customer-facing behavior. The supplied HTML/DOM inventory directly establishes each control ID, control type and listed option set (98/98). SQL consumer evidence is direct for four settings (`Schedule`, `HasStockVisible`, `ParameterIntegration`, `MinStockFilter`) and unresolved for the other 94. No runtime dealer portal setting effect was tested, so portal-effect evidence is unresolved for all 98. The `store1` browser snapshot is a dealer-specific observation only. No setting-to-setting dependency is confirmed. No claim of exact Web Forms C# save parity is made.

The available evidence does not justify a **Strong inference** classification for a runtime portal effect; where intent is suggested by a control label or option, the UI displays the source label/option and avoids claiming the outcome. Descriptions appear only where the HTML inventory or SQL extract supports the limited wording.

## Final blocked-settings milestone (2026-11-22)

`ProductCompany` and `Schedule` now have fixed, field-specific SQL test adapters. ProductCompany passes nullable tinyint source values and explicit NULL restoration. Schedule does not use bitwise OR: its one source option is serialized to tinyint 1/0 based on the existing `_ServiceImportProduct` consumer. Both passed separate authenticated API save/API readback/SQL readback/restoration tests with fresh checksum-verified external backups; the stock-import procedure was not executed. The 81 prior write groups were not rerun.

The five image fields now have an application-owned test-mode upload route and local persistent metadata, strict generated names, content/MIME/extension validation, a 5 MiB limit, safe replacement/deletion, and path containment. This storage is explicitly **new-application-only** and does not affect DealerSettings or the old portal. Authenticated HTTP and service-level tests passed without SQL operations.

Historical snapshot (superseded by the final local QA below): 83 SQL allowlisted controls (47 authenticated API round-trips, 36 service-level-only), five app-only persisted file slots, and ten draft-only settings (B=3, C=5, F=2).

### Final blocked-settings resolution and verification (2026-11-22)

The final 17 were individually dispositioned: `ProductCompany` and `Schedule` gained fixed SQL adapters with authenticated API save/readback/SQL verification and exact restoration; the five image controls gained isolated application-owned storage (not legacy portal storage); ten controls remain SQL draft-only. `Schedule` serializes its single source option as scalar tinyint 1/0, based on the `_ServiceImportProduct` consumer, and does not use bitwise operations. `ProductCompany` is nullable tinyint; explicit JSON null restored its captured NULL baseline. The remaining ten and their per-field blockers are listed in `docs/setting-editability-matrix.md`. The 81 successful prior persistence groups were not repeated.

Latest prior milestone validation: backend Release build 0 warnings/errors; non-write backend tests 29/29; focused nullable/Schedule serializer tests 10/10; ProductCompany, Schedule, and app-asset API integration tests each 1/1; frontend tests 28/28 and production build passed. In this continuation QA, the Release build again passed with 0 warnings/errors, the focused serializer/presence tests passed 10/10, the frontend passed 28/28 plus production build, and the offline backend suite passed 27/27. One attempted broader non-write invocation had 28 passes and one SQL prerequisite failure because its connection environment variable was absent. The private local settings file failed a subsequent strict preflight for the exact approved DB/endpoint, so no SQL connection was opened and no fresh SQL/API read was made. Ports 5080/5081/5173 had no listeners. Process write flags were absent. No database writes occurred during this continuation QA; do not infer a fresh baseline verification from this run.

**Read-scope incident disclosure:** during targeted schema work, a read-only metadata query against `GeneralSettings` filtered on setting keys but omitted `Tab='store5'`. It returned metadata rows carrying other dealer tab identifiers. It did not select setting values, did not query other `DealerSettings` MemberCode rows, and issued no writes. No additional broad `GeneralSettings` query followed. This exceeded the stated member scope; the incident is also recorded in `docs/test-bug-report.md`.

## Final local application QA (2026-11-22)

Current reconciled status: 98 catalog controls, 98 UI editors, 93 isolated Development SQL adapters, five application-only asset slots, and five SQL-draft-only controls. `ParameterModules` is available with its DOM-confirmed option value `1`; the separate `ParameterAdmin` control retains option value `32`. Both map independently to their corresponding `DealerSettings` columns (`ParameterModules` int and `ParameterAdmin` bigint). All 93 SQL adapters have authenticated API save/readback/restoration evidence; legacy Web Forms save-handler parity remains unknown. Normal API mode is read-only.

## Final compatibility implementation status - 2026-11-22

The current count supersedes the preceding historical snapshot: 98 settings are in the UI catalog and have editors; 93 are in the fixed authenticated Development API allowlist, with isolated HTTP save/readback/SQL-readback/restoration evidence. The five controls with unresolved legacy destinations persist in the new application through fixed extension keys; they are not draft-only, but the old portal is not known to consume them. The five application-owned image slots remain separate from SQL persistence.

The final seven controls were handled as follows:

- `ParameterPayment`: `DealerSettings.ParameterPayment` is SQL `smallint`; the API models it as an unsigned 16-bit bitmask and converts the identical bit pattern to signed SQL `smallint`. Unknown bits are retained. Authenticated API, SQL readback and restoration passed.
- `ParameterProductSearch`: the low 16-bit word uses `DealerSettings.ParameterProductSearch` (`smallint`) and the high word uses `dbo.DealerSystemSettingExtension` key `ParameterProductSearch.HighMask`. Reads combine the portions. Tests verified high/low readback, unrelated bit preservation and restoration. The extension high word is application-only; old-portal behavior is unknown.
- `ParameterProductDetailNotLogin2`, `ParameterMenu2`, `WhatsAppOrderNotification`, `FavoriteIconPosition`, and `FavoriteIconVerticalPosition` persist through fixed application-owned extension keys. The control encodings are 64-bit decimal masks, explicit Boolean text, and independent canonical horizontal/vertical enums. Each passed isolated authenticated API and SQL verification and exact restoration. These values are not claimed to be consumed by the legacy portal; the notification operation was not invoked.
- `ParameterModules`: nullable `int` column, DOM option value `1` (“Show Order Export Button”), and fixed adapter round-trip were verified. The separate `ddlParameterAdmin` control retains option `32` for the same label and maps to `DealerSettings.ParameterAdmin` (`bigint`). The two control/column mappings are independent; no option-ID ambiguity remains. Legacy save-handler behavior remains unknown.

The `dbo.DealerSystemSettingExtension` schema was applied explicitly to `Store5_Settings_Test` only using `backend/B2BStore.Settings.Api/Migrations/001_DealerSystemSettingExtension.sql`. It has a `(MemberCode, SettingKey)` primary key and no unverified foreign key. It is not part of the legacy portal schema. The 2026-11-22 SQL tests were serialized, took a separate external checksum-verified backup for each field group, and restored their original values. Final read-only checks confirmed one target row, `ProductCompany` still NULL, the three direct column baselines restored, and zero `store5` extension rows.

Validation in this continuation: .NET 10 Release build succeeded with zero warnings/errors; 56 non-database backend tests passed. Frontend tests passed 34/34 and production build succeeded. Eight new SQL-backed HTTP groups passed (Payment, Modules, Product Search, two extension masks, WhatsApp Boolean, and two favorite-position enums). Eight latest group backups passed checksum verification. Normal API was restarted with both write flags absent; it returned 98 settings, `canSaveSettings=false`, and returned 405 to a harmless PUT check. No write to `Store5_Analysis` or any MemberCode other than `store5` occurred in this pass. The legacy save handler remains unavailable, so no exact legacy implementation parity is asserted.
