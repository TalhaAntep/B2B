# MonsterASP: Herkese açık yazma modunu etkinleştirme

Bu seçenek, proje sahibinin isteğiyle hazırlanmış geçici değerlendirme modudur. Etkin olduğunda internetten siteye ulaşan herkes yönetici tokenı girmeden ayarları okuyabilir ve aynı SQL kaydına değişiklik yazabilir.

## MonsterASP'ta eklenecek değişken

`Website → Manage → Scripting → Environment variables`

| Name | Value |
|---|---|
| `B2BSTORE_PUBLIC_WRITE_ENABLED` | `true` |

Mevcut ayarlar korunur:

- `ASPNETCORE_ENVIRONMENT=Production`
- `B2BSTORE_SETTINGS_WRITE_MODE=HostedStore`
- `B2BSTORE_MEMBER_CODE=store5`
- `B2BSTORE_SQL_CONNECTION` mevcut `db82141` bağlantı dizesi
- `B2BSTORE_ADMIN_TOKEN` public modda kullanılmaz; güvenli moda dönmek için saklanabilir

## Yayın

Kaynak klasöründe PowerShell açıp:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File ".\scripts\publish-monsterasp.ps1"
```

Oluşan `artifacts\B2BStore-MonsterASP-Publish.zip` dosyasını MonsterASP `wwwroot` klasörüne yükle ve mevcut dosyaların üzerine çıkar. Sonra website'i restart et.

## Davranış

- Site ilk açıldığında capability endpoint'inden `HostedPublicWrite` kipini tespit eder.
- Yönetici token alanı gizlenir ve arayüz otomatik olarak SQL ayarlarını yükler.
- Save işlemleri anonim olarak `db82141` / `store5` hedefine yazılır.
- Sabit ayar allowlist'i, tip doğrulama, transaction/concurrency kontrolleri ve readback doğrulaması devam eder.
- Ziyaretçilere ortak veritabanının herkese açık olarak düzenlendiğini bildiren uyarı gösterilir.

## Riski ve geri alma

İnternetteki herkes ayarları değiştirebilir; örneğin site durumunu pasife çekebilir, menü/ödeme/ürün/diğer ayarları değiştirebilir veya uygulamaya özel asset'leri değiştirebilir. Bu nedenle yalnızca sentetik ve geçici değerlendirme verisiyle kullan. Gerçek müşteri/üretim verisi için kullanma.

Erişimi tekrar token ile korumak için `B2BSTORE_PUBLIC_WRITE_ENABLED` değişkenini sil veya `false` yap ve website'i restart et. Bu durumda `B2BSTORE_ADMIN_TOKEN` yeniden zorunlu olur.

Bu geçici kip, iş tanımındaki “yetkilendirme olmadan ayar yazma endpoint'i herkese açık bırakılmamalıdır” şartıyla çelişir; bu bir bilinçli proje sahibi tercihidir, güvenli production mimarisi olarak sunulmamalıdır.
