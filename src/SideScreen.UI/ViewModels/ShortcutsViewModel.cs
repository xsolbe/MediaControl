using SideScreen.Core.Config;

namespace SideScreen.UI.ViewModels;

public sealed record ShortcutRow(string Action, string Gesture, string? Warning);

/// <summary>
/// Shortcuts — Fase 2: exibe defaults seguros + aviso F1/F2. Fase 4: captura real + conflito.
/// </summary>
public sealed class ShortcutsViewModel : ObservableObject
{
    public ShortcutsViewModel()
    {
        var c = AppConfig.Default();
        Rows =
        [
            new ShortcutRow("Aumentar volume", c.Shortcuts["volumeUp"], null),
            new ShortcutRow("Diminuir volume", c.Shortcuts["volumeDown"], null),
            new ShortcutRow("Play/Pause", c.Shortcuts["playPause"], null),
            new ShortcutRow("Próximo vídeo", c.Shortcuts["next"], null),
            new ShortcutRow("Vídeo anterior", c.Shortcuts["previous"], null),
        ];
        Note = "Defaults usam Ctrl+Alt de propósito — F1/F2 sozinhos conflitam com jogos. Edição real na Fase 4.";
    }

    public List<ShortcutRow> Rows { get; }
    public string Note { get; }
}
