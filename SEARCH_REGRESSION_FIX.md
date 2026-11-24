# Smart search and UI regression fixes

Apply these two files on the existing `public-demo` branch:
- `frontend/src/App.vue`
- `frontend/src/App.test.ts`

Changes:
- Removes cross-control implementation notes from natural-language search. This prevents a technical ID such as `ddlParameterAdmin` from also returning `ParameterModules` just because the latter's notes refer to the former.
- Mounts the Ctrl+K test in the document so it verifies real DOM focus instead of a detached Vue test mount.
- Sanitizes local test database names and test-harness wording in the customer-facing technical-details panel. Detailed evidence remains in the repository reports.
- Updates stale test expectations to match the current presentation copy and actual behavior: NULL-baseline controls remain read-only, and image drafts stay in their own upload flow rather than the settings Save bar.

No API, SQL, migration, or persistence behavior is changed. Do not change deployment or database configuration as part of this patch.

From `frontend`, run:

```powershell
npm test
npm run build
```

Do not commit or push until both commands pass.
