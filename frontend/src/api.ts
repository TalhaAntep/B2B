export type SettingValue = string | boolean | null;

export interface SettingItem {
  parameterId: string;
  value: SettingValue;
  mappingStatus: 'candidate' | 'unresolved' | 'verified';
  editable: boolean;
}

export interface SettingsResponse {
  mode: 'Mock' | 'SqlServer';
  items: SettingItem[];
  warning?: string;
  testWritesEnabled?: boolean;
}

export interface SettingsCapabilities {
  canSaveSettings: boolean;
  canManageApplicationAssets: boolean;
  environment: 'ReadOnly' | 'IsolatedLocalDemo' | 'HostedStore' | 'HostedPublicWrite';
  target: string | null;
}

export interface BitOperation {
  optionValue: string;
  enabled: boolean;
}

export interface SettingChange {
  parameterId: string;
  expectedValue: SettingValue;
  value?: SettingValue;
  bitOperations?: BitOperation[];
}

export interface PersistentSettingsSaveRequest {
  siteStatus?: { expectedValue: number | null; newValue: number };
  parameterOrder?: { expectedValue: string | null; operations: BitOperation[] };
  layout?: { expectedValue: number | null; newValue: number };
  dontShowMenuAtDealers?: { expectedValue: number | null; newValue: number };
  deliveryDateShow?: { expectedValue: boolean | null; newValue: boolean };
  changes?: SettingChange[];
}

export interface PersistentSettingsSaveResult {
  siteStatus: number | null;
  parameterOrder: string | null;
  committed: boolean;
  layout?: number | null;
  dontShowMenuAtDealers?: number | null;
  deliveryDateShow?: boolean | null;
  /** Values read back by the server after the SQL transaction commits. */
  values?: Record<string, SettingValue>;
}

export interface ApplicationAssetMetadata {
  parameterId: string;
  storedFileName: string;
  contentType: string;
  length: number;
}

export interface ApplicationAssetListResponse {
  assets: Record<string, ApplicationAssetMetadata>;
  applicationOnly: true;
  legacyPortalCompatible: false;
}

let adminToken = '';

/** Keeps the administrator token in memory only; it is never persisted in browser storage. */
export function setAdminToken(token: string): void { adminToken = token.trim(); }

export class ApiError extends Error {
  constructor(message: string, readonly status: number) {
    super(message);
    this.name = 'ApiError';
  }
}

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  let response: Response;
  try {
    response = await fetch(`/api${path}`, {
      ...init,
      headers: { 'Content-Type': 'application/json', ...(adminToken ? { Authorization: `Bearer ${adminToken}` } : {}), ...init?.headers },
    });
  } catch {
    throw new ApiError('Yerel API’ye ulaşılamadı. API hizmetinin 127.0.0.1:5080 adresinde çalıştığını kontrol edin.', 0);
  }
  const body = await response.json().catch(() => ({})) as { message?: string; code?: string };
  if (!response.ok) {
    throw new ApiError(body.message ?? body.code ?? `Request failed (${response.status})`, response.status);
  }
  return body as T;
}

export const loadSettings = () => request<SettingsResponse>('/settings');

export const loadSettingsCapabilities = () => request<SettingsCapabilities>('/settings/capabilities');

export const saveSettings = (changes: SettingChange[]) => request<PersistentSettingsSaveResult>(
  '/settings',
  { method: 'PUT', body: JSON.stringify({ changes }) },
);

export const saveIsolatedTestSettings = (changes: PersistentSettingsSaveRequest) => request<PersistentSettingsSaveResult>(
  '/settings/test/persistent-save',
  { method: 'POST', body: JSON.stringify(changes) },
);

async function applicationAssetRequest<T>(path: string, init?: RequestInit): Promise<T> {
  let response: Response;
  try {
    response = await fetch(`/api/application-assets${path}`, {
      ...init,
      headers: { ...(adminToken ? { Authorization: `Bearer ${adminToken}` } : {}), ...init?.headers },
    });
  } catch {
    throw new ApiError('Yerel API’ye ulaşılamadı.', 0);
  }
  const body = await response.json().catch(() => ({})) as { message?: string; code?: string };
  if (!response.ok) throw new ApiError(body.message ?? body.code ?? `Request failed (${response.status})`, response.status);
  return body as T;
}

export const listApplicationAssets = () => applicationAssetRequest<ApplicationAssetListResponse>('');

export const uploadApplicationAsset = (parameterId: string, file: File) => {
  const form = new FormData();
  form.append('file', file);
  return applicationAssetRequest<{ metadata: ApplicationAssetMetadata; applicationOnly: true; legacyPortalCompatible: false }>(
    `/${encodeURIComponent(parameterId)}`, { method: 'POST', body: form },
  );
};

export const deleteApplicationAsset = (parameterId: string) => applicationAssetRequest<{ deleted: boolean; applicationOnly: true }>(
  `/${encodeURIComponent(parameterId)}`, { method: 'DELETE' },
);

export const applicationAssetContentUrl = (parameterId: string) =>
  `/api/application-assets/${encodeURIComponent(parameterId)}/content`;
