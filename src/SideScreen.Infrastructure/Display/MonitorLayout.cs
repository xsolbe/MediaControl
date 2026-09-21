using SideScreen.Infrastructure.Windows;

namespace SideScreen.Infrastructure.Display;

/// <summary>Matemática pura de posicionamento (testável, sem Win32).</summary>
public static class MonitorLayout
{
    /// <summary>
    /// Mantém o deslocamento relativo da janela ao trocar de monitor, prendendo dentro do alvo.
    /// Se a janela for maior que o alvo, encosta no canto superior-esquerdo.
    /// </summary>
    public static (int x, int y) ClampIntoBounds(
        int x, int y, int w, int h,
        int fromLeft, int fromTop,
        int toLeft, int toTop, int toWidth, int toHeight)
    {
        int relX = x - fromLeft;
        int relY = y - fromTop;
        int nx = toLeft + relX;
        int ny = toTop + relY;

        if (w >= toWidth) nx = toLeft;
        else nx = Math.Clamp(nx, toLeft, toLeft + toWidth - w);

        if (h >= toHeight) ny = toTop;
        else ny = Math.Clamp(ny, toTop, toTop + toHeight - h);

        return (nx, ny);
    }

    /// <summary>True se a janela cobre ~todo o monitor (fullscreen/borderless) — nesses casos não movemos.</summary>
    public static bool CoversMonitor(int x, int y, int w, int h, DisplayMonitor m, double tolerance = 0.98) =>
        w >= m.Width * tolerance && h >= m.Height * tolerance
        && Math.Abs(x - m.Left) <= 4 && Math.Abs(y - m.Top) <= 4;
}
