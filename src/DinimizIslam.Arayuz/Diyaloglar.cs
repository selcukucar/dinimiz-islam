using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using DinimizIslam.Cekirdek;
using Microsoft.Win32;

namespace DinimizIslam.Arayuz;

public static class Diyaloglar
{
    private static Window Pencere(Window owner, string title, double width = 720, double height = 540) => new()
    {
        Owner = owner, Title = title, Width = width, Height = height, MinWidth = Math.Min(width, 420),
        MinHeight = Math.Min(height, 260), WindowStartupLocation = WindowStartupLocation.CenterOwner,
        ShowInTaskbar = false, FontFamily = owner.FontFamily, FontSize = owner.FontSize
    };
    public static void Metin(Window owner, string title, string text)
    {
        var w = Pencere(owner, title); var panel = new DockPanel { Margin = new Thickness(20) };
        var close = new Button { Content = "Kapat", IsCancel = true, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 12, 0, 0), MinWidth = 100 };
        close.Click += (_, _) => w.Close(); DockPanel.SetDock(close, Dock.Bottom); panel.Children.Add(close);
        var box = new TextBox { Text = text, Style = (Style)Application.Current.FindResource("Okuma"), FontSize = owner.FontSize + 3 };
        AutomationProperties.SetName(box, title); panel.Children.Add(box); w.Content = panel;
        w.Loaded += (_, _) => { box.CaretIndex = 0; box.ScrollToHome(); box.Focus(); }; w.ShowDialog();
    }
    public static string? Sor(Window owner, string title, string prompt, string initial = "")
    {
        var w = Pencere(owner, title, 650, 260); var panel = new StackPanel { Margin = new Thickness(22) };
        var input = new TextBox { Text = initial, Padding = new Thickness(8), Margin = new Thickness(0, 6, 0, 16) };
        AutomationProperties.SetName(input, prompt); panel.Children.Add(new Label { Content = prompt, Target = input }); panel.Children.Add(input);
        var row = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
        var ok = new Button { Content = "Tamam", IsDefault = true, MinWidth = 100 }; ok.Click += (_, _) => w.DialogResult = true;
        row.Children.Add(ok); row.Children.Add(new Button { Content = "Vazgeç", IsCancel = true, MinWidth = 100 }); panel.Children.Add(row); w.Content = panel;
        w.Loaded += (_, _) => { input.Focus(); input.SelectAll(); };
        return w.ShowDialog() == true ? input.Text : null;
    }
    public static bool Onay(Window owner, string title, string text)
    {
        var w = Pencere(owner, title, 600, 300); var panel = new DockPanel { Margin = new Thickness(22) };
        var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
        var yes = new Button { Content = "Sil", MinWidth = 100 }; yes.Click += (_, _) => w.DialogResult = true;
        var cancel = new Button { Content = "Vazgeç", IsCancel = true, MinWidth = 100 };
        buttons.Children.Add(yes); buttons.Children.Add(cancel); DockPanel.SetDock(buttons, Dock.Bottom); panel.Children.Add(buttons);
        var box = new TextBox { Text = text, Style = (Style)Application.Current.FindResource("Okuma") }; AutomationProperties.SetName(box, "Silme onayı"); panel.Children.Add(box);
        w.Content = panel; w.Loaded += (_, _) => cancel.Focus(); return w.ShowDialog() == true;
    }
    public static Baslik? BaslikSec(Window owner, string title, List<Baslik> values)
    {
        var w = Pencere(owner, title); var panel = new DockPanel { Margin = new Thickness(20) }; Baslik? selected = null;
        var list = new ListBox { ItemsSource = values, DisplayMemberPath = "Title" }; AutomationProperties.SetName(list, title);
        void Ac() { if (list.SelectedItem is Baslik b) { selected = b; w.DialogResult = true; } }
        list.PreviewKeyDown += (_, e) => { if (e.Key == Key.Enter) { e.Handled = true; Ac(); } };
        list.MouseDoubleClick += (_, _) => Ac();
        var row = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 12, 0, 0) };
        var open = new Button { Content = "Aç" }; open.Click += (_, _) => Ac(); row.Children.Add(open); row.Children.Add(new Button { Content = "Kapat", IsCancel = true });
        DockPanel.SetDock(row, Dock.Bottom); panel.Children.Add(row); panel.Children.Add(list); w.Content = panel;
        w.Loaded += (_, _) => { list.SelectedIndex = 0; list.UpdateLayout(); if (list.ItemContainerGenerator.ContainerFromIndex(0) is ListBoxItem item) item.Focus(); else list.Focus(); };
        w.ShowDialog(); return selected;
    }
    private sealed record Secenek(string Ad, string Deger);
    public static void Ayarlar(Window owner, Depo depo, Action<bool> temizle)
    {
        var w = Pencere(owner, "Ayarlar", 620, 680);
        var root = new DockPanel { Margin = new Thickness(22) };
        var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
        var save = new Button { Content = "Kaydet", IsDefault = true, MinWidth = 100 };
        buttons.Children.Add(save); buttons.Children.Add(new Button { Content = "Vazgeç", IsCancel = true, MinWidth = 100 });
        DockPanel.SetDock(buttons, Dock.Bottom); root.Children.Add(buttons);
        var panel = new StackPanel(); root.Children.Add(new ScrollViewer { Content = panel, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Focusable = false });
        void Ekle(string label, Control control) { AutomationProperties.SetName(control, label); panel.Children.Add(new Label { Content = label, Target = control }); control.Margin = new Thickness(0, 0, 0, 12); panel.Children.Add(control); }
        var theme = new ComboBox { ItemsSource = new[] { new Secenek("Sistemi izle", "system"), new Secenek("Açık", "light"), new Secenek("Koyu", "dark") }, DisplayMemberPath = "Ad", SelectedValuePath = "Deger", SelectedValue = depo.Ayar("theme", "system") };
        Ekle("Görünüm teması", theme);
        var size = new ComboBox { ItemsSource = Enumerable.Range(7, 10).Select(i => (double)i * 2).ToList(), SelectedItem = depo.Ayar("font_size_wpf", 16d) }; Ekle("Yazı boyutu", size);
        var hours = new ComboBox { ItemsSource = new[] { 1, 6, 12, 24, 72, 168, 720 }, SelectedItem = depo.Ayar("cache_hours", 24) }; Ekle("Önbellek süresi, saat", hours);
        var directory = new TextBox { Text = depo.Ayar("pdf_dir", Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)), Padding = new Thickness(8) }; Ekle("PDF klasörü", directory);
        var choose = new Button { Content = "PDF klasörü seç", HorizontalAlignment = HorizontalAlignment.Left };
        choose.Click += (_, _) => { var dialog = new OpenFolderDialog { Title = "PDF klasörü seç" }; if (dialog.ShowDialog(w) == true) directory.Text = dialog.FolderName; }; panel.Children.Add(choose);
        var note = new TextBlock { Text = "Yüksek karşıtlıkta Windows sistem renkleri kullanılır. Veriler yalnız bu bilgisayarda saklanır. Ağ yenilemesi kullanıcı komutlarıyla yapılır.", TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 12, 0, 12) }; panel.Children.Add(note);
        var clear = new Button { Content = "Önbelleği temizle", HorizontalAlignment = HorizontalAlignment.Left }; clear.Click += (_, _) => temizle(false); panel.Children.Add(clear);
        var history = new Button { Content = "Okuma geçmişini temizle", HorizontalAlignment = HorizontalAlignment.Left }; history.Click += (_, _) => temizle(true); panel.Children.Add(history);
        var defaults = new Button { Content = "Varsayılanları geri yükle", HorizontalAlignment = HorizontalAlignment.Left };
        defaults.Click += (_, _) => { theme.SelectedValue = "system"; size.SelectedItem = 16d; hours.SelectedItem = 24; directory.Text = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments); }; panel.Children.Add(defaults);
        save.Click += (_, _) =>
        {
            if (!Directory.Exists(directory.Text)) { Metin(w, "Ayar kaydedilemedi", "PDF klasörü mevcut bir klasör olmalıdır."); return; }
            depo.AyarYaz("theme", theme.SelectedValue as string ?? "system"); depo.AyarYaz("font_size_wpf", size.SelectedItem is double value ? value : 16d);
            depo.AyarYaz("cache_hours", hours.SelectedItem is int cache ? cache : 24); depo.AyarYaz("pdf_dir", directory.Text); w.DialogResult = true;
        };
        w.Content = root; w.Loaded += (_, _) => theme.Focus(); w.ShowDialog();
    }
}
