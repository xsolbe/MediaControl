using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Threading;
using MediaControl.Interop;

namespace MediaControl.Services;

internal sealed record WindowCandidate(string Title, string ExecutablePath)
{
    public string Display => $"{Title}  ·  {Path.GetFileName(ExecutablePath)}";
    public string ExecutableName => Path.GetFileName(ExecutablePath);
}

internal sealed class ForegroundProgramMonitor : IDisposable
{
    private readonly Dispatcher _dispatcher;
    private readonly Action<string?> _onChanged;
    private readonly NativeMethods.WinEventProc _callback;
    private readonly DispatcherTimer _fallbackTimer;
    private readonly IntPtr _hook;

    public ForegroundProgramMonitor(Window owner, Action<string?> onChanged)
    {
        _dispatcher = owner.Dispatcher;
        _onChanged = onChanged;
        _callback = OnWinEvent;
        _hook = NativeMethods.SetWinEventHook(
            NativeMethods.EventSystemForeground, NativeMethods.EventSystemForeground,
            IntPtr.Zero, _callback, 0, 0, NativeMethods.WinEventOutOfContext);
        _fallbackTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
        _fallbackTimer.Tick += (_, _) => NotifyCurrent();
        _fallbackTimer.Start();
        NotifyCurrent();
    }

    public static string? GetForegroundExecutablePath() => GetExecutablePath(NativeMethods.GetForegroundWindow(), allowNameFallback: true);

    public static IReadOnlyList<WindowCandidate> ListOpenWindows()
    {
        var result = new List<WindowCandidate>();
        int currentId = Environment.ProcessId;
        NativeMethods.EnumWindowsProc callback = (window, _) =>
        {
            if (!NativeMethods.IsWindowVisible(window))
                return true;
            NativeMethods.GetWindowThreadProcessId(window, out uint processId);
            if (processId == currentId)
                return true;
            int length = NativeMethods.GetWindowTextLength(window);
            if (length <= 0)
                return true;
            var title = new StringBuilder(Math.Min(length + 1, 512));
            NativeMethods.GetWindowText(window, title, title.Capacity);
            string? path = GetExecutablePath(window);
            if (!string.IsNullOrWhiteSpace(path) && Path.IsPathFullyQualified(path)
                && !string.IsNullOrWhiteSpace(title.ToString())
                && !result.Any(item => string.Equals(item.ExecutablePath, path, StringComparison.OrdinalIgnoreCase)))
                result.Add(new WindowCandidate(title.ToString(), path));
            return true;
        };
        NativeMethods.EnumWindows(callback, IntPtr.Zero);
        GC.KeepAlive(callback);
        return result.OrderBy(item => item.Title, StringComparer.CurrentCultureIgnoreCase).ToList();
    }

    private static string? GetExecutablePath(IntPtr window, bool allowNameFallback = false)
    {
        if (window == IntPtr.Zero || NativeMethods.GetWindowThreadProcessId(window, out uint processId) == 0)
            return null;
        IntPtr process = NativeMethods.OpenProcess(NativeMethods.ProcessQueryLimitedInformation, false, processId);
        if (process == IntPtr.Zero)
            return allowNameFallback ? GetExecutableName(processId) : null;
        try
        {
            var path = new StringBuilder(32768);
            uint size = (uint)path.Capacity;
            return NativeMethods.QueryFullProcessImageName(process, 0, path, ref size)
                ? path.ToString() : allowNameFallback ? GetExecutableName(processId) : null;
        }
        finally
        {
            NativeMethods.CloseHandle(process);
        }
    }

    private static string? GetExecutableName(uint processId)
    {
        try
        {
            using var process = Process.GetProcessById((int)processId);
            return process.ProcessName;
        }
        catch
        {
            return null;
        }
    }

    private void OnWinEvent(IntPtr hook, uint eventType, IntPtr window,
        int objectId, int childId, uint eventThread, uint eventTime)
    {
        if (!_dispatcher.HasShutdownStarted)
            _dispatcher.BeginInvoke(NotifyCurrent);
    }

    private void NotifyCurrent() => _onChanged(GetForegroundExecutablePath());

    public void Dispose()
    {
        _fallbackTimer.Stop();
        if (_hook != IntPtr.Zero)
            NativeMethods.UnhookWinEvent(_hook);
        GC.KeepAlive(_callback);
    }
}
