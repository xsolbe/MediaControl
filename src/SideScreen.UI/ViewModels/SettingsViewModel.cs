namespace SideScreen.UI.ViewModels;

public sealed class SettingsViewModel : ObservableObject
{
    public string Version => "0.2.0 — Fase 2";
    public string Runtime => $".NET {Environment.Version} + WPF";
    public string ConfigPath => @"%AppData%\SideScreen\config.json (real na Fase 6)";
    public string About => "SideScreen controla o player no segundo monitor via SendMessage, sem hotkeys globais perigosas.";
}
