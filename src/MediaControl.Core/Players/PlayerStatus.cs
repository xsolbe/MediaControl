namespace MediaControl.Core.Players;

/// <summary>
/// Estado de reprodução reportado por um player.
/// Fase 1: modelo mínimo. Fase 3 vai preencher via PotPlayer SDK (POT_GET_PLAY_STATUS).
/// </summary>
public enum PlaybackState
{
    Unknown,
    Stopped,
    Paused,
    Playing
}

/// <summary>
/// Snapshot imutável do status do player para exibir no Dashboard.
/// </summary>
public sealed record PlayerStatus(
    bool IsRunning,
    PlaybackState State,
    int Volume, // 0-100
    string? CurrentFile,
    DateTime CheckedAt);
