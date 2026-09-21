using SideScreen.Core.Config;
using SideScreen.Core.Players;
using SideScreen.Infrastructure.Players;

namespace SideScreen.UI.ViewModels;

/// <summary>
/// Dashboard — Fase 3: chama PotPlayer real via SendMessage. Sem foco, sem SendKeys.
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

        PlayPauseCommand = new RelayCommand(() =>
        {
            _player.PlayPause();
            RefreshStatus("Play/Pause enviado via WM_COMMAND 10014");
        });
        NextCommand = new RelayCommand(() =>
        {
            _player.Next();
            RefreshStatus("Next enviado via WM_COMMAND 10124");
        });
        PreviousCommand = new RelayCommand(() =>
        {
            _player.Previous();
            RefreshStatus("Previous enviado via WM_COMMAND 10123");
        });
        RefreshCommand = new RelayCommand(() => RefreshStatus("Refresh manual"));

        // Volume inicial vem do player (POT_GET_VOLUME). Shuffle vem do config (sem GET_SHUFFLE oficial).
        var s = _player.GetStatus();
        _volume = s.Volume;
        _shuffle = config.ShuffleEnabled;
        RefreshStatus("Inicializado — leitura via POT_GET_VOLUME/POT_GET_PLAY_STATUS");
        Raise(nameof(PlayerState));
    }

    public string PlayerName => "PotPlayer";
    public string PlayerState => _player.IsRunning() ? GetStateLabel(_player.GetStatus().State) : "Offline — abra o PotPlayer";

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
            int v = Math.Clamp(value, 0, 100);
            if (Set(ref _volume, v))
            {
                _player.SetVolume(v);
                RefreshStatus($"SET_VOLUME {v} via POT_SET_VOLUME 0x5001", skipVolumeRead: true);
            }
        }
    }

    public bool Shuffle
    {
        get => _shuffle;
        set
        {
            if (Set(ref _shuffle, value))
            {
                _player.SetShuffle(value);
                RefreshStatus($"Shuffle {(value ? "ON" : "OFF")} — ID candidato, a validar com Spy++ (ver docs)", skipVolumeRead: false);
            }
        }
    }

    public RelayCommand PlayPauseCommand { get; }
    public RelayCommand NextCommand { get; }
    public RelayCommand PreviousCommand { get; }
    public RelayCommand RefreshCommand { get; }

    private void RefreshStatus(string action, bool skipVolumeRead = false)
    {
        try
        {
            var s = _player.GetStatus();
            if (!skipVolumeRead)
                Set(ref _volume, s.Volume);
            string state = s.IsRunning ? GetStateLabel(s.State) : "Offline";
            StatusLine = $"[{DateTime.Now:HH:mm:ss}] {action} | {state} Vol={s.Volume} HWND=0x{_player.GetWindowHandle():X}";
            Raise(nameof(PlayerState));
        }
        catch (Exception ex)
        {
            StatusLine = $"[{DateTime.Now:HH:mm:ss}] {action} | erro: {ex.Message}";
        }
    }

    private static string GetStateLabel(PlaybackState state) => state switch
    {
        PlaybackState.Playing => "Playing",
        PlaybackState.Paused => "Paused",
        PlaybackState.Stopped => "Stopped",
        _ => "Unknown",
    };
}
