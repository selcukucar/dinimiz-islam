using System.Globalization;

namespace DinimizIslam.Cekirdek;

public sealed class KaynakHatasi(string mesaj, Exception? ic = null) : Exception(mesaj, ic);
public sealed record Baslik(string Title, string Url, bool Category = false)
{
    public string OkunurAd => (Category ? "Kategori: " : "Makale: ") + Title;
}
public sealed record Makale
{
    public string Title { get; init; } = "";
    public string Url { get; init; } = "";
    public string Html { get; init; } = "";
    public string Text { get; init; } = "";
    public DateTimeOffset Fetched { get; init; } = DateTimeOffset.UtcNow;
    public string Warning { get; init; } = "";
    public List<string> Paragraphs { get; init; } = [];
    public string OkumaMetni => Title + "\r\n\r\n" + string.Join("\r\n", Paragraphs);
}
public sealed record Sehir(string Ad, string Kod);
public sealed record Imsakiye
{
    public string Title { get; init; } = "";
    public string Url { get; init; } = "";
    public int Year { get; init; }
    public List<string> Headers { get; init; } = [];
    public List<List<string>> Rows { get; init; } = [];
    public List<Sehir> Locations { get; init; } = [];
    public List<string> Notes { get; init; } = [];
    public DateTimeOffset Fetched { get; init; } = DateTimeOffset.UtcNow;
    public string Metin => Title + "\r\n\r\n" + string.Join(" | ", Headers) + "\r\n" +
        string.Join("\r\n", Rows.Select(r => string.Join(" | ", r))) + "\r\n\r\n" + string.Join("\r\n", Notes);
}
public sealed record Sonuc<T>(T Veri, string Durum, bool Onbellekten);
public sealed record Kayit<T>(T Veri, DateTimeOffset Tarih);

public static class Turkce
{
    public static readonly CultureInfo Kultur = CultureInfo.GetCultureInfo("tr-TR");
    public static string Esle(string metin) => metin.Normalize().ToLower(Kultur);
    public static bool Icerir(string metin, string sorgu) => Esle(metin).Contains(Esle(sorgu), StringComparison.Ordinal);
    public static string Tarih(DateTimeOffset tarih) => tarih.ToLocalTime().ToString("d MMMM yyyy HH:mm", Kultur);
    public static string DosyaAdi(string baslik)
    {
        const string yasak = "<>:\"/\\|?*";
        var ad = new string(baslik.Select(c => c < 32 || yasak.Contains(c) ? '_' : c).Take(90).ToArray()).Trim(' ', '.');
        var kok = ad.Split('.')[0].ToUpperInvariant();
        if (ad.Length == 0 || new[] { "CON", "PRN", "AUX", "NUL" }.Contains(kok) ||
            System.Text.RegularExpressions.Regex.IsMatch(kok, @"^(COM|LPT)[1-9]$")) ad = "dinimiz_islam_" + ad;
        return ad + ".pdf";
    }
}
