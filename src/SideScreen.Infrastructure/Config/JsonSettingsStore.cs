using System.Text.Json;
using SideScreen.Core.Config;

namespace SideScreen.Infrastructure.Config;

/// <summary>
/// Persistência em config.json (dono único: MainViewModel).
/// - Load: defaults → migrações por versão → backup se corrompido.
/// - Nunca lança para a UI.
/// </summary>
public sealed class JsonSettingsStore
{
    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = true };

    public static string DefaultDir => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SideScreen");
    public static string DefaultPath => Path.Combine(DefaultDir, "config.json");

    public JsonSettingsStore(string? dir = null)
    {
        DirPath = dir ?? DefaultDir;
        ConfigPath = Path.Combine(DirPath, "config.json");
    }

    public string DirPath { get; }
    public string ConfigPath { get; }

    public AppConfig Load()
    {
        try
        {
            if (!File.Exists(ConfigPath))
                return AppConfig.Default();
            var json = File.ReadAllText(ConfigPath);
            AppConfig? cfg;
            try
            {
                cfg = JsonSerializer.Deserialize<AppConfig>(json, JsonOpts);
            }
            catch
            {
                BackupCorrupt();
                return AppConfig.Default();
            }
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
            Directory.CreateDirectory(DirPath);
            var json = JsonSerializer.Serialize(config, JsonOpts);
            File.WriteAllText(ConfigPath, json);
            return null;
        }
        catch (Exception ex) { return ex.Message; }
    }

    private void BackupCorrupt()
    {
        try
        {
            var backup = Path.Combine(DirPath, $"config.corrupt-{DateTime.Now:yyyyMMdd-HHmmss}.json");
            File.Copy(ConfigPath, backup, overwrite: false);
        }
        catch { }
    }
}
