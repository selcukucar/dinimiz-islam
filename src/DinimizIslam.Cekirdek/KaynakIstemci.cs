using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace DinimizIslam.Cekirdek;

public sealed class RobotsKurallari
{
    private readonly List<(bool Izin, string Yol)> _kurallar = [];
    public RobotsKurallari(string metin)
    {
        var agents = new List<string>(); bool rules = false;
        foreach (string raw in metin.Split('\n'))
        {
            string line = raw.Split('#')[0].Trim(); int colon = line.IndexOf(':');
            if (colon < 0) continue;
            string key = line[..colon].Trim().ToLowerInvariant(), value = line[(colon + 1)..].Trim();
            if (key == "user-agent")
            {
                if (rules) { agents.Clear(); rules = false; }
                agents.Add(value.ToLowerInvariant());
            }
            else if (key is "allow" or "disallow")
            {
                rules = true;
                if (value.Length > 0 && agents.Any(a => a == "*" || a == "dinimizislamwpf")) _kurallar.Add((key == "allow", value));
            }
        }
    }
    public bool Izinli(Uri uri)
    {
        int longest = -1; bool allowed = true;
        foreach (var (allow, path) in _kurallar)
        {
            string pattern = "^" + Regex.Escape(path).Replace(@"\*", ".*").Replace(@"\$", "$");
            if (!Regex.IsMatch(uri.PathAndQuery, pattern, RegexOptions.CultureInvariant)) continue;
            int size = path.Replace("*", "").Length;
            if (size > longest || size == longest && allow) { longest = size; allowed = allow; }
        }
        return allowed;
    }
}

public interface IKaynakIstemci
{
    Task<string> AlAsync(string adres, CancellationToken iptal);
}

public sealed class KaynakIstemci : IKaynakIstemci, IDisposable
{
    private readonly HttpClient _http;
    private readonly SemaphoreSlim _kilit = new(1);
    private readonly Dictionary<string, RobotsKurallari> _robots = [];
    private DateTimeOffset _son = DateTimeOffset.MinValue;
    public KaynakIstemci(HttpMessageHandler? handler = null)
    {
        _http = new HttpClient(handler ?? new HttpClientHandler { AllowAutoRedirect = false, AutomaticDecompression = DecompressionMethods.All });
        _http.Timeout = TimeSpan.FromSeconds(25);
        _http.DefaultRequestHeaders.UserAgent.ParseAdd("DinimizIslamWpf/1.0 (independent accessible desktop reader)");
    }
    public async Task<string> AlAsync(string adres, CancellationToken iptal)
    {
        var uri = Ayristirici.Adres(adres);
        await _kilit.WaitAsync(iptal).ConfigureAwait(false);
        try
        {
            if (!_robots.TryGetValue(uri.Host, out var robots))
            {
                robots = new RobotsKurallari(await IndirAsync(new Uri(uri.GetLeftPart(UriPartial.Authority) + "/robots.txt"), iptal).ConfigureAwait(false));
                _robots[uri.Host] = robots;
            }
            if (!robots.Izinli(uri)) throw new KaynakHatasi("Site robots.txt kuralları bu sayfanın alınmasına izin vermiyor.");
            return await IndirAsync(uri, iptal).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!iptal.IsCancellationRequested) { throw new KaynakHatasi("Kaynak zamanında yanıt vermedi. Yeniden deneyin veya önbelleği kullanın."); }
        catch (HttpRequestException e) { throw new KaynakHatasi("İnternet bağlantısı kurulamadı. Bağlantınızı kontrol edip yeniden deneyin.", e); }
        finally { _kilit.Release(); }
    }
    private async Task<string> IndirAsync(Uri uri, CancellationToken iptal)
    {
        string host = uri.Host;
        for (int redirect = 0; redirect < 5; redirect++)
        {
            var wait = TimeSpan.FromSeconds(1) - (DateTimeOffset.UtcNow - _son);
            if (wait > TimeSpan.Zero) await Task.Delay(wait, iptal).ConfigureAwait(false);
            _son = DateTimeOffset.UtcNow;
            using var response = await _http.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, iptal).ConfigureAwait(false);
            if ((int)response.StatusCode is >= 300 and < 400 && response.Headers.Location is not null)
            {
                uri = Ayristirici.Adres(response.Headers.Location.ToString(), uri.AbsoluteUri);
                if (uri.Host != host) throw new KaynakHatasi("Kaynak farklı sunucuya yönlendirdi. Özgün sayfayı tarayıcıda açın.");
                continue;
            }
            if (response.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.TooManyRequests)
                throw new KaynakHatasi($"Site erişimi sınırladı ({(int)response.StatusCode}). Daha sonra yeniden deneyin.");
            if (response.StatusCode == HttpStatusCode.NotFound) throw new KaynakHatasi("Kaynak sayfa bulunamadı (404). Özgün adresi kontrol edin.");
            if (!response.IsSuccessStatusCode) throw new KaynakHatasi($"Kaynak şu anda alınamadı ({(int)response.StatusCode}). Daha sonra yeniden deneyin.");
            await using var stream = await response.Content.ReadAsStreamAsync(iptal).ConfigureAwait(false);
            using var buffer = new MemoryStream(); byte[] bytes = new byte[65536]; int count;
            while ((count = await stream.ReadAsync(bytes, iptal).ConfigureAwait(false)) != 0)
            {
                if (buffer.Length + count > 6_000_000) throw new KaynakHatasi("Yanıt güvenli boyut sınırını aştı; içerik kaydedilmedi.");
                buffer.Write(bytes, 0, count);
            }
            try { return new UTF8Encoding(false, true).GetString(buffer.ToArray()).TrimStart('\uFEFF'); }
            catch (DecoderFallbackException) { throw new KaynakHatasi("Kaynak metnin UTF-8 kodlaması okunamadı; bozuk içerik kaydedilmedi."); }
        }
        throw new KaynakHatasi("Kaynak çok fazla yönlendirme yaptı.");
    }
    public void Dispose() { _http.Dispose(); _kilit.Dispose(); }
}
