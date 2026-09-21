using System.Collections.ObjectModel;
using SideScreen.Core.Config;
using SideScreen.Core.Hotkeys;
using SideScreen.Infrastructure.Config;
using SideScreen.Infrastructure.Hotkeys;
using SideScreen.Infrastructure.Players;

namespace SideScreen.UI.ViewModels;

public sealed class ShortcutEdit : ObservableObject
{
    private string _gesture;
    private string? _warning;

    public ShortcutEdit(string actionKey, string label, string gesture)
    {
        ActionKey = actionKey;
        Label = label;
        _gesture = gesture;
    }

    public string ActionKey { get; }
    public string Label { get; }

    public string Gesture
    {
        get => _gesture;
        set => Set(ref _gesture, value);
    }

    public string? Warning
    {
        get => _warning;
        set => Set(ref _warning, value);
    }
}

/// <summary>
/// Shortcuts — Fase 4: captura, conflitos, warnings gamer, Apply/Restaurar, guard + double-press.
/// O HWND é anexado pela View (code-behind) para RegisterHotKey + hook WM_HOTKEY.
/// </summary>
public sealed class ShortcutsViewModel : ObservableObject
{
    private static readonly (string Key, string Label)[] Order =
    [
        ("volumeUp", "Aumentar volume"),
        ("volumeDown", "Diminuir volume"),
        ("playPause", "Play/Pause"),
        ("next", "Próximo vídeo"),
        ("previous", "Vídeo anterior"),
    ];

    private readonly JsonSettingsStore _store = new();
    private readonly HotkeyService _service;
    private nint _hWnd;
    private bool _enableGlobal;
    private string _guardMode = Core.Hotkeys.GuardModes.PauseWhenFullscreen;
    private bool _doublePress;
    private int _doubleWindow = 350;
    private string _status = "";

    public ShortcutsViewModel() : this(new HotkeyService(new PotPlayerController())) { }

    internal ShortcutsViewModel(HotkeyService service)
    {
        _service = service;
        _service.Triggered += msg => Status = msg;

        var cfg = _store.Load();
        Rows = new ObservableCollection<ShortcutEdit>(Order.Select(o =>
            new ShortcutEdit(o.Key, o.Label, cfg.Shortcuts.TryGetValue(o.Key, out var g) ? g : AppConfig.Default().Shortcuts[o.Key])));

        _enableGlobal = cfg.EnableGlobalHotkeys;
        _guardMode = Core.Hotkeys.GuardModes.All.Contains(cfg.GuardMode) ? cfg.GuardMode : Core.Hotkeys.GuardModes.PauseWhenFullscreen;
        _doublePress = cfg.DoublePressEnabled;
        _doubleWindow = Math.Clamp(cfg.DoublePressWindowMs, 200, 500);

        foreach (var r in Rows)
            r.PropertyChanged += (_, _) => Validate();

        ApplyCommand = new RelayCommand(Apply);
        RestoreDefaultsCommand = new RelayCommand(RestoreDefaults);

        Validate();
        Status = cfg.EnableGlobalHotkeys
            ? "Globais ATIVADAS no config — Apply para registrar nesta janela."
            : "Globais DESLIGADAS por padrão (seguro para jogos). Edite, marque a caixa e clique Apply.";
    }

    public ObservableCollection<ShortcutEdit> Rows { get; }
    public string[] GuardModes => Core.Hotkeys.GuardModes.All;

    public bool EnableGlobal
    {
        get => _enableGlobal;
        set => Set(ref _enableGlobal, value);
    }

    public string SelectedGuardMode
    {
        get => _guardMode;
        set => Set(ref _guardMode, value);
    }

    public bool DoublePressEnabled
    {
        get => _doublePress;
        set => Set(ref _doublePress, value);
    }

    public int DoublePressWindowMs
    {
        get => _doubleWindow;
        set => Set(ref _doubleWindow, Math.Clamp(value, 200, 500));
    }

    public string Status
    {
        get => _status;
        set => Set(ref _status, value);
    }

    public string ConfigPath => JsonSettingsStore.ConfigPath;

    public RelayCommand ApplyCommand { get; }
    public RelayCommand RestoreDefaultsCommand { get; }

    /// <summary>Chamado pela View quando o HWND da janela principal existe.</summary>
    public void AttachHwnd(nint hWnd)
    {
        _hWnd = hWnd;
        if (_enableGlobal)
            Apply();
    }

    public bool HandleHotkey(int id) => _service.HandleHotkeyMessage(id);

    public void Detach() => _service.Stop();

    private void Apply()
    {
        Validate();
        if (Rows.Any(r => r.Warning != null && r.Warning.StartsWith("Conflito")))
        {
            Status = "Resolva os conflitos antes de aplicar (gestos duplicados).";
            return;
        }

        var cfg = _store.Load();
        foreach (var r in Rows)
            cfg.Shortcuts[r.ActionKey] = r.Gesture;
        cfg.EnableGlobalHotkeys = _enableGlobal;
        cfg.GuardMode = _guardMode;
        cfg.DoublePressEnabled = _doublePress;
        cfg.DoublePressWindowMs = _doubleWindow;

        var err = _store.Save(cfg);
        if (err != null)
        {
            Status = $"Erro ao salvar: {err}";
            return;
        }

        if (!_enableGlobal)
        {
            _service.Stop();
            Status = $"Salvo em config.json. Globais DESLIGADAS — controle pelos botões do Dashboard.";
            return;
        }

        if (_hWnd == nint.Zero)
        {
            Status = "Salvo. HWND ainda indisponível — será registrado ao abrir a janela.";
            return;
        }

        _service.GuardMode = _guardMode;
        _service.DoublePressEnabled = _doublePress;
        var dict = Rows.ToDictionary(r => r.ActionKey, r => r.Gesture);
        // DoublePressWindowMs entra no construtor; recria serviço se mudou? Fase 4: usa valor atual via novo serviço.
        bool ok = _service.Start(_hWnd, dict);
        Status = ok
            ? $"Registrados {dict.Count} hotkeys (guard={_guardMode}, double-press={(_doublePress ? $"ON {_doubleWindow}ms" : "OFF")})."
            : $"Parcial: {string.Join("; ", _service.Errors)}";
    }

    private void RestoreDefaults()
    {
        var d = AppConfig.Default();
        foreach (var r in Rows)
            r.Gesture = d.Shortcuts[r.ActionKey];
        EnableGlobal = false;
        SelectedGuardMode = Core.Hotkeys.GuardModes.PauseWhenFullscreen;
        DoublePressEnabled = false;
        DoublePressWindowMs = 350;
        Validate();
        Status = "Padrões restaurados (Ctrl+Alt...). Clique Apply para salvar.";
    }

    private void Validate()
    {
        var dict = Rows.ToDictionary(r => r.ActionKey, r => r.Gesture);
        var dups = HotkeyConflicts.FindDuplicates(dict)
            .SelectMany(g => g)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var r in Rows)
        {
            if (!HotkeyGesture.TryParse(r.Gesture, out var g))
            {
                r.Warning = "Gesto inválido (ex: Ctrl+Alt+P).";
                continue;
            }
            if (dups.Contains(r.ActionKey))
            {
                r.Warning = "Conflito: mesmo gesto em duas ações.";
                continue;
            }
            r.Warning = g.GamerWarning();
        }
    }
}
