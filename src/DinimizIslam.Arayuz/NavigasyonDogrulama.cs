using System.Text.Json;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Documents;
using System.Windows.Media;
using DinimizIslam.Cekirdek;

namespace DinimizIslam.Arayuz;

public partial class MainWindow
{
    // Test yordamı yalnız açık bayrakla, ayrı veri klasöründe çalışır. İşletim sistemi tuş enjeksiyonu yapmaz.
    public static async Task<int> NavigasyonuDogrula(string ornekler, string cikti)
    {
        Directory.CreateDirectory(cikti);
        if (File.Exists(Path.Combine(cikti, "veriler.sqlite3"))) throw new IOException("Test için yeni bir klasör seçin.");
        Environment.SetEnvironmentVariable("DI_TEST_DATA", cikti);
        var report = new Dictionary<string, object?> { ["Surum"] = "1.0.2", ["Tarih"] = DateTimeOffset.Now,
            ["Yontem"] = "Gerçek kaynak HTML örnekleriyle WPF odak dolaşımı ve komut/olay işleyicisi regresyon denetimi; fiziksel NVDA testi değildir." };
        MainWindow? w = null;
        void Kontrol(string ad, bool dogru) { report[ad] = dogru; if (!dogru) throw new InvalidOperationException(ad); }
        async Task Bekle(Func<bool> hazir)
        {
            var son = DateTime.UtcNow.AddSeconds(10);
            while (!hazir()) { if (DateTime.UtcNow > son) throw new TimeoutException("Arayüz sonucu beklenirken süre aşıldı."); await Task.Delay(30); }
            await Task.Delay(100);
        }
        try
        {
            var depo = new Depo(cikti);
            string Html(string ad) => File.ReadAllText(Path.Combine(ornekler, ad + ".html"));
            var root = Ayristirici.Kategoriler(Html("home"), Ayristirici.AnaSayfa);
            var child = Ayristirici.Kategoriler(Html("category"), root[0].Url);
            var article = Ayristirici.Makale(Html("article"), Ayristirici.AnaSayfa + "detay.asp?Aid=4764");
            depo.Yaz(Ayristirici.AnaSayfa + "#wpf-kategoriler-1", "categories", root);
            depo.Yaz(root[0].Url + "#wpf-kategoriler-1", "categories", child);
            depo.Yaz(article.Url, "article", article);
            depo.Yaz("wpf-baslik-dizini-1", "search-index", root.Concat(child).Append(new(article.Title, article.Url)).ToList());
            w = new MainWindow(); w.Show(); w.Activate();
            await Bekle(() => w.KategoriListesi.Items.Count == root.Count && w._iptal is null);
            Kontrol("PencereBasligi", w.Title == "Dinimiz İslam");
            Kontrol("MenuTabDisinda", KeyboardNavigation.GetTabNavigation(w.AnaMenu) == KeyboardNavigationMode.None);
            Kontrol("DurumTabDisinda", !w.DurumKutusu.IsTabStop);
            Kontrol("KategoriErisilebilirAdi", UIElementAutomationPeer.CreatePeerForElement((ListBoxItem)w.KategoriListesi.ItemContainerGenerator.ContainerFromIndex(0))!.GetName().Contains(root[0].Title));
            w.KategoriArama.Focus();
            var ziyaret = new List<string>();
            for (int i = 0; i < 24; i++)
            {
                if (Keyboard.FocusedElement is FrameworkElement element)
                {
                    Kontrol("TabMenuyeGirmiyor" + i, element is not MenuItem && element != w.AnaMenu && element != w.DurumKutusu);
                    ziyaret.Add(element.GetType().Name + ":" + element.Name);
                    element.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
                }
                await Task.Delay(10);
            }
            report["TabSirasi"] = ziyaret;
            for (int i = 0; i < 24; i++)
            {
                if (Keyboard.FocusedElement is FrameworkElement element)
                {
                    Kontrol("ShiftTabMenuyeGirmiyor" + i, element is not MenuItem && element != w.AnaMenu && element != w.DurumKutusu);
                    element.MoveFocus(new TraversalRequest(FocusNavigationDirection.Previous));
                }
                await Task.Delay(10);
            }
            w.Sekme(2, false); w.AramaKutusu.Focus(); w.AramaKutusu.Text = "MEAL";
            var source = PresentationSource.FromVisual(w)!;
            w.AramaKutusu.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice, source, Environment.TickCount, Key.Enter) { RoutedEvent = Keyboard.PreviewKeyDownEvent });
            await Bekle(() => w.SonucListesi.Items.Count > 0 && w._iptal is null);
            Kontrol("EnterAramaSonucu", w.SonucListesi.Items.Cast<Baslik>().Any(b => b.Url == article.Url));
            Kontrol("AramaSonucOdak", w.SonucListesi.IsKeyboardFocusWithin);
            w.SonucListesi.SelectedItem = w.SonucListesi.Items.Cast<Baslik>().First(b => b.Url == article.Url);
            w.SonucListesi.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice, source, Environment.TickCount, Key.Enter) { RoutedEvent = Keyboard.PreviewKeyDownEvent });
            await Bekle(() => w._makale is not null && w._iptal is null);
            Kontrol("EnterSonucuAcar", w.Sekmeler.SelectedIndex == 1 && w.MakaleKutusu.IsKeyboardFocused);
            Kontrol("MakaleSaltOkunur", w.MakaleKutusu.IsReadOnly && w.MakaleKutusu.IsReadOnlyCaretVisible && !w.MakaleKutusu.AcceptsTab);
            Kontrol("MakaleSariliyor", w.MakaleKutusu.LineCount > article.Paragraphs.Count);
            var peer = new TextBoxAutomationPeer(w.MakaleKutusu);
            Kontrol("MakaleUIATextPattern", peer.GetPattern(PatternInterface.Text) is ITextProvider);
            Kontrol("MakaleUIASaltOkunur", (peer.GetPattern(PatternInterface.Value) as IValueProvider)?.IsReadOnly == true);
            int once = w.MakaleKutusu.CaretIndex;
            EditingCommands.MoveDownByLine.Execute(null, w.MakaleKutusu);
            Kontrol("SatirKomutuImleciIlerletir", w.MakaleKutusu.CaretIndex > once);
            w.MakaleKutusu.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
            Kontrol("MakaleTabDugmeyeGecer", Keyboard.FocusedElement is Button);
            ((UIElement)Keyboard.FocusedElement).MoveFocus(new TraversalRequest(FocusNavigationDirection.Previous));
            Kontrol("MakaleShiftTabMetneDoner", w.MakaleKutusu.IsKeyboardFocused);
            await w.Geri(); await Task.Delay(100);
            Kontrol("GeriAramaSeciminiKorur", w.Sekmeler.SelectedIndex == 2 && (w.SonucListesi.SelectedItem as Baslik)?.Url == article.Url && w.SonucListesi.IsKeyboardFocusWithin);
            w.Sekme(0, false); w.KategoriListesi.SelectedIndex = 0;
            await w.BaslikAc(root[0]); await Task.Delay(100);
            Kontrol("KategoriAcVeOdak", w.KategoriListesi.Items.Count == child.Count && w.KategoriListesi.IsKeyboardFocusWithin);
            await w.Geri(); await Task.Delay(100);
            Kontrol("KategoriGeriSecimiKorur", w.KategoriListesi.Items.Count == root.Count && (w.KategoriListesi.SelectedItem as Baslik)?.Url == root[0].Url);
            Kontrol("BaslikSabitKalir", w.Title == "Dinimiz İslam");
            report["Basarili"] = true;
        }
        catch (Exception ex) { report["Basarili"] = false; report["Hata"] = ex.ToString(); }
        finally
        {
            w?.Close();
            File.WriteAllText(Path.Combine(cikti, "navigasyon-sonuc.json"), JsonSerializer.Serialize(report, new JsonSerializerOptions {
                WriteIndented = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping }));
        }
        return report.GetValueOrDefault("Basarili") is true ? 0 : 1;
    }
}
