using MediaControl.Core.Config;
using MediaControl.Infrastructure.Config;
using MediaControl.Infrastructure.Hotkeys;
using MediaControl.Infrastructure.Players;
using MediaControl.UI.Localization;

namespace MediaControl.UI.ViewModels;

/// <summary>
/// Shell — navegação lateral + DONO da persistência e dos hotkeys globais.
/// Persistência central: UpdateConfig() é o único caminho de escrita (uma VM nunca salva direto).
/// Hotkeys registrados no startup (AttachHwnd), NÃO ao visitar a tela Shortcuts.
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
    private bool _isDashboardActive = true;
    private bool _isPlayersActive;
    private bool _isShortcutsActive;
    private bool _isDisplayActive;
    private bool _isSettingsActive;

    public MainViewModel()
    {
        _config = _store.Load();
        _service = new HotkeyService(new PotPlayerController());
        _service.Triggered += _ => RefreshSummary();

        Dashboard = new DashboardViewModel(new PotPlayerController(), _config, shuffle => UpdateConfig(c => c.ShuffleEnabled = shuffle));
        Players = new PlayersViewModel(_config.SelectedPlayerId, id => UpdateConfig(c => c.SelectedPlayerId = id));
        Shortcuts = new ShortcutsViewModel(_service, _store, ReloadHotkeys);
        Display = new DisplayViewModel(() => _config, UpdateConfig);
        Settings = new SettingsViewModel(() => _config, UpdateConfig, ApplyLanguage);

        _current = Dashboard;
        NavigateCommand = new RelayCommand(nav => Navigate(nav as string ?? "Dashboard"));
        RefreshSummary();
    }

    /// <summary>ÚNICO caminho de escrita do config. Aplica a mutação em memória + salva no disco.</summary>
    public void UpdateConfig(Action<AppConfig> mutate)
    {
        mutate(_config);
        _store.Save(_config);
    }

    public void RefreshSummary() => RefreshSummaryImpl();

    private void RefreshSummaryImpl()
    {
        HotkeySummary = _config.EnableGlobalHotkeys
            ? $"Hotkeys: ON · {_service.Registered.Count}"
            : "Hotkeys: OFF";
    }

    public DashboardViewModel Dashboard { get; private set; }
    public PlayersViewModel Players { get; private set; }
    public ShortcutsViewModel Shortcuts { get; private set; }
    public DisplayViewModel Display { get; private set; }
    public SettingsViewModel Settings { get; private set; }

    /// <summary>Troca de idioma ao vivo: troca o dicionário e recria as telas (serviço de hotkeys é mantido).</summary>
    public void ApplyLanguage(string lang)
    {
        LanguageService.Apply(lang);
        Dashboard = new DashboardViewModel(new PotPlayerController(), _config, shuffle => UpdateConfig(c => c.ShuffleEnabled = shuffle));
        Players = new PlayersViewModel(_config.SelectedPlayerId, id => UpdateConfig(c => c.SelectedPlayerId = id));
        Shortcuts = new ShortcutsViewModel(_service, _store, ReloadHotkeys);
        Display = new DisplayViewModel(() => _config, UpdateConfig);
        Settings = new SettingsViewModel(() => _config, UpdateConfig, ApplyLanguage);
        Raise(nameof(Dashboard));
        Raise(nameof(Players));
        Raise(nameof(Shortcuts));
        Raise(nameof(Display));
        Raise(nameof(Settings));
        Navigate(SelectedNav);
    }

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

    public bool IsDashboardActive { get => _isDashboardActive; private set => Set(ref _isDashboardActive, value); }
    public bool IsPlayersActive { get => _isPlayersActive; private set => Set(ref _isPlayersActive, value); }
    public bool IsShortcutsActive { get => _isShortcutsActive; private set => Set(ref _isShortcutsActive, value); }
    public bool IsDisplayActive { get => _isDisplayActive; private set => Set(ref _isDisplayActive, value); }
    public bool IsSettingsActive { get => _isSettingsActive; private set => Set(ref _isSettingsActive, value); }

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

    private void Navigate(string nav)
    {
        SelectedNav = nav;
        IsDashboardActive = nav == "Dashboard";
        IsPlayersActive = nav == "Players";
        IsShortcutsActive = nav == "Shortcuts";
        IsDisplayActive = nav == "Display";
        IsSettingsActive = nav == "Settings";
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
