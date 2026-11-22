# Turkish setting option labels

## Scope and counts

The presentation mapping in `frontend/src/option-labels.tr.ts` was audited against all 98 controls in `B2B_STORE_DealerSystemSetting_inventory_v2.json` and `docs/parameter-catalog.json`.

- Options inspected: **812**
- Option entries given a Turkish presentation label: **642**
- Distinct original labels translated: **494**
- Original option entries, values, setting IDs, API payloads, and persistence code: unchanged

Repeated labels are mapped by setting ID and original option value, so one word can receive different translations where the setting context differs. The interface uses the Turkish label for display and retains the original label as an alternate search term. Selected values are rendered through the same display mapping; the underlying selection values remain the source strings.

## Untranslated exceptions

The following six option entries remain in their original wording because the inventory alone does not establish a safe translation. They represent three distinct labels/reasons.

| Setting ID | Original value | Original label | Reason |
| --- | --- | --- | --- |
| `ctl00_PageContent_ddlParameterDesign` | `1099511627776` | `Energetic Intro` | Named design/feature; the source does not describe its effect. |
| `ctl00_PageContent_ddlProductDetailParameter2` | `268435456` | `<%$Resources:GlobalLanguage, ShowStockWhenSessionClosed %>(İptal)` | Unresolved ASP.NET resource key; source text is unavailable. |
| `ctl00_PageContent_ddlParameterProductDetailNotLogin2` | `268435456` | `<%$Resources:GlobalLanguage, ShowStockWhenSessionClosed %>(İptal)` | Same unresolved ASP.NET resource key. |
| `ctl00_PageContent_ddlProductCurrency` | `7` | `CHD` | Currency-like code may be a source typo; intended currency is unverified. |
| `ctl00_PageContent_ddlPosCurrency` | `7` | `CHD` | Same unverified currency-like code. |
| `ctl00_PageContent_ddIntegrationCurrency` | `7` | `CHD` | Same unverified currency-like code. |

Currency codes and symbols, technical acronyms such as `MD5`, and source labels already in Turkish (including `Normal`, `Manuel`, and `Model`) are preserved as source labels rather than translated. These are not unresolved English sentences.

## Verification

The frontend test suite verifies that all 98 source controls and 812 source option values are represented; every translation key points to an existing setting/value pair; the 642 mapped labels change display text only; all English-like labels left unmapped are listed as exceptions; both Turkish and original English text are searchable; and a selected `ParameterOrder` bitmask continues to select the same option values after labels are localized.

No backend, API, database, or persistence code was changed for this localization.
