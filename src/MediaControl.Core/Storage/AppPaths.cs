namespace MediaControl.Core.Storage;

/// <summary>
/// Caminhos de dados do MediaControl. Nome novo: %AppData%\MediaControl.
/// Migra config.json do legado %AppData%\SideScreen uma única vez (sem apagar o legado).
/// </summary>
public static class AppPaths
{
    public static string DataDir => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MediaControl");
    public static string LegacyDir => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SideScreen");
    public static string ConfigPath => Path.Combine(DataDir, "config.json");
    public static string LogPath => Path.Combine(DataDir, "hotkeys.log");
    public static string CrashPath => Path.Combine(DataDir, "crash.log");

    public static void EnsureMigrated()
    {
        try
        {
            if (File.Exists(ConfigPath)) return;
            var legacy = Path.Combine(LegacyDir, "config.json");
            if (!File.Exists(legacy)) return;
            Directory.CreateDirectory(DataDir);
            File.Copy(legacy, ConfigPath, overwrite: false);
        }
        catch { }
    }
}
