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
            if (cfg is null)
                return AppConfig.Default();
            // Migração v1 → v2: padrões viraram os comandos do usuário (Volume+/-, F2, F1, F1 x2).
            // Quem já salvou na v1 recebe os novos padrões; personalizações manuais são refeitas na tela.
            if (cfg.Version < 2)
            {
                var d = AppConfig.Default();
                cfg.Shortcuts = d.Shortcuts;
                cfg.DoublePressEnabled = d.DoublePressEnabled;
                cfg.DoublePressWindowMs = d.DoublePressWindowMs;
                cfg.Version = 2;
            }
            return cfg;
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
