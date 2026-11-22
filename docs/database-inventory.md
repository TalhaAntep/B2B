# Database inventory

## Current database status — 2026-11-22

The active read-only application target is `Store5_Settings_Test` on `(localdb)\MSSQLLocalDB`, scoped to the single configured `MemberCode=store5` row. The API returned 98 settings in SQL mode with all save and asset write capabilities off. The final direct SQL verification recorded in `sql-validation-report.md` was SELECT-only and confirmed `ProductCompany` remained NULL after its earlier restoration and no extension rows remained for the target. No write occurred during this release pass. The earlier `Store5_Analysis` observations below are historical read-only discovery evidence; that database is neither the active application target nor a permitted write target.
## Current consolidated release status — 2026-11-22

This status supersedes earlier discovery and milestone snapshots in this report. Those dated findings remain as historical evidence and are not current-state claims.

- The UI catalog contains 98 unique DealerSystemSetting control IDs. The authenticated Development-only settings API has a fixed allowlist of 93 setting adapters; five image controls use the separate application-owned asset service.
- Five setting controls use application-owned extension keys because no legacy destination was established: `ParameterProductDetailNotLogin2`, `ParameterMenu2`, `WhatsAppOrderNotification`, `FavoriteIconPosition`, and `FavoriteIconVerticalPosition`. Their new-app save/readback/restoration was tested; old-portal reads remain unverified. `ParameterProductSearch` is hybrid: its low 16-bit word is stored in candidate `DealerSettings.ParameterProductSearch` (`smallint`), and its high word in an application-owned extension key.
- `ParameterPayment` is persisted as the same unsigned 16-bit bit pattern in SQL `smallint` using signed representation. `ParameterModules` option `1` belongs to `ddlParameterModules` / `DealerSettings.ParameterModules` (`int`); `ParameterAdmin` option `32` belongs to the separate `ddlParameterAdmin` / `DealerSettings.ParameterAdmin` (`bigint`).
- Legacy save-handler parity remains unverified for all 98 controls because the original server-side Web Forms handler is unavailable.
- Current normal API mode is read-only; isolated test-write flags are disabled. Final SQL checks were SELECT-only against `Store5_Settings_Test` and the unique `MemberCode=store5` row. No database writes or public deployment occurred during this final delivery pass.
- Historical test and backup evidence is retained in the SQL/test reports. Backend test suites and SQL integration groups were not rerun during this delivery pass; only frontend tests/build and backend Release build are current-run results.

## Backup and local server

- `source/store5.bak`: present, 356,638,208 bytes. The file is excluded from Git by `.gitignore` and is not copied into build output.
- Read-only `RESTORE HEADERONLY` succeeded against the local SQL Server instance. Header names database `Store5`, backup SQL version `13.0.5026`, date 2026-01-11, backup size 356,629,504 bytes.
- Local SQL Server reported version `16.0.1220.3` (SQL Server 2022 Express).
- `DB_ID(N'Store5_Analysis')` returned `NULL` on the checked default and SQLEXPRESS instances.
- No restore, schema change, row change, or backup mutation was performed.

## SQL validation update

See [`sql-validation-report.md`](sql-validation-report.md). The local `(localdb)\MSSQLLocalDB` connection to `Store5_Analysis` succeeded using Windows Integrated Security. Metadata confirmed 222 `DealerSettings` columns and one row for `MemberCode='store5'`; no NULL MemberCode row exists. The 93 candidate column names are present, but a type discrepancy exists for `UseFiltering` (`nvarchar`), and save-path behavior remains unverified. No customer values were displayed and no SQL write was performed.

## Schema status

**Verified by SQL metadata:** `DealerSettings` has 222 columns; target `MemberCode='store5'` cardinality is one; NULL MemberCode cardinality is zero; no PK/unique index or parent FK was found in the metadata query. Candidate column types/counts and the `UseFiltering` discrepancy are documented in the SQL validation report.

**Still unverified:** save-path mapping, data semantics, defaults, triggers, stored module behavior and full control-to-column persistence. The original application code-behind is absent.

The implementation prompt states an earlier expected schema had 222 nullable columns and four rows with no primary/unique index or foreign key, but those claims were not reverified locally. Do not use them as verified facts.

## Isolated settings test database update (2026-11-20)

Before the controlled persistent save test, a `COPY_ONLY` + `CHECKSUM` backup of `Store5_Settings_Test` was created outside the repository. `RESTORE VERIFYONLY WITH CHECKSUM` passed and backup header metadata confirmed the exact database name. The controlled browser test temporarily updated only `SiteStatus` and `ParameterOrder` on the single `store5` row, then restored both values. UI refresh/readback matched the captured original values, including the exact ParameterOrder decimal string. A final read-only SQL check confirmed one store5 row and non-NULL supported values for those two columns. No other MemberCode row was queried. The source `store5.bak` and `Store5_Analysis` were not modified or used for writes.

## Safe next inspection

After the Project Manager confirms the isolated database name and target record context, inspect catalog metadata only first. The application itself queries `sys.columns` and then counts exact `MemberCode` matches. It does not select arbitrary `TOP 1`; zero or multiple matches are errors. It does not support `MemberCode IS NULL` fallback until the intended record rule is clarified.

Do not restore over an existing database or use `WITH REPLACE`. Use a newly named isolated database if a restore is explicitly authorized.

## Extension migration and final baseline (2026-11-22)

backend/B2BStore.Settings.Api/Migrations/001_DealerSystemSettingExtension.sql was explicitly applied to Store5_Settings_Test after an external checksum-verified backup. It creates only dbo.DealerSystemSettingExtension, with key (MemberCode, SettingKey) and columns for value, type and update timestamp. It has no foreign key to DealerSettings. This table is application-owned; legacy portal compatibility is unverified. No automatic production migration is configured.

Eight authenticated API integration groups were serialized, each preceded by a new external copy-only checksum backup verified with RESTORE VERIFYONLY WITH CHECKSUM. Each group read values through the API and direct SQL, then restored the captured state. Final verification confirmed Store5_Settings_Test, exactly one matching DealerSettings row for store5, documented scalar baselines, ProductCompany IS NULL, and zero extension rows for store5. The running API was returned to read-only mode. Store5_Analysis and other MemberCode rows were not modified or accessed during this pass.


