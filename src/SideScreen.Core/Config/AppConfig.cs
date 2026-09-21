namespace SideScreen.Core.Config;

/// <summary>
/// Configuração persistida em %AppData%\SideScreen\config.json
/// Fase 1: modelo + defaults. Fase 6: leitura/escrita real com versionamento.
/// </summary>
public sealed class AppConfig
{
    public int Version { get; set; } = 1;

    public string SelectedPlayerId { get; set; } = "potplayer";

    public bool ShuffleEnabled { get; set; } = false;

    public int SelectedMonitorIndex { get; set; } = 1;

    public bool LockPlayerToMonitor { get; set; } = false;

    public Dictionary<string, string> Shortcuts { get; set; } = new()
    {
        // Valores iniciais SEGUROS: sem hotkeys globais perigosas.
        // Fase 4 vai permitir personalizar com detecção de conflito.
        // F1/F2 sozinhos são propositalmente evitados por padrão (conflito com jogos).
        ["volumeUp"] = "Ctrl+Alt+Up",
        ["volumeDown"] = "Ctrl+Alt+Down",
        ["playPause"] = "Ctrl+Alt+P",
        ["next"] = "Ctrl+Alt+Right",
        ["previous"] = "Ctrl+Alt+Left",
    };

    public static AppConfig Default() => new();
}
