# MonsterASP hosted writable status

**Updated:** 2026-11-24

## Owner-observed deployment

The project owner published the ASP.NET Core .NET 10 application to MonsterASP and confirmed:

- The application started in Production under IIS.
- `GET /health/database` returned `{"status":"ready"}` for hosted SQL database `db82141` and `MemberCode=store5`.
- The admin-token UI connection succeeded and settings loaded.
- One Save operation reported a successful persistence/readback result.
- After publishing the performance optimization build, the owner reported refresh/save interactions were responsive.

This is a hosted smoke test, not proof that all 98 settings were validated in the hosted environment.

## Standard authenticated hosted mode

Required variables:

- `ASPNETCORE_ENVIRONMENT=Production`
- `B2BSTORE_SETTINGS_WRITE_MODE=HostedStore`
- `B2BSTORE_MEMBER_CODE=store5`
- `B2BSTORE_SQL_CONNECTION` — private connection string targeting `db82141`
- `B2BSTORE_ADMIN_TOKEN` — private administrator secret

The service retains the fixed hosted target guard and fixed allowlists. Never commit environment-variable values.

## Optional anonymous public-write mode

The source also contains an explicit temporary override:

- `B2BSTORE_PUBLIC_WRITE_ENABLED=true`

It is effective only with Production + SqlServer + `B2BSTORE_SETTINGS_WRITE_MODE=HostedStore`. When enabled, the frontend detects `HostedPublicWrite`, hides the token field and auto-connects. All visitors can modify the same allowed settings and application-owned extension/assets.

This deliberately bypasses the assignment's authentication requirement and is only suitable for a temporary synthetic evaluation. It is not appropriate for real customer data or production use. The current active state in the hosting panel must be checked; do not infer activation from source code alone.

## Database and compatibility limits

The extension table already existed in MonsterASP `db82141` when checked through WebMSSQL (`EXISTS`); no second migration was necessary. The legacy ASP.NET Web Forms save handler remains unavailable, so the compatibility of extension-backed values and exact old-portal save behaviour is not established. Five unresolved fields use app-owned extension keys; five image slots use app-owned asset storage.

## HTTPS and public endpoint

The owner tested the MonsterASP URL over HTTP. HTTPS activation was not confirmed. Do not send real credentials or customer data over the current HTTP deployment.
