# MonsterASP hosted smoke-test record

**Observed by project owner:** 2026-11-23

## Environment

- Website: `http://b2b-store-settings.runasp.net/`
- Hosting: MonsterASP FreeSite
- API: ASP.NET Core .NET 10, Production
- SQL target configured server-side: database `db82141`, `MemberCode=store5`
- No credentials, connection strings, or tokens are recorded here.

## Observed results

| Check | Result | Evidence / qualification |
|---|---|---|
| IIS/.NET startup | Passed | ASP.NET Core debug log showed `Application started` and Production environment. |
| SQL database readiness | Passed | `GET /health/database` returned `\{\"status\":\"ready\"\}` over HTTP. |
| Extension table presence | Passed | WebMSSQL query returned `EXISTS` for `dbo.DealerSystemSettingExtension`; no second migration was run. |
| Admin UI connection | Passed | UI status changed to connected and settings loaded after the owner entered the private admin token. |
| Settings Save/readback | Passed for one smoke test | UI displayed its successful save/readback confirmation for the tested delivery-date setting. Not a full 98-setting test. |
| Performance update | Owner-reported improvement | After publishing the performance source update, owner reported refresh/save interactions became responsive. |
| Public no-token write mode | Not independently confirmed | Source supports `B2BSTORE_PUBLIC_WRITE_ENABLED=true`; verify `/api/settings/capabilities` returns `environment: HostedPublicWrite` before claiming this mode is live. |
| HTTPS | Not confirmed | Owner tested HTTP. |

## Performance change

The optimization source combines target-row uniqueness validation with settings retrieval in a single SQL statement, uses the same combined read for save verification, and consumes committed values already returned by the API instead of reloading the full catalogue repeatedly. See `PERFORMANCE-OPTIMIZATION-NOTES.md`. The code change reduces avoidable round trips; no controlled latency benchmark was captured.

## Scope limits

- The hosted database is a synthetic evaluation target, not the legacy production database.
- Only one end-to-end hosted Save smoke test was observed. The 98-control hosted acceptance suite was not run.
- Old Web Forms save-handler parity remains unknown.
- Anonymous public-write mode, when enabled, allows every visitor to change shared settings. Use only with synthetic evaluation data and temporarily.
- HTTPS was not confirmed; do not send real credentials or customer information to this HTTP endpoint.
