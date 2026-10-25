using System.Runtime.InteropServices;
using System.Text;
using MediaControl.Interop;
using MediaControl.Models;
using MediaControl.Localization;

namespace MediaControl.Services;

internal readonly record struct PlayerSnapshot(
    bool IsRunning, string State, int? Volume, long? PositionMs, long? DurationMs, string Title);

internal static class PotPlayerController
{
    private const uint SmtoAbortIfHung = 0x0002;
    private const uint GetVolumeCommand = 0x5000;
    private const uint SetVolumeCommand = 0x5001;
    private const uint GetDurationCommand = 0x5002;
    private const uint GetPositionCommand = 0x5004;
    private const uint SetPositionCommand = 0x5005;
    private const uint GetPlayStatusCommand = 0x5006;

    public static PlayerSnapshot ReadSnapshot()
    {
        IntPtr window = FindPlayerWindow();
        if (window == IntPtr.Zero)
            return new(false, "OFFLINE", null, null, null, "Faixa atual");

        int? volume = ReadValue(window, GetVolumeCommand) is long v and >= 0 and <= 100
            ? (int)v : null;
        string state = ReadValue(window, GetPlayStatusCommand) switch
        {
            2 => "TOCANDO",
            1 => "PAUSADO",
            0 or -1 => "PARADO",
            _ => "ONLINE",
        };
        long? duration = ReadValue(window, GetDurationCommand);
        long? position = ReadValue(window, GetPositionCommand);
        int length = NativeMethods.GetWindowTextLength(window);
        var titleBuffer = new StringBuilder(Math.Max(length + 1, 1));
        NativeMethods.GetWindowText(window, titleBuffer, titleBuffer.Capacity);
        string title = titleBuffer.ToString();
        if (title.EndsWith(" - PotPlayer", StringComparison.OrdinalIgnoreCase))
            title = title[..^" - PotPlayer".Length];
        if (string.IsNullOrWhiteSpace(title) || title.Equals("PotPlayer", StringComparison.OrdinalIgnoreCase))
            title = "Faixa atual";
        return new(true, state, volume, position, duration, title);
    }

    public static bool TryExecute(string action, out string error)
    {
        IntPtr window = FindPlayerWindow();
        if (window == IntPtr.Zero)
        {
            error = UiText.T("PotPlayer não encontrado.");
            return false;
        }

        bool sent = action switch
        {
            ControlActions.Previous => NativeMethods.PostMessage(window, NativeMethods.WmAppCommand, 0, 0xC0000),
            ControlActions.Next => NativeMethods.PostMessage(window, NativeMethods.WmAppCommand, 0, 0xB0000),
            ControlActions.PlayPause => NativeMethods.PostMessage(window, NativeMethods.WmAppCommand, 0, 0xE0000),
            ControlActions.VolumeUp => NativeMethods.PostMessage(window, NativeMethods.WmCommand, 10035, 0),
            ControlActions.VolumeDown => NativeMethods.PostMessage(window, NativeMethods.WmCommand, 10036, 0),
            ControlActions.SeekBackward => SendArrow(window, 0x25),
            ControlActions.SeekForward => SendArrow(window, 0x27),
            _ => false,
        };

        error = sent ? "" : $"{UiText.T("Falha ao enviar comando")} (Windows {Marshal.GetLastWin32Error()}).";
        return sent;
    }

    public static bool TrySetVolume(int value, out string error)
    {
        IntPtr window = FindPlayerWindow();
        if (window == IntPtr.Zero)
        {
            error = UiText.T("PotPlayer não encontrado.");
            return false;
        }

        bool sent = NativeMethods.PostMessage(window, NativeMethods.WmUser,
            (IntPtr)SetVolumeCommand, (IntPtr)Math.Clamp(value, 0, 100));
        error = sent ? "" : $"{UiText.T("Falha ao ajustar volume")} (Windows {Marshal.GetLastWin32Error()}).";
        return sent;
    }

    public static bool TrySetPosition(long milliseconds, out string error)
    {
        IntPtr window = FindPlayerWindow();
        if (window == IntPtr.Zero)
        {
            error = UiText.T("PotPlayer não encontrado.");
            return false;
        }
        bool sent = NativeMethods.PostMessage(window, NativeMethods.WmUser,
            (IntPtr)SetPositionCommand, (IntPtr)Math.Max(0, milliseconds));
        error = sent ? "" : $"{UiText.T("Falha ao mudar posição")} (Windows {Marshal.GetLastWin32Error()}).";
        return sent;
    }

    private static bool SendArrow(IntPtr window, uint virtualKey)
    {
        long scanCode = NativeMethods.MapVirtualKey(virtualKey, 0);
        long keyDown = 1L | (scanCode << 16) | (1L << 24); // setas usam tecla estendida
        long keyUp = keyDown | (1L << 30) | (1L << 31);
        bool down = NativeMethods.PostMessage(window, NativeMethods.WmKeyDown,
            (IntPtr)virtualKey, (IntPtr)keyDown);
        bool up = NativeMethods.PostMessage(window, NativeMethods.WmKeyUp,
            (IntPtr)virtualKey, (IntPtr)keyUp);
        return down && up;
    }

    private static long? ReadValue(IntPtr window, uint command)
    {
        IntPtr sent = NativeMethods.SendMessageTimeout(window, NativeMethods.WmUser,
            (IntPtr)command, IntPtr.Zero, SmtoAbortIfHung, 100, out IntPtr result);
        return sent == IntPtr.Zero ? null : result.ToInt64();
    }

    private static IntPtr FindPlayerWindow()
    {
        IntPtr window = NativeMethods.FindWindow("PotPlayer64", null);
        if (window != IntPtr.Zero && NativeMethods.IsWindow(window))
            return window;

        window = NativeMethods.FindWindow("PotPlayer", null);
        if (window != IntPtr.Zero && NativeMethods.IsWindow(window))
            return window;

        IntPtr found = IntPtr.Zero;
        NativeMethods.EnumWindowsProc callback = (candidate, _) =>
        {
            if (!NativeMethods.IsWindowVisible(candidate))
                return true;
            int length = NativeMethods.GetWindowTextLength(candidate);
            if (length <= 0)
                return true;
            var title = new StringBuilder(length + 1);
            NativeMethods.GetWindowText(candidate, title, title.Capacity);
            if (!title.ToString().Contains("PotPlayer", StringComparison.OrdinalIgnoreCase))
                return true;
            found = candidate;
            return false;
        };
        NativeMethods.EnumWindows(callback, IntPtr.Zero);
        GC.KeepAlive(callback);
        return found;
    }
}
