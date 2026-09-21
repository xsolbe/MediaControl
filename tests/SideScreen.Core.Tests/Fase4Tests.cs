using SideScreen.Core.Hotkeys;
using SideScreen.Infrastructure.Hotkeys;

namespace SideScreen.Core.Tests;

public class Fase4Tests
{
    [Theory]
    [InlineData("Ctrl+Alt+P", true, true, false, "P")]
    [InlineData("ctrl+alt+right", true, true, false, "Right")]
    [InlineData("F1", false, false, false, "F1")]
    [InlineData("Shift+F2", false, false, true, "F2")]
    [InlineData("NumpadAdd", false, false, false, "NumpadAdd")]
    [InlineData("+", false, false, false, "NumpadAdd")]
    [InlineData("-", false, false, false, "NumpadSub")]
    [InlineData("Right", false, false, false, "Right")]
    [InlineData("Left", false, false, false, "Left")]
    public void Parse_NormalizesModifiersAndKeys(string text, bool ctrl, bool alt, bool shift, string key)
    {
        Assert.True(HotkeyGesture.TryParse(text, out var g));
        Assert.Equal(ctrl, g.Ctrl);
        Assert.Equal(alt, g.Alt);
        Assert.Equal(shift, g.Shift);
        Assert.Equal(key, g.Key);
    }

    [Fact]
    public void Parse_Invalid_ReturnsFalse()
    {
        Assert.False(HotkeyGesture.TryParse("", out _));
        Assert.False(HotkeyGesture.TryParse("Ctrl+Alt", out _)); // sem tecla principal
        Assert.False(HotkeyGesture.TryParse(null, out _));
    }

    [Fact]
    public void GamerWarning_FlagsBareKeys()
    {
        Assert.True(HotkeyGesture.TryParse("F1", out var f1));
        Assert.NotNull(f1.GamerWarning()); // aviso informa, não bloqueia (usuário pediu F1/F2)

        Assert.True(HotkeyGesture.TryParse("VolumeUp", out var media));
        Assert.Null(media.GamerWarning()); // mídia é segura (Windows lida sem roubar foco)

        Assert.True(HotkeyGesture.TryParse("Ctrl+Alt+P", out var safe));
        Assert.Null(safe.GamerWarning());
    }

    [Fact]
    public void Conflicts_DetectsDuplicates()
    {
        var dict = new Dictionary<string, string>
        {
            ["next"] = "Ctrl+Alt+Right",
            ["previous"] = "ctrl+alt+right", // mesmo gesto, case diferente
            ["playPause"] = "Ctrl+Alt+P",
        };
        var dups = HotkeyConflicts.FindDuplicates(dict);
        Assert.Single(dups);
        Assert.Contains("next", dups[0]);
        Assert.Contains("previous", dups[0]);
    }

    [Fact]
    public void KeyMapper_MapsCommonKeys()
    {
        Assert.True(KeyMapper.TryGetVk("P", out uint p) && p == 0x50);
        Assert.True(KeyMapper.TryGetVk("F1", out uint f1) && f1 == 0x70);
        Assert.True(KeyMapper.TryGetVk("Up", out uint up) && up == 0x26);
        Assert.True(KeyMapper.TryGetVk("VolumeUp", out uint vu) && vu == 0xAF);
        Assert.True(KeyMapper.TryGetVk("VolumeDown", out uint vd) && vd == 0xAE);
        Assert.True(KeyMapper.TryGetVk("NumpadAdd", out uint na) && na == 0x6B);
        Assert.True(KeyMapper.TryGetVk("NumpadSub", out uint ns) && ns == 0x6D);
        Assert.False(KeyMapper.TryGetVk("MediaPlay", out _)); // fora da tabela mínima
    }

    [Fact]
    public void DoublePress_FirstThenSecondWithinWindow()
    {
        var t = new DoublePressTracker(350);
        var now = DateTime.UtcNow;
        Assert.Equal(DoublePressTracker.Result.First, t.Press(now));
        Assert.Equal(DoublePressTracker.Result.Second, t.Press(now.AddMilliseconds(200)));
    }

    [Fact]
    public void DoublePress_ExpiredBecomesNewFirst()
    {
        var t = new DoublePressTracker(350);
        var now = DateTime.UtcNow;
        Assert.Equal(DoublePressTracker.Result.First, t.Press(now));
        // Segundo toque após a janela = novo First (não Previous acidental).
        Assert.Equal(DoublePressTracker.Result.First, t.Press(now.AddMilliseconds(900)));
    }

    [Fact]
    public void Guard_PauseWhenFullscreen_BlocksFullscreen()
    {
        var guard = new ForegroundGuard(
            foregroundHook: () => (nint)123,
            windowSizeHook: _ => (2560, 1440),
            screenSizeHook: () => (2560, 1440),
            isPlayerFocusedHook: () => false);
        Assert.False(guard.ShouldExecute(GuardModes.PauseWhenFullscreen));
        Assert.True(guard.ShouldExecute(GuardModes.Always));
    }

    [Fact]
    public void Guard_OnlyWhenPlayerFocused_RespectsFocus()
    {
        var focused = new ForegroundGuard(
            foregroundHook: () => (nint)1,
            windowSizeHook: _ => (800, 600),
            screenSizeHook: () => (2560, 1440),
            isPlayerFocusedHook: () => true);
        Assert.True(focused.ShouldExecute(GuardModes.OnlyWhenPlayerFocused));

        var notFocused = new ForegroundGuard(
            foregroundHook: () => (nint)1,
            windowSizeHook: _ => (800, 600),
            screenSizeHook: () => (2560, 1440),
            isPlayerFocusedHook: () => false);
        Assert.False(notFocused.ShouldExecute(GuardModes.OnlyWhenPlayerFocused));
    }

    [Fact]
    public void NormalizeProcess_StripsExe_IgnoresCase()
    {
        Assert.Equal("bnsr", GuardModes.NormalizeProcess("BNSR.exe"));
        Assert.Equal("bnsr", GuardModes.NormalizeProcess("bnsr"));
        Assert.Equal("", GuardModes.NormalizeProcess(null));
    }

    [Fact]
    public void Guard_OnlyListed_MatchesForegroundProcess()
    {
        ForegroundGuard InGame() => new(
            foregroundHook: () => (nint)7,
            windowSizeHook: _ => (800, 600),
            screenSizeHook: () => (2560, 1440),
            isPlayerFocusedHook: () => false,
            foregroundProcessHook: () => "BNSR");

        var guard = InGame();
        Assert.True(guard.ShouldExecute(GuardModes.OnlyListed, ["BNSR"]));
        Assert.True(guard.ShouldExecute(GuardModes.OnlyListed, ["bnsr.exe"])); // normaliza
        Assert.False(guard.ShouldExecute(GuardModes.OnlyListed, ["notepad"]));
        Assert.False(guard.ShouldExecute(GuardModes.OnlyListed, [])); // lista vazia = nada passa
        Assert.False(guard.ShouldExecute(GuardModes.OnlyListed, null));
    }

    [Fact]
    public void Guard_OnlyListed_UnknownProcess_Blocks()
    {
        var guard = new ForegroundGuard(
            foregroundHook: () => (nint)7,
            windowSizeHook: _ => (800, 600),
            screenSizeHook: () => (2560, 1440),
            isPlayerFocusedHook: () => false,
            foregroundProcessHook: () => null); // ex: processo elevado ilegível
        Assert.False(guard.ShouldExecute(GuardModes.OnlyListed, ["BNSR"]));
    }
}
