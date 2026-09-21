namespace SideScreen.UI.ViewModels;

/// <summary>
/// Shell — navegação lateral. Uma ViewModel por tela, sem framework externo.
/// </summary>
public sealed class MainViewModel : ObservableObject
{
    private object _current = new DashboardViewModel();
    private string _selectedNav = "Dashboard";

    public MainViewModel()
    {
        Dashboard = new DashboardViewModel();
        Players = new PlayersViewModel();
        Shortcuts = new ShortcutsViewModel();
        Display = new DisplayViewModel();
        Settings = new SettingsViewModel();

        _current = Dashboard;
        NavigateCommand = new RelayCommand(nav => Navigate(nav as string ?? "Dashboard"));
    }

    public DashboardViewModel Dashboard { get; }
    public PlayersViewModel Players { get; }
    public ShortcutsViewModel Shortcuts { get; }
    public DisplayViewModel Display { get; }
    public SettingsViewModel Settings { get; }

    public object Current
    {
        get => _current;
        set => Set(ref _current, value);
    }

    public string SelectedNav
    {
        get => _selectedNav;
        set => Set(ref _selectedNav, value);
    }

    public RelayCommand NavigateCommand { get; }

    private void Navigate(string nav)
    {
        SelectedNav = nav;
        Current = nav switch
        {
            "Players" => Players,
            "Shortcuts" => Shortcuts,
            "Display" => Display,
            "Settings" => Settings,
            _ => Dashboard,
        };
    }
}
