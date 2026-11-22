# SQL validation report

## Current final read-only check — 2026-11-22

The SQL-backed application check used only the approved `Store5_Settings_Test` database and `MemberCode=store5` configuration. The API returned 98 settings in `SqlServer` mode; capabilities were `ReadOnly`, save and asset-management capabilities were false, and the write-test flag was false. Earlier SQL validation confirmed exactly one target `dbo.DealerSettings` row, `ProductCompany IS NULL`, and zero application-extension rows for `store5` after restoration. Those direct SQL checks were SELECT-only. No SQL integration tests or writes were performed during final delivery. No other MemberCode was selected. `Store5_Analysis` was not modified.

The dated sections that follow preserve historical preflight and test milestones. Their old database-selection claims describe only those dated steps and do not supersede this current target/read-only statement.
## Current consolidated release status — 2026-11-22

This status supersedes earlier discovery and milestone snapshots in this report. Those dated findings remain as historical evidence and are not current-state claims.

- The UI catalog contains 98 unique DealerSystemSetting control IDs. The authenticated Development-only settings API has a fixed allowlist of 93 setting adapters; five image controls use the separate application-owned asset service.
- Five setting controls use application-owned extension keys because no legacy destination was established: `ParameterProductDetailNotLogin2`, `ParameterMenu2`, `WhatsAppOrderNotification`, `FavoriteIconPosition`, and `FavoriteIconVerticalPosition`. Their new-app save/readback/restoration was tested; old-portal reads remain unverified. `ParameterProductSearch` is hybrid: its low 16-bit word is stored in candidate `DealerSettings.ParameterProductSearch` (`smallint`), and its high word in an application-owned extension key.
- `ParameterPayment` is persisted as the same unsigned 16-bit bit pattern in SQL `smallint` using signed representation. `ParameterModules` option `1` belongs to `ddlParameterModules` / `DealerSettings.ParameterModules` (`int`); `ParameterAdmin` option `32` belongs to the separate `ddlParameterAdmin` / `DealerSettings.ParameterAdmin` (`bigint`).
- Legacy save-handler parity remains unverified for all 98 controls because the original server-side Web Forms handler is unavailable.
- Current normal API mode is read-only; isolated test-write flags are disabled. Final SQL checks were SELECT-only against `Store5_Settings_Test` and the unique `MemberCode=store5` row. No database writes or public deployment occurred during this final delivery pass.
- Historical test and backup evidence is retained in the SQL/test reports. Backend test suites and SQL integration groups were not rerun during this delivery pass; only frontend tests/build and backend Release build are current-run results.

## Scope and safety

Validation performed 2026-11-19 against the user-designated local analysis database only. All SQL statements were metadata/count/read operations. No customer setting values were printed, logged, exported to project files, or included here. No database record or schema was modified. The API was run in `SqlServer` mode with its process environment configured locally; no connection string or token was written to repository files.

For the safe-write milestone, a separate `Store5_Settings_Test` database was created from a `COPY_ONLY` backup of `Store5_Analysis` and restored under distinct MDF/LDF paths. Both databases were verified `ONLINE`; the temporary seed backup was deleted. No settings data or schema was changed in either database. This clone is isolated for future tests, but writes remain disabled until a verified-field allowlist exists.

## Connection and record scope

| Check | Result |
|---|---|
| LocalDB instance inventory | `MSSQLLocalDB` is installed and running. |
| Standard endpoint | `(localdb)\MSSQLLocalDB` connected using Windows Integrated Security. |
| Server reported by SQL Server | `Talha\LOCALDB#23773EBD` (matches the supplied instance). |
| Database | `Store5_Analysis`. |
| Target selection | `MemberCode='store5'` matched exactly 1 row. |
| NULL tenant row | 0 rows. |
| Other MemberCodes | Not selected or accessed. |
| `DealerSettings` columns | 222 columns. |
| Index/key metadata | No index with `index_id > 0` was returned for this table; no PK/unique index was found. |
| Foreign keys | No FK referencing this table as parent was returned. |

The API was configured with `B2BSTORE_MEMBER_CODE=store5`; it returned 98 catalog entries in SQL mode and marked 0 editable. Response values were intentionally suppressed from the validation output. No SQL write endpoint was called.

## Catalog-to-schema comparison

### ParameterModules and ParameterAdmin option reconciliation (2026-11-22)

The original browser DOM evidence assigns `Show Order Export Button` value `1` to `ctl00_PageContent_ddlParameterModules` and value `32` to the separate `ctl00_PageContent_ddlParameterAdmin`. The fixed catalog keeps these controls independent: `ddlParameterModules` maps to `DealerSettings.ParameterModules` (`int`) and has exactly option `1`; `ddlParameterAdmin` maps to `DealerSettings.ParameterAdmin` (`bigint`) and retains option `32`. The existing authenticated API integration runner explicitly tests option `1` for `ddlParameterModules`, verifies API and SQL readback, and restores its baseline. That successful field group was not rerun for this documentation/catalog correction. This resolves the option-ID ambiguity without establishing legacy save-handler parity.

The generated catalog contains 98 controls: 93 candidate mappings and 5 unresolved mappings. **No persistence mapping is promoted to verified by this schema comparison.** Column-name presence and SQL type are schema evidence only; they do not prove which control is persisted through the legacy save path.

| Result | Count |
|---|---:|
| Candidate column names present on `dbo.DealerSettings` | 93 |
| Candidate column names absent | 0 |
| Unresolved controls without a safe column candidate | 5 |
| Persistence mappings verified end-to-end | 0 |

Actual SQL types among the 93 present candidate columns were nullable: `int` 44, `nvarchar` 24, `bit` 10, `bigint` 7, `tinyint` 4, `smallint` 3, `decimal` 1. The selected type samples `ParameterDesign`, `ParameterAdmin`, `ParameterOrder`, and `ParameterMenu` are `bigint`; `SiteStatus` is `int`.

### Type discrepancy

`UseFiltering` (UI label **Filter Display**) uses four distinct positive power-of-two options, and the exact target row's candidate column is nullable `nvarchar` containing decimal numeric text. A fixed Development test-mode adapter now serializes the selected mask as decimal text, uses parameterized SQL and optimistic concurrency, and preserves unknown/unselected bits. Its authenticated API save/readback/restoration test passed in `Store5_Settings_Test` for `MemberCode=store5`, after an external checksum-verified backup. This confirms the new application's isolated adapter format only; legacy save serialization and portal behavior remain unknown. The candidate mapping is not promoted to verified.

`DiscountType` is nullable `nvarchar`; the supplied UI values are literal quoted codes `'A'`, `'P'`, and `'C'`, while the target row held a bare one-character code. A fixed test-mode codec maps those values to/from bare codes. Its authenticated API save/readback/restoration test passed against the isolated test database with a verified external backup. A declaration in `dbo._FrontProcCart` alone does not prove that the procedure reads `DealerSettings.DiscountType`; legacy consumer and save behavior remain unknown, so this remains a candidate mapping.

The normal API returns catalog candidate values read-only. The UI bitmask selection is supported only by the fixed isolated test adapter for `UseFiltering`; its selected flags serialize to decimal text and preserve unknown bits. Legacy encoding and portal behavior remain unknown, so the adapter does not establish legacy parity.

## Candidate and unresolved classifications

- **Candidate (93):** the candidate column name exists in `dbo.DealerSettings`. This confirms the column and its SQL metadata, not the control-to-column mapping or semantic equivalence. The application adapters for `UseFiltering` and `DiscountType` have been tested independently, but neither confirms the legacy save path.
- **Unresolved (5):** `ddlParameterProductDetailNotLogin2`, `ddlParameterMenu2`, `chkWhatsAppOrderNotification`, and the two favorite-icon position radio controls. No safe database column candidate was supplied for these controls.
- **Verified (0):** no control was proven through the original Web Forms save implementation or a controlled isolated write/readback. SQL writes remain disabled.

The full catalog and original labels/options remain in [`parameter-catalog.json`](parameter-catalog.json). The column comparison used the server-side candidate fields from that catalog and `sys.columns` metadata; no row values were queried for reporting.

## API validation

Command configuration (values were process-local and not persisted): `B2BSTORE_MODE=SqlServer`, `B2BSTORE_MEMBER_CODE=store5`, and a Windows Integrated Security connection to `(localdb)\MSSQLLocalDB` / `Store5_Analysis`.

At the time of the 2026-11-19 baseline, `GET /api/settings` returned HTTP 200, mode `SqlServer`, 98 items, 93 candidate and 5 unresolved; zero editable. That smoke check exercised only the read route. Later rollback-only and controlled persistent test validation is recorded below.

## Evidence still needed

1. Original `DealerSystemSetting` server-side save/persistence code or equivalent non-mutating trace for the 93 candidates.
2. Legacy compatibility evidence for the application-specific `UseFiltering` decimal-text encoding and `DiscountType` bare-code codec; neither was established by the isolated adapter tests.
3. Mapping/evidence for the five unresolved controls.
4. Any claim of update correctness requires a disposable isolated DB copy and an explicitly authorized transaction/rollback or test restore; this validation made no such write attempt.

## Targeted compatibility pass for remaining controls (2026-11-22)

Read-only checks were scoped to `Store5_Settings_Test` and the one `DealerSettings` row where `MemberCode='store5'`. The target row count remained one and the `ProductCompany` value remained NULL. Checks returned only type/match indicators; customer setting values were not printed.

- `ddlProductSearchParameter` has 21 original multi-select options from 1 through 1,048,576. The candidate column is nullable `smallint`, and the target row's current candidate value matches none of those options. A distinct `DealerSettings.ProductSearchType` `tinyint` exists, but no catalog control or SQL module consumer links that column to this control. Mapping remains unresolved; no write adapter was added.
- `ddlPaymentParameter` has 15 original multi-select options including 32,768. The candidate column is nullable `smallint`; the target row value matches an option, but not every original option is representable. No alternate mapped column or proven encoding was found. Mapping remains read-only.
- Targeted searches of the available SQL module extracts and current database module definitions found no exact consumers for `ParameterProductSearch`, `ParameterPayment`, `WhatsAppOrderNotification`, or the favorite-position settings. No unique destinations were found for `ParameterProductDetailNotLogin2`, `ParameterMenu2`, WhatsApp notification configuration, or favorite icon positions. The source inventory establishes their control shapes/options, not storage.
- The five legacy upload candidate columns (`Logo`, `Logo2`, `LogoLogin`, `Favicon`, `NoImageImage`) are nullable `nvarchar(200)`. For the authorized row, four values were non-NULL and path-like, one was NULL; none was URL-shaped or rooted. The source HTML has file inputs and delete links that call `/DealerSystemSetting.aspx/FileDelete` with filename/column-name arguments, but no legacy WebMethod implementation, actual static-file retrieval base, or upload save handler is available. The HTML provides no slot-specific preview image source. Existing generated-name application storage remains application-only; writing a relative reference to a legacy column would be speculative.
- The captured inventory/browser snapshot did not capture this control's option content, but the subsequently supplied original browser DOM evidence resolves it: `ddlParameterModules` option `1` and the separate `ddlParameterAdmin` option `32` are distinct. See the reconciliation section above; the option-1 adapter group already passed and was not repeated.

No new field met the evidence threshold for a fixed persistence adapter. Consequently this pass made no writes, created no database backups, and did not repeat prior successful integration groups.

## Rollback-only isolated SQL test (2026-11-20)

The implementation milestone exercised only `Store5_Settings_Test` using Windows Integrated Security and the configured `MemberCode=store5`. The test service rejects any other member, data source, or database before opening a connection; after connection it verifies `DB_NAME()` and the runtime SQL types (`SiteStatus=int`, `ParameterOrder=bigint`). It reads only these two columns for the single `store5` row during the write transaction.

The test-only allowlist contains the Site State control mapped to `SiteStatus` and Cart Parameter control mapped to `ParameterOrder`. Site status accepts only the supplied HTML values Active=`1` and Inactive=`0`. The ParameterOrder test toggles the supplied power-of-two option `1` (Request Contract Approval); all other set bits and all bits outside the inventory mask were asserted unchanged. Bigint API/test values remain decimal strings.

The rollback test executed both updates in one serializable transaction, read both values back inside that transaction, rolled it back in `finally`, and verified the original values after rollback with an additional read. A separate stale ParameterOrder expected value intentionally caused a concurrency conflict after the SiteStatus update; the service rolled back that earlier update and the original row values were verified unchanged. No database record remained modified after either path. `Store5_Analysis` was rejected by the pre-connection database guard; it was not connected to or queried.

Validation results: `dotnet build .\B2BStore.Settings.slnx --configuration Release --no-restore` passed with 0 warnings/errors; `dotnet test .\B2BStore.Settings.slnx --configuration Release --no-build --no-restore` passed all 21 tests when the local test connection and `B2BSTORE_SQL_WRITE_TEST_MODE=true` were supplied to the test process. The suite includes authorization/test-mode guards, invalid values, concurrency rollback, database isolation, read-after-write, null-safe bigint strings, and bit preservation. No setting values, credentials, tokens, or connection strings are included here.

The standard `GET /api/settings` and catalog routes remain read-only, and no general-purpose `PUT /api/settings` route is registered. The rollback-only route remains a distinct, unconditional rollback path. The persistent route is registered only in Development + SqlServer mode when the explicit process environment flag is true and requires bearer authentication. Both routes require the approved LocalDB endpoint, exact database name and hardcoded `store5` target, and are absent in Production. The two test-only fields remain candidate mappings; controlled test saves do not establish legacy save behavior or general user editability.

## Controlled persistent save test (2026-11-20)

Before any committed update, a copy-only checksum backup of `Store5_Settings_Test` was created outside the repository. `RESTORE VERIFYONLY ... WITH CHECKSUM` passed; `RESTORE HEADERONLY` confirmed the backup database name was exactly `Store5_Settings_Test`. Read-only preflight confirmed exactly one matching `store5` row, `SiteStatus` non-NULL and in the supported 0/1 range, and `ParameterOrder` non-NULL. No other `MemberCode` row was queried.

Using the browser UI at the local frontend, an authenticated request to the dedicated persistent-save endpoint changed only `SiteStatus` and the catalog-listed `ParameterOrder` option `1`. The UI then refreshed from SQL and displayed both saved values. The test changed an existing option from its prior state, and the write path updates only the selected flag; unrelated and unknown bits are preserved by the backend's bigint-safe mask operation. No raw customer setting values are recorded here.

The browser then saved the captured original status and toggled the same option back. A second explicit UI refresh confirmed the original status and exact original ParameterOrder decimal string matched the captured pre-test values. The tested flag also matched. The endpoint's in-transaction and post-commit read-backs succeeded for both saves. This sequence left the targeted values restored. Store5_Analysis was never connected to or modified. Other fields and MemberCode rows were not targets.

Additional final backend integration checks exercised a stale SiteStatus expected value (zero rows updated) and a stale ParameterOrder expected value after a temporary SiteStatus update inside the same transaction. The first case made no update; the second produced a concurrency conflict and rolled the earlier update back. Both post-test reads matched the prior row state. They did not commit any change.

This committed-write test is distinct from the rollback-only test above: rollback-only tests always roll back; the browser test deliberately committed two temporary states and then committed the original values back. The original source backup was not modified. No production write configuration was enabled, and no deployment occurred.

Validation for the preceding milestone (before the later expansion below): frontend tests 8/8 and frontend build passed; .NET 10 Release build passed with 0 warnings/errors; backend tests passed 26/26. The test flag was supplied only to the local Development API/test processes and is not present in user or machine environment variables.

## Earlier expanded isolated write allowlist (2026-11-20; superseded below)

The persistent authenticated Development test route now allowlists exactly five fields: SiteStatus, ParameterOrder, Layout, DontShowMenuAtDealers, and DeliveryDateShow. Three newly enabled controls were individually changed in the browser, refreshed from SQL, restored to their captured pre-test state, and refreshed again. All three changed-state readbacks and all three restoration checks passed. A backup created outside the repository before the committed operations passed `RESTORE VERIFYONLY WITH CHECKSUM`; its header identified `Store5_Settings_Test`. Only MemberCode `store5` was addressed. Store5_Analysis was not targeted. No row values are recorded here.

## Earlier full-evidence expansion — grouped isolated adapter tests (2026-11-20; superseded by latest status below)

Historical snapshot (2026-11-20): the fixed authenticated Development test allowlist then contained 17 controls total: the previously tested five plus `ParameterTheme`, `ProductTableStockType`, `ParameterDesign`, `ParameterStock`, `ParameterProductDetail`, `ParameterProductDetail2`, `ParameterProductDetailNotLogin`, `ExportProductList`, `ParameterAccount`, `MainProducts`, `ParameterMenu`, and `ParameterDynamicSearch`. Before adding these twelve fields, a read-only SQL preflight checked the actual `DealerSettings` column types/nullability and only NULL/non-NULL state for the unique `MemberCode='store5'` target; no setting values were returned to logs or reports.

Four serialized integration-test groups each created a separate `COPY_ONLY`, `CHECKSUM` backup outside the repository. `RESTORE VERIFYONLY WITH CHECKSUM` passed for each backup and the backup header identified `Store5_Settings_Test`. Each group then persisted temporary values through the fixed-field adapter, read them back from SQL, restored the captured original values, and verified the originals after restoration:

| Group | Fields | Test result | Backup verification | Restore check |
|---|---|---|---|---|
| Baseline | `SiteStatus`, `ParameterOrder`, `Layout`, `DontShowMenuAtDealers`, `DeliveryDateShow` | Passed | Passed | Passed |
| 1 | `ParameterTheme`, `ProductTableStockType`, `ParameterDesign` | Passed | Passed | Passed |
| 2 | `ParameterStock`, `ParameterProductDetail`, `ParameterProductDetail2`, `ParameterProductDetailNotLogin` | Passed | Passed | Passed |
| 3 | `ExportProductList`, `ParameterAccount`, `MainProducts`, `ParameterMenu`, `ParameterDynamicSearch` | Passed | Passed | Passed |

The bitmask groups verified that only selected listed bits changed; unrelated and unknown bits matched their original state after the temporary save. Values remained decimal strings across the API for bigint columns. No other `MemberCode` row was selected. A post-test scope check confirmed `Store5_Settings_Test` still has exactly one row for `MemberCode='store5'`. The test flag and SQL connection were removed from the process environment when tests completed. `Store5_Analysis` was not accessed or modified.

This verifies the new adapter's persistence and restoration against the isolated schema. It does **not** prove legacy Web Forms save behavior or portal effects. Normal application mode remains read-only for all settings; the 38-field allowlist is only active when Development, SQL mode, explicit `B2BSTORE_SQL_WRITE_TEST_MODE=true`, and bearer authorization are all present. The allowlist rejects every database except `Store5_Settings_Test` and hardcodes `MemberCode=store5`.

External pre-test backup files were created under `%LOCALAPPDATA%\B2BStore\backups`:

- `Store5_Settings_Test_baseline-five_20261120-135324-6797.bak`
- `Store5_Settings_Test_scalar-theme-stock-design_20261120-135326-1819.bak`
- `Store5_Settings_Test_product-detail-stock_20261120-135327-6605.bak`
- `Store5_Settings_Test_account-menu-search_20261120-135329-1518.bak`

Final automated validation on 2026-11-20: .NET 10 Release build passed with zero warnings/errors and all 33 backend tests passed. The suite disables parallel execution for SQL tests. The grouped test itself creates a new external backup and verifies its header before each of four groups. The write-test flag and connection variable were cleared after completion. Frontend validation passed 11/11 tests and the production build.

## New evidence bundle read-only follow-up (2026-11-21)

The new HTML/DOM/JS bundle was reviewed and targeted SQL extracts were checked only for remaining candidates; the full 130-module review was not repeated. No remaining field was promoted because FormManagement.js is not invoked by the exported settings page, the legacy C# handler is absent, and no end-to-end persisted encoding was established. A fresh authenticated API read returned SqlServer mode with 98 settings and zero editable; the catalog also contains 98. Read-only SQL preflight confirmed Store5_Settings_Test and exactly one MemberCode=store5 row. No write or data restoration was needed for this evidence review. The local API authorization diagnostic was corrected to send the raw nested token value; the private token was rotated and not recorded.
# Application API scalar persistence expansion (2026-11-21)

`MinCartCount` (`int`), `MinCartTotal` (`decimal(18,2)`), and `MaximumOrderQuantity` (`int`) were added to the fixed local Development test-mode allowlist and saved using authenticated `PUT /api/settings`. A read-only preflight confirmed the API was in SQL mode, write-test mode was explicitly enabled, all 98 settings loaded, and the private local config targeted only `Store5_Settings_Test` / `MemberCode=store5`. The only SQL row predicate used for verification was the exact `MemberCode='store5'` row, whose count was checked to equal one.

Before each group, a `COPY_ONLY,CHECKSUM` database backup was written under the external `%LOCALAPPDATA%\B2BStore\backups` directory and `RESTORE VERIFYONLY WITH CHECKSUM` passed. Each field was changed serially through the application API, read back from the API and compared to a read-only SQL query, restored to the captured original value, then verified through the API and SQL. All three fields passed save/read-back/restoration. The decimal was handled as an invariant decimal string with SQL precision 18 and scale 2. No setting values or backup paths are recorded in this report.

The first SQL verifier omitted `SET NOCOUNT ON`, which caused its output parser to report a false negative after the API had already confirmed the original value was restored. No additional field was written during that diagnostic. A read-only comparison then confirmed SQL matched the API value; the verifier was corrected and the remaining two fields passed direct SQL confirmation. A second external checksum-verified backup was made before those two tests. All three originals were verified restored at the end.

The application write route was then stopped; standard API mode was returned to read-only. `Store5_Analysis` was not connected to or modified. No other `MemberCode` row was accessed. The original 38 adapter tests were not repeated; only the three new fields received API-level integration tests in this milestone.

## Final local API and safety check (2026-11-21)

The final read used the private local configuration and authenticated Vite proxy. The normal API returned SQL Server mode and 98 settings; health returned `ok`, `testWritesEnabled` was false, and `PUT /api/settings` returned HTTP 405. No setting values were printed. The process environment did not contain `B2BSTORE_SQL_WRITE_TEST_MODE`. Previously captured and restored committed-test groups are documented above; no database writes were performed during this final check. The configured target remains the isolated `Store5_Settings_Test` database and `MemberCode=store5`; no other member row was queried. `Store5_Analysis` remained untouched.

## Service-only fields promoted to authenticated application API (2026-11-21)

All 36 previously service-only adapter fields passed serial authenticated `PUT /api/settings` save, API readback, direct SQL readback, and exact original-value restoration. The group used an external `COPY_ONLY,CHECKSUM` backup and the test verified it with `RESTORE VERIFYONLY WITH CHECKSUM`. The test/API selected only `MemberCode=store5` in `Store5_Settings_Test`. It did not repeat the prior 45 HTTP/browser-verified fields.

## Blocked nullable-candidate test (2026-11-21)

An exploratory `ProductCompany` (`ddlCommonSystem`) test confirmed an API save from the captured NULL baseline, but the restore request returned HTTP 400. The request DTO currently loses the distinction between an omitted scalar and explicit JSON `null`, so the captured NULL could not be restored through the application API. A subsequent authenticated read confirmed that this field no longer matches its captured NULL baseline. The external backup was checksum-verified before the test; it was not restored because doing so would replace unrelated member rows. The write-enabled API was stopped, no more SQL writes were attempted, and no further write testing should run until explicit exact-row recovery is authorized and the request contract can represent explicit NULL. No actual setting value is recorded here.

## Exact ProductCompany recovery and null-contract fix (2026-11-21)

Recovery was authorized and performed only after verifying the active database, configured LocalDB target, one `MemberCode=store5` row, `ProductCompany` schema nullability, and absence of enabled `DealerSettings` triggers. The prior captured baseline was confirmed as NULL. A fresh external `COPY_ONLY,CHECKSUM` backup passed `RESTORE VERIFYONLY WITH CHECKSUM`. A serializable transaction captured the current value, checked it had not changed since preflight, updated only `dbo.DealerSettings.ProductCompany` for `MemberCode='store5'`, required exactly one output row, verified NULL inside the transaction, and committed. A later SQL read again verified the exact database, one target row, and NULL. No other column was updated, no other MemberCode was queried, and `Store5_Analysis` was not accessed. The current API read returns all 98 settings and NULL for this field with `testWritesEnabled=false`; the write-test API remains stopped.

The DTO now uses a non-nullable `JsonElement` for `SettingChange.Value`, preserving `Undefined` (property omitted) versus `Null` (explicit JSON null). Omitted scalar changes are no-ops; explicit NULL is accepted only when catalog and actual `sys.columns.is_nullable` metadata both permit it. Empty strings, numeric zero, and Boolean false remain typed values. Regression tests cover these distinctions, non-nullable rejection, and missing expected-value rejection.

## ProductCompany isolated API save/readback/restoration (2026-11-22)

The exact `ddlCommonSystem` → `dbo.DealerSettings.ProductCompany` candidate is now in the fixed Development test allowlist only. Source HTML establishes the scalar options `1` and `0`; SQL metadata establishes nullable `tinyint`; this new adapter writes the selected option string as its scalar byte value without bit operations. The authenticated API integration test captured the target's NULL baseline, created an external checksum-verified backup, saved a source option through `PUT /api/settings`, verified API and SQL readback, and restored explicit JSON null through the API. Restoration was verified through both API and SQL. Integration test result: 1/1 passed. ProductCompany remains test-only; the original save handler is absent, so legacy serialization parity and business semantics remain unknown. The 81 prior persistence groups were not rerun. The temporary write-enabled API was stopped and the normal API remains read-only.

## Schedule scalar persistence (2026-11-22)

`ddlSchedule` has one HTML multiple-select option with value `1`. The candidate `dbo.DealerSettings.Schedule` is nullable `tinyint`. `[dbo].[_ServiceImportProduct]` reads `ISNULL(Schedule,0)` and compares it to `1` when deciding whether incoming product flags replace manually curated flags. A dedicated serializer maps the one option to scalar `1` or `0`; it performs no bitwise operation. A checksum-verified external backup preceded the authenticated API save. API and SQL readback matched; the captured original was restored and verified through API and SQL. Integration result: 1/1. No import/stock synchronization procedure was invoked, and the 81 earlier persistence groups were not rerun.

## Application-owned file persistence (2026-11-22)

Five image controls have a separate authenticated API route available only in explicit Development SQL write-test mode with the fixed `store5` / `Store5_Settings_Test` guard. Content and metadata are stored under LocalAppData with server-generated names. Uploads require PNG, JPEG, WebP, or ICO signatures and matching MIME/extension, with a 5 MiB limit. Replacement uses staged files and an atomic metadata move; deletion is scoped to generated filenames under the application-owned directory. The API test verified authentication, upload, metadata listing, content readback, and deletion. Unit tests cover replacement, path containment, type/signature/size checks, and unsupported slots. **No SQL update or `DealerSettings` path change occurred. These files are new-application-only and do not affect the legacy portal.**

The final 17-field set now consists of two SQL settings with authenticated round-trip tests (`ProductCompany`, `Schedule`), five application-only file slots, and ten draft-only settings (three serialization candidates, five unresolved mappings, and two input/business validation fields). The normal API remains read-only and the temporary test API was stopped.

## Final seven-control validation (2026-11-22)

Targeted read-only metadata confirmed the target column types used in this adapter: DealerSettings.ParameterPayment is smallint, ParameterProductSearch is smallint, and ParameterModules is int. The signed-smallint conversions were exercised with authenticated API updates and direct SQL readback. Product Search stores its high bits only in the application-owned DealerSystemSettingExtension key ParameterProductSearch.HighMask; this does not establish an old-portal reader.

No separate DealerSettings destinations were established for ParameterProductDetailNotLogin2, ParameterMenu2, WhatsAppOrderNotification, FavoriteIconPosition, or FavoriteIconVerticalPosition. They use fixed keys in the application-owned dbo.DealerSystemSettingExtension table. The table was applied by the explicit migration file to Store5_Settings_Test; it is not a legacy table and no old-portal compatibility is claimed. Its value strings preserve missing versus configured values. Five groups passed API/SQL readback and exact restoration. WhatsApp tests changed configuration only; no notification was sent.

Original browser DOM evidence resolves the earlier ParameterModules option discrepancy. `ddlParameterModules` uses option `1`; `ddlParameterAdmin` uses option `32` for the same label. They map to distinct columns and types: `DealerSettings.ParameterModules` (`int`) and `DealerSettings.ParameterAdmin` (`bigint`). The existing isolated int adapter round-trip for option `1` passed; legacy save-handler behavior remains unavailable.

Eight new groups were tested serially; each had a separate external COPY_ONLY, CHECKSUM backup and RESTORE VERIFYONLY WITH CHECKSUM. The final state was read-only validated: only the exact store5 row was selected, the Payment/Modules/Product Search column baselines were restored, ProductCompany remained NULL, and no extension row remained for store5. Other MemberCode rows were not accessed. No database write in this pass targeted Store5_Analysis or production.


