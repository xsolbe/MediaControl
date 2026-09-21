using System.Text.Json;
using SideScreen.Core.Config;

namespace SideScreen.Infrastructure.Config;

/// <summary>
/// Persistência mínima em %AppData%\SideScreen\config.json (Fase 4: só atalhos/hotkeys; Fase 6: tudo + migração).
/// Nunca lança para a UI — retorna Default em erro.
/// </summary>
public sealed class JsonSettingsStore
{
    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = true };

    public static string ConfigDir => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SideScreen");
    public static string ConfigPath => Path.Combine(ConfigDir, "config.json");

    public AppConfig Load()
    {
        try
        {
            if (!File.Exists(ConfigPath))
                return AppConfig.Default();
            var json = File.ReadAllText(ConfigPath);
            var cfg = JsonSerializer.Deserialize<AppConfig>(json, JsonOpts);
            return cfg ?? AppConfig.Default();
        }
        catch { return AppConfig.Default(); }
    }

    public string? Save(AppConfig config)
    {
        try
        {
            Directory.CreateDirectory(ConfigDir);
            var json = JsonSerializer.Serialize(config, JsonOpts);
            File.WriteAllText(ConfigPath, json);
            return null;
        }
        catch (Exception ex) { return ex.Message; }
    }
}
