using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using DinimizIslam.Cekirdek;
using Microsoft.Win32;

namespace DinimizIslam.Arayuz;

public partial class MainWindow
{
    private async void Adres_Click(object sender, RoutedEventArgs e)
    {
        string? text = Diyaloglar.Sor(this, "Makale adresi aç", "Dinimiz İslam makalesinin HTTPS adresi:");
        if (string.IsNullOrWhiteSpace(text)) return;
        try
        {
            var adres = Ayristirici.Adres(text.Trim());
            if (!System.Text.RegularExpressions.Regex.IsMatch(adres.Query, @"[?&]aid=\d+", System.Text.RegularExpressions.RegexOptions.IgnoreCase)) throw new KaynakHatasi("Makale bağlantısında Aid numarası bulunmalıdır.");
            await BaslikAc(new("Makale", adres.AbsoluteUri));
        }
        catch (KaynakHatasi ex) { Hata(ex.Message); }
    }
    private void Ac(string adres)
    {
        try { var uri = Ayristirici.Adres(adres); Process.Start(new ProcessStartInfo(uri.AbsoluteUri) { UseShellExecute = true }); }
        catch (Exception ex) { App.HataYaz(ex); Hata("Varsayılan tarayıcı açılamadı. Kaynak adresini kopyalayarak açabilirsiniz."); }
    }
    private void KategoriKaynak_Click(object sender, RoutedEventArgs e) => Ac(_kategoriAdresi);
    private void MakaleKaynak_Click(object sender, RoutedEventArgs e) { if (_makale is not null) Ac(_makale.Url); }
    private void ImsakiyeKaynak_Click(object sender, RoutedEventArgs e) => Ac(_imsakiye?.Url ?? Ayristirici.ImsakiyeAdresi);
    private void KaynakBilgisi_Click(object sender, RoutedEventArgs e)
    {
        if (_makale is null) { Bildir("Önce bir makale açın."); return; }
        Diyaloglar.Metin(this, "Makale kaynak bilgisi", _makale.Title + "\r\n" + _makale.Url + "\r\nAlınma tarihi: " + Turkce.Tarih(_makale.Fetched) + "\r\n" + _makale.Warning);
    }
    private void MetniKopyala_Click(object sender, RoutedEventArgs e) { if (_makale is not null) { Clipboard.SetText(_makale.OkumaMetni); Bildir("Makalenin tamamı kopyalandı."); } }
    private void YerImiDegistir_Click(object sender, RoutedEventArgs e)
    {
        if (_makale is null) { Bildir("Önce bir makale açın."); return; }
        bool added = _depo.Isaretle(new(_makale.Title, _makale.Url)); Bildir(added ? "Yer imi eklendi." : "Yer imi kaldırıldı.");
    }
    private void YerImiSil_Click(object sender, RoutedEventArgs e)
    {
        if (YerImiListesi.SelectedItem is not Baslik b) return;
        _depo.Isaretle(b); Doldur(YerImiListesi, _depo.Liste()); ListeyeOdakla(YerImiListesi, YerImiListesi); Bildir("Yer imi kaldırıldı.");
    }
    private void Baglantilar_Click(object sender, RoutedEventArgs e)
    {
        if (_makale is null) return;
        var links = Ayristirici.Baglantilar(_makale);
        if (links.Count == 0) { Bildir("Bu makalede okuyucuda açılabilen bağlantı yok."); return; }
        var selected = Diyaloglar.BaslikSec(this, "Makaledeki bağlantılar", links);
        if (selected is not null)
        {
            if (selected.Url.Contains("aid=", StringComparison.OrdinalIgnoreCase)) _ = BaslikAc(selected);
            else Ac(selected.Url);
        }
    }
    private async Task Yenile()
    {
        switch (Sekmeler.SelectedIndex)
        {
            case 0: await KategoriGetir(_kategoriAdresi, _kategoriAdi, true, true); break;
            case 1 when _makale is not null:
                var url = _makale.Url;
                await Islem("makale:" + url, "Makale yenileniyor…", async ct => { var s = await _servis.Makale(url, true, ct); ct.ThrowIfCancellationRequested(); MakaleyiGoster(s.Veri, s.Durum, _makaleKaynagi); }); break;
            case 2: await Ara(); break;
            case 3: await ImsakiyeGetir(true, true); break;
            case 4: Doldur(YerImiListesi, _depo.Liste()); break;
            case 5: Doldur(GecmisListesi, _depo.Liste(true)); break;
        }
    }
    private async void Yenile_Click(object sender, RoutedEventArgs e) => await Yenile();
    private void GorunumuUygula()
    {
        FontSize = Math.Clamp(_depo.Ayar("font_size_wpf", 16d), 14, 32);
        MakaleKutusu.FontSize = FontSize + 3;
        Application.Current.ThemeMode = _depo.Ayar("theme", "system") switch { "light" => ThemeMode.Light, "dark" => ThemeMode.Dark, _ => ThemeMode.System };
    }
    private void YaziBoyutu(double value) { _depo.AyarYaz("font_size_wpf", Math.Clamp(value, 14, 32)); GorunumuUygula(); }
    private void Buyut_Click(object sender, RoutedEventArgs e) => YaziBoyutu(FontSize + 2);
    private void Kucult_Click(object sender, RoutedEventArgs e) => YaziBoyutu(FontSize - 2);
    private void NormalBoyut_Click(object sender, RoutedEventArgs e) => YaziBoyutu(16);
    private void Ayarlar_Click(object sender, RoutedEventArgs e)
    {
        Diyaloglar.Ayarlar(this, _depo, history =>
        {
            if (_iptal is not null) { Bildir("Verileri temizlemeden önce süren işlemi iptal edin."); return; }
            if (Diyaloglar.Onay(this, "Yerel verileri temizle", history ? "Okuma geçmişi silinsin mi?" : "Önbellek silinsin mi? Çevrimdışı makale kopyaları kaldırılır; yer imleri korunur.")) { _depo.Temizle(history); Bildir("Seçilen yerel veriler temizlendi."); }
        });
        GorunumuUygula();
    }
    private void Durum_Click(object sender, RoutedEventArgs e) { DurumKutusu.Focus(); DurumKutusu.CaretIndex = 0; }
    private void Gunluk_Click(object sender, RoutedEventArgs e) => Process.Start(new ProcessStartInfo(App.VeriKlasoru) { UseShellExecute = true });
    private void Bul()
    {
        if (_makale is null) { Bildir("Önce bir makale açın."); return; }
        var query = Diyaloglar.Sor(this, "Makale içinde bul", "Bulunacak metin:"); if (string.IsNullOrEmpty(query)) return;
        int start = MakaleKutusu.SelectionStart + MakaleKutusu.SelectionLength;
        int found = Turkce.Kultur.CompareInfo.IndexOf(MakaleKutusu.Text, query, start, System.Globalization.CompareOptions.IgnoreCase);
        if (found < 0) found = Turkce.Kultur.CompareInfo.IndexOf(MakaleKutusu.Text, query, System.Globalization.CompareOptions.IgnoreCase);
        if (found < 0) { Bildir("Metin bulunamadı."); return; }
        Sekme(1, false); MakaleKutusu.Select(found, query.Length); MakaleKutusu.Focus(); MakaleKutusu.ScrollToLine(MakaleKutusu.GetLineIndexFromCharacterIndex(found));
    }
    private const string Kisayollar = """
        Ctrl+1 / Ctrl+2: Konular ve kategoriler
        Ctrl+3: Başlıklarda ve yerel metinlerde arama
        Ctrl+4: Makale okuyucu
        Ctrl+5: İmsakiye
        Ctrl+B: Yer imleri
        Ctrl+F: Arama alanı
        Ctrl+O: Makale adresi aç
        Alt+Sol: Üst kategori / önceki liste
        Enter: Seçili başlığı aç
        Alt / F10: Menü çubuğu
        Tab / Shift+Tab: Sonraki / önceki denetim
        Oklar: Listelerde ve metin satırlarında gezin
        Ctrl+Home / Ctrl+End: Metnin başı / sonu
        Ctrl+A / Ctrl+C: Metni seç / kopyala
        Ctrl+K: Makaledeki bağlantılar
        F6: Makalede bul
        Ctrl+R / F5: Yenile
        Ctrl+P: Yazdır
        Ctrl+Shift+S: PDF olarak kaydet
        Ctrl+Artı / Ctrl+Eksi / Ctrl+0: Yazı boyutu
        Ctrl+Virgül: Ayarlar
        F1: Yardım
        F2: Klavye kısayolları
        F4: Son işlem durumu
        Escape: Süren işlemi iptal et / iletişim kutusunu kapat
        Alt+F4: Uygulamayı kapat
        """;
    private void Kisayollar_Click(object sender, RoutedEventArgs e) => Diyaloglar.Metin(this, "Klavye kısayolları", Kisayollar);
    private void Yardim_Click(object sender, RoutedEventArgs e) => Diyaloglar.Metin(this, "Kullanım kılavuzu", """
        Dinimiz İslam — WPF sürümü

        Uygulama doğrudan ana kategorilerle açılır. Ok tuşlarıyla seçin; Enter ile kategoriye veya makaleye girin. Başlıklarda ara alanı bulunduğunuz kategorinin başlıklarını süzer. Bu alanda Aşağı ok ya da Enter listeye geçer. Alt+Sol üst kategoriye döner; filtre ve seçiminiz korunur. Konular menüsü ana başlıklara doğrudan erişim sağlar.

        Makale açıldığında odak salt okunur, imleci görünür metin kutusuna gelir. Başlık ilk satırdadır. Paragraflar gerçek satır sonlarıyla ayrılır; uzun paragraflar pencereye göre sarılır. Oklar ve Ctrl+oklar metni okur. Ctrl+Home başa gider. Metin değiştirilemez. Tab işlem düğmelerine geçer. Alt+Sol geldiğiniz listeye döner. Ctrl+K bağlantıları ayrı listede açar. F6 metin bulur.

        Arama site kategori ve makale başlıklarını, ayrıca daha önce açılmış makalelerin metinlerini kapsar. İlk aramada kategori başlıkları kaynaktan alınır; sonraki aramalar kaydedilmiş dizini kullanır. Başlık dizinini yenile düğmesi güncel başlıkları getirir. Sorgunuz dışarı gönderilmez. Henüz açılmamış makalelerin tam metinleri aranmaz. Ctrl+F ile arama alanına gidip Enter kullanın. Sonuçlara odak gelir; oklarla seçip Enter ile açın. Kaynak alınamazsa eksik sonuç açıklaması verilir.

        Ctrl+5 imsakiyeyi açar. Kaynakta yayımlanan yıl gösterilir; başka yıl veya vakit hesaplanmaz. Şehir değiştirildiğinde kaynak tablo alınır. Oklarla tablo hücreleri gezilir. Tarih süz alanı görünümü süzer; yazdırma ve PDF belgenin tamamını içerir.

        Yer imleri, son okunanlar ve ayarlar yereldir. İnternet yoksa mevcut kopya önbellek ve alınma tarihi belirtilerek açılır. Ctrl+R kaynaktan yeniden almaya çalışır; Escape süren işi iptal eder.

        Ctrl+P Windows yazdırma penceresini açar. PDF komutu Microsoft Print to PDF yazıcısını kullanır; kayıt tamamlanıp dosya kontrol edilmeden başarı bildirilmez. Bu yazıcı bulunmazsa durum açıklanır. PDF/UA etiketleme uygunluğu doğrulanmamıştır.

        Ayarlarda tema, yazı boyutu, önbellek süresi ve PDF klasörü seçilir. Yüksek karşıtlıkta Windows renkleri izlenir. F4 son durumu tekrar okumanızı sağlar. Kaynak içindeki medya veya karmaşık tablo için özgün sayfayı açabilirsiniz.

        Bu bağımsız uygulama dinimizislam.com'un resmî ürünü değildir. NVDA ile yapılmayan testler yapılmış olarak bildirilmez. Sorun varsa sürümü, başlığı ve kullandığınız tuşları kaydedin. Günlük klasörü Yardım menüsündedir.
        """ + "\r\n\r\n" + Kisayollar);
    private void Hakkinda_Click(object sender, RoutedEventArgs e) => Diyaloglar.Metin(this, "Hakkında", "Dinimiz İslam 1.0.2 — WPF\r\nC# / .NET 10 / Windows Presentation Foundation\r\n\r\nBağımsız okuyucudur; resmî bir site ürünü veya onayı değildir.\r\nKaynak: https://dinimizislam.com/\r\n\r\nMimari ve kullanım referansı: ersalih51/turkiye-takvimi.\r\nBağımlılıklar: HtmlAgilityPack (MIT), Microsoft.Data.Sqlite ve .NET/WPF (MIT).\r\nKaynak kod lisansı ve bildirimler dağıtım içindedir. Doğrulanmış bir destek adresi sağlanmamıştır.");
    private async void Pencere_KeyDown(object sender, KeyEventArgs e)
    {
        var key = e.Key == Key.System ? e.SystemKey : e.Key;
        if (Keyboard.Modifiers == ModifierKeys.Alt && key == Key.Left) { e.Handled = true; await Geri(); return; }
        if (Keyboard.Modifiers == ModifierKeys.Control)
        {
            switch (key)
            {
                case Key.D1: case Key.D2: e.Handled = true; Sekme(0); return;
                case Key.D3: e.Handled = true; Sekme(2); return;
                case Key.D4: e.Handled = true; Sekme(1); return;
                case Key.D5: e.Handled = true; Sekme(3); return;
                case Key.B: e.Handled = true; Sekme(4); return;
                case Key.F: e.Handled = true; Sekme(2, false); AramaKutusu.Focus(); AramaKutusu.SelectAll(); return;
                case Key.O: e.Handled = true; Adres_Click(this, e); return;
                case Key.K: e.Handled = true; Baglantilar_Click(this, e); return;
                case Key.R: e.Handled = true; await Yenile(); return;
                case Key.P: e.Handled = true; Yazdir_Click(this, e); return;
                case Key.OemComma: e.Handled = true; Ayarlar_Click(this, e); return;
                case Key.Add: case Key.OemPlus: e.Handled = true; YaziBoyutu(FontSize + 2); return;
                case Key.Subtract: case Key.OemMinus: e.Handled = true; YaziBoyutu(FontSize - 2); return;
                case Key.D0: e.Handled = true; YaziBoyutu(16); return;
            }
        }
        if (Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift) && key == Key.S) { e.Handled = true; Pdf_Click(this, e); return; }
        if (Keyboard.Modifiers != ModifierKeys.None) return;
        switch (key)
        {
            case Key.F1: e.Handled = true; Yardim_Click(this, e); break;
            case Key.F2: e.Handled = true; Kisayollar_Click(this, e); break;
            case Key.F4: e.Handled = true; DurumKutusu.Focus(); DurumKutusu.CaretIndex = 0; break;
            case Key.F5: e.Handled = true; await Yenile(); break;
            case Key.F6: e.Handled = true; Bul(); break;
            case Key.Escape when _iptal is not null: e.Handled = true; _iptal.Cancel(); break;
        }
    }
}
