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
    public void AppConfig_Defaults_AreSafeForGamers()
    {
        var config = AppConfig.Default();

        // Defaults nunca devem ser F1/F2 sozinhos (conflito com jogos).
        Assert.DoesNotContain(config.Shortcuts.Values, s => s == "F1" || s == "F2");
        Assert.Equal("potplayer", config.SelectedPlayerId);
        Assert.Equal(1, config.Version);
    }
}
