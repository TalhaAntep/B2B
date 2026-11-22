import rawCatalog from '../../docs/parameter-catalog.json';
import rawAudit from '../../docs/mapping-audit.json';

export interface CatalogOption {
  value: string;
  label: string;
}

export type MappingStatus = 'candidate' | 'unresolved' | 'verified';
export type ControlType = 'select' | 'bitmask' | 'checkbox' | 'text' | 'textarea' | 'radio' | 'file';

export interface SettingDefinition {
  parameterId: string;
  originalLabel: string;
  panel: string;
  category: string;
  htmlControlName: string;
  htmlControlId: string;
  controlType: ControlType;
  multiple: boolean;
  options: CatalogOption[];
  mapping: {
    status: MappingStatus;
    tableCandidate: string | null;
    columnCandidate: string | null;
    sqlDataType: string | null;
    nullable: boolean | null;
    evidence: string;
    outstandingQuestion: string | null;
  };
  valueSemantics: string;
}

export interface MappingAuditEntry {
  htmlControlId: string;
  schemaCompatible: boolean;
  legacySaveBehavior: string;
  mappingStatus: string;
  sqlType: string | null;
  encoding: string;
}

export interface SettingGroup {
  panel: string;
  section: string;
  controls: SettingDefinition[];
}

export const catalog = rawCatalog as {
  schemaVersion: number;
  controlCount: number;
  statusCounts: Record<string, number>;
  groups: SettingGroup[];
};

export const allSettings = catalog.groups.flatMap((group) => group.controls);
export const mappingAudit = new Map<string, MappingAuditEntry>(
  (rawAudit as MappingAuditEntry[]).map((entry) => [entry.htmlControlId, entry]),
);

const categoryOrder = ['Design', 'Menu', 'Products', 'Product Details', 'Search', 'Cart', 'Orders', 'Payments', 'Accounts', 'Admin', 'General'];

const categoryLabels: Record<string, string> = {
  Design: 'Tasarım', Menu: 'Menü', Products: 'Ürünler', 'Product Details': 'Ürün Detayları',
  Search: 'Arama', Cart: 'Sepet', Orders: 'Siparişler', Payments: 'Ödemeler',
  Accounts: 'Hesaplar', Admin: 'Yönetim', General: 'Genel',
};

export function categoryDisplayLabel(category: string): string {
  return categoryLabels[category] ?? category;
}

export function categoryEnglishLabel(category: string): string {
  return category;
}

export function categoryFor(setting: SettingDefinition): string {
  const haystack = `${setting.originalLabel} ${setting.htmlControlName} ${setting.mapping.columnCandidate ?? ''}`.toLocaleLowerCase();
  if (/productdetail|product detail/.test(haystack)) return 'Product Details';
  if (/menu/.test(haystack)) return 'Menu';
  if (/design|layout|theme|logo|favicon|color|aspect ratio/.test(haystack)) return 'Design';
  if (/search|filter|sortfilter/.test(haystack)) return 'Search';
  if (/cart|basket|mincart|maximumorder|variationcode/.test(haystack)) return 'Cart';
  if (/payment|commission|installment|poscurrency/.test(haystack)) return 'Payments';
  if (/order|delivery|invoice|shipping/.test(haystack)) return 'Orders';
  if (/account|cari|customer|dealer|register|application|discount|bonus|balance/.test(haystack)) return 'Accounts';
  if (/product|stock|price|export|parametric/.test(haystack)) return 'Products';
  if (setting.panel === 'Admin' && setting.category === 'Admin') return 'Admin';
  if (setting.panel === 'User' && setting.category === 'Visual Settings') return 'Design';
  if (setting.panel === 'User' && setting.category === 'Color Settings') return 'Design';
  return setting.panel === 'Admin' ? 'Admin' : 'General';
}

export const categories = categoryOrder.map((name) => ({
  name,
  settings: allSettings.filter((setting) => categoryFor(setting) === name),
}));

const friendlyLabels: Record<string, string> = {
  'Site State': 'Site Durumu', Layout: 'Yerleşim', 'Product Image Aspect Ratio': 'Ürün Görseli En-Boy Oranı',
  'Logo (Dark)': 'Koyu Logo', 'Logo (Light)': 'Açık Logo', 'Logo (Log In)': 'Giriş Sayfası Logosu',
  Favicon: 'Site Simgesi', 'Default Image in Non-Image Products': 'Görseli Olmayan Ürünler İçin Varsayılan Görsel',
  'Layout Color': 'Yerleşim Rengi', 'Layout Color 2': 'İkinci Yerleşim Rengi', 'Meta Tags': 'Meta Etiketleri',
  'Script Codes': 'Script Kodları', 'NoScript Codes': 'NoScript Kodları', 'Category Menu': 'Kategori Menüsü',
  'Design Parameters': 'Tasarım Ayarları', 'Theme Parameter': 'Tema Ayarı', "Don't Show Menu At Dealers": 'Bayilerde Menüyü Gösterme',
  'Filter Show': 'Filtre Görünümü', 'Filter Display': 'Filtre Seçenekleri', 'Product Sorting': 'Ürün Sıralaması',
  'Dealer Limit Tracking': 'Bayi Limit Takibi', 'Current Limit Warning': 'Mevcut Limit Uyarısı',
  'Automatic Definition': 'Otomatik Tanımlama', 'Dont Show Price': 'Fiyat Görünümü', 'Stock Tracking': 'Stok Takibi',
  'Stock Presentation Type': 'Stok Gösterim Biçimi', 'Stock Display Parameter': 'Stok Gösterim Seçenekleri',
  'Tax Excluded Products Show': 'Vergi Hariç Ürün Gösterimi', 'B2B & B2C Partners': 'B2B ve B2C İş Ortakları',
  'Product Detail Parameter': 'Ürün Detayları', 'Product Detail Parameter 2': 'Ek Ürün Detayları',
  'Ürün Detay Parametresi (B2C - Oturum Kapalı)': 'Ürün Detayları (Oturum Açmamış B2C)',
  'Ürün Detay Parametresi 2 (B2C - Oturum Kapalı)': 'Ek Ürün Detayları (Oturum Açmamış B2C)',
  'Product Search Parameter': 'Ürün Arama Seçenekleri', 'Show Export Button': 'Dışa Aktarma Seçenekleri',
  'Parametric Modules': 'Parametrik Modüller', 'Total Minimum Cart': 'Sepet Alt Sınırı',
  'Payment Detail Show': 'Ödeme Detayları', 'Show  Product Variation Code In Cart': 'Sepette Ürün Varyasyon Kodu',
  'Min Cart Price': 'En Düşük Sepet Tutarı', 'Maximum Product Order Quantity': 'En Yüksek Ürün Sipariş Adedi',
  'Delivery Date Get': 'Teslimat Tarihi İste', 'Minimum Inventory Quantity': 'En Düşük Stok Adedi',
  'Discount Type': 'İndirim Türü', 'Login Screen  ( Login Page )': 'Giriş Sayfası',
  'Bank Installments Impressions': 'Banka Taksit Bilgileri', 'Login With Expiry': 'Son Kullanma Tarihli Giriş',
  'Use Currency Type': 'Para Birimi Türü', 'Current Maturity Date': 'Güncel Vade Tarihi',
  'Current As Well Total Balance': 'Güncel ve Toplam Bakiye', 'E-PayCommission': 'Elektronik Ödeme Komisyonu',
  'Discount Authorization for Customer Representative': 'Müşteri Temsilcisi İndirim Yetkisi',
  'Registration Form Link': 'Üyelik Başvuru Formu Bağlantısı', 'Main Page': 'Ana Sayfa',
  'Current Account Payment': 'Cari Hesap Ödemesi', 'Dealer Phone': 'Bayi Telefonu', 'Dealer Adress': 'Bayi Adresi',
  'Search By Brand': 'Markaya Göre Arama', 'Advanced Search': 'Gelişmiş Arama',
  'Using the Quick Search Listings': 'Hızlı Arama Listeleri', 'Desktop Application': 'Masaüstü Uygulaması',
  'Current Account Details': 'Cari Hesap Detayları', 'Product Currency': 'Ürün Para Birimi', 'Pos Currency': 'POS Para Birimi',
  'Integration     Parameter': 'Entegrasyon Seçenekleri', 'Show details add button is clicked': 'Ekle Düğmesine Basıldığında Detayları Göster',
  'Do Not Send Mail To Customer': 'Müşteriye E-posta Gönderme', 'Send Delivery Address If Different': 'Farklı Teslimat Adresi Gönder',
  'Currency Price Type': 'Para Birimi Fiyat Türü', 'All Products': 'Tüm Ürünler', 'Different Payment Link': 'Alternatif Ödeme Bağlantısı',
  'Payment Parameters': 'Ödeme Seçenekleri', 'Application Form Settings': 'Üyelik Başvuru Formu Ayarları',
  'Application Form Menu Display': 'Başvuru Formu Menü Görünümü', 'Admin  Parameter': 'Yönetici Seçenekleri',
  'Cart Parameter': 'Sepet Seçenekleri', 'Account Parameter': 'Hesap Seçenekleri', 'Main Products': 'Ana Ürünler',
  Integrated: 'Entegrasyon', 'Add New Shipping Address': 'Yeni Teslimat Adresi Ekle', Bonus: 'Bonus',
  'Site Map': 'Site Haritası', 'Dealer Sub-Dealer Property': 'Bayi Alt Bayi Özelliği',
  'Adding Address You Added Company Information': 'Adres Eklerken Firma Bilgisi', 'Dealer Name': 'Bayi Adı',
  'Recaptcha Code': 'reCAPTCHA Kodu', 'Dealer Website': 'Bayi İnternet Sitesi', 'E Invoice No': 'E-Fatura Numarası',
  'Use Encryption': 'Şifreleme Kullanımı', 'Variations Showing': 'Varyant Görünümü', 'Menu Display': 'Menü Görünümü',
  'Menu Display 2': 'Ek Menü Görünümü', 'Dynamic Search Parameter': 'Dinamik Arama Seçenekleri',
  'Currency type in order integration transfer': 'Sipariş Entegrasyonunda Para Birimi',
  'Send WhatsApp Order Notification': 'WhatsApp Sipariş Bildirimi',
  'Favorite Icon Position / Left / Right / Top / Bottom': 'Favori Simgesinin Konumu',
};

export function displayLabel(setting: SettingDefinition): string {
  return friendlyLabels[setting.originalLabel] ?? setting.originalLabel;
}

export function englishDisplayLabel(setting: SettingDefinition): string {
  return setting.originalLabel;
}

const settingPurpose: Record<string, string> = {
  'Site State': 'Belirtilen seçenekler siteyi aktif veya pasif duruma getirir.',
  Layout: 'Ürün listelerindeki sepete ekle düğmesinin hizasını belirler.',
  'Product Image Aspect Ratio': 'Ürün görselleri için en-boy oranını belirtir.',
  'Logo (Dark)': 'Site için koyu logo dosyasını seçer.', 'Logo (Light)': 'Site için açık logo dosyasını seçer.',
  'Logo (Log In)': 'Giriş sayfasında kullanılacak logo dosyasını seçer.', Favicon: 'Tarayıcı sekmesinde görünen site simgesini seçer.',
  'Default Image in Non-Image Products': 'Kendi görseli olmayan ürünlerde gösterilecek görseli seçer.',
  'Layout Color': 'Site yerleşimi için renk değerini belirtir.', 'Layout Color 2': 'Site yerleşimi için ikinci renk değerini belirtir.',
  'Meta Tags': 'Site için girilmiş meta etiketlerini içerir.', 'Script Codes': 'Site için girilmiş script kodlarını içerir.',
  'NoScript Codes': 'Site için girilmiş NoScript kodlarını içerir.',
  'Product Sorting': 'Ürün listelerinde kullanılabilen sıralama seçeneklerini belirler.',
  'Filter Display': 'Kaynak formda filtre seçenekleri listelenmiştir; etkileri doğrulanmamıştır.',
  'Stock Presentation Type': 'Ürün listelerinde stok durumunun nasıl gösterileceğini belirler.',
  'Product Detail Parameter': 'Ürün detay sayfasında listelenen bilgi ve işlem seçeneklerini belirler.',
  'Product Detail Parameter 2': 'Ürün detay sayfasındaki ek seçenekleri belirler.',
  'Product Search Parameter': 'Ürün aramada sunulan seçenekleri belirler.', 'Total Minimum Cart': 'Sepette bulunması gereken en düşük ürün adedini belirtir.',
  'Min Cart Price': 'Sepet için belirtilen en düşük tutarı gösterir.', 'Maximum Product Order Quantity': 'Ürün başına belirtilen en yüksek sipariş adedini gösterir.',
  'Delivery Date Get': 'Sipariş sırasında teslimat tarihi istenip istenmediğini belirtir.',
  'Dealer Phone': 'Bayi telefon numarası alanını içerir.', 'Dealer Adress': 'Bayi adresi alanını içerir.',
  'Product Currency': 'Ürün fiyatlarında kullanılan para birimini belirler.', 'Pos Currency': 'POS ödemelerinde kullanılan para birimini belirler.',
  'Payment Parameters': 'Formda listelenen ödeme seçeneklerini içerir.', 'Menu Display': 'Formda listelenen menü seçeneklerini içerir.',
  'Menu Display 2': 'Kaynak formda ek bir menü seçeneği listelenmiştir; etkisi doğrulanmamıştır.',
  'Send WhatsApp Order Notification': 'Kaynak formda WhatsApp sipariş bildirimi seçeneği listelenmiştir; kayıt davranışı doğrulanmamıştır.',
  'Favorite Icon Position / Left / Right / Top / Bottom': 'Kaynak formda favori simgesi konum seçenekleri listelenmiştir; kayıt davranışı doğrulanmamıştır.',
};

export function helpText(setting: SettingDefinition): string {
  return settingPurpose[setting.originalLabel] ?? '';
}

const englishPurpose: Record<string, string> = {
  'Site State': 'Choose whether the site is active or inactive.',
  Layout: 'Sets the alignment of the Add to Cart button in product listings.',
  'Product Image Aspect Ratio': 'Sets the aspect ratio used for product images.',
  'Logo (Dark)': 'Selects the dark logo file for the site.',
  'Logo (Light)': 'Selects the light logo file for the site.',
  'Logo (Log In)': 'Selects the logo used on the login page.',
  Favicon: 'Selects the site icon shown in the browser tab.',
  'Default Image in Non-Image Products': 'Selects the image shown for products without their own image.',
  'Layout Color': 'Stores a color value for the site layout.',
  'Layout Color 2': 'Stores a second color value for the site layout.',
  'Meta Tags': 'Contains the meta tags entered for the site.',
  'Script Codes': 'Contains script code entered for the site.',
  'NoScript Codes': 'Contains NoScript code entered for the site.',
  'Product Sorting': 'Lists sorting options available for product listings.',
  'Filter Display': 'The source form lists filter options; their effects have not been verified.',
  'Stock Presentation Type': 'Sets how stock status is presented in product listings.',
  'Product Detail Parameter': 'Lists information and actions available on the product detail page.',
  'Product Detail Parameter 2': 'Lists additional options for the product detail page.',
  'Product Search Parameter': 'Lists options offered in product search.',
  'Total Minimum Cart': 'Sets the minimum number of products required in the cart.',
  'Min Cart Price': 'Stores the minimum cart amount shown by this setting.',
  'Maximum Product Order Quantity': 'Stores the stated maximum order quantity per product.',
  'Delivery Date Get': 'Indicates whether an order asks for a delivery date.',
  'Dealer Phone': 'Contains the dealer phone field value.',
  'Dealer Adress': 'Contains the dealer address field value.',
  'Product Currency': 'Sets the currency used for product prices.',
  'Pos Currency': 'Sets the currency used for POS payments.',
  'Payment Parameters': 'Lists payment options shown by the source form.',
  'Menu Display': 'Lists menu options shown by the source form.',
  'Menu Display 2': 'The source form lists an additional menu option; its effect is unverified.',
  'Send WhatsApp Order Notification': 'The source form lists a WhatsApp order notification option; its persistence is unverified.',
  'Favorite Icon Position / Left / Right / Top / Bottom': 'Sets the favorite icon position from the listed options.',
};

export function helpTextEnglish(setting: SettingDefinition): string {
  return englishPurpose[setting.originalLabel] ?? '';
}

export function mappingLabel(setting: SettingDefinition): string {
  if (setting.mapping.status === 'verified') return 'Confirmed mapping';
  if (setting.mapping.status === 'unresolved' || mappingAudit.get(setting.htmlControlId)?.mappingStatus === 'Unresolved mapping') return 'Unresolved · read-only';
  if (mappingAudit.get(setting.htmlControlId)?.schemaCompatible) return 'Schema-compatible · save unknown';
  return 'Candidate · read-only';
}
