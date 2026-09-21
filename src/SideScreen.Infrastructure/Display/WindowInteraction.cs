using SideScreen.Infrastructure.Windows;

namespace SideScreen.Infrastructure.Display;

/// <summary>
/// Click-through: com lock ativo, a janela do player ignora o mouse (cliques atravessam,
/// não dá para agarrar/arrastar). Troca de estado do window manager — sem injeção, sem hook.
/// Restaurar é só limpar o bit (feito no Stop/Dispose/exit do app).
/// </summary>
public static class WindowInteraction
{
    public static long WithFlag(long exStyle, long flag) => exStyle | flag;
    public static long WithoutFlag(long exStyle, long flag) => exStyle & ~flag;
    public static bool HasFlag(long exStyle, long flag) => (exStyle & flag) != 0;

    /// <summary>Lê o exStyle atual (0 em falha).</summary>
    public static long GetExStyle(nint hWnd)
    {
        try { return NativeMethods.GetWindowLongPtr(hWnd, NativeMethods.GwlpExStyle).ToInt64(); }
        catch { return 0; }
    }

    public static bool IsClickThrough(nint hWnd) =>
        HasFlag(GetExStyle(hWnd), NativeMethods.WsExTransparent);

    /// <summary>Liga/desliga click-through + aplica com FRAMECHANGED. Idempotente.</summary>
    public static bool SetClickThrough(nint hWnd, bool enable)
    {
        try
        {
            if (hWnd == nint.Zero || !NativeMethods.IsWindow(hWnd))
                return false;

            long current = GetExStyle(hWnd);
            bool already = HasFlag(current, NativeMethods.WsExTransparent);
            if (already == enable)
                return true;

            long next = enable ? WithFlag(current, NativeMethods.WsExTransparent)
                               : WithoutFlag(current, NativeMethods.WsExTransparent);
            NativeMethods.SetWindowLongPtr(hWnd, NativeMethods.GwlpExStyle, (nint)next);
            NativeMethods.SetWindowPos(hWnd, nint.Zero, 0, 0, 0, 0,
                NativeMethods.SwpNoMove | NativeMethods.SwpNoSize | NativeMethods.SwpNoZOrder
                | NativeMethods.SwpNoActivate | NativeMethods.SwpFrameChanged);
            return HasFlag(GetExStyle(hWnd), NativeMethods.WsExTransparent) == enable;
        }
        catch { return false; }
    }
}
