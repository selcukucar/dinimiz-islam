using System.Text.Json;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Automation.Text;
using System.Windows.Controls;
using DinimizIslam.Cekirdek;

namespace DinimizIslam.Arayuz;

/// <summary>Yalnız açık komut satırı bayrağıyla çalışan, ayrı klasöre yazan canlı derleme denetimi.</summary>
public static class PaketDogrulama
{
    public static async Task<int> Calistir(string klasor)
    {
        Directory.CreateDirectory(klasor); var report = new Dictionary<string, object?> { ["Surum"] = "1.0.2 WPF", ["Tarih"] = DateTimeOffset.Now };
        Window? window = null;
        try
        {
            using var client = new KaynakIstemci(); var service = new IcerikServisi(client, new Depo(klasor));
            var root = await service.Kategoriler(Ayristirici.AnaSayfa, true, default);
            var child = await service.Kategoriler(root.Veri[0].Url, true, default);
            var article = await service.Makale(Ayristirici.AnaSayfa + "detay.asp?Aid=4764", true, default);
            var calendar = await service.Imsakiye("", true, default);
            report["AnaKategoriler"] = root.Veri.Count; report["AltBasliklar"] = child.Veri.Count;
            report["MakaleParagraflari"] = article.Veri.Paragraphs.Count; report["ImsakiyeGunleri"] = calendar.Veri.Rows.Count;
            var box = new TextBox { Text = article.Veri.OkumaMetni, IsReadOnly = true, IsReadOnlyCaretVisible = true,
                TextWrapping = TextWrapping.Wrap, AcceptsReturn = true, AcceptsTab = false, FontSize = 19, Padding = new Thickness(16),
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            AutomationProperties.SetName(box, article.Veri.Title);
            window = new Window { Title = "Dinimiz İslam — Teknik doğrulama", Width = 650, Height = 450, Content = box, ShowActivated = false };
            window.Show(); window.UpdateLayout(); box.CaretIndex = 0;
            var peer = new TextBoxAutomationPeer(box);
            var provider = peer.GetPattern(PatternInterface.Text) as ITextProvider ?? throw new InvalidOperationException("UIA TextPattern yok.");
            var range = provider.DocumentRange.Clone(); range.MoveEndpointByRange(TextPatternRangeEndpoint.End, range, TextPatternRangeEndpoint.Start);
            range.ExpandToEnclosingUnit(TextUnit.Line); string first = range.GetText(-1);
            range.Move(TextUnit.Line, 1); string second = range.GetText(-1);
            var longParagraph = article.Veri.Paragraphs.First(p => p.Length > 300);
            var longRange = provider.DocumentRange.FindText(longParagraph[..30], false, false)
                ?? throw new InvalidOperationException("Uzun paragraf UIA içinde bulunamadı.");
            longRange.ScrollIntoView(true); window.UpdateLayout();
            longRange.MoveEndpointByRange(TextPatternRangeEndpoint.End, longRange, TextPatternRangeEndpoint.Start);
            longRange.ExpandToEnclosingUnit(TextUnit.Line); string longFirst = longRange.GetText(-1);
            longRange.Move(TextUnit.Line, 1); string longSecond = longRange.GetText(-1);
            report["Denetim"] = "WPF TextBox"; report["SaltOkunur"] = box.IsReadOnly;
            report["UIA_TextPattern"] = true; report["UIA_BirinciSatir"] = first; report["UIA_IkinciSatir"] = second;
            report["GorselSatirlar"] = box.LineCount;
            report["UIA_UzunParagrafBirinciSatir"] = longFirst;
            report["UIA_UzunParagrafIkinciSatir"] = longSecond;
            report["UIA_Value_SaltOkunur"] = (peer.GetPattern(PatternInterface.Value) as IValueProvider)?.IsReadOnly;
            if (first.Length > 300 || first == second || box.LineCount < 20) throw new InvalidOperationException("UIA satır sınırları doğrulanamadı.");
            if (longFirst.Length > 200 || longFirst == longSecond || longFirst.Length == 0)
                throw new InvalidOperationException("Uzun paragraf ayrı UIA satırları olarak okunamıyor.");
            try
            {
                await Task.Run(() => PdfYazici.Kaydet(article.Veri.Title, article.Veri.OkumaMetni + "\r\nKaynak: " + article.Veri.Url,
                    Path.Combine(klasor, "makale.pdf"), default));
                report["PDF"] = "Dosya yazıldı ve PDF yapısı doğrulandı.";
            }
            catch (Exception ex) { report["PDF"] = "Bu ortamda doğrulanamadı: " + ex.Message; }
            report["NVDA_SesliTest"] = "Yapılmadı; UIA TextPattern denetimi sesli NVDA testi değildir.";
            report["Basarili"] = true;
        }
        catch (Exception ex) { report["Basarili"] = false; report["Hata"] = ex.ToString(); }
        finally
        {
            if (window is not null) window.Close();
            File.WriteAllText(Path.Combine(klasor, "paket-sonuc.json"), JsonSerializer.Serialize(report, new JsonSerializerOptions {
                WriteIndented = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping }));
        }
        return report.TryGetValue("Basarili", out var ok) && ok is true ? 0 : 1;
    }
}
