using SideScreen.Core.Config;
using SideScreen.Infrastructure.Display;

namespace SideScreen.Core.Tests;

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
}
