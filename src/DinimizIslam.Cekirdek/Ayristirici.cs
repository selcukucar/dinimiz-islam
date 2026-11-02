using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using HtmlAgilityPack;

namespace DinimizIslam.Cekirdek;

public static partial class Ayristirici
{
    public const string AnaSayfa = "https://dinimizislam.com/";
    public const string ImsakiyeAdresi = "https://m.dinimizislam.com/imsakiye.asp";
    private static readonly HashSet<string> Alanlar = ["dinimizislam.com", "www.dinimizislam.com", "m.dinimizislam.com"];

    public static Uri Adres(string adres, string temel = AnaSayfa)
    {
        if (adres.Contains('\\') || !Uri.TryCreate(new Uri(temel), adres, out var uri) ||
            uri.Scheme != "https" || !Alanlar.Contains(uri.IdnHost) || !string.IsNullOrEmpty(uri.UserInfo) || uri.Port != 443)
            throw new KaynakHatasi("Bu bağlantı izin verilen Dinimiz İslam kaynaklarından biri değil.");
        return uri;
    }
    private static HtmlDocument Belge(string html)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);
        return doc;
    }
    private static string Sinif(string ad) => $"contains(concat(' ',normalize-space(@class),' '),' {ad} ')";
    private static string Duz(string metin) => Bosluk().Replace(WebUtility.HtmlDecode(metin), " ").Trim();

    public static List<Baslik> Kategoriler(string html, string adres)
    {
        var doc = Belge(html);
        string sinif = new Uri(adres).AbsolutePath == "/" ? "link" : "menu-alt";
        var nodes = doc.DocumentNode.SelectNodes($"//a[{Sinif(sinif)} and @href]");
        var sonuc = new List<Baslik>();
        var gorulen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var node in nodes ?? Enumerable.Empty<HtmlNode>())
        {
            Uri uri;
            try { uri = Adres(WebUtility.HtmlDecode(node.GetAttributeValue("href", "")), adres); }
            catch (KaynakHatasi) { continue; }
            var query = uri.Query;
            bool makale = Regex.IsMatch(query, @"(?:[?&])aid=\d+", RegexOptions.IgnoreCase);
            bool kategori = !makale && Regex.IsMatch(query, @"(?:[?&])id=\d+", RegexOptions.IgnoreCase);
            string ad = Duz(node.InnerText);
            if ((makale || kategori) && ad.Length > 0 && gorulen.Add(uri.AbsoluteUri))
                sonuc.Add(new(ad, uri.AbsoluteUri, kategori));
        }
        if (sonuc.Count == 0) throw new KaynakHatasi("Kategori veya konu başlıkları bulunamadı. Site yapısı değişmiş olabilir; özgün sayfayı açabilirsiniz.");
        return sonuc;
    }

    public static List<string> Paragraflar(string html)
    {
        var doc = Belge(html);
        var builder = new StringBuilder();
        void Satir() { if (builder.Length > 0 && builder[^1] != '\n') builder.Append('\n'); }
        void Gez(HtmlNode node)
        {
            if (node.NodeType == HtmlNodeType.Text) { builder.Append(WebUtility.HtmlDecode(((HtmlTextNode)node).Text)); return; }
            if (node.NodeType == HtmlNodeType.Comment || new[] { "script", "style", "iframe", "object", "form", "button", "noscript" }.Contains(node.Name)) return;
            if (node.Name == "br") { builder.Append('\n'); return; }
            if (node.Name == "img")
            {
                var alt = node.GetAttributeValue("alt", "");
                if (alt.Length > 0) builder.Append("[Görsel: " + WebUtility.HtmlDecode(alt) + "]");
                return;
            }
            bool blok = new[] { "p", "div", "h1", "h2", "h3", "h4", "h5", "h6", "li", "blockquote", "tr", "pre", "ul", "ol" }.Contains(node.Name);
            if (blok) Satir();
            foreach (var child in node.ChildNodes) Gez(child);
            if (node.Name is "td" or "th") builder.Append(" | ");
            if (blok) Satir();
        }
        Gez(doc.DocumentNode);
        return builder.ToString().Replace('\u2028', '\n').Replace('\u2029', '\n').Split('\n')
            .Select(Duz).Where(p => p.Length > 0).ToList();
    }

    public static Makale Makale(string html, string adres)
    {
        var doc = Belge(html);
        var content = doc.GetElementbyId("content");
        var title = doc.DocumentNode.SelectSingleNode($"//*[{Sinif("baslik")}]") ??
            doc.DocumentNode.SelectSingleNode($"//h1[{Sinif("detay-baslik")}]");
        if (content is null || title is null) throw new KaynakHatasi("Makale metni ayrıştırılamadı. Özgün sayfayı açabilirsiniz.");
        var paragraphs = Paragraflar(content.InnerHtml);
        if (paragraphs.Sum(x => x.Length) < 30) throw new KaynakHatasi("Makale metni boş veya eksik; önbelleğe kaydedilmedi.");
        bool medya = content.SelectSingleNode(".//img|.//iframe|.//video|.//audio") is not null;
        bool tablo = content.SelectSingleNode(".//table") is not null;
        string warning = (medya ? "Görsel ve medya için özgün sayfayı açın. " : "") +
                         (tablo ? "Tablo hücreleri satır sırasıyla düz metne aktarılmıştır; özgün düzen için kaynak sayfayı açın." : "");
        return new() { Title = Duz(title.InnerText), Url = Adres(adres).AbsoluteUri, Html = content.InnerHtml,
            Text = string.Join("\n", paragraphs), Paragraphs = paragraphs, Warning = warning };
    }

    public static List<Baslik> Baglantilar(Makale makale)
    {
        var doc = Belge(makale.Html);
        var sonuc = new List<Baslik>();
        foreach (var node in doc.DocumentNode.SelectNodes("//a[@href]") ?? Enumerable.Empty<HtmlNode>())
        {
            try
            {
                var uri = Adres(WebUtility.HtmlDecode(node.GetAttributeValue("href", "")), makale.Url);
                var title = Duz(node.InnerText);
                sonuc.Add(new(title.Length > 0 ? title : uri.AbsoluteUri, uri.AbsoluteUri));
            }
            catch (KaynakHatasi) { }
        }
        return sonuc.DistinctBy(x => x.Url).ToList();
    }

    public static Imsakiye Imsakiye(string html, string adres)
    {
        var doc = Belge(html);
        var title = doc.DocumentNode.SelectSingleNode($"//h1[{Sinif("detay-baslik")}]");
        var table = doc.DocumentNode.SelectSingleNode($"//table[{Sinif("table")}]");
        if (title is null || table is null) throw new KaynakHatasi("Kaynağın imsakiye tablosu bulunamadı. Özgün sayfayı açabilirsiniz.");
        string heading = Duz(title.InnerText);
        var year = Regex.Match(heading, @"(20\d{2}) Yılı");
        var headers = (table.SelectNodes(".//thead//th") ?? Enumerable.Empty<HtmlNode>()).Select(n => Duz(n.InnerText)).ToList();
        var rows = (table.SelectNodes(".//tbody/tr") ?? Enumerable.Empty<HtmlNode>()).Select(r =>
            (r.SelectNodes("./td") ?? Enumerable.Empty<HtmlNode>()).Select(n => Duz(n.InnerText)).ToList()).Where(r => r.Count > 0).ToList();
        if (!year.Success || headers.Count != 9 || rows.Count is < 29 or > 30 ||
            rows.Any(r => r.Count != 9 || r.Skip(2).Any(t => !Saat().IsMatch(t))))
            throw new KaynakHatasi("İmsakiye eksik veya beklenmeyen biçimde. Vakit üretilmedi; özgün sayfayı kontrol edin.");
        var locations = (doc.DocumentNode.SelectNodes("//select[@id='iller']/option[@value]") ?? Enumerable.Empty<HtmlNode>())
            .Where(n => Regex.IsMatch(n.GetAttributeValue("value", ""), @"^\d+$"))
            .Select(n => new Sehir(Duz(n.InnerText), n.GetAttributeValue("value", ""))).ToList();
        var notes = (doc.DocumentNode.SelectNodes("//p") ?? Enumerable.Empty<HtmlNode>()).Select(n => Duz(n.InnerText)).Where(t =>
            t.Contains("Kadir Gecesi") || t.Contains("Bayram Namazı") || t.Contains("Tehbih")).Distinct().ToList();
        return new() { Title = heading, Url = Adres(adres).AbsoluteUri, Year = int.Parse(year.Groups[1].Value),
            Headers = headers, Rows = rows, Locations = locations, Notes = notes };
    }

    [GeneratedRegex(@"[\s\u00a0]+")]
    private static partial Regex Bosluk();
    [GeneratedRegex(@"^(?:[01]\d|2[0-3]):[0-5]\d$")]
    private static partial Regex Saat();
}
