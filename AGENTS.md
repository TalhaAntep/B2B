# AGENTS.md – B2B STORE Teknik Ayarlar Modülü

Bu dosya, Codex ve diğer kod ajanları için depo kökünde geçerli çalışma talimatlarıdır. Önce `PROJECT_REQUIREMENTS.md` dosyasını tamamen oku; bu dosyadaki kurallar ile proje gereksinimleri birlikte uygulanır.

## 1. Rolün ve ana hedefin

Mevcut B2B STORE Teknik Ayarlar (DealerSystemSetting) modülünü analiz etmeye ve mevcut davranışları koruyarak Vue.js + TypeScript, C# .NET Core Web API ve Microsoft SQL Server ile yeniden geliştirmeye yardımcı olan kıdemli yazılım mühendisi gibi çalış.

Görev tüm B2B platformunu yeniden yazmak değildir. Önce mevcut sistemi anlamak, parametreleri kanıtla eşleştirmek ve sonra yalnızca gerekli modülü geliştirmektir.

## 2. İlk oturumda yapılacaklar – kod değiştirmeden keşif

İlk görevde hiçbir dosyayı değiştirme. Önce:

1. Repo kökünü ve tüm alt proje klasörlerini listele.
2. `git status`, aktif branch ve mevcut değişiklikleri kontrol et; kullanıcı değişikliklerini asla ezme.
3. `*.sln`, `*.csproj`, `package.json`, lock dosyaları, `vite.config.*`, `vue.config.*`, router, config ve test dosyalarını bul.
4. .NET, Vue, TypeScript, Node ve SQL veri erişim sürümlerini kaynaklardan tespit et.
5. `DealerSystemSetting`, `DealerSettings`, `DealerSystemSettings`, `ProductTableStockType` ve `Parameter*` benzeri isimleri ara.
6. Controller, service, repository, model/entity, DTO, stored procedure, view, trigger ve SQL fonksiyonlarını ilişkilendir.
7. Veritabanı yedeği dosyalarını ve teknik dokümanları bul; `.bak` dosyasını değiştirme veya repoya ekleme.
8. Bulguları `docs/repository-inventory.md`, `docs/database-inventory.md`, `docs/technical-analysis.md` ve `docs/risks-and-decisions.md` dosyalarına yaz.
9. Her bulguyu `Doğrulandı`, `Kaynakta bulundu`, `Güçlü çıkarım`, `Varsayım` veya `Doğrulanamadı` olarak etiketle.
10. Sonuçları özetle ve sonraki aşama için önerilen planı sun. Kullanıcı onayı olmadan büyük refactor veya deployment başlatma.

Kaynak arşiv veya gerekli dokümanlar repo içinde yoksa varmış gibi davranma. Eksik kaynağı açıkça raporla; koddan çıkarılamayan parametre anlamları uydurma.

## 3. Kanıt ve doğruluk kuralları

- `PROJECT_REQUIREMENTS.md` iş kapsamı için ana referanstır.
- Mevcut kaynak kodu ve SQL Server şeması teknik gerçeklik için ana kanıttır.
- Çalışan uygulamadaki davranış kontrollü testle doğrulanır.
- Parametre adından, kolon adından veya sayısal değerinden tek başına işlev çıkarma.
- Kaynak kodda bulunması, davranışın runtime'da çalıştığını kanıtlamaz.
- Test edilmemiş davranışı `Başarılı` olarak raporlama.
- Çelişen kaynaklar varsa farkı saklama; her iki bulguyu ve belirsizliği yaz.
- Eksik bilgi için sessizce genel bilgiyle boşluk doldurma.

## 4. Veri güvenliği ve izinler

Aşağıdaki kurallara istisnasız uy:

- Üretim ortamı veya gerçek müşteri veritabanında deneysel güncelleme yapma.
- Yalnızca restore edilmiş ayrı analiz/test veritabanı ve açıkça izin verilen test ortamında çalış.
- `store5.bak` dosyasını silme, değiştirme, yeniden adlandırma veya Git'e ekleme.
- Veritabanı restore ederken mevcut DB üzerine yazma; `WITH REPLACE`, `DROP DATABASE`, toplu silme veya benzeri yıkıcı komutları açık kullanıcı onayı olmadan çalıştırma.
- Kullanıcının mevcut dosya ve çalışma ağacı değişikliklerini koru.
- Parola, token, cookie, connection string, kişisel veri veya müşteri verisini kaynak koduna, loglara, test fixture'larına, screenshot'a, Markdown dosyasına veya Git'e yazma.
- Gerçek admin giriş bilgilerini hiçbir dosyaya kopyalama. Gerekirse yerel secret store veya environment variable kullan.
- `appsettings.*` içinde gerçek secret saklama; yalnızca güvenli örnek placeholder içeren `appsettings.example.json` üret.
- Public demo/hosting'e gerçek veritabanı yedeği veya gerçek müşteri verisi yükleme. Yalnızca sentetik veya anonimleştirilmiş veri kullan.
- Public demo için güvenli authentication/authorization kurulamıyorsa yazma işlemlerini kapat veya demo modunu salt okunur/sentetik veriyle sınırla.
- Dış ortama deploy etme, repo push etme, PR açma veya ücret doğurabilecek kaynak oluşturma işlemlerini kullanıcı onayı olmadan yapma.

## 5. Mimari ve kodlama ilkeleri

- Önce mevcut solution mimarisini ve kod standartlarını öğren.
- Mevcut teknoloji ve sürümlerle uyumlu kal; gerekçesiz büyük yükseltme veya teknoloji değişikliği yapma.
- Katmanlar mevcut yapıya uygun olmalı. Controller/API, application/service, domain/model ve data erişim sorumluluklarını karıştırma.
- SQL parametreli olmalı; kullanıcı girdisini SQL string'ine birleştirme.
- Entity'leri doğrudan API response olarak döndürme; DTO kullan.
- TypeScript'te net tipler ve API contract kullan; gereksiz `any` kullanma.
- Tekrarlanan UI ve domain kurallarını merkezi hale getir.
- İsimlendirme mevcut repodaki konvansiyonu izlesin.
- Gereksiz dependency ekleme. Dependency eklemeden önce neden gerektiğini açıkla.
- Büyük değişiklikleri küçük, gözden geçirilebilir adımlara böl.
- Her anlamlı değişiklik sonrasında uygun test/lint/build komutunu çalıştır; çalıştıramadıysan sebebini yaz.

## 6. Parametre katalog ilkesi

- Her parametre merkezi bir katalog/metadata yapısında tanımlanmalıdır.
- Katalogda orijinal ekran etiketi, açıklama, kategori, gerçek DB eşleştirmesi, veri tipi, control type, seçenekler, bağımlılıklar, portal etkisi ve kanıt durumu bulunmalıdır.
- Kaynakla doğrulanmayan kolon, enum, varsayılan, mask veya flag değeri uydurma.
- Frontend kontrolü parametrenin gerçek veri tipine göre seçilmeli.
- Boolean, tek seçimli enum, metinsel/sayısal değer ve Bitwise alanı birbirine karıştırma.
- UI'da ham Bitwise integer düzenletme; doğrulanmış seçenekleri göster.
- Orijinal parametre adlarını analizde koru; kullanıcıya dönük yeni etiketi ayrı tut.

## 7. Bitwise için zorunlu kurallar

- Maskeleri yalnızca kaynak kod, teknik doküman veya kontrollü testten doğrula.
- Boolean flag açarken `current | flagMask` mantığını kullan.
- Boolean flag kapatırken `current & ~flagMask` mantığını kullan.
- Çok değerli alt alanı değiştirirken `(current & ~groupMask) | selectedValue` yaklaşımını kullan; gerçek maskeleri doğrula.
- Değişiklik sırasında aynı kolondaki diğer bitler ve bilinmeyen bitler korunmalıdır.
- İlgili bit grubunun dışında kalan değerleri kabul etme.
- Aynı ayarı ikinci kez açma/kapama idempotent davranmalıdır.
- Her Bitwise alan için açma, kapama, komşu flag'i koruma, bilinmeyen bitleri koruma ve geçersiz değer testleri yaz.
- Frontend'de Bitwise hesaplama mantığını çoğaltma; backend/domain katmanında merkezi uygulama kullan.

## 8. API ve tenant güvenliği

- Ayar okuma ve yazma işlemleri kimlik doğrulama ve yetkilendirme sınırında olmalıdır.
- Tenant/member/customer kapsamını istemcinin serbestçe gönderdiği değere göre belirleme. Mevcut kimlik ve yetki modelinden türet.
- İstemciden gelen setting key listesi allowlist/catalog ile doğrulanmalıdır.
- İstemciden tablo adı, kolon adı, SQL ifadesi veya ham bağlantı bilgisi kabul etme.
- Geçersiz değerler backend'de reddedilmelidir.
- Gerekliyse transaction ve concurrency kontrolü kullan.
- API hata yanıtları hassas iç ayrıntı sızdırmamalıdır.

## 9. Veritabanı güvenliği

- Mevcut `DealerSettings` yapısının anlamını ve kolonlarını koru.
- Şema değişikliği zorunlu değilse DDL/migration ekleme.
- Önce tablo, kolon, tip, nullability, default, key, index, FK ve constraint envanteri çıkar.
- Geliştirme/test için restore edilmiş ayrı DB kullan.
- Kaydetme sırasında sadece değişen alanı veya doğrulanmış ayar alanlarını güncelle; diğer alanların üzerine varsayılan değer yazma.
- Tek bir Bitwise flag güncellenirken bütün integer kolonu yeni flag değeriyle ezme.
- Üretim veri tabanı üzerinde test etme.

## 10. Test stratejisi

Her yeni işte uygun testler ekle ve çalıştır:

- Unit test: domain/Bitwise/validation/mapper.
- Backend integration test: API + ayrı test DB.
- Frontend test: rendering, input type, filtering, dirty state, save/error.
- Regression test: ayar kaydetme ve tekrar okuma; diğer flag'lerin korunması.
- Manuel portal testi: beklenen/gerçekleşen davranış ve kanıt.

Test edemediğin senaryoya açıkça `Test Edilemedi` yaz. Test sonucu uydurma veya sadece derleme başarılı diye davranışı doğru ilan etme.

## 11. Dokümantasyon

Aşağıdaki belgeleri güncel tut:

- `PROJECT_REQUIREMENTS.md` – kapsam ve gereksinimler; değişiklik önerilerini önce not et.
- `README.md` – kurulum, konfigürasyon, çalıştırma, test, build ve deployment.
- `docs/repository-inventory.md` – proje keşfi.
- `docs/database-inventory.md` – DB şeması ve restore/test bilgileri.
- `docs/technical-analysis.md` – parametre açıklamaları ve eşleştirmeler.
- `docs/parameter-catalog.md` – makine tarafından da tüketilebilecek envanter.
- `docs/test-and-defect-report.md` – test matrisi ve hatalar.
- `docs/ai-usage-report.md` – AI araçları ve doğrulama.
- `docs/risks-and-decisions.md` – açık kararlar ve riskler.
- `docs/deployment.md` – public demo mimarisi, env var adları ve limitler; secret değerleri değil.

## 12. Önerilen çalışma aşamaları

1. **Keşif:** Repo, kaynak arşiv, SQL yedeği, şema, mevcut UI ve API analizi.
2. **Parametre envanteri:** Tüm kategori ve parametreleri kaynakla eşleştir.
3. **Davranış testi:** Mevcut admin ve bayi portalında kontrollü test matrisi.
4. **Tasarım:** API contract, domain modeli, parametre kataloğu ve UI akışı.
5. **Backend:** Güvenli okuma/kaydetme, validasyon, Bitwise servisleri ve testler.
6. **Frontend:** Kategori, arama, kontrol tipleri, yardım, dirty state ve API entegrasyonu.
7. **Regresyon:** Parametre/portal etkileri, tenant ayrımı, veri bütünlüğü.
8. **Dokümantasyon ve demo:** README, analiz/test/AI raporları, sentetik veri ve onaylı hosting.

Aşama kapıları: Keşif tamamlanmadan geniş kapsamlı kodlama; şema ve maskeler doğrulanmadan Bitwise implementation; güvenlik kontrolü yapılmadan public deployment yapma.

## 13. Codex'in ilk görevi için beklenen yanıt formatı

İlk keşiften sonra kullanıcıya şu başlıklarla rapor ver:

1. **Bulunan projeler ve teknoloji sürümleri**
2. **Önemli dosyalar ve uygulama başlangıç noktaları**
3. **DealerSettings / DealerSystemSettings bulguları**
4. **Bulunan parametre ve Bitwise izleri**
5. **Eksik kaynaklar ve erişim engelleri**
6. **Güvenlik ve veri riski**
7. **Henüz doğrulanmamış varsayımlar**
8. **Önerilen bir sonraki küçük adım**

İlk görevde kod değişikliği yapma; yalnızca keşif ve raporlama yap.
