# B2B STORE presentation UI update

This patch refreshes the customer-facing Vue interface without changing the API contract, SQL write path, fixed MemberCode/database guard, local asset endpoints, or GitHub Pages safety mode.

## Included files
- `frontend/src/App.vue` — product-facing labels, demo-store messaging, cleaner setting status tags, updated page heading.
- `frontend/src/style.css` — refreshed enterprise-style theme, navigation, cards, controls, file area, and responsive layout.
- `frontend/src/App.test.ts` — updates the UI regression expectation for the new “Düzenlenebilir” label.
- `frontend/index.html` — updated browser title and description.

## Safety boundary
- The backend remains unchanged.
- The isolated write capability is still granted only when the API reports `IsolatedLocalDemo` for the exact approved `Store5_Settings_Test / store5` target.
- No production or public SQL configuration is introduced.
- The public synthetic demo continues to run without API/SQL access.
- The interface uses “Demo mağazası” rather than exposing internal test-environment terminology; this does not change the underlying database target or claim legacy portal compatibility.

## Apply
Extract this ZIP at the project root and overwrite the four included files. Restart only the Vite frontend (`Ctrl+C`, then `npm run dev -- --host=127.0.0.1`). The ASP.NET API does not need to restart because no backend files changed.

Then run from `frontend`:

```powershell
npm test
npm run build
```
