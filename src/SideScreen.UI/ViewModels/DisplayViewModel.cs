using SideScreen.Core.Config;
using SideScreen.Infrastructure.Display;

namespace SideScreen.UI.ViewModels;

/// <summary>
/// Display — somente informação de monitores (trava REMOVIDA — ver docs/monitors.md).
/// Persistência via config central (dono: MainViewModel).
/// </summary>
public sealed class DisplayViewModel : ObservableObject
{
    private readonly Action<Action<AppConfig>> _update;
    private DisplayMonitor? _selected;

    public DisplayViewModel(Func<AppConfig> getConfig, Action<Action<AppConfig>> update)
    {
        _update = update;
        Monitors = MonitorService.List();
        var initial = getConfig().SelectedMonitorDevice;
        _selected = Monitors.FirstOrDefault(m => m.DeviceKey == initial)
            ?? Monitors.FirstOrDefault(m => m.IsPrimary)
            ?? Monitors.FirstOrDefault();
    }

    public List<DisplayMonitor> Monitors { get; }

    public DisplayMonitor? Selected
    {
        get => _selected;
        set
        {
            if (Set(ref _selected, value))
            {
                try { _update(c => c.SelectedMonitorDevice = _selected?.DeviceKey ?? ""); } catch { }
            }
        }
    }

    public string Note => "Trava de posição removida: os frames do PotPlayer são layered e a blindagem virava contra os controles. Use fullscreen (F5 no PotPlayer) no monitor desejado — em fullscreen não há arrasto.";
}
