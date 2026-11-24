# B2B STORE — hızlı başlangıç

Bu depo, B2B STORE Teknik Ayarlar modülünün Vue 3 + TypeScript frontend, ASP.NET Core .NET 10 API ve SQL Server katmanını içerir.

## Önce oku

1. `README.md` — mevcut mimari, modlar ve dağıtımlar.
2. `docs/deployment.md` — MonsterASP, GitHub Pages ve ortam değişkenleri.
3. `docs/delivery-checklist.md` — istenen A–D teslimatları ve doğrulanmış sınırlamalar.
4. `docs/technical-analysis-report.md` ve `docs/test-and-defect-report.md` — analiz ve test sonuçları.

## Frontend

```powershell
Set-Location .\frontend
npm ci
npm test
npm run build
```

## Backend

.NET 10 SDK ile depo kökünde:

```powershell
dotnet restore .\B2BStore.Settings.slnx
dotnet build .\B2BStore.Settings.slnx --configuration Release
dotnet test .\B2BStore.Settings.slnx --configuration Release
```

SQL entegrasyon testlerini çalıştırmadan önce `docs/deployment.md` ve `docs/sql-validation-report.md` içindeki hedef veritabanı kısıtlarını okuyun. `Store5_Analysis` hiçbir zaman yazma hedefi değildir.

## MonsterASP publish

Windows üzerinde Node/npm ve .NET 10 SDK hazırken:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File ".\scripts\publish-monsterasp.ps1"
```

Çıktı: `artifacts\B2BStore-MonsterASP-Publish.zip`. Yayın ve ortam değişkenleri için `docs/deployment.md` belgesini takip edin.

## Public-write modu uyarısı

`B2BSTORE_PUBLIC_WRITE_ENABLED=true`, tüm ziyaretçilere ortak SQL ayarlarını değiştirme izni verir. Yalnızca sentetik değerlendirme verisiyle ve geçici olarak kullanılmalıdır. Bu seçenek assignment authentication gereksiniminden bilinçli bir sapmadır; gerçek müşteri verisi veya üretim için uygun değildir.
