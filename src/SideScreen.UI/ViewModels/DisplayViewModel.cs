using System.Windows;
using SideScreen.Core.Config;
using SideScreen.Infrastructure.Config;
using SideScreen.Infrastructure.Display;
using SideScreen.Infrastructure.Players;

namespace SideScreen.UI.ViewModels;

/// <summary>
/// Display — Fase 5: monitores reais + lock por reposicionamento (nunca rouba foco).
/// O Timer do serviço roda em threadpool: Status é marshalled para a UI via Dispatcher.
/// </summary>
public sealed class DisplayViewModel : ObservableObject
{
    private readonly JsonSettingsStore _store = new();
    private readonly MonitorLockService _lock;
    private DisplayMonitor? _selected;
    private bool _lockEnabled;
    private string _status = "";

    public DisplayViewModel() : this(new MonitorLockService(new PotPlayerController())) { }

    internal DisplayViewModel(MonitorLockService lockService)
    {
        _lock = lockService;
        _lock.StatusChanged += () => App.Current?.Dispatcher.Invoke(() => Status = _lock.Status);
        if (App.Current is not null)
            App.Current.Exit += (_, _) => _lock.Stop(); // garante: solta o mouse ao fechar o app

        Monitors = MonitorService.List();
        var cfg = _store.Load();

        _selected = Monitors.FirstOrDefault(m => m.DeviceKey == cfg.SelectedMonitorDevice)
            ?? Monitors.FirstOrDefault(m => m.IsPrimary)
            ?? Monitors.FirstOrDefault();
        _lockEnabled = cfg.LockPlayerToMonitor;
        _status = "Escolha o monitor e marque o lock.";

        if (_lockEnabled && _selected is not null)
            _lock.Start(_selected.DeviceKey);
    }

    public List<DisplayMonitor> Monitors { get; }

    public DisplayMonitor? Selected
    {
        get => _selected;
        set
        {
            if (Set(ref _selected, value))
                ApplyChoice();
        }
    }

    public bool LockPlayer
    {
        get => _lockEnabled;
        set
        {
            if (Set(ref _lockEnabled, value))
                ApplyChoice();
        }
    }

    public string Status
    {
        get => _status;
        set => Set(ref _status, value);
    }

    public string Note => "Lock = blindagem (o mouse atravessa a janela, impossível agarrar) + restaura a posição se algo mover via teclado. Clique no player não funciona travado — use os botões/atalhos. Nunca rouba foco; ignora maximizado/fullscreen.";

    private void ApplyChoice()
    {
        var cfg = _store.Load();
        cfg.SelectedMonitorDevice = _selected?.DeviceKey ?? "";
        cfg.LockPlayerToMonitor = _lockEnabled;
        var err = _store.Save(cfg);

        if (!_lockEnabled || _selected is null)
        {
            _lock.Stop();
            Status = err ?? "Lock desligado e salvo.";
            return;
        }

        _lock.Start(_selected.DeviceKey);
        Status = err ?? $"Vigiando {_selected.Label}. Arraste o player para outro monitor para testar.";
    }
}
