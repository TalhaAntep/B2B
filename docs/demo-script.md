# Local demonstration script

1. Open the local app and use the **EN / TR** selector. English is the default; refresh once to demonstrate that the selected language is remembered.
2. Browse categories and search by either the English source label, Turkish display label, setting ID, or SQL field. Search remains global while a category is selected.
3. Connect to the authenticated local API and confirm **98 settings loaded**. Do not display or capture raw customer values in screenshots or recordings.
4. Explain that normal application mode is read-only and that draft edits are not represented as saved.
5. In a separately configured local Development test environment only, show one allowlisted scalar setting and the isolated database warning. Save only after verifying the server process targets `Store5_Settings_Test` and `MemberCode=store5`; capture, back up, read back, restore, and verify the original value using the approved test procedure.
6. Show `ParameterOrder` option toggling as a bigint-safe bit operation. Explain that the API changes only selected allowlisted bits and preserves unrelated and unknown bits; never manually edit the raw integer.
7. Open Technical Information for an unresolved setting or an image slot. Explain the legacy mapping gap; app-owned extension values and image storage are replacement-app features and do not prove the old portal reads them.
8. Show API validation and read-after-write only in the isolated test environment, with secret and setting-value output suppressed.

The normal local app must have `B2BSTORE_SQL_WRITE_TEST_MODE` unset. Do not use `Store5_Analysis`, production, or another MemberCode. A synthetic Mock-mode recording is available at `docs/evidence/delivery/b2b-store-settings-demo.mp4` (with the source WebM beside it). It demonstrates local read-only/draft UI behavior only and must not be described as a live SQL or customer-data demonstration.
