namespace SideScreen.Core.Config;

/// <summary>
/// Configuração persistida em %AppData%\SideScreen\config.json
/// Fase 4: hotkeys globais opt-in + guard mode + double-press. Fase 6: store com versionamento.
/// </summary>
public sealed class AppConfig
{
    public int Version { get; set; } = 4;

    public string SelectedPlayerId { get; set; } = "potplayer";

    public bool ShuffleEnabled { get; set; } = false;

    public int SelectedMonitorIndex { get; set; } = 1;

    public bool LockPlayerToMonitor { get; set; } = false;

    /// <summary>
    /// Comandos do usuário (script MusicControl/AutoHotkey, confirmado via strings do .exe):
    /// NumpadAdd/NumpadSub = volume, F2 = Play/Pause, F1 = próximo, F1 x2 = anterior,
    /// seta → = +5s, seta ← = -5s.
    /// F1/F2/setas sozinhas exibem aviso gamer na UI e respeitam o guard.
    /// </summary>
    public Dictionary<string, string> Shortcuts { get; set; } = new()
    {
        ["volumeUp"] = "NumpadAdd",
        ["volumeDown"] = "NumpadSub",
        ["playPause"] = "F2",
        ["next"] = "F1",
        ["previous"] = "F1",
        ["seekForward"] = "Right",
        ["seekBackward"] = "Left",
    };

    /// <summary>Globais desligadas por padrão — usuário opt-in (requisito: não interferir em jogos).</summary>
    public bool EnableGlobalHotkeys { get; set; } = false;

    /// <summary>
    /// Always | PauseWhenFullscreen | OnlyWhenPlayerFocused.
    /// Default Always: o app existe para controlar o vídeo ENQUANTO joga (uso confirmado no BNSR).
    /// Quem preferir segurança total pode trocar para PauseWhenFullscreen.
    /// </summary>
    public string GuardMode { get; set; } = "Always";

    /// <summary>F1 x2 = vídeo anterior. Ligado por padrão (pedido do usuário); tem delay de ~350ms no Next.</summary>
    public bool DoublePressEnabled { get; set; } = true;

    public int DoublePressWindowMs { get; set; } = 350;

    public static AppConfig Default() => new();
}
