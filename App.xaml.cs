using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Security.Principal;
using System.Threading;
using System.Windows;
#if DEBUG
using System.Windows.Media;
using System.Windows.Media.Imaging;
#endif
using MediaControl.Interop;
using MediaControl.Services;

namespace MediaControl;

public partial class App : Application
{
    private Mutex? _singleInstance;
    private bool _ownsMutex;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

#if DEBUG
        if (e.Args.Length >= 2 && e.Args[0] == "--render-window-picker")
        {
            RenderWindowPicker(e.Args[1]);
            Shutdown();
            return;
        }
        if (e.Args.Length >= 2 && e.Args[0] == "--render-preview")
        {
            RenderPreview(e.Args[1], e.Args.Length >= 3 ? e.Args[2] : "reproducao",
                e.Args.Length >= 4 ? e.Args[3] : "en",
                e.Args.Length >= 5 && e.Args[4] == "paused");
            Shutdown();
            return;
        }
#endif

        if (!IsAdministrator())
        {
            RestartAsAdministrator();
            Shutdown();
            return;
        }

        _singleInstance = new Mutex(true, @"Local\MediaControl.SingleInstance", out bool firstInstance);
        if (!firstInstance)
        {
            NativeMethods.ActivateExistingWindow();
            Shutdown();
            return;
        }

        _ownsMutex = true;
        var window = new MainWindow(preview: false);
        MainWindow = window;
        window.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        if (_ownsMutex)
            _singleInstance?.ReleaseMutex();
        _singleInstance?.Dispose();
        base.OnExit(e);
    }

    private static bool IsAdministrator()
    {
        using var identity = WindowsIdentity.GetCurrent();
        return new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator);
    }

    private static void RestartAsAdministrator()
    {
        string? executable = Environment.ProcessPath;
        if (string.IsNullOrEmpty(executable))
            return;

        try
        {
            Process.Start(new ProcessStartInfo(executable)
            {
                UseShellExecute = true,
                Verb = "runas",
            });
        }
        catch (Win32Exception)
        {
            // O usuário cancelou o UAC. O processo sem elevação é encerrado.
        }
    }

#if DEBUG
    private static void RenderWindowPicker(string outputPath)
    {
        var picker = new OpenWindowPicker(
        [
            new WindowCandidate("AION 2", @"C:\Games\AION2.exe"),
            new WindowCandidate("Discord", @"C:\Apps\Discord.exe"),
            new WindowCandidate("MusicControl - PotPlayer", @"C:\Apps\PotPlayerMini64.exe"),
            new WindowCandidate("Browser with a long window title to show how names are shortened", @"C:\Apps\Browser.exe"),
        ]);
        if (picker.Content is not FrameworkElement content)
            return;
        const int width = 470;
        const int height = 420;
        content.Measure(new Size(width, height));
        content.Arrange(new Rect(0, 0, width, height));
        content.UpdateLayout();
        var image = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
        image.Render(content);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(image));
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outputPath))!);
        using var stream = File.Create(outputPath);
        encoder.Save(stream);
        picker.Close();
    }

    private static void RenderPreview(string outputPath, string page, string language, bool paused)
    {
        var window = new MainWindow(preview: true);
        window.SetPreviewLanguage(language);
        window.SetPreviewPaused(paused);
        window.ShowPreviewPage(page);
        if (window.Content is not FrameworkElement content)
            return;

        const int width = 470;
        const int height = 254;
        content.Measure(new Size(width, height));
        content.Arrange(new Rect(0, 0, width, height));
        content.UpdateLayout();

        var image = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
        image.Render(content);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(image));
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outputPath))!);
        using var stream = File.Create(outputPath);
        encoder.Save(stream);
        window.Close();
    }
#endif
}
