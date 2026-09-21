using System.Runtime.InteropServices;
using SideScreen.Infrastructure.Windows;

namespace SideScreen.Infrastructure.Display;

/// <summary>
/// Move one-shot do player para o monitor escolhido (Closes #1).
/// Explícito, sem loop: centraliza preservando o tamanho; maximizado/fullscreen não move.
/// </summary>
public static class MonitorMover
{
    public static string MoveTo(nint hWnd, DisplayMonitor target)
    {
        if (hWnd == nint.Zero || !NativeMethods.IsWindow(hWnd))
            return "Player fechado — nada a mover.";
        try
        {
            var wp = new NativeMethods.WINDOWPLACEMENT { length = Marshal.SizeOf<NativeMethods.WINDOWPLACEMENT>() };
            if (NativeMethods.GetWindowPlacement(hWnd, ref wp) && wp.showCmd == NativeMethods.SwShowMaximized)
                return "Maximizado — desmaximize para mover.";

            if (!NativeMethods.GetWindowRect(hWnd, out var rc))
                return "Janela inacessível no momento.";

            int w = rc.Right - rc.Left, hgt = rc.Bottom - rc.Top;
            if (CoversMonitor(rc.Left, rc.Top, w, hgt, target))
                return "Fullscreen detectado — saia do fullscreen para mover.";

            int nx = w >= target.Width ? target.Left : target.Left + (target.Width - w) / 2;
            int ny = hgt >= target.Height ? target.Top : target.Top + (target.Height - hgt) / 2;
            bool ok = NativeMethods.SetWindowPos(hWnd, nint.Zero, nx, ny, 0, 0,
                NativeMethods.SwpNoSize | NativeMethods.SwpNoZOrder | NativeMethods.SwpNoActivate);
            return ok ? $"Player movido para {target.Label.Split('—')[0].Trim()}." : "Falha ao mover (acesso negado?).";
        }
        catch (Exception ex)
        {
            return $"Erro ao mover: {ex.Message}";
        }
    }

    /// <summary>Centro puro (testável, sem Win32).</summary>
    public static (int x, int y) Center(int w, int h, int toLeft, int toTop, int toWidth, int toHeight) =>
        (w >= toWidth ? toLeft : toLeft + (toWidth - w) / 2,
         h >= toHeight ? toTop : toTop + (toHeight - h) / 2);

    private static bool CoversMonitor(int x, int y, int w, int h, DisplayMonitor m, double tolerance = 0.98) =>
        w >= m.Width * tolerance && h >= m.Height * tolerance
        && Math.Abs(x - m.Left) <= 4 && Math.Abs(y - m.Top) <= 4;
}
