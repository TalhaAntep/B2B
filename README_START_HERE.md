# Codex ile B2B STORE projesine başlama

Bu pakette üç dosya bulunur:

- `AGENTS.md`: Codex'in depo kökünde takip etmesi gereken çalışma kuralları.
- `PROJECT_REQUIREMENTS.md`: İş tanımı, fonksiyonel/teknik gereksinimler, teslimatlar ve kabul kriterleri.
- `README_START_HERE.md`: Bu paketin nasıl kullanılacağı.

## 1. Dosyaları projeye yerleştir

1. Bilgisayarında B2B STORE için kullanacağın proje klasörünü aç.
2. `AGENTS.md` ve `PROJECT_REQUIREMENTS.md` dosyalarını proje köküne kopyala.
3. `README_START_HERE.md` dosyasını da istersen kökte tutabilirsin.
4. Gerçek `.bak`, parola, token veya müşteri verisini Git deposuna ekleme.

Örnek:

```text
B2B-STORE-TECHNICAL-SETTINGS/
├── AGENTS.md
├── PROJECT_REQUIREMENTS.md
└── README_START_HERE.md
```

Kaynak kod arşivini açtığında mevcut proje klasörlerini bu yapının altına yerleştirmek yerine, mevcut repo köküne göre belgeleri konumlandır. Var olan `AGENTS.md` varsa üzerine yazma; iki dosyanın talimatlarını dikkatle birleştir.

## 2. Codex'e verilecek ilk talimat

Aşağıdaki prompt'u Codex'e gönder:

> Önce repo kökündeki `AGENTS.md` ve `PROJECT_REQUIREMENTS.md` dosyalarını tamamen oku. Bu aşamada hiçbir dosyayı değiştirme, yeni kod yazma, paket kurma, veritabanını güncelleme veya deployment yapma. Yalnızca mevcut repository'yi keşfet: klasör yapısını, Vue/TypeScript ve .NET projelerini, sürümleri, başlangıç komutlarını, testleri, DealerSystemSetting/DealerSettings/DealerSystemSettings referanslarını, SQL nesnelerini ve Bitwise kullanımlarını bul. Veritabanı yedeğine veya hassas ayarlara dokunma. Bulguları kanıt durumlarıyla ayırarak gereksinim dokümanındaki keşif çıktılarında raporla. Eksik kaynakları ve belirsizlikleri açıkça belirt. En sonda önerdiğin bir sonraki küçük adımı sun.

## 3. Önemli not

Şu an eldeki `store5.bak` SQL Server yedeğidir. Kaynak kodların bulunduğu `store5.rar` arşivinin ayrıca indirilip incelenmesi gerekir. `.bak` dosyasının varlığı, uygulama kaynak kodlarının da hazır olduğu anlamına gelmez.
