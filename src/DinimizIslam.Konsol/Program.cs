using System.Text;
using System.Text.Json;
using DinimizIslam.Cekirdek;

Console.OutputEncoding = Encoding.UTF8;
if (args.Length == 0) { Console.WriteLine("Kullanım: DinimizIslam.Konsol canli [sonuc-klasoru] | kategori ADRES | makale ADRES"); return 0; }
var directory = args[0] == "canli" && args.Length > 1 ? Path.GetFullPath(args[1]) : Path.Combine(Path.GetTempPath(), "DinimizIslam-Konsol");
Directory.CreateDirectory(directory);
using var client = new KaynakIstemci(); var service = new IcerikServisi(client, new Depo(directory));
try
{
    if (args[0] == "canli")
    {
        var root = await service.Kategoriler(Ayristirici.AnaSayfa, true, default);
        var child = await service.Kategoriler(root.Veri[0].Url, true, default);
        var article = await service.Makale(Ayristirici.AnaSayfa + "detay.asp?Aid=4764", true, default);
        var calendar = await service.Imsakiye("", true, default);
        var report = new { Surum = "1.0.2 WPF", Tarih = DateTimeOffset.Now, AnaKategoriler = root.Veri.Count,
            AltKategoriOgeleri = child.Veri.Count, Makale = article.Veri.Title, Paragraflar = article.Veri.Paragraphs.Count,
            MakaleKarakterleri = article.Veri.Text.Length, ImsakiyeYili = calendar.Veri.Year, ImsakiyeGunleri = calendar.Veri.Rows.Count };
        var json = JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping });
        File.WriteAllText(Path.Combine(directory, "canli-sonuc.json"), json); Console.WriteLine(json);
    }
    else if (args[0] == "kategori" && args.Length > 1)
        foreach (var item in (await service.Kategoriler(args[1], false, default)).Veri) Console.WriteLine(item.OkunurAd + "\t" + item.Url);
    else if (args[0] == "makale" && args.Length > 1) Console.WriteLine((await service.Makale(args[1], false, default)).Veri.OkumaMetni);
    else { Console.WriteLine("Bilinmeyen komut. Yardım için parametresiz çalıştırın."); return 1; }
    return 0;
}
catch (Exception ex) { Console.Error.WriteLine(ex is KaynakHatasi ? ex.Message : "Denetim tamamlanamadı: " + ex.Message); return 1; }
