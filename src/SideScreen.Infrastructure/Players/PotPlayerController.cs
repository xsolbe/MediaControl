using SideScreen.Core.Players;
using SideScreen.Infrastructure.Windows;

namespace SideScreen.Infrastructure.Players;

/// <summary>
/// Controlador real do PotPlayer (Fase 3).
/// Tudo via SendMessage — sem foco, sem SendKeys.
/// Se HWND == 0 (fechado): todos os comandos viram no-op e GetStatus retorna Offline.
/// Para testes determinísticos: injete um windowResolver fake.
/// </summary>
public sealed class PotPlayerController : IPlayerController
{
    private readonly Func<nint> _windowResolver;

    public PotPlayerController() : this(WindowFinder.FindPotPlayer) { }

    public PotPlayerController(Func<nint> windowResolver)
    {
        _windowResolver = windowResolver;
    }

    public string Id => "potplayer";
    public string DisplayName => "PotPlayer";

    public bool IsRunning() => GetWindowHandle() != nint.Zero;

    public nint GetWindowHandle()
    {
        try { return _windowResolver(); }
        catch { return nint.Zero; }
    }

    public PlayerStatus GetStatus()
    {
        var h = GetWindowHandle();
        if (h == nint.Zero)
            return new(false, PlaybackState.Unknown, 0, null, DateTime.UtcNow);

        int volume = TryGetVolume(h);
        var state = TryGetState(h);
        return new(true, state, volume, null, DateTime.UtcNow);
    }

    public void PlayPause() => SendAppCommand(PotPlayerCommandIds.AppCommandMediaPlayPause);
    public void Next() => SendAppCommand(PotPlayerCommandIds.AppCommandMediaNext);
    public void Previous() => SendAppCommand(PotPlayerCommandIds.AppCommandMediaPrevious);

    public void VolumeUp(int step = 2) => SendWmCommand(PotPlayerCommandIds.CmdVolumeUp);

    public void VolumeDown(int step = 2) => SendWmCommand(PotPlayerCommandIds.CmdVolumeDown);

    public void SetVolume(int volume)
    {
        var h = GetWindowHandle();
        if (h == nint.Zero) return;
        int v = Math.Clamp(volume, 0, 100);
        try { NativeMethods.SendMessage(h, PotPlayerCommandIds.PotCommand, PotPlayerCommandIds.PotSetVolume, v); }
        catch { /* player fechou no meio — ignora, UI mostra Offline no próximo refresh */ }
    }

    public void SetShuffle(bool enabled)
    {
        // WM_COMMAND 10069 (lista comunitária ld3l). Sem leitura de estado no protocolo:
        // o toggle é write-only — confira no player se Next ficou sequencial/aleatório.
        SendWmCommand(PotPlayerCommandIds.CmdShuffleToggle);
    }

    public void SeekForward(int seconds = 5) => SeekBy(seconds);

    public void SeekBackward(int seconds = 5) => SeekBy(-seconds);

    private void SeekBy(int seconds)
    {
        var h = GetWindowHandle();
        if (h == nint.Zero) return;
        try
        {
            long cur = NativeMethods.SendMessage(h, PotPlayerCommandIds.PotCommand, PotPlayerCommandIds.PotGetCurrentTime, 0).ToInt64();
            long total = NativeMethods.SendMessage(h, PotPlayerCommandIds.PotCommand, PotPlayerCommandIds.PotGetTotalTime, 0).ToInt64();
            long target = cur + (long)seconds * 1000;
            if (target < 0) target = 0;
            if (total > 0 && target > total) target = total;
            NativeMethods.SendMessage(h, PotPlayerCommandIds.PotCommand, PotPlayerCommandIds.PotSetCurrentTime, (nint)target);
        }
        catch { }
    }

    public void EnsureOnMonitor(int monitorIndex)
    {
        // Fase 5: MonitorService + SetWindowPos.
    }

    // ---- helpers ----

    private void SendWmCommand(int cmdId)
    {
        var h = GetWindowHandle();
        if (h == nint.Zero) return;
        try { NativeMethods.SendMessage(h, PotPlayerCommandIds.WmCommand, cmdId, 0); }
        catch { }
    }

    private void SendAppCommand(int appCommand)
    {
        var h = GetWindowHandle();
        if (h == nint.Zero) return;
        try { NativeMethods.SendMessage(h, PotPlayerCommandIds.WmAppCommand, 0, appCommand); }
        catch { }
    }

    private static int TryGetVolume(nint h)
    {
        try
        {
            var r = NativeMethods.SendMessage(h, PotPlayerCommandIds.PotCommand, PotPlayerCommandIds.PotGetVolume, 0);
            long v = r.ToInt64();
            if (v < 0 || v > 100) return 0;
            return (int)v;
        }
        catch { return 0; }
    }

    private static PlaybackState TryGetState(nint h)
    {
        try
        {
            var r = NativeMethods.SendMessage(h, PotPlayerCommandIds.PotCommand, PotPlayerCommandIds.PotGetPlayStatus, 0);
            long v = r.ToInt64();
            // SDK 2012 dizia 0:Stopped, SDK 2023+ diz -1:Stopped. Aceita ambos.
            return v switch
            {
                2 => PlaybackState.Playing,
                1 => PlaybackState.Paused,
                0 or -1 => PlaybackState.Stopped,
                _ => PlaybackState.Unknown,
            };
        }
        catch { return PlaybackState.Unknown; }
    }
}
