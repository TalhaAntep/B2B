# B2B STORE — Teknik Ayarlar Modülü | Teslim Paketi

Bu depo, 2. Aşama Teknik Değerlendirme kapsamında istenen dört teslimatı içerir.

## Canlı bağlantılar

- Kaynak kod: https://github.com/TalhaAntep/B2B
- Sentetik, salt-okunur GitHub Pages arayüzü: https://talhaantep.github.io/B2B/
- MonsterASP full-stack değerlendirme uygulaması: http://b2b-store-settings.runasp.net/ (HTTP; HTTPS etkinliği doğrulanmadı)

MonsterASP smoke testinde SQL readiness `ready` döndürmüş, yönetici bağlantısı kurulmuş ve bir Save/readback işlemi başarıyla tamamlanmıştır. Bu, 98 kontrolün tamamının hosted test edildiği anlamına gelmez. Opt-in anonim public-write modunun canlı ortamda etkinliği ayrıca doğrulanmalıdır.

## A — Teknik Analiz Raporu

- `docs/technical-analysis-report.md`
- `docs/technical-analysis.md`
- `docs/parameter-catalog.md` / `.json` — 98 kontrol
- `docs/mapping-audit.md` / `.json`
- `docs/setting-editability-matrix.md`
- `docs/database-inventory.md`, `docs/sql-validation-report.md`
- `docs/legacy-persistence-reverse-engineering.md`, `docs/risks-and-decisions.md`

## B — Test ve Hata Raporu

- `docs/test-and-defect-report.md`
- `docs/test-bug-report.md`
- `docs/sql-validation-report.md`
- `docs/evidence/delivery/` — sentetik ekran görüntüleri ve kısa kayıt
- `docs/hosted-smoke-test.md` — MonsterASP deploy doğrulamaları

## C — Çalışan Yazılım

- `frontend/` — Vue 3 + TypeScript + Vite
- `backend/B2BStore.Settings.Api/` — ASP.NET Core .NET 10 API ve SQL Server veri katmanı
- `tests/` — backend testleri
- `scripts/` — katalog, kontrollü SQL doğrulama ve MonsterASP publish komutları
- `B2BStore.Settings.slnx`, `README.md`, `README_START_HERE.md`

## D — AI Kullanım Özeti

- `docs/ai-usage-report.md`
- `docs/technical-interview-notes.md`

## Sınırlamalar

1. Eski ASP.NET Web Forms save handler bulunamadığı için 98 kontrolün legacy save parity'si doğrulanmamıştır.
2. Beş setting extension key, beş image slot uygulama-özel storage kullanır; eski portal tarafından okundukları iddia edilmez.
3. Bayi portalı etkileri 98 kontrol için kontrollü before/after testiyle tamamlanmamıştır.
4. MonsterASP'ta bir hosted Save smoke test edildi; hosted 98-setting tam test matrisi tamamlanmadı.
5. Anonim public-write seçeneği etkinse tüm ziyaretçiler ortak ayarları değiştirebilir; bu, assignment authentication şartından bilinçli sapmadır ve yalnızca geçici sentetik test için uygundur.
6. MonsterASP HTTPS etkinliği doğrulanmadı. Gerçek müşteri verisi veya gerçek kimlik bilgileriyle kullanmayın.

Bu depoya `.bak`, `.rar`, `.env`, yerel settings dosyası, connection string, bearer token, parola veya gerçek müşteri verisi eklenmemelidir.
