using SideScreen.Core.Config;
using SideScreen.Infrastructure.Config;
using SideScreen.Infrastructure.Hotkeys;
using SideScreen.Infrastructure.Players;

namespace SideScreen.UI.ViewModels;

/// <summary>
/// Shell — navegação lateral + dono dos hotkeys globais.
/// Os hotkeys são registrados no startup (AttachHwnd), NÃO ao visitar a tela Shortcuts —
/// antes eles só registravam se o usuário abrisse a tela, e nada disparava.
/// </summary>
public sealed class MainViewModel : ObservableObject
{
    private readonly JsonSettingsStore _store = new();
    private readonly HotkeyService _service;
    private AppConfig _config;
    private nint _hWnd;

    private object _current;
    private string _selectedNav = "Dashboard";
    private string _hotkeySummary = "Hotkeys: ...";

    public MainViewModel()
    {
        _config = _store.Load();
        _service = new HotkeyService(new PotPlayerController());
        _service.Triggered += _ => RefreshSummary();

        Dashboard = new DashboardViewModel();
        Players = new PlayersViewModel();
        Shortcuts = new ShortcutsViewModel(_service, _store, ReloadHotkeys);
        Display = new DisplayViewModel();
        Settings = new SettingsViewModel();

        _current = Dashboard;
        NavigateCommand = new RelayCommand(nav => Navigate(nav as string ?? "Dashboard"));
        RefreshSummary();
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

    /// <summary>Exibido no rodapé da sidebar. Ex: "Hotkeys: ON · 6" ou "Hotkeys: OFF".</summary>
    public string HotkeySummary
    {
        get => _hotkeySummary;
        private set => Set(ref _hotkeySummary, value);
    }

    public RelayCommand NavigateCommand { get; }

    /// <summary>Chamado pela MainWindow quando o HWND existe. Registra se o config mandar.</summary>
    public void AttachHwnd(nint hWnd)
    {
        _hWnd = hWnd;
        HotkeyLog.Append($"attach hWnd=0x{hWnd:X} enable={_config.EnableGlobalHotkeys} il={Infrastructure.Security.ProcessIntegrity.Current()}");
        if (_config.EnableGlobalHotkeys)
            StartService();
        else
            RefreshSummary();
    }

    public bool HandleHotkey(int id) => _service.HandleHotkeyMessage(id);

    public void Detach() => _service.Stop();

    /// <summary>Recarrega o config do disco (após Apply na tela) e religa/desliga.</summary>
    public void ReloadHotkeys()
    {
        _config = _store.Load();
        if (!_config.EnableGlobalHotkeys)
        {
            _service.Stop();
            HotkeyLog.Append("reload: desligadas pelo config");
        }
        else if (_hWnd != nint.Zero)
        {
            StartService();
        }
        RefreshSummary();
    }

    private void StartService()
    {
        _service.GuardMode = _config.GuardMode;
        _service.DoublePressEnabled = _config.DoublePressEnabled;
        _service.DoublePressWindowMs = _config.DoublePressWindowMs;
        _service.AllowedProcesses = [.. _config.AllowedProcesses];
        _service.Start(_hWnd, new Dictionary<string, string>(_config.Shortcuts));
        RefreshSummary();
    }

    private void RefreshSummary()
    {
        HotkeySummary = _config.EnableGlobalHotkeys
            ? $"Hotkeys: ON · {_service.Registered.Count}"
            : "Hotkeys: OFF";
    }

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
