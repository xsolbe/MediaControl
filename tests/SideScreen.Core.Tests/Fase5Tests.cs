using SideScreen.Infrastructure.Display;

namespace SideScreen.Core.Tests;

public class Fase5Tests
{
    [Fact]
    public void ClampIntoBounds_KeepsRelativeOffset()
    {
        // Janela 800x600 em (100,100) no monitor 1 (0,0,1920,1080) → alvo monitor 2 (1920,0,1920,1080).
        var (x, y) = MonitorLayout.ClampIntoBounds(100, 100, 800, 600, 0, 0, 1920, 0, 1920, 1080);
        Assert.Equal(2020, x);
        Assert.Equal(100, y);
    }

    [Fact]
    public void ClampIntoBounds_ClampsOverflow()
    {
        // Janela grudada na borda direita do monitor 1 não pode vazar do alvo.
        var (x, y) = MonitorLayout.ClampIntoBounds(1800, 900, 800, 600, 0, 0, 1920, 0, 1920, 1080);
        Assert.Equal(1920 + 1920 - 800, x); // 2920
        Assert.Equal(1080 - 600, y);        // 480
    }

    [Fact]
    public void ClampIntoBounds_OversizedWindow_PinsToCorner()
    {
        var (x, y) = MonitorLayout.ClampIntoBounds(0, 0, 3000, 2000, 0, 0, 1920, 0, 1920, 1080);
        Assert.Equal(1920, x);
        Assert.Equal(0, y);
    }

    [Fact]
    public void CoversMonitor_DetectsFullscreen()
    {
        var m = new DisplayMonitor("D", "M", 0, 0, 1920, 1080, true);
        Assert.True(MonitorLayout.CoversMonitor(0, 0, 1920, 1080, m));
        Assert.False(MonitorLayout.CoversMonitor(100, 100, 800, 600, m));
    }

    [Fact]
    public void RestorePosition_WithAnchor_ReturnsExactSpot()
    {
        // Âncora (200,150) no alvo; janela atual 800x600 fora → volta exatamente para a âncora.
        var (x, y) = MonitorLayout.RestorePosition(3000, 500, 800, 600, (200, 150), 0, 0, 0, 0, 1920, 1080);
        Assert.Equal(200, x);
        Assert.Equal(150, y);
    }

    [Fact]
    public void RestorePosition_AnchorClamped_WhenVideoBigger()
    {
        // Âncora válida, mas vídeo atual maior que o alvo: encosta no canto em vez de vazar.
        var (x, y) = MonitorLayout.RestorePosition(0, 0, 2000, 1200, (100, 100), 0, 0, 0, 0, 1920, 1080);
        Assert.Equal(0, x);
        Assert.Equal(0, y);
    }

    [Fact]
    public void RestorePosition_WithoutAnchor_FallsBackToRelative()
    {
        var (x, y) = MonitorLayout.RestorePosition(100, 100, 800, 600, null, 0, 0, 1920, 0, 1920, 1080);
        Assert.Equal(2020, x);
        Assert.Equal(100, y);
    }

    [Fact]
    public void MonitorService_ListsAtLeastOne()
    {
        // Roda na máquina real: sempre há ≥1 monitor. Sem asserts de quantidade exata.
        var list = MonitorService.List();
        Assert.NotEmpty(list);
        Assert.Contains(list, m => m.Width > 0 && m.Height > 0);
    }

    [Theory]
    [InlineData(true, false, false, MonitorLockService.LockAction.UpdateAnchor)]   // no alvo → ancora
    [InlineData(false, true, false, MonitorLockService.LockAction.Skip)]           // maximizado → ignora
    [InlineData(false, true, true, MonitorLockService.LockAction.Skip)]            // fullscreen arrastando? ignora
    [InlineData(false, false, true, MonitorLockService.LockAction.HoldEdge)]       // fora + arrasto → parede
    [InlineData(false, false, false, MonitorLockService.LockAction.RestoreAnchor)] // fora + solto → restaura
    public void Decide_PicksRightAction(bool onTarget, bool blocked, bool dragging, MonitorLockService.LockAction expected)
    {
        Assert.Equal(expected, MonitorLockService.Decide(onTarget, blocked, dragging));
    }

    [Fact]
    public void ClickThrough_BitMath()
    {
        const long transparent = 0x20L;
        Assert.True(WindowInteraction.HasFlag(WindowInteraction.WithFlag(0, transparent), transparent));
        Assert.False(WindowInteraction.HasFlag(WindowInteraction.WithoutFlag(transparent, transparent), transparent));
        // Preserva os outros bits ao ligar/desligar.
        long exo = 0x40000L;
        Assert.Equal(exo | transparent, WindowInteraction.WithFlag(exo, transparent));
        Assert.Equal(exo, WindowInteraction.WithoutFlag(exo | transparent, transparent));
    }
}
