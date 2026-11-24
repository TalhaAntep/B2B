import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { flushPromises, mount } from '@vue/test-utils';
import App from './App.vue';
import rawInventory from '../../B2B_STORE_DealerSystemSetting_inventory_v2.json';
import { allSettings, categories, categoryDisplayLabel, displayLabel } from './catalog';
import { optionDisplayLabel, optionSearchLabels, turkishOptionLabels, untranslatedOptionExceptions } from './option-labels.tr';
import type { SettingsResponse, SettingItem } from './api';

type SourceOption = { value: string; label?: string };
type SourceControl = {
  controlId: string;
  multiple?: boolean;
  type: string;
  options?: SourceOption[];
  radioOptions?: SourceOption[];
};

describe('read-only settings experience', () => {
  let requestMock: ReturnType<typeof vi.fn>;
  let testWrites = false;

  beforeEach(() => {
    localStorage.clear();
    localStorage.setItem('b2b-settings-language', 'tr');
    const items: SettingItem[] = allSettings.map((setting) => ({
      parameterId: setting.parameterId,
      value: setting.parameterId === 'ctl00_PageContent_ddlParameterAdmin'
        ? '4611686018427387904'
        : setting.parameterId === 'ctl00_PageContent_ddUseFiltering' ? '12'
          : setting.parameterId === 'ctl00_PageContent_ddlParameterMenu' ? '0' : null,
      mappingStatus: setting.mapping.status,
      editable: false,
    }));
    testWrites = false;
    Object.defineProperty(URL, 'createObjectURL', { configurable: true, value: vi.fn(() => 'blob:test-preview') });
    Object.defineProperty(URL, 'revokeObjectURL', { configurable: true, value: vi.fn() });
    requestMock = vi.fn(async (_input: RequestInfo | URL, init?: RequestInit) => {
      const responseBody = init?.method === 'PUT'
        ? { committed: true }
        : { mode: 'SqlServer', items: testWrites ? items.map((item) => ({ ...item,
          value: item.parameterId === 'ctl00_PageContent_ddlSiteStatus' ? '1'
            : item.parameterId === 'ctl00_PageContent_ddlOrderParameter' ? '1'
              : item.parameterId === 'ctl00_PageContent_ddlLayout' ? '1'
                : item.parameterId === 'ctl00_PageContent_ddlDontShowMenuAtDealers' ? '1'
                  : item.parameterId === 'ctl00_PageContent_chkDeliveryDateShow' ? true
                    : item.parameterId === 'ctl00_PageContent_ddUseFiltering' ? '12'
                      : item.parameterId === 'ctl00_PageContent_ddlDiscountType' ? "'A'" : item.value,
        })) : items, testWritesEnabled: testWrites } satisfies SettingsResponse;
      return new Response(JSON.stringify(responseBody), { status: 200, headers: { 'Content-Type': 'application/json' } });
    });
    vi.stubGlobal('fetch', requestMock);
  });

  afterEach(() => {
    vi.unstubAllGlobals();
    Reflect.deleteProperty(URL, 'createObjectURL');
    Reflect.deleteProperty(URL, 'revokeObjectURL');
  });

  it('auto-connects without an admin token in explicit hosted public-write mode', async () => {
    const settings = allSettings.map((setting) => ({
      parameterId: setting.parameterId,
      value: setting.parameterId === 'ctl00_PageContent_ddlSiteStatus' ? '1'
        : setting.parameterId === 'ctl00_PageContent_ddlOrderParameter' ? '1'
          : setting.parameterId === 'ctl00_PageContent_ddlLayout' ? '1'
            : setting.parameterId === 'ctl00_PageContent_ddlDontShowMenuAtDealers' ? '1'
              : setting.parameterId === 'ctl00_PageContent_chkDeliveryDateShow' ? true : null,
      mappingStatus: setting.mapping.status,
      editable: true,
    } satisfies SettingItem));
    requestMock.mockImplementation(async (input: RequestInfo | URL) => {
      const path = String(input);
      const body = path.endsWith('/settings/capabilities')
        ? { canSaveSettings: true, canManageApplicationAssets: true, environment: 'HostedPublicWrite', target: 'Public shared settings' }
        : path.endsWith('/application-assets')
          ? { assets: {}, applicationOnly: true, legacyPortalCompatible: false }
          : { mode: 'SqlServer', items: settings, testWritesEnabled: true } satisfies SettingsResponse;
      return new Response(JSON.stringify(body), { status: 200, headers: { 'Content-Type': 'application/json' } });
    });
    const wrapper = mount(App);
    await flushPromises();
    expect(wrapper.find('.admin-token-entry').exists()).toBe(false);
    expect(wrapper.find('.mode-pill').text()).toContain('Bağlandı');
    expect(wrapper.find('.message-public').text()).toContain('ortak veritabanına kaydedilir');
    expect(requestMock).toHaveBeenCalledWith('/api/settings', expect.objectContaining({
      headers: expect.not.objectContaining({ Authorization: expect.any(String) }),
    }));
    wrapper.unmount();
  });

  it('uses Turkish-first customer labels while preserving original identifiers and values', () => {
    expect(categoryDisplayLabel('Product Details')).toBe('Ürün Detayları');
    const site = allSettings.find((setting) => setting.originalLabel === 'Site State')!;
    expect(displayLabel(site)).toBe('Site Durumu');
    expect(site.htmlControlId).toBe('ctl00_PageContent_ddlSiteStatus');
    expect(optionDisplayLabel(site.parameterId, site.options[0])).toBe('Aktif');
    expect(site.options.find((option) => option.label === 'Active')?.value).toBe('1');
  });

  it('defaults to English, switches language without losing drafts, and remembers the choice', async () => {
    localStorage.removeItem('b2b-settings-language');
    const wrapper = await connectedApp();
    expect(wrapper.find('.language-switch button[aria-pressed="true"]').text()).toBe('EN');
    expect(wrapper.find('h1').text()).toBe('General');
    await wrapper.find('input[type="search"]').setValue('Product Image Aspect Ratio');
    const input = wrapper.find('.setting-value input[type="text"]');
    await input.setValue('3:2');
    const before = (input.element as HTMLInputElement).value;
    await wrapper.find('.language-switch button:nth-child(2)').trigger('click');
    expect(wrapper.find('.language-switch button[aria-pressed="true"]').text()).toBe('TR');
    expect((wrapper.find('.setting-value input[type="text"]').element as HTMLInputElement).value).toBe(before);
    expect(wrapper.text()).toContain('Ürün Görseli En-Boy Oranı');
    expect(localStorage.getItem('b2b-settings-language')).toBe('tr');
    await wrapper.find('.language-switch button:nth-child(1)').trigger('click');
    expect(wrapper.text()).toContain('Product Image Aspect Ratio');
    expect((wrapper.find('.setting-value input[type="text"]').element as HTMLInputElement).value).toBe(before);
    wrapper.unmount();
    const refreshed = mount(App);
    expect(refreshed.find('.language-switch button[aria-pressed="true"]').text()).toBe('EN');
    expect(refreshed.find('h1').text()).toBe('General');
    refreshed.unmount();
  });

  it('labels synthetic Mock mode without implying an SQL connection', async () => {
    const syntheticItems = allSettings.map((setting) => ({
      parameterId: setting.parameterId,
      value: null,
      mappingStatus: setting.mapping.status,
      editable: false,
    } satisfies SettingItem));
    requestMock.mockImplementation(async (input: RequestInfo | URL) => {
      const responseBody = String(input).endsWith('/settings/capabilities')
        ? { canSaveSettings: false, canManageApplicationAssets: false, environment: 'ReadOnly', target: null }
        : { mode: 'Mock', items: syntheticItems, testWritesEnabled: false } satisfies SettingsResponse;
      return new Response(JSON.stringify(responseBody), { status: 200, headers: { 'Content-Type': 'application/json' } });
    });
    localStorage.removeItem('b2b-settings-language');
    const wrapper = await connectedApp();
    expect(wrapper.text()).toContain('Demo');
    expect(wrapper.text()).not.toMatch(/Demo settings loaded|SQL settings loaded/i);
    expect(wrapper.text()).not.toContain('SQL settings loaded.');
    expect(wrapper.findAll('.category-nav-item')).toHaveLength(11);
    expect(wrapper.find('.section-sidebar').exists()).toBe(true);
    expect(wrapper.find('.summary-row').exists()).toBe(false);
    expect(wrapper.find('.message-demo').exists()).toBe(false);
    expect(wrapper.find('.page-count').exists()).toBe(false);
    wrapper.unmount();
  });

  it('renders selected option values with English labels without changing their source values', async () => {
    localStorage.removeItem('b2b-settings-language');
    const loaded = allSettings.map((setting) => ({
      parameterId: setting.parameterId,
      value: setting.parameterId === 'ctl00_PageContent_ddlSiteStatus' ? '1' : null,
      mappingStatus: setting.mapping.status,
      editable: false,
    } satisfies SettingItem));
    requestMock.mockImplementation(async () => new Response(JSON.stringify({ mode: 'SqlServer', items: loaded, testWritesEnabled: false } satisfies SettingsResponse), { status: 200 }));
    const wrapper = await connectedApp();
    await wrapper.find('input[type="search"]').setValue('Site State');
    const select = wrapper.find('.setting-value select');
    expect((select.element as HTMLSelectElement).value).toBe('1');
    expect((select.element as HTMLSelectElement).selectedOptions[0]?.textContent).toBe('Active');
    expect(loaded.find((item) => item.parameterId === 'ctl00_PageContent_ddlSiteStatus')?.value).toBe('1');
    wrapper.unmount();
  });

  it('offers UseFiltering and DiscountType editors only when isolated test writes are enabled', async () => {
    testWrites = true;
    const wrapper = await connectedApp();
    await wrapper.find('input[type="search"]').setValue('ddUseFiltering');
    const filteringCard = wrapper.findAll('.setting-card').find((card) => card.text().includes('Filter Display'))!;
    const filterTwo = filteringCard.find('input[type="checkbox"][value="2"]');
    const filterFour = filteringCard.find('input[type="checkbox"][value="4"]');
    const filterEight = filteringCard.find('input[type="checkbox"][value="8"]');
    expect(filterTwo.exists()).toBe(true);
    expect((filterTwo.element as HTMLInputElement).checked).toBe(false);
    expect((filterFour.element as HTMLInputElement).checked).toBe(true);
    expect((filterEight.element as HTMLInputElement).checked).toBe(true);

    await wrapper.find('input[type="search"]').setValue('Discount Type');
    const discountCard = wrapper.findAll('.setting-card').find((card) => card.text().includes('Discount Type'))!;
    const discountSelect = discountCard.find('select.setting-input');
    expect(discountSelect.exists()).toBe(true);
    expect((discountSelect.element as HTMLSelectElement).value).toBe("'A'");
    expect(Array.from((discountSelect.element as HTMLSelectElement).options).map((option) => option.value).filter(Boolean)).toEqual(["'A'", "'P'", "'C'"]);
    wrapper.unmount();
  });

  it('keeps unsigned smallint masks and the discovered ParameterModules option exact', () => {
    const payment = allSettings.find((setting) => setting.parameterId === 'ctl00_PageContent_ddlPaymentParameter')!;
    const search = allSettings.find((setting) => setting.parameterId === 'ctl00_PageContent_ddlProductSearchParameter')!;
    const modules = allSettings.find((setting) => setting.parameterId === 'ctl00_PageContent_ddlParameterModules')!;
    expect(payment.options.find((option) => option.value === '32768')?.value).toBe('32768');
    expect(payment.mapping.sqlDataType).toBe('smallint');
    expect(search.options.find((option) => option.value === '1048576')?.value).toBe('1048576');
    expect(modules.options).toEqual([{ value: '1', label: 'Show Order Export Button' }]);
  });

  it('keeps an extension checkbox unconfigured until an explicit false value is saved', async () => {
    testWrites = true;
    const wrapper = await connectedApp();
    await wrapper.find('input[type="search"]').setValue('WhatsApp Order Notification');
    expect(wrapper.find('.setting-card button').text()).toBe('Kapalı olarak yapılandır');
    expect(wrapper.find('.save-bar').exists()).toBe(false);
    await wrapper.find('.setting-card button').trigger('click');
    expect(wrapper.find('.save-bar').text()).toContain('1 değişiklik uygulanabilir');
    await wrapper.find('.save-bar .button-primary').trigger('click');
    await flushPromises();
    const call = requestMock.mock.calls.find(([url, init]) => String(url).endsWith('/settings') && init?.method === 'PUT');
    const body = JSON.parse(String(call?.[1]?.body));
    expect(body.changes).toEqual([expect.objectContaining({
      parameterId: 'ctl00_PageContent_chkWhatsAppOrderNotification', expectedValue: null, value: false,
    })]);
    wrapper.unmount();
  });

  it('allows explicit zero for an unconfigured extension bitmask', async () => {
    testWrites = true;
    const wrapper = await connectedApp();
    await wrapper.find('input[type="search"]').setValue('ParameterMenu2');
    const emptySelection = wrapper.find('.setting-card button');
    expect(emptySelection.exists()).toBe(true);
    await emptySelection.trigger('click');
    await wrapper.find('.save-bar .button-primary').trigger('click');
    await flushPromises();
    const call = requestMock.mock.calls.find(([url, init]) => String(url).endsWith('/settings') && init?.method === 'PUT');
    const body = JSON.parse(String(call?.[1]?.body));
    expect(body.changes).toEqual([expect.objectContaining({
      parameterId: 'ctl00_PageContent_ddlParameterMenu2', expectedValue: null, bitOperations: [],
    })]);
    wrapper.unmount();
  });

  it('keeps the full 98-setting catalog and original control kinds', () => {
    expect(allSettings).toHaveLength(98);
    expect(new Set(allSettings.map((setting) => setting.htmlControlId)).size).toBe(98);
    expect(allSettings.some((setting) => setting.controlType === 'file')).toBe(true);
    expect(allSettings.some((setting) => setting.controlType === 'bitmask')).toBe(true);
  });

  it('matches every option entry to the source inventory and changes presentation labels only', () => {
    const sourceSettings = rawInventory.groups.flatMap((group) => group.controls) as SourceControl[];
    expect(sourceSettings).toHaveLength(98);
    let translatedOptionCount = 0;
    let totalOptionCount = 0;
    const translatedOriginalLabels = new Set<string>();
    const sourceOptionIndex = new Map<string, string>();
    for (const setting of allSettings) {
      const source = sourceSettings.find((entry) => entry.controlId === setting.parameterId);
      expect(source).toBeDefined();
      expect(Boolean(source?.multiple)).toBe(setting.multiple);
      expect(source?.type).toBe(setting.controlType === 'bitmask' ? 'select' : setting.controlType);
      const sourceOptions = source?.options ?? source?.radioOptions ?? [];
      if (setting.parameterId === 'ctl00_PageContent_ddlParameterModules') {
        expect(sourceOptions).toEqual([]);
        expect(setting.options).toEqual([{ value: '1', label: 'Show Order Export Button' }]);
      } else {
        expect(sourceOptions.map((option) => option.value)).toEqual(setting.options.map((option) => option.value));
        if (sourceOptions.every((option) => typeof option.label === 'string')) {
          expect(sourceOptions.map((option) => option.label)).toEqual(setting.options.map((option) => option.label));
        }
      }
      for (const option of setting.options) {
        totalOptionCount++;
        sourceOptionIndex.set(`${setting.parameterId}\u0000${option.value}`, option.label);
        const translated = turkishOptionLabels[setting.parameterId]?.[option.value];
        if (!translated) continue;
        translatedOptionCount++;
        translatedOriginalLabels.add(option.label);
        expect(translated).not.toBe(option.label);
        expect(optionDisplayLabel(setting.parameterId, option)).toBe(translated);
        if (setting.parameterId === 'ctl00_PageContent_ddlParameterModules') {
          expect(option.value).toBe('1');
        } else {
          expect(option.value).toBe(sourceOptions.find((entry) => entry.value === option.value)?.value);
        }
      }
    }
    expect(totalOptionCount).toBe(813);
    expect(translatedOptionCount).toBe(643);
    expect(translatedOriginalLabels.size).toBe(494);
    for (const [parameterId, byValue] of Object.entries(turkishOptionLabels)) {
      for (const [value, displayLabel] of Object.entries(byValue)) {
        const sourceLabel = sourceOptionIndex.get(`${parameterId}\u0000${value}`);
        expect(sourceLabel, `unexpected translation key ${parameterId}/${value}`).toBeDefined();
        expect(displayLabel).not.toBe(sourceLabel);
      }
    }

    const exceptionKeys = new Set(untranslatedOptionExceptions.map(({ parameterId, value }) => `${parameterId}\u0000${value}`));
    for (const exception of untranslatedOptionExceptions) {
      expect(sourceOptionIndex.get(`${exception.parameterId}\u0000${exception.value}`)).toBe(exception.originalLabel);
      expect(exception.reason.length).toBeGreaterThan(20);
    }
    const alreadyTurkishAsciiOrTechnical = new Set(['Normal', 'Manuel', 'Mobil telefon', 'Akordiyon Kategori', 'Kategori filtresi', 'Model', 'lei', 'TL', 'MD5']);
    for (const setting of allSettings) {
      for (const option of setting.options) {
        const key = `${setting.parameterId}\u0000${option.value}`;
        const looksEnglish = /[A-Za-z]{2,}/.test(option.label)
          && !/[çğıöşüÇĞİÖŞÜ]/.test(option.label)
          && !/^(?:[A-Z]{3}|Design \d+|Icon \d+|\d+)$/.test(option.label)
          && !alreadyTurkishAsciiOrTechnical.has(option.label);
        if (looksEnglish && !turkishOptionLabels[setting.parameterId]?.[option.value]) {
          expect(exceptionKeys.has(key), `untranslated English option ${setting.parameterId}/${option.value}: ${option.label}`).toBe(true);
        }
      }
    }
  });

  it('searches option labels in both Turkish and their original English form', async () => {
    const wrapper = await connectedApp();
    await wrapper.find('input[type="search"]').setValue('First Name');
    expect(wrapper.text()).toContain('Başvuru Formu Ayarları');
    expect(wrapper.text()).toContain('Başvuru Formu Menü Görünümü');
    await wrapper.find('input[type="search"]').setValue('Ad');
    expect(wrapper.text()).toContain('Başvuru Formu Ayarları');
    expect(wrapper.text()).toContain('Başvuru Formu Menü Görünümü');
    const appField = allSettings.find((setting) => setting.parameterId === 'ctl00_PageContent_ddlParameterLogin')!;
    const nameOption = appField.options.find((option) => option.value === '1')!;
    expect(optionSearchLabels(appField.parameterId, nameOption)).toEqual(['Ad', 'First Name']);
    wrapper.unmount();
  });

  it('finds settings by purpose text through the single global search', async () => {
    const wrapper = await connectedApp();
    expect(wrapper.find('.topbar-search').exists()).toBe(true);
    await wrapper.find('input[type="search"]').setValue('Logo');
    expect((wrapper.find('input[type="search"]').element as HTMLInputElement).value).toBe('Logo');
    expect(wrapper.text()).toContain('Koyu Logo');
    expect(wrapper.text()).toContain('Açık Logo');

    await wrapper.find('input[type="search"]').setValue('browser tab');
    expect(wrapper.text()).toContain('Site Simgesi');
    expect(wrapper.findAll('.setting-card')).toHaveLength(1);

    await wrapper.find('input[type="search"]').setValue('odeme');
    expect(wrapper.text()).toContain('Ödeme');
    expect(wrapper.findAll('.setting-card').length).toBeGreaterThan(0);
    wrapper.unmount();
  });

  it('focuses quick search with Ctrl+K and clears it with Escape', async () => {
    const wrapper = await connectedApp(document.body);
    const searchInput = wrapper.find('input[type="search"]');
    window.dispatchEvent(new KeyboardEvent('keydown', { key: 'k', ctrlKey: true, bubbles: true }));
    expect(document.activeElement).toBe(searchInput.element);
    await searchInput.setValue('Product Search');
    searchInput.element.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape', bubbles: true }));
    expect((searchInput.element as HTMLInputElement).value).toBe('');
    wrapper.unmount();
  });

  it('shows the Turkish label for a selected value while keeping the source value selected', async () => {
    const loaded = allSettings.map((setting) => ({
      parameterId: setting.parameterId,
      value: setting.parameterId === 'ctl00_PageContent_ddlSiteStatus' ? '1' : null,
      mappingStatus: setting.mapping.status,
      editable: false,
    } satisfies SettingItem));
    requestMock.mockImplementation(async () => new Response(JSON.stringify({ mode: 'SqlServer', items: loaded, testWritesEnabled: false } satisfies SettingsResponse), { status: 200 }));
    const wrapper = await connectedApp();
    await wrapper.find('input[type="search"]').setValue('Site State');
    const select = wrapper.find('.setting-value select');
    expect((select.element as HTMLSelectElement).value).toBe('1');
    expect((select.element as HTMLSelectElement).selectedOptions[0]?.textContent).toBe('Aktif');
    wrapper.unmount();
  });

  it('preserves bitmask selection when translated option labels are rendered', async () => {
    const items = allSettings.map((setting) => ({
      parameterId: setting.parameterId,
      value: setting.parameterId === 'ctl00_PageContent_ddlOrderParameter' ? '3' : null,
      mappingStatus: setting.mapping.status,
      editable: false,
    }));
    requestMock.mockImplementation(async () => new Response(JSON.stringify({ mode: 'SqlServer', items, testWritesEnabled: false } satisfies SettingsResponse), { status: 200 }));
    const wrapper = await connectedApp();
    await wrapper.find('input[type="search"]').setValue('Request Contract Approval');
    const requestApproval = wrapper.findAll('.setting-card .editable-option').find((node) => node.text().includes('Sözleşme onayı iste'));
    expect(requestApproval?.exists()).toBe(true);
    await wrapper.find('input[type="search"]').setValue('ParameterOrder');
    const selectedOptions = wrapper.findAll('.setting-card .editable-option input[type="checkbox"]')
      .filter((input) => (input.element as HTMLInputElement).checked)
      .map((input) => (input.element as HTMLInputElement).value);
    expect(selectedOptions).toEqual(expect.arrayContaining(['1', '2']));
    expect(items.find((item) => item.parameterId === 'ctl00_PageContent_ddlOrderParameter')?.value).toBe('3');
    wrapper.unmount();
  });

  it('shows Turkish loading and connected states', async () => {
    const pending: Array<() => void> = [];
    requestMock.mockImplementation((input: RequestInfo | URL) => new Promise<Response>((resolve) => {
      const isCapabilities = String(input).endsWith('/settings/capabilities');
      pending.push(() => resolve(new Response(JSON.stringify(isCapabilities
        ? { canSaveSettings: false, canManageApplicationAssets: false, environment: 'ReadOnly', target: null }
        : { mode: 'SqlServer', items: [], testWritesEnabled: false }), { status: 200 })));
    }));
    const wrapper = mount(App);
    await wrapper.find('.connect-panel button').trigger('click');
    expect(wrapper.find('.connect-panel button').text()).toBe('Bağlanıyor…');
    expect(wrapper.find('.mode-pill').text()).toContain('Bağlı değil');
    pending.forEach((resolve) => resolve());
    await flushPromises();
    expect(wrapper.find('.mode-pill').text()).toContain('Bağlandı');
    wrapper.unmount();
  });

  it('explains API connection failures in Turkish without exposing low-level error details', async () => {
    requestMock.mockRejectedValue(new TypeError('socket detail must not be shown'));
    const wrapper = mount(App);
    await wrapper.find('.connect-panel button').trigger('click');
    await flushPromises();
    expect(wrapper.find('.mode-pill').text()).toContain('Bağlı değil');
    expect(wrapper.find('.message-error').text()).toContain('Yerel API’ye ulaşılamadı');
    expect(wrapper.find('.message-error').text()).not.toContain('socket detail');
    expect(wrapper.find('.empty-state').exists()).toBe(true);
    wrapper.unmount();
  });

  async function connectedApp(attachTo?: Element) {
    const currentImplementation = requestMock.getMockImplementation();
    requestMock.mockImplementation((input: RequestInfo | URL, init?: RequestInit) => {
      if (String(input).endsWith('/settings/capabilities')) {
        return Promise.resolve(new Response(JSON.stringify(testWrites
          ? { canSaveSettings: true, canManageApplicationAssets: true, environment: 'IsolatedLocalDemo', target: 'Store5_Settings_Test / store5' }
          : { canSaveSettings: false, canManageApplicationAssets: false, environment: 'ReadOnly', target: null }), { status: 200 }));
      }
      return currentImplementation!(input, init) as Promise<Response>;
    });
    const wrapper = mount(App, attachTo ? { attachTo } : undefined);
    await wrapper.find('.connect-panel button').trigger('click');
    await flushPromises();
    return wrapper;
  }

  it('offers the requested task categories and navigates between them', async () => {
    const expected = ['Design', 'Menu', 'Products', 'Product Details', 'Search', 'Cart', 'Orders', 'Payments', 'Accounts', 'Admin', 'General'];
    expect(categories.map((category) => category.name)).toEqual(expected);
    const wrapper = await connectedApp();
    const menu = wrapper.findAll('.category-nav-item').find((button) => button.text().includes('Menü'));
    await menu?.trigger('click');
    expect(wrapper.find('.section-sidebar-heading h2').text()).toBe('Menü');
    expect((wrapper.find('input[type="search"]').element as HTMLInputElement).value).toBe('');
    wrapper.unmount();
  });

  it('keeps authorization server-side and reports the backend capability state', async () => {
    const wrapper = await connectedApp();
    expect(wrapper.find('.mode-pill').text()).toContain('Bağlandı');
    expect(wrapper.find('.save-bar .button-primary').exists()).toBe(false);
    for (const [, init] of requestMock.mock.calls) {
      const headers = init?.headers as Record<string, string> | undefined;
      expect(headers?.Authorization).toBeUndefined();
      expect(headers?.authorization).toBeUndefined();
    }
    wrapper.unmount();
  });

  it('searches globally across categories and shows the result category', async () => {
    const wrapper = await connectedApp();
    const design = wrapper.findAll('.category-nav-item').find((button) => button.text().includes('Design'));
    await design?.trigger('click');
    await wrapper.find('input[type="search"]').setValue('ddlParameterAdmin');
    expect(wrapper.text()).toContain('Yönetici Seçenekleri');
    expect(wrapper.text()).toContain('ctl00_PageContent_ddlParameterAdmin');
    expect(wrapper.find('.group-heading h2').text()).toBe('Yönetim');
    expect(wrapper.findAll('.setting-card')).toHaveLength(1);
    wrapper.unmount();
  });

  it('uses a two-level settings navigation and filters by the selected section', async () => {
    const wrapper = await connectedApp();
    const design = wrapper.findAll('.category-nav-item').find((button) => button.text().includes('Tasarım'))!;
    await design.trigger('click');
    expect(wrapper.find('.section-sidebar').exists()).toBe(true);
    expect(wrapper.findAll('.section-nav-item').length).toBeGreaterThan(1);
    const colors = wrapper.findAll('.section-nav-item').find((button) => button.text().includes('Renkler'))!;
    await colors.trigger('click');
    expect(wrapper.text()).toContain('Yerleşim Rengi');
    expect(wrapper.text()).not.toContain('Koyu Logo');
    await wrapper.find('input[type="search"]').setValue('Koyu Logo');
    expect(wrapper.find('.section-sidebar').exists()).toBe(false);
    expect(wrapper.text()).toContain('Koyu Logo');
    wrapper.unmount();
  });

  it('keeps technical mapping and raw values collapsed until requested', async () => {
    const wrapper = await connectedApp();
    await wrapper.find('input[type="search"]').setValue('UseFiltering');
    const card = wrapper.find('.setting-card');
    const details = card.find('details.technical-details');
    expect((details.element as HTMLDetailsElement).open).toBe(false);
    expect(details.find('summary').text()).toBe('Teknik bilgiler');
    expect(card.find('.setting-title-row .setting-read-only').exists()).toBe(false);
    expect(card.find('.help-text').text()).toContain('etkileri doğrulanmamıştır');
    expect(card.find('.mapping-badge').exists()).toBe(false);
    await details.find('summary').trigger('click');
    expect((details.element as HTMLDetailsElement).open).toBe(true);
    expect(details.text()).toContain('Schema-compatible · save unknown');
    expect(details.text()).toContain('Ham SQL değeri12');

    await wrapper.find('input[type="search"]').setValue('ParameterAdmin');
    const adminDetails = wrapper.find('.setting-card details.technical-details');
    await adminDetails.find('summary').trigger('click');
    expect(adminDetails.text()).toContain('4611686018427387904');
    expect(wrapper.find('.save-bar .button-primary').exists()).toBe(false);
    expect(wrapper.findAll('.setting-card input[type="checkbox"]').length).toBeGreaterThan(0);
    wrapper.unmount();
  });

  it('shows the original control type for every setting while limiting persistence to the test allowlist', async () => {
    testWrites = true;
    const wrapper = await connectedApp();
    expect(wrapper.find('.mode-pill').text()).toContain('Bağlandı');
    expect(wrapper.find('.message-demo').exists()).toBe(false);
    expect(wrapper.text()).not.toMatch(/test ortamı|test veritabanı|Store5_Settings_Test|test environment/i);
    await wrapper.find('input[type="search"]').setValue('ctl00_PageContent_ddlSiteStatus');
    expect(wrapper.find('#setting-site-status').exists()).toBe(true);
    await wrapper.find('input[type="search"]').setValue('ParameterOrder');
    expect(wrapper.findAll('.editable-options input[type="checkbox"]').length).toBeGreaterThan(0);
    await wrapper.find('input[type="search"]').setValue('ddlLayout');
    expect(wrapper.find('#setting-layout').exists()).toBe(true);
    expect(wrapper.find('#setting-layout').text()).toContain('Sepete Ekle düğmesini');
    await wrapper.find('input[type="search"]').setValue('DontShowMenuAtDealers');
    expect(wrapper.find('#setting-menu-visibility').exists()).toBe(true);
    expect(wrapper.find('#setting-menu-visibility').text()).toContain('Gizle');
    await wrapper.find('input[type="search"]').setValue('DeliveryDateShow');
    expect(wrapper.find('.setting-value input[type="checkbox"]').exists()).toBe(true);
    await wrapper.find('input[type="search"]').setValue('UseFiltering');
    expect(wrapper.find('.setting-editable').exists()).toBe(false);
    expect(wrapper.findAll('.editable-options input[type="checkbox"]').length).toBe(4);
    wrapper.unmount();
  });

  it('saves only the newly verified scalar and Boolean controls in test mode', async () => {
    testWrites = true;
    const testItems = allSettings.map((setting) => ({
      parameterId: setting.parameterId,
      value: setting.parameterId === 'ctl00_PageContent_ddlCategoryMenu' ? '1'
        : setting.parameterId === 'ctl00_PageContent_chkAddCartAction' ? true : null,
      mappingStatus: setting.mapping.status,
      editable: false,
    } satisfies SettingItem));
    requestMock.mockImplementation(async (_input: RequestInfo | URL, init?: RequestInit) => init?.method === 'PUT'
      ? new Response(JSON.stringify({ committed: true }), { status: 200 })
      : new Response(JSON.stringify({ mode: 'SqlServer', items: testItems, testWritesEnabled: true } satisfies SettingsResponse), { status: 200 }));
    const wrapper = await connectedApp();
    await wrapper.find('input[type="search"]').setValue('Category Menu');
    await wrapper.find('.setting-value select').setValue('2');
    await wrapper.find('input[type="search"]').setValue('AddCartAction');
    await wrapper.find('.setting-value input[type="checkbox"]').setValue(false);
    expect(wrapper.find('.save-bar').text()).toContain('2 değişiklik uygulanabilir');
    await wrapper.find('.save-bar .button-primary').trigger('click');
    await flushPromises();
    const call = requestMock.mock.calls.find(([url, init]) => String(url).endsWith('/settings') && init?.method === 'PUT');
    const body = JSON.parse(String(call?.[1]?.body));
    expect(body.changes).toEqual(expect.arrayContaining([
      expect.objectContaining({ parameterId: 'ctl00_PageContent_ddlCategoryMenu', expectedValue: '1', value: '2' }),
      expect.objectContaining({ parameterId: 'ctl00_PageContent_chkAddCartAction', expectedValue: true, value: false }),
    ]));
    wrapper.unmount();
  });

  it('posts only changed test fields, keeps bigint values strings, and refreshes after save', async () => {
    testWrites = true;
    const items: SettingItem[] = allSettings.map((setting) => ({
      parameterId: setting.parameterId,
      value: setting.parameterId === 'ctl00_PageContent_ddlSiteStatus' ? '1'
        : setting.parameterId === 'ctl00_PageContent_ddlOrderParameter' ? '9223372036854775000'
          : setting.parameterId === 'ctl00_PageContent_ddlProductTableStockType' ? '5'
            : setting.parameterId === 'ctl00_PageContent_ddlParameterStock' ? '1'
              : setting.parameterId === 'ctl00_PageContent_ddlLayout' ? '1'
                : setting.parameterId === 'ctl00_PageContent_ddlDontShowMenuAtDealers' ? '1'
                  : setting.parameterId === 'ctl00_PageContent_chkDeliveryDateShow' ? true : null,
      mappingStatus: setting.mapping.status,
      editable: false,
    }));
    requestMock.mockImplementation(async (_input: RequestInfo | URL, init?: RequestInit) => {
      if (init?.method === 'PUT') return new Response(JSON.stringify({ committed: true }), { status: 200 });
      return new Response(JSON.stringify({ mode: 'SqlServer', items, testWritesEnabled: true } satisfies SettingsResponse), { status: 200 });
    });
    const wrapper = await connectedApp();
    await wrapper.find('input[type="search"]').setValue('Site State');
    await wrapper.find('#setting-site-status').setValue('0');
    await wrapper.find('input[type="search"]').setValue('ParameterOrder');
    const orderOption = wrapper.findAll('.editable-options input[type="checkbox"]').find((input) => (input.element as HTMLInputElement).value === '1');
    await orderOption?.setValue(true);
    await wrapper.find('input[type="search"]').setValue('ProductTableStockType');
    await wrapper.find('.setting-value select').setValue('4');
    await wrapper.find('input[type="search"]').setValue('ParameterStock');
    const stockOption = wrapper.findAll('.editable-options input[type="checkbox"]').find((input) => (input.element as HTMLInputElement).value === '2');
    await stockOption?.setValue(true);
    expect((wrapper.find('.save-bar .button-primary').element as HTMLButtonElement).disabled, wrapper.find('.save-bar').text()).toBe(false);
    await wrapper.find('.save-bar .button-primary').trigger('click');
    await flushPromises();
    const saveCall = requestMock.mock.calls.find(([url, init]) => String(url).endsWith('/settings') && init?.method === 'PUT');
    expect(saveCall).toBeDefined();
    expect(saveCall?.[1]?.method).toBe('PUT');
    const requestBody = JSON.parse(String(saveCall?.[1]?.body));
    expect(requestBody.siteStatus).toBeUndefined();
    expect(requestBody.parameterOrder).toBeUndefined();
    expect(requestBody.changes).toEqual(expect.arrayContaining([
      expect.objectContaining({ parameterId: 'ctl00_PageContent_ddlSiteStatus', expectedValue: '1', value: '0' }),
      expect.objectContaining({ parameterId: 'ctl00_PageContent_ddlOrderParameter', expectedValue: '9223372036854775000', bitOperations: [{ optionValue: '1', enabled: true }] }),
      expect.objectContaining({ parameterId: 'ctl00_PageContent_ddlProductTableStockType', expectedValue: '5', value: '4' }),
      expect.objectContaining({ parameterId: 'ctl00_PageContent_ddlParameterStock', expectedValue: '1', bitOperations: [{ optionValue: '2', enabled: true }] }),
    ]));
    expect(requestMock.mock.calls.some(([url, init]) => String(url).endsWith('/settings') && init?.method === 'PUT')).toBe(true);
    expect(wrapper.text()).toContain('yeniden doğrulandı');
    wrapper.unmount();
  });

  it('allows ProductCompany only as a test-mode scalar and sends explicit NULL concurrency state', async () => {
    testWrites = true;
    const wrapper = await connectedApp();
    await wrapper.find('input[type="search"]').setValue('B2B & B2C Partners');
    const control = wrapper.find('.setting-card select');
    expect(control.exists()).toBe(true);
    expect((control.element as HTMLSelectElement).value).toBe('');
    expect(wrapper.find('.message-demo').exists()).toBe(false);
    await control.setValue('1');
    expect(wrapper.find('.save-bar').exists()).toBe(true);
    await wrapper.find('.save-bar .button-primary').trigger('click');
    const saveCall = requestMock.mock.calls.find(([url, init]) => String(url).endsWith('/settings') && init?.method === 'PUT');
    expect(saveCall).toBeDefined();
    const requestBody = JSON.parse(String(saveCall?.[1]?.body));
    expect(requestBody.changes).toEqual([expect.objectContaining({
      parameterId: 'ctl00_PageContent_ddlCommonSystem',
      expectedValue: null,
      value: '1',
    })]);
    wrapper.unmount();
  });

  it('edits Schedule as its single SQL scalar option without sending generic bitmask values', async () => {
    testWrites = true;
    const wrapper = await connectedApp();
    await wrapper.find('input[type="search"]').setValue('Automatic Definition');
    const option = wrapper.find('.setting-card input[type="checkbox"]');
    expect(option.exists()).toBe(true);
    expect(wrapper.text()).toContain('Ürün içe aktarma davranışını etkiler');
    await option.setValue(true);
    await wrapper.find('.save-bar .button-primary').trigger('click');
    const saveCall = requestMock.mock.calls.find(([url, init]) => String(url).endsWith('/settings') && init?.method === 'PUT');
    const requestBody = JSON.parse(String(saveCall?.[1]?.body));
    expect(requestBody.changes).toEqual([expect.objectContaining({
      parameterId: 'ctl00_PageContent_ddlSchedule',
      expectedValue: null,
      bitOperations: [{ optionValue: '1', enabled: true }],
    })]);
    expect(requestBody.changes[0].value).toBeUndefined();
    wrapper.unmount();
  });

  it('presents image controls as first-class management fields and uploads them through the asset service', async () => {
    testWrites = true;
    requestMock.mockImplementation(async (input: RequestInfo | URL, init?: RequestInit) => {
      const url = String(input);
      if (url.includes('/application-assets/') && init?.method === 'POST') {
        return new Response(JSON.stringify({ metadata: {
          parameterId: 'ctl00_PageContent_imgLogo_imageUpload', storedFileName: 'generated.png', contentType: 'image/png', length: 8,
        }, applicationOnly: true, legacyPortalCompatible: false }), { status: 200 });
      }
      if (url.endsWith('/settings/capabilities')) return new Response(JSON.stringify({ canSaveSettings: true, canManageApplicationAssets: true, environment: 'IsolatedLocalDemo', target: 'Store5_Settings_Test / store5' }), { status: 200 });
      if (url.endsWith('/settings') && init?.method === 'PUT') return new Response(JSON.stringify({ committed: true }), { status: 200 });
      if (url.endsWith('/application-assets')) return new Response(JSON.stringify({ assets: {}, applicationOnly: true, legacyPortalCompatible: false }), { status: 200 });
      return new Response(JSON.stringify({ mode: 'SqlServer', items: allSettings.map((setting) => ({
        parameterId: setting.parameterId, value: null, mappingStatus: setting.mapping.status, editable: false,
      })), testWritesEnabled: true } satisfies SettingsResponse), { status: 200 });
    });
    const wrapper = await connectedApp();
    expect(wrapper.find('.section-sidebar').exists()).toBe(true);
    await wrapper.find('input[type="search"]').setValue('Logo (Dark)');
    expect(wrapper.find('input[type="file"]').exists()).toBe(true);
    expect(wrapper.text()).toContain('Bu görsel yeni yönetim panelinde kullanılır.');
    expect(wrapper.find('.save-bar').exists()).toBe(false);
    const fileInput = wrapper.find('input[type="file"]');
    const imageFile = new File(['image'], 'dealer-logo.png', { type: 'image/png' });
    Object.defineProperty(fileInput.element, 'files', { value: { item: (index: number) => index === 0 ? imageFile : null } });
    await fileInput.trigger('change');
    await wrapper.find('.asset-actions .button-primary').trigger('click');
    await flushPromises();
    expect(requestMock.mock.calls.some(([url, init]) => String(url).includes('/application-assets/') && init?.method === 'POST')).toBe(true);
    expect(wrapper.text()).toContain('Görsel bu yönetim paneline kaydedildi.');
    wrapper.unmount();
  });

  it('keeps an unset allowlisted value read-only so exact restoration remains possible', async () => {
    testWrites = true;
    requestMock.mockImplementation(async (_input: RequestInfo | URL, init?: RequestInit) => init?.method === 'PUT'
      ? new Response(JSON.stringify({ committed: true }), { status: 200 })
      : new Response(JSON.stringify({ mode: 'SqlServer', items: allSettings.map((setting) => ({ parameterId: setting.parameterId, value: null, mappingStatus: setting.mapping.status, editable: false })), testWritesEnabled: true } satisfies SettingsResponse), { status: 200 }));
    const wrapper = await connectedApp();
    await wrapper.find('input[type="search"]').setValue('ctl00_PageContent_ddlSiteStatus');
    expect(wrapper.find('.setting-card').exists()).toBe(true);
    expect(wrapper.find('#setting-site-status').exists()).toBe(false);
    expect(wrapper.find('.setting-card .setting-read-only').exists()).toBe(true);
    await wrapper.find('input[type="search"]').setValue('ddlLayout');
    expect(wrapper.find('#setting-layout').exists()).toBe(false);
    await wrapper.find('input[type="search"]').setValue('DontShowMenuAtDealers');
    expect(wrapper.find('#setting-menu-visibility').exists()).toBe(false);
    await wrapper.find('input[type="search"]').setValue('DeliveryDateShow');
    expect(wrapper.find('.setting-value input[type="checkbox"]').exists()).toBe(true);
    expect(wrapper.find('.save-bar').exists()).toBe(false);
    wrapper.unmount();
  });

  it('keeps image drafts in their own upload flow instead of the settings save bar', async () => {
    testWrites = true;
    const wrapper = await connectedApp();
    await wrapper.find('input[type="search"]').setValue('Logo (Dark)');
    const file = new File(['safe test draft'], 'logo.png', { type: 'image/png' });
    const input = wrapper.find('.setting-value input[type="file"]');
    Object.defineProperty(input.element, 'files', { configurable: true, value: { item: (index: number) => index === 0 ? file : null } });
    await input.trigger('change');
    expect(wrapper.find('.asset-preview').attributes('src')).toBe('blob:test-preview');
    expect(wrapper.find('.save-bar').exists()).toBe(false);
    expect(wrapper.find('.asset-actions .button-primary').text()).toBe('Görseli kaydet');
    expect(requestMock.mock.calls.some(([url, init]) => String(url).endsWith('/settings') && init?.method === 'PUT')).toBe(false);
    wrapper.unmount();
  });

  it('previews and removes an application-owned asset using its isolated API route', async () => {
    testWrites = true;
    requestMock.mockImplementation(async (input: RequestInfo | URL, init?: RequestInit) => {
      const url = String(input);
      if (url.endsWith('/settings/capabilities')) return new Response(JSON.stringify({ canSaveSettings: true, canManageApplicationAssets: true, environment: 'IsolatedLocalDemo', target: 'Store5_Settings_Test / store5' }), { status: 200 });
      if (url.endsWith('/application-assets')) return new Response(JSON.stringify({ assets: {
        'ctl00_PageContent_imgLogo_imageUpload': { parameterId: 'ctl00_PageContent_imgLogo_imageUpload', storedFileName: 'generated.png', contentType: 'image/png', length: 8 },
      }, applicationOnly: true, legacyPortalCompatible: false }), { status: 200 });
      if (url.includes('/content')) return new Response(new Blob(['image']), { status: 200 });
      if (url.includes('/application-assets/') && init?.method === 'DELETE') return new Response(JSON.stringify({ deleted: true, applicationOnly: true }), { status: 200 });
      return new Response(JSON.stringify({ mode: 'SqlServer', items: allSettings.map((setting) => ({ parameterId: setting.parameterId, value: null, mappingStatus: setting.mapping.status, editable: false })) }), { status: 200 });
    });
    const wrapper = await connectedApp();
    await wrapper.find('input[type="search"]').setValue('Logo');
    expect(wrapper.find('.asset-preview').attributes('src')).toContain('/api/application-assets/');
    await wrapper.find('.setting-card button').trigger('click');
    await flushPromises();
    expect(requestMock.mock.calls.some(([url, init]) => String(url).includes('/application-assets/') && init?.method === 'DELETE')).toBe(true);
    expect(wrapper.text()).toContain('Görsel kaldırıldı');
    wrapper.unmount();
  });

  it('saves setting changes while preserving a separate pending image upload', async () => {
    testWrites = true;
    const wrapper = await connectedApp();
    await wrapper.find('input[type="search"]').setValue('Site State');
    await wrapper.find('#setting-site-status').setValue('0');
    await wrapper.find('input[type="search"]').setValue('Logo (Dark)');
    const file = new File(['safe test draft'], 'logo.png', { type: 'image/png' });
    const input = wrapper.find('.setting-value input[type="file"]');
    Object.defineProperty(input.element, 'files', { configurable: true, value: { item: (index: number) => index === 0 ? file : null } });
    await input.trigger('change');
    expect(wrapper.find('.save-bar').text()).toContain('1 değişiklik uygulanabilir');
    expect(wrapper.find('.save-bar').text()).not.toContain('değişiklik taslak olarak kalır');
    await wrapper.find('.save-bar .button-primary').trigger('click');
    await flushPromises();
    expect(requestMock.mock.calls.some(([url, init]) => String(url).endsWith('/settings') && init?.method === 'PUT')).toBe(true);
    expect(wrapper.text()).toContain('Kaydedilebilir ayarlar gruplar halinde uygulandı');
    expect(wrapper.find('.save-bar').exists()).toBe(false);
    expect(wrapper.find('.asset-actions .button-primary').text()).toBe('Görseli kaydet');
    wrapper.unmount();
  });

  it('sends only changed verified scalar and boolean fields with their declared types', async () => {
    testWrites = true;
    const wrapper = await connectedApp();
    await wrapper.find('input[type="search"]').setValue('ddlLayout');
    await wrapper.find('#setting-layout').setValue('0');
    await wrapper.find('input[type="search"]').setValue('DontShowMenuAtDealers');
    await wrapper.find('#setting-menu-visibility').setValue('0');
    await wrapper.find('input[type="search"]').setValue('DeliveryDateShow');
    await wrapper.find('.setting-value input[type="checkbox"]').setValue(false);
    await wrapper.find('.save-bar .button-primary').trigger('click');
    await flushPromises();
    const saveCall = requestMock.mock.calls.find(([url, init]) => String(url).endsWith('/settings') && init?.method === 'PUT');
    const body = JSON.parse(String(saveCall?.[1]?.body));
    expect(body.changes).toEqual(expect.arrayContaining([
      expect.objectContaining({ parameterId: 'ctl00_PageContent_ddlLayout', expectedValue: '1', value: '0' }),
      expect.objectContaining({ parameterId: 'ctl00_PageContent_ddlDontShowMenuAtDealers', expectedValue: '1', value: '0' }),
      expect.objectContaining({ parameterId: 'ctl00_PageContent_chkDeliveryDateShow', expectedValue: true, value: false }),
    ]));
    expect(body.siteStatus).toBeUndefined();
    expect(body.parameterOrder).toBeUndefined();
    expect(saveCall?.[1]?.method).toBe('PUT');
    wrapper.unmount();
  });

  it('edits the three verified numeric text settings and sends decimal values as strings through the settings API', async () => {
    testWrites = true;
    const items: SettingItem[] = allSettings.map((setting) => ({
      parameterId: setting.parameterId,
      value: setting.parameterId === 'ctl00_PageContent_txtMinCartCount' ? '0'
        : setting.parameterId === 'ctl00_PageContent_txtMinCartTotal' ? '12.30'
          : setting.parameterId === 'ctl00_PageContent_txtMaximumOrderQuantity' ? '100' : null,
      mappingStatus: setting.mapping.status,
      editable: false,
    }));
    requestMock.mockImplementation(async (_input: RequestInfo | URL, init?: RequestInit) => init?.method === 'PUT'
      ? new Response(JSON.stringify({ committed: true }), { status: 200 })
      : new Response(JSON.stringify({ mode: 'SqlServer', items, testWritesEnabled: true } satisfies SettingsResponse), { status: 200 }));
    const wrapper = await connectedApp();
    await wrapper.find('input[type="search"]').setValue('Total Minimum Cart');
    expect(wrapper.find('#draft-ctl00_PageContent_txtMinCartCount').exists()).toBe(true);
    await wrapper.find('#draft-ctl00_PageContent_txtMinCartCount').setValue('7');
    await wrapper.find('input[type="search"]').setValue('Min Cart Price');
    await wrapper.find('#draft-ctl00_PageContent_txtMinCartTotal').setValue('15.55');
    await wrapper.find('input[type="search"]').setValue('Maximum Product Order Quantity');
    await wrapper.find('#draft-ctl00_PageContent_txtMaximumOrderQuantity').setValue('200');
    expect(wrapper.find('.save-bar').text()).toContain('3 değişiklik uygulanabilir');
    await wrapper.find('.save-bar .button-primary').trigger('click');
    await flushPromises();
    const call = requestMock.mock.calls.find(([url, init]) => String(url).endsWith('/settings') && init?.method === 'PUT');
    const body = JSON.parse(String(call?.[1]?.body));
    expect(body.changes).toEqual(expect.arrayContaining([
      expect.objectContaining({ parameterId: 'ctl00_PageContent_txtMinCartCount', expectedValue: '0', value: '7' }),
      expect.objectContaining({ parameterId: 'ctl00_PageContent_txtMinCartTotal', expectedValue: '12.30', value: '15.55' }),
      expect.objectContaining({ parameterId: 'ctl00_PageContent_txtMaximumOrderQuantity', expectedValue: '100', value: '200' }),
    ]));
    wrapper.unmount();
  });

  it('sends verified text scalars as strings and preserves SQL bit dropdown representation', async () => {
    testWrites = true;
    const values: Record<string, string | boolean | null> = {
      'ctl00_PageContent_txtLayoutColor': '#AABBCC',
      'ctl00_PageContent_ddlCurrencyType': false,
      'ctl00_PageContent_ddlintegrated': true,
      'ctl00_PageContent_chkDoNotSendMailToCustomer': '0',
    };
    const items: SettingItem[] = allSettings.map((setting) => ({
      parameterId: setting.parameterId, value: values[setting.parameterId] ?? null,
      mappingStatus: setting.mapping.status, editable: false,
    }));
    requestMock.mockImplementation(async (_input: RequestInfo | URL, init?: RequestInit) => init?.method === 'PUT'
      ? new Response(JSON.stringify({ committed: true }), { status: 200 })
      : new Response(JSON.stringify({ mode: 'SqlServer', items, testWritesEnabled: true } satisfies SettingsResponse), { status: 200 }));
    const wrapper = await connectedApp();
    await wrapper.find('input[type="search"]').setValue('Layout Color');
    expect((wrapper.find('#draft-ctl00_PageContent_txtLayoutColor').element as HTMLInputElement).value).toBe('#AABBCC');
    await wrapper.find('#draft-ctl00_PageContent_txtLayoutColor').setValue('#112233');
    await wrapper.find('input[type="search"]').setValue('Currency Type');
    expect((wrapper.find('#draft-ctl00_PageContent_ddlCurrencyType').element as HTMLSelectElement).value).toBe('0');
    await wrapper.find('#draft-ctl00_PageContent_ddlCurrencyType').setValue('1');
    await wrapper.find('input[type="search"]').setValue('Do Not Send Mail To Customer');
    expect((wrapper.find('.setting-value input[type="checkbox"]').element as HTMLInputElement).checked).toBe(false);
    await wrapper.find('.setting-value input[type="checkbox"]').setValue(true);
    await wrapper.find('.save-bar .button-primary').trigger('click');
    await flushPromises();
    const call = requestMock.mock.calls.find(([url, init]) => String(url).endsWith('/settings') && init?.method === 'PUT');
    const body = JSON.parse(String(call?.[1]?.body));
    expect(body.changes).toEqual(expect.arrayContaining([
      expect.objectContaining({ parameterId: 'ctl00_PageContent_txtLayoutColor', expectedValue: '#AABBCC', value: '#112233' }),
      expect.objectContaining({ parameterId: 'ctl00_PageContent_ddlCurrencyType', expectedValue: false, value: '1' }),
      expect.objectContaining({ parameterId: 'ctl00_PageContent_chkDoNotSendMailToCustomer', expectedValue: '0', value: true }),
    ]));
    wrapper.unmount();
  });

  it('sends one operation for duplicated source values in the menu bitmask', async () => {
    testWrites = true;
    const wrapper = await connectedApp();
    await wrapper.find('input[type="search"]').setValue('ddlParameterMenu');
    const duplicateInputs = wrapper.findAll('.editable-options input[type="checkbox"]')
      .filter((input) => (input.element as HTMLInputElement).value === '4611686018427387904');
    expect(duplicateInputs.length).toBe(2);
    await duplicateInputs[0].setValue(true);
    await wrapper.find('.save-bar .button-primary').trigger('click');
    await flushPromises();
    const saveCall = requestMock.mock.calls.find(([url, init]) => String(url).endsWith('/settings') && init?.method === 'PUT');
    const body = JSON.parse(String(saveCall?.[1]?.body));
    const menuChange = body.changes.find((change: { parameterId: string }) => change.parameterId === 'ctl00_PageContent_ddlParameterMenu');
    expect(menuChange.bitOperations).toEqual([{ optionValue: '4611686018427387904', enabled: true }]);
    wrapper.unmount();
  });
});
