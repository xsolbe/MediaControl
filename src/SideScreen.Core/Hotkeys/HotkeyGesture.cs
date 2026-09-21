namespace SideScreen.Core.Hotkeys;

/// <summary>
/// Guard modes — quando um hotkey global pode executar (requisito: não interferir em jogos).
/// </summary>
public static class GuardModes
{
    public const string Always = "Always";
    public const string PauseWhenFullscreen = "PauseWhenFullscreen";
    public const string OnlyWhenPlayerFocused = "OnlyWhenPlayerFocused";

    public static readonly string[] All = [Always, PauseWhenFullscreen, OnlyWhenPlayerFocused];
}

/// <summary>
/// Gesto normalizado: modificadores + tecla principal. Ex: "Ctrl+Alt+P".
/// Parsing puro (sem Win32) — testável.
/// </summary>
public sealed record HotkeyGesture(bool Ctrl, bool Alt, bool Shift, bool Win, string Key)
{
    public static bool TryParse(string? text, out HotkeyGesture gesture)
    {
        gesture = new(false, false, false, false, "");
        if (string.IsNullOrWhiteSpace(text))
            return false;

        bool ctrl = false, alt = false, shift = false, win = false;
        string? key = null;

        foreach (var raw in text.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var p = raw.ToLowerInvariant() switch
            {
                "ctrl" or "control" => "ctrl",
                "alt" => "alt",
                "shift" => "shift",
                "win" or "windows" or "super" => "win",
                _ => raw,
            };
            switch (p)
            {
                case "ctrl": ctrl = true; break;
                case "alt": alt = true; break;
                case "shift": shift = true; break;
                case "win": win = true; break;
                default: key = raw; break; // última parte não-modificadora vence
            }
        }

        if (string.IsNullOrWhiteSpace(key))
            return false;

        gesture = new(ctrl, alt, shift, win, NormalizeKey(key!));
        return true;
    }

    public override string ToString()
    {
        var parts = new List<string>();
        if (Ctrl) parts.Add("Ctrl");
        if (Alt) parts.Add("Alt");
        if (Shift) parts.Add("Shift");
        if (Win) parts.Add("Win");
        parts.Add(Key);
        return string.Join("+", parts);
    }

    /// <summary>Chave canônica para detecção de conflitos (case-insensitive, ordem fixa).</summary>
    public string Canonical() => ToString().ToLowerInvariant();

    public bool HasAnyModifier() => Ctrl || Alt || Shift || Win;

    /// <summary>
    /// Aviso gamer: tecla sozinha (F1/F2/letras/setas sem modificador) como global é perigoso.
    /// Retorna mensagem de aviso ou null se seguro.
    /// </summary>
    public string? GamerWarning()
    {
        if (!HasAnyModifier())
        {
            if (Key.Equals("F1", StringComparison.OrdinalIgnoreCase) || Key.Equals("F2", StringComparison.OrdinalIgnoreCase))
                return "F1/F2 sozinho como global rouba a tecla de jogos — evite.";
            return $"'{Key}' sozinho como global interfere em jogos/programas — prefira Ctrl+Alt+...";
        }
        return null;
    }

    private static string NormalizeKey(string k)
    {
        // Normaliza setas e nomes comuns para exibição consistente.
        return k.ToLowerInvariant() switch
        {
            "up" or "arrowup" => "Up",
            "down" or "arrowdown" => "Down",
            "left" or "arrowleft" => "Left",
            "right" or "arrowright" => "Right",
            "space" or "spacebar" => "Space",
            "esc" or "escape" => "Esc",
            _ when k.Length == 1 => k.ToUpperInvariant(),
            _ when k.StartsWith("F", StringComparison.OrdinalIgnoreCase) && k.Length <= 3 => k.ToUpperInvariant(),
            _ => k,
        };
    }
}

/// <summary>Validação de conflitos entre atalhos (gestos duplicados).</summary>
public static class HotkeyConflicts
{
    /// <summary>Retorna grupos de actions que compartilham o mesmo gesto canônico.</summary>
    public static List<List<string>> FindDuplicates(IDictionary<string, string> actionToGesture)
    {
        var byGesture = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var (action, gestureText) in actionToGesture)
        {
            if (!HotkeyGesture.TryParse(gestureText, out var g))
                continue;
            var canon = g.Canonical();
            if (!byGesture.TryGetValue(canon, out var list))
                byGesture[canon] = list = [];
            list.Add(action);
        }
        return byGesture.Values.Where(v => v.Count > 1).ToList();
    }
}
