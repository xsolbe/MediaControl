using MediaControl.Core.Config;
using MediaControl.Infrastructure.Config;

namespace MediaControl.Core.Tests;

/// <summary>Fase 6: store — roundtrip, corrompido com backup, migrações. (VMs testadas via build + manual.)</summary>
public class Fase6Tests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "sidescreen-tests-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        try { Directory.Delete(_dir, recursive: true); } catch { }
    }

    [Fact]
    public void AppConfig_Defaults_AppearanceDark_LanguageEn()
    {
        var cfg = AppConfig.Default();
        Assert.Equal("Dark", cfg.Appearance);
        Assert.Equal("en", cfg.Language);
    }

    [Fact]
    public void Roundtrip_PreservesAllFields()
    {
        var store = new JsonSettingsStore(_dir);
        var cfg = AppConfig.Default();
        cfg.SelectedPlayerId = "mpc-hc";
        cfg.ShuffleEnabled = true;
        cfg.Shortcuts["playPause"] = "F2";
        cfg.AllowedProcesses.Add("BNSR");

        Assert.Null(store.Save(cfg));
        var back = store.Load();

        Assert.Equal("mpc-hc", back.SelectedPlayerId);
        Assert.True(back.ShuffleEnabled);
        Assert.Equal("F2", back.Shortcuts["playPause"]);
        Assert.Contains("BNSR", back.AllowedProcesses);
    }

    [Fact]
    public void MissingFile_ReturnsDefaults()
    {
        var store = new JsonSettingsStore(_dir);
        var cfg = store.Load();
        Assert.Equal(AppConfig.Default().SelectedPlayerId, cfg.SelectedPlayerId);
    }

    [Fact]
    public void CorruptFile_BackupAndDefaults()
    {
        Directory.CreateDirectory(_dir);
        File.WriteAllText(Path.Combine(_dir, "config.json"), "{json quebrado!!!");

        var store = new JsonSettingsStore(_dir);
        var cfg = store.Load();

        Assert.Equal(AppConfig.Default().GuardMode, cfg.GuardMode); // defaults, sem lançar
        Assert.NotEmpty(Directory.GetFiles(_dir, "config.corrupt-*.json")); // backup criado
    }

    [Fact]
    public void MigrationV1_FillsSeekKeys()
    {
        Directory.CreateDirectory(_dir);
        File.WriteAllText(Path.Combine(_dir, "config.json"),
            """{"Version":1,"SelectedPlayerId":"potplayer","ShuffleEnabled":false,"SelectedMonitorIndex":1,"LockPlayerToMonitor":false,"Shortcuts":{"volumeUp":"NumpadAdd","volumeDown":"NumpadSub","playPause":"F2","next":"F1","previous":"F1"},"EnableGlobalHotkeys":true,"GuardMode":"Always","DoublePressEnabled":true,"DoublePressWindowMs":350,"AllowedProcesses":[]}""");

        var cfg = new JsonSettingsStore(_dir).Load();

        Assert.Equal("Right", cfg.Shortcuts["seekForward"]);
        Assert.Equal("Left", cfg.Shortcuts["seekBackward"]);
        Assert.Equal(4, cfg.Version);
        Assert.Equal("NumpadAdd", cfg.Shortcuts["volumeUp"]); // personalizado preservado
    }
}
