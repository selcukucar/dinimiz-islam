using System.ComponentModel;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using DinimizIslam.Cekirdek;

namespace DinimizIslam.Arayuz;

public partial class MainWindow : Window
{
    private readonly Depo _depo;
    private readonly KaynakIstemci _istemci;
    private readonly IcerikServisi _servis;
    private CancellationTokenSource? _iptal;
    private long _isNo;
    private CancellationTokenSource? _sehirBekleme;
    private string _isAnahtari = "";
    private bool _kapaniyor, _sehirDolduruluyor;
    private List<Baslik> _basliklar = [];
    private readonly Stack<KategoriKonumu> _geri = [];
    private string _kategoriAdresi = Ayristirici.AnaSayfa, _kategoriAdi = "Ana kategoriler";
    private Makale? _makale;
    private Imsakiye? _imsakiye;
    private int _makaleKaynagi;
    private record KategoriKonumu(string Adres, string Ad, string Secim, string Filtre);
    public record VakitSatiri(string Gun, string Tarih, string Imsak, string Sabah, string Gunes, string Ogle, string Ikindi, string Aksam, string Yatsi);

    public MainWindow()
    {
        InitializeComponent();
        _depo = new Depo(App.VeriKlasoru); _istemci = new KaynakIstemci(); _servis = new IcerikServisi(_istemci, _depo);
        GorunumuUygula();
    }
    private async void Pencere_Loaded(object sender, RoutedEventArgs e) => await KategoriGetir(Ayristirici.AnaSayfa, "Ana kategoriler", false, true);
    private void Pencere_Closing(object? sender, CancelEventArgs e) { _kapaniyor = true; _sehirBekleme?.Cancel(); _iptal?.Cancel(); _istemci.Dispose(); }

    private async Task Islem(string key, string durum, Func<CancellationToken, Task> islem)
    {
        if (_iptal is not null && _isAnahtari == key) { Bildir("Bu işlem sürüyor. İptal etmek için Escape kullanabilirsiniz."); return; }
        _iptal?.Cancel(); var cts = new CancellationTokenSource(); _iptal = cts;
        long no = ++_isNo; _isAnahtari = key; IptalDugmesi.IsEnabled = true; Bildir(durum);
        try { await islem(cts.Token); }
        catch (OperationCanceledException) { if (no == _isNo && !_kapaniyor) Bildir("İşlem iptal edildi."); }
        catch (KaynakHatasi ex) { if (no == _isNo && !_kapaniyor) Hata(ex.Message); }
        catch (Exception ex)
        {
            App.HataYaz(ex);
            if (no == _isNo && !_kapaniyor) Hata("İşlem tamamlanamadı. Disk alanını ve bağlantınızı kontrol edin. Günlük klasörü Yardım menüsündedir.");
        }
        finally
        {
            if (no == _isNo) { bool iptalOdakta = IptalDugmesi.IsKeyboardFocused; _iptal = null; _isAnahtari = ""; IptalDugmesi.IsEnabled = false; if (iptalOdakta) IcerigeOdakla(Sekmeler.SelectedIndex); }
            cts.Dispose();
        }
    }
    private void Hata(string mesaj) { Bildir(mesaj); Diyaloglar.Metin(this, "İşlem tamamlanamadı", mesaj + "\r\n\r\nÖzgün sayfa komutuyla kaynağı tarayıcıda açabilir veya Ctrl+R ile yeniden deneyebilirsiniz."); }
    private void Bildir(string mesaj)
    {
        DurumKutusu.Text = mesaj;
        if (!IsActive) return;
        Dispatcher.BeginInvoke(DispatcherPriority.Background, () =>
        {
            if (!IsActive || _kapaniyor) return;
            var peer = UIElementAutomationPeer.FromElement(DurumKutusu) ?? UIElementAutomationPeer.CreatePeerForElement(DurumKutusu);
            peer?.RaiseAutomationEvent(AutomationEvents.LiveRegionChanged);
        });
    }
    private void Sekme(int index, bool odak = true)
    {
        Sekmeler.SelectedIndex = index;
        if (odak) _ = Dispatcher.BeginInvoke(DispatcherPriority.Input, () => IcerigeOdakla(index));
    }
    private void IcerigeOdakla(int index)
    {
        if (Sekmeler.SelectedIndex != index || !IsActive || _kapaniyor) return;
        switch (index)
        {
            case 0: ListeyeOdakla(KategoriListesi, KategoriArama); break;
            case 1: MakaleKutusu.Focus(); break;
            case 2: if (SonucListesi.Items.Count > 0) ListeyeOdakla(SonucListesi, AramaKutusu); else AramaKutusu.Focus(); break;
            case 3: SehirKutusu.Focus(); break;
            case 4: ListeyeOdakla(YerImiListesi, YerImiListesi); break;
            case 5: ListeyeOdakla(GecmisListesi, GecmisListesi); break;
        }
    }
    private static void ListeyeOdakla(ListBox liste, UIElement bos)
    {
        liste.UpdateLayout();
        if (liste.Items.Count == 0) { bos.Focus(); return; }
        if (liste.SelectedIndex < 0) liste.SelectedIndex = 0;
        liste.ScrollIntoView(liste.SelectedItem); liste.UpdateLayout();
        if (liste.ItemContainerGenerator.ContainerFromItem(liste.SelectedItem) is ListBoxItem item) item.Focus(); else liste.Focus();
    }
    private static void Doldur(ListBox liste, List<Baslik> basliklar, string? secim = null)
    {
        liste.ItemsSource = basliklar;
        liste.SelectedItem = basliklar.FirstOrDefault(b => b.Url == secim) ?? basliklar.FirstOrDefault();
    }
    private async Task KategoriGetir(string adres, string ad, bool yenile, bool odak, KategoriKonumu? geri = null)
    {
        await Islem("kategori:" + adres, "Kategoriler alınıyor…", async ct =>
        {
            var sonuc = await _servis.Kategoriler(adres, yenile, ct); ct.ThrowIfCancellationRequested();
            _kategoriAdresi = adres; _kategoriAdi = ad; _basliklar = sonuc.Veri;
            KategoriBasligi.Text = ad;
            KategoriArama.Text = geri?.Filtre ?? ""; KategorileriSuz();
            if (geri is not null && KategoriListesi.ItemsSource is List<Baslik> liste) Doldur(KategoriListesi, liste, geri.Secim);
            if (adres == Ayristirici.AnaSayfa) KonularMenusunuDoldur(sonuc.Veri);
            if (Sekmeler.SelectedIndex == 0) { if (odak) _ = Dispatcher.BeginInvoke(DispatcherPriority.Input, () => { if (Sekmeler.SelectedIndex == 0 && IsActive) ListeyeOdakla(KategoriListesi, KategoriArama); }); }
            KategoriAltBilgi.Text = $"{sonuc.Veri.Count} başlık. Enter ile açın; Alt+Sol ile geri dönün.";
            Bildir(ad + ". " + sonuc.Veri.Count + " başlık. " + sonuc.Durum);
        });
    }
    private void KonularMenusunuDoldur(List<Baslik> basliklar)
    {
        while (KonularMenusu.Items.Count > 3) KonularMenusu.Items.RemoveAt(3);
        foreach (var baslik in basliklar)
        {
            var item = new MenuItem { Header = baslik.Title.Replace("_", "__"), Tag = baslik };
            item.Click += async (_, _) => { Sekme(0, false); await BaslikAc(baslik); }; KonularMenusu.Items.Add(item);
        }
    }
    private void KategorileriSuz()
    {
        if (!IsLoaded) return;
        var liste = _basliklar.Where(b => Turkce.Icerir(b.Title, KategoriArama.Text)).ToList();
        Doldur(KategoriListesi, liste);
        KategoriAltBilgi.Text = $"{liste.Count} başlık. Enter ile açın; Alt+Sol ile geri dönün.";
    }
    private KategoriKonumu KategoriDurumu() => new(_kategoriAdresi, _kategoriAdi, (KategoriListesi.SelectedItem as Baslik)?.Url ?? "", KategoriArama.Text);
    private async Task BaslikAc(Baslik baslik)
    {
        if (baslik.Category)
        {
            _geri.Push(KategoriDurumu()); Sekme(0, false);
            await KategoriGetir(baslik.Url, baslik.Title, false, true);
        }
        else
        {
            int kaynak = Sekmeler.SelectedIndex;
            await Islem("makale:" + baslik.Url, "Makale açılıyor…", async ct =>
            {
                var sonuc = await _servis.Makale(baslik.Url, false, ct); ct.ThrowIfCancellationRequested();
                MakaleyiGoster(sonuc.Veri, sonuc.Durum, kaynak);
            });
        }
    }
    private void MakaleyiGoster(Makale makale, string durum, int kaynak)
    {
        _makale = makale; _makaleKaynagi = kaynak == 1 ? _makaleKaynagi : kaynak; _depo.Okundu(makale);
        MakaleBasligi.Text = makale.Title; MakaleKutusu.Text = makale.OkumaMetni;
        AutomationProperties.SetName(MakaleKutusu, makale.Title + ", salt okunur makale");
        AutomationProperties.SetHelpText(MakaleKutusu, "Satırları oklarla okuyun. Ctrl+Home metnin başına gider. Alt+Sol başlıklara döner.");
        Sekme(1, false); MakaleKutusu.UpdateLayout(); MakaleKutusu.CaretIndex = 0; MakaleKutusu.ScrollToHome();
        Dispatcher.BeginInvoke(DispatcherPriority.Input, () => { if (Sekmeler.SelectedIndex == 1 && IsActive) MakaleKutusu.Focus(); });
        Bildir("Makale açıldı. " + durum);
    }
    private async Task Geri()
    {
        if (Sekmeler.SelectedIndex == 1) { Sekme(_makaleKaynagi); return; }
        if (Sekmeler.SelectedIndex == 0 && _geri.TryPop(out var konum)) await KategoriGetir(konum.Adres, konum.Ad, false, true, konum);
        else Sekme(0);
    }
    private async Task Ara(bool yenile = false)
    {
        var query = AramaKutusu.Text.Trim(); if (query.Length == 0) { Bildir("Aramak için bir kelime yazın."); AramaKutusu.Focus(); return; }
        await Islem("arama:" + query + ":" + yenile, "Başlıklarda ve yerel metinlerde aranıyor…", async ct =>
        {
            var ilerleme = new Progress<string>(mesaj => { if (!ct.IsCancellationRequested && !_kapaniyor) { AramaBilgi.Text = mesaj; Bildir(mesaj); } });
            var result = await _servis.BasliklardaAra(query, yenile, ilerleme, ct);
            ct.ThrowIfCancellationRequested(); Doldur(SonucListesi, result.Basliklar);
            AramaBilgi.Text = $"{result.Basliklar.Count} sonuç. {result.Durum}";
            if (Sekmeler.SelectedIndex == 2) Sekme(2);
            Bildir(AramaBilgi.Text);
        });
    }
    private async Task ImsakiyeGetir(bool yenile, bool odak)
    {
        string kod = SehirKutusu.SelectedValue as string ?? _depo.Ayar("location", "");
        await Islem("imsakiye:" + kod, "İmsakiye alınıyor…", async ct =>
        {
            var sonuc = await _servis.Imsakiye(kod, yenile, ct); ct.ThrowIfCancellationRequested(); _imsakiye = sonuc.Veri;
            _sehirDolduruluyor = true;
            var sehirler = new List<Sehir> { new("Kaynağın varsayılan şehri", "") }; sehirler.AddRange(_imsakiye.Locations);
            SehirKutusu.ItemsSource = sehirler; SehirKutusu.SelectedValue = kod; if (SehirKutusu.SelectedIndex < 0) SehirKutusu.SelectedIndex = 0;
            _sehirDolduruluyor = false; _depo.AyarYaz("location", kod);
            YilKutusu.Text = _imsakiye.Year.ToString();
            ImsakiyeBilgi.Text = _imsakiye.Title + "\n" + sonuc.Durum;
            ImsakiyeNotlari.Text = string.Join("\r\n", _imsakiye.Notes);
            ImsakiyeyiSuz();
            if (Sekmeler.SelectedIndex == 3 && odak) _ = Dispatcher.BeginInvoke(DispatcherPriority.Input, () => ImsakiyeTablosu.Focus());
            Bildir($"{_imsakiye.Title}. {_imsakiye.Rows.Count} gün. {sonuc.Durum}");
        });
    }
    private void ImsakiyeyiSuz()
    {
        if (_imsakiye is null) return;
        ImsakiyeTablosu.ItemsSource = _imsakiye.Rows.Where(r => Turkce.Icerir(r[0] + " " + r[1], TarihSuz.Text))
            .Select(r => new VakitSatiri(r[0],r[1],r[2],r[3],r[4],r[5],r[6],r[7],r[8])).ToList();
    }
    private void Sekmeler_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (!IsLoaded || e.OriginalSource != Sekmeler) return;
        int index = Sekmeler.SelectedIndex;
        _iptal?.Cancel();
        if (index is 4 or 5)
        {
            var liste = index == 4 ? YerImiListesi : GecmisListesi; var selected = (liste.SelectedItem as Baslik)?.Url;
            Doldur(liste, _depo.Liste(index == 5), selected);
        }
        if (index == 3 && _imsakiye is null) _ = ImsakiyeGetir(false, false);

    }
    private void KategoriArama_Changed(object sender, TextChangedEventArgs e) => KategorileriSuz();
    private void KategoriArama_KeyDown(object sender, KeyEventArgs e) { if (e.Key is Key.Down or Key.Enter) { e.Handled = true; ListeyeOdakla(KategoriListesi, KategoriArama); } }
    private async void Liste_KeyDown(object sender, KeyEventArgs e) { if (e.Key == Key.Enter && sender is ListBox list && list.SelectedItem is Baslik b) { e.Handled = true; await BaslikAc(b); } }
    private async void Liste_DoubleClick(object sender, MouseButtonEventArgs e) { if (sender is ListBox list && list.SelectedItem is Baslik b) await BaslikAc(b); }
    private async void Arama_KeyDown(object sender, KeyEventArgs e) { if (e.Key == Key.Enter) { e.Handled = true; await Ara(); } else if (e.Key == Key.Down) { e.Handled = true; ListeyeOdakla(SonucListesi, AramaKutusu); } }
    private async void Sehir_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (!IsLoaded || _sehirDolduruluyor || SehirKutusu.IsDropDownOpen || SehirKutusu.SelectedItem is not Sehir) return;
        _sehirBekleme?.Cancel(); using var cts = new CancellationTokenSource(); _sehirBekleme = cts;
        try { await Task.Delay(450, cts.Token); await ImsakiyeGetir(false, false); }
        catch (OperationCanceledException) { }
        finally { if (_sehirBekleme == cts) _sehirBekleme = null; }
    }
    private async void Sehir_Kapandi(object sender, EventArgs e)
    {
        _sehirBekleme?.Cancel();
        if (IsLoaded && !_sehirDolduruluyor && SehirKutusu.SelectedItem is Sehir) await ImsakiyeGetir(false, false);
    }
    private void TarihSuz_Changed(object sender, TextChangedEventArgs e) { if (IsLoaded) ImsakiyeyiSuz(); }
    private void Iptal_Click(object sender, RoutedEventArgs e) => _iptal?.Cancel();
    private async void BaslikAc_Click(object sender, RoutedEventArgs e) { if (KategoriListesi.SelectedItem is Baslik b) await BaslikAc(b); }
    private async void UstKategori_Click(object sender, RoutedEventArgs e) => await Geri();
    private async void AnaKategoriler_Click(object sender, RoutedEventArgs e) { _geri.Clear(); Sekme(0, false); await KategoriGetir(Ayristirici.AnaSayfa, "Ana kategoriler", false, true); }
    private async void KategoriYenile_Click(object sender, RoutedEventArgs e) => await KategoriGetir(_kategoriAdresi, _kategoriAdi, true, true);
    private void Konular_Click(object sender, RoutedEventArgs e) => Sekme(0);
    private void Okuyucu_Click(object sender, RoutedEventArgs e) => Sekme(1);
    private void Arama_Click(object sender, RoutedEventArgs e) => Sekme(2);
    private void Imsakiye_Click(object sender, RoutedEventArgs e) => Sekme(3);
    private void YerImleri_Click(object sender, RoutedEventArgs e) => Sekme(4);
    private void Gecmis_Click(object sender, RoutedEventArgs e) => Sekme(5);
    private async void Geri_Click(object sender, RoutedEventArgs e) => await Geri();
    private async void AramaDiziniYenile_Click(object sender, RoutedEventArgs e) => await Ara(true);
    private async void Ara_Click(object sender, RoutedEventArgs e) => await Ara();
    private async void ImsakiyeYenile_Click(object sender, RoutedEventArgs e) => await ImsakiyeGetir(true, true);
    private async void YerImiAc_Click(object sender, RoutedEventArgs e) { if (YerImiListesi.SelectedItem is Baslik b) await BaslikAc(b); }
    private void Cikis_Click(object sender, RoutedEventArgs e) => Close();
}
