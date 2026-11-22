# Test and defect report

## Current consolidated release status — 2026-11-22

This status supersedes earlier discovery and milestone snapshots in this report. Those dated findings remain as historical evidence and are not current-state claims.

- The UI catalog contains 98 unique DealerSystemSetting control IDs. The authenticated Development-only settings API has a fixed allowlist of 93 setting adapters; five image controls use the separate application-owned asset service.
- Five setting controls use application-owned extension keys because no legacy destination was established: `ParameterProductDetailNotLogin2`, `ParameterMenu2`, `WhatsAppOrderNotification`, `FavoriteIconPosition`, and `FavoriteIconVerticalPosition`. Their new-app save/readback/restoration was tested; old-portal reads remain unverified. `ParameterProductSearch` is hybrid: its low 16-bit word is stored in candidate `DealerSettings.ParameterProductSearch` (`smallint`), and its high word in an application-owned extension key.
- `ParameterPayment` is persisted as the same unsigned 16-bit bit pattern in SQL `smallint` using signed representation. `ParameterModules` option `1` belongs to `ddlParameterModules` / `DealerSettings.ParameterModules` (`int`); `ParameterAdmin` option `32` belongs to the separate `ddlParameterAdmin` / `DealerSettings.ParameterAdmin` (`bigint`).
- Legacy save-handler parity remains unverified for all 98 controls because the original server-side Web Forms handler is unavailable.
- Current normal API mode is read-only; isolated test-write flags are disabled. Final SQL checks were SELECT-only against `Store5_Settings_Test` and the unique `MemberCode=store5` row. No database writes or public deployment occurred during this final delivery pass.
- Historical test and backup evidence is retained in the SQL/test reports. Backend test suites and SQL integration groups were not rerun during this delivery pass; only frontend tests/build and backend Release build are current-run results.

See [`test-bug-report.md`](test-bug-report.md) for the detailed automated test matrix, blockers, and legacy behavior limitations. The final delivery-capture pass passed frontend tests **35/35**, the production build, and the .NET 10 Release solution build with zero warnings/errors. The **56/56** filtered non-database backend suite and eight focused SQL API groups passed in the preceding compatibility pass; neither was rerun during the final delivery-capture pass. The final API/SQL checks were read-only, and no database writes occurred in that pass. Legacy persistence behavior remains unverified for all controls.

## Final compatibility pass - 2026-11-22

The eight newly enabled settings passed serialized authenticated HTTP save/readback, direct SQL verification and exact restoration against only Store5_Settings_Test / MemberCode=store5: ParameterPayment, ParameterProductSearch, ParameterModules, ParameterProductDetailNotLogin2, ParameterMenu2, WhatsAppOrderNotification, FavoriteIconPosition, and FavoriteIconVerticalPosition. A separate external checksum-verified backup was verified before each setting group. Payment and Modules use fixed DealerSettings columns; Product Search uses its smallint low word plus application-owned high word; five independent settings use application-owned extension rows. No notification or other external operation was run.

The seven new-control groups used their source controls and confirmed API contracts. Product Search tests exercised a high option and the SQL low word; the high portion remained in extension storage. ProductDetail and Menu2 bit tests verified nonselected bits remained unchanged. Boolean tests confirmed missing versus false and true. Horizontal and vertical enums were stored independently. The ParameterModules integration test used option `1`, which original browser DOM evidence assigns to `ddlParameterModules`; option `32` belongs to separate `ddlParameterAdmin` / `ParameterAdmin` (`bigint`). No legacy save-handler parity claim is made.

Build/test commands in this continuation:
- dotnet build B2BStore.Settings.slnx -c Release --no-restore: passed, 0 warnings, 0 errors.
- dotnet test B2BStore.Settings.slnx -c Release --no-build with the explicit exclusion filter for SQL-dependent integration classes: passed 56/56.
- Frontend npm test -- --run: passed 34/34 at the compatibility-pass checkpoint; the subsequent final delivery-capture run passed 35/35 after adding the Mock-versus-SQL connection-notice regression test.
- Frontend npm run build: passed.
- Eight new serialized SQL-backed HTTP groups: passed with per-group backup, readback and restoration evidence.
- Latest eight group backup files: RESTORE VERIFYONLY WITH CHECKSUM passed for all 8.

A separate unfiltered backend test invocation without the SQL integration environment produced failures in SQL-dependent test classes because required isolated SQL flags/API URL were absent. No writes occurred from that invocation. Those tests were not used as a passing result; the targeted eight groups above were run through the authorized isolated API. Prior successful persistence groups were not repeated.

Final read-only state: API returns 98 settings, capabilities report ReadOnly and canSaveSettings=false, a harmless PUT received HTTP 405, and SQL verified one exact target row, ProductCompany NULL, prior direct-column baselines restored and zero extension rows for store5. Both local services were left running with the API started without either write-mode flag. No Store5_Analysis write, production connection, or other MemberCode access occurred in this pass.


