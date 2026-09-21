using SideScreen.Core.Hotkeys;
using SideScreen.Infrastructure.Players;

namespace SideScreen.Infrastructure.Hotkeys;

/// <summary>
/// Decide se um hotkey global pode executar agora (requisito: não interferir em jogos).
/// - Always: executa sempre (opt-in explícito).
/// - PauseWhenFullscreen (default): se a janela em foco ocupa a tela toda, ignora.
/// - OnlyWhenPlayerFocused: só se o foco estiver no PotPlayer.
/// - OnlyListed: só se o processo em foco estiver na allowlist (ex: só BNSR).
/// Injetável para testes.
/// </summary>
public sealed class ForegroundGuard
{
    private readonly Func<nint> _foreground;
    private readonly Func<nint, (int w, int h)?> _windowSize;
    private readonly Func<(int w, int h)> _screenSize;
    private readonly Func<bool> _isPlayerFocused;
    private readonly Func<string?> _foregroundProcess;

    public ForegroundGuard() : this(
        Windows.NativeMethods.GetForegroundWindow,
        GetRect,
        PrimaryScreenSize,
        IsPotPlayerForeground,
        ForegroundProcessName)
    {
    }

    public ForegroundGuard(
        Func<nint> foregroundHook,
        Func<nint, (int w, int h)?> windowSizeHook,
        Func<(int w, int h)> screenSizeHook,
        Func<bool> isPlayerFocusedHook,
        Func<string?>? foregroundProcessHook = null)
    {
        _foreground = foregroundHook;
        _windowSize = windowSizeHook;
        _screenSize = screenSizeHook;
        _isPlayerFocused = isPlayerFocusedHook;
        _foregroundProcess = foregroundProcessHook ?? (() => null);
    }

    public bool ShouldExecute(string guardMode) => ShouldExecute(guardMode, null);

    public bool ShouldExecute(string guardMode, IEnumerable<string>? allowedProcesses)
    {
        return guardMode switch
        {
            GuardModes.Always => true,
            GuardModes.OnlyWhenPlayerFocused => Safe(_isPlayerFocused),
            GuardModes.OnlyListed => IsForegroundListed(allowedProcesses),
            _ => !IsFullscreenForeground(), // PauseWhenFullscreen (default)
        };
    }

    /// <summary>True se o processo em foco está na allowlist. Nome desconhecido = bloqueia (fail closed).</summary>
    public bool IsForegroundListed(IEnumerable<string>? allowed)
    {
        try
        {
            var set = (allowed ?? []).Select(Core.Hotkeys.GuardModes.NormalizeProcess)
                .Where(s => s.Length > 0).ToHashSet();
            if (set.Count == 0) return false; // lista vazia = nada passa (não vire Always sem querer)
            var fg = SafeName(_foregroundProcess);
            if (string.IsNullOrEmpty(fg)) return false;
            return set.Contains(Core.Hotkeys.GuardModes.NormalizeProcess(fg));
        }
        catch { return false; }
    }

    public bool IsFullscreenForeground()
    {
        try
        {
            var fg = _foreground();
            if (fg == nint.Zero) return false;
            var win = _windowSize(fg);
            if (win is null) return false;
            var screen = _screenSize();
            // Fullscreen (ou borderless cobrindo tudo): tolerância de 2px para bordas.
            return Math.Abs(win.Value.w - screen.w) <= 2 && Math.Abs(win.Value.h - screen.h) <= 2;
        }
        catch { return false; }
    }

    private static bool Safe(Func<bool> f)
    {
        try { return f(); }
        catch { return false; }
    }

    private static string? SafeName(Func<string?> f)
    {
        try { return f(); }
        catch { return null; }
    }

    private static string? ForegroundProcessName()
    {
        try
        {
            var fg = Windows.NativeMethods.GetForegroundWindow();
            if (fg == nint.Zero) return null;
            Windows.NativeMethods.GetWindowThreadProcessId(fg, out uint pid);
            using var p = System.Diagnostics.Process.GetProcessById((int)pid);
            return p.ProcessName; // ex: "BNSR" (GetProcessById funciona até p/ processo elevado)
        }
        catch { return null; }
    }

    private static (int w, int h)? GetRect(nint hWnd)
    {
        try
        {
            if (!Windows.NativeMethods.GetWindowRect(hWnd, out var r)) return null;
            return (r.Width, r.Height);
        }
        catch { return null; }
    }

    private static (int w, int h) PrimaryScreenSize()
    {
        try
        {
            // SM_CXSCREEN=0, SM_CYSCREEN=1. Sem dependência de WPF/WinForms na Infrastructure.
            int w = Windows.NativeMethods.GetSystemMetrics(0);
            int h = Windows.NativeMethods.GetSystemMetrics(1);
            return (w > 0 ? w : 1920, h > 0 ? h : 1080);
        }
        catch { return (1920, 1080); }
    }

    private static bool IsPotPlayerForeground()
    {
        try
        {
            var fg = Windows.NativeMethods.GetForegroundWindow();
            if (fg == nint.Zero) return false;
            var pot = Windows.WindowFinder.FindPotPlayer();
            return pot != nint.Zero && fg == pot;
        }
        catch { return false; }
    }
}
