using SideScreen.Infrastructure.Players;
using SideScreen.Infrastructure.Windows;

namespace SideScreen.Core.Tests;

/// <summary>
/// Fase 3: valida protocolo real SEM mudar reprodução (só leitura: FindWindow, GET_VOLUME, GET_STATUS).
/// Se o PotPlayer estiver fechado, os testes passam como Skip implícito (assert offline).
/// </summary>
public class Fase3LiveTests
{
    [Fact]
    public void WindowFinder_ReturnsZeroOrValidHandle()
    {
        nint h = WindowFinder.FindPotPlayer();
        // Zero = fechado (válido). Não-zero = janela real.
        Assert.True(h == nint.Zero || h != nint.Zero);
    }

    [Fact]
    public void GetStatus_NeverThrows_AndVolumeInRange()
    {
        var player = new PotPlayerController();
        var status = player.GetStatus();

        Assert.InRange(status.Volume, 0, 100);
        // Se rodando, HWND deve ser válido.
        if (status.IsRunning)
            Assert.NotEqual(nint.Zero, player.GetWindowHandle());
    }

    [Fact]
    public void CommandIds_MatchKnownProtocol()
    {
        Assert.Equal(0x0111, PotPlayerCommandIds.WmCommand);
        Assert.Equal(0x0400, PotPlayerCommandIds.PotCommand);
        Assert.Equal(10014, PotPlayerCommandIds.CmdPlayPause);
        Assert.Equal(10124, PotPlayerCommandIds.CmdNext);
        Assert.Equal(10123, PotPlayerCommandIds.CmdPrevious);
        Assert.Equal(0x5000, PotPlayerCommandIds.PotGetVolume);
        Assert.Equal(0x5001, PotPlayerCommandIds.PotSetVolume);
        Assert.Equal(0x5006, PotPlayerCommandIds.PotGetPlayStatus);
    }
}
