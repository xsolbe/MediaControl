using System.Windows;
using SideScreen.Infrastructure.Players;

namespace SideScreen.UI;

/// <summary>
/// Fase 1: prova que UI -> Core (IPlayerController) -> Infrastructure (PotPlayerController) compila e roda.
/// Fase 2: trocar por MVVM real (MainViewModel).
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        var player = new PotPlayerController();
        var status = player.GetStatus();
        StatusText.Text = $"IsRunning={status.IsRunning} — {player.DisplayName} ({player.Id}) — Fase 3 vai detectar via FindWindow.";
        FooterText.Text = $"SideScreen 0.1.0 · .NET {Environment.Version} + WPF · Player: {player.DisplayName}";
    }
}
