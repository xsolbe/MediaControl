using SideScreen.Infrastructure.Players;

namespace SideScreen.Infrastructure.Windows;

/// <summary>
/// Localiza a janela principal do PotPlayer.
/// Tenta 64-bit primeiro (PotPlayer64), depois 32-bit (PotPlayer).
/// Retorna nint.Zero se fechado — nunca lança.
/// </summary>
public static class WindowFinder
{
    private static readonly string[] ProcessNames = ["PotPlayerMini64", "PotPlayerMini", "PotPlayer"];

    public static nint FindPotPlayer()
    {
        var h = NativeMethods.FindWindow(PotPlayerCommandIds.WindowClass64, null);
        if (h != nint.Zero && NativeMethods.IsWindow(h))
            return h;

        h = NativeMethods.FindWindow(PotPlayerCommandIds.WindowClass32, null);
        if (h != nint.Zero && NativeMethods.IsWindow(h))
            return h;

        // Fallback robusto: via processo (cobre variações de classe e múltiplas instâncias — pega a 1ª com janela principal).
        // Validado live em 2026-09-21: PID 42252 PotPlayerMini64, HWND 0xA760ACE, classe PotPlayer64.
        try
        {
            foreach (var name in ProcessNames)
            {
                foreach (var p in System.Diagnostics.Process.GetProcessesByName(name))
                {
                    try
                    {
                        if (p.MainWindowHandle != nint.Zero)
                            return p.MainWindowHandle;
                    }
                    catch { }
                    finally { p.Dispose(); }
                }
            }
        }
        catch { }

        return nint.Zero;
    }
}
