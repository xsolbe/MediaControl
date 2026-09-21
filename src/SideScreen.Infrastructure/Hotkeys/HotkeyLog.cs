namespace SideScreen.Infrastructure.Hotkeys;

/// <summary>
/// Log de diagnóstico dos hotkeys em %AppData%\SideScreen\hotkeys.log.
/// Registra SOMENTE eventos dos nossos próprios hotkeys (registro, disparo, guard) —
/// NÃO é keylogger (não enxerga nenhuma outra tecla do sistema).
/// Rotaciona ao passar de ~100KB. Nunca lança.
/// </summary>
public static class HotkeyLog
{
    public static string Path { get; } = System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "SideScreen", "hotkeys.log");

    public static void Append(string message)
    {
        try
        {
            var dir = System.IO.Path.GetDirectoryName(Path);
            if (dir != null) Directory.CreateDirectory(dir);
            var info = new FileInfo(Path);
            if (info.Exists && info.Length > 100_000)
                File.WriteAllText(Path, $"[{DateTime.Now:O}] log rotacionado{Environment.NewLine}");
            File.AppendAllText(Path, $"[{DateTime.Now:O}] {message}{Environment.NewLine}");
        }
        catch { }
    }
}
