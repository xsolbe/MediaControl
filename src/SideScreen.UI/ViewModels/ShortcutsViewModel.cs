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

    public ShortcutEdit(string actionKey, string label, string description, string gesture)
    {
        ActionKey = actionKey;
        Label = label;
        Description = description;
        _gesture = gesture;
    }

    public string ActionKey { get; }
    public string Label { get; }
    public string Description { get; }

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

public sealed class ProcessOption : ObservableObject
{
    private bool _selected;

    public ProcessOption(string name, string title, bool selected)
    {
        Name = name;
        Title = title;
        _selected = selected;
    }

    public string Name { get; }
    public string Title { get; }

    public bool Selected
    {
        get => _selected;
        set => Set(ref _selected, value);
    }
}

/// <summary>
/// Shortcuts — captura, conflitos, warnings gamer, Apply/Restaurar, guard + double-press.
/// Usa o HotkeyService COMPARTILHADO (dono: MainViewModel) — o registro acontece no startup do app.
/// </summary>
public sealed class ShortcutsViewModel : ObservableObject
{
    private static readonly (string Key, string Label, string Description)[] Order =
    [
        ("volumeUp", "Aumentar volume", "Sobe o volume do PotPlayer (Numpad +)"),
        ("volumeDown", "Diminuir volume", "Desce o volume do PotPlayer (Numpad -)"),
        ("playPause", "Play / Pause", "Pausa ou retoma o vídeo (F2 — mostra aviso gamer; guard pausa em fullscreen)"),
        ("next", "Próximo vídeo", "Avança para o próximo vídeo da playlist/pasta (F1)"),
        ("previous", "Vídeo anterior", "Volta ao vídeo anterior (F1 pressionado 2x — double-press)"),
        ("seekForward", "Avançar 5 segundos", "Pula +5s no vídeo atual (seta →)"),
        ("seekBackward", "Voltar 5 segundos", "Volta -5s no vídeo atual (seta ←)"),
    ];

    private readonly JsonSettingsStore _store;
    private readonly HotkeyService _service;
    private readonly Action _onConfigSaved;
    private bool _enableGlobal;
    private string _guardMode = Core.Hotkeys.GuardModes.Always;
    private bool _doublePress;
    private int _doubleWindow = 350;
    private string _status = "";

    public ShortcutsViewModel(HotkeyService service, JsonSettingsStore store, Action onConfigSaved)
    {
        _service = service;
        _store = store;
        _onConfigSaved = onConfigSaved;
        _service.Triggered += msg => Status = msg;

        var cfg = _store.Load();
        Rows = new ObservableCollection<ShortcutEdit>(Order.Select(o =>
            new ShortcutEdit(o.Key, o.Label, o.Description, cfg.Shortcuts.TryGetValue(o.Key, out var g) ? g : AppConfig.Default().Shortcuts[o.Key])));

        _enableGlobal = cfg.EnableGlobalHotkeys;
        _guardMode = Core.Hotkeys.GuardModes.All.Contains(cfg.GuardMode) ? cfg.GuardMode : Core.Hotkeys.GuardModes.Always;
        _doublePress = cfg.DoublePressEnabled;
        _doubleWindow = Math.Clamp(cfg.DoublePressWindowMs, 200, 500);

        foreach (var r in Rows)
            r.PropertyChanged += (_, _) => Validate();

        ApplyCommand = new RelayCommand(Apply);
        RestoreDefaultsCommand = new RelayCommand(RestoreDefaults);
        RefreshProcessesCommand = new RelayCommand(() => RefreshProcesses());

        RefreshProcesses();
        Validate();
        Status = cfg.EnableGlobalHotkeys
            ? "Globais ATIVADAS — registradas ao abrir o app (ver rodapé: Hotkeys ON)."
            : "Globais DESLIGADAS. Marque a caixa e clique Apply.";
        Status += ConflictNote();
    }

    private static string ConflictNote()
    {
        var notes = new List<string>();
        try
        {
            if (System.Diagnostics.Process.GetProcessesByName("MusicControl").Length > 0)
                notes.Add("MusicControl.exe rodando: o AHK engole F1/F2/Numpad antes do SideScreen — feche-o para testar.");
            if (System.Diagnostics.Process.GetProcessesByName("SideScreen.UI").Length > 1)
                notes.Add("outra instância do SideScreen aberta: só uma registra os hotkeys.");
        }
        catch { }
        return notes.Count == 0 ? "" : " Atenção: " + string.Join(" ", notes);
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
        set { if (Set(ref _doublePress, value)) Validate(); }
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

    public string ConfigPath => _store.ConfigPath;

    public RelayCommand ApplyCommand { get; }
    public RelayCommand RestoreDefaultsCommand { get; }
    public RelayCommand RefreshProcessesCommand { get; }

    /// <summary>Processos com janela (candidatos ao escopo). Marcados = AllowedProcesses do config.</summary>
    public ObservableCollection<ProcessOption> Processes { get; } = [];

    public void RefreshProcesses()
    {
        var cfg = _store.Load();
        var allowed = cfg.AllowedProcesses.Select(Core.Hotkeys.GuardModes.NormalizeProcess).ToHashSet();
        var seen = new HashSet<string>();
        var list = new List<ProcessOption>();

        foreach (var p in System.Diagnostics.Process.GetProcesses())
        {
            string name;
            string title;
            try
            {
                if (p.MainWindowHandle == nint.Zero) continue;
                name = p.ProcessName;
                title = p.MainWindowTitle;
            }
            catch { continue; }
            finally { try { p.Dispose(); } catch { } }

            var norm = Core.Hotkeys.GuardModes.NormalizeProcess(name);
            if (norm.Length == 0 || !seen.Add(norm)) continue;
            list.Add(new ProcessOption(name, title, allowed.Contains(norm)));
        }

        Processes.Clear();
        foreach (var item in list.OrderBy(x => x.Name))
            Processes.Add(item);
    }

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
        cfg.AllowedProcesses = Processes.Where(p => p.Selected).Select(p => p.Name).ToList();

        var err = _store.Save(cfg);
        if (err != null)
        {
            Status = $"Erro ao salvar: {err}";
            return;
        }

        _onConfigSaved(); // MainViewModel relê o config e (re)liga o serviço
        Status = !_enableGlobal
            ? "Salvo em config.json. Globais DESLIGADAS — controle pelos botões do Dashboard."
            : $"Salvo e aplicado (ver rodapé: Hotkeys ON).{_serviceErrors()}";
        Status += ConflictNote();
    }

    private string _serviceErrors() =>
        _service.Errors.Count == 0 ? "" : $" Parcial: {string.Join("; ", _service.Errors)}";

    private void RestoreDefaults()
    {
        var d = AppConfig.Default();
        foreach (var r in Rows)
            r.Gesture = d.Shortcuts[r.ActionKey];
        foreach (var p in Processes)
            p.Selected = false;
        EnableGlobal = false;
        SelectedGuardMode = Core.Hotkeys.GuardModes.Always;
        DoublePressEnabled = true;
        DoublePressWindowMs = 350;
        Validate();
        Status = "Padrões restaurados (Volume+/-, F2, F1, F1 x2). Clique Apply para salvar.";
    }

    private void Validate()
    {
        var dict = Rows.ToDictionary(r => r.ActionKey, r => r.Gesture);
        var dups = HotkeyConflicts.FindDuplicates(dict)
            .SelectMany(g => g)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        // F1 x2 = anterior por design: next e previous compartilham a tecla quando o double-press está ligado.
        if (_doublePress
            && dict.TryGetValue("next", out var ng) && dict.TryGetValue("previous", out var pg)
            && HotkeyGesture.TryParse(ng, out var n) && HotkeyGesture.TryParse(pg, out var p)
            && n.Canonical() == p.Canonical())
        {
            dups.Remove("next");
            dups.Remove("previous");
        }

        foreach (var r in Rows)
        {
            if (!HotkeyGesture.TryParse(r.Gesture, out var g))
            {
                r.Warning = "Gesto inválido (ex: F2 ou Ctrl+Alt+P).";
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
