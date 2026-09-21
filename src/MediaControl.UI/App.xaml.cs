using System.IO;
using System.Windows;
using System.Windows.Threading;
using MediaControl.Core.Storage;
using MediaControl.Infrastructure.Config;
using MediaControl.UI.Localization;

namespace MediaControl.UI;

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
            Directory.CreateDirectory(AppPaths.DataDir);
            File.AppendAllText(AppPaths.CrashPath,
                $"[{DateTime.Now:O}]{Environment.NewLine}{e.Exception}{Environment.NewLine}{Environment.NewLine}");
        }
        catch { }
        MessageBox.Show("MediaControl encontrou um erro e será fechado.\nDetalhes em %AppData%\\MediaControl\\crash.log",
            "MediaControl", MessageBoxButton.OK, MessageBoxImage.Error);
        e.Handled = false;
    }
}
