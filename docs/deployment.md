# Deployment readiness and plan

## Current release state

- No public deployment has been performed and no public URL exists.
- The local API and frontend are available at `http://127.0.0.1:5080/` and `http://127.0.0.1:5173/`. The API was checked during this audit and reported `ReadOnly`; settings saves and asset management are disabled.
- The local API uses Windows Integrated Security and a local SQL Server LocalDB database. That database is not reachable from a public hosting service and must never be exposed or copied to a public host.
- The API's current CORS origins are localhost. The Vite development proxy reads the local admin token server-side; that development-only proxy is not part of the production static frontend.
- The API uses a single local bearer token rather than reviewed multi-user identity and authorization. Do not publish this token or put it in browser code.
- Public writes, test-write flags, real tenant data, the SQL backup, and application-owned upload data must remain disabled/excluded.

## Free-tier feasibility (checked 2026-11-22)

| Component | Candidate | Verified free limits / fit | Assessment |
|---|---|---|---|
| Vue static frontend | Cloudflare Pages Free | 500 builds/month, one concurrent build, 20-minute build timeout; deployments are retained 30 days, with the 100 most recent always retained. See [Cloudflare Pages limits](https://developers.cloudflare.com/pages/platform/limits/). | Suitable for the static Vue build if the API/auth architecture is made production-safe. |
| ASP.NET Core API | Azure Container Apps Consumption | Per subscription/month, the first 180,000 vCPU-seconds, 360,000 GiB-seconds and 2 million external HTTP requests are free. Other Azure resources/networking can add charges. See [Container Apps billing](https://learn.microsoft.com/azure/container-apps/billing). | A plausible low-traffic API host; validate container build, scaling, logs, networking and billing behavior. A free allowance is not a zero-cost guarantee. |
| SQL-compatible storage | Azure SQL Database Free Tier | Up to 10 General Purpose serverless databases per subscription; each has 100,000 vCore-seconds, 32 GB data and 32 GB backup storage per month. The free tier has no SLA; auto-pause-at-limit avoids automatic paid overage but makes the DB unavailable until the next month. Existing databases cannot be restored/converted into this offer. See [Azure SQL free tier](https://learn.microsoft.com/en-us/azure/azure-sql/database/free-offer) and [FAQ](https://learn.microsoft.com/en-us/azure/azure-sql/database/free-offer-faq). | This is actual Azure SQL Database, based on the SQL Server engine; it is a viable SQL-compatible candidate, not yet proven against this app's exact schema, migration, and query behavior. Use a new synthetic database only. |
| Alternative API host | Render Free Web Service | Sleeps after 15 minutes without inbound traffic; cold start may take about a minute; filesystem is ephemeral; free web services have no persistent disk. Render's free relational offering is PostgreSQL and expires after 30 days, so it is not a SQL Server replacement. See [Render free services](https://render.com/docs/free). | Not a suitable end-to-end storage choice for this SQL Server-targeted app. Could host an API only if it connects to another approved SQL Server-compatible service and its sleep/outbound limits are acceptable. |

PostgreSQL or SQLite must not be substituted for SQL Server without an explicitly approved data-access redesign and compatibility testing. No SQL Server service is configured in a hosting account today.

## Recommended deployment sequence

1. **Project Manager selects hosting and approves cost controls.** Confirm Azure account/subscription eligibility, region, billing method, monthly budgets/alerts, and Azure SQL free-limit behavior. Creating resources has not been authorized in this task.
2. **Prepare synthetic-only SQL.** Create a new Azure SQL Database using the free-tier offer and a newly authored synthetic seed. Never restore `source/store5.bak`, `Store5_Settings_Test`, or `Store5_Analysis`; the SQL free offer cannot be applied to a restored existing DB. Validate schema and API queries in a private staging environment.
3. **Close the authentication gap.** Replace the local single shared bearer-token pattern with an approved multi-user OIDC identity flow (for example, Microsoft Entra ID) and server-side authorization. Do not embed bearer credentials in the static bundle. Add a same-origin API/BFF route or an explicitly reviewed API token exchange; configure exact-origin CORS only if cross-origin calls remain.
4. **Make the hosting artifact.** Build the Vue app as a static site for Cloudflare Pages. Containerize the .NET 10 API for Azure Container Apps and bind to the host-provided port. Keep write routes unavailable in deployment; the public demo must be synthetic-data and read-only.
5. **Handle assets deliberately.** The current application-owned upload store uses local filesystem storage. Do not use it on an ephemeral container filesystem. Either disable uploads in the public demo or design and review durable object storage before enabling them; never claim old-portal compatibility for app-only assets.
6. **Stage and verify privately.** Configure secrets through provider secret stores/managed identity, TLS, logging redaction, request limits, health checks, backups, and free-usage monitoring. Run frontend/backend tests and read-only API acceptance checks against synthetic data. Confirm all response values are synthetic and that save routes are absent/disabled.
7. **Human release approval.** Review security, tenant boundaries, accessibility, labels, SQL compatibility, cost safeguards and the public demo content. Only then publish and add a URL after it is externally reachable and verified.

## Current blockers

- No hosting provider/account, Azure subscription, hosted SQL database, synthetic seed, or approved region is configured.
- Public authentication and production same-origin/API integration are not implemented or reviewed. Current local Vite token proxy cannot be copied into a static frontend deployment.
- Exact Azure SQL compatibility has not been tested; some mappings and legacy portal effects remain unresolved.
- No public URL exists. Local evidence is available as seven synthetic Mock-mode screenshots and MP4/WebM demo recordings in `docs/evidence/delivery/`; these do not represent a public deployment or live SQL customer data.
- The uploaded snapshot has no usable Git baseline commit: `HEAD` points to `refs/heads/master`, but the master ref and Git index are absent. Review the staged-file list and excluded private files before creating the first local commit.

## Safety requirements

- Never deploy `Store5_Analysis`, `Store5_Settings_Test`, another real dealer's data, `source/store5.bak`, credentials, tokens, or private config.
- Keep `B2BSTORE_SQL_WRITE_TEST_MODE` and `B2BSTORE_SETTINGS_WRITE_MODE` unset on hosted services. The hosted API must remain read-only.
- Keep account creation, paid resources, external deployment, and source publication subject to Project Manager approval.
- The Azure and Render limits above are provider documentation snapshots checked on 2026-11-22; re-check immediately before provisioning.

