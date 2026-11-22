<script setup lang="ts">
import { computed, onBeforeUnmount, ref } from 'vue';
import { ApiError, applicationAssetContentUrl, deleteApplicationAsset, listApplicationAssets, loadSettings, loadSettingsCapabilities, saveSettings, uploadApplicationAsset, type ApplicationAssetMetadata, type SettingChange, type SettingItem, type SettingValue } from './api';
import { allSettings, categories, catalog, categoryFor, categoryDisplayLabel, categoryEnglishLabel, displayLabel, englishDisplayLabel, helpText, helpTextEnglish, mappingAudit, mappingLabel, type SettingDefinition } from './catalog';
import { isOptionEnabled } from './bitmask';
import { optionDisplayLabel, optionSearchLabels } from './option-labels.tr';

type Language = 'en' | 'tr';
const language = ref<Language>(localStorage.getItem('b2b-settings-language') === 'tr' ? 'tr' : 'en');
const englishUi: Record<string, string> = {
  'AYARLAR': 'SETTINGS', 'Ayar yönetimi': 'Settings management', 'Arama sonuçları': 'Search results', 'Bağlandı': 'Connected', 'Bağlı değil': 'Disconnected',
  'Ayarları arayın': 'Find settings', 'Bayi mağazanızın ayarlarını buradan inceleyip düzenleyin.': 'Review and manage your dealer store settings here.',
  'Bağlanıyor…': 'Connecting…', 'Ayarları yenile': 'Refresh settings', 'Bağlan': 'Connect', 'Toplam ayar': 'Total settings',
  'Yüklenen ayar': 'Loaded settings', 'Salt okunur ayar': 'Read-only settings', 'Ayar özeti': 'Settings summary',
  'Ayarlar yüklenemedi. Lütfen yerel API bağlantısını kontrol edip yeniden deneyin.': 'Settings could not be loaded. Check the local API connection and try again.',
  'Beklenmeyen bir bağlantı hatası oluştu. Yerel API hizmetini kontrol edip yeniden deneyin.': 'An unexpected connection error occurred. Check the local API service and try again.',
  'Ayarlar yüklendi. Değişiklikler yerel taslak olarak kalır; kaydetme kapalıdır.': 'Settings loaded. Changes remain local drafts; saving is disabled.',
  'SQL ayarları yüklendi. Değişiklikler yerel taslak olarak kalır; kaydetme kapalıdır.': 'SQL settings loaded. Changes remain local drafts; saving is disabled.',
  'Demo ayarları yüklendi. Değerler sentetik ve boştur; SQL bağlantısı kullanılmadı. Değişiklikler yerel taslak olarak kalır.': 'Demo settings loaded. Values are synthetic and unset; no SQL connection was used. Changes remain local drafts.',
  'Ayarlar yalıtılmış test veritabanından yüklendi. Kaydedilebilir olarak işaretlenen değişiklikler yalnızca burada uygulanır.': 'Settings loaded from the isolated test database. Changes marked as saveable apply only there.',
  'Yalıtılmış test veritabanı:': 'Isolated test database:', 'kaydedilen değişiklikler yalnızca Store5_Settings_Test veritabanındaki store5 kaydına uygulanır. Diğer değişiklikler taslak olarak kalır.': 'Saved changes apply only to the store5 row in Store5_Settings_Test. Other changes remain drafts.',
  'eşleşen ayar': 'matching settings', ' ayar': ' settings', ' ayarları': ' settings', ' ayar listeleniyor': ' settings shown',
  'Tüm kategorilerde ayar adlarıyla arayın.': 'Search setting names across all categories.', 'Ayarlarda ara...': 'Search settings...',
  'Ayar adında veya teknik adında ara': 'Search by setting or technical name', 'Ayarlar yükleniyor': 'Loading settings',
  'Yapılandırılmış mağaza kaydı okunuyor.': 'Reading the configured store record.', 'Mevcut ayarları görmek için bağlanın': 'Connect to view current settings',
  'Ayar kataloğunu inceleyebilir, Bağlan düğmesiyle yerel kaynaktaki değerleri yükleyebilirsiniz.': 'Browse the catalog, then connect to load values from the local source.',
  'Teknik bilgiler': 'Technical information', 'Orijinal etiket': 'Original label', 'HTML denetim kimliği': 'HTML control ID',
  'HTML denetim adı': 'HTML control name', 'Kategori': 'Category', 'Veritabanı alanı': 'Database field',
  'Eşleşme doğrulanmadı': 'Mapping not confirmed', 'SQL veri türü': 'SQL data type', 'Doğrulanmadı': 'Not verified',
  'Şema uyumluluğu': 'Schema compatibility', 'Eşleme durumu': 'Mapping status', 'Eski kaydetme davranışı': 'Legacy save behavior',
  'Değer kodlaması': 'Value encoding', 'Ham SQL değeri': 'Raw SQL value', 'Kanıt': 'Evidence', 'Açık soru': 'Open question',
  'Test ortamındaki değer': 'Test environment value', 'Mevcut değer': 'Current value', 'Yüklenmedi': 'Not loaded',
  'Değeri yüklemek için bağlanın.': 'Connect to load the value.', 'Aktif': 'Active', 'Pasif': 'Inactive',
  'Yalnızca seçtiğiniz seçenekler değişir.': 'Only the options you select will change.',
  'Yapılandırılmamış': 'Unconfigured',
  'Seçenek seçmeden sıfır değerini kaydet': 'Save a zero value without selected options',
  'Kapalı olarak yapılandır': 'Configure as off',
  'Kaydedilebilir ayarlar gruplar halinde uygulandı; kalan düzenlemeler taslak olarak korundu.': 'Saveable settings were applied in separate groups; remaining edits were kept as drafts.',
  'Kaynak envanterde seçenek listelenmediği için seçim yapılamıyor.': 'No options are listed in the source inventory, so no selection is available.',
  'Bu değişiklik kaydedilene kadar taslak olarak kalır.': 'This change remains a draft until saved.',
  'Kayıt biçimi doğrulanmadı; bu değişiklik kaydedilemez.': 'The storage format is unverified; this change cannot be saved.',
  'Bir seçenek belirleyin': 'Choose an option', 'Dosya seç: ': 'Choose file: ',
  'Dosyanın nasıl kaydedildiği doğrulanmadığından bu değişiklik kaydedilemez.': 'The file storage process is unverified, so this change cannot be saved.',
  'Yeni uygulamaya özel dosya alanı; eski portalda görünmez.': 'Application-only file storage; the legacy portal cannot see it.',
  'Uygulama alanında kayıtlı': 'Stored in this application', 'bayt': 'bytes',
  'Yalıtılmış uygulama alanına yükle': 'Upload to isolated application storage', 'Yükleniyor…': 'Uploading…',
  'Görsel yeni uygulamanın yalıtılmış alanına kaydedildi. Eski bayi portalında kullanılmaz.': 'Image saved to this application’s isolated storage. The legacy dealer portal will not use it.',
  'Görsel kaydedilemedi. Dosya türünü ve boyutunu kontrol edip yeniden deneyin.': 'Image could not be saved. Check the file type and size, then try again.',
  'Uygulama görseli kaldırıldı. Eski portalda kullanılan dosyalar etkilenmedi.': 'Application image removed. Files used by the legacy portal were not affected.',
  'Uygulama görseli kaldırılamadı. Lütfen yeniden deneyin.': 'Application image could not be removed. Try again.',
  'Görsel önizlemesi': 'Image preview', 'Bu uygulamada kayıtlı görsel': 'Image stored in this application',
  'Uygulama görselini kaldır': 'Remove application image', 'Görsel kaldırılıyor…': 'Removing image…', 'Görseli değiştir veya kaldır.': 'Replace or remove the image.',
  'En fazla 5 MiB; PNG, JPEG, WebP veya ICO.': 'Maximum 5 MiB; PNG, JPEG, WebP, or ICO.',
  'Yalıtılmış yerel demo modu etkin. Kaydetme yalnızca Store5_Settings_Test / store5 hedefini kullanır.': 'Isolated local demo mode is active. Saves use only the Store5_Settings_Test / store5 target.',
  'Bu değişiklikler kaydedilmedi. Desteklenmeyen düzenlemeler taslak olarak korunuyor.': 'These changes were not saved. Unsupported edits remain as drafts.',
  'Yerel API kimlik doğrulaması başarısız oldu.': 'Local API authentication failed.',
  'Değer boş veya desteklenmeyen biçimde olduğu için değiştirilemiyor.': 'Cannot edit because the value is empty or has an unsupported format.',
  'Değer boş olduğu için değiştirilemiyor.': 'Cannot edit because the value is empty.',
  'Sonuç bulunamadı': 'No results found', 'Farklı bir ayar adı, kategori veya teknik alan adı deneyin.': 'Try another setting label, category, or technical name.',
  'Kaydedilmemiş değişiklikler': 'Unsaved changes', ' ayarda değişiklik var.': ' settings have changes.',
  ' değişiklik uygulanabilir': ' changes can be applied', '; ': '; ', ' değişiklik taslak olarak kalır.': ' changes will remain drafts.',
  'Değişiklikleri geri al': 'Discard changes', 'Kaydediliyor…': 'Saving…', 'Değişiklikleri uygula': 'Apply changes',
  'Yalnızca doğrulanmış ayarlar yalıtılmış test ortamına kaydedilebilir.': 'Only verified settings can be saved to the isolated test environment.',
  'Kaydetme kapalıdır; değişiklikler yerel taslak olarak kalır.': 'Saving is disabled; changes remain local drafts.',
  'Taslak · kaydetme desteklenmiyor': 'Draft · saving unavailable',
  'Kaydedilmemiş değişiklik': 'Unsaved changes', 'Test ortamında kaydedilebilir': 'Saveable in test mode',
  'Taslak': 'Draft', 'Kaydetme başarısız oldu. Ayarları yenileyip tekrar deneyin.': 'Save failed. Refresh the settings and try again.',
  'Kaydetme sonucu doğrulanamadı. Yeni bir değişiklik yapmadan önce ayarları yenileyin.': 'Save could not be verified. Refresh settings before making another change.',
  'Ayar yüklendikten sonra değişmiş. Yeniden yükleyip kontrol ettikten sonra tekrar deneyin.': 'The setting changed after it was loaded. Refresh, review, and try again.',
  'Değişiklikler test veritabanında kaydedildi ve yeniden okunarak doğrulandı.': 'Changes were saved to the test database and verified by reading them again.',
  'Kaydedilmemiş değişiklikler geri alındı.': 'Unsaved changes were discarded.', 'Belirtilmemiş': 'Not specified',
  'Seçim ayrıntıları teknik bilgiler bölümünde.': 'Selection details are in Technical information.', 'Etkin': 'Enabled', 'Devre dışı': 'Disabled',
  'Site durumu': 'Site status', 'Bayilerde menü görünümü': 'Dealer menu visibility',
  'Kaydedilebilir bir değişiklik bulunmuyor. Bu ayarlar taslak olarak kalır.': 'There are no saveable changes. These settings remain drafts.',
  'Yerel API kimlik doğrulaması başarısız oldu. Yerel API ayarlarını ve Vite proxy yapılandırmasını kontrol edin.': 'Local API authentication failed. Check local API settings and the Vite proxy configuration.',
  'API ayarları okuyamadı. Yerel SQL hizmetini ve Store5_Settings_Test bağlantısını kontrol edin.': 'The API could not read settings. Check the local SQL service and Store5_Settings_Test connection.',
  'Test ortamına kaydetme doğrulanamadı.': 'Saving to the test environment could not be verified.',
  'Bu değişiklik kaydedilemez.': 'This change cannot be saved.',
  'Ayar kategorileri': 'Settings categories', 'Language': 'Dil',
  'Yerel API’ye ulaşılamadı. API hizmetinin 127.0.0.1:5080 adresinde çalıştığını kontrol edin.': 'Could not reach the local API. Check that the API service is running at 127.0.0.1:5080.',
};
function t(text: string): string {
  if (text === 'Language') return language.value === 'en' ? 'Language' : 'Dil';
  return language.value === 'en' ? (englishUi[text] ?? text) : text;
}
function setLanguage(next: Language): void { language.value = next; localStorage.setItem('b2b-settings-language', next); }
function categoryLabel(category: string): string { return language.value === 'en' ? categoryEnglishLabel(category) : categoryDisplayLabel(category); }
function settingLabel(setting: SettingDefinition): string { return language.value === 'en' ? englishDisplayLabel(setting) : displayLabel(setting); }
function settingHelp(setting: SettingDefinition): string { return language.value === 'en' ? helpTextEnglish(setting) : helpText(setting); }
function optionLabel(parameterId: string, option: { value: string; label: string }): string { return language.value === 'en' ? option.label : optionDisplayLabel(parameterId, option); }

const search = ref('');
const selectedCategory = ref('General');
const items = ref<SettingItem[]>([]);
const loading = ref(false);
const error = ref('');
const notice = ref('');
const mode = ref<'Mock' | 'SqlServer' | ''>('');
const canSaveSettings = ref(false);
const canManageApplicationAssets = ref(false);
const applicationAssets = ref<Record<string, ApplicationAssetMetadata>>({});
const assetSavingId = ref('');
const assetDeletingId = ref('');
const saving = ref(false);
const siteStatusDraft = ref('');
const orderDraft = ref<Record<string, boolean>>({});
const baselineSiteStatus = ref<SettingValue>(null);
const baselineParameterOrder = ref<SettingValue>(null);
const siteStatusId = 'ctl00_PageContent_ddlSiteStatus';
const parameterOrderId = 'ctl00_PageContent_ddlOrderParameter';
const layoutId = 'ctl00_PageContent_ddlLayout';
const menuVisibilityId = 'ctl00_PageContent_ddlDontShowMenuAtDealers';
const deliveryDateId = 'ctl00_PageContent_chkDeliveryDateShow';
const productCompanyId = 'ctl00_PageContent_ddlCommonSystem';
const scheduleId = 'ctl00_PageContent_ddlSchedule';
const useFilteringId = 'ctl00_PageContent_ddUseFiltering';
const discountTypeId = 'ctl00_PageContent_ddlDiscountType';
const paymentParametersId = 'ctl00_PageContent_ddlPaymentParameter';
const productSearchId = 'ctl00_PageContent_ddlProductSearchParameter';
const parameterModulesId = 'ctl00_PageContent_ddlParameterModules';
const productDetailNotLogin2Id = 'ctl00_PageContent_ddlParameterProductDetailNotLogin2';
const parameterMenu2Id = 'ctl00_PageContent_ddlParameterMenu2';
const whatsAppOrderNotificationId = 'ctl00_PageContent_chkWhatsAppOrderNotification';
const favoriteIconPositionId = 'ctl00_PageContent_rbFavoriteIconLeft';
const favoriteIconVerticalPositionId = 'ctl00_PageContent_rbFavoriteIconTop';
const extensionSettingIds = new Set([productDetailNotLogin2Id, parameterMenu2Id, whatsAppOrderNotificationId, favoriteIconPositionId, favoriteIconVerticalPositionId]);
const extendedWriteIds = new Set([productSearchId, ...extensionSettingIds]);
const textScalarWriteIds = new Set([
  'ctl00_PageContent_txtProductImageAspectRatio', 'ctl00_PageContent_txtLayoutColor', 'ctl00_PageContent_txtLayoutColor2',
  'ctl00_PageContent_txtGoogleAnalysticScript', 'ctl00_PageContent_txtGoogleAnalysticProfileID',
  'ctl00_PageContent_txtGoogleAdWordsReMarketingCode', 'ctl00_PageContent_txtMinStockFilter',
  'ctl00_PageContent_txtRegisterFormUrl', 'ctl00_PageContent_txtBayiTelefon', 'ctl00_PageContent_txtBayiAdres',
  'ctl00_PageContent_txtPaymentLink', 'ctl00_PageContent_TxtDealerName', 'ctl00_PageContent_txtBayiYetkiliKisi',
  'ctl00_PageContent_txtBayiWebAdres', 'ctl00_PageContent_txtEInvoiceNo',
]);
const bitDropdownWriteIds = new Set([
  'ctl00_PageContent_ddlCurrencyType', 'ctl00_PageContent_ddlExpiryDate',
  'ctl00_PageContent_ddlintegrated', 'ctl00_PageContent_ddlBonusShow',
]);
const booleanIntegerWriteIds = new Set(['ctl00_PageContent_chkDoNotSendMailToCustomer']);
const testWritableIds = new Set([
  siteStatusId, parameterOrderId, layoutId, menuVisibilityId, deliveryDateId,
  useFilteringId, discountTypeId,
  paymentParametersId, productSearchId, parameterModulesId, ...extensionSettingIds,
  'ctl00_PageContent_ddParameterThema', 'ctl00_PageContent_ddlProductTableStockType',
  'ctl00_PageContent_ddlParameterDesign', 'ctl00_PageContent_ddlParameterStock',
  'ctl00_PageContent_ddlProductDetailParameter', 'ctl00_PageContent_ddlProductDetailParameter2',
  'ctl00_PageContent_ddlParameterProductDetailNotLogin', 'ctl00_PageContent_ddlExportProductList',
  'ctl00_PageContent_ddlParameterAccount', 'ctl00_PageContent_ddMainProducts',
  'ctl00_PageContent_ddlParameterMenu', 'ctl00_PageContent_ddlParameterDynamicSearch',
  'ctl00_PageContent_ddlCategoryMenu', 'ctl00_PageContent_ddlFilterSort', 'ctl00_PageContent_ddlProductSort',
  'ctl00_PageContent_ddlCariLimitTracing', 'ctl00_PageContent_ddlCariLimitTracingInfo',
  'ctl00_PageContent_ddlDontShowPrice', 'ctl00_PageContent_ddlTaxInRatio',
  'ctl00_PageContent_ddlCartProductVariationCodeShow', 'ctl00_PageContent_ddlHomeStyle',
  'ctl00_PageContent_ddlSearchMarka', 'ctl00_PageContent_ddlSearchDetail',
  'ctl00_PageContent_ddlRowDescriptionShow', 'ctl00_PageContent_ddlAllProductsShow',
  'ctl00_PageContent_DDLDeliveryAddress', 'ctl00_PageContent_ddlSiteMap', 'ctl00_PageContent_ddSubDealerShow',
  'ctl00_PageContent_chkCustomerCodeForLogin', 'ctl00_PageContent_chkLoginWithExpiry',
  'ctl00_PageContent_chkDontUseLcnAutoComplate', 'ctl00_PageContent_chkAddCartAction',
  'ctl00_PageContent_chkBillingAdress',
  'ctl00_PageContent_txtMinCartCount', 'ctl00_PageContent_txtMinCartTotal', 'ctl00_PageContent_txtMaximumOrderQuantity',
  ...textScalarWriteIds,
  'ctl00_PageContent_ddlHasStock', 'ctl00_PageContent_ddlPaymentDetailShow', 'ctl00_PageContent_ddlBankTableType',
  'ctl00_PageContent_ddlAccountActivityGenralCuurencyTableShow', 'ctl00_PageContent_ddlCommissionType',
  'ctl00_PageContent_ddlSpecialDiscount', 'ctl00_PageContent_ddlPaymentForAccountActivityShow',
  'ctl00_PageContent_ddlUseDesktopApi', 'ctl00_PageContent_ddlProductCurrency', 'ctl00_PageContent_ddlPosCurrency',
  'ctl00_PageContent_ddlSendDeliveryAddressIfDifferent', 'ctl00_PageContent_ddlCurrencyPriceType',
  'ctl00_PageContent_ddlintegrated', 'ctl00_PageContent_ddldiscountTypeInvoice', 'ctl00_PageContent_ddlBonusShow',
  'ctl00_PageContent_ddlUseMd5Encryption', 'ctl00_PageContent_ddlVariationShow', 'ctl00_PageContent_ddIntegrationCurrency',
  ...bitDropdownWriteIds, ...booleanIntegerWriteIds,
  scheduleId,
  'ctl00_PageContent_ddlParameterLogin', 'ctl00_PageContent_ddlParameterLoginMenu',
  'ctl00_PageContent_ddlParameterAdmin', 'ctl00_PageContent_ddlParameterIntegration',
  'ctl00_PageContent_ddlCommonSystem',
]);
const numericTextWriteIds = new Set(['ctl00_PageContent_txtMinCartCount', 'ctl00_PageContent_txtMinCartTotal', 'ctl00_PageContent_txtMaximumOrderQuantity']);
const customWriteIds = new Set([siteStatusId, parameterOrderId, layoutId, menuVisibilityId, deliveryDateId]);
const parameterOrderSetting = allSettings.find((setting) => setting.parameterId === parameterOrderId)!;
const layoutSetting = allSettings.find((setting) => setting.parameterId === layoutId)!;
const menuVisibilitySetting = allSettings.find((setting) => setting.parameterId === menuVisibilityId)!;
const siteStatusCanEdit = computed(() => canSaveSettings.value && (baselineSiteStatus.value === '0' || baselineSiteStatus.value === '1'));
const parameterOrderCanEdit = computed(() => canSaveSettings.value && typeof baselineParameterOrder.value === 'string');
const layoutBaseline = ref<SettingValue>(null);
const menuVisibilityBaseline = ref<SettingValue>(null);
const deliveryDateBaseline = ref<SettingValue>(null);
const layoutDraft = ref('');
const menuVisibilityDraft = ref('');
const deliveryDateDraft = ref(false);
const draftValues = ref<Record<string, SettingValue>>({});
const baselineDraftValues = ref<Record<string, SettingValue>>({});
const bitDrafts = ref<Record<string, Record<string, boolean>>>({});
const baselineBitDrafts = ref<Record<string, Record<string, boolean>>>({});
const configuredExtensionDrafts = ref<Record<string, boolean>>({});
const fileDrafts = ref<Record<string, File | null>>({});
const filePreviewUrls = ref<Record<string, string>>({});
const fileResetKey = ref(0);
const layoutCanEdit = computed(() => canSaveSettings.value && typeof layoutBaseline.value === 'string' && layoutSetting.options.some(option => option.value === layoutBaseline.value));
const menuVisibilityCanEdit = computed(() => canSaveSettings.value && typeof menuVisibilityBaseline.value === 'string' && menuVisibilitySetting.options.some(option => option.value === menuVisibilityBaseline.value));
const deliveryDateCanEdit = computed(() => canSaveSettings.value && typeof deliveryDateBaseline.value === 'boolean');
const changedOrderOperations = computed(() => parameterOrderSetting.options
  .filter((option) => (orderDraft.value[option.value] ?? false) !== isOptionEnabled(typeof baselineParameterOrder.value === 'string' ? baselineParameterOrder.value : null, option.value))
  .map((option) => ({ optionValue: option.value, enabled: orderDraft.value[option.value] ?? false })));
const hasChanges = computed(() => (siteStatusCanEdit.value && siteStatusDraft.value !== String(baselineSiteStatus.value))
  || changedOrderOperations.value.length > 0
  || (layoutCanEdit.value && layoutDraft.value !== String(layoutBaseline.value))
  || (menuVisibilityCanEdit.value && menuVisibilityDraft.value !== String(menuVisibilityBaseline.value))
  || (deliveryDateCanEdit.value && deliveryDateDraft.value !== deliveryDateBaseline.value)
  || allSettings.some((setting) => !isEditableInTestMode(setting) && draftChanged(setting))
  || allSettings.some((setting) => isEditableInTestMode(setting) && !customWriteIds.has(setting.parameterId) && draftChanged(setting)));
const unsupportedDrafts = computed(() => allSettings.filter((setting) => !isEditableInTestMode(setting) && draftChanged(setting)));
const supportedDrafts = computed(() => allSettings.filter((setting) => isEditableInTestMode(setting) && draftChanged(setting)));
const pendingSettingCount = computed(() => allSettings.filter(draftChanged).length);
const editableCount = computed(() => allSettings.filter(isEditableInTestMode).length);
const categoryCounts = computed(() => categories.map((category) => ({ ...category, count: category.settings.length })));
const itemById = computed(() => new Map(items.value.map((item) => [item.parameterId, item])));
const categorySettings = computed(() => categories.find((category) => category.name === selectedCategory.value)?.settings ?? []);
const searchActive = computed(() => search.value.trim().length > 0);
const visibleSettings = computed(() => {
  const source = searchActive.value ? allSettings : categorySettings.value;
  const query = search.value.trim().toLocaleLowerCase();
  return source.filter((setting) => {
    if (!query) return true;
    return [displayLabel(setting), setting.originalLabel, setting.htmlControlId, setting.htmlControlName,
      setting.mapping.columnCandidate ?? '', setting.category, setting.panel, categoryFor(setting), categoryDisplayLabel(categoryFor(setting)),
      ...setting.options.flatMap((option) => optionSearchLabels(setting.parameterId, option))]
      .some((part) => part.toLocaleLowerCase().includes(query));
  });
});
const groupedResults = computed(() => searchActive.value
  ? categories.map((category) => ({ name: category.name, settings: visibleSettings.value.filter((setting) => category.settings.includes(setting)) })).filter((group) => group.settings.length)
  : [{ name: selectedCategory.value, settings: visibleSettings.value }]);
function currentValue(setting: SettingDefinition): SettingValue {
  return itemById.value.get(setting.parameterId)?.value ?? null;
}

function sqlType(setting: SettingDefinition): string | null {
  return mappingAudit.get(setting.htmlControlId)?.sqlType ?? setting.mapping.sqlDataType;
}

function inputType(setting: SettingDefinition): string {
  const type = sqlType(setting)?.toLocaleLowerCase();
  return type && ['int', 'bigint', 'smallint', 'tinyint', 'decimal', 'numeric', 'money', 'smallmoney', 'float', 'real'].includes(type)
    ? 'number' : 'text';
}

function validNumericText(setting: SettingDefinition): boolean {
  const value = currentValue(setting);
  if (typeof value !== 'string') return false;
  const type = sqlType(setting)?.toLocaleLowerCase();
  if (type === 'int') return /^-?\d+$/.test(value) && Number.isSafeInteger(Number(value)) && Number(value) >= -2147483648 && Number(value) <= 2147483647;
  if (type === 'decimal') return /^-?\d+(?:\.\d{1,2})?$/.test(value) && Math.abs(Number(value)) < 10000000000000000;
  return false;
}

function hasIntegralBitmaskEvidence(setting: SettingDefinition): boolean {
  const declaredType = sqlType(setting)?.toLocaleLowerCase() ?? '';
  const integral = extendedWriteIds.has(setting.parameterId)
    || ['tinyint', 'smallint', 'int', 'bigint'].includes(declaredType)
    || (setting.parameterId === useFilteringId && declaredType === 'nvarchar');
  return setting.controlType === 'bitmask' && setting.options.length > 0 && integral
    && setting.options.every((option) => {
      try { const bit = BigInt(option.value); return bit > 0n && (bit & (bit - 1n)) === 0n; }
      catch { return false; }
    });
}

function draftChanged(setting: SettingDefinition): boolean {
  if (setting.parameterId === siteStatusId) return baselineSiteStatus.value !== null && siteStatusDraft.value !== String(baselineSiteStatus.value);
  if (setting.parameterId === layoutId) return layoutBaseline.value !== null && layoutDraft.value !== String(layoutBaseline.value);
  if (setting.parameterId === menuVisibilityId) return menuVisibilityBaseline.value !== null && menuVisibilityDraft.value !== String(menuVisibilityBaseline.value);
  if (setting.parameterId === deliveryDateId) return typeof deliveryDateBaseline.value === 'boolean' && deliveryDateDraft.value !== deliveryDateBaseline.value;
  if (setting.parameterId === parameterOrderId) return changedOrderOperations.value.length > 0;
  if (extendedWriteIds.has(setting.parameterId) && currentValue(setting) === null
    && configuredExtensionDrafts.value[setting.parameterId] === true) return true;
  if (setting.controlType === 'bitmask') {
    const current = bitDrafts.value[setting.parameterId] ?? {};
    const baseline = baselineBitDrafts.value[setting.parameterId] ?? {};
    return setting.options.some((option) => (current[option.value] ?? false) !== (baseline[option.value] ?? false));
  }
  if (setting.controlType === 'file') return fileDrafts.value[setting.parameterId] != null;
  return !Object.is(draftValues.value[setting.parameterId], baselineDraftValues.value[setting.parameterId]);
}

function setBitDraft(parameterId: string, optionValue: string, checked: boolean): void {
  bitDrafts.value[parameterId] = { ...(bitDrafts.value[parameterId] ?? {}), [optionValue]: checked };
  if (extendedWriteIds.has(parameterId) && currentValue(allSettings.find(setting => setting.parameterId === parameterId)!) === null)
    configuredExtensionDrafts.value[parameterId] = true;
}

function configureUnselectedExtension(parameterId: string): void {
  configuredExtensionDrafts.value[parameterId] = true;
  if (parameterId === whatsAppOrderNotificationId) draftValues.value[parameterId] = false;
}

function isExtensionSetting(parameterId: string): boolean { return extensionSettingIds.has(parameterId); }

function setFileDraft(parameterId: string, event: Event): void {
  const target = event.target as HTMLInputElement;
  const oldPreview = filePreviewUrls.value[parameterId];
  if (oldPreview && typeof URL.revokeObjectURL === 'function') URL.revokeObjectURL(oldPreview);
  const selected = target.files?.item(0) ?? null;
  fileDrafts.value[parameterId] = selected;
  if (selected && typeof URL.createObjectURL === 'function') filePreviewUrls.value[parameterId] = URL.createObjectURL(selected);
  else {
    const { [parameterId]: _removed, ...remaining } = filePreviewUrls.value;
    filePreviewUrls.value = remaining;
  }
}

function clearFileDraft(parameterId: string): void {
  const oldPreview = filePreviewUrls.value[parameterId];
  if (oldPreview && typeof URL.revokeObjectURL === 'function') URL.revokeObjectURL(oldPreview);
  const { [parameterId]: _removedPreview, ...remainingPreviews } = filePreviewUrls.value;
  const { [parameterId]: _removedDraft, ...remainingDrafts } = fileDrafts.value;
  filePreviewUrls.value = remainingPreviews;
  fileDrafts.value = remainingDrafts;
  fileResetKey.value++;
}

function previewUrl(parameterId: string): string | undefined {
  return filePreviewUrls.value[parameterId]
    ?? (applicationAssets.value[parameterId] ? applicationAssetContentUrl(parameterId) : undefined);
}

async function saveApplicationAsset(parameterId: string): Promise<void> {
  const file = fileDrafts.value[parameterId];
  if (!file || !canManageApplicationAssets.value || assetSavingId.value || assetDeletingId.value) return;
  assetSavingId.value = parameterId;
  error.value = '';
  try {
    const result = await uploadApplicationAsset(parameterId, file);
    applicationAssets.value = { ...applicationAssets.value, [parameterId]: result.metadata };
    clearFileDraft(parameterId);
    notice.value = t('Görsel yeni uygulamanın yalıtılmış alanına kaydedildi. Eski bayi portalında kullanılmaz.');
  } catch (cause) {
    error.value = cause instanceof ApiError && cause.status === 401
      ? t('Yerel API kimlik doğrulaması başarısız oldu.')
      : t('Görsel kaydedilemedi. Dosya türünü ve boyutunu kontrol edip yeniden deneyin.');
  } finally {
    assetSavingId.value = '';
  }
}

async function removeApplicationAsset(parameterId: string): Promise<void> {
  if (!canManageApplicationAssets.value || assetDeletingId.value || assetSavingId.value) return;
  assetDeletingId.value = parameterId;
  error.value = '';
  notice.value = '';
  try {
    await deleteApplicationAsset(parameterId);
    const { [parameterId]: _removed, ...remaining } = applicationAssets.value;
    applicationAssets.value = remaining;
    notice.value = t('Uygulama görseli kaldırıldı. Eski portalda kullanılan dosyalar etkilenmedi.');
  } catch {
    error.value = t('Uygulama görseli kaldırılamadı. Lütfen yeniden deneyin.');
  } finally {
    assetDeletingId.value = '';
  }
}

function isLoaded(setting: SettingDefinition): boolean {
  return itemById.value.has(setting.parameterId);
}

function isEditableInTestMode(setting: SettingDefinition): boolean {
  if (!canSaveSettings.value || !testWritableIds.has(setting.parameterId)) return false;
  return (extendedWriteIds.has(setting.parameterId) && setting.controlType === 'bitmask'
      && (typeof currentValue(setting) === 'string' || currentValue(setting) === null) && hasIntegralBitmaskEvidence(setting))
    || (extensionSettingIds.has(setting.parameterId) && setting.controlType === 'checkbox'
      && (typeof currentValue(setting) === 'boolean' || currentValue(setting) === null))
    || (extensionSettingIds.has(setting.parameterId) && setting.controlType === 'radio'
      && (typeof currentValue(setting) === 'string' || currentValue(setting) === null))
    || (setting.parameterId === siteStatusId && siteStatusCanEdit.value)
    || (setting.parameterId === parameterOrderId && parameterOrderCanEdit.value)
    || (setting.parameterId === layoutId && layoutCanEdit.value)
    || (setting.parameterId === menuVisibilityId && menuVisibilityCanEdit.value)
    || (setting.parameterId === deliveryDateId && deliveryDateCanEdit.value)
    || (numericTextWriteIds.has(setting.parameterId) && setting.controlType === 'text' && validNumericText(setting))
    || (textScalarWriteIds.has(setting.parameterId) && ['text', 'textarea'].includes(setting.controlType)
      && !setting.multiple && (typeof currentValue(setting) === 'string' || currentValue(setting) === null))
    || (booleanIntegerWriteIds.has(setting.parameterId) && setting.controlType === 'checkbox'
      && (currentValue(setting) === '0' || currentValue(setting) === '1'))
    || (bitDropdownWriteIds.has(setting.parameterId) && setting.controlType === 'select'
      && typeof currentValue(setting) === 'boolean'
      && setting.options.some(option => option.value === '0') && setting.options.some(option => option.value === '1'))
    || (setting.parameterId === productCompanyId && (currentValue(setting) === null
      || (typeof currentValue(setting) === 'string' && setting.options.some(option => option.value === currentValue(setting)))))
    || (setting.parameterId === scheduleId && (currentValue(setting) === null || currentValue(setting) === '0' || currentValue(setting) === '1'))
    || (!customWriteIds.has(setting.parameterId) && !numericTextWriteIds.has(setting.parameterId)
      && (setting.controlType === 'bitmask'
        ? typeof currentValue(setting) === 'string' && hasIntegralBitmaskEvidence(setting)
        : setting.controlType === 'checkbox'
          ? typeof currentValue(setting) === 'boolean' && sqlType(setting) === 'bit'
          : setting.controlType === 'select' && typeof currentValue(setting) === 'string'
            && setting.options.some((option) => option.value === currentValue(setting))));
}

function displayScalar(value: SettingValue, setting: SettingDefinition): string {
  if (value === null) return t('Belirtilmemiş');
  if (setting.parameterId === deliveryDateId && typeof value === 'boolean') return value ? t('Etkin') : t('Devre dışı');
  const text = bitDropdownWriteIds.has(setting.parameterId) && typeof value === 'boolean' ? (value ? '1' : '0') : String(value);
  const option = setting.options.find((entry) => entry.value === text);
  if (option && ['select', 'radio'].includes(setting.controlType)) return optionLabel(setting.parameterId, option);
  if (['select', 'radio', 'checkbox'].includes(setting.controlType)) return t('Seçim ayrıntıları teknik bilgiler bölümünde.');
  return text;
}

function rawValue(setting: SettingDefinition): string {
  if (!isLoaded(setting)) return 'Yüklenmedi';
  const value = currentValue(setting);
  return value === null ? 'NULL' : String(value);
}

function syncDrafts(values: SettingItem[]): void {
  if (typeof URL.revokeObjectURL === 'function') for (const preview of Object.values(filePreviewUrls.value)) URL.revokeObjectURL(preview);
  filePreviewUrls.value = {};
  const byId = new Map(values.map((item) => [item.parameterId, item.value]));
  const nextDrafts: Record<string, SettingValue> = {};
  const nextBitDrafts: Record<string, Record<string, boolean>> = {};
  for (const setting of allSettings) {
    const value = byId.get(setting.parameterId) ?? null;
    nextDrafts[setting.parameterId] = bitDropdownWriteIds.has(setting.parameterId) && typeof value === 'boolean'
      ? value ? '1' : '0'
      : setting.controlType === 'checkbox'
      ? typeof value === 'boolean' ? value : value === '0' ? false : value === '1' ? true : value
      : value;
    if (setting.controlType === 'bitmask') {
      const canDecode = hasIntegralBitmaskEvidence(setting) && typeof value === 'string';
      nextBitDrafts[setting.parameterId] = Object.fromEntries(setting.options.map((option) => [
        option.value,
        setting.parameterId === scheduleId ? value === '1' : canDecode ? isOptionEnabled(value, option.value) : false,
      ]));
    }
  }
  draftValues.value = nextDrafts;
  baselineDraftValues.value = { ...nextDrafts };
  bitDrafts.value = nextBitDrafts;
  baselineBitDrafts.value = Object.fromEntries(Object.entries(nextBitDrafts).map(([key, value]) => [key, { ...value }]));
  configuredExtensionDrafts.value = {};
  fileDrafts.value = {};
  fileResetKey.value++;
  baselineSiteStatus.value = byId.get(siteStatusId) ?? null;
  baselineParameterOrder.value = byId.get(parameterOrderId) ?? null;
  layoutBaseline.value = byId.get(layoutId) ?? null;
  menuVisibilityBaseline.value = byId.get(menuVisibilityId) ?? null;
  deliveryDateBaseline.value = byId.get(deliveryDateId) ?? null;
  siteStatusDraft.value = baselineSiteStatus.value === null ? '' : String(baselineSiteStatus.value);
  layoutDraft.value = layoutBaseline.value === null ? '' : String(layoutBaseline.value);
  menuVisibilityDraft.value = menuVisibilityBaseline.value === null ? '' : String(menuVisibilityBaseline.value);
  deliveryDateDraft.value = deliveryDateBaseline.value === true;
  const rawOrder = typeof baselineParameterOrder.value === 'string' ? baselineParameterOrder.value : null;
  orderDraft.value = Object.fromEntries(parameterOrderSetting.options.map((option) => [option.value, isOptionEnabled(rawOrder, option.value)]));
}

function discardChanges(): void {
  syncDrafts(items.value);
  error.value = '';
    notice.value = t('Kaydedilmemiş değişiklikler geri alındı.');
}

async function saveChanges(): Promise<void> {
  if (!hasChanges.value || saving.value) return;
  if (!canSaveSettings.value || supportedDrafts.value.length === 0) {
    error.value = t('Kaydedilebilir bir değişiklik bulunmuyor. Bu ayarlar taslak olarak kalır.');
    return;
  }
  const retainedDraftIds = unsupportedDrafts.value.map((setting) => setting.parameterId);
  const retainedValues = { ...draftValues.value };
  const retainedBits = Object.fromEntries(Object.entries(bitDrafts.value).map(([id, bits]) => [id, { ...bits }]));
  const retainedConfigured = { ...configuredExtensionDrafts.value };
  const retainedFiles = { ...fileDrafts.value };
  saving.value = true;
  error.value = '';
  notice.value = '';
  const catalogChanges: SettingChange[] = [];
  for (const setting of allSettings) {
    if (!isEditableInTestMode(setting) || !draftChanged(setting)) continue;
    const expectedValue = currentValue(setting);
    if (setting.controlType === 'bitmask') {
      const bitOperations = setting.parameterId === parameterOrderId
        ? changedOrderOperations.value
        : (() => {
          const draft = bitDrafts.value[setting.parameterId] ?? {};
          const baseline = baselineBitDrafts.value[setting.parameterId] ?? {};
          const emitted = new Set<string>();
          return setting.options
            .filter((option) => {
              const changed = (draft[option.value] ?? false) !== (baseline[option.value] ?? false);
              if (!changed || emitted.has(option.value)) return false;
              emitted.add(option.value);
              return true;
            })
            .map((option) => ({ optionValue: option.value, enabled: draft[option.value] ?? false }));
        })();
      catalogChanges.push({ parameterId: setting.parameterId, expectedValue, bitOperations });
    } else {
      const value = setting.parameterId === siteStatusId ? siteStatusDraft.value
        : setting.parameterId === layoutId ? layoutDraft.value
          : setting.parameterId === menuVisibilityId ? menuVisibilityDraft.value
            : setting.parameterId === deliveryDateId ? deliveryDateDraft.value
              : draftValues.value[setting.parameterId];
      catalogChanges.push({ parameterId: setting.parameterId, expectedValue, value });
    }
  }
  try {
    const committedIds = new Set<string>();
    const allPendingIds = new Set([...retainedDraftIds, ...catalogChanges.map(change => change.parameterId)]);
    const groups = [
      catalogChanges.filter(change => !extendedWriteIds.has(change.parameterId)),
      catalogChanges.filter(change => extendedWriteIds.has(change.parameterId)),
    ].filter(group => group.length > 0);
    let refreshed: Awaited<ReturnType<typeof loadSettings>> | null = null;
    for (const group of groups) {
      const saved = await saveSettings(group);
      if (!saved.committed) throw new Error('Test ortamına kaydetme doğrulanamadı.');
      group.forEach(change => { committedIds.add(change.parameterId); allPendingIds.delete(change.parameterId); });
      refreshed = await loadSettings();
      items.value = refreshed.items;
      syncDrafts(refreshed.items);
      for (const id of allPendingIds) {
        if (id in retainedValues) draftValues.value[id] = retainedValues[id];
        if (retainedBits[id]) bitDrafts.value[id] = { ...retainedBits[id] };
        if (retainedConfigured[id]) configuredExtensionDrafts.value[id] = true;
        if (retainedFiles[id]) {
          fileDrafts.value[id] = retainedFiles[id];
          if (typeof URL.createObjectURL === 'function') filePreviewUrls.value[id] = URL.createObjectURL(retainedFiles[id]);
        }
      }
    }
    const [latest, capabilities] = await Promise.all([loadSettings(), loadSettingsCapabilities()]);
    refreshed = latest;
    items.value = refreshed.items;
    syncDrafts(refreshed.items);
    for (const id of allPendingIds) {
      if (id in retainedValues) draftValues.value[id] = retainedValues[id];
      if (retainedBits[id]) bitDrafts.value[id] = { ...retainedBits[id] };
      if (retainedConfigured[id]) configuredExtensionDrafts.value[id] = true;
      if (retainedFiles[id]) {
        fileDrafts.value[id] = retainedFiles[id];
        if (typeof URL.createObjectURL === 'function') filePreviewUrls.value[id] = URL.createObjectURL(retainedFiles[id]);
      }
    }
    mode.value = refreshed.mode;
    canSaveSettings.value = capabilities.canSaveSettings === true
      && capabilities.environment === 'IsolatedLocalDemo'
      && capabilities.target === 'Store5_Settings_Test / store5';
    canManageApplicationAssets.value = capabilities.canManageApplicationAssets === true && canSaveSettings.value;
    notice.value = allPendingIds.size
      ? t('Kaydedilebilir ayarlar gruplar halinde uygulandı; kalan düzenlemeler taslak olarak korundu.')
      : t('Değişiklikler test veritabanında kaydedildi ve yeniden okunarak doğrulandı.');
  } catch (cause) {
    if (cause instanceof ApiError && [401, 403, 404, 405].includes(cause.status)) {
      canSaveSettings.value = false;
      canManageApplicationAssets.value = false;
    }
    error.value = cause instanceof ApiError
      ? cause.status === 409 ? t('Ayar yüklendikten sonra değişmiş. Yeniden yükleyip kontrol ettikten sonra tekrar deneyin.') : t('Kaydetme başarısız oldu. Ayarları yenileyip tekrar deneyin.')
      : t('Kaydetme sonucu doğrulanamadı. Yeni bir değişiklik yapmadan önce ayarları yenileyin.');
  } finally {
    saving.value = false;
  }
}

function schemaCompatibility(setting: SettingDefinition): string {
  return mappingAudit.get(setting.htmlControlId)?.schemaCompatible ? 'SQL type compatible with the UI control' : 'Not confirmed';
}

function legacySaveBehavior(setting: SettingDefinition): string {
  return mappingAudit.get(setting.htmlControlId)?.legacySaveBehavior ?? 'unknown';
}

function valueEncoding(setting: SettingDefinition): string {
  return mappingAudit.get(setting.htmlControlId)?.encoding ?? setting.valueSemantics;
}

async function connect(): Promise<void> {
  loading.value = true;
  error.value = '';
  notice.value = '';
  try {
    const [result, capabilities] = await Promise.all([loadSettings(), loadSettingsCapabilities()]);
    items.value = result.items;
    mode.value = result.mode;
    canSaveSettings.value = capabilities.canSaveSettings === true
      && capabilities.environment === 'IsolatedLocalDemo'
      && capabilities.target === 'Store5_Settings_Test / store5';
    canManageApplicationAssets.value = capabilities.canManageApplicationAssets === true && canSaveSettings.value;
    syncDrafts(result.items);
    applicationAssets.value = {};
    if (canManageApplicationAssets.value) {
      try { applicationAssets.value = (await listApplicationAssets()).assets ?? {}; }
      catch { applicationAssets.value = {}; }
    }
    notice.value = canSaveSettings.value
      ? t('İzole yerel demo modu etkin. Kaydetme yalnızca Store5_Settings_Test / store5 hedefini kullanır.')
      : result.mode === 'Mock'
        ? t('Demo ayarları yüklendi. Değerler sentetik ve boştur; SQL bağlantısı kullanılmadı. Değişiklikler yerel taslak olarak kalır.')
        : t('SQL ayarları yüklendi. Değişiklikler yerel taslak olarak kalır; kaydetme kapalıdır.');
  } catch (cause) {
    items.value = [];
    applicationAssets.value = {};
    canSaveSettings.value = false;
    canManageApplicationAssets.value = false;
    syncDrafts([]);
    error.value = cause instanceof ApiError
      ? cause.status === 0
          ? t(cause.message)
        : cause.status === 401
          ? t('Yerel API kimlik doğrulaması başarısız oldu. Yerel API ayarlarını ve Vite proxy yapılandırmasını kontrol edin.')
          : cause.status === 503
            ? t('API ayarları okuyamadı. Yerel SQL hizmetini ve Store5_Settings_Test bağlantısını kontrol edin.')
            : t('Ayarlar yüklenemedi. Lütfen yerel API bağlantısını kontrol edip yeniden deneyin.')
      : t('Beklenmeyen bir bağlantı hatası oluştu. Yerel API hizmetini kontrol edip yeniden deneyin.');
  } finally {
    loading.value = false;
  }
}

onBeforeUnmount(() => {
  if (typeof URL.revokeObjectURL === 'function') for (const preview of Object.values(filePreviewUrls.value)) URL.revokeObjectURL(preview);
});

</script>

<template>
  <div class="app-shell">
    <aside class="sidebar" :aria-label="t('Ayar kategorileri')">
      <a class="brand" href="#main"><span class="brand-mark" aria-hidden="true">B</span><span>B2B <b>STORE</b></span></a>
      <p class="sidebar-label">{{ t('AYARLAR') }}</p>
      <nav class="category-nav">
        <button v-for="category in categoryCounts" :key="category.name" class="category-nav-item"
          :class="{ active: selectedCategory === category.name && !searchActive }"
          :aria-current="selectedCategory === category.name && !searchActive ? 'page' : undefined"
          @click="selectedCategory = category.name; search = ''">
          <span>{{ categoryLabel(category.name) }}</span><span class="nav-count">{{ category.count }}</span>
        </button>
      </nav>
      <div class="sidebar-foot"><span class="status-dot" aria-hidden="true"></span> {{ t('Ayar yönetimi') }}</div>
    </aside>

    <main id="main" class="main-area">
      <header class="topbar">
        <div class="crumb">B2B STORE <span>/</span> <b>{{ searchActive ? t('Arama sonuçları') : categoryLabel(selectedCategory) }}</b></div>
        <div class="topbar-actions"><div class="language-switch" role="group" :aria-label="t('Language')"><button type="button" :aria-pressed="language === 'en'" @click="setLanguage('en')">EN</button><button type="button" :aria-pressed="language === 'tr'" @click="setLanguage('tr')">TR</button></div>
        <div class="mode-pill" :class="mode ? 'mode-sql' : ''"><span aria-hidden="true"></span>{{ mode ? t('Bağlandı') : t('Bağlı değil') }}</div></div>
      </header>

      <div class="content">
        <section class="page-heading">
          <div>
            <h1>{{ searchActive ? t('Ayarları arayın') : categoryLabel(selectedCategory) }}</h1>
            <p class="subtitle">{{ t('Bayi mağazanızın ayarlarını buradan inceleyip düzenleyin.') }}</p>
          </div>
          <div class="connect-panel">
            <button class="button button-primary" :disabled="loading" @click="connect">{{ loading ? t('Bağlanıyor…') : mode ? t('Ayarları yenile') : t('Bağlan') }}</button>
          </div>
        </section>

        <section class="summary-row" :aria-label="t('Ayar özeti')">
          <div class="summary-card"><span class="summary-icon violet" aria-hidden="true">▦</span><div><strong>{{ catalog.controlCount }}</strong><small>{{ t('Toplam ayar') }}</small></div></div>
          <div class="summary-card"><span class="summary-icon green" aria-hidden="true">✓</span><div><strong>{{ items.length }}</strong><small>{{ t('Yüklenen ayar') }}</small></div></div>
          <div class="summary-card"><span class="summary-icon blue" aria-hidden="true">◌</span><div><strong>{{ catalog.controlCount - editableCount }}</strong><small>{{ t('Salt okunur ayar') }}</small></div></div>
        </section>

        <div v-if="error" class="message message-error" role="alert"><span aria-hidden="true">!</span>{{ error }}</div>
        <div v-if="notice" class="message message-info" role="status"><span aria-hidden="true">i</span>{{ notice }}</div>
        <div v-if="canSaveSettings && items.length" class="message message-warning" role="note">
          <span aria-hidden="true">!</span><span><b>{{ t('Yalıtılmış test veritabanı:') }}</b> {{ t('kaydedilen değişiklikler yalnızca Store5_Settings_Test veritabanındaki store5 kaydına uygulanır. Diğer değişiklikler taslak olarak kalır.') }}</span>
        </div>

        <section class="settings-toolbar">
          <div><h2>{{ searchActive ? `${visibleSettings.length} ${t('eşleşen ayar')}` : `${categoryLabel(selectedCategory)}${t(' ayarları')}` }}</h2>
            <p>{{ searchActive ? t('Tüm kategorilerde ayar adlarıyla arayın.') : `${visibleSettings.length}${t(' ayar listeleniyor')}` }}</p></div>
          <label class="search-box"><span aria-hidden="true">⌕</span><span class="sr-only">{{ t('Ayar adında veya teknik adında ara') }}</span>
            <input v-model="search" type="search" :placeholder="t('Ayarlarda ara...')" /></label>
        </section>

        <section v-if="loading" class="empty-state" aria-live="polite"><div class="spinner" aria-hidden="true"></div><h3>{{ t('Ayarlar yükleniyor') }}</h3><p>{{ t('Yapılandırılmış mağaza kaydı okunuyor.') }}</p></section>
        <section v-else-if="!items.length" class="empty-state"><div class="empty-icon" aria-hidden="true">⌁</div><h3>{{ t('Mevcut ayarları görmek için bağlanın') }}</h3><p>{{ t('Ayar kataloğunu inceleyebilir, Bağlan düğmesiyle yerel kaynaktaki değerleri yükleyebilirsiniz.') }}</p></section>
        <section v-else class="settings-groups" aria-live="polite">
          <div v-for="group in groupedResults" :key="group.name" class="settings-group">
             <div v-if="searchActive" class="group-heading"><h2>{{ categoryLabel(group.name) }}</h2><span>{{ group.settings.length }}</span></div>
            <article v-for="setting in group.settings" :key="setting.parameterId" class="setting-card">
              <div class="setting-copy">
                  <div class="setting-title-row"><h3>{{ settingLabel(setting) }}</h3><span v-if="draftChanged(setting)" class="setting-dirty">{{ t('Kaydedilmemiş değişiklik') }}</span><span v-else-if="isEditableInTestMode(setting)" class="setting-editable">{{ t('Test ortamında kaydedilebilir') }}</span><span v-else class="setting-read-only">{{ t('Taslak · kaydetme desteklenmiyor') }}</span></div>
                 <p class="help-text">{{ settingHelp(setting) }}</p>
                <details class="technical-details">
                    <summary>{{ t('Teknik bilgiler') }}</summary>
                    <dl><dt>{{ t('Orijinal etiket') }}</dt><dd>{{ setting.originalLabel }}</dd><dt>{{ t('HTML denetim kimliği') }}</dt><dd><code>{{ setting.htmlControlId }}</code></dd>
                      <dt>{{ t('HTML denetim adı') }}</dt><dd><code>{{ setting.htmlControlName }}</code></dd>
                      <dt>{{ t('Kategori') }}</dt><dd>{{ categoryLabel(categoryFor(setting)) }}</dd>
                      <dt>{{ t('Veritabanı alanı') }}</dt><dd><code>{{ setting.mapping.tableCandidate && setting.mapping.columnCandidate ? `${setting.mapping.tableCandidate}.${setting.mapping.columnCandidate}` : t('Eşleşme doğrulanmadı') }}</code></dd>
                      <dt>{{ t('SQL veri türü') }}</dt><dd>{{ setting.mapping.sqlDataType ?? t('Doğrulanmadı') }}</dd>
                      <dt>{{ t('Şema uyumluluğu') }}</dt><dd>{{ schemaCompatibility(setting) }}</dd>
                      <dt>{{ t('Eşleme durumu') }}</dt><dd>{{ mappingLabel(setting) }}</dd>
                      <dt>{{ t('Eski kaydetme davranışı') }}</dt><dd>{{ legacySaveBehavior(setting) }}</dd>
                      <dt>{{ t('Değer kodlaması') }}</dt><dd>{{ valueEncoding(setting) }}</dd>
                      <dt>{{ t('Ham SQL değeri') }}</dt><dd><code>{{ rawValue(setting) }}</code></dd>
                      <dt>{{ t('Kanıt') }}</dt><dd>{{ setting.mapping.evidence }}</dd>
                      <template v-if="setting.mapping.outstandingQuestion"><dt>{{ t('Açık soru') }}</dt><dd>{{ setting.mapping.outstandingQuestion }}</dd></template>
                  </dl>
                </details>
              </div>
              <div class="setting-value">
                  <span class="read-only-label">{{ isLoaded(setting) ? (isEditableInTestMode(setting) ? t('Test ortamındaki değer') : t('Mevcut değer')) : t('Yüklenmedi') }}</span>
                  <template v-if="!isLoaded(setting)"><span class="value-empty">{{ t('Değeri yüklemek için bağlanın.') }}</span></template>
                <template v-else-if="setting.parameterId === siteStatusId && siteStatusCanEdit">
                    <label class="sr-only" for="test-site-status">{{ t('Site durumu') }}</label>
                  <select id="test-site-status" v-model="siteStatusDraft" class="setting-input" :disabled="saving">
                      <option value="1">{{ t('Aktif') }}</option><option value="0">{{ t('Pasif') }}</option>
                  </select>
                </template>
                <template v-else-if="setting.parameterId === parameterOrderId && parameterOrderCanEdit">
                    <div class="editable-options"><label v-for="option in parameterOrderSetting.options" :key="option.value" class="editable-option"><input v-model="orderDraft[option.value]" type="checkbox" :value="option.value" :disabled="saving" /> <span>{{ optionLabel(parameterOrderSetting.parameterId, option) }}</span></label></div>
                    <span class="value-hint">{{ t('Yalnızca seçtiğiniz seçenekler değişir.') }}</span>
                </template>
                <template v-else-if="setting.parameterId === layoutId && layoutCanEdit">
                  <label class="sr-only" for="test-layout">{{ t('Layout') }}</label>
                  <select id="test-layout" v-model="layoutDraft" class="setting-input" :disabled="saving"><option v-for="option in layoutSetting.options" :key="option.value" :value="option.value">{{ optionLabel(layoutSetting.parameterId, option) }}</option></select>
                </template>
                <template v-else-if="setting.parameterId === menuVisibilityId && menuVisibilityCanEdit">
                   <label class="sr-only" for="test-menu-visibility">{{ t('Bayilerde menü görünümü') }}</label>
                  <select id="test-menu-visibility" v-model="menuVisibilityDraft" class="setting-input" :disabled="saving"><option v-for="option in menuVisibilitySetting.options" :key="option.value" :value="option.value">{{ optionLabel(menuVisibilitySetting.parameterId, option) }}</option></select>
                </template>
                <template v-else-if="setting.parameterId === deliveryDateId && deliveryDateCanEdit">
                  <label class="editable-option"><input v-model="deliveryDateDraft" type="checkbox" :disabled="saving" /> <span>{{ t('Teslimat tarihi istensin') }}</span></label>
                </template>
                <template v-else-if="setting.controlType === 'bitmask'">
                  <div v-if="setting.options.length" class="editable-options"><label v-for="option in setting.options" :key="option.value" class="editable-option"><input type="checkbox" :value="option.value" :checked="bitDrafts[setting.parameterId]?.[option.value] ?? false" @change="setBitDraft(setting.parameterId, option.value, ($event.target as HTMLInputElement).checked)" /> <span>{{ optionLabel(setting.parameterId, option) }}</span></label></div>
                  <select v-else class="setting-input" multiple disabled :aria-label="settingLabel(setting)"></select>
                  <span v-if="extendedWriteIds.has(setting.parameterId) && currentValue(setting) === null" class="value-hint">{{ t('Yapılandırılmamış') }}</span>
                  <button v-if="canSaveSettings && extendedWriteIds.has(setting.parameterId) && currentValue(setting) === null && !configuredExtensionDrafts[setting.parameterId]" class="button button-secondary" type="button" @click="configureUnselectedExtension(setting.parameterId)">{{ t('Seçenek seçmeden sıfır değerini kaydet') }}</button>
                   <span v-if="!setting.options.length" class="value-hint">{{ t('Kaynak envanterde seçenek listelenmediği için seçim yapılamıyor.') }}</span>
                    <span class="value-hint">{{ setting.parameterId === scheduleId ? t('Ürün içe aktarma davranışını etkiler; yalnızca izole test ortamında kaydedilebilir.') : canSaveSettings && isEditableInTestMode(setting) ? t('Yalnızca izole test veritabanına kaydedilir; eski portal davranışı doğrulanmadı.') : hasIntegralBitmaskEvidence(setting) ? t('Bu değişiklik kaydedilene kadar taslak olarak kalır.') : t('Kayıt biçimi doğrulanmadı; bu değişiklik kaydedilemez.') }}</span>
                </template>
                <template v-else-if="setting.controlType === 'select'">
                  <label class="sr-only" :for="`draft-${setting.parameterId}`">{{ settingLabel(setting) }}</label>
                   <select :id="`draft-${setting.parameterId}`" class="setting-input" :value="String(draftValues[setting.parameterId] ?? '')" @change="draftValues[setting.parameterId] = ($event.target as HTMLSelectElement).value">
                    <option value="" disabled>{{ t('Bir seçenek belirleyin') }}</option><option v-for="option in setting.options" :key="option.value" :value="option.value">{{ optionLabel(setting.parameterId, option) }}</option>
                  </select>
                </template>
                <template v-else-if="setting.controlType === 'radio'">
                  <fieldset class="editable-options"><legend class="sr-only">{{ settingLabel(setting) }}</legend><label v-for="option in setting.options" :key="option.value" class="editable-option"><input type="radio" :name="setting.parameterId" :value="option.value" :checked="draftValues[setting.parameterId] === option.value" @change="draftValues[setting.parameterId] = option.value" /> <span>{{ optionLabel(setting.parameterId, option) }}</span></label></fieldset>
                </template>
                <template v-else-if="setting.controlType === 'checkbox'">
                  <button v-if="isExtensionSetting(setting.parameterId) && currentValue(setting) === null && !configuredExtensionDrafts[setting.parameterId]" class="button button-secondary" type="button" @click="configureUnselectedExtension(setting.parameterId)">{{ t('Kapalı olarak yapılandır') }}</button>
                  <span v-else-if="isExtensionSetting(setting.parameterId) && currentValue(setting) === null" class="value-hint">{{ t('Yapılandırılmamış') }}</span>
                  <label v-else class="editable-option"><input type="checkbox" :checked="draftValues[setting.parameterId] === true" @change="draftValues[setting.parameterId] = ($event.target as HTMLInputElement).checked" /> <span>{{ settingLabel(setting) }}</span></label>
                </template>
                <template v-else-if="setting.controlType === 'file'">
                    <img v-if="previewUrl(setting.parameterId)" class="asset-preview" :src="previewUrl(setting.parameterId)" :alt="t('Görsel önizlemesi')" />
                    <label class="file-editor">{{ t('Dosya seç: ') }}{{ settingLabel(setting) }}<input :key="fileResetKey" type="file" accept=".png,.jpg,.jpeg,.webp,.ico,image/png,image/jpeg,image/webp,image/x-icon" :aria-label="settingLabel(setting)" @change="setFileDraft(setting.parameterId, $event)" /></label>
                    <span class="value-hint">{{ t('Yeni uygulamaya özel dosya alanı; eski portalda görünmez.') }}</span>
                    <span v-if="applicationAssets[setting.parameterId]" class="value-hint">{{ t('Uygulama alanında kayıtlı') }} · {{ applicationAssets[setting.parameterId].contentType }} · {{ applicationAssets[setting.parameterId].length }} {{ t('bayt') }} · {{ t('Görseli değiştir veya kaldır.') }}</span>
                    <button v-if="canManageApplicationAssets && fileDrafts[setting.parameterId]" class="button button-secondary" :disabled="assetSavingId !== '' || assetDeletingId !== ''" @click="saveApplicationAsset(setting.parameterId)">{{ assetSavingId === setting.parameterId ? t('Yükleniyor…') : t('Yalıtılmış uygulama alanına yükle') }}</button>
                    <button v-if="canManageApplicationAssets && applicationAssets[setting.parameterId]" class="button button-secondary" :disabled="assetSavingId !== '' || assetDeletingId !== ''" @click="removeApplicationAsset(setting.parameterId)">{{ assetDeletingId === setting.parameterId ? t('Görsel kaldırılıyor…') : t('Uygulama görselini kaldır') }}</button>
                </template>
                <template v-else-if="setting.controlType === 'textarea'">
                  <label class="sr-only" :for="`draft-${setting.parameterId}`">{{ settingLabel(setting) }}</label>
                  <textarea :id="`draft-${setting.parameterId}`" class="setting-input setting-textarea" :value="String(draftValues[setting.parameterId] ?? '')" @input="draftValues[setting.parameterId] = ($event.target as HTMLTextAreaElement).value"></textarea>
                </template>
                <template v-else-if="setting.controlType === 'text'">
                  <label class="sr-only" :for="`draft-${setting.parameterId}`">{{ settingLabel(setting) }}</label>
                  <input :id="`draft-${setting.parameterId}`" class="setting-input" :type="inputType(setting)" :step="sqlType(setting) === 'decimal' ? '0.01' : inputType(setting) === 'number' ? '1' : undefined" :value="String(draftValues[setting.parameterId] ?? '')" @input="draftValues[setting.parameterId] = ($event.target as HTMLInputElement).value" />
                </template>
                <span v-else class="value-display" :class="{ 'value-null': currentValue(setting) === null }">{{ displayScalar(currentValue(setting), setting) }}</span>
                 <span v-if="canSaveSettings && setting.parameterId === siteStatusId && !siteStatusCanEdit" class="value-hint">{{ t('Değer boş veya desteklenmeyen biçimde olduğu için değiştirilemiyor.') }}</span>
                 <span v-if="canSaveSettings && setting.parameterId === parameterOrderId && !parameterOrderCanEdit" class="value-hint">{{ t('Değer boş olduğu için değiştirilemiyor.') }}</span>
                 <span v-if="canSaveSettings && setting.parameterId === layoutId && !layoutCanEdit" class="value-hint">{{ t('Değer boş veya desteklenmeyen biçimde olduğu için değiştirilemiyor.') }}</span>
                 <span v-if="canSaveSettings && setting.parameterId === menuVisibilityId && !menuVisibilityCanEdit" class="value-hint">{{ t('Değer boş veya desteklenmeyen biçimde olduğu için değiştirilemiyor.') }}</span>
                 <span v-if="canSaveSettings && setting.parameterId === deliveryDateId && !deliveryDateCanEdit" class="value-hint">{{ t('Değer boş olduğu için değiştirilemiyor.') }}</span>
              </div>
            </article>
          </div>
            <div v-if="!visibleSettings.length" class="empty-state search-empty"><div class="empty-icon" aria-hidden="true">⌕</div><h3>{{ t('Sonuç bulunamadı') }}</h3><p>{{ t('Farklı bir ayar adı, kategori veya teknik alan adı deneyin.') }}</p></div>
        </section>
          <div v-if="items.length && hasChanges" class="save-bar" role="region" :aria-label="t('Kaydedilmemiş değişiklikler')">
           <span><b>{{ pendingSettingCount }}{{ t(' ayarda değişiklik var.') }}</b> {{ supportedDrafts.length }}{{ t(' değişiklik uygulanabilir') }}<span v-if="unsupportedDrafts.length">{{ t('; ') }}{{ unsupportedDrafts.length }}{{ t(' değişiklik taslak olarak kalır.') }}</span></span>
           <div><button class="button button-secondary" :disabled="saving" @click="discardChanges">{{ t('Değişiklikleri geri al') }}</button><button v-if="canSaveSettings" class="button button-primary" :disabled="supportedDrafts.length === 0 || saving" @click="saveChanges">{{ saving ? t('Kaydediliyor…') : t('Değişiklikleri uygula') }}</button></div>
        </div>
          <footer class="catalog-footer"><span>{{ canSaveSettings ? t('Yalnızca doğrulanmış ayarlar yalıtılmış test ortamına kaydedilebilir.') : t('Kaydetme kapalıdır; değişiklikler yerel taslak olarak kalır.') }}</span><span>{{ allSettings.length }}{{ t(' ayar') }}</span></footer>
      </div>
    </main>
  </div>
</template>
