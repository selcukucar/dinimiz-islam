using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using DinimizIslam.Cekirdek;

namespace DinimizIslam.Arayuz;

public partial class App : Application
{
    private Mutex? _mutex;
    private EventWaitHandle? _show;
    public static string VeriKlasoru => Environment.GetEnvironmentVariable("DI_TEST_DATA") ??
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DinimizIslamOkuyucu");
    protected override async void OnStartup(StartupEventArgs e)
    {
        CultureInfo.DefaultThreadCurrentCulture = Turkce.Kultur;
        CultureInfo.DefaultThreadCurrentUICulture = Turkce.Kultur;
        base.OnStartup(e);
        if (e.Args.Length == 3 && e.Args[0] == "--verify-navigation")
        {
            ShutdownMode = ShutdownMode.OnExplicitShutdown;
            int result = await DinimizIslam.Arayuz.MainWindow.NavigasyonuDogrula(Path.GetFullPath(e.Args[1]), Path.GetFullPath(e.Args[2]));
            Shutdown(result); return;
        }
        if (e.Args.Length == 2 && e.Args[0] == "--verify-package")
        {
            ShutdownMode = ShutdownMode.OnExplicitShutdown;
            int result = await PaketDogrulama.Calistir(Path.GetFullPath(e.Args[1])); Shutdown(result); return;
        }
        if (e.Args.Contains("--smoke-test"))
        {
            var window = new MainWindow(); MainWindow = window; window.Show();
            var timer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
            timer.Tick += (_, _) => { timer.Stop(); Shutdown(); }; timer.Start(); return;
        }
        _mutex = new Mutex(true, @"Local\DinimizIslam-Wpf-1", out bool first);
        _show = new EventWaitHandle(false, EventResetMode.AutoReset, @"Local\DinimizIslam-Wpf-goster");
        if (!first) { _show.Set(); Shutdown(); return; }
        DispatcherUnhandledException += (_, args) =>
        {
            HataYaz(args.Exception); args.Handled = true;
            MessageBox.Show("İşlem tamamlanamadı. Yardım menüsünden günlük klasörünü açabilirsiniz.", "Dinimiz İslam", MessageBoxButton.OK, MessageBoxImage.Warning);
        };
        try { MainWindow = new MainWindow(); MainWindow.Show(); }
        catch (Exception ex) { HataYaz(ex); MessageBox.Show("Uygulama başlatılamadı. Yerel veri klasörünün izinlerini ve disk alanını kontrol edin.", "Dinimiz İslam"); Shutdown(1); return; }
        new Thread(() => { while (_show.WaitOne()) Dispatcher.BeginInvoke(() => { MainWindow.Show(); MainWindow.WindowState = WindowState.Normal; MainWindow.Activate(); }); }) { IsBackground = true }.Start();
        SystemParameters.StaticPropertyChanged += (_, args) => { if (args.PropertyName == nameof(SystemParameters.HighContrast)) Dispatcher.BeginInvoke(YuksekKarsitlik); };
        YuksekKarsitlik();
        EventManager.RegisterClassHandler(typeof(MenuItem), Keyboard.PreviewKeyDownEvent, new KeyEventHandler((sender, args) =>
        {
            if (args.Key != Key.Space || sender is not MenuItem item || args.OriginalSource != item) return;
            var source = PresentationSource.FromVisual(item); if (source is null) return;
            args.Handled = true;
            item.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice, source, Environment.TickCount, Key.Enter) { RoutedEvent = Keyboard.KeyDownEvent });
        }));
    }
    public static void HataYaz(Exception ex)
    {
        try
        {
            Directory.CreateDirectory(VeriKlasoru); var path = Path.Combine(VeriKlasoru, "wpf-hata.log");
            if (File.Exists(path) && new FileInfo(path).Length > 1_000_000) File.Move(path, path + ".1", true);
            File.AppendAllText(path, DateTimeOffset.Now.ToString("O") + " " + ex + Environment.NewLine);
        }
        catch (IOException) { }
    }
    private void YuksekKarsitlik()
    {
        var colors = new Dictionary<string, Brush>
        {
            ["TextFillColorPrimaryBrush"] = SystemColors.WindowTextBrush,
            ["CardBackgroundFillColorDefaultBrush"] = SystemColors.WindowBrush,
            ["SolidBackgroundFillColorBaseBrush"] = SystemColors.WindowBrush,
            ["CardStrokeColorDefaultBrush"] = SystemColors.WindowTextBrush,
            ["AccentFillColorDefaultBrush"] = SystemColors.HighlightBrush,
            ["TextOnAccentFillColorPrimaryBrush"] = SystemColors.HighlightTextBrush,
            ["AccentFillColorSelectedTextBackgroundBrush"] = SystemColors.HighlightBrush,
            ["AccentTextFillColorPrimaryBrush"] = SystemColors.HotTrackBrush
        };
        foreach (var (key, brush) in colors) { if (SystemParameters.HighContrast) Resources[key] = brush; else Resources.Remove(key); }
    }
    protected override void OnExit(ExitEventArgs e) { _mutex?.Dispose(); base.OnExit(e); }
}
