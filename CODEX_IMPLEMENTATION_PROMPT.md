# Codex Implementation Brief — B2B STORE DealerSystemSetting

You are the implementation engineer. Work in the existing project folder and deliver a runnable application, not just a plan. Follow `AGENTS.md` and `PROJECT_REQUIREMENTS.md` as the governing instructions. Read the latest `B2B_STORE_DealerSystemSetting_inventory_v2.json/.md` and `B2B_STORE_DealerSystemSetting_field_mapping.json/.md` before coding. Treat the mapping file as a set of **candidates**, not proof. The project also contains `source/store5.bak`.

## 1. Goal and scope

Rebuild only the B2B STORE **Dealer System Settings / Advanced Settings** module as a modern responsive application:
- Frontend: Vue 3 + TypeScript (Vite).
- Backend: C# ASP.NET Core Web API.
- Database: Microsoft SQL Server.
- Preserve the existing `DealerSettings` schema and legacy behavior where supported by evidence.
- Do not rebuild the full B2B platform.

The source archive does not contain the original application source. Work from the supplied requirement documents, HTML-derived inventory/mapping, stored-procedure evidence recorded in the conversation/project docs, and the restored SQL backup. Do not pretend that unavailable code-behind or portal behavior has been inspected.

## 2. First actions — inspect the environment and inputs

1. Inspect the repository, all Markdown/JSON inputs, `source/`, installed .NET SDK, Node/npm, and available SQL Server tools.
2. Validate the JSON files and identify the inventory version (`v2` is the current one). Ignore older duplicate inventory files unless needed for comparison.
3. Check that `source/store5.bak` exists. Do not copy it into the frontend, build output, public folder, deployment artifact, or Git history.
4. The backup has already been restored by the user locally as `Store5_Analysis` in SQL Server, but connection details may vary. Detect/document a usable local connection if possible. If connection cannot be established, do not guess credentials or block all work: build against the verified schema and ask only for the missing local connection detail at the next checkpoint.
5. Query metadata only at first. Do not dump customer, company, user, order, password, token, email, or other business data. If record inspection is needed, redact identifiers and show counts/status only.
6. Confirm the actual `DealerSettings` columns, types, nullability, indexes, keys and row cardinality. Earlier discovery found 222 nullable columns and 4 rows, but re-verify against the active restored database. Earlier searches found no PK/unique index and no FK for this table; verify before deciding an update strategy.

## 3. Critical data facts and guardrails

- `DealerSettings` is the primary candidate table for this screen. It has 222 columns, including individual fields and multiple `Parameter*` fields.
- `DealerSystemSettings` contains repeated `LoginImage1`, `LoginImage2`, `LoginImage3`, and `LogoURL` keys; a stored-module search found no SQL-module references. Do not mistake it for the main settings table.
- `Settings` is a separate general StoreSettings key/value table managed by `procSettings`; it is not the primary `DealerSystemSetting` table.
- `GeneralSettings` appears to be a separate per-store/general configuration table. Do not include it unless a direct screen mapping is proven.
- `B2CSettings` mostly resembles the DealerSettings columns but is a distinct table. Do not silently substitute it.
- The provided page HTML shows classic ASP.NET Web Forms: a form posts to `./DealerSystemSetting`, and Save invokes `__doPostBack`. Its UI structure and form control names/options are evidence; the original server-side code-behind is unavailable.
- The HTML reflects an authenticated `store1` admin session. Its selected control values are NOT the same thing as the local `store5.bak` values. Never use those selected values as seed/default data for `Store5_Analysis`.
- Never hardcode, print, commit, or deploy credentials, session values, `__VIEWSTATE`, connection strings, live tokens, or real customer data.
- Never connect this app to the live B2B STORE production platform or mutate the live system.
- Never update/delete/alter the original backup or the user's restored database during initial development. Use read-only SQL inspection. For write tests, create an isolated disposable test database or use a transaction that is always rolled back; explain the test isolation clearly.
- Do not change legacy schema (add PK, columns, indexes, migrations, constraints) without explicit approval. If a deterministic row key is absent, report that and design a guarded update strategy; do not use an unqualified `TOP 1` update.

## 4. Settings inventory and mapping

Use the supplied v2 inventory and field mapping as starting artifacts. Keep all original labels/control IDs/options. Mark each field as one of:
- `verified`: confirmed through matching markup + SQL schema and/or SQL behavior;
- `candidate`: plausible name/type match, not yet verified;
- `unresolved`: no safe match;
- `not_persisted_or_ui_only`: only after confirming this from evidence.

Do not promote a candidate to verified just because the control ID resembles a column name. For each mapping, capture original label, HTML control name/id, parent section/subsection, SQL table/column, SQL type/nullability, value semantics, evidence, confidence and outstanding questions.

The inventory currently reports 98 controls (including 5 file-upload controls). Recount and validate against the actual JSON/HTML-derived data; if the count differs, document why. A few unresolved controls include the second menu parameter, a second unauthenticated B2C product-detail parameter, WhatsApp order notification, and favorite icon position radio values. Investigate whether they are stored elsewhere or are UI-only; do not invent columns.

## 5. Bitmask rules — correctness is mandatory

Multiple `Parameter*` columns are bitmasks, not ordinary enums. The HTML provides option values as powers of two for multi-select controls. SQL procedures confirm several behaviors, including:

- `ParameterOrder`: bit 16 (`65536`) is used for VAT-inclusive cart price handling; bit 28 (`2^28`) for a duplicate-item warning/check; bit 40 (`2^40`) for a variation-related cart restriction; bit 13 (`2^13`) for grouping cart products by category; bit 33 (`2^33`, decimal `8589934592`) changes cart item-count calculation. Treat descriptions as procedure-specific evidence, not complete documentation of all effects.
- `ParameterAdmin`: bit 10 (`2^10`) relates to regional warehouse display; bit 22 (`2^22`) to depot hiding/stock calculation; bit 35 (`2^35`) to depot-based cart behavior; bit 20 (`2^20`) to product hiding in cart SQL; bits 19 and 31 affect company-brand visibility/discount paths; bit 36 affects payment-type-specific discount filtering. Other HTML options exist and must be inventoried separately.
- `ParameterIntegration`: bit 1 (`1`) affects customer-group field updates in customer import; bit 2 (`2`) changes customer discount matching; bit 4 (`4`) affects ERP-imported sub-dealer orders.
- `ParameterMenu`: bit 20 (`1048576`) is associated with sub-dealer menu behavior in customer import.

These are partial confirmed findings, not a complete bitmask catalogue. Extract every option from the HTML inventory, compute its bit/decimal safely, and link only behavior backed by SQL evidence or tests. Avoid overclaiming.

**Never use JavaScript `Number`, `parseInt`, or floating-point arithmetic for SQL `bigint` masks.** Serialize SQL bigint values as decimal strings in JSON and use JavaScript `BigInt` for UI bit operations. Use C# `long`/`long?` (or safe SQL bigint handling) on the server. Preserve unknown/unrepresented bits when toggling one option:
`next = current | bit` to enable; `next = current & ~bit` to disable.
Preserve SQL `NULL` distinctly from `0`; never normalize every NULL to zero on read. Untouched NULL fields must remain NULL. If a user explicitly edits a NULL mask, display that it was unset and define/document the explicit transition used for the edited field only.

## 6. Tenant/row selection and safe persistence

- `MemberCode` is a tenant/record discriminator and must not be ignored.
- Some legacy code selects `MemberCode IS NULL OR MemberCode = ...`; another procedure selects by `MemberCode=@MemberCode`; others use `TOP 1`. This inconsistent legacy behavior needs documentation.
- There are four `DealerSettings` rows in the restored DB. Do not assume the first row is the intended row, do not select arbitrary `TOP 1`, and do not expose raw customer/member identifiers in logs or reports.
- Build an explicit local configuration for the target MemberCode/record context. If it is missing or maps to multiple ambiguous rows, the API must refuse writes and return a clear actionable error. A masked identifier or row ordinal may be used for diagnostic selection; do not output raw IDs.
- For writes, use parameterized SQL, a transaction, row-count checking, and optimistic concurrency where feasible. Update only explicitly changed columns. For bitmasks, update a whole validated mask value calculated by the backend or use a safe atomic bit operation. Preserve every unrelated bit.
- Do not accept arbitrary table/column names from client input. Use a server-side allowlist from the catalog.

## 7. Application architecture and UX

Build a clear solution structure, for example:
- `frontend/`: Vue 3 + TypeScript + Vite.
- `backend/`: ASP.NET Core Web API.
- `docs/`: technical analysis, mapping/catalog, test report, architecture, AI usage report.
- root README and `.gitignore`.

The UI must:
- Be responsive and professional, with a left/category navigation or tabs, searchable settings, section headings and concise help text.
- Reproduce all confirmed controls and options from the inventory without dropping labels/options.
- Use correct control types (switch, checkbox, select, multi-select/bitmask list, number, text, radio, file/image field) based on actual metadata.
- Make bitmask selections understandable and preserve unknown bits.
- Show unset/NULL distinctly where meaningful.
- Show dependencies only when verified; otherwise present evidence/confidence notes to admins, not invented warnings.
- Have loading, empty, error, dirty/unsaved, saving, saved and validation states.
- Provide Save and Cancel/Discard changes. Warn before navigation with unsaved changes.
- Keep original labels visible in a details/help view if friendly labels are introduced.
- Do not expose raw bigint mask numbers as ordinary editable numbers.

API:
- Provide health endpoint and documented settings read/update endpoints.
- Protect settings endpoints with an authentication mechanism suitable for a local admin tool. Credentials/secrets must be supplied via environment variables/user-secrets, never defaults committed to source. In Development, make local setup easy; in Production, fail startup if required secrets are absent.
- Validate field names and values against an allowlist/catalog on the server.
- Return bigint values as decimal strings. Preserve nulls.
- Use DTOs rather than exposing database rows wholesale.
- Return clear error codes/messages for ambiguous tenant selection, concurrency conflict, validation failure, missing configuration and DB connectivity.

## 8. Testing

At minimum implement automated tests for:
- Bitmask enable/disable leaves every unrelated bit unchanged.
- High-order bigint options remain exact through JSON round trip (including values above JavaScript's safe integer limit).
- SQL NULL remains distinct from zero when untouched.
- Invalid fields/values are rejected by the API.
- Ambiguous/missing MemberCode prevents writes.
- Updates are restricted to changed allowlisted columns and check expected row count.
- Frontend can load and edit sample/mock settings without a real customer DB.

Create a sanitized synthetic dataset for UI development. Never copy real customer data into fixtures. If end-to-end portal behavior cannot be tested because a dealer portal account or source code is unavailable, mark it `Not tested / access unavailable`, never invent a result.

## 9. Deliverables

Produce and maintain:
1. Runnable frontend and backend source code.
2. SQL connection/setup guide using environment variables/user-secrets.
3. `docs/technical-analysis-report.md` — parameter purpose, mappings, SQL evidence, mask details, unknowns and confidence.
4. `docs/test-bug-report.md` — expected vs observed, test evidence/screenshots where available, blockers and existing defects (do not fix defects in the legacy platform).
5. `docs/parameter-catalog.json` — machine-readable master catalog used by the app wherever practical.
6. `docs/architecture.md` and API documentation (Swagger/OpenAPI).
7. `docs/ai-usage-report.md` — AI tools and assistance used, user verification and limitations.
8. Root `README.md` with exact Windows setup/run/test instructions.
9. Deployment notes for free hosting, but no deployment of real customer database or secrets. Keep deployment configuration ready; do not publish until a safe anonymized/demo database and secure environment variables are available.
10. A short demo recording if the environment supports recording. If it does not, provide a concise demo script and state the limitation honestly.

## 10. Execution order and reporting

Do not stop after writing another long analysis. Implement in short milestones:
1. Inspect inputs/environment and write a concise findings summary.
2. Build validated schema/catalog and safe DB access layer.
3. Implement backend read/update API and unit tests.
4. Implement responsive Vue UI against mock data, then connect to API.
5. Run build/tests and fix actual errors.
6. Finish documentation and demo script.

Do not ask the user broad preference questions. Make sensible, reversible choices and document them. Ask only for a genuinely blocking fact (for example, the exact local SQL Server instance/connection detail or which MemberCode record should be managed) and continue other work meanwhile.

At each milestone, report briefly:
- files created/changed;
- commands run and pass/fail results;
- verified vs candidate vs unresolved mappings;
- blockers that require user input.

**Start now by inspecting the project. Do not modify the original backup or live B2B STORE system. Do not commit secrets or real data.**
