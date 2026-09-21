using SideScreen.Core.Config;
using SideScreen.Infrastructure.Players;

namespace SideScreen.Core.Tests;

public class Fase1Tests
{
    [Fact]
    public void PotPlayerController_Stub_ReportsNotRunning()
    {
        var player = new PotPlayerController();
        Assert.Equal("potplayer", player.Id);
        Assert.False(player.IsRunning());
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
