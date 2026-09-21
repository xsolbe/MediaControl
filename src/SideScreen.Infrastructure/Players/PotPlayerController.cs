using SideScreen.Core.Players;

namespace SideScreen.Infrastructure.Players;

/// <summary>
/// Controlador do PotPlayer.
/// Fase 1: STUB — sempre reporta "não rodando". Não envia SendMessage ainda.
/// Fase 3: implementar FindWindowEx + SendMessage usando PotPlayerCommandIds.
/// </summary>
public sealed class PotPlayerController : IPlayerController
{
    public string Id => "potplayer";
    public string DisplayName => "PotPlayer";

    public bool IsRunning() => GetWindowHandle() != nint.Zero;

    public nint GetWindowHandle()
    {
        // TODO Fase 3: P/Invoke FindWindowEx para PotPlayer64 / PotPlayer.
        return nint.Zero;
    }

    public PlayerStatus GetStatus() =>
        new(
            IsRunning: false,
            State: PlaybackState.Unknown,
            Volume: 0,
            CurrentFile: null,
            CheckedAt: DateTime.UtcNow);

    public void PlayPause() { /* TODO Fase 3 */ }
    public void Next() { /* TODO Fase 3 */ }
    public void Previous() { /* TODO Fase 3 */ }
    public void VolumeUp(int step = 2) { /* TODO Fase 3 */ }
    public void VolumeDown(int step = 2) { /* TODO Fase 3 */ }
    public void SetVolume(int volume) { /* TODO Fase 3 */ }
    public void SetShuffle(bool enabled) { /* TODO Fase 3 */ }
    public void EnsureOnMonitor(int monitorIndex) { /* TODO Fase 5 */ }
}
