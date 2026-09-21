using MediaControl.Core.Hotkeys;
using MediaControl.Infrastructure.Hotkeys;
using MediaControl.Infrastructure.Players;

namespace MediaControl.Core.Tests;

/// <summary>
/// Fase 7: HotkeyService sem tocar Win32 de verdade (gestos inválidos/não-mapeados falham
/// antes de qualquer P/Invoke) + fallback de guard desconhecido.
/// </summary>
public class Fase7Tests
{
    private static HotkeyService OfflineService(bool doublePress = false)
    {
        var player = new PotPlayerController(() => nint.Zero);
        return new HotkeyService(player) { DoublePressEnabled = doublePress };
    }

    [Fact]
    public void Start_InvalidGesture_FalseWithoutWin32()
    {
        var svc = OfflineService();
        bool ok = svc.Start(nint.Zero, new Dictionary<string, string> { ["playPause"] = "Ctrl+Alt" });
        Assert.False(ok);
        Assert.Empty(svc.Registered);
        Assert.Contains(svc.Errors, e => e.Contains("playPause"));
        svc.Dispose();
    }

    [Fact]
    public void Start_UnsupportedKey_FalseWithoutWin32()
    {
        var svc = OfflineService();
        bool ok = svc.Start(nint.Zero, new Dictionary<string, string> { ["next"] = "MediaPlay" });
        Assert.False(ok);
        Assert.Empty(svc.Registered);
        svc.Dispose();
    }

    [Fact]
    public void Start_SharedDoublePress_SkipsPreviousSilently()
    {
        // next+previous com gesto idêntico e double-press: previous nem é registrado;
        // "MediaPlay" não é mapeável, então nada chama RegisterHotKey — só o next erra.
        var svc = OfflineService(doublePress: true);
        bool ok = svc.Start(nint.Zero, new Dictionary<string, string>
        {
            ["next"] = "MediaPlay",
            ["previous"] = "MediaPlay",
        });
        Assert.False(ok);
        Assert.DoesNotContain(svc.Errors, e => e.Contains("previous"));
        Assert.Contains(svc.Errors, e => e.Contains("next"));
        svc.Dispose();
    }

    [Fact]
    public void HandleHotkey_Unstarted_ReturnsFalse()
    {
        var svc = OfflineService();
        Assert.False(svc.HandleHotkeyMessage(0xA001));
        svc.Dispose();
    }

    [Fact]
    public void Guard_UnknownMode_FallsBackToPauseWhenFullscreen()
    {
        var guard = new ForegroundGuard(
            foregroundHook: () => (nint)1,
            windowSizeHook: _ => (800, 600),
            screenSizeHook: () => (2560, 1440),
            isPlayerFocusedHook: () => false);
        // Janela pequena: PauseWhenFullscreen deixaria passar — desconhecido faz o mesmo.
        Assert.True(guard.ShouldExecute("modo-que-nao-existe"));
    }

    [Fact]
    public void DoublePress_DefaultOff_NoDelay()
    {
        var svc = OfflineService();
        Assert.False(svc.DoublePressEnabled);
        svc.Dispose();
    }
}
