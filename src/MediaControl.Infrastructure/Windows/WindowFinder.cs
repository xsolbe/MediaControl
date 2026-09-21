using System.Diagnostics;
using MediaControl.Infrastructure.Players;

namespace MediaControl.Infrastructure.Windows;

/// <summary>
/// Localiza as janelas do PotPlayer.
/// Achado 2026-09-21: o PotPlayer (MFC + skins) tem VÁRIAS top-levels visíveis (frame Afx, sombras,
/// vídeo). Blindar só a principal não adianta — o arrasto acontece nas outras. Por isso:
/// FindPotPlayer() = âncora (move o dono e as owned seguem); FindPotPlayerWindows() = todas p/ blindar.
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
                foreach (var p in Process.GetProcessesByName(name))
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

    /// <summary>Todas as top-levels VISÍVEIS do processo do PotPlayer (para blindar o mouse em todas).</summary>
    public static List<nint> FindPotPlayerWindows()
    {
        var result = new List<nint>();
        try
        {
            var pids = new HashSet<int>();
            foreach (var name in ProcessNames)
            {
                foreach (var p in Process.GetProcessesByName(name))
                {
                    try { pids.Add(p.Id); }
                    catch { }
                    finally { try { p.Dispose(); } catch { } }
                }
            }
            if (pids.Count == 0) return result;

            NativeMethods.EnumWindowsProc callback = (hWnd, _) =>
            {
                try
                {
                    if (!NativeMethods.IsWindowVisible(hWnd)) return true;
                    NativeMethods.GetWindowThreadProcessId(hWnd, out uint pid);
                    if (pids.Contains((int)pid))
                        result.Add(hWnd);
                }
                catch { }
                return true;
            };
            NativeMethods.EnumWindows(callback, nint.Zero);
            GC.KeepAlive(callback);
        }
        catch { }
        return result;
    }
}
