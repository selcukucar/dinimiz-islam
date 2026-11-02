# Erişilebilirlik ve doğrulama

## 1.0.2 — 2 Kasım 2026

38 çevrimdışı çekirdek testi geçti. Gerçek kaynak HTML örneklerinden ayrı geçici veritabanı kuran WPF navigasyon denetimi geçti. Kullanıcının gerçek verileri bu teste dahil edilmez.

Denetlenen akışlar: ana başlığın sabit kalması; kategori öğelerinin erişilebilir adı; 24 ileri ve 24 geri odak adımında menü ve durum alanının Tab sırasına girmemesi; Enter ile arama sonuçlarının yüklenmesi ve sonuç listesine odak; sonuçta Enter ile makale açılması; makale TextPattern ve salt okunur ValuePattern; gerçek satır sarma; satır aşağı komutunun imleci ilerletmesi; makaleden Tab ile düğmeye ve Shift+Tab ile metne dönüş; arama seçiminin ve kategori seçiminin geri dönüşte korunması.

Bunlar uygulama içi WPF odak ve klavye olay/komut testleridir. Fiziksel tuş enjeksiyonu veya NVDA sesli kullanıcı testi değildir. Canlı bilgisayar denetimi erişim onayında zaman aşımına uğramıştır. NVDA 2025.3.3 ile sesli kabul testi tamamlanmış olarak bildirilmez.

Önceki 1.0.0 teknik denetiminde uzun makale Windows UI Automation tarafından 519 görsel satır olarak algılandı; Microsoft Print to PDF çıktısı üretildi ve PDF yapısı doğrulandı. 1.0.2'de aynı yerel WPF TextBox ve yazdırma yolu korunur. PDF/UA uygunluğu doğrulanmadı.

Bu çalışmada canlı arama sayfası 403 döndürdü. Uygulama site başlıklarını kategori sayfalarından toplar; erişim engelini aşmaz. Eksik taramada başarı veya tüm site tam metin araması iddiası göstermez. İlk dizin taraması zaman alabilir; sonraki arama yereldir. Başlık dizini ile yerel makale metni araması arasındaki kapsam yardımda açıklanır.

## NVDA 2025.3.3 kabul adımları

1. Açılışta Dinimiz İslam başlığını ve kategori adını dinleyin. Oklarla kategori seçin; Enter, Alt+Sol ile girip geri dönün. Aynı öğeye dönüşü kontrol edin.
2. Tab ve Shift+Tab ile tüm bölümleri gezin. Menü adları Tab sırasında çıkmamalıdır. Alt veya F10 ile Dosya, Konular, Görünüm, Ayarlar, Yardım menülerini oklarla açıp Escape ile önceki denetime dönün.
3. Ctrl+F, bir başlık kelimesi, Enter. İlk dizin taramasında F4 ile durumu okuyun. Sonuçlar gelince oklar ve Enter ile makale açın. Alt+Sol sonuçtaki seçimi korumalıdır. Sonuçsuz ve engelli ağ durumlarını da sınayın.
4. Makalede Ctrl+Home, Aşağı/Yukarı ok, Ctrl+ok, Home/End, PageDown/PageUp, Ctrl+End kullanın. Uzun paragraf tek seferde okunmamalı; imleç görsel satırlarda ilerlemelidir. Ctrl+A/C kopyalamalı; yazı yazmak metni değiştirmemelidir. F6 bulma ve Ctrl+K bağlantı seçimi sonrasında odağı kontrol edin.
5. Ctrl+5, şehirde oklar ve Alt+Aşağı, şehir seçimi, Tab ile tabloya giriş. Hücreleri oklarla gezin; tarih ve vakit başlıkları okunmalıdır. Tab tablo dışına çıkmalıdır.
6. F1/F2, ayarlar, adres, yazdırma ve PDF iletişim kutularında Tab/Shift+Tab ve Escape deneyin. Menüden veya iletişim kutusundan dönüşte odak kaybolmamalıdır.
7. Windows yüksek karşıtlık, açık/koyu tema, %200 ölçek ve büyütülmüş yazıda odak görünürlüğünü ve taşmaları sınayın. İnternet kesilmesi, Escape ile iptal ve yükleme sürerken başka bölüme geçişte odağın geri çekilmediğini kontrol edin.

Kapsamlı otomatik denetim, tüm Windows/NVDA yapılandırmalarında hatasızlık garantisi değildir. Gerçek kullanıcı geri bildirimi yeni regresyon testleriyle takip edilir.
