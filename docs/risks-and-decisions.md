# Risks and decisions

## Current consolidated release status — 2026-11-22

This status supersedes earlier discovery and milestone snapshots in this report. Those dated findings remain as historical evidence and are not current-state claims.

- The UI catalog contains 98 unique DealerSystemSetting control IDs. The authenticated Development-only settings API has a fixed allowlist of 93 setting adapters; five image controls use the separate application-owned asset service.
- Five setting controls use application-owned extension keys because no legacy destination was established: `ParameterProductDetailNotLogin2`, `ParameterMenu2`, `WhatsAppOrderNotification`, `FavoriteIconPosition`, and `FavoriteIconVerticalPosition`. Their new-app save/readback/restoration was tested; old-portal reads remain unverified. `ParameterProductSearch` is hybrid: its low 16-bit word is stored in candidate `DealerSettings.ParameterProductSearch` (`smallint`), and its high word in an application-owned extension key.
- `ParameterPayment` is persisted as the same unsigned 16-bit bit pattern in SQL `smallint` using signed representation. `ParameterModules` option `1` belongs to `ddlParameterModules` / `DealerSettings.ParameterModules` (`int`); `ParameterAdmin` option `32` belongs to the separate `ddlParameterAdmin` / `DealerSettings.ParameterAdmin` (`bigint`).
- Legacy save-handler parity remains unverified for all 98 controls because the original server-side Web Forms handler is unavailable.
- Current normal API mode is read-only; isolated test-write flags are disabled. Final SQL checks were SELECT-only against `Store5_Settings_Test` and the unique `MemberCode=store5` row. No database writes or public deployment occurred during this final delivery pass.
- Historical test and backup evidence is retained in the SQL/test reports. Backend test suites and SQL integration groups were not rerun during this delivery pass; only frontend tests/build and backend Release build are current-run results.

| Risk / question | Evidence | Decision for this implementation |
|---|---|---|
| Original code-behind and save path absent | Only HTML-derived inventory/mapping supplied | The normal and Production API stay read-only. A fixed 93-setting allowlist exists only in authenticated local Development test mode; this proves the new adapter round-trip, not legacy portal persistence. |
| Original database must remain protected | `Store5_Settings_Test` is a separate local clone; Store5_Analysis is forbidden for writes | Both write test paths require exact LocalDB endpoint/database/member. Persistent tests require an external verified backup and restoration; Store5_Analysis remains rejected. |
| `DealerSettings` lacks known key behavior | Prompt gives earlier unverified cardinality/key notes | Require exact configured `MemberCode` and exactly one match; refuse null/ambiguous matches. |
| Multi-select option semantics partial | HTML options are powers of two; prompt contains partial SQL behavior notes | Preserve option values as strings, compute in backend, document behavior only where the brief cites SQL evidence. |
| Settings include potentially sensitive values | Inventory includes scripts, address, phone, recaptcha and other text fields | API requires bearer token; logs and docs do not include field values. |
| Legacy upload persistence unknown | Five image controls in inventory; no original upload handler or stable legacy path contract | A guarded Development-only application-owned store is implemented with generated names, metadata, signature/MIME/extension checks, size limits, replacement, and delete. It is explicitly not legacy-compatible and does not write DealerSettings. PM clarification is still needed before any legacy portal integration. |
| Runtime security and deployment context unknown | No original auth or hosting setup | Local API token; production requires explicit secrets; no deployment performed. |
| Legacy save behavior unknown | Code-behind absent; current mappings are candidates | Rollback-only and controlled persistent tests cover the new API mechanics for `SiteStatus` and `ParameterOrder`; no legacy save behavior or general editability claim is made. |

## Remaining decisions / evidence required

The verified environment and target are Store5_Settings_Test / store5; no additional database details are needed for this milestone. Remaining compatibility evidence is the original ASP.NET save handler or a controlled, authorized old-portal consumer test, and evidence that the old portal reads the application-owned extension fields. Original browser DOM evidence resolves the distinct ParameterModules (`1`, int column) and ParameterAdmin (`32`, bigint column) option IDs. Five controls still have no legacy database destination, but their new-application extension persistence is implemented and tested. Do not claim legacy parity while those items remain unresolved.

## Final compatibility decisions (2026-11-22)

- 93 of 98 controls have fixed authenticated Development-only settings API persistence adapters. The five controls with no legacy destination use application-owned extension storage. The other five controls use the separate app-owned asset API. Legacy save-handler parity remains unknown for every control.
- Five setting extension keys and the Product Search high word reside in the application-owned dbo.DealerSystemSettingExtension table. They are not part of the legacy schema, and no evidence shows that the old portal reads them. Treat them as new-application-only.
- ParameterPayment uses the exact unsigned/signed 16-bit smallint bit-pattern conversion. ParameterProductSearch stores a low word in its candidate smallint and a high word under a fixed app-owned key. Both passed serialized API/SQL readback and exact restoration.
- ParameterModules option `1` passed the fixed int adapter round-trip and is assigned to that control by original browser DOM evidence. Option `32` for the same label belongs to the separate ParameterAdmin control and bigint column. Legacy save-handler parity remains unknown.
- The extension migration is explicit and was applied only to Store5_Settings_Test. No automatic Production migration exists. The normal API was restarted without write flags and remains read-only.

