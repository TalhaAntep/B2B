# Smart Quick Search update

Apply on the existing `public-demo` branch. This update keeps the existing search field and makes it more useful without changing the API, SQL, or persistence behavior.

## Files
- `frontend/src/App.vue`
- `frontend/src/style.css`
- `frontend/src/App.test.ts`

## Improvements
- Searches localized and original English labels, help/description text, source option labels, setting IDs/names, candidate database fields, category names, and curated everyday-language aliases.
- Normalizes Turkish characters and accents so searches such as `odeme` can find `Ödeme` settings.
- Ranks close title matches above technical/description matches.
- Adds common quick-search chips for Logo, Stock/Stok, Payment/Ödeme, Order/Sipariş, and Menu/Menü.
- Adds `Ctrl+K` / `Cmd+K` to focus and select the search input; `/` is an alternative shortcut when focus is not already in a typing control; `Escape` clears the active search.
- Adds a clear-search control and keeps category-grouped results.

## Apply
Extract these files over the matching paths in the repository while on `public-demo`.

Then run from PowerShell:

```powershell
cd C:\Users\Talha\Desktop\project\frontend
npm test
npm run build
```

The patch was statically checked here, but Vitest/build could not be executed in the packaging environment because dependency installation did not complete. Use the existing Windows project environment for the final verification before committing/pushing.
