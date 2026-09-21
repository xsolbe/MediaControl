namespace SideScreen.Core.Config;

/// <summary>
/// Configuração persistida em %AppData%\SideScreen\config.json
/// Fase 4: hotkeys globais opt-in + guard mode + double-press. Fase 6: store com versionamento.
/// </summary>
public sealed class AppConfig
{
    public int Version { get; set; } = 2;

    public string SelectedPlayerId { get; set; } = "potplayer";

    public bool ShuffleEnabled { get; set; } = false;

    public int SelectedMonitorIndex { get; set; } = 1;

    public bool LockPlayerToMonitor { get; set; } = false;

    /// <summary>
    /// Comandos padrão do usuário (vindos do script MusicControl/AutoHotkey):
    /// Volume+ / Volume- (teclas de mídia — seguras, não conflitam com jogos),
    /// F2 = Play/Pause, F1 = próximo, F1 x2 = anterior (double-press, compartilha o F1).
    /// F1/F2 sozinhos exibem aviso gamer na UI e respeitam o guard (pausa em fullscreen).
    /// </summary>
    public Dictionary<string, string> Shortcuts { get; set; } = new()
    {
        ["volumeUp"] = "VolumeUp",
        ["volumeDown"] = "VolumeDown",
        ["playPause"] = "F2",
        ["next"] = "F1",
        ["previous"] = "F1",
    };

    /// <summary>Globais desligadas por padrão — usuário opt-in (requisito: não interferir em jogos).</summary>
    public bool EnableGlobalHotkeys { get; set; } = false;

    /// <summary>Always | PauseWhenFullscreen (default) | OnlyWhenPlayerFocused</summary>
    public string GuardMode { get; set; } = "PauseWhenFullscreen";

    /// <summary>F1 x2 = vídeo anterior. Ligado por padrão (pedido do usuário); tem delay de ~350ms no Next.</summary>
    public bool DoublePressEnabled { get; set; } = true;

    public int DoublePressWindowMs { get; set; } = 350;

    public static AppConfig Default() => new();
}
