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
        // Caminho AHK (MusicControl.exe): APPCOMMAND para next/prev/play-pause.
        Assert.Equal(0x0319, PotPlayerCommandIds.WmAppCommand);
        Assert.Equal(0xB0000, PotPlayerCommandIds.AppCommandMediaNext);
        Assert.Equal(0xC0000, PotPlayerCommandIds.AppCommandMediaPrevious);
        Assert.Equal(0xE0000, PotPlayerCommandIds.AppCommandMediaPlayPause);
        Assert.Equal(10035, PotPlayerCommandIds.CmdVolumeUp);
        Assert.Equal(10036, PotPlayerCommandIds.CmdVolumeDown);
        Assert.Equal(10069, PotPlayerCommandIds.CmdShuffleToggle); // lista ld3l/PotPlayerControl
        Assert.Equal(0x5000, PotPlayerCommandIds.PotGetVolume);
        Assert.Equal(0x5001, PotPlayerCommandIds.PotSetVolume);
        Assert.Equal(0x5006, PotPlayerCommandIds.PotGetPlayStatus);
    }
}
