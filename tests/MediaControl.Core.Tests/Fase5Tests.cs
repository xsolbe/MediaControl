using MediaControl.Core.Config;
using MediaControl.Infrastructure.Display;

namespace MediaControl.Core.Tests;

/// <summary>
/// Fase 5: a trava de posição foi REMOVIDA (post-mortem em docs/monitors.md).
/// Restou: listagem de monitores + seleção persistida (tela Display informativa).
/// </summary>
public class Fase5Tests
{
    [Fact]
    public void MonitorService_ListsAtLeastOne()
    {
        // Roda na máquina real: sempre há ≥1 monitor. Sem asserts de quantidade exata.
        var list = MonitorService.List();
        Assert.NotEmpty(list);
        Assert.Contains(list, m => m.Width > 0 && m.Height > 0);
    }

    [Fact]
    public void MonitorService_KeysAreStable()
    {
        var list = MonitorService.List();
        Assert.All(list, m => Assert.False(string.IsNullOrWhiteSpace(m.DeviceKey)));
        Assert.Equal(list.Count, list.Select(m => m.DeviceKey).Distinct().Count());
    }

    [Fact]
    public void AppConfig_MonitorSelection_DefaultsEmpty()
    {
        var cfg = AppConfig.Default();
        Assert.Equal("", cfg.SelectedMonitorDevice);
    }

    [Fact]
    public void MonitorMover_Center_PutsMiddle()
    {
        var (x, y) = MonitorMover.Center(800, 600, 1920, 0, 1920, 1080);
        Assert.Equal(1920 + (1920 - 800) / 2, x);
        Assert.Equal((1080 - 600) / 2, y);
    }

    [Fact]
    public void MonitorMover_Center_Oversized_PinsCorner()
    {
        var (x, y) = MonitorMover.Center(3000, 2000, 1920, 0, 1920, 1080);
        Assert.Equal(1920, x);
        Assert.Equal(0, y);
    }

    [Fact]
    public void MonitorMover_Resolve_WithSaved_ReturnsExactSpot()
    {
        var target = new DisplayMonitor("D2", "Monitor 2", 0, 0, 2560, 1440, true);
        var (x, y) = MonitorMover.Resolve(new MonitorMover.Rect(100, 100, 800, 600), target, (200, 150, 800, 600));
        Assert.Equal(200, x);
        Assert.Equal(150, y);
    }

    [Fact]
    public void MonitorMover_Resolve_WithoutSaved_Centers()
    {
        var target = new DisplayMonitor("D2", "Monitor 2", 0, 0, 2560, 1440, true);
        var (x, y) = MonitorMover.Resolve(new MonitorMover.Rect(100, 100, 800, 600), target, null);
        Assert.Equal((2560 - 800) / 2, x);
        Assert.Equal((1440 - 600) / 2, y);
    }

    [Fact]
    public void MonitorMover_Resolve_SavedOutside_ClampsInside()
    {
        // Resolução mudou depois de salvo: prende dentro dos limites atuais.
        var target = new DisplayMonitor("D2", "Monitor 2", 0, 0, 1920, 1080, true);
        var (x, y) = MonitorMover.Resolve(new MonitorMover.Rect(0, 0, 800, 600), target, (5000, 5000, 800, 600));
        Assert.Equal(1920 - 800, x);
        Assert.Equal(1080 - 600, y);
    }
}
