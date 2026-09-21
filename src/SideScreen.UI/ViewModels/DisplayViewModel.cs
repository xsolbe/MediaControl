namespace SideScreen.UI.ViewModels;

/// <summary>
/// Display — Fase 2: placeholder. Fase 5: EnumDisplayMonitors + auto-reposicionamento.
/// </summary>
public sealed class DisplayViewModel : ObservableObject
{
    private int _selectedMonitor = 1;
    private bool _lock;

    public List<string> Monitors { get; } =
    [
        "Monitor 1 — Principal (placeholder)",
        "Monitor 2 — Secundário (placeholder)",
    ];

    public int SelectedMonitor
    {
        get => _selectedMonitor;
        set => Set(ref _selectedMonitor, value);
    }

    public bool LockPlayer
    {
        get => _lock;
        set => Set(ref _lock, value);
    }

    public string Note => "Lock real na Fase 5 via MonitorFromWindow + SetWindowPos (sem roubar foco).";
}
