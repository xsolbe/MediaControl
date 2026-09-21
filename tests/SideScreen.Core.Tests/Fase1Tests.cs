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
    }

    [Fact]
    public void AppConfig_Defaults_MatchUserCommands()
    {
        // Padrões do usuário (script MusicControl/AutoHotkey): Volume+/-, F2, F1, F1 x2.
        var config = AppConfig.Default();

        Assert.Equal("VolumeUp", config.Shortcuts["volumeUp"]);
        Assert.Equal("VolumeDown", config.Shortcuts["volumeDown"]);
        Assert.Equal("F2", config.Shortcuts["playPause"]);
        Assert.Equal("F1", config.Shortcuts["next"]);
        Assert.Equal("F1", config.Shortcuts["previous"]);
        Assert.True(config.DoublePressEnabled); // F1 x2 = anterior
        Assert.Equal("potplayer", config.SelectedPlayerId);
        Assert.False(config.EnableGlobalHotkeys); // opt-in: não registra nada sozinho
    }
}
