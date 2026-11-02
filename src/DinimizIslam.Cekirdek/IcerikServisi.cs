namespace DinimizIslam.Cekirdek;

public sealed class IcerikServisi(IKaynakIstemci istemci, Depo depo)
{
    private async Task<Sonuc<T>> Getir<T>(string adres, string key, string kind, Func<string, string, T> parser,
        bool yenile, CancellationToken iptal)
    {
        var kayit = await Task.Run(() => depo.Oku<T>(key), iptal).ConfigureAwait(false);
        if (!yenile && kayit is not null && DateTimeOffset.UtcNow - kayit.Tarih < TimeSpan.FromHours(depo.Ayar("cache_hours", 24)))
            return new(kayit.Veri, "Önbellekten. Kayıt: " + Turkce.Tarih(kayit.Tarih), true);
        try
        {
            string html = await istemci.AlAsync(adres, iptal).ConfigureAwait(false);
            var data = await Task.Run(() => parser(html, adres), iptal).ConfigureAwait(false);
            iptal.ThrowIfCancellationRequested();
            await Task.Run(() => depo.Yaz(key, kind, data), iptal).ConfigureAwait(false);
            return new(data, "Kaynaktan yeni alındı.", false);
        }
        catch (KaynakHatasi) when (kayit is not null && !iptal.IsCancellationRequested)
        { return new(kayit.Veri, "Kaynağa erişilemedi. Önbellekten; güncelliği doğrulanmadı. Kayıt: " + Turkce.Tarih(kayit.Tarih), true); }
    }
    public Task<Sonuc<List<Baslik>>> Kategoriler(string adres, bool yenile, CancellationToken iptal) =>
        Getir(adres, adres + "#wpf-kategoriler-1", "categories", Ayristirici.Kategoriler, yenile, iptal);
    public async Task<Sonuc<Makale>> Makale(string adres, bool yenile, CancellationToken iptal)
    {
        var sonuc = await Getir(adres, adres, "article", Ayristirici.Makale, yenile, iptal).ConfigureAwait(false);
        if (sonuc.Veri.Paragraphs.Count == 0)
        {
            var m = sonuc.Veri with { Paragraphs = Ayristirici.Paragraflar(sonuc.Veri.Html) };
            sonuc = sonuc with { Veri = m };
        }
        return sonuc;
    }
    public Task<Sonuc<Imsakiye>> Imsakiye(string sehir, bool yenile, CancellationToken iptal)
    {
        string adres = Ayristirici.ImsakiyeAdresi + (sehir.Length > 0 ? "?il=" + Uri.EscapeDataString(sehir) : "");
        return Getir(adres, adres + "#wpf-imsakiye-1", "calendar", Ayristirici.Imsakiye, yenile, iptal);
    }
    public Task<List<Baslik>> Ara(string sorgu, CancellationToken iptal) => Task.Run(() => depo.Ara(sorgu), iptal);

    public async Task<AramaSonucu> BasliklardaAra(string sorgu, bool yenile, IProgress<string>? ilerleme, CancellationToken iptal)
    {
        const string key = "wpf-baslik-dizini-1";
        var kayit = await Task.Run(() => depo.Oku<List<Baslik>>(key), iptal).ConfigureAwait(false);
        var tumu = new Dictionary<string, Baslik>(StringComparer.OrdinalIgnoreCase);
        foreach (var b in kayit?.Veri ?? []) tumu.TryAdd(b.Url, b);
        string durum = "Kaydedilmiş site başlıklarında ve yerel makale metinlerinde arandı.";
        if (yenile || kayit is null)
        {
            var bekleyen = new Queue<string>(); bekleyen.Enqueue(Ayristirici.AnaSayfa);
            var gorulen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            int eksik = 0;
            while (bekleyen.TryDequeue(out var url))
            {
                iptal.ThrowIfCancellationRequested();
                if (!gorulen.Add(url)) continue;
                if (gorulen.Count > 1000) { eksik += bekleyen.Count + 1; break; }
                ilerleme?.Report($"Site başlıkları alınıyor: {gorulen.Count} kategori, {tumu.Count} başlık. Escape ile iptal edebilirsiniz.");
                try
                {
                    var sonuc = await Kategoriler(url, yenile, iptal).ConfigureAwait(false);
                    foreach (var b in sonuc.Veri)
                    {
                        tumu[b.Url] = b;
                        if (b.Category && !gorulen.Contains(b.Url)) bekleyen.Enqueue(b.Url);
                    }
                }
                catch (KaynakHatasi) { eksik++; }
            }
            iptal.ThrowIfCancellationRequested();
            // Eksik tarama tamamlanmış dizin olarak saklanmaz; sonraki Enter yeniden deneyebilir.
            if (eksik == 0) await Task.Run(() => depo.Yaz(key, "search-index", tumu.Values.ToList()), iptal).ConfigureAwait(false);
            durum = eksik == 0 ? $"{tumu.Count} site başlığında ve yerel makale metinlerinde arandı."
                : $"{eksik} kategori alınamadı; sonuçlar eksik olabilir. Kaydedilmiş başlıklar ve yerel metinler arandı. Yeniden denemek için Enter kullanın.";
        }
        var kelimeler = sorgu.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        var sonucListesi = tumu.Values.Where(b => kelimeler.All(k => Turkce.Icerir(b.Title, k))).ToList();
        var adresler = sonucListesi.Select(b => b.Url).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var b in await Ara(sorgu, iptal).ConfigureAwait(false)) if (adresler.Add(b.Url)) sonucListesi.Add(b);
        return new(sonucListesi, durum);
    }
}

public sealed record AramaSonucu(List<Baslik> Basliklar, string Durum);
