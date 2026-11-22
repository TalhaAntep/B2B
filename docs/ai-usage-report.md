# AI usage report

## Scope of AI assistance

Codex (GPT-6-based coding agent; exact hosted model revision is not exposed in the project environment) assisted with repository inventory, interpretation of the assignment requirements, legacy form/DOM and SQL evidence review, catalog and mapping work, Vue 3/TypeScript and ASP.NET Core implementation, test authoring, debugging, and documentation. It also helped prepare isolated SQL validation procedures and reviewed their safety constraints.

The original ASP.NET Web Forms save handler was not available. AI assistance therefore did not establish exact legacy save-handler parity or infer undocumented behavior from names alone. The ParameterModules option assignment was corrected from the original browser DOM evidence: option `1` belongs to `ddlParameterModules` / `DealerSettings.ParameterModules` (`int`), while option `32` belongs to `ddlParameterAdmin` / `DealerSettings.ParameterAdmin` (`bigint`).

## Verification performed

- Final delivery-capture checks: `npm test` passed **35/35** frontend tests; `npm run build` passed; `dotnet build B2BStore.Settings.slnx --configuration Release --no-restore` passed with 0 warnings and 0 errors.
- Earlier backend test results are documented in `test-and-defect-report.md` and `test-bug-report.md`. The backend test suite was not rerun during this final audit; no new backend test result is claimed here.
- Prior controlled SQL persistence tests were run serially against only `Store5_Settings_Test` / `MemberCode=store5`, using external `COPY_ONLY,CHECKSUM` backups, `RESTORE VERIFYONLY WITH CHECKSUM`, API/SQL readback, and restoration checks. Successful groups were not repeated during this delivery audit.
- Current read-only checks used the authenticated local API and SELECT-only SQL queries. The API reported `ReadOnly`, `canSaveSettings=false`, `canManageApplicationAssets=false`, 98 settings loaded, and `testWritesEnabled=false`. SQL checks confirmed the exact approved database, one target row, zero extension rows for `store5`, and `ProductCompany IS NULL`. No database writes were performed in this audit.
- Secret values and setting values were suppressed from command output and are not recorded in this report.

AI-generated code and explanations are not treated as proof by themselves. Runtime/API and SQL statements above refer to command results or earlier test evidence recorded in the linked reports. No external review, security certification, or human acceptance sign-off is claimed.

## Human review and decisions still required

- A project owner/reviewer must accept the mapping and evidence matrix, including the five legacy mapping gaps and the areas where new-application persistence does not prove that the old portal reads the value.
- A security reviewer must approve public authentication, tenant isolation, CORS, secret handling, and the final hosting configuration before any public endpoint is exposed.
- The Project Manager must select or provide the approved hosting account/subscription, synthetic demo data source, region, billing safeguards, and publication approval.
- Product/operations reviewers must approve user-facing Turkish terminology and decide which legacy portal effects require manual acceptance testing.

## Remaining limitations

- Exact legacy save behavior and portal effects remain unverified where the original code-behind or authorized portal tests are unavailable.
- No public deployment URL is verified. Seven screenshots and a 10.36-second H.264 MP4 plus the source WebM are present in `docs/evidence/delivery/`. They were captured from the actual Vue app in local synthetic Mock mode with unset values; no SQL connection was used for the media. The video is evidence of the local read-only/draft UI, not a SQL-backed customer-data demo.
- Free hosting quotas do not make the complete application cost-free: supporting resources, network, logging, storage, and overages may incur charges. Azure SQL free tier is a genuine SQL Server-compatible option, but it has monthly limits and is not a drop-in guarantee for every SQL Server feature used by the application.

