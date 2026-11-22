import { readFile, writeFile } from 'node:fs/promises';
import { resolve } from 'node:path';

const root = resolve(import.meta.dirname, '..');
const mappingPath = resolve(root, 'B2B_STORE_DealerSystemSetting_field_mapping.json');
const inventoryPath = resolve(root, 'B2B_STORE_DealerSystemSetting_inventory_v2.json');
const outPath = resolve(root, 'docs/parameter-catalog.json');
const auditPath = resolve(root, 'docs/mapping-audit.json');

const [mapping, inventory, schemaAudit] = await Promise.all([
  readFile(mappingPath, 'utf8').then(JSON.parse),
  readFile(inventoryPath, 'utf8').then(JSON.parse),
  readFile(auditPath, 'utf8').then(JSON.parse),
]);

const inventoryControls = inventory.groups.flatMap((group) => group.controls);
const mappingControls = mapping.groups.flatMap((group) => group.controls);
if (inventoryControls.length !== 98 || mappingControls.length !== inventoryControls.length) {
  throw new Error(`Expected 98 paired controls, got inventory=${inventoryControls.length}, mapping=${mappingControls.length}`);
}

const mappingById = new Map(mappingControls.map((control) => [control.controlId, control]));
const schemaById = new Map(schemaAudit.map((entry) => [entry.htmlControlId, entry]));
const statusMap = {
  semantic_control_mapping_candidate: 'candidate',
  unresolved_control_has_no_clear_column: 'unresolved',
  unresolved_no_schema_match: 'unresolved',
};

const groups = inventory.groups.map((group) => ({
  panel: group.panel,
  section: group.section,
  controls: group.controls.map((control) => {
    const match = mappingById.get(control.controlId);
    const schema = schemaById.get(control.controlId);
    if (!match) throw new Error(`Missing mapping for ${control.controlId}`);
    if (!schema) throw new Error(`Missing schema audit for ${control.controlId}`);
    const status = statusMap[match.mappingStatus];
    if (!status) throw new Error(`Unknown mapping status ${match.mappingStatus}`);
    const controlType = control.type === 'select' && control.multiple
      ? 'bitmask'
      : control.type;
    return {
      parameterId: control.controlId,
      originalLabel: control.label,
      panel: control.panel,
      category: control.section,
      htmlControlName: control.controlName,
      htmlControlId: control.controlId,
      htmlElement: control.element,
      controlType,
      multiple: Boolean(control.multiple),
      options: (control.options ?? control.radioOptions ?? []).map((option) => ({
        value: String(option.value),
        label: option.label ?? String(option.value).replace(/^rbFavoriteIcon/, ''),
      })),
      mapping: {
        status,
        sourceStatus: match.mappingStatus,
        tableCandidate: match.databaseColumnCandidate ? 'DealerSettings' : null,
        columnCandidate: match.databaseColumnCandidate ?? null,
        sqlDataType: schema.sqlType,
        nullable: schema.nullable,
        evidence: schema.sqlType
          ? `HTML-derived control inventory and candidate column; Store5_Settings_Test sys.columns reports ${schema.sqlType}${schema.nullable ? ' NULL' : ' NOT NULL'}. Legacy save behavior remains unknown.`
          : 'HTML-derived inventory and candidate mapping; no schema-compatible DealerSettings column was confirmed.',
        outstandingQuestion: match.verificationNote,
      },
      valueSemantics: controlType === 'bitmask'
        ? 'candidate-bitmask; option values are preserved as decimal strings; runtime semantics unverified'
        : control.type === 'checkbox'
          ? 'candidate-boolean; persistence mapping unverified'
          : control.type === 'file'
            ? 'file-upload control; persistence behavior unverified and disabled in this app'
            : 'unverified; preserve source option labels and values',
    };
  }),
}));

// Evidence added after the captured v2 inventory is kept as an explicit overlay so
// regeneration does not erase source discoveries or misstate extension persistence.
const controlsById = new Map(groups.flatMap((group) => group.controls).map((control) => [control.parameterId, control]));
const modules = controlsById.get('ctl00_PageContent_ddlParameterModules');
modules.options = [{ value: '1', label: 'Show Order Export Button' }];
modules.valueSemantics = 'The original browser DOM identifies option value 1 as Show Order Export Button for this control. It is independent from ddlParameterAdmin, whose option value 32 has the same label.';
modules.mapping.evidence = 'Original browser DOM evidence identifies option value 1 = Show Order Export Button for ctl00_PageContent_ddlParameterModules. The separate ctl00_PageContent_ddlParameterAdmin control identifies option value 32 for the same label and maps to DealerSettings.ParameterAdmin (bigint); this control maps to DealerSettings.ParameterModules (int). Store5_Settings_Test metadata and the isolated adapter round-trip for option 1 are verified. Legacy save-handler behavior remains unavailable.';
modules.mapping.outstandingQuestion = 'No option-ID ambiguity remains between ParameterModules and ParameterAdmin. Legacy save-handler behavior and old-portal parity remain unverified.';

const persistenceOverrides = {
  ctl00_PageContent_ddlPaymentParameter: {
    valueSemantics: '16-bit unsigned bit-pattern mask transported as a decimal string; persisted to nullable DealerSettings.ParameterPayment smallint by unchecked signed conversion. Unknown and unlisted bits are preserved.',
    evidence: 'User-supplied task evidence confirms the DealerSettings.ParameterPayment smallint destination, source options through 32768, and unsigned 16-bit mask semantics. No setting values are included. Legacy save handler remains unavailable.',
    question: 'Legacy portal save/consumer parity is unknown; only the isolated API adapter is in scope.',
  },
  ctl00_PageContent_ddlProductSearchParameter: {
    valueSemantics: 'Decimal-string bitmask. Low 16-bit word is persisted as signed smallint bit pattern in DealerSettings.ParameterProductSearch; high bits use the application-owned extension key ParameterProductSearch.HighMask.',
    evidence: 'User-supplied task evidence confirms DealerSettings.ParameterProductSearch smallint and its option range through 1,048,576. Low-word unsigned conversion and application-owned high-mask extension are fixed by this implementation; legacy high-bit storage is not demonstrated.',
    question: 'The old portal cannot consume the application-owned high-mask extension unless separately updated; legacy save-handler behavior remains unknown.',
  },
  ctl00_PageContent_ddlParameterProductDetailNotLogin2: {
    valueSemantics: '64-bit decimal-string bitmask in application-owned extension storage; absent is distinct from explicit zero.',
    evidence: 'No legacy destination is established. Fixed application-owned DealerSystemSettingExtension key ParameterProductDetailNotLogin2; old portal does not automatically consume it.',
    question: 'Legacy portal compatibility is unverified; this field is application-only until an actual legacy consumer is demonstrated.',
  },
  ctl00_PageContent_ddlParameterMenu2: {
    valueSemantics: 'Decimal-string bitmask in application-owned extension storage; only source option 1 is known; absent is distinct from explicit zero.',
    evidence: 'No legacy destination is established. Fixed application-owned DealerSystemSettingExtension key ParameterMenu2; old portal does not automatically consume it.',
    question: 'Legacy portal compatibility is unverified; this field is application-only until an actual legacy consumer is demonstrated.',
  },
  ctl00_PageContent_chkWhatsAppOrderNotification: {
    valueSemantics: 'Boolean in application-owned extension storage; absent is distinct from explicit false.',
    evidence: 'No legacy destination is established. Fixed application-owned DealerSystemSettingExtension key WhatsAppOrderNotification; old portal does not automatically consume it.',
    question: 'Legacy portal compatibility is unverified; this field is application-only until an actual legacy consumer is demonstrated.',
  },
  ctl00_PageContent_rbFavoriteIconLeft: {
    valueSemantics: 'Source radio choices map to canonical application-owned enum values Left and Right; absent remains unconfigured.',
    evidence: 'No legacy destination is established. Fixed application-owned DealerSystemSettingExtension key FavoriteIconPosition; old portal does not automatically consume it.',
    question: 'Legacy portal compatibility is unverified; this field is application-only until an actual legacy consumer is demonstrated.',
  },
  ctl00_PageContent_rbFavoriteIconTop: {
    valueSemantics: 'Source radio choices map to canonical application-owned enum values Top and Bottom; absent remains unconfigured.',
    evidence: 'No legacy destination is established. Fixed application-owned DealerSystemSettingExtension key FavoriteIconVerticalPosition; old portal does not automatically consume it.',
    question: 'Legacy portal compatibility is unverified; this field is application-only until an actual legacy consumer is demonstrated.',
  },
};
for (const [parameterId, override] of Object.entries(persistenceOverrides)) {
  const control = controlsById.get(parameterId);
  control.valueSemantics = override.valueSemantics;
  control.mapping.evidence = override.evidence;
  control.mapping.outstandingQuestion = override.question;
}

const statusCounts = groups.flatMap((group) => group.controls).reduce((counts, control) => {
  counts[control.mapping.status] = (counts[control.mapping.status] ?? 0) + 1;
  return counts;
}, {});

const catalog = {
  schemaVersion: 1,
  title: 'B2B STORE DealerSystemSetting parameter catalog',
  sourceInventory: 'B2B_STORE_DealerSystemSetting_inventory_v2.json',
  sourceMapping: 'B2B_STORE_DealerSystemSetting_field_mapping.json',
  evidenceNote: 'The supplied field mapping is candidate evidence, not proof of persistence. No parameter is promoted to verified by this generator.',
  controlCount: groups.reduce((sum, group) => sum + group.controls.length, 0),
  statusCounts,
  groups,
};

await writeFile(outPath, `${JSON.stringify(catalog, null, 2)}\n`, 'utf8');
console.log(`Wrote ${catalog.controlCount} catalog controls (${JSON.stringify(statusCounts)})`);
