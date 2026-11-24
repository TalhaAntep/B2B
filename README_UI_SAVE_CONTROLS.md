# B2B Store save controls and connection-message refinement

This patch updates only the Vue presentation layer.

## Changes
- Removes the full-width success notice shown after a successful settings load. A successful connection remains indicated in the compact top-bar connection state.
- Removes the sticky bottom save bar.
- Shows Save / Discard actions inline beside the page heading only when the API explicitly reports the approved `IsolatedLocalDemo` capability and there is at least one saveable pending change.
- Keeps save success feedback compact beside the page controls.
- Keeps error feedback visible and accessible.
- Does not change backend write guards, SQL target validation, persistence logic, database schema, or public synthetic demo behavior.

## Apply
Extract `frontend/src/App.vue` and `frontend/src/style.css` over the same paths in the project. Do not overwrite `App.test.ts`; it contains the current local test adjustment for the two-level navigation.

For real SQL save testing, the API must be started explicitly in Development + SqlServer + `IsolatedLocalDemo` mode and must confirm the fixed local target `Store5_Settings_Test / store5`. A read-only API process will correctly hide Save actions.
