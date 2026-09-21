using SideScreen.Core.Config;
using SideScreen.Infrastructure.Config;
using SideScreen.Infrastructure.Display;

namespace SideScreen.UI.ViewModels;

/// <summary>
/// Display — somente informação de monitores.
/// A trava de posição foi REMOVIDA (2026-09-21, pedido do usuário): Jerry-built não colou —
/// os frames do PotPlayer são layered (skins gerenciam o estilo e vencem a corrida),
/// resultado prático era controles mortos + arrasto livre (o inverso do desejado).
/// Ver docs/monitors.md (post-mortem).
/// </summary>
public sealed class DisplayViewModel : ObservableObject
{
    private readonly JsonSettingsStore _store = new();
    private DisplayMonitor? _selected;

    public DisplayViewModel()
    {
        Monitors = MonitorService.List();
        var cfg = _store.Load();
        _selected = Monitors.FirstOrDefault(m => m.DeviceKey == cfg.SelectedMonitorDevice)
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
                var cfg = _store.Load();
                cfg.SelectedMonitorDevice = _selected?.DeviceKey ?? "";
                _store.Save(cfg);
            }
        }
    }

    public string Note => "Trava de posição removida: os frames do PotPlayer são layered e a blindagem virava contra os controles. Use fullscreen (F5 no PotPlayer) no monitor desejado — em fullscreen não há arrasto.";
}
