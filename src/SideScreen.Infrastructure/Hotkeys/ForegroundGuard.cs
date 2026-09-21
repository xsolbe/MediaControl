using SideScreen.Core.Hotkeys;
using SideScreen.Infrastructure.Players;

namespace SideScreen.Infrastructure.Hotkeys;

/// <summary>
/// Decide se um hotkey global pode executar agora (requisito: não interferir em jogos).
/// - Always: executa sempre (opt-in explícito).
/// - PauseWhenFullscreen (default): se a janela em foco ocupa a tela toda, ignora.
/// - OnlyWhenPlayerFocused: só se o foco estiver no PotPlayer.
/// Injetável para testes (foreground + bounds + isPlayer como funções).
/// </summary>
public sealed class ForegroundGuard
{
    private readonly Func<nint> _foreground;
    private readonly Func<nint, (int w, int h)?> _windowSize;
    private readonly Func<(int w, int h)> _screenSize;
    private readonly Func<bool> _isPlayerFocused;

    public ForegroundGuard() : this(
        Windows.NativeMethods.GetForegroundWindow,
        GetRect,
        PrimaryScreenSize,
        IsPotPlayerForeground)
    {
    }

    public ForegroundGuard(
        Func<nint> foregroundHook,
        Func<nint, (int w, int h)?> windowSizeHook,
        Func<(int w, int h)> screenSizeHook,
        Func<bool> isPlayerFocusedHook)
    {
        _foreground = foregroundHook;
        _windowSize = windowSizeHook;
        _screenSize = screenSizeHook;
        _isPlayerFocused = isPlayerFocusedHook;
    }

    public bool ShouldExecute(string guardMode)
    {
        return guardMode switch
        {
            GuardModes.Always => true,
            GuardModes.OnlyWhenPlayerFocused => Safe(_isPlayerFocused),
            _ => !IsFullscreenForeground(), // PauseWhenFullscreen (default)
        };
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
