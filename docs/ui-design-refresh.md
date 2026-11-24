# B2B STORE UI design refresh

## Design reference

This refresh follows the supplied NEON settings-screen reference: compact typography, neutral surfaces, a two-level settings navigation, a global search in the top bar, and direct form controls instead of dashboard-style cards.

## B2B STORE visual system

- Primary brand accent: B2B STORE blue (`#3F5FC6`), reserved for active navigation, focus states, and primary actions.
- Surfaces: white content area with a light neutral primary navigation rail.
- Typography: Inter-first system font stack, falling back to Segoe UI / Arial.
- Controls: compact rectangular inputs, subtle borders, minimal corner radius, no gradients, and no decorative shadows.

## Information architecture

- Primary navigation: the existing 11 Dealer System Settings categories.
- Secondary navigation: source sections within the selected category (for example Appearance, Colors, and Design under Design).
- Main workspace: setting label and help text on the left; the existing editor on the right.
- Global search: stays in the top bar, searches all categories, and retains the Ctrl+K / Escape keyboard behavior.

## Removed visual noise

- Summary/statistic cards.
- Promotional/demo information banners.
- Category counters and popular-search chips.
- Oversized rounded setting cards and gradient treatments.
- Repeated demo/test messaging beside editable fields.

## Behavior and safety preserved

This is a presentation and navigation update only. It does not change the settings catalog, value codecs, API routes, fixed `MemberCode` guard, isolated Development write-mode guard, application-owned asset routes, or GitHub Pages synthetic mode. The public demo remains frontend-only and read-only. The local SQL write capability remains restricted to the configured isolated `Store5_Settings_Test / store5` target.

## Files updated

- `frontend/src/App.vue`
- `frontend/src/style.css`
- `frontend/src/App.test.ts`
- `frontend/index.html`

## Validation

`npx vue-tsc -b` passed in the preparation environment. Vitest and the Vite production bundle were not run here because the extracted Linux workspace did not contain Rollup's Linux optional native package; run `npm test` and `npm run build` in the project's Windows environment before committing or deploying.
