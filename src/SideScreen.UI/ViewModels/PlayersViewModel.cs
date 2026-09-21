using SideScreen.Core.Config;

namespace SideScreen.UI.ViewModels;

public sealed record PlayerOption(string Id, string Name, string Status, bool IsFuture);

/// <summary>
/// Players — seleção persistida no config. O controle real ainda mira o PotPlayer
/// (fábrica de controllers entra na fase pós-v1 com MPC-HC).
/// </summary>
public sealed class PlayersViewModel : ObservableObject
{
    private readonly Action<string>? _onSelected;
    private PlayerOption? _selected;

    public PlayersViewModel() : this(AppConfig.Default().SelectedPlayerId, null) { }

    public PlayersViewModel(string selectedId, Action<string>? onSelected)
    {
        _onSelected = onSelected;
        Available =
        [
            new PlayerOption("potplayer", "PotPlayer", "Suportado — controle real via SendMessage", false),
            new PlayerOption("mpc-hc", "MPC-HC", "Futuro — Fase pós-v1", true),
            new PlayerOption("vlc", "VLC", "Futuro — ideia", true),
        ];
        _selected = Available.FirstOrDefault(o => o.Id == selectedId) ?? Available[0];
        SelectPlayerCommand = new RelayCommand(id =>
        {
            var opt = Available.FirstOrDefault(o => o.Id == (id as string));
            if (opt is not null && !opt.IsFuture)
                Selected = opt;
        });
    }

    public List<PlayerOption> Available { get; }

    public PlayerOption? Selected
    {
        get => _selected;
        set
        {
            if (Set(ref _selected, value) && value is not null)
            {
                try { _onSelected?.Invoke(value.Id); } catch { }
            }
        }
    }

    public RelayCommand SelectPlayerCommand { get; }
}
