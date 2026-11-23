# Submission checklist — B2B STORE Technical Settings

**Updated:** 2026-11-23

## Links to include in the submission email/form

- **Source code repository:** https://github.com/TalhaAntep/B2B
- **Public synthetic UI demo:** https://talhaantep.github.io/B2B/

Important: the public URL is a static frontend showcase with synthetic unset values. It is not a live SQL-backed administration service. The ASP.NET Core API and SQL Server implementation are run locally; this distinction is documented in `docs/deployment.md`.

## The four required deliverables from the assignment

### A. Technical Analysis Report

Attach/include:

1. `docs/technical-analysis-report.md` (also available as `docs/technical-analysis.md`).
2. `docs/parameter-catalog.md` and `docs/parameter-catalog.json` — 98-control catalogue.
3. `docs/mapping-audit.md` and `docs/mapping-audit.json` — field-by-field evidence/mapping status.
4. `docs/setting-editability-matrix.md` — editable/read-only and persistence evidence.
5. `docs/database-inventory.md` and `docs/sql-validation-report.md` — SQL schema/target evidence.
6. `docs/legacy-persistence-reverse-engineering.md` and `docs/risks-and-decisions.md` — save-path investigation and unresolved items.

### B. Test and Defect Report

Attach/include:

1. `docs/test-and-defect-report.md` — concise current summary.
2. `docs/test-bug-report.md` — detailed historical test matrix, failures, testable/untestable areas and limitations.
3. `docs/sql-validation-report.md` — isolated SQL/API verification and restoration evidence.
4. `docs/evidence/delivery/README.md`, seven PNG screenshots, and `b2b-store-settings-demo.mp4` (the WebM source may be included as an additional artifact).

The current frontend results are 35/35 local and 36/36 public-demo tests; the public demo build passed. The backend Release build passed with 0 warnings/errors. The prior 56/56 non-database backend tests and eight focused SQL groups passed in the earlier compatibility pass and were not rerun during public publication. Portal before/after behaviour was not established for every control.

### C. Working Software

Provide:

1. Source repository link above (the repository contains the Vue frontend, ASP.NET Core API, SQL integration code, tests, scripts and docs).
2. Public synthetic demo link above.
3. `README.md` for local setup/run commands and safety boundaries.
4. `docs/architecture.md`, `docs/deployment.md`, and `docs/demo-script.md` for architecture and demonstration.

State explicitly that the public Pages site deploys only the frontend; the ASP.NET Core API and SQL Server are not hosted publicly. Do not present the static demo as the full-stack service.

### D. AI Usage Summary

Attach/include `docs/ai-usage-report.md`.

## Suggested compact hand-in set

If the evaluator prefers fewer attachments, send:

- `B2B_STORE_Submission_Package.zip` (reports, screenshots and demo video).
- The source GitHub URL.
- The public demo URL.

The source code need not be duplicated in the report ZIP because it is in the public GitHub repository.

## Do NOT submit or publish

- `source/store5.bak` or any SQL backup/database files.
- `store5.rar` or other original customer/source archives unless the project owner explicitly authorises transfer to the evaluator via a secure channel.
- `%LOCALAPPDATA%\B2BStore\settings.local.json`, `.env`, appsettings secret files, bearer tokens, passwords, connection strings, private keys or other credentials.
- Real customer/dealer values or unredacted screenshots.
- The raw `docs/evidence/legacy/` bundle unless individually reviewed and approved for sharing. The public report package uses only the approved synthetic delivery evidence.

## Acceptance gaps to disclose

- Full-stack public deployment is **not complete**; only the synthetic frontend is on GitHub Pages.
- The original Web Forms save handler is unavailable; old-portal save parity remains unverified for all 98 controls.
- Five setting values use application-owned extension storage and five image slots use app-owned asset storage; no old-portal reader is demonstrated for these.
- The current MP4 is 10.36 seconds and demonstrates the synthetic read-only UI only, not a hosted SQL save workflow.
