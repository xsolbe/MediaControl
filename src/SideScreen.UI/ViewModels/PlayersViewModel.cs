namespace SideScreen.UI.ViewModels;

public sealed record PlayerOption(string Id, string Name, string Status, bool IsFuture);

/// <summary>
/// Players — Fase 2: lista estática. Futuro: MPC-HC via novo IPlayerController.
/// </summary>
public sealed class PlayersViewModel : ObservableObject
{
    private PlayerOption? _selected;

    public PlayersViewModel()
    {
        Available =
        [
            new PlayerOption("potplayer", "PotPlayer", "Suportado — stub Fase 2", false),
            new PlayerOption("mpc-hc", "MPC-HC", "Futuro — Fase pós-v1", true),
            new PlayerOption("vlc", "VLC", "Futuro — ideia", true),
        ];
        _selected = Available[0];
    }

    public List<PlayerOption> Available { get; }

    public PlayerOption? Selected
    {
        get => _selected;
        set => Set(ref _selected, value);
    }
}
