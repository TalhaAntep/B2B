# Test and defect report

## Current release test summary — 2026-11-23

- **Public synthetic frontend:** `npm test` passed **36/36** (three test files); `VITE_PUBLIC_DEMO=true` + `npm run build` passed.
- **Local full-stack frontend:** `npm test` passed **35/35**; normal `npm run build` passed.
- **Backend Release build:** `dotnet build .\B2BStore.Settings.slnx -c Release --no-restore` passed with **0 warnings and 0 errors**.
- **Backend non-database tests:** **56/56** passed in the preceding 2026-11-22 compatibility pass; not rerun during public-demo publication.
- **SQL API integration:** eight focused groups passed in the preceding compatibility pass, each with external checksum-verified backup, API/SQL readback, and exact restoration; not rerun during public-demo publication.
- **Public demo smoke check:** the project owner opened `https://talhaantep.github.io/B2B/` successfully on 2026-11-23. The deployed Pages artifact is static frontend only, with synthetic unset values and no API/SQL backend.

## Current defect and limitation summary

- No defect is currently known in the deployed static page based on the reported smoke check and successful demo build/tests.
- The first GitHub Actions deployment attempt failed with `404 Not Found` because the Pages site had not yet been initialised/configured for Actions. Pages was configured and the workflow rerun; the URL was then opened successfully.
- The original ASP.NET Web Forms server-side save handler was unavailable. Legacy save-handler behaviour and old-portal parity remain **unverified for all 98 controls**.
- Portal before/after effects were not tested for all settings. The relevant rows remain marked `Not tested` / `Unverified` in the catalogue and mapping reports.
- Five settings use app-owned extension keys because legacy destinations were not established; five image controls use app-owned asset storage. These do not prove old-portal compatibility.
- The public demo is a read-only synthetic showcase; the ASP.NET API and SQL Server are not hosted publicly.

Detailed historical test commands, per-setting matrix, defects, integration evidence and test blockers are retained in [`test-bug-report.md`](test-bug-report.md). Do not interpret historical milestone counts as current reruns.

## Final compatibility pass - 2026-11-22

The eight newly enabled settings passed serialized authenticated HTTP save/readback, direct SQL verification and exact restoration against only Store5_Settings_Test / MemberCode=store5: ParameterPayment, ParameterProductSearch, ParameterModules, ParameterProductDetailNotLogin2, ParameterMenu2, WhatsAppOrderNotification, FavoriteIconPosition, and FavoriteIconVerticalPosition. A separate external checksum-verified backup was verified before each setting group. Payment and Modules use fixed DealerSettings columns; Product Search uses its smallint low word plus application-owned high word; five independent settings use application-owned extension rows. No notification or other external operation was run.

The seven new-control groups used their source controls and confirmed API contracts. Product Search tests exercised a high option and the SQL low word; the high portion remained in extension storage. ProductDetail and Menu2 bit tests verified nonselected bits remained unchanged. Boolean tests confirmed missing versus false and true. Horizontal and vertical enums were stored independently. The ParameterModules integration test used option `1`, which original browser DOM evidence assigns to `ddlParameterModules`; option `32` belongs to separate `ddlParameterAdmin` / `ParameterAdmin` (`bigint`). No legacy save-handler parity claim is made.

Build/test commands in this continuation:
- dotnet build B2BStore.Settings.slnx -c Release --no-restore: passed, 0 warnings, 0 errors.
- dotnet test B2BStore.Settings.slnx -c Release --no-build with the explicit exclusion filter for SQL-dependent integration classes: passed 56/56.
- Frontend npm test -- --run: passed 34/34.
- Frontend npm run build: passed.
- Eight new serialized SQL-backed HTTP groups: passed with per-group backup, readback and restoration evidence.
- Latest eight group backup files: RESTORE VERIFYONLY WITH CHECKSUM passed for all 8.

A separate unfiltered backend test invocation without the SQL integration environment produced failures in SQL-dependent test classes because required isolated SQL flags/API URL were absent. No writes occurred from that invocation. Those tests were not used as a passing result; the targeted eight groups above were run through the authorized isolated API. Prior successful persistence groups were not repeated.

Final read-only state: API returns 98 settings, capabilities report ReadOnly and canSaveSettings=false, a harmless PUT received HTTP 405, and SQL verified one exact target row, ProductCompany NULL, prior direct-column baselines restored and zero extension rows for store5. Both local services were left running with the API started without either write-mode flag. No Store5_Analysis write, production connection, or other MemberCode access occurred in this pass.
