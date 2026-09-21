namespace SideScreen.Infrastructure.Hotkeys;

/// <summary>
/// Mapeia nome canônico da tecla (de HotkeyGesture) para Virtual-Key (Win32).
/// Tabela mínima e explícita — sem depender de WPF na Infrastructure.
/// </summary>
public static class KeyMapper
{
    public static bool TryGetVk(string key, out uint vk)
    {
        vk = 0;
        if (string.IsNullOrWhiteSpace(key)) return false;

        // Letras A-Z
        if (key.Length == 1)
        {
            char c = char.ToUpperInvariant(key[0]);
            if (c is >= 'A' and <= 'Z') { vk = (uint)c; return true; }
            if (c is >= '0' and <= '9') { vk = (uint)c; return true; }
        }

        // F1-F24: VK_F1=0x70
        if (key.Length is 2 or 3 && (key[0] == 'F' || key[0] == 'f') && int.TryParse(key[1..], out int f) && f is >= 1 and <= 24)
        {
            vk = (uint)(0x70 + f - 1);
            return true;
        }

        vk = key.ToLowerInvariant() switch
        {
            "up" => 0x26,
            "down" => 0x28,
            "left" => 0x25,
            "right" => 0x27,
            "space" => 0x20,
            "esc" => 0x1B,
            "tab" => 0x09,
            "enter" or "return" => 0x0D,
            "insert" => 0x2D,
            "delete" or "del" => 0x2E,
            "home" => 0x24,
            "end" => 0x23,
            "pageup" or "pgup" => 0x21,
            "pagedown" or "pgdn" => 0x22,
            _ => 0,
        };
        return vk != 0;
    }

    public static uint Modifiers(Core.Hotkeys.HotkeyGesture g)
    {
        uint m = 0;
        if (g.Alt) m |= Windows.NativeMethods.ModAlt;
        if (g.Ctrl) m |= Windows.NativeMethods.ModControl;
        if (g.Shift) m |= Windows.NativeMethods.ModShift;
        if (g.Win) m |= Windows.NativeMethods.ModWin;
        // RegisterHotKey exige ao menos... na verdade permite 0 (tecla pura) — permitimos, com warning na UI.
        return m;
    }
}
