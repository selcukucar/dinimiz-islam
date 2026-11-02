using System.Net;
using System.Text;
using System.Text.Json;
using DinimizIslam.Cekirdek;
using Microsoft.Data.Sqlite;
using Xunit;

namespace DinimizIslam.Testler;

public sealed class CekirdekTestleri
{
    private static string Fixture(string ad) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "fixtures", ad + ".html"));
    private static Depo YeniDepo() => new(Path.Combine(Path.GetTempPath(), "DinimizIslam-Test", Guid.NewGuid().ToString("N")));

    [Theory]
    [InlineData("file:///C:/secret")][InlineData("http://dinimizislam.com/")]
    [InlineData("https://evil.test/")][InlineData("https://dinimizislam.com.evil.test/")]
    [InlineData("https://user:pw@dinimizislam.com/")][InlineData("https://dinimizislam.com:9000/")]
    [InlineData("javascript:alert(1)")]
    public void IzinVerilmeyenAdreslerReddedilir(string adres) => Assert.Throws<KaynakHatasi>(() => Ayristirici.Adres(adres));
    [Theory]
    [InlineData("İMAN", "iman")][InlineData("IŞIK", "ışık")][InlineData("ŞÜKÜR", "şükür")]
    public void TurkceEsleme(string metin, string beklenen) => Assert.Equal(beklenen, Turkce.Esle(metin));
    [Fact] public void NoktaliVeNoktasizAyri() => Assert.NotEqual(Turkce.Esle("I"), Turkce.Esle("i"));
    [Fact]
    public void AnaKategoriKaynakSirasi()
    {
        var basliklar = Ayristirici.Kategoriler(Fixture("home"), Ayristirici.AnaSayfa);
        Assert.Equal(33, basliklar.Count); Assert.Equal("Doğru iman bilgileri", basliklar[0].Title);
        Assert.DoesNotContain(basliklar, b => b.Title == "Mobil");
        Assert.All(basliklar, b => Assert.True(b.Category));
    }
    [Fact]
    public void AltKategorilerVeMakaleler()
    {
        var list = Ayristirici.Kategoriler(Fixture("category"), Ayristirici.AnaSayfa + "detay.asp?id=1");
        Assert.Equal(17, list.Count); Assert.Contains(list, x => x.Title == "Kadere iman" && !x.Category);
        Assert.Contains(list, x => x.Title == "İman ve İslam" && x.Category);
    }
    [Fact]
    public void GercekMakaleParagraflariKorunur()
    {
        var m = Ayristirici.Makale(Fixture("article"), Ayristirici.AnaSayfa + "detay.asp?Aid=4764");
        Assert.Equal("Meal ve tefsir okumak hakkında", m.Title);
        Assert.True(m.Paragraphs.Count > 50); Assert.True(m.Text.Length > 30000);
        Assert.StartsWith("Sual:", m.Paragraphs[0]); Assert.DoesNotContain("Güncelleme Tarihi", m.Text);
        Assert.Contains("\r\n", m.OkumaMetni); Assert.DoesNotContain("<script", m.OkumaMetni);
    }
    [Fact]
    public void SatirlarVeInlineBicimlerBirbirineKarismaz()
    {
        var p = Ayristirici.Paragraflar("<p>Bir <b>kalın</b> kelime.</p><p>İkinci<br>Üçüncü</p><script>bad()</script><p>بِسْمِ اللَّهِ</p>");
        Assert.Equal(new[] { "Bir kalın kelime.", "İkinci", "Üçüncü", "بِسْمِ اللَّهِ" }, p);
    }
    [Fact]
    public void TablonunButunHucreleriKorunur()
    {
        var p = Ayristirici.Paragraflar("<table><tr><th>Ad</th><th>Değer</th></tr><tr><td>Türkçe</td><td>İslâm</td></tr></table>");
        Assert.Equal(2, p.Count); Assert.Contains("Ad | Değer", p[0]); Assert.Contains("Türkçe | İslâm", p[1]);
    }
    [Fact]
    public void ImsakiyeKaynaktakiVakitleriKullanir()
    {
        var c = Ayristirici.Imsakiye(Fixture("imsakiye"), Ayristirici.ImsakiyeAdresi);
        Assert.Equal(2026, c.Year); Assert.Equal(29, c.Rows.Count); Assert.Equal(9, c.Headers.Count);
        Assert.Equal("06:05", c.Rows[0][2]); Assert.Contains(c.Locations, x => x.Ad == "Ankara" && x.Kod == "16709");
        Assert.Contains(c.Notes, x => x.Contains("Tehbih"));
        Assert.Throws<KaynakHatasi>(() => Ayristirici.Imsakiye(Fixture("imsakiye").Replace("06:05", "25:99"), Ayristirici.ImsakiyeAdresi));
    }
    [Fact]
    public void DegisenYapiReddedilir()
    {
        Assert.Throws<KaynakHatasi>(() => Ayristirici.Makale("<p>Boş</p>", Ayristirici.AnaSayfa));
        Assert.Throws<KaynakHatasi>(() => Ayristirici.Kategoriler("<p>Boş</p>", Ayristirici.AnaSayfa));
        Assert.Throws<KaynakHatasi>(() => Ayristirici.Imsakiye("<p>Boş</p>", Ayristirici.ImsakiyeAdresi));
    }
    [Theory][InlineData("CON")][InlineData("COM1")][InlineData("a/b:c?d*")][InlineData("")][InlineData("İman ve İslâm")]
    public void DosyaAdlariGuvenli(string title)
    {
        var name = Turkce.DosyaAdi(title); Assert.EndsWith(".pdf", name); Assert.DoesNotContain('/', name); Assert.DoesNotContain(':', name);
        Assert.NotEqual("CON.pdf", name); Assert.NotEqual("COM1.pdf", name);
    }
    [Fact]
    public void DepoVerileriKaliciVeParametreli()
    {
        var d = YeniDepo(); var m = Ayristirici.Makale(Fixture("article"), Ayristirici.AnaSayfa + "detay.asp?Aid=4764");
        d.Yaz(m.Url, "article", m); d.Okundu(m); Assert.True(d.Isaretle(new(m.Title, m.Url))); d.AyarYaz("font_size_wpf", 22);
        var again = new Depo(d.Klasor); Assert.Equal(22, again.Ayar("font_size_wpf", 16)); Assert.Single(again.Liste()); Assert.Single(again.Liste(true));
        Assert.Single(again.Ara("KUR’AN")); Assert.Empty(again.Ara("' OR 1=1 --")); Assert.False(again.Isaretle(new(m.Title, m.Url)));
        again.Temizle(false); Assert.Null(again.Oku<Makale>(m.Url)); Assert.Single(again.Liste(true)); again.Temizle(true); Assert.Empty(again.Liste(true));
    }
    [Fact]
    public void EskiPythonKaydiOkunur()
    {
        var d = YeniDepo(); string url = Ayristirici.AnaSayfa + "detay.asp?Aid=1";
        using var db = new SqliteConnection("Data Source=" + Path.Combine(d.Klasor, "veriler.sqlite3")); db.Open();
        using var command = db.CreateCommand(); command.CommandText = "INSERT INTO cache VALUES($url,'article',$json,$date)";
        command.Parameters.AddWithValue("$url", url); command.Parameters.AddWithValue("$json", "{\"title\":\"Eski makale\",\"url\":\"" + url + "\",\"html\":\"<p>Birinci paragraf</p><p>İkinci paragraf</p>\",\"text\":\"Birinci paragraf\",\"fetched\":\"2026-11-01T10:00:00+00:00\"}");
        command.Parameters.AddWithValue("$date", DateTimeOffset.UtcNow.ToString("O")); command.ExecuteNonQuery();
        Assert.Equal("Eski makale", d.Oku<Makale>(url)!.Veri.Title);
    }
    [Fact]
    public async Task BozukYanitOnbellegiBozmaz()
    {
        var d = YeniDepo(); string url = Ayristirici.AnaSayfa + "detay.asp?Aid=4764";
        var m = Ayristirici.Makale(Fixture("article"), url); d.Yaz(url, "article", m);
        var s = new IcerikServisi(new SahteIstemci("<p>Bozuk</p>"), d);
        var result = await s.Makale(url, true, default); Assert.True(result.Onbellekten); Assert.Contains("doğrulanmadı", result.Durum); Assert.Equal(m.Text, result.Veri.Text);
    }
    [Fact]
    public async Task OnbellekSuresiVeYenileme()
    {
        var d = YeniDepo(); string url = Ayristirici.AnaSayfa + "detay.asp?Aid=4764";
        var client = new SahteIstemci(Fixture("article")); var s = new IcerikServisi(client, d);
        await s.Makale(url, false, default); await s.Makale(url, false, default); Assert.Equal(1, client.Count);
        using var db = new SqliteConnection("Data Source=" + Path.Combine(d.Klasor, "veriler.sqlite3")); db.Open(); using var cmd = db.CreateCommand(); cmd.CommandText = "UPDATE cache SET fetched='2000-01-01T00:00:00+00:00'"; cmd.ExecuteNonQuery();
        await s.Makale(url, false, default); Assert.Equal(2, client.Count);
    }
    [Fact]
    public async Task HttpRobotsVeIcerik()
    {
        var h = new SahteHttp(_ => new(HttpStatusCode.OK) { Content = new StringContent(_.AbsolutePath == "/robots.txt" ? "User-agent: *\nAllow: /" : Fixture("home"), Encoding.UTF8) });
        using var client = new KaynakIstemci(h); var html = await client.AlAsync(Ayristirici.AnaSayfa, default);
        Assert.Equal(33, Ayristirici.Kategoriler(html, Ayristirici.AnaSayfa).Count); Assert.Equal(2, h.Count);
    }
    [Theory][InlineData(403)][InlineData(404)][InlineData(429)][InlineData(500)]
    public async Task HttpHatalariTurkce(int code)
    {
        using var client = new KaynakIstemci(new SahteHttp(uri => uri.AbsolutePath == "/robots.txt" ? new(HttpStatusCode.OK) { Content = new StringContent("User-agent: *\nAllow: /") } : new((HttpStatusCode)code)));
        await Assert.ThrowsAsync<KaynakHatasi>(() => client.AlAsync(Ayristirici.AnaSayfa, default));
    }
    [Fact]
    public async Task RobotsEngeliIcerigiIndirmez()
    {
        var handler = new SahteHttp(_ => new(HttpStatusCode.OK) { Content = new StringContent("User-agent: *\nDisallow: /") });
        using var client = new KaynakIstemci(handler); await Assert.ThrowsAsync<KaynakHatasi>(() => client.AlAsync(Ayristirici.AnaSayfa, default)); Assert.Equal(1, handler.Count);
    }
    [Fact]
    public async Task DisYonlendirmeIzlenmez()
    {
        var h = new SahteHttp(uri =>
        {
            if (uri.AbsolutePath == "/robots.txt") return new(HttpStatusCode.OK) { Content = new StringContent("User-agent: *\nAllow: /") };
            var r = new HttpResponseMessage(HttpStatusCode.Redirect); r.Headers.Location = new Uri("https://evil.test/"); return r;
        });
        using var c = new KaynakIstemci(h); await Assert.ThrowsAsync<KaynakHatasi>(() => c.AlAsync(Ayristirici.AnaSayfa, default)); Assert.Equal(2, h.Count);
    }
    [Fact]
    public async Task IptalYeniKayitOlusturmaz()
    {
        var d = YeniDepo(); using var cts = new CancellationTokenSource(); cts.Cancel();
        var s = new IcerikServisi(new SahteIstemci(Fixture("article")), d);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => s.Makale(Ayristirici.AnaSayfa, false, cts.Token)); Assert.Null(d.Oku<Makale>(Ayristirici.AnaSayfa));
    }
    [Fact]
    public async Task BosOnbellekteSiteBasliklariAranirVeDizinTekrarKullanilir()
    {
        var depo = YeniDepo(); var client = new DizinIstemci(); var servis = new IcerikServisi(client, depo);
        var ilk = await servis.BasliklardaAra("NAMAZ", false, null, default);
        Assert.Single(ilk.Basliklar); Assert.Equal("Namaz bilgileri", ilk.Basliklar[0].Title);
        Assert.False(ilk.Basliklar[0].Category); Assert.Equal(2, client.Count);
        Assert.Empty(depo.Ara("namaz"));
        var ikinci = await servis.BasliklardaAra("namaz", false, null, default);
        Assert.Single(ikinci.Basliklar); Assert.Equal(2, client.Count);
        await servis.BasliklardaAra("namaz", true, null, default); Assert.Equal(4, client.Count);
    }
    [Fact]
    public async Task AramadaKaynakHatasiBosBasariOlarakGosterilmez()
    {
        var servis = new IcerikServisi(new DizinIstemci { Hata = true }, YeniDepo());
        var sonuc = await servis.BasliklardaAra("namaz", false, null, default);
        Assert.Empty(sonuc.Basliklar); Assert.Contains("alınamadı", sonuc.Durum);
        Assert.Contains("eksik", sonuc.Durum);
    }
    [Fact]
    public async Task IptalEdilenAramaTamamlanmisDizinYazmaz()
    {
        var depo = YeniDepo(); using var ct = new CancellationTokenSource(); ct.Cancel();
        var servis = new IcerikServisi(new DizinIstemci(), depo);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => servis.BasliklardaAra("namaz", false, null, ct.Token));
        Assert.Null(depo.Oku<List<Baslik>>("wpf-baslik-dizini-1"));
    }
    private sealed class DizinIstemci : IKaynakIstemci
    {
        public int Count; public bool Hata;
        public Task<string> AlAsync(string adres, CancellationToken iptal)
        {
            Count++; iptal.ThrowIfCancellationRequested();
            if (Hata) throw new KaynakHatasi("Kaynak yok");
            return Task.FromResult(adres == Ayristirici.AnaSayfa
                ? "<a class='link' href='detay.asp?id=1'>İbadet</a>"
                : "<a class='menu-alt' href='detay.asp?Aid=10'>Namaz bilgileri</a><a class='menu-alt' href='detay.asp?id=1'>İbadet</a>");
        }
    }
    private sealed class SahteIstemci(string html) : IKaynakIstemci
    { public int Count; public Task<string> AlAsync(string adres, CancellationToken iptal) { Count++; iptal.ThrowIfCancellationRequested(); return Task.FromResult(html); } }
    private sealed class SahteHttp(Func<Uri, HttpResponseMessage> response) : HttpMessageHandler
    {
        public int Count;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        { Count++; return Task.FromResult(response(request.RequestUri!)); }
    }
}
