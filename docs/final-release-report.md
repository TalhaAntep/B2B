# Final release report

Audit date: 2026-11-23. This report describes the latest repository snapshot and the final local delivery checks. No database write or public deployment was performed during the final delivery-capture pass.

## Delivery status

- **98/98 controls** are represented in the UI catalog and editor inventory.
- **93 settings API adapters** are available only in authenticated Development isolated-test mode and have prior isolated API save/readback/restoration evidence. Five image controls use a separate application-owned asset service; these are not part of the 93 SQL/settings adapters.
- Five of the 93 settings adapters use application-owned extension keys because their legacy destinations/readers are not established: `ParameterProductDetailNotLogin2`, `ParameterMenu2`, `WhatsAppOrderNotification`, `FavoriteIconPosition`, and `FavoriteIconVerticalPosition`. `ParameterProductSearch` uses a hybrid low SQL word plus extension-backed high word. These app-owned values do not establish old-portal compatibility.
- `ParameterPayment` preserves the unsigned 16-bit bit pattern in SQL `smallint`. Original browser DOM evidence confirms `ddlParameterModules` option `1` maps to `DealerSettings.ParameterModules` (`int`), while the separate `ddlParameterAdmin` option `32` maps to `DealerSettings.ParameterAdmin` (`bigint`).
- Legacy Web Forms save-handler parity and portal effects remain unverified for all 98 controls because the original server-side handler is unavailable.

## Final local checks

| Check | Result |
|---|---|
| Frontend tests | `npm test`: passed, **35/35** tests in two files. |
| Frontend production build | `npm run build`: passed (`vue-tsc -b` and Vite production build). |
| Backend Release build | `dotnet build B2BStore.Settings.slnx --configuration Release --no-restore`: passed, 0 warnings and 0 errors. |
| Backend non-database tests | **56/56 passed** in the 2026-11-22 compatibility pass. Not rerun during the final delivery-capture pass. |
| SQL integration groups | Eight focused groups passed in the 2026-11-22 compatibility pass, each with an external checksum-verified backup, API/SQL readback, and exact restoration. Not rerun during the final delivery-capture pass. |
| SQL/read-only verification | Final SQL checks in the local audit were SELECT-only. The SQL-backed API returned 98 settings in `SqlServer` mode with `ReadOnly`, `canSaveSettings=false`, `canManageApplicationAssets=false`, and the test-write flag false. |
| Database writes | None during the final delivery-capture pass. |
| Browser media | Seven Playwright screenshots plus a 10.36-second H.264 MP4 and original WebM were captured from the real Vue app in synthetic Mock mode. The mock values were unset; no SQL connection was used for the media. The MP4 was inspected with `ffprobe`; browser page errors were reported as zero. |

## Safety and compatibility limits

Normal mode is read-only. The Development test-write flags remain disabled. Final SQL checks were limited to the configured `Store5_Settings_Test` target and the exact `MemberCode=store5` row; no other member row was selected. `Store5_Analysis` was not modified. Application-owned extension values and image assets are not claimed to be compatible with or consumed by the old portal.

The original save handler, old-portal reads for extension storage, and legacy asset retrieval convention remain unavailable or unverified. Save-validation feedback was not captured because read-only mode has no Save action; the screenshots show disabled-save and local-draft behavior. See `docs/evidence/delivery/README.md`.

## Git and deployment

The uploaded snapshot contains a `.git` directory, but `HEAD` points to `refs/heads/master` while that branch ref and the Git index are absent. No usable baseline commit is present in this snapshot; the final report in the earlier archive incorrectly stated that a commit had been prepared. No remote push occurred. Review the exact staged file list and secret exclusions before creating the first local commit.

No public deployment exists and no public URL is claimed. Blockers include an approved hosting subscription, a new synthetic Azure SQL database and compatibility tests, production authentication with controlled per-user/demo sessions, restricted CORS, HTTPS, rate limiting, secure secret configuration, durable or disabled asset storage, and release approval. See `docs/deployment.md`.
