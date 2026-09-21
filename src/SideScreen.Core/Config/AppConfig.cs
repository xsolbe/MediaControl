namespace SideScreen.Core.Config;

/// <summary>
/// Configuração persistida em %AppData%\SideScreen\config.json
/// Fase 4: hotkeys globais opt-in + guard mode + double-press. Fase 6: store com versionamento.
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
        // F1/F2 sozinhos são propositalmente evitados por padrão (conflito com jogos).
        ["volumeUp"] = "Ctrl+Alt+Up",
        ["volumeDown"] = "Ctrl+Alt+Down",
        ["playPause"] = "Ctrl+Alt+P",
        ["next"] = "Ctrl+Alt+Right",
        ["previous"] = "Ctrl+Alt+Left",
    };

    /// <summary>Globais desligadas por padrão — usuário opt-in (requisito: não interferir em jogos).</summary>
    public bool EnableGlobalHotkeys { get; set; } = false;

    /// <summary>Always | PauseWhenFullscreen (default) | OnlyWhenPlayerFocused</summary>
    public string GuardMode { get; set; } = "PauseWhenFullscreen";

    /// <summary>Experimental, desligado por padrão (introduz delay no Next). Ver docs/hotkeys.md.</summary>
    public bool DoublePressEnabled { get; set; } = false;

    public int DoublePressWindowMs { get; set; } = 350;

    public static AppConfig Default() => new();
}
