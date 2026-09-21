using System.Diagnostics;
using SideScreen.Infrastructure.Windows;

namespace SideScreen.Infrastructure.Security;

/// <summary>
/// Nível de integridade do próprio processo (UAC/UIPI).
/// Se o jogo roda elevado e o SideScreen não, o Windows não entrega WM_HOTKEY com o jogo em foco.
/// Logado no attach para diagnóstico remoto.
/// </summary>
public static class ProcessIntegrity
{
    private const uint TokenQuery = 0x8;
    private const uint TokenElevation = 20;

    public static string Current()
    {
        try
        {
            using var p = Process.GetCurrentProcess();
            if (!NativeMethods.OpenProcessToken(p.Handle, TokenQuery, out var token))
                return "unknown";
            try
            {
                if (!NativeMethods.GetTokenInformation(token, TokenElevation, out int elevated, 4, out _))
                    return "unknown";
                return elevated != 0 ? "elevated" : "medium";
            }
            finally
            {
                NativeMethods.CloseHandle(token);
            }
        }
        catch { return "unknown"; }
    }
}
