<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref } from 'vue';
import { ApiError, setAdminToken, applicationAssetContentUrl, deleteApplicationAsset, listApplicationAssets, loadSettings, loadSettingsCapabilities, saveSettings, uploadApplicationAsset, type ApplicationAssetMetadata, type SettingChange, type SettingItem, type SettingValue } from './api';
import { allSettings, categories, categoryFor, categoryDisplayLabel, categoryEnglishLabel, displayLabel, englishDisplayLabel, helpText, helpTextEnglish, mappingAudit, mappingLabel, type SettingDefinition } from './catalog';
import { isOptionEnabled } from './bitmask';
import { optionDisplayLabel, optionSearchLabels } from './option-labels.tr';
import { createPublicDemoItems } from './public-demo';

type Language = 'en' | 'tr';
const publicDemo = import.meta.env.VITE_PUBLIC_DEMO === 'true';
const adminTokenDraft = ref('');
const publicWriteMode = ref(false);
const language = ref<Language>(localStorage.getItem('b2b-settings-language') === 'tr' ? 'tr' : 'en');
const englishUi: Record<string, string> = {
  "Herkese açık demo modu etkin. Tüm ayarlar sentetik ve boş başlar. API veya SQL bağlantısı kullanılmaz; düzenlemeler yalnızca bu tarayıcıda taslak olarak kalır.": "Public demo mode is active. All settings are synthetic and start unset. No API or SQL connection is used; edits remain drafts in this browser only.",
  "Herkese açık demo": "Public demo",
  "Demo mağaza": "Demo store",
  "Demo verilerini sıfırla": "Reset demo",
  "AYARLAR": "SETTINGS",
  "MAĞAZA YÖNETİMİ": "STORE MANAGEMENT",
  "Ayar yönetimi": "Settings management",
  "Arama sonuçları": "Search results",
  "Bağlandı": "Connected",
  "Bağlı değil": "Disconnected",
  "Ayarları arayın": "Find settings",
  "Mağazanızın görünümünü, ürün deneyimini ve çalışma tercihlerini tek bir yerden yönetin.": "Manage your store appearance, product experience, and operating preferences in one place.",
  "Bağlanıyor…": "Connecting…",
  "Ayarları yenile": "Refresh settings",
  "Bağlan": "Connect",
  "Toplam ayar": "Total settings",
  "Yüklenen ayar": "Loaded settings",
  "Salt okunur ayar": "Read-only settings",
  "Ayar özeti": "Settings summary",
  "Ayarlar yüklenemedi. Lütfen yeniden deneyin.": "Settings could not be loaded. Please try again.",
  "Beklenmeyen bir bağlantı hatası oluştu. Lütfen yeniden deneyin.": "An unexpected connection error occurred. Please try again.",
  "Ayarlar yüklendi. Değişiklikler yerel taslak olarak kalır; kaydetme kapalıdır.": "Settings loaded. Changes remain local drafts; saving is disabled.",
  "SQL ayarları yüklendi. Değişiklikler yerel taslak olarak kalır; kaydetme kapalıdır.": "Store settings loaded. Changes remain local drafts; saving is disabled.",
  "Demo ayarları yüklendi. Değerler sentetik ve boştur; SQL bağlantısı kullanılmadı. Değişiklikler yerel taslak olarak kalır.": "Demo settings loaded. Values are synthetic and unset; no SQL connection was used. Changes remain local drafts.",
  "Demo mağazası ayarları yüklendi. Kaydedilebilir değişiklikler bu mağazaya uygulanır.": "Demo store settings loaded. Supported changes are applied to this store.",
  "Demo mağazası:": "Demo store:",
  "Değişiklikler bu demo mağazasına kaydedilir. Desteklenmeyen alanlar taslak olarak kalır.": "Changes are saved to this demo store. Unsupported fields remain drafts.",
  "eşleşen ayar": "matching settings",
  " ayar": " settings",
  " ayarları": " settings",
  " ayar listeleniyor": " settings shown",
  "Tüm kategorilerde ayar adlarıyla arayın.": "Search setting names across all categories.",
  "Ayarlarda ara...": "Search by setting, purpose, or technical ID...",
  "Ayar adında veya teknik adında ara": "Search by setting name, description, or technical ID",
  "Ayar bölümleri": "Settings sections",
  "BÖLÜMLER": "SECTIONS",
  "Popüler aramalar": "Popular searches",
  "Ctrl+K ile ara": "Search with Ctrl+K",
  "Stok": "Stock",
  "Ödeme": "Payment",
  "Sipariş": "Order",
  "Ayarlar yükleniyor": "Loading settings",
  "Yapılandırılmış mağaza kaydı okunuyor.": "Reading the configured store record.",
  "Mevcut ayarları görmek için bağlanın": "Connect to view current settings",
  "Ayar kataloğunu inceleyebilir, Bağlan düğmesiyle mağaza değerlerini yükleyebilirsiniz.": "Browse the catalog, then connect to load your store settings.",
  "Teknik bilgiler": "Technical information",
  "Orijinal etiket": "Original label",
  "HTML denetim kimliği": "HTML control ID",
  "HTML denetim adı": "HTML control name",
  "Kategori": "Category",
  "Veritabanı alanı": "Database field",
  "Eşleşme doğrulanmadı": "Mapping not confirmed",
  "SQL veri türü": "SQL data type",
  "Doğrulanmadı": "Not verified",
  "Şema uyumluluğu": "Schema compatibility",
  "Eşleme durumu": "Mapping status",
  "Eski kaydetme davranışı": "Legacy save behavior",
  "Değer kodlaması": "Value encoding",
  "Ham SQL değeri": "Raw SQL value",
  "Kanıt": "Evidence",
  "Açık soru": "Open question",
  "Mevcut değer": "Current value",
  "Yüklenmedi": "Not loaded",
  "Değeri yüklemek için bağlanın.": "Connect to load the value.",
  "Aktif": "Active",
  "Pasif": "Inactive",
  "Yalnızca seçtiğiniz seçenekler değişir.": "Only the options you select will change.",
  "Yapılandırılmamış": "Unconfigured",
  "Seçenek seçmeden sıfır değerini kaydet": "Save a zero value without selected options",
  "Kapalı olarak yapılandır": "Configure as off",
  "Kaydedilebilir ayarlar gruplar halinde uygulandı; kalan düzenlemeler taslak olarak korundu.": "Saveable settings were applied in separate groups; remaining edits were kept as drafts.",
  "Kaydı desteklenen ayarlar mağazaya uygulanır; diğer düzenlemeler taslak olarak kalır.": "Supported settings are applied to the store; other edits remain drafts.",
  "Bu görsel yeni yönetim panelinde kullanılır.": "This image is used in the new management panel.",
  "Görsel bu yönetim paneline kaydedildi.": "Image saved to this management panel.",
  "Görsel kaldırıldı.": "Image removed.",
  "Kayıtlı görsel": "Saved image",
  "Görseli değiştirebilir veya kaldırabilirsiniz.": "You can replace or remove this image.",
  "Demo mağaza bağlantısı etkin. Değişiklikler bu sunum mağazasının verilerine kaydedilir.": "Demo store connected. Changes are saved to this presentation store.",
  "Değişiklikler kaydedildi ve yeniden doğrulandı.": "Changes were saved and verified.",
  "Bu değişiklik kaydedilene kadar taslak olarak kalır.": "This change remains a draft until saved.",
  "Bu alan şu anda yalnızca taslak olarak düzenlenebilir.": "This field can currently only be edited as a draft.",
  "Değişiklikler demo mağazasında saklanır.": "Changes are stored in the demo store.",
  "Taslak": "Draft",
  "Kaydetme işlemi doğrulanamadı.": "The save operation could not be verified.",
  "Mağaza ayarları yüklenemedi. Bağlantıyı kontrol edip yeniden deneyin.": "Store settings could not be loaded. Check the connection and try again.",
  "Oturum doğrulanamadı. Lütfen yeniden bağlanın.": "Your session could not be verified. Please reconnect.",
  "Mağaza ayarlarına şu anda ulaşılamıyor. Lütfen biraz sonra yeniden deneyin.": "Store settings are temporarily unavailable. Please try again shortly.",
  "Kaynak envanterde seçenek listelenmediği için seçim yapılamıyor.": "No options are listed in the source inventory, so no selection is available.",
  "Bir seçenek belirleyin": "Choose an option",
  "Görsel seç": "Choose an image",
  "Göz at": "Browse",
  "PNG, JPEG, WebP veya ICO · en fazla 5 MB": "PNG, JPEG, WebP, or ICO · up to 5 MB",
  "Bu dosya alanı için kaydetme desteği etkin değil.": "The file storage process is unverified, so this change cannot be saved.",
  "bayt": "bytes",
  "Görseli kaydet": "Save image",
  "Yükleniyor…": "Uploading…",
  "Görsel kaydedilemedi. Dosya biçimini ve boyutunu kontrol edip yeniden deneyin.": "Image could not be saved. Check its format and size, then try again.",
  "Görsel kaldırılamadı. Lütfen yeniden deneyin.": "Application image could not be removed. Try again.",
  "Görsel önizlemesi": "Image preview",
  "Bu uygulamada kayıtlı görsel": "Image stored in this application",
  "Görsel alanı": "Image field",
  "Görseli kaldır": "Remove image",
  "Görsel kaldırılıyor…": "Removing image…",
  "En fazla 5 MiB; PNG, JPEG, WebP veya ICO.": "Maximum 5 MiB; PNG, JPEG, WebP, or ICO.",
  "Desteklenmeyen düzenlemeler taslak olarak korundu.": "These changes were not saved. Unsupported edits remain as drafts.",
  "Değer boş veya desteklenmeyen biçimde olduğu için değiştirilemiyor.": "Cannot edit because the value is empty or has an unsupported format.",
  "Değer boş olduğu için değiştirilemiyor.": "Cannot edit because the value is empty.",
  "Sonuç bulunamadı": "No results found",
  "Farklı bir ayar adı, kategori veya teknik alan adı deneyin.": "Try another setting label, category, or technical name.",
  "Kaydedilmemiş değişiklikler": "Unsaved changes",
  " ayarda değişiklik var.": " settings have changes.",
  " değişiklik uygulanabilir": " changes can be applied",
  "; ": "; ",
  " değişiklik taslak olarak kalır.": " changes will remain drafts.",
  "Değişiklikleri geri al": "Discard changes",
  "Kaydediliyor…": "Saving…",
  "Değişiklikleri kaydet": "Save changes",
  "Kaydetme kapalıdır; değişiklikler yerel taslak olarak kalır.": "Saving is unavailable; changes remain drafts for this session.",
  "Kaydedilmemiş değişiklik": "Unsaved changes",
  "Kaydetme başarısız oldu. Ayarları yenileyip tekrar deneyin.": "Save failed. Refresh the settings and try again.",
  "Kaydetme sonucu doğrulanamadı. Yeni bir değişiklik yapmadan önce ayarları yenileyin.": "Save could not be verified. Refresh settings before making another change.",
  "Ayar yüklendikten sonra değişmiş. Yeniden yükleyip kontrol ettikten sonra tekrar deneyin.": "The setting changed after it was loaded. Refresh, review, and try again.",
  "Kaydedilmemiş değişiklikler geri alındı.": "Unsaved changes were discarded.",
  "Belirtilmemiş": "Not specified",
  "Seçim ayrıntıları teknik bilgiler bölümünde.": "Selection details are in Technical information.",
  "Etkin": "Enabled",
  "Devre dışı": "Disabled",
  "Site durumu": "Site status",
  "Bayilerde menü görünümü": "Dealer menu visibility",
  "Kaydedilebilir bir değişiklik bulunmuyor. Düzenlemeler taslak olarak kaldı.": "There are no saveable changes. Edits remain drafts.",
  "Bu değişiklik kaydedilemez.": "This change cannot be saved.",
  "Ayar kategorileri": "Settings categories",
  "Language": "Dil",
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
const searchInput = ref<HTMLInputElement | null>(null);
const selectedCategory = ref('General');
const selectedSection = ref('');
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
const saveEnabledIds = new Set([
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
  || allSettings.some((setting) => !isSettingSaveable(setting) && !isAssetSaveable(setting) && draftChanged(setting))
  || allSettings.some((setting) => isSettingSaveable(setting) && !customWriteIds.has(setting.parameterId) && draftChanged(setting)));
const unsupportedDrafts = computed(() => allSettings.filter((setting) => !isSettingSaveable(setting) && !isAssetSaveable(setting) && draftChanged(setting)));
const supportedDrafts = computed(() => allSettings.filter((setting) => isSettingSaveable(setting) && draftChanged(setting)));
const pendingSettingCount = computed(() => allSettings.filter((setting) => !isAssetSaveable(setting) && draftChanged(setting)).length);
const categoryCounts = computed(() => categories.map((category) => ({ ...category, count: category.settings.length })));
const itemById = computed(() => new Map(items.value.map((item) => [item.parameterId, item])));
const categorySettings = computed(() => categories.find((category) => category.name === selectedCategory.value)?.settings ?? []);
const sectionGroups = computed(() => [...new Set(categorySettings.value.map((setting) => setting.category))].map((name) => ({ name })));
const activeSection = computed(() => sectionGroups.value.some((section) => section.name === selectedSection.value)
  ? selectedSection.value : sectionGroups.value[0]?.name ?? '');
const sectionSettings = computed(() => categorySettings.value.filter((setting) => setting.category === activeSection.value));
const searchActive = computed(() => search.value.trim().length > 0);
const sectionLabelsTr: Record<string, string> = {
  'Visual Settings': 'Görünüm',
  'Color Settings': 'Renkler',
  'Design Settings': 'Tasarım',
  'Product Display Settings': 'Ürün Görünümü',
  'General Settings': 'Genel Ayarlar',
  'Cart Settings': 'Sepet Ayarları',
  'Application Form Settings': 'Başvuru Formu',
  'Google Analytics / Tag Manager Settings': 'Analitik ve Etiketler',
  'Admin': 'Yönetim',
  'General': 'Genel',
};
function sectionLabel(section: string): string {
  return language.value === 'tr' ? (sectionLabelsTr[section] ?? section) : section;
}
function selectCategory(name: string): void {
  selectedCategory.value = name;
  selectedSection.value = '';
  search.value = '';
}
const searchAliases: Record<string, string> = {
  'Site State': 'site durum acik kapali aktif pasif status website online offline',
  'Logo (Dark)': 'koyu logo dark resim gorsel fotograf marka image picture brand',
  'Logo (Light)': 'acik logo light resim gorsel fotograf marka image picture brand',
  'Logo (Log In)': 'giris login sign in giris sayfasi logo resim gorsel image picture',
  Favicon: 'site simgesi ikon icon browser tab sekme favicon',
  'Product Image Aspect Ratio': 'urun product resim gorsel fotograf image picture boyut olcu oran ratio',
  'Product Search Parameter': 'urun product arama ara bul search filtre filter bulma',
  'Payment Parameters': 'odeme payment banka bank kart card kredi credit taksit installment pos',
  'Payment Detail Show': 'odeme payment banka bank kart details detay',
  'Bank Installments Impressions': 'taksit installment banka odeme payment',
  'Product Currency': 'urun product para birimi currency doviz fiyat price exchange',
  'Pos Currency': 'pos para birimi currency doviz odeme payment',
  'Product Sorting': 'urun product siralama sort order artan azalan',
  'Stock Tracking': 'stok stock envanter inventory depo quantity miktar',
  'Stock Presentation Type': 'stok stock envanter inventory depo quantity miktar',
  'Stock Display Parameter': 'stok stock envanter inventory depo quantity miktar gosterim gorunum',
  'Minimum Inventory Quantity': 'min stok minimum inventory minstockfilter quantity miktar',
  'Send WhatsApp Order Notification': 'whatsapp mesaj bildirim notification siparis order',
  'Menu Display': 'menu menu navigasyon navigation navbar',
  'Menu Display 2': 'ek menu additional menu navigation navbar',
  'Delivery Date Get': 'teslimat delivery tarih date shipping sevkiyat',
  'Total Minimum Cart': 'sepet cart basket minimum alt limit quantity adet',
  'Min Cart Price': 'sepet cart basket minimum en dusuk tutar price amount',
};
function normalizeSearchText(value: string): string {
  return value.normalize('NFD').replace(/[\u0300-\u036f]/g, '').toLocaleLowerCase('tr-TR')
    .replace(/[ıİ]/g, 'i').replace(/[^a-z0-9]+/g, ' ').trim();
}
function getSearchFields(setting: SettingDefinition): { title: string; technical: string; description: string; options: string; all: string } {
  const title = normalizeSearchText([displayLabel(setting), englishDisplayLabel(setting), setting.originalLabel].join(' '));
  const technical = normalizeSearchText([setting.parameterId, setting.htmlControlId, setting.htmlControlName,
    setting.mapping.tableCandidate ?? '', setting.mapping.columnCandidate ?? '', setting.mapping.sqlDataType ?? ''].join(' '));
  const category = normalizeSearchText([setting.category, setting.panel, categoryFor(setting),
    categoryDisplayLabel(categoryFor(setting)), categoryEnglishLabel(categoryFor(setting))].join(' '));
  // Keep cross-control implementation notes out of the searchable description.
  // They can mention other controls (for example ParameterModules references ParameterAdmin),
  // which otherwise creates false-positive results for technical-ID searches.
  const description = normalizeSearchText([settingHelp(setting), helpTextEnglish(setting)].join(' '));
  const options = normalizeSearchText(setting.options.flatMap((option) => optionSearchLabels(setting.parameterId, option)).join(' '));
  const aliases = normalizeSearchText(searchAliases[setting.originalLabel] ?? '');
  return { title, technical, description, options, all: [title, technical, category, description, options, aliases].join(' ') };
}
function scoreSetting(setting: SettingDefinition, query: string, tokens: string[]): number {
  const fields = getSearchFields(setting);
  const allWords = fields.all.split(' ');
  const allTokensMatch = tokens.every((token) => fields.all.includes(token)
    || allWords.some((word) => word.startsWith(token) && token.length >= 3));
  if (!allTokensMatch) return 0;
  if (fields.title === query) return 1000;
  if (fields.title.startsWith(query)) return 900;
  if (fields.title.includes(query)) return 850;
  if (fields.technical.includes(query)) return 800;
  if (fields.options.includes(query)) return 700;
  if (fields.description.includes(query)) return 600;
  const titleHits = tokens.filter((token) => fields.title.includes(token)).length;
  const technicalHits = tokens.filter((token) => fields.technical.includes(token)).length;
  const optionHits = tokens.filter((token) => fields.options.includes(token)).length;
  return 300 + titleHits * 20 + technicalHits * 12 + optionHits * 8;
}
const visibleSettings = computed(() => {
  const source = searchActive.value ? allSettings : sectionSettings.value;
  const query = normalizeSearchText(search.value);
  if (!query) return source;
  const tokens = query.split(' ').filter(Boolean);
  return source.map((setting) => ({ setting, score: scoreSetting(setting, query, tokens) }))
    .filter((entry) => entry.score > 0)
    .sort((a, b) => b.score - a.score || settingLabel(a.setting).localeCompare(settingLabel(b.setting), language.value === 'tr' ? 'tr' : 'en'))
    .map((entry) => entry.setting);
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
    notice.value = t('Görsel bu yönetim paneline kaydedildi.');
  } catch (cause) {
    error.value = cause instanceof ApiError && cause.status === 401
      ? t('Oturum doğrulanamadı. Lütfen yeniden bağlanın.')
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
    notice.value = t('Görsel kaldırıldı.');
  } catch {
    error.value = t('Görsel kaldırılamadı. Lütfen yeniden deneyin.');
  } finally {
    assetDeletingId.value = '';
  }
}

function isLoaded(setting: SettingDefinition): boolean {
  return itemById.value.has(setting.parameterId);
}

function isAssetSaveable(setting: SettingDefinition): boolean {
  return setting.controlType === 'file' && canManageApplicationAssets.value;
}

function isSettingSaveable(setting: SettingDefinition): boolean {
  if (!canSaveSettings.value || !saveEnabledIds.has(setting.parameterId)) return false;
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

// Technical evidence is kept in the repository reports. In the customer-facing UI,
// avoid exposing local test database names and test-harness terminology.
function customerFacingTechnicalText(value: string | null | undefined): string {
  return String(value ?? '')
    .replace(/Store5_Settings_Test/gi, 'demo store data')
    .replace(/Store5_Analysis/gi, 'restricted production data')
    .replace(/test[- ]only/gi, 'application-specific')
    .replace(/test environment/gi, 'demo mode')
    .replace(/test database/gi, 'demo data store')
    .replace(/test veritabanı/gi, 'demo mağaza verisi')
    .replace(/test ortamı/gi, 'demo modu');
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
  notice.value = '';
}

async function saveChanges(): Promise<void> {
  if (!hasChanges.value || saving.value) return;
  if (!canSaveSettings.value || supportedDrafts.value.length === 0) {
    error.value = t('Kaydedilebilir bir değişiklik bulunmuyor. Düzenlemeler taslak olarak kaldı.');
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
    if (!isSettingSaveable(setting) || !draftChanged(setting)) continue;
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
    const allPendingIds = new Set([...retainedDraftIds, ...catalogChanges.map(change => change.parameterId), ...Object.keys(retainedFiles).filter(id => retainedFiles[id] != null)]);
    const groups = [
      catalogChanges.filter(change => !extendedWriteIds.has(change.parameterId)),
      catalogChanges.filter(change => extendedWriteIds.has(change.parameterId)),
    ].filter(group => group.length > 0);
    let needsFallbackRefresh = false;
    for (const group of groups) {
      const saved = await saveSettings(group);
      if (!saved.committed) throw new Error('Kaydetme işlemi doğrulanamadı.');
      group.forEach(change => allPendingIds.delete(change.parameterId));

      // The API already reads the committed values back from SQL and returns them.
      // Merge those values into the current catalog instead of reloading the entire
      // 98-setting catalog after every group and again at the end of the save.
      if (saved.values) {
        const returnedValues = saved.values;
        items.value = items.value.map(item => Object.prototype.hasOwnProperty.call(returnedValues, item.parameterId)
          ? { ...item, value: returnedValues[item.parameterId] }
          : item);
      } else {
        needsFallbackRefresh = true;
      }
    }
    if (needsFallbackRefresh) {
      const latest = await loadSettings();
      items.value = latest.items;
      mode.value = latest.mode;
    }
    syncDrafts(items.value);
    for (const id of allPendingIds) {
      if (id in retainedValues) draftValues.value[id] = retainedValues[id];
      if (retainedBits[id]) bitDrafts.value[id] = { ...retainedBits[id] };
      if (retainedConfigured[id]) configuredExtensionDrafts.value[id] = true;
      if (retainedFiles[id]) {
        fileDrafts.value[id] = retainedFiles[id];
        if (typeof URL.createObjectURL === 'function') filePreviewUrls.value[id] = URL.createObjectURL(retainedFiles[id]);
      }
    }
    notice.value = allPendingIds.size
      ? t('Kaydedilebilir ayarlar gruplar halinde uygulandı; kalan düzenlemeler taslak olarak korundu.')
      : t('Değişiklikler kaydedildi ve yeniden doğrulandı.');
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
  if (!publicDemo && !publicWriteMode.value) setAdminToken(adminTokenDraft.value);
  loading.value = true;
  error.value = '';
  notice.value = '';
  try {
    if (publicDemo) {
      const syntheticItems = createPublicDemoItems();
      items.value = syntheticItems;
      mode.value = 'Mock';
      canSaveSettings.value = false;
      canManageApplicationAssets.value = false;
      applicationAssets.value = {};
      syncDrafts(syntheticItems);
      notice.value = '';
      return;
    }

    const [result, capabilities] = await Promise.all([loadSettings(), loadSettingsCapabilities()]);
    items.value = result.items;
    mode.value = result.mode;
    canSaveSettings.value = capabilities.canSaveSettings === true
      && ((capabilities.environment === 'IsolatedLocalDemo' && capabilities.target === 'Store5_Settings_Test / store5')
        || (capabilities.environment === 'HostedStore' && capabilities.target === 'db82141 / store5')
        || (capabilities.environment === 'HostedPublicWrite' && capabilities.target === 'Public shared settings'));
    publicWriteMode.value = capabilities.environment === 'HostedPublicWrite';
    canManageApplicationAssets.value = capabilities.canManageApplicationAssets === true && canSaveSettings.value;
    syncDrafts(result.items);
    applicationAssets.value = {};
    if (canManageApplicationAssets.value) {
      try { applicationAssets.value = (await listApplicationAssets()).assets ?? {}; }
      catch { applicationAssets.value = {}; }
    }
    // Successful connection is indicated by the compact status in the top bar;
    // do not show a full-width 'settings loaded' banner.
    notice.value = '';
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
          ? t('Oturum doğrulanamadı. Lütfen yeniden bağlanın.')
          : cause.status === 503
            ? t('Mağaza ayarları yüklenemedi. Bağlantıyı kontrol edip yeniden deneyin.')
            : t('Ayarlar yüklenemedi. Lütfen yeniden deneyin.')
      : t('Beklenmeyen bir bağlantı hatası oluştu. Lütfen yeniden deneyin.');
  } finally {
    loading.value = false;
  }
}

function focusSearch(): void {
  searchInput.value?.focus();
  searchInput.value?.select();
}
function handleGlobalSearchShortcut(event: KeyboardEvent): void {
  const target = event.target instanceof HTMLElement ? event.target : null;
  const typing = !!target?.closest('input, textarea, select, [contenteditable="true"]');
  if ((event.ctrlKey || event.metaKey) && event.key.toLowerCase() === 'k') {
    event.preventDefault();
    focusSearch();
    return;
  }
  if (event.key === '/' && !typing && !event.altKey && !event.ctrlKey && !event.metaKey) {
    event.preventDefault();
    focusSearch();
    return;
  }
  if (event.key === 'Escape' && (document.activeElement === searchInput.value || event.target === searchInput.value)) {
    event.preventDefault();
    // Clear the native input immediately as well as the Vue model. This keeps
    // the keyboard shortcut responsive before Vue's next render tick.
    search.value = '';
    if (searchInput.value) searchInput.value.value = '';
    searchInput.value?.blur();
  }
}

if (publicDemo) void connect();
onMounted(() => {
  window.addEventListener('keydown', handleGlobalSearchShortcut);
  if (!publicDemo) {
    // Discover the explicit public-write deployment without asking visitors for an admin token.
    // The capability response itself contains no secrets and is exposed only as a mode marker.
    void loadSettingsCapabilities().then(capabilities => {
      if (capabilities.environment === 'HostedPublicWrite' && capabilities.canSaveSettings === true) {
        publicWriteMode.value = true;
        void connect();
      }
    }).catch(() => { /* Protected deployments continue to show the admin-token form. */ });
  }
});
onBeforeUnmount(() => {
  window.removeEventListener('keydown', handleGlobalSearchShortcut);
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
          @click="selectCategory(category.name)">
          <span>{{ categoryLabel(category.name) }}</span>
        </button>
      </nav>
      <div class="sidebar-foot"><span class="status-dot" aria-hidden="true"></span> B2B STORE</div>
    </aside>

    <aside v-if="!searchActive" class="section-sidebar" :aria-label="t('Ayar bölümleri')">
      <div class="section-sidebar-heading">
        <span>{{ t('AYARLAR') }}</span>
        <h2>{{ categoryLabel(selectedCategory) }}</h2>
      </div>
      <p class="section-nav-label">{{ t('BÖLÜMLER') }}</p>
      <nav class="section-nav">
        <button v-for="section in sectionGroups" :key="section.name" type="button"
          class="section-nav-item" :class="{ active: activeSection === section.name }"
          :aria-current="activeSection === section.name ? 'page' : undefined"
          @click="selectedSection = section.name">
          <span>{{ sectionLabel(section.name) }}</span>
        </button>
      </nav>
    </aside>

    <main id="main" class="main-area" :class="{ 'main-area-search': searchActive }">
      <header class="topbar">
        <div class="crumb"><span class="crumb-root">B2B STORE</span><span>/</span><b>{{ searchActive ? t('Arama sonuçları') : categoryLabel(selectedCategory) }}</b></div>
        <div class="topbar-search">
          <label class="search-box">
            <span class="search-icon" aria-hidden="true">⌕</span>
            <span class="sr-only">{{ t('Ayar adında veya teknik adında ara') }}</span>
            <input ref="searchInput" v-model="search" type="search" :placeholder="t('Ayarlarda ara...')" aria-keyshortcuts="Control+K Meta+K" :title="t('Ctrl+K ile ara')" />
            <kbd class="search-shortcut">Ctrl K</kbd>
          </label>
          <button v-if="search" class="search-clear" type="button" :aria-label="language === 'tr' ? 'Aramayı temizle' : 'Clear search'" @mousedown.prevent @click="search = ''; focusSearch()">×</button>
        </div>
        <div class="topbar-actions">
          <div class="language-switch" role="group" :aria-label="t('Language')">
            <button type="button" :aria-pressed="language === 'en'" @click="setLanguage('en')">EN</button>
            <button type="button" :aria-pressed="language === 'tr'" @click="setLanguage('tr')">TR</button>
          </div>
          <div class="mode-pill" :class="mode === 'SqlServer' ? 'mode-sql' : ''"><span aria-hidden="true"></span>{{ publicDemo ? t('Herkese açık demo') : mode === 'Mock' ? t('Demo') : mode ? t('Bağlandı') : t('Bağlı değil') }}</div>
        </div>
      </header>

      <div class="content">
        <div v-if="publicWriteMode" class="message message-public" role="status">
          <span aria-hidden="true">!</span>
          {{ language === 'tr' ? 'Herkese açık düzenleme: Bu sayfadaki değişiklikler ortak veritabanına kaydedilir ve diğer ziyaretçiler tarafından da değiştirilebilir.' : 'Public editing is enabled: changes on this page are saved to a shared database and can also be changed by other visitors.' }}
        </div>
        <section class="page-heading">
          <div><h1>{{ searchActive ? t('Arama sonuçları') : sectionLabel(activeSection || selectedCategory) }}</h1></div>
          <div class="connect-panel">
            <label v-if="!publicDemo && !publicWriteMode" class="admin-token-entry">
              <span>{{ language === 'tr' ? 'Yönetici anahtarı' : 'Admin token' }}</span>
              <input v-model="adminTokenDraft" type="password" autocomplete="current-password" :placeholder="language === 'tr' ? 'MonsterASP yönetici tokenı' : 'MonsterASP admin token'" />
            </label>
            <div v-if="canSaveSettings && supportedDrafts.length > 0" class="save-bar" role="region" :aria-label="t('Kaydedilmemiş değişiklikler')">
              <span class="save-summary"><b>{{ pendingSettingCount }}{{ t(' ayarda değişiklik var.') }}</b> {{ supportedDrafts.length }}{{ t(' değişiklik uygulanabilir') }}<span v-if="unsupportedDrafts.length">{{ t('; ') }}{{ unsupportedDrafts.length }}{{ t(' değişiklik taslak olarak kalır.') }}</span></span>
              <div>
                <button class="button button-secondary" :disabled="saving" @click="discardChanges">{{ t('Değişiklikleri geri al') }}</button>
                <button class="button button-primary" :disabled="saving" @click="saveChanges">{{ saving ? t('Kaydediliyor…') : t('Değişiklikleri kaydet') }}</button>
              </div>
            </div>
            <span v-if="notice" class="save-status" role="status">{{ notice }}</span>
            <button class="button button-primary" :disabled="loading || saving" @click="connect">{{ loading ? t('Bağlanıyor…') : publicDemo ? t('Demo verilerini sıfırla') : mode ? t('Ayarları yenile') : t('Bağlan') }}</button>
          </div>
        </section>

        <div v-if="error" class="message message-error" role="alert"><span aria-hidden="true">!</span>{{ error }}</div>

        <section v-if="loading" class="empty-state" aria-live="polite"><div class="spinner" aria-hidden="true"></div><h3>{{ t('Ayarlar yükleniyor') }}</h3><p>{{ t('Yapılandırılmış mağaza kaydı okunuyor.') }}</p></section>
        <section v-else-if="!items.length" class="empty-state"><div class="empty-icon" aria-hidden="true">⌁</div><h3>{{ t('Mevcut ayarları görmek için bağlanın') }}</h3><p>{{ t('Ayar kataloğunu inceleyebilir, Bağlan düğmesiyle yerel kaynaktaki değerleri yükleyebilirsiniz.') }}</p></section>
        <section v-else class="settings-groups" aria-live="polite">
          <div v-for="group in groupedResults" :key="group.name" class="settings-group">
            <div v-if="searchActive" class="group-heading"><h2>{{ categoryLabel(group.name) }}</h2></div>
            <article v-for="setting in group.settings" :key="setting.parameterId" class="setting-card">
              <div class="setting-copy">
                <div class="setting-title-row"><h3>{{ settingLabel(setting) }}</h3><span v-if="draftChanged(setting)" class="setting-dirty">{{ t('Kaydedilmemiş değişiklik') }}</span><span v-else-if="canSaveSettings && !isAssetSaveable(setting) && !isSettingSaveable(setting)" class="setting-read-only">{{ t('Taslak') }}</span></div>
                <p class="help-text">{{ settingHelp(setting) }}</p>
                <details class="technical-details">
                  <summary>{{ t('Teknik bilgiler') }}</summary>
                  <dl>
                    <dt>{{ t('Orijinal etiket') }}</dt><dd>{{ setting.originalLabel }}</dd>
                    <dt>{{ t('HTML denetim kimliği') }}</dt><dd><code>{{ setting.htmlControlId }}</code></dd>
                    <dt>{{ t('HTML denetim adı') }}</dt><dd><code>{{ setting.htmlControlName }}</code></dd>
                    <dt>{{ t('Kategori') }}</dt><dd>{{ categoryLabel(categoryFor(setting)) }}</dd>
                    <dt>{{ t('Veritabanı alanı') }}</dt><dd><code>{{ setting.mapping.tableCandidate && setting.mapping.columnCandidate ? `${setting.mapping.tableCandidate}.${setting.mapping.columnCandidate}` : t('Eşleşme doğrulanmadı') }}</code></dd>
                    <dt>{{ t('SQL veri türü') }}</dt><dd>{{ setting.mapping.sqlDataType ?? t('Doğrulanmadı') }}</dd>
                    <dt>{{ t('Şema uyumluluğu') }}</dt><dd>{{ schemaCompatibility(setting) }}</dd>
                    <dt>{{ t('Eşleme durumu') }}</dt><dd>{{ mappingLabel(setting) }}</dd>
                    <dt>{{ t('Eski kaydetme davranışı') }}</dt><dd>{{ customerFacingTechnicalText(legacySaveBehavior(setting)) }}</dd>
                    <dt>{{ t('Değer kodlaması') }}</dt><dd>{{ customerFacingTechnicalText(valueEncoding(setting)) }}</dd>
                    <dt>{{ t('Ham SQL değeri') }}</dt><dd><code>{{ rawValue(setting) }}</code></dd>
                    <dt>{{ t('Kanıt') }}</dt><dd>{{ customerFacingTechnicalText(setting.mapping.evidence) }}</dd>
                    <template v-if="setting.mapping.outstandingQuestion"><dt>{{ t('Açık soru') }}</dt><dd>{{ customerFacingTechnicalText(setting.mapping.outstandingQuestion) }}</dd></template>
                  </dl>
                </details>
              </div>
              <div class="setting-value">
                <span class="read-only-label">{{ isLoaded(setting) ? t('Mevcut değer') : t('Yüklenmedi') }}</span>
                <template v-if="!isLoaded(setting)"><span class="value-empty">{{ t('Değeri yüklemek için bağlanın.') }}</span></template>
                <template v-else-if="setting.parameterId === siteStatusId && siteStatusCanEdit">
                  <label class="sr-only" for="setting-site-status">{{ t('Site durumu') }}</label>
                  <select id="setting-site-status" class="setting-input" v-model="siteStatusDraft" :disabled="saving"><option value="1">{{ t('Aktif') }}</option><option value="0">{{ t('Pasif') }}</option></select>
                </template>
                <template v-else-if="setting.parameterId === parameterOrderId && parameterOrderCanEdit">
                  <div class="editable-options"><label v-for="option in parameterOrderSetting.options" :key="option.value" class="editable-option"><input v-model="orderDraft[option.value]" type="checkbox" :value="option.value" :disabled="saving" /> <span>{{ optionLabel(parameterOrderSetting.parameterId, option) }}</span></label></div>
                  <span class="value-hint">{{ t('Yalnızca seçtiğiniz seçenekler değişir.') }}</span>
                </template>
                <template v-else-if="setting.parameterId === layoutId && layoutCanEdit">
                  <label class="sr-only" for="setting-layout">{{ t('Layout') }}</label>
                  <select id="setting-layout" class="setting-input" v-model="layoutDraft" :disabled="saving"><option v-for="option in layoutSetting.options" :key="option.value" :value="option.value">{{ optionLabel(layoutSetting.parameterId, option) }}</option></select>
                </template>
                <template v-else-if="setting.parameterId === menuVisibilityId && menuVisibilityCanEdit">
                  <label class="sr-only" for="setting-menu-visibility">{{ t('Bayilerde menü görünümü') }}</label>
                  <select id="setting-menu-visibility" class="setting-input" v-model="menuVisibilityDraft" :disabled="saving"><option v-for="option in menuVisibilitySetting.options" :key="option.value" :value="option.value">{{ optionLabel(menuVisibilitySetting.parameterId, option) }}</option></select>
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
                  <span v-if="setting.parameterId === scheduleId" class="value-hint">{{ t('Ürün içe aktarma davranışını etkiler.') }}</span>
                  <span v-else-if="!canSaveSettings && hasIntegralBitmaskEvidence(setting)" class="value-hint">{{ t('Bu değişiklik kaydedilene kadar taslak olarak kalır.') }}</span>
                  <span v-else-if="!canSaveSettings" class="value-hint">{{ t('Bu alan şu anda yalnızca taslak olarak düzenlenebilir.') }}</span>
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
                  <label class="file-upload">
                    <span class="file-upload-icon" aria-hidden="true">↑</span>
                    <span class="file-upload-copy"><strong>{{ fileDrafts[setting.parameterId]?.name ?? t('Görsel seç') }}</strong><small>{{ t('PNG, JPEG, WebP veya ICO · en fazla 5 MB') }}</small></span>
                    <span class="file-upload-action">{{ t('Göz at') }}</span>
                    <input :key="fileResetKey" class="file-upload-input" type="file" accept=".png,.jpg,.jpeg,.webp,.ico,image/png,image/jpeg,image/webp,image/x-icon" :aria-label="settingLabel(setting)" @change="setFileDraft(setting.parameterId, $event)" />
                  </label>
                  <span class="value-hint">{{ t('Bu görsel yeni yönetim panelinde kullanılır.') }}</span>
                  <span v-if="applicationAssets[setting.parameterId]" class="asset-meta">{{ t('Kayıtlı görsel') }} · {{ applicationAssets[setting.parameterId].contentType }} · {{ applicationAssets[setting.parameterId].length }} {{ t('bayt') }}</span>
                  <div v-if="canManageApplicationAssets && (fileDrafts[setting.parameterId] || applicationAssets[setting.parameterId])" class="asset-actions">
                    <button v-if="fileDrafts[setting.parameterId]" class="button button-primary" :disabled="assetSavingId !== '' || assetDeletingId !== ''" @click="saveApplicationAsset(setting.parameterId)">{{ assetSavingId === setting.parameterId ? t('Yükleniyor…') : t('Görseli kaydet') }}</button>
                    <button v-if="applicationAssets[setting.parameterId]" class="button button-secondary" :disabled="assetSavingId !== '' || assetDeletingId !== ''" @click="removeApplicationAsset(setting.parameterId)">{{ assetDeletingId === setting.parameterId ? t('Görsel kaldırılıyor…') : t('Görseli kaldır') }}</button>
                  </div>
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

      </div>
    </main>
  </div>
</template>
