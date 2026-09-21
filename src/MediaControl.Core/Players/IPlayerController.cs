namespace MediaControl.Core.Players;

/// <summary>
/// Abstração central do projeto MediaControl.
/// A UI (Dashboard) só fala com esta interface — nunca com PotPlayer diretamente.
/// Para adicionar MPC-HC no futuro: criar MpcHcController : IPlayerController.
/// </summary>
public interface IPlayerController
{
    /// <summary>Id estável usado em config.json. Ex: "potplayer".</summary>
    string Id { get; }

    /// <summary>Nome para exibir no ComboBox. Ex: "PotPlayer".</summary>
    string DisplayName { get; }

    bool IsRunning();
    nint GetWindowHandle();
    PlayerStatus GetStatus();

    void PlayPause();
    void Next();
    void Previous();

    void VolumeUp(int step = 2);
    void VolumeDown(int step = 2);
    void SetVolume(int volume);

    /// <summary>Avança/retrocede N segundos no vídeo atual (padrão 5s, pedido do usuário).</summary>
    void SeekForward(int seconds = 5);
    void SeekBackward(int seconds = 5);

    /// <summary>
    /// Liga/desliga modo aleatório nativo do player.
    /// Fase 1: não implementado. Fase 3: PotPlayer via WM_COMMAND.
    /// </summary>
    void SetShuffle(bool enabled);

    /// <summary>
    /// Garante que a janela do player esteja no monitor indicado.
    /// Fase 1: não implementado. Fase 5: MonitorService + SetWindowPos.
    /// </summary>
    void EnsureOnMonitor(int monitorIndex);
}
