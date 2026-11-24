# Technical interview notes — B2B STORE Technical Settings

These notes help explain the implementation accurately. They are not a substitute for understanding the code or the unresolved evidence.

## 1. Scope and architecture

- The assignment is a focused rebuild of the existing `DealerSystemSetting` technical-settings screen, not a rewrite of the full B2B STORE platform.
- Frontend: Vue 3 + TypeScript + Vite. The 98-control catalogue drives categories, labels, option values, typed editors and search.
- Backend: ASP.NET Core .NET 10 Web API. It exposes settings/catalog/capability/health endpoints and validates updates against a fixed catalogue/adapter allowlist.
- Persistence: Microsoft SQL Server through `Microsoft.Data.SqlClient`; row selection requires exactly one configured `MemberCode` row.

## 2. Why there are separate operating modes

- Public GitHub Pages mode is synthetic and browser-only. It contains no connection string/token, makes no API/SQL requests and cannot persist settings.
- Normal API mode is read-only by default.
- Local write verification is opt-in and scoped to Development + SQL Server + the isolated `Store5_Settings_Test` LocalDB database and `store5` member.
- `HostedStore` is a separate, explicit temporary MonsterASP demonstration mode. Its guard restricts the DB name to `db82141` and member to `store5`. It was used only on a restored synthetic/test copy. The temporary deployment was HTTP-only and is not production-secure.

## 3. Bitwise settings

A bitmask represents several independent on/off flags in one integer. For a known flag mask `m` and current value `v`:

- Enable: `v | m`
- Disable: `v & ~m`
- Test: `(v & m) == m`

The key invariant is to preserve unrelated bits, including unknown/future bits. For a multi-bit subfield, clear only the subfield mask and OR the selected value: `(v & ~groupMask) | selectedValue`. Never infer flag values from labels alone; retain only values evidenced by the source inventory/tests.

`ParameterPayment` uses a signed SQL `smallint` while the UI/domain value is treated as the same unsigned 16-bit bit pattern; conversion preserves the bit pattern rather than changing meaning. `ParameterProductSearch` is hybrid: its low 16-bit portion is mapped to the candidate SQL `smallint`, while the high word is stored in an app-owned extension key.

## 4. Why the application uses fixed adapters

The client never sends table names or column names. It sends setting IDs and typed values. The server resolves setting IDs through the allowlist, validates values, selects the exact member row and uses parameterized SQL. This limits accidental updates and avoids turning the API into arbitrary SQL access.

## 5. Data model limitations

- 98 source controls are represented.
- 93 fixed settings adapters support new-application persistence.
- Five controls with unresolved legacy columns use app-owned extension keys.
- Five image controls use application-owned asset storage.
- The original ASP.NET Web Forms code-behind/save handler was not supplied. Therefore save parity and portal effect cannot be claimed for all 98 controls.

## 6. Validation evidence

- Frontend tests: 35/35 for the local full-stack frontend and 36/36 for public-demo mode; both production builds passed.
- Backend Release solution build: 0 warnings / 0 errors.
- Filtered non-database backend suite: 56/56 passed in the preceding compatibility pass.
- Eight focused SQL API integration groups passed with checksum-verified backups, API/SQL readback and exact restoration in the isolated local test database. These suites were not rerun during the final static-demo publication pass.
- MonsterASP smoke: owner observed application start, `/health/database` = `ready`, admin UI connected and one Save success response. This was not a complete 98-control test, and was HTTP-only.

## 7. Questions an interviewer may ask

**Why not just store every value as a string?**

Because the original schema has typed scalar, nullable, enum and bitmask fields. Typed validation and codecs reduce invalid writes and prevent precision/sign mistakes.

**How do you avoid damaging other bitmask flags?**

Update only the requested mask, preserve every other bit, and test the operation with unrelated and unknown bits already set.

**How is tenant isolation enforced?**

The server uses configured member context and requires exactly one matching row; the client cannot choose SQL identifiers. The temporary hosted demo uses a single member/token and is not a full public multi-tenant identity system.

**Does the new app exactly reproduce the old save handler?**

That cannot be claimed: the original server-side Web Forms handler was unavailable. The reports separate candidate mappings, app-adapter persistence evidence and unverified legacy portal behavior.

**What would you improve next?**

Enable HTTPS and a real per-user identity/role system for hosted access; test all affected portal screens in an authorised test tenant; resolve the five legacy extension destinations and image-storage conventions; add end-to-end tests for the remaining controls; and run a repeatable deployment/load test.
