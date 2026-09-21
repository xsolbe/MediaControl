using SideScreen.Core.Config;
using SideScreen.Core.Players;
using SideScreen.Infrastructure.Players;

namespace SideScreen.UI.ViewModels;

/// <summary>
/// Dashboard — Fase 2: exibe estado fake + chama stub. Fase 3: liga no PotPlayer real.
/// </summary>
public sealed class DashboardViewModel : ObservableObject
{
    private readonly IPlayerController _player;
    private string _statusLine = "";
    private int _volume;
    private bool _shuffle;

    public DashboardViewModel() : this(new PotPlayerController(), AppConfig.Default()) { }

    public DashboardViewModel(IPlayerController player, AppConfig config)
    {
        _player = player;
        Shuffle = config.ShuffleEnabled;

        PlayPauseCommand = new RelayCommand(() => StatusLine = $"[{DateTime.Now:HH:mm:ss}] Play/Pause → {_player.DisplayName} (stub Fase 2, real na Fase 3)");
        NextCommand = new RelayCommand(() => StatusLine = $"[{DateTime.Now:HH:mm:ss}] Next → {_player.DisplayName} (stub)");
        PreviousCommand = new RelayCommand(() => StatusLine = $"[{DateTime.Now:HH:mm:ss}] Previous → {_player.DisplayName} (stub)");

        Refresh();
    }

    public string PlayerName => "PotPlayer";
    public string PlayerState => _player.IsRunning() ? "Running" : "Offline (stub Fase 1)";

    public string StatusLine
    {
        get => _statusLine;
        set => Set(ref _statusLine, value);
    }

    public int Volume
    {
        get => _volume;
        set
        {
            if (Set(ref _volume, Math.Clamp(value, 0, 100)))
                StatusLine = $"Volume → {Volume} (slider local, SET_VOLUME real na Fase 3)";
        }
    }

    public bool Shuffle
    {
        get => _shuffle;
        set
        {
            if (Set(ref _shuffle, value))
                StatusLine = $"Shuffle {(value ? "ON" : "OFF")} (nativo do player na Fase 3)";
        }
    }

    public RelayCommand PlayPauseCommand { get; }
    public RelayCommand NextCommand { get; }
    public RelayCommand PreviousCommand { get; }

    private void Refresh()
    {
        var s = _player.GetStatus();
        _volume = s.Volume;
        StatusLine = $"IsRunning={s.IsRunning} — {PlayerName} ({_player.Id}) — Fase 3 vai detectar via FindWindow.";
    }
}
