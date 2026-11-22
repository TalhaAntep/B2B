# Parameter catalog

The machine-readable catalog used by the frontend and backend is [`parameter-catalog.json`](parameter-catalog.json). Regenerate it with:

```powershell
node .\scripts\build-catalog.mjs
```

It preserves the v2 inventory's original labels, control IDs/names, categories and options. The legacy field-mapping source remains 93 candidates, 5 unresolved and zero legacy save mappings verified. The separate isolated Development API allowlist has 93 tested settings adapters; that is not legacy portal verification. The mapping audit records available SQL types, application-owned storage, per-field test evidence and unresolved compatibility. No actual screen-selected customer values are included.

Multi-select option values remain decimal strings. Do not parse SQL `bigint` values with JavaScript `Number` or `parseInt`. The frontend uses `BigInt` for display state and submits bit operations for backend calculation.

## Persistence status update - 2026-11-22

The generated catalog contains 98 settings. The fixed isolated Development API allowlist covers 93 settings with authenticated API save/readback/restoration evidence. This does not establish legacy save-handler behavior. Five extension-backed settings and the Product Search high word use application-owned storage; old-portal consumption is not demonstrated. Original browser DOM evidence confirms `ddlParameterModules` option `1` and separately confirms `ddlParameterAdmin` option `32`; these map to `DealerSettings.ParameterModules` (`int`) and `DealerSettings.ParameterAdmin` (`bigint`) respectively. The option-ID ambiguity is resolved; legacy save-handler behavior remains unverified.
