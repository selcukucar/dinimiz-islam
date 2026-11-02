# Dinimiz İslam

Windows için C# / .NET 10 WPF okuyucusu. Klavye ile kategori dolaşımı ve salt okunur makale okuma için yerel Windows denetimleri kullanır. Python, Qt veya WebView gerektirmez. Resmî bir dinimizislam.com uygulaması değildir.

## İndirme ve çalıştırma

Windows 10 2004 (19041) veya daha yeni, x64 gerekir. .NET çalışma zamanı paketin içindedir.

- **Taşınabilir:** `DinimizIslam-WPF-1.0.2-Tasinabilir-x64.zip` arşivini tamamen çıkarıp `DinimizIslam.exe` çalıştırın.
- **Kurulum:** `DinimizIslam-WPF-1.0.2-Kurulum-x64.exe` çalıştırın. Kurulum Türkçedir, yalnız geçerli kullanıcı içindir ve yönetici yetkisi istemez. Başlat menüsü kısayolu ve kaldırıcı oluşturur; masaüstü kısayolu isteğe bağlıdır.
- İki sürüm aynı yerel veri klasörünü kullanır: `%LOCALAPPDATA%\DinimizIslamOkuyucu`. Taşınabilir burada kurulum gerektirmeyen anlamındadır; ayarlar USB bellekte tutulmaz. Kaldırma yer imlerini ve önbelleği silmez.
- Yayın dosyalarının SHA-256 değerleri `SHA256SUMS.txt` içindedir. Paketler kod imzalı değildir.

## Klavye ile kullanım

Uygulama ana kategorilerde açılır. Oklarla seçin, Enter ile açın. Alt+Sol önceki kategori/listeye döner; seçim ve filtre korunur. Başlık süzme alanında Aşağı ok listeye geçer. Makalede oklar satırları, Ctrl+oklar kelimeleri dolaşır. Metin salt okunurdur ve pencereye göre sarılır. Tab / Shift+Tab sonraki/önceki denetime gider. Menü Tab sırasına girmez; Alt veya F10 ile açılır, oklar ve Enter/Boşluk ile kullanılır, Escape ile çıkılır.

| Tuş | İşlev |
| --- | --- |
| Ctrl+2 | Kategoriler |
| Ctrl+3 / Ctrl+F | Arama bölümü / sorgu alanı |
| Ctrl+4 | Makale okuyucu |
| Ctrl+5 | İmsakiye |
| Ctrl+B | Yer imleri |
| Ctrl+O | Makale adresi aç |
| Ctrl+Home / Ctrl+End | Metin başı / sonu |
| Ctrl+K / F6 | Makale bağlantıları / metinde bul |
| Ctrl+R / F5 | Yenile |
| Ctrl+P | Yazdır |
| Ctrl+Shift+S | PDF kaydet |
| Ctrl+Artı / Ctrl+Eksi / Ctrl+0 | Yazı boyutu |
| Ctrl+Virgül | Ayarlar |
| F1 / F2 / F4 | Yardım / kısayollar / son durum |
| Escape | İşlemi iptal et veya iletişim kutusunu kapat |

Arama site kategori ve makale **başlıklarını**, ayrıca daha önce açılmış makalelerin metinlerini kapsar. Enter veya Ara düğmesi aramayı başlatır; sonuçlar yüklenince listeye odaklanır. İlk aramada başlık dizini hazırlanması ağ hızına ve kategori sayısına bağlı olarak sürebilir; durum alanı ilerlemeyi gösterir, Escape iptal eder. Sonraki aramalar diskteki dizini kullanır. Başlık dizinini yenile düğmesi günceller. Sorgu dışarı gönderilmez; henüz açılmamış makalelerin tam metinleri aranmaz. Ağ engeli veya eksik tarama açıkça bildirilir.

İmsakiye kaynakta yayımlanan yıl ve vakitleri gösterir; hesap yapmaz. Tabloya bir kez Tab ile girip oklarla hücreleri okuyabilirsiniz. Yazdırma ve PDF için Windows yazıcıları kullanılır. PDF komutu Microsoft Print to PDF gerektirir; PDF/UA etiketleme doğrulanmamıştır.

## Mimari ve geliştirme

Türkiye Takvimi referansındaki .NET 10 + WPF + yerleşik Fluent yapısı izlenir. `Cekirdek`: HTTP, HTML ayrıştırma, SQLite ve arama; `Arayuz`: yerel WPF denetimleri; `Konsol`: kaynak tanı araçları; `Testler`: çevrimdışı xUnit testleri. Kaynak isteği iptal edilebilir, istekler seri ve aralıklıdır. robots.txt, HTTPS alan adı sınırı, yanıt boyutu ve hata denetimleri uygulanır. Yer imleri ve önceki Python sürümündeki SQLite kayıtları korunur.

.NET SDK 10.0.403 veya global.json uyumlu SDK ile:

```powershell
dotnet restore DinimizIslam.slnx --locked-mode
dotnet test tests/DinimizIslam.Testler -m:1
dotnet run --project src/DinimizIslam.Arayuz
.\paket\paketle.ps1 -Iscc 'C:\Program Files (x86)\Inno Setup 6\ISCC.exe'
```

Paketleme Inno Setup 6.7.4 veya uyumlu derleyici kullanır. `.github/workflows/release.yml`, `v1.0.2` gibi etiketlerde test ve paketleme yaparak GitHub Releases'e kurulum, taşınabilir ve kaynak dosyalarını ekler. [Yayın yönergesi](docs/GITHUB.md), [erişilebilirlik denetimi](docs/ERISILEBILIRLIK.md), [değişiklikler](CHANGELOG.md).

MIT lisansı yalnız yazılım içindir; [bildirimleri](NOTICE.md) okuyun. Hata bildirirken sürüm, Windows/NVDA sürümü ve tekrarlanabilir tuş sırasını belirtin. Kişisel verilerinizi ve önbellek/veritabanınızı herkese açık depoya yüklemeyin.
