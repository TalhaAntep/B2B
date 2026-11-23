# AI usage report

**Updated:** 2026-11-23

## Tools used

- ChatGPT was used for project planning, requirement interpretation, technical review, documentation, test-result interpretation, and preparation of implementation/review instructions.
- OpenAI Codex was used as the coding agent for repository inspection, Vue/TypeScript and ASP.NET Core implementation, tests, debugging, Playwright evidence capture, and documentation changes.
- Exact hosted model/build identifiers for the Codex runs were not retained in the project logs; no unverified model version is claimed here.
- PowerShell, .NET SDK, Node/npm, Vitest, Playwright/Chromium, Git, GitHub Actions, and ffmpeg/ffprobe were used as normal engineering tools to run and inspect the work.

## Areas where AI assistance was used

- Repository and requirement inventory.
- HTML/DOM control catalogue and candidate SQL mapping analysis.
- Bitmask/value handling design and tests.
- Vue 3/TypeScript UI, API contracts, ASP.NET Core endpoints, SQL persistence adapters, and test implementation.
- Reverse-engineering evidence review and identification of unknown legacy save behaviour.
- Test/defect reporting, README, deployment notes, and demo-mode implementation.
- Creation of a static synthetic GitHub Pages mode.

## Verification and human review

AI suggestions were not treated as evidence by themselves. The user ran and shared the following local results:

- Local frontend `npm test`: **35/35 passed**; local production build passed.
- Public-demo frontend `npm test`: **36/36 passed**; `VITE_PUBLIC_DEMO=true` production build passed.
- .NET Release solution build: passed with **0 warnings and 0 errors**.
- A preceding compatibility pass recorded **56/56** filtered non-database backend tests and eight focused SQL API groups. These backend and SQL groups were not rerun during public-demo publication.
- The eight SQL groups were previously run against only `Store5_Settings_Test` / `MemberCode=store5`, with external checksum-verified backups, API/SQL readback, and restoration.
- The project owner opened the public GitHub Pages URL on 2026-11-23.

The original browser DOM evidence corrected the `ParameterModules` / `ParameterAdmin` option distinction: `ddlParameterModules` option `1` maps to `DealerSettings.ParameterModules` (`int`); the separate `ddlParameterAdmin` option `32` maps to `DealerSettings.ParameterAdmin` (`bigint`). This correction was based on the provided DOM evidence and adapter test, not on an AI-generated assumption.

## AI errors, limitations, and corrections

- Early technical drafts temporarily mixed the two option IDs for `ParameterModules` and `ParameterAdmin`; the mapping was corrected after the original live DOM evidence was reviewed.
- Some earlier release documents described the site as not deployed. After the public-demo workflow was fixed and the project owner confirmed the page opened, the deployment status was updated to distinguish the deployed static frontend from the not-deployed API/SQL backend.
- The original ASP.NET Web Forms server-side save handler was not available. AI assistance did not establish old-portal save parity or invent missing mappings.
- The initial GitHub Pages Actions run failed with a Pages API `404 Not Found`; the Pages source was configured and the workflow rerun successfully before the URL was opened.

## Data protection

- Production data was not used for writes.
- `Store5_Analysis` was not a permitted write target; controlled SQL tests were scoped to the isolated local `Store5_Settings_Test` / `MemberCode=store5` target.
- SQL backup files, local settings files, bearer tokens, connection strings, and credentials were excluded from the public repository and public Pages artifact.
- Public Pages uses synthetic unset values and does not connect to API or SQL Server.
- Screenshots/video were captured in synthetic Mock mode, not with real customer values.

## Remaining human review

A technical reviewer should still verify the 98-control inventory, five legacy mapping gaps, app-owned extension/asset decisions, and all `Not tested` portal effects. Public full-stack hosting requires a separate human security and provider approval; no security certification or external acceptance sign-off is claimed.
