# Deployment plan — public static demo

## Current state

- GitHub repository: `https://github.com/TalhaAntep/B2B` (Public).
- Initial source commit `263bb0b` is on `main` and was pushed to `origin/main`. A local `public-demo` branch has been created for the static demo; it must be pushed after its changes are reviewed and committed.
- No public website URL is live yet. The expected GitHub Pages project URL after a successful deployment is `https://talhaantep.github.io/B2B/`; verify it in a browser before describing it as deployed.
- The public demo uses only the Vue static frontend and the checked-in parameter catalog. It creates 98 synthetic, unset values in the browser. It does not call the ASP.NET API or SQL Server.
- `Store5_Settings_Test`, `Store5_Analysis`, `source/store5.bak`, local configuration, bearer tokens, and all real dealer values must remain excluded from the Pages artifact.

## Public demo architecture

| Component | Deployment | Purpose |
|---|---|---|
| Vue 3 + TypeScript | GitHub Pages | Static project showcase built from `frontend/`. |
| Synthetic data | Browser memory | `createPublicDemoItems()` maps all 98 catalog controls to `value: null`, `editable: false`. |
| ASP.NET Core API | Not deployed | Local-only service; no API host is required for the public demo. |
| SQL Server | Not deployed | No database connection or database credentials exist in the public demo build. |
| Settings writes | Disabled | UI changes remain browser-only drafts; the Save action is unavailable. |
| Asset persistence | Disabled | File selection is only a local preview; no upload/delete request is made. |

The Vite build uses `VITE_PUBLIC_DEMO=true` to select the static demo branch and the repository subpath base `/B2B/`. `frontend/src/App.vue` returns synthetic data before reaching API calls. The public branch does not require any GitHub repository secrets.

## Publish steps

1. Review the public repository for secrets or unintended data before every push. Keep it Public only if all committed code and evidence are approved for public viewing.
2. Work on `public-demo`; do not change `main` for this static demo task.
3. Run frontend tests and both normal and `VITE_PUBLIC_DEMO=true` production builds locally. Confirm the public demo has no API requests and that the Save/upload actions are unavailable.
4. Commit the reviewed changes and push `public-demo` to `origin`.
5. In GitHub repository Settings → Pages, choose **GitHub Actions** as the deployment source.
6. Open Actions and verify `Deploy public synthetic demo` passes.
7. Open `https://talhaantep.github.io/B2B/` in a private browser window. Verify all 98 settings load, EN/TR works, search works, edits remain drafts, no Save/upload action is exposed, and the browser network panel shows no `/api` requests.
8. Only after those checks pass, record the public URL in the README and final delivery report.

## Security and data boundaries

- The GitHub Pages artifact contains static frontend assets only. It does not contain the ASP.NET API binaries, local appsettings, `.env` files, connection strings, bearer tokens, `source/store5.bak`, SQL data files, or SQL backups.
- The static public demo has no connection to `Store5_Settings_Test` or `Store5_Analysis`. Never restore a database backup into GitHub Pages or commit real customer values.
- `canSaveSettings` and `canManageApplicationAssets` remain `false` in public demo mode. Browser drafts are not sent to a server and disappear when the page is reset/reloaded.
- The GitHub Actions workflow grants only `contents: read`, `pages: write`, and `id-token: write`; it has no application secrets.
- Public source publication is intentional. Never commit tokens, private keys, personal data, customer data, or the excluded legacy-evidence bundle. If a secret is ever committed, making the repository private or deleting the latest file is not enough: revoke/rotate the secret and consider the full Git history exposed.

## Separate future phase: full-stack hosted application

The GitHub Pages demo does **not** satisfy a live SQL-backed production deployment. A separate full-stack project would require a SQL Server-compatible managed database populated only with synthetic data, a hosted ASP.NET Core API, per-user identity and authorization (not the current single local admin token), exact-origin access controls, HTTPS, secret management, rate limits, logging redaction, durable asset storage or disabled uploads, and a cost/availability review. No Azure subscription is available in the currently checked university directory; Azure is not mandatory for the static demo. Do not provision paid services or deploy any database as part of this Pages task.

## Current blockers

- Static demo code and workflow are prepared on the local `public-demo` branch but have not yet been committed/pushed in the current stage.
- GitHub Pages has not yet been enabled with the GitHub Actions source.
- No public site URL has been verified.
- Full-stack ASP.NET/SQL hosting remains out of scope and has not been configured.

## Current checks

The existing local delivery checks remain: frontend `npm test` **35/35**, frontend production build passed, and .NET Release solution build passed with zero warnings and zero errors. After the static demo changes are applied, repeat frontend tests and build in the user's Windows environment with `VITE_PUBLIC_DEMO=true`; do not rerun SQL integration tests or enable write flags for this static demo.
