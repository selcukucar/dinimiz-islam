using System.Drawing;
using System.Drawing.Printing;
using System.Text;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using DinimizIslam.Cekirdek;
using Microsoft.Win32;
using Font = System.Drawing.Font;
using Brushes = System.Drawing.Brushes;
using SizeF = System.Drawing.SizeF;

namespace DinimizIslam.Arayuz;

public partial class MainWindow
{
    private (string Baslik, string Metin, string Adres) BelgeMetni()
    {
        if (Sekmeler.SelectedIndex == 1 && _makale is not null)
            return (_makale.Title, _makale.OkumaMetni + "\r\n\r\nKaynak: " + _makale.Url + "\r\nAlınma tarihi: " + Turkce.Tarih(_makale.Fetched) + "\r\nUygulama tarafından biçimlendirilmiştir.", _makale.Url);
        if (Sekmeler.SelectedIndex == 3 && _imsakiye is not null)
            return (_imsakiye.Title, _imsakiye.Metin + "\r\n\r\nKaynak: " + _imsakiye.Url + "\r\nAlınma tarihi: " + Turkce.Tarih(_imsakiye.Fetched) + "\r\nUygulama tarafından biçimlendirilmiştir.", _imsakiye.Url);
        if (Sekmeler.SelectedIndex == 2 && SonucListesi.Items.Count > 0)
            return ("Önbellek arama sonuçları", string.Join("\r\n\r\n", SonucListesi.Items.Cast<Baslik>().Select(b => b.Title + "\r\n" + b.Url)), Ayristirici.AnaSayfa);
        throw new KaynakHatasi("Önce bir makale, dolu arama sonucu veya imsakiye açın.");
    }
    private void Yazdir_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var (title, text, _) = BelgeMetni(); var dialog = new System.Windows.Controls.PrintDialog();
            if (dialog.ShowDialog() != true) return;
            var doc = new FlowDocument { FontFamily = new System.Windows.Media.FontFamily("Segoe UI"), FontSize = 15,
                Foreground = System.Windows.Media.Brushes.Black, Background = System.Windows.Media.Brushes.White,
                PagePadding = new Thickness(45), ColumnWidth = double.PositiveInfinity,
                PageWidth = dialog.PrintableAreaWidth, PageHeight = dialog.PrintableAreaHeight };
            foreach (string paragraph in text.Split('\n')) doc.Blocks.Add(new Paragraph(new Run(paragraph.TrimEnd('\r'))) { Margin = new Thickness(0, 0, 0, 7) });
            dialog.PrintDocument(((IDocumentPaginatorSource)doc).DocumentPaginator, title);
            Bildir("Belge yazıcıya gönderildi. Fiziksel çıktıyı kontrol edin.");
        }
        catch (Exception ex) { App.HataYaz(ex); Hata(ex is KaynakHatasi ? ex.Message : "Yazdırma tamamlanamadı. Yazıcıyı ve kağıt ayarlarını kontrol edin."); }
    }
    private async void Pdf_Click(object sender, RoutedEventArgs e)
    {
        (string title, string text, string _) belge;
        try { belge = BelgeMetni(); } catch (KaynakHatasi ex) { Hata(ex.Message); return; }
        var save = new SaveFileDialog { Title = "PDF kaydet", Filter = "PDF belgeleri (*.pdf)|*.pdf", AddExtension = true,
            DefaultExt = ".pdf", FileName = Turkce.DosyaAdi(belge.title), InitialDirectory = _depo.Ayar("pdf_dir", Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)) };
        if (save.ShowDialog(this) != true) return;
        if (File.Exists(save.FileName)) { Hata("Bu dosya zaten var. Var olan dosyanın üzerine yazılmaz; yeni bir ad seçin."); return; }
        await Islem("pdf", "PDF hazırlanıyor…", async ct =>
        {
            await Task.Run(() => PdfYazici.Kaydet(belge.title, belge.text, save.FileName, ct), ct);
            Bildir("PDF dosyası kaydedildi ve dosya yapısı kontrol edildi.");
        });
    }
}

/// <summary>Windows'un yerel PDF sürücüsü; üçüncü taraf PDF motoru veya Python/Qt çalıştırmaz.</summary>
public static class PdfYazici
{
    public static void Kaydet(string title, string text, string hedef, CancellationToken iptal)
    {
        const string printer = "Microsoft Print to PDF";
        if (!PrinterSettings.InstalledPrinters.Cast<string>().Contains(printer, StringComparer.OrdinalIgnoreCase))
            throw new KaynakHatasi("Microsoft Print to PDF yazıcısı bu bilgisayarda bulunamadı. Yazdır menüsünden mevcut bir PDF yazıcısı seçebilirsiniz.");
        if (File.Exists(hedef)) throw new KaynakHatasi("Dosya zaten var. Başka bir dosya adı seçin.");
        string temporary = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(hedef))!, ".dinimiz-" + Guid.NewGuid().ToString("N") + ".pdf");
        try
        {
            using var doc = new PrintDocument { DocumentName = title, PrintController = new StandardPrintController() };
            doc.PrinterSettings.PrinterName = printer; doc.PrinterSettings.PrintToFile = true; doc.PrinterSettings.PrintFileName = temporary;
            doc.DefaultPageSettings.Margins = new Margins(60, 60, 60, 60);
            var a4 = doc.PrinterSettings.PaperSizes.Cast<PaperSize>().FirstOrDefault(p => p.Kind == PaperKind.A4); if (a4 is not null) doc.DefaultPageSettings.PaperSize = a4;
            using var font = new Font("Segoe UI", 11); using var small = new Font("Segoe UI", 8);
            using var format = new StringFormat(StringFormat.GenericTypographic) { Trimming = StringTrimming.None, FormatFlags = StringFormatFlags.LineLimit };
            string remaining = text.Replace("\r\n", "\n"); int page = 0;
            doc.PrintPage += (_, args) =>
            {
                if (iptal.IsCancellationRequested) { args.Cancel = true; return; }
                var g = args.Graphics ?? throw new KaynakHatasi("PDF yazıcısının çizim alanı açılamadı.");
                var bounds = args.MarginBounds; var area = new SizeF(bounds.Width, bounds.Height - 35);
                g.MeasureString(remaining, font, area, format, out int chars, out int lines);
                if (chars <= 0) throw new KaynakHatasi("PDF sayfasına metin yerleştirilemedi.");
                g.DrawString(remaining[..chars], font, Brushes.Black, new RectangleF(bounds.X, bounds.Y, area.Width, area.Height), format);
                remaining = remaining[chars..]; page++;
                g.DrawString(page.ToString(Turkce.Kultur), small, Brushes.Black, bounds.Right - 25, bounds.Bottom - 12);
                args.HasMorePages = remaining.Length > 0;
            };
            doc.Print(); iptal.ThrowIfCancellationRequested();
            bool valid = false;
            for (int i = 0; i < 100 && !valid; i++)
            {
                iptal.ThrowIfCancellationRequested();
                try
                {
                    if (File.Exists(temporary))
                    {
                        byte[] data = File.ReadAllBytes(temporary);
                        valid = data.Length > 500 && Encoding.ASCII.GetString(data, 0, 5) == "%PDF-" && Encoding.ASCII.GetString(data, Math.Max(0, data.Length - 1024), Math.Min(1024, data.Length)).Contains("%%EOF");
                    }
                }
                catch (IOException) { }
                if (!valid) Thread.Sleep(100);
            }
            if (!valid) throw new KaynakHatasi("PDF dosyası yazılamadı veya tamamlanması doğrulanamadı. Yazıcı hizmetini ve klasör izinlerini kontrol edin.");
            File.Move(temporary, hedef, false);
        }
        finally { try { if (File.Exists(temporary)) File.Delete(temporary); } catch (IOException) { } }
    }
}
