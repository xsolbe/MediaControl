using System.IO;
using System.Text.Json;
using System.Windows.Input;

namespace MediaControl.Models;

internal static class ControlActions
{
    public const string Previous = "previous";
    public const string Next = "next";
    public const string PlayPause = "playPause";
    public const string VolumeUp = "volumeUp";
    public const string VolumeDown = "volumeDown";
    public const string SeekBackward = "seekBackward";
    public const string SeekForward = "seekForward";
    public const string PauseHotkeys = "pauseHotkeys";

    public static readonly (string Id, string Label)[] All =
    [
        (Next, "Próxima faixa"),
        (PlayPause, "Pausar / continuar"),
        (VolumeUp, "Volume +"),
        (VolumeDown, "Volume -"),
        (SeekBackward, "Voltar 5s"),
        (SeekForward, "Avançar 5s"),
    ];

    public static readonly (string Id, string Label)[] Configurable =
    [
        .. All,
        (PauseHotkeys, "Pausar / reativar atalhos"),
    ];
}

internal sealed class HotkeyGesture
{
    public uint Modifiers { get; set; }
    public uint VirtualKey { get; set; }

    public HotkeyGesture() { }

    public HotkeyGesture(uint modifiers, uint virtualKey)
    {
        Modifiers = modifiers;
        VirtualKey = virtualKey;
    }

    public HotkeyGesture Copy() => new(Modifiers, VirtualKey);

    public bool IsValid => Modifiers <= 0xF && VirtualKey is > 0 and <= 0xFE
        && VirtualKey is not (0x10 or 0x11 or 0x12 or 0x5B or 0x5C);

    public string Display
    {
        get
        {
            var parts = new List<string>();
            if ((Modifiers & 0x2) != 0) parts.Add("Ctrl");
            if ((Modifiers & 0x1) != 0) parts.Add("Alt");
            if ((Modifiers & 0x4) != 0) parts.Add("Shift");
            if ((Modifiers & 0x8) != 0) parts.Add("Win");
            parts.Add(VirtualKey switch
            {
                >= 0x70 and <= 0x87 => $"F{VirtualKey - 0x6F}",
                0x6B => "Numpad +",
                0x6D => "Numpad -",
                0x25 => "←",
                0x27 => "→",
                >= 0x41 and <= 0x5A => ((char)VirtualKey).ToString(),
                >= 0x30 and <= 0x39 => ((char)VirtualKey).ToString(),
                _ => KeyInterop.KeyFromVirtualKey((int)VirtualKey) is var key && key != Key.None
                    ? key.ToString()
                    : $"VK {VirtualKey:X2}",
            });
            return string.Join(" + ", parts);
        }
    }

    public static HotkeyGesture? FromKeyEvent(KeyEventArgs e)
    {
        Key key = e.Key == Key.System ? e.SystemKey : e.Key;
        if (key is Key.LeftCtrl or Key.RightCtrl or Key.LeftAlt or Key.RightAlt
            or Key.LeftShift or Key.RightShift or Key.LWin or Key.RWin or Key.System)
            return null;

        int vk = KeyInterop.VirtualKeyFromKey(key);
        if (vk <= 0 || vk > 0xFE)
            return null;

        uint modifiers = 0;
        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Alt)) modifiers |= 0x1;
        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control)) modifiers |= 0x2;
        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift)) modifiers |= 0x4;
        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Windows)) modifiers |= 0x8;
        return new HotkeyGesture(modifiers, (uint)vk);
    }
}

internal sealed class AppSettings
{
    public int Version { get; set; } = 6;
    public Dictionary<string, HotkeyGesture> Bindings { get; set; } = new();
    public bool OnlyListedPrograms { get; set; }
    public List<string> AllowedExecutables { get; set; } = [];
    public string Language { get; set; } = "en";
    public bool HotkeysPaused { get; set; }

    public AppSettings Copy() => new()
    {
        Version = 6,
        Bindings = Bindings.ToDictionary(pair => pair.Key, pair => pair.Value.Copy()),
        OnlyListedPrograms = OnlyListedPrograms,
        AllowedExecutables = [.. AllowedExecutables],
        Language = Language,
        HotkeysPaused = HotkeysPaused,
    };

    public static AppSettings CreateDefault() => new()
    {
        Bindings = new Dictionary<string, HotkeyGesture>
        {
            [ControlActions.Next] = new(0, 0x70),
            [ControlActions.PlayPause] = new(0, 0x71),
            [ControlActions.VolumeUp] = new(0, 0x6B),
            [ControlActions.VolumeDown] = new(0, 0x6D),
            [ControlActions.SeekBackward] = new(0, 0x25),
            [ControlActions.SeekForward] = new(0, 0x27),
            [ControlActions.PauseHotkeys] = new(0, 0x78),
        },
    };
}

internal static class SettingsStore
{
    public static string FilePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "MediaControl", "settings.json");

    public static AppSettings Load()
    {
        var settings = AppSettings.CreateDefault();
        try
        {
            if (!File.Exists(FilePath))
                return settings;

            var saved = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(FilePath));
            if (saved?.Version is not (2 or 3 or 4 or 5 or 6) || saved.Bindings is null)
                return settings;

            foreach (var (id, _) in ControlActions.Configurable)
            {
                if (id == ControlActions.PauseHotkeys && saved.Version < 6)
                    continue;
                if (saved.Bindings.TryGetValue(id, out var binding) && binding?.IsValid == true
                    && (saved.Version >= 6 || binding.Modifiers != 0 || binding.VirtualKey != 0x78))
                    settings.Bindings[id] = binding.Copy();
            }
            if (settings.Bindings.Values.Select(binding => (binding.Modifiers, binding.VirtualKey)).Distinct().Count()
                != ControlActions.Configurable.Length)
                settings.Bindings = AppSettings.CreateDefault().Bindings;
            if (saved.Version >= 3)
            {
                settings.OnlyListedPrograms = saved.OnlyListedPrograms;
                settings.AllowedExecutables = (saved.AllowedExecutables ?? [])
                    .Where(path => !string.IsNullOrWhiteSpace(path) && Path.IsPathFullyQualified(path))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
            }
            settings.Language = saved.Language == "pt-BR" ? "pt-BR" : "en";
            settings.HotkeysPaused = saved.Version >= 5 && saved.HotkeysPaused;
        }
        catch (Exception)
        {
            // Um arquivo inválido não impede os atalhos padrão de funcionar.
        }
        return settings;
    }

    public static void Save(AppSettings settings)
    {
        string path = FilePath;
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        string temporary = path + ".tmp";
        File.WriteAllText(temporary, JsonSerializer.Serialize(settings, new JsonSerializerOptions
        {
            WriteIndented = true,
        }));
        File.Move(temporary, path, overwrite: true);
    }
}
