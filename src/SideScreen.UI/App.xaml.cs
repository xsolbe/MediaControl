using System.IO;
using System.Windows;
using System.Windows.Threading;
using SideScreen.Infrastructure.Config;
using SideScreen.UI.Localization;

namespace SideScreen.UI;

/// <summary>
/// Interaction logic for App.xaml. Crashes viram crash.log em vez de morte silenciosa.
/// </summary>
public partial class App : Application
{
    public App()
    {
        DispatcherUnhandledException += OnCrash;
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        // Tema e idioma salvos antes da primeira janela (sem flash de tema errado).
        try
        {
            var cfg = new JsonSettingsStore().Load();
            ThemeService.Apply(cfg.Appearance);
            LanguageService.Apply(cfg.Language);
        }
        catch { }
        base.OnStartup(e);
    }

    private static void OnCrash(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        try
        {
            var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SideScreen");
            Directory.CreateDirectory(dir);
            File.AppendAllText(Path.Combine(dir, "crash.log"),
                $"[{DateTime.Now:O}]{Environment.NewLine}{e.Exception}{Environment.NewLine}{Environment.NewLine}");
        }
        catch { }
        MessageBox.Show("MediaControl encontrou um erro e será fechado.\nDetalhes em %AppData%\\SideScreen\\crash.log",
            "MediaControl", MessageBoxButton.OK, MessageBoxImage.Error);
        e.Handled = false;
    }
}
