import { allSettings } from './catalog';
import type { SettingItem } from './api';

/**
 * Builds a browser-only demonstration snapshot. No values are read from SQL,
 * the local API, or an external service. Every setting intentionally starts
 * unset; edits in the UI remain unpersisted drafts.
 */
export function createPublicDemoItems(): SettingItem[] {
  return allSettings.map((setting) => ({
    parameterId: setting.parameterId,
    value: null,
    mappingStatus: setting.mapping.status,
    editable: false,
  }));
}
