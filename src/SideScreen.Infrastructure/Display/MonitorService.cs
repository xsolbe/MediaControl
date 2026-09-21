using System.Runtime.InteropServices;
using SideScreen.Infrastructure.Windows;

namespace SideScreen.Infrastructure.Display;

/// <summary>Enumera monitores reais via EnumDisplayMonitors (lida com layout negativo, portrait, etc.).</summary>
public static class MonitorService
{
    public static List<DisplayMonitor> List()
    {
        var result = new List<DisplayMonitor>();
        var handles = new List<nint>();

        NativeMethods.MonitorEnumProc callback = (nint hMon, nint hdc, ref NativeMethods.RECT rc, nint data) =>
        {
            handles.Add(hMon);
            return true;
        };

        try
        {
            if (!NativeMethods.EnumDisplayMonitors(nint.Zero, nint.Zero, callback, nint.Zero))
                return result;
        }
        catch { return result; }

        // Mantém o delegate vivo durante as chamadas nativas.
        GC.KeepAlive(callback);

        int i = 0;
        foreach (var h in handles)
        {
            i++;
            try
            {
                var mi = new NativeMethods.MONITORINFO { cbSize = Marshal.SizeOf<NativeMethods.MONITORINFO>() };
                if (!NativeMethods.GetMonitorInfo(h, ref mi))
                    continue;
                int w = mi.rcMonitor.Right - mi.rcMonitor.Left;
                int hgt = mi.rcMonitor.Bottom - mi.rcMonitor.Top;
                bool primary = (mi.dwFlags & 1) == 1;
                string device = string.IsNullOrWhiteSpace(mi.szDevice) ? $"DISPLAY{i}" : mi.szDevice;
                result.Add(new DisplayMonitor(
                    device,
                    $"Monitor {i} — {w}x{hgt}{(primary ? " (Principal)" : "")}",
                    mi.rcMonitor.Left, mi.rcMonitor.Top, w, hgt, primary));
            }
            catch { }
        }

        return result;
    }
}
