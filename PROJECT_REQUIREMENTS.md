# B2B STORE – Teknik Ayarlar Modülü
## Proje Tanımı, Teknik Gereksinimler ve Kabul Kriterleri

> **Belge durumu:** Başlangıç gereksinim dokümanı  
> **Tarih:** 19 Kasım 2026  
> **Hedef:** Codex'in projeyi doğru anlaması, kaynakları incelemesi ve geliştirmeyi kontrollü biçimde yürütmesi.

---

## 1. Projenin tek cümlelik tanımı

Mevcut B2B STORE yönetim panelindeki Teknik Ayarlar (DealerSystemSetting) modülünün bütün parametrelerini, veritabanı karşılıklarını, Bitwise yapılarını ve bayi portalına etkilerini analiz edip test ederek; mevcut davranışları koruyan, Vue.js + TypeScript arayüzü, C# .NET Core Web API backend'i ve Microsoft SQL Server veri katmanı olan modern, sürdürülebilir ve internette ücretsiz bir hizmet üzerinde yayınlanabilir yeni bir modül geliştirmek.

## 2. İş bağlamı ve hedef kullanıcılar

B2B STORE, bir işletmenin kendi bayilerine ürün, stok, fiyat, sepet, sipariş ve benzeri B2B e-ticaret işlevleri sunduğu bir platformdur. Bu görev bütün B2B STORE platformunu yeniden yazma görevi değildir. Kapsam, mevcut yönetim panelindeki **Teknik Ayarlar** ekranının analiz edilmesi ve bu ayarları yönetebilen yeni bir modül geliştirilmesidir.

İlgili taraflar:

- **Yönetim paneli:** `https://platform.b2bstore.com/`
- **Teknik Ayarlar ekranı:** `https://platform.b2bstore.com/DealerSystemSetting`
- **Bayi portalı:** `https://store1.b2bstore.com/`
- **Hedef kullanıcı:** Teknik bilgisi sınırlı olsa da ayarların etkisini anlayıp güvenle değiştirebilmesi gereken B2B müşteri/yönetici.
- **Geliştirme kullanıcısı:** Modülü analiz edecek, kodlayacak, test edecek ve teknik görüşmede açıklayacak aday.

Görev mesajında test ortamı için giriş bilgileri verilmiştir. Bu bilgiler **kaynak koduna, Git deposuna, Markdown belgelerine, istemci tarafı koduna, ekran görüntülerine veya herkese açık yapay zekâ istemlerine yazılmamalıdır**. Giriş bilgileri yalnızca yetkili test ortamında ve gerektiği ölçüde kullanılmalıdır.

## 3. Kaynaklar ve kanıt durumu

Codex, aşağıdaki kaynakları birbirinden ayırmalı ve doğrulanmamış bilgileri kesin gerçek gibi sunmamalıdır.

| Kaynak | Konum / açıklama | Durum ve kullanım |
|---|---|---|
| İş tanımı | Kullanıcının paylaştığı “İŞ BAŞVURUSU – 2. AŞAMA TEKNİK DEĞERLENDİRME” metni | **Gereksinim kaynağı.** Kapsam, teknoloji ve teslimatlar için esas alınır. |
| Admin paneli | `https://platform.b2bstore.com/` | Oturum gerektirir. Yalnızca yetkili test hesabıyla incelenir. |
| Teknik Ayarlar | `https://platform.b2bstore.com/DealerSystemSetting` | Parametre envanteri ve mevcut UI davranışının birincil referansı. Giriş sonrası bütün kategoriler incelenmelidir. |
| Bayi portalı | `https://store1.b2bstore.com/` | Ayar değişikliklerinin gözlemlenebilir etkilerini test etmek için referans. |
| Kaynak arşivi | Kullanıcı tarafından verilen Dropbox `store5.rar` bağlantısı | **Kaynak kodları henüz yerel proje klasöründe doğrulanmış değil.** İndirilip içeriği incelenmeli. |
| Veritabanı yedeği | Sohbete yüklenen `store5.bak` | Dosya SQL Server backup dosyası olarak tanınmıştır; boyutu 356,638,208 bayttır. Yedeğin header bilgileri, tam SQL Server sürümü, veritabanı adı, restore edilebilirliği ve şeması SQL Server üzerinde ayrıca doğrulanmalıdır. |
| İlave dokümanlar | İş tanımında daha sonra paylaşılacağı belirtiliyor | DealerSettings dokümantasyonu ve diğer teknik belgeler geldikçe incelemeye eklenmeli. |

**Önemli:** `store5.rar` kaynak arşivinin indirilmiş ve açılmış olduğu varsayılmamalıdır. Yalnızca `store5.bak` dosyasının varlığı, C# veya Vue kaynak kodlarının elde olduğu anlamına gelmez. Benzer şekilde ham `.bak` dosyasından tablo/kolon davranışları tam olarak doğrulanmış sayılmaz; doğrulama SQL Server restore ve sorgularıyla yapılmalıdır.

## 4. Kapsam

### 4.1 Dahil olan işler

1. Mevcut Teknik Ayarlar ekranındaki tüm kategori ve parametrelerin eksiksiz envanterinin çıkarılması.
2. Her parametrenin amacının ve bayi portalındaki etkisinin araştırılması.
3. Parametrenin aktif/pasif veya farklı değerlerde nasıl davrandığının test edilmesi.
4. Parametrelerin koşullarının ve birbirleriyle bağımlılıklarının bulunması.
5. Her parametrenin `DealerSettings` ve gerekli olduğunda ilişkili tablolardaki karşılığının bulunması.
6. Kolon veri tipleri, varsayılan değerler, nullable durumu ve tekil seçim / Bitwise ayrımının belgelenmesi.
7. Bitwise alanların bit/maske/değer eşleştirmelerinin çıkarılması.
8. Mevcut sistemde çalışmayan veya beklenenden farklı davranan ayarların raporlanması.
9. Vue.js + TypeScript tabanlı modern Teknik Ayarlar arayüzü geliştirilmesi.
10. C# .NET Core Web API üzerinden ayar okuma ve kaydetme işlemleri.
11. Microsoft SQL Server üzerinde mevcut veri karşılıklarının korunması.
12. Otomatik ve manuel testler, README, AI kullanım özeti ve kısa demo kaydı.
13. Uygulamanın ücretsiz bir web hizmeti üzerinde yayınlanabilir hale getirilmesi ve güvenli demo verisiyle erişilebilir demo sunulması.

### 4.2 Kapsam dışı / izin gerektiren işler

- Üretim B2B STORE veritabanında değişiklik yapmak.
- Üretim sistemindeki mevcut hataları düzeltmek. Bu görevde beklenen, hataları tespit edip raporlamaktır.
- B2B STORE'un tamamını yeniden yazmak.
- Kaynak kodu, parola, müşteri verisi, token, bağlantı dizesi veya gerçek veritabanı yedeğini herkese açık depoya ya da demo hosting'e koymak.
- Şema değişikliği, tablo/kolon silme, toplu veri güncelleme veya mevcut sistemi etkileyebilecek herhangi bir işlem için açık onay olmadan ilerlemek.

## 5. İncelenecek parametre kategorileri

İş tanımında sayılan kategoriler başlangıç listesidir. Mevcut ekranda bulunan başka tüm kategoriler de eklenmelidir.

- Tasarım
- Menü Gösterimi
- Ürün Gösterim Parametreleri
- Ürün Detay Parametreleri
- Arama
- Sepet
- Genel Ayarlar
- Ödeme
- Başvuru Formu
- Admin
- Ekranda yer alan diğer tüm gruplar

Kategori adları mevcut üründeki etiketlerle karşılaştırılmalı; kullanıcı deneyimi için daha iyi başlık önerilse bile orijinal ad, analiz eşleştirmesinde kaybolmamalıdır.

## 6. Parametre envanteri için zorunlu veri modeli

Her bir parametre için mümkün olan tüm aşağıdaki alanlar kaydedilmelidir. Bilinmeyenler tahmin edilmemeli, `Doğrulanacak` olarak işaretlenmelidir.

| Alan | Açıklama |
|---|---|
| `parameterId` | Uygulama içi kararlı kimlik. |
| `category` | Mevcut ekrandaki kategori. |
| `originalLabel` | Mevcut ekrandaki parametre adı. |
| `displayLabel` | Yeni arayüzde kullanılacak sade ve anlaşılır ad. |
| `description` | Parametrenin işlevini açıklayan yardım metni. |
| `controlType` | Switch, checkbox, dropdown, textbox, number, radio vb. |
| `settingKey` | Kaynak koddaki gerçek property/alan adı; doğrulanmadan üretilmemeli. |
| `tableName` | İlgili veritabanı tablosu. |
| `columnName` | Gerçek DB kolonu. |
| `sqlDataType` | SQL Server veri tipi ve uzunluğu. |
| `nullable` / `defaultValue` | Null ve varsayılan bilgileri. |
| `valueType` | Boolean, single-select, string, integer, decimal, bitmask vb. |
| `allowedValues` | Enum/dropdown seçenekleri ve doğrulanmış değerleri. |
| `bitMask` | Bitwise flag değeri; sadece kaynak veya test ile kanıtlandıysa. |
| `conditions` | Hangi koşullarda devreye girdiği. |
| `dependencies` | Etkilediği veya çalışmak için ihtiyaç duyduğu diğer ayarlar. |
| `affectedPortalArea` | Bayi portalındaki etkilenen ekran/işlev. |
| `expectedBehavior` | Beklenen davranış. |
| `testStatus` | Başarılı / Hatalı / Test Edilemedi / Doğrulanacak. |
| `observedBehavior` | Testte gerçekten görülen davranış. |
| `evidence` | Kaynak kod, SQL nesnesi, doküman, test kaydı veya ekran referansı. |
| `confidence` | Doğrulanmış / Güçlü kanıt / Varsayım / Bilinmiyor. |

Parametre tanımları tek bir merkezi katalogda tutulmalı; frontend, backend doğrulaması ve teknik analiz raporu mümkün olduğunca aynı tanım kaynağından beslenmelidir. Ancak mevcut sistemin veri modelini keşfetmeden yeni bir katalog formatı zorla uygulanmamalıdır.

## 7. Fonksiyonel gereksinimler

### FR-01 – Tüm ayarları görüntüleme

- API, yetkili kullanıcının kapsamındaki ayarları okuyabilmelidir.
- Frontend ayarları kategorilere göre göstermelidir.
- Parametrelerin mevcut değerleri, veritabanı ve mevcut sistemle tutarlı olmalıdır.
- Veri yüklenirken loading durumu; hata olduğunda anlaşılır hata mesajı gösterilmelidir.

### FR-02 – Kategori ve arama

- Ayarlar anlamlı kategoriler altında gruplanmalıdır.
- Parametre adı ve açıklamasında arama yapılabilmelidir.
- Kullanıcı, mümkünse kategoriye veya ayar türüne göre filtreleyebilmelidir.
- Arama/filtreleme ayarın gerçek değerini değiştirmemelidir.

### FR-03 – Doğru kontrol bileşeni

- Boolean ayarlar switch/checkbox ile gösterilmelidir.
- Birden fazla sabit seçenekten yalnızca biri seçiliyorsa dropdown/radio kullanılmalıdır.
- Sayısal ve metinsel değerler uygun input türleriyle gösterilmelidir.
- Bitwise maskenin ham integer değeri son kullanıcıya doğrudan düzenlenebilir bir sayı alanı olarak gösterilmemelidir; doğrulanmış seçenekler anlaşılır kontrollerle sunulmalıdır.
- Kontrol seçimi gerçek veri tipine ve mevcut ekran davranışına göre yapılmalıdır.

### FR-04 – Açıklama ve yönlendirme

- Her parametrede kısa açıklama/yardım metni bulunmalıdır.
- Bağımlı ayar varsa kullanıcıya bunun etkisi belirtilmelidir.
- Başka bir ayar aktif değilken kullanılamayan parametreler, gerçek davranış doğrulandıktan sonra uygun şekilde devre dışı bırakılmalı veya uyarılmalıdır.
- UI, doğrulanmamış bağımlılıkları varmış gibi göstermemelidir.

### FR-05 – Değişiklik yönetimi

- Kullanıcı değişiklik yaptığında kaydedilmemiş durum görünür olmalıdır.
- Kaydetme sırasında tekrar gönderim veya çift tıklama kaynaklı tutarsızlık engellenmelidir.
- Başarı ve hata bildirimleri açık olmalıdır.
- Kaydetme sonrası backend'den okunan kalıcı değer UI ile senkronize edilmelidir.
- Sayfa terk edilirken kaydedilmemiş değişiklik varsa kullanıcı uyarılmalıdır.
- Tek tek kaydetme veya toplu kaydetme kararı mevcut sistem incelemesinden sonra belirlenmeli; seçilen yaklaşım dokümante edilmelidir.

### FR-06 – Doğrulama

- İzin verilen değerler backend'de de doğrulanmalıdır.
- Frontend validasyonu tek başına yeterli değildir.
- Yanlış tip, geçersiz enum, izin verilmeyen bit maskesi ve kapsam dışı ayar anahtarı reddedilmelidir.
- Hata yanıtları kullanıcıya güvenli ve anlaşılır şekilde sunulmalıdır.

### FR-07 – Parametre ve portal davranışı

- Her parametrenin etkilediği bayi portalı ekranı/işlevi belirlenmelidir.
- Ayar değişikliği sonrasında beklenen portal davranışı kontrollü test ortamında doğrulanmalıdır.
- Mevcut platformdaki kusur düzeltilmeden raporlanmalıdır; yeni modülde mevcut beklenen davranışın aynısı hedeflenmelidir.

## 8. Backend gereksinimleri – C# .NET Core Web API

1. REST API, mevcut uygulamanın mimarisi ve kullanılan .NET sürümü incelendikten sonra uyumlu sürümle geliştirilmelidir.
2. Kod katmanları okunabilir ve sürdürülebilir olmalıdır. Mevcut projede kullanılan repository/service/EF/Dapper gibi desenler önce incelenmeli; sebepsiz teknoloji değişikliği yapılmamalıdır.
3. Ayarlar API üzerinden okunmalı ve kaydedilmelidir.
4. DTO'lar kullanılmalı; SQL entity modelleri doğrudan istemciye açılmamalıdır.
5. İstekler doğrulanmalı, hata durumları standart ve güvenli HTTP yanıtlarıyla dönmelidir.
6. Veritabanı sorguları parametreli olmalı; string birleştirme ile SQL oluşturulmamalıdır.
7. Çok müşterili/bayili veri kapsamı varsa `MemberCode` veya eşdeğer müşteri kimliği mevcut uygulamanın güvenli kimlik bağlamından belirlenmelidir. İstemcinin gönderdiği müşteri koduna tek başına güvenilmemelidir.
8. Yetkilendirme olmadan ayar yazma endpoint'i herkese açık bırakılmamalıdır.
9. Güncelleme, ilişkili değerler için gerekiyorsa transaction içinde yürütülmelidir.
10. Eşzamanlı değişikliklerin üzerine sessizce yazılmasını önlemek için mevcut şema destekliyorsa concurrency kontrolü uygulanmalıdır; desteklemiyorsa risk belgelenmelidir.
11. API, gerçek müşteri/tenant sınırlarını korumalıdır.
12. Sağlık kontrolü endpoint'i eklenmesi deployment için değerlendirilmelidir.

### 8.1 Önerilen API sözleşmesi (kaynak kod incelemesinden sonra kesinleştirilecek)

Aşağıdakiler başlangıç önerisidir; mevcut API isimleri ve sözleşmeleri incelenmeden birebir uygulanması zorunlu değildir.

- `GET /api/settings` – Yetkili kapsam için güncel ayarları getirir.
- `GET /api/settings/catalog` – Parametre açıklamaları, kategoriler ve izin verilen seçenekleri getirir.
- `PUT /api/settings` – Doğrulanmış ayar değişikliklerini kaydeder.
- `GET /health` – Uygulama ve gerekli bağımlılıklar için sağlık bilgisi verir; hassas ayrıntı döndürmez.

Örnek bir güncelleme gövdesi tasarlanırsa yalnızca izin verilen anahtar/değer DTO'ları kabul edilmeli; istemciden ham SQL, tablo adı, kolon adı veya sınırsız bitmask değeri alınmamalıdır.

## 9. Veritabanı gereksinimleri – Microsoft SQL Server

1. Mevcut `DealerSettings` tablosu ve görevde adı geçen ilişkili yapılar esas alınmalıdır.
2. `DealerSettings` ile `DealerSystemSettings` isimleri görev ve yüklenen yedek incelemesinde araştırılması gereken tablo adlarıdır. Her ikisinin varlığı, rolü ve ilişkisi SQL Server katalog sorgularıyla doğrulanmalıdır.
3. Her parametre gerçek tablo/kolon, tip, nullable, varsayılan değer ve constraint ile eşleştirilmelidir.
4. Tek değerli alanlar ile birden çok özelliği aynı integer alanda tutan Bitwise alanlar birbirinden ayrılmalıdır.
5. Mevcut kolon isimleri ve anlamları korunmalıdır.
6. Şemada değişiklik zorunlu değilse migration/DDL uygulanmamalıdır.
7. Önce yedek, restore edilmiş ayrı inceleme veritabanı ve test verisi kullanılmalıdır.
8. Üretim veritabanına doğrudan bağlanıp deneysel güncelleme yapılmamalıdır.
9. Kaydetme işlemleri veri kaybına neden olmamalı; yalnızca değiştirilen parametre etkilenmelidir.
10. SQL Server sürümü, compatibility level, collation ve restore gereksinimleri gerçek ortamdan öğrenilmelidir.

### 9.1 Bitwise uygulama kuralları

Bitwise alanların tam maskeleri ve anlamları ancak mevcut kaynak kod, teknik doküman veya kontrollü testle doğrulandıktan sonra katalogda tanımlanmalıdır. İsimden veya sayısal değerden anlam çıkarılarak flag uydurulmamalıdır.

Boolean flag için genel işlem mantığı:

```csharp
// Flag'i etkinleştir: diğer bitleri korur.
currentValue |= flagMask;

// Flag'i devre dışı bırak: diğer bitleri korur.
currentValue &= ~flagMask;

// Flag etkin mi?
bool isEnabled = (currentValue & flagMask) == flagMask;
```

Bir alanda tek tek boolean flag değil de çok değerli bir alt alan saklanıyorsa, alt alanın maskesiyle seçilmiş değer kullanılmalıdır:

```csharp
// Sadece ilgili bit grubunu değiştirir, diğer bitleri korur.
currentValue = (currentValue & ~groupMask) | selectedValue;
```

Zorunlu kontroller:

- `flagMask` ve `groupMask` kaynaklardan doğrulanmalı.
- Bir flag değiştirilirken aynı kolondaki diğer bitler korunmalı.
- Seçilen değer ilgili grubun maskesi dışında bit içermemeli.
- Bilinmeyen/ileride eklenmiş bitler, açıkça tasarlanmış bir migration/temizleme gereksinimi yoksa korunmalı.
- İşlemden sonra güncel değer veritabanından tekrar okunup doğrulanmalı.
- Bitwise mantık için unit test yazılmalı: açma, kapama, aynı flag'i tekrar uygulama, başka flag'leri koruma, bilinmeyen bitleri koruma ve invalid value senaryoları.
- SQL tarafında da bitwise kontrol varsa C# ve SQL davranışları karşılaştırılmalı.

## 10. Frontend gereksinimleri – Vue.js + TypeScript

1. Kaynak arşivde Vue sürümü, build sistemi, router ve mevcut UI kütüphanesi varsa önce bunlar incelenmelidir.
2. Yeni arayüz responsive olmalıdır.
3. Kategoriler kolay gezilebilir olmalıdır.
4. Arama ve filtreleme bulunmalıdır.
5. Her ayar için anlaşılır etiket ve açıklama sunulmalıdır.
6. Bileşen türü parametrenin gerçek veri tipine göre seçilmelidir.
7. Kaydedilmemiş değişiklik, loading, success ve error durumları görünür olmalıdır.
8. Form kontrollerinde klavye erişilebilirliği, etiket ilişkileri ve yeterli kontrast gözetilmelidir.
9. API hataları kullanıcıya gösterilmeli ancak stack trace, connection string veya hassas backend ayrıntısı gösterilmemelidir.
10. TypeScript'te `any` kullanımı gerekmedikçe önlenmeli; API DTO tipleri tanımlanmalıdır.
11. Tekrarlanan parametre kartları/form alanları reusable component olarak tasarlanmalıdır.
12. Parametre katalog verisi ile kullanıcı değerleri ayrıştırılmalıdır.
13. UI içinde bitwise hesaplamaları kopyalanıp çoğaltılmamalı; maskelerin tek otoritesi backend/domain katmanı olmalıdır. UI yalnızca doğrulanmış flag seçeneklerini gösterir.

## 11. Test gereksinimleri ve hata raporu

### 11.1 Parametre bazlı test kaydı

Her testte şu alanlar olmalıdır:

- Parametre adı ve anahtarı
- Kategori
- Test edilen portal ekranı/özelliği
- Başlangıç değeri
- Uygulanan yeni değer
- Beklenen davranış
- Gerçekleşen davranış
- Sonuç: Başarılı / Hatalı / Test Edilemedi
- Hata açıklaması
- Kanıt (ekran görüntüsü, kod referansı veya test kaydı)
- Test tarihi ve test ortamı

### 11.2 Otomatik test seviyeleri

- **Unit test:** Bitwise yardımcıları, enum/value doğrulaması, parametre katalog eşlemesi.
- **Backend integration test:** API + test veritabanı okuma/kaydetme, geçersiz istekler, tenant kapsamı.
- **Frontend test:** Parametrelerin doğru input türüyle görünmesi, arama/filtreleme, değişiklik takibi, hata ve başarı durumları.
- **End-to-end / manuel regresyon:** Admin tarafında ayarı değiştir; bayi portalında beklenen etkisini kontrol et.

### 11.3 Zorunlu regresyon senaryoları

- Bir ayar değiştirilip kaydedildikten sonra doğru değer geri okunuyor mu?
- Tekil seçimli parametrede tüm geçerli seçenekler doğru saklanıyor mu?
- Bitwise bir flag açıldığında diğer flag'ler aynı kalıyor mu?
- Bitwise bir flag kapatıldığında diğer flag'ler aynı kalıyor mu?
- Geçersiz değer API tarafından reddediliyor mu?
- Başka müşteri/bayi kapsamındaki ayar yanlışlıkla okunabiliyor veya değiştirilebiliyor mu?
- Kaydetme hatasında UI yanlışlıkla başarı göstermiyor mu?
- Sayfa yenilenince kaydedilmiş değerler korunuyor mu?

## 12. Teknik analiz raporunun yapısı

`docs/technical-analysis.md` dosyasında en az şu bölümler bulunmalıdır:

1. Mevcut sistem özeti
2. İncelenen kaynaklar ve sürüm bilgileri
3. Teknik Ayarlar kategori listesi
4. Parametre envanteri
5. Parametre → DB tablo/kolon eşleştirmesi
6. Kolon veri tipleri ve varsayılanlar
7. Tekil seçimli alanlar
8. Bitwise alanlar, maskeler ve değer eşleştirmeleri
9. Parametre bağımlılıkları ve koşulları
10. Bayi portalındaki etkiler
11. Belirsiz veya doğrulanamayan alanlar
12. Tasarım/uygulama kararları ve gerekçeleri

## 13. Test ve hata raporunun yapısı

`docs/test-and-defect-report.md` dosyasında parametre bazlı test matrisi, test kanıtları, hatalar, test edilemeyen senaryolar ve bilinen sınırlamalar bulunmalıdır. Test edilmemiş bir parametre başarılı olarak raporlanmamalıdır.

## 14. AI kullanım özeti

`docs/ai-usage-report.md` dosyasında:

- Kullanılan yapay zekâ araçları ve sürümleri (biliniyorsa)
- Hangi aşamalarda yararlanıldığı
- Yapay zekâ önerilerinin nasıl doğrulandığı
- İnsan tarafından kontrol edilen kod ve kararlar
- Yapay zekânın hatalı/eksik önerileri ve bunların nasıl düzeltildiği
- Hassas veri paylaşımını önlemek için alınan önlemler

belgelenmelidir. Yapay zekâ tarafından önerilen kod, test edilmeden doğru kabul edilmemelidir.

## 15. Yayınlama ve demo gereksinimleri

- Uygulama, görevde istendiği gibi ücretsiz kullanım seçeneği sunan web hizmetlerinde yayınlanabilir olmalıdır.
- Hosting, SQL hizmeti, ücretsiz katman limitleri, uyku/soğuk başlatma, disk kalıcılığı, süre/istek limitleri ve bölge uygunluğu yayınlama tarihinde kontrol edilmelidir.
- Frontend ve backend ayrı servisler olarak yayınlanabilir; seçilen mimari README'de açıklanmalıdır.
- Gerçek `store5.bak`, gerçek müşteri kayıtları, parola, token ve connection string herkese açık hosting'e veya repoya yüklenmemelidir.
- Demo için anonimleştirilmiş/sentetik veri kullanılmalıdır.
- Public demo gerçek sistemin admin kimlik doğrulamasını taklit etmemeli ve gerçek müşteri verisine erişmemelidir.
- Yönetim işlemleri public erişimdeyse uygun kimlik doğrulama ve yetkilendirme zorunludur. Güvenli kimlik doğrulama kurulamayacaksa demo salt okunur veya sentetik verili kontrollü demo olmalıdır.
- Secrets yalnızca hosting'in environment/secrets alanlarında tutulmalıdır.
- HTTPS, doğru CORS origin listesi, loglarda hassas veri maskeleme ve hata mesajlarında bilgi sızıntısını engelleme uygulanmalıdır.
- Yayın öncesi güvenlik, smoke test ve veri doğruluğu kontrolü yapılmalıdır.
- Deployment ve public URL, kullanıcı onayı olmadan gerçekleştirilmemelidir.

## 16. Teslimatlar

| Kod | Teslimat | İçerik |
|---|---|---|
| A | Teknik Analiz Raporu | Parametreler, açıklamalar, portal etkileri, DB eşleştirmeleri, Bitwise ve bağımlılıklar. |
| B | Test ve Hata Raporu | Beklenen/gerçekleşen davranış, sonuçlar, hata açıklamaları ve kanıtlar. |
| C | Çalışan Yazılım | Vue + TypeScript frontend, C# .NET Core API, SQL Server bağlantısı, okuma/kaydetme, README, kısa ekran kaydı. |
| D | AI Kullanım Özeti | Araçlar, kullanım alanları, kontrol/doğrulama yöntemi ve öğrenilenler. |

Önerilen dosya düzeni (mevcut repo incelendikten sonra uyarlanabilir):

```text
project-root/
├── AGENTS.md
├── PROJECT_REQUIREMENTS.md
├── README.md
├── docs/
│   ├── technical-analysis.md
│   ├── parameter-catalog.md
│   ├── test-and-defect-report.md
│   ├── ai-usage-report.md
│   └── deployment.md
├── frontend/                 # Vue + TypeScript (mevcut yapıya göre)
├── backend/                  # ASP.NET Core Web API (mevcut yapıya göre)
└── tests/                    # Test projeleri (mevcut yapıya göre)
```

Bu yalnızca önerilen düzen; kaynak arşivde mevcut bir çözüm yapısı varsa önce o yapı anlaşılmalı ve sebepsiz yere taşınmamalıdır.

## 17. Kabul kriterleri (Definition of Done)

Proje aşağıdaki maddelerin tümü karşılanmadan tamamlandı sayılmamalıdır:

- [ ] Teknik Ayarlar ekranındaki bütün kategoriler ve parametreler envantere alındı.
- [ ] Her parametrenin amacı, kontrol türü, veri tipi ve gerçek DB eşleştirmesi kanıtlandı veya neden doğrulanamadığı açıklandı.
- [ ] Bitwise alanların maskeleri ve alt alanları kaynak/test kanıtıyla belgelendi.
- [ ] Bir bit değiştiğinde diğer bitlerin bozulmadığını gösteren otomatik testler var.
- [ ] Parametrelerin bayi portalındaki etkileri test edildi; test edilemeyenler açıkça işaretlendi.
- [ ] Mevcut platformda tespit edilen hatalar düzeltme yapılmadan raporlandı.
- [ ] Frontend responsive, aranabilir, kategorili ve açıklamalı.
- [ ] Frontend API üzerinden ayar okuyup kaydedebiliyor.
- [ ] Backend validasyon, hata yönetimi ve güvenli erişim kontrollerini uyguluyor.
- [ ] SQL Server bağlantısı ve kayıt işlemleri ayrı test veritabanında doğrulandı.
- [ ] Var olan şemanın anlamı korunuyor; gerekçesiz DDL/migration yok.
- [ ] Public demo gerçek müşteri verisi veya gizli bilgi içermiyor.
- [ ] README kurulum, konfigürasyon, test ve çalıştırma talimatlarını içeriyor.
- [ ] Ekran kaydı uygulamanın çalışmasını gösteriyor.
- [ ] AI kullanım özeti hazır.
- [ ] Teknik görüşmede mimari, veri eşleştirmesi, Bitwise ve test kararları açıklanabilir durumda.

## 18. Açık konular ve keşif sırasında cevaplanacak sorular

Bunlar Codex'in kaynakları inceleyerek cevaplaması gereken keşif maddeleridir; cevapları tahmin etmemelidir.

1. `store5.rar` içindeki gerçek kaynak kodu projeleri ve başlangıç noktaları nelerdir?
2. Mevcut frontend ve backend sürümleri/teknolojileri nelerdir? Görevde istenen teknolojiyle ne kadar uyumludur?
3. `DealerSettings` ve `DealerSystemSettings` tablolarının gerçek şeması ve aralarındaki ilişki nedir?
4. Her Teknik Ayarlar kontrolünün gerçek property/column eşleştirmesi nedir?
5. Bitwise kolonlar ve her flag/enum değerinin anlamı nedir?
6. Birden fazla `MemberCode` veya varsayılan kayıt eşleştiğinde mevcut sistem hangi kaydı seçer?
7. Mevcut API, servis, stored procedure ve SQL fonksiyonlarının hangileri ayarları kullanır?
8. Teknik Ayarlar değişikliği bayi portalına hangi cache/yenileme mekanizmasıyla yansır?
9. Bazı ayarların çalışması için hangi modül, lisans veya başka ayar gerekir?
10. Test ortamında hangi kullanıcı rolleri ve test verileri kullanılabilir?
11. Yayın için ücretsiz servislerin mevcut limitleri ve SQL Server uyumluluğu nelerdir?
12. Demo uygulamasında authentication nasıl sağlanacak ve demo hangi sentetik veri setini kullanacak?

## 19. İlk uygulanacak iş: keşif raporu

Kod değişikliğine başlamadan önce Codex şu çıktıları üretmelidir:

- `docs/repository-inventory.md` – repo, projeler, framework sürümleri ve başlangıç komutları.
- `docs/database-inventory.md` – yedek, restore durumu, tablolar, kolonlar, ilişkiler ve doğrulama sorguları.
- `docs/technical-analysis.md` – ilk parametre ve kategori envanteri; kanıt ve bilinmeyenler ayrı gösterilir.
- `docs/risks-and-decisions.md` – teknik riskler, açık kararlar ve öneriler.

Bu keşif raporu gözden geçirilmeden büyük çaplı frontend/backend refactor veya deployment yapılmamalıdır.

---

## 20. Kanıt ve belirsizlik yazım standardı

Her kritik teknik iddia aşağıdaki statülerden biriyle işaretlenmelidir:

- **Doğrulandı:** Çalışan uygulama, SQL Server sorgusu, kaynak kod veya test ile kanıtlandı.
- **Kaynakta bulundu:** Kod/dokümanda mevcut; runtime davranışı henüz test edilmedi.
- **Güçlü çıkarım:** Birden fazla bulgu destekliyor ancak doğrudan test yok.
- **Varsayım:** Henüz kanıt yok; uygulama gereksinimi olarak kabul edilmemeli.
- **Doğrulanamadı:** Kaynak veya erişim eksik.

Raporlar, `Varsayım` ve `Doğrulanamadı` maddelerini doğrulanmış gereksinimlerle aynı seviyede sunmamalıdır.
