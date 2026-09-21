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
            // Migração v1/v2 → v3 (diagnóstico 2026-09-21 via MusicControl.exe):
            // volume virou NumpadAdd/NumpadSub (o usuário usa o teclado numérico, não teclas de mídia),
            // guard virou Always (o uso real é com jogo fullscreen — BNSR).
            // Só migra o que ainda está com valor de default/antigo inválido; resto é preservado.
            if (cfg.Version < 3)
            {
                var d = AppConfig.Default();
                if (IsOldVolume(cfg.Shortcuts.GetValueOrDefault("volumeUp")))
                    cfg.Shortcuts["volumeUp"] = d.Shortcuts["volumeUp"];
                if (IsOldVolume(cfg.Shortcuts.GetValueOrDefault("volumeDown")))
                    cfg.Shortcuts["volumeDown"] = d.Shortcuts["volumeDown"];
                if (cfg.GuardMode == "PauseWhenFullscreen")
                    cfg.GuardMode = d.GuardMode;
                cfg.Version = 3;
            }
            // Migração v3 → v4: novos comandos seek ±5s (setas). Só adiciona o que falta.
            if (cfg.Version < 4)
            {
                var d = AppConfig.Default();
                if (!cfg.Shortcuts.ContainsKey("seekForward"))
                    cfg.Shortcuts["seekForward"] = d.Shortcuts["seekForward"];
                if (!cfg.Shortcuts.ContainsKey("seekBackward"))
                    cfg.Shortcuts["seekBackward"] = d.Shortcuts["seekBackward"];
                cfg.Version = 4;
            }
            return cfg;
        }
        catch { return AppConfig.Default(); }
    }

    private static bool IsOldVolume(string? g) =>
        string.IsNullOrWhiteSpace(g)
        || g is "+" or "-" // captura manual inválida da caixa de texto (sem suporte a numpad)
        || string.Equals(g, "VolumeUp", StringComparison.OrdinalIgnoreCase)
        || string.Equals(g, "VolumeDown", StringComparison.OrdinalIgnoreCase)
        || string.Equals(g, "Ctrl+Alt+Up", StringComparison.OrdinalIgnoreCase)
        || string.Equals(g, "Ctrl+Alt+Down", StringComparison.OrdinalIgnoreCase);

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
