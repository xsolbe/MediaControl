using SideScreen.Core.Config;
using SideScreen.Infrastructure.Display;
using SideScreen.Infrastructure.Players;
using SideScreen.Infrastructure.Windows;

namespace SideScreen.UI.ViewModels;

/// <summary>
/// Display — monitores reais + mover o player ao selecionar (one-shot, Closes #1).
/// Sem lock/vigia (removido — ver docs/monitors.md). Persistência via config central.
/// </summary>
public sealed class DisplayViewModel : ObservableObject
{
    private readonly Action<Action<AppConfig>> _update;
    private DisplayMonitor? _selected;
    private string _status = "";

    public DisplayViewModel(Func<AppConfig> getConfig, Action<Action<AppConfig>> update)
    {
        _update = update;
        Monitors = MonitorService.List();
        var initial = getConfig().SelectedMonitorDevice;
        _selected = Monitors.FirstOrDefault(m => m.DeviceKey == initial)
            ?? Monitors.FirstOrDefault(m => m.IsPrimary)
            ?? Monitors.FirstOrDefault();
        _status = "Escolha o monitor: o player vai para ele na hora.";
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
                if (_selected is not null)
                {
                    var h = WindowFinder.FindPotPlayer();
                    Status = MonitorMover.MoveTo(h, _selected);
                }
            }
        }
    }

    public string Status
    {
        get => _status;
        set => Set(ref _status, value);
    }

    public string Note => "Mover é one-shot (sem vigia). Para não arrastar sem querer, use fullscreen (F5) no monitor desejado.";
}
