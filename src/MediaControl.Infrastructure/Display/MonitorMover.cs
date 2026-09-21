using System.Runtime.InteropServices;
using MediaControl.Infrastructure.Windows;

namespace MediaControl.Infrastructure.Display;

/// <summary>
/// Move one-shot do player para o monitor escolhido (Closes #1).
/// Com memória por monitor: ao sair de um monitor, guarda o lugar; ao voltar,
/// restaura o ponto exato (limitado aos limites atuais). Sem memória: centraliza.
/// Explícito, sem loop; maximizado/fullscreen não move.
/// </summary>
public static class MonitorMover
{
    private static readonly object _gate = new();
    private static readonly Dictionary<string, (int x, int y, int w, int h)> _memory = new();

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
            var monitors = MonitorService.List();
            var current = ResolveCurrent(hWnd, monitors, rc.Left + w / 2, rc.Top + hgt / 2);
            if (current is not null && CoversMonitor(rc.Left, rc.Top, w, hgt, current))
                return "Fullscreen detectado — saia do fullscreen para mover.";

            if (current is not null && current.DeviceKey == target.DeviceKey)
                return $"Já está no {target.Label.Split('—')[0].Trim()}.";

            (int nx, int ny) dest;
            lock (_gate)
            {
                if (current is not null)
                    _memory[current.DeviceKey] = (rc.Left, rc.Top, w, hgt);
                var saved = _memory.TryGetValue(target.DeviceKey, out var s) ? s : ((int, int, int, int)?)null;
                dest = Resolve(new Rect(rc.Left, rc.Top, w, hgt), target, saved);
                _memory[target.DeviceKey] = (dest.nx, dest.ny, w, hgt);
            }

            bool ok = NativeMethods.SetWindowPos(hWnd, nint.Zero, dest.nx, dest.ny, 0, 0,
                NativeMethods.SwpNoSize | NativeMethods.SwpNoZOrder | NativeMethods.SwpNoActivate);
            return ok ? $"Player movido para {target.Label.Split('—')[0].Trim()}." : "Falha ao mover (acesso negado?).";
        }
        catch (Exception ex)
        {
            return $"Erro ao mover: {ex.Message}";
        }
    }

    public sealed record Rect(int X, int Y, int W, int H);

    /// <summary>
    /// Puro (testável): com lugar salvo no alvo, restaura exato (preso nos limites atuais);
    /// sem, centraliza.
    /// </summary>
    public static (int nx, int ny) Resolve(Rect cur, DisplayMonitor target, (int x, int y, int w, int h)? saved)
    {
        int baseX, baseY;
        if (saved.HasValue) { baseX = saved.Value.x; baseY = saved.Value.y; }
        else
        {
            var c = Center(cur.W, cur.H, target.Left, target.Top, target.Width, target.Height);
            baseX = c.x; baseY = c.y;
        }
        int nx = cur.W >= target.Width ? target.Left : Math.Clamp(baseX, target.Left, target.Left + target.Width - cur.W);
        int ny = cur.H >= target.Height ? target.Top : Math.Clamp(baseY, target.Top, target.Top + target.Height - cur.H);
        return (nx, ny);
    }

    /// <summary>Centro puro (testável, sem Win32).</summary>
    public static (int x, int y) Center(int w, int h, int toLeft, int toTop, int toWidth, int toHeight) =>
        (w >= toWidth ? toLeft : toLeft + (toWidth - w) / 2,
         h >= toHeight ? toTop : toTop + (toHeight - h) / 2);

    private static DisplayMonitor? ResolveCurrent(nint hWnd, List<DisplayMonitor> monitors, int cx, int cy)
    {
        try
        {
            var found = NativeMethods.MonitorFromWindow(hWnd, NativeMethods.MonitorDefaultToNearest);
            if (found != nint.Zero)
            {
                var mi = new NativeMethods.MONITORINFO { cbSize = Marshal.SizeOf<NativeMethods.MONITORINFO>() };
                if (NativeMethods.GetMonitorInfo(found, ref mi))
                {
                    int mw = mi.rcMonitor.Right - mi.rcMonitor.Left;
                    int mh = mi.rcMonitor.Bottom - mi.rcMonitor.Top;
                    var hit = monitors.FirstOrDefault(m => m.Left == mi.rcMonitor.Left && m.Top == mi.rcMonitor.Top && m.Width == mw && m.Height == mh);
                    if (hit is not null) return hit;
                }
            }
        }
        catch { }
        DisplayMonitor? best = null;
        double bestDist = double.MaxValue;
        foreach (var m in monitors)
        {
            int qx = Math.Clamp(cx, m.Left, m.Left + m.Width);
            int qy = Math.Clamp(cy, m.Top, m.Top + m.Height);
            double d = Math.Pow(cx - qx, 2) + Math.Pow(cy - qy, 2);
            if (d < bestDist) { bestDist = d; best = m; }
        }
        return best;
    }

    private static bool CoversMonitor(int x, int y, int w, int h, DisplayMonitor m, double tolerance = 0.98) =>
        w >= m.Width * tolerance && h >= m.Height * tolerance
        && Math.Abs(x - m.Left) <= 4 && Math.Abs(y - m.Top) <= 4;
}
