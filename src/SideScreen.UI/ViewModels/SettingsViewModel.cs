namespace SideScreen.UI.ViewModels;

public sealed class SettingsViewModel : ObservableObject
{
    public string Version => "MediaControl 1.0";
    public string Runtime => $".NET {Environment.Version} + WPF";
    public string ConfigPath => @"%AppData%\SideScreen\config.json";
    public string About => "MediaControl pilota o player no segundo monitor via SendMessage, com hotkeys que respeitam seus jogos.";
}
