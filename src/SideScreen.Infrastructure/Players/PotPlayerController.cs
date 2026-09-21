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
        // Sem API oficial. Estratégia honesta: tenta o candidato e documenta.
        // Se o candidato não existir nessa versão, o PotPlayer ignora WM_COMMAND desconhecido.
        // NÃO simulamos tecla aqui — fallback será definido após validação Spy++ (ver docs/potplayer-protocol.md).
        // Por enquanto: envia toggle somente se já sabemos o estado desejado difere? Sem GET_SHUFFLE, envia toggle.
        // Para evitar toggle acidental, a UI deve confirmar antes (Dashboard mostra "a validar").
        SendWmCommand(PotPlayerCommandIds.CmdShuffleToggleCandidate);
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
