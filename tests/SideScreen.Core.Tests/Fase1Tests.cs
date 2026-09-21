using SideScreen.Core.Config;
using SideScreen.Infrastructure.Players;

namespace SideScreen.Core.Tests;

public class Fase1Tests
{
    [Fact]
    public void PotPlayerController_OfflineFake_ReportsNotRunning()
    {
        // Determinístico: HWND fake zero = player fechado. Não depende do PotPlayer real estar aberto.
        var player = new PotPlayerController(() => nint.Zero);
        Assert.Equal("potplayer", player.Id);
        Assert.False(player.IsRunning());

        var status = player.GetStatus();
        Assert.False(status.IsRunning);

        // No-op quando offline: não deve lançar.
        player.PlayPause();
        player.Next();
        player.Previous();
        player.SetVolume(50);
        player.VolumeUp();
        player.VolumeDown();
        player.SeekForward();
        player.SeekBackward();
    }

    [Fact]
    public void AppConfig_Defaults_MatchUserCommands()
    {
        // Padrões do usuário (MusicControl/AutoHotkey, confirmado no .exe):
        // Numpad+/- volume, F2 play/pause, F1 próximo, F1 x2 anterior.
        var config = AppConfig.Default();

        Assert.Equal("NumpadAdd", config.Shortcuts["volumeUp"]);
        Assert.Equal("NumpadSub", config.Shortcuts["volumeDown"]);
        Assert.Equal("F2", config.Shortcuts["playPause"]);
        Assert.Equal("F1", config.Shortcuts["next"]);
        Assert.Equal("F1", config.Shortcuts["previous"]);
        Assert.Equal("Right", config.Shortcuts["seekForward"]);
        Assert.Equal("Left", config.Shortcuts["seekBackward"]);
        Assert.True(config.DoublePressEnabled); // F1 x2 = anterior
        Assert.Equal("Always", config.GuardMode); // uso real é com jogo fullscreen (BNSR)
        Assert.Equal("potplayer", config.SelectedPlayerId);
        Assert.False(config.EnableGlobalHotkeys); // opt-in: não registra nada sozinho
    }
}
