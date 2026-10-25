using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;

namespace MediaControl.Localization;

internal static class UiText
{
    private sealed class OriginalText
    {
        public string? Text;
        public string? Content;
        public string? ToolTip;
    }

    private static readonly ConditionalWeakTable<DependencyObject, OriginalText> Originals = new();
    private static readonly HashSet<string> DynamicNames =
    [
        "TrackTitle", "PlaybackBadge", "PlayPauseCaption", "NextTrackButtonLabel",
        "VolumeHotkeyHint", "ProgramStatusText", "HintText", "CountText",
    ];

    private static readonly Dictionary<string, string> English = new(StringComparer.Ordinal)
    {
        ["CONTROLE DE MÍDIA"] = "MEDIA CONTROL",
        ["Minimizar para a bandeja"] = "Minimize to tray",
        ["Fechar"] = "Close",
        ["Reprodução"] = "Playback",
        ["Faixas"] = "Tracks",
        ["Programas"] = "Programs",
        ["Atalhos"] = "Hotkeys",
        ["Geral"] = "General",
        ["Faixa atual"] = "Current track",
        ["TOCANDO"] = "PLAYING",
        ["PAUSADO"] = "PAUSED",
        ["PARADO"] = "STOPPED",
        ["Voltar 5 segundos"] = "Back 5 seconds",
        ["Faixa anterior"] = "Previous track",
        ["Pausar ou continuar"] = "Play or pause",
        ["Próxima faixa"] = "Next track",
        ["Avançar 5 segundos"] = "Forward 5 seconds",
        ["VOLTAR"] = "BACK",
        ["ANT."] = "PREV.",
        ["PAUSAR"] = "PAUSE",
        ["TOCAR"] = "PLAY",
        ["PRÓX."] = "NEXT",
        ["AVANÇAR"] = "FORWARD",
        ["Silenciar ou restaurar volume"] = "Mute or restore volume",
        ["FAIXAS"] = "TRACKS",
        ["Navegar pela reprodução"] = "Navigate playback",
        ["Use os botões ou os atalhos globais."] = "Use the buttons or global hotkeys.",
        ["Volume do PotPlayer"] = "PotPlayer volume",
        ["Diminuir volume"] = "Decrease volume",
        ["Silenciar ou restaurar"] = "Mute or unmute",
        ["PROGRAMAS"] = "PROGRAMS",
        ["Onde os atalhos funcionam"] = "Where hotkeys work",
        ["Todos"] = "All apps",
        ["Somente lista"] = "Listed only",
        ["Nenhum programa cadastrado"] = "No programs added",
        ["Janela aberta"] = "Open window",
        ["Escolher arquivo"] = "Browse file",
        ["Remover"] = "Remove",
        ["Pausar / continuar"] = "Play / pause",
        ["Voltar 5s"] = "Back 5s",
        ["Avançar 5s"] = "Forward 5s",
        ["Padrão"] = "Defaults",
        ["Aplicar"] = "Apply",
        ["Adicionar janela aberta"] = "Add open window",
        ["Escolha um programa em execução"] = "Choose a running program",
        ["Buscar janela ou executável"] = "Search window or executable",
        ["JANELAS ABERTAS"] = "OPEN WINDOWS",
        ["Nenhuma janela corresponde à busca"] = "No matching windows",
        ["Selecione uma janela para adicionar"] = "Select a window to add",
        ["Cancelar"] = "Cancel",
        ["Adicionar"] = "Add",
        ["GERAL"] = "GENERAL",
        ["Preferências do aplicativo"] = "App preferences",
        ["Idioma"] = "Language",
        ["Escolha o idioma da interface"] = "Choose the interface language",
        ["A alteração é aplicada imediatamente."] = "Changes apply immediately.",
        ["Atalhos indisponíveis: abra Atalhos"] = "Hotkeys unavailable: open Hotkeys",
        ["Atalhos ativos em todos os programas"] = "Hotkeys active in all apps",
        ["Lista vazia: atalhos desativados"] = "Empty list: hotkeys disabled",
        ["Atalhos ativos neste programa"] = "Hotkeys active in this app",
        ["Fora da lista: teclas livres"] = "Outside list: keys are free",
        ["Falha ao registrar atalhos neste programa"] = "Could not register hotkeys in this app",
        ["Esse programa já está na lista"] = "This program is already listed",
        ["Nenhuma janela de programa encontrada"] = "No program windows found",
        ["Selecione um programa da lista"] = "Select a program from the list",
        ["Pressione uma tecla · Esc cancela"] = "Press a key · Esc cancels",
        ["Pressione..."] = "Press key...",
        ["Clique em Aplicar para salvar"] = "Click Apply to save",
        ["Clique na tecla para alterar"] = "Click a key to change it",
        ["Atalhos indisponíveis."] = "Hotkeys unavailable.",
        ["Atalhos salvos"] = "Hotkeys saved",
        ["Falha ao salvar"] = "Could not save",
        ["Falha: veja o aviso no painel principal"] = "Failed: see the main panel notice",
        ["Abrir MediaControl"] = "Open MediaControl",
        ["Sair"] = "Exit",
        ["PotPlayer não encontrado."] = "PotPlayer not found.",
        ["Falha ao enviar comando"] = "Could not send command",
        ["Falha ao ajustar volume"] = "Could not set volume",
        ["Falha ao mudar posição"] = "Could not seek",
        ["Janela do MediaControl indisponível."] = "MediaControl window unavailable.",
        ["Também falhou a restauração: "] = "Restore also failed: ",
        ["Atalho inválido para "] = "Invalid hotkey for ",
        ["A tecla "] = "The key ",
        [" aparece em duas ações."] = " is assigned to two actions.",
        ["não pôde ser registrado. Código Windows: "] = "could not be registered. Windows code: ",
        ["Aumentar volume"] = "Increase volume",
        ["1 programa"] = "1 program",
        ["programas"] = "programs",
        ["ajustam o volume"] = "adjust volume",
        ["Falha ao salvar programas: "] = "Could not save programs: ",
        ["Falha ao salvar: "] = "Could not save: ",
        ["Não foi possível salvar o idioma: "] = "Could not save language: ",
        ["Programas (*.exe;*.bin)|*.exe;*.bin|Todos os arquivos (*.*)|*.*"] = "Programs (*.exe;*.bin)|*.exe;*.bin|All files (*.*)|*.*",
        ["Escolher programa para os atalhos"] = "Choose a program for hotkeys",
        ["OFFLINE"] = "OFFLINE",
        ["ONLINE"] = "ONLINE",
        ["AVISO"] = "NOTICE",
        ["SALVO"] = "SAVED",
    };

    public static string Language { get; private set; } = "en";

    public static void SetLanguage(string? language) =>
        Language = language == "pt-BR" ? "pt-BR" : "en";

    public static string T(string portuguese) =>
        Language == "en" && English.TryGetValue(portuguese, out string? translated)
            ? translated : portuguese;

    public static void ApplyStaticText(DependencyObject root)
    {
        foreach (object child in LogicalTreeHelper.GetChildren(root))
            if (child is DependencyObject dependency)
                ApplyStaticText(dependency);

        if (root is not FrameworkElement element || DynamicNames.Contains(element.Name))
            return;
        var original = Originals.GetValue(root, _ => new OriginalText());
        if (root is TextBlock textBlock)
        {
            original.Text ??= textBlock.Text;
            if (English.ContainsKey(original.Text))
                textBlock.Text = T(original.Text);
        }
        if (root is ContentControl content && content.Content is string label)
        {
            original.Content ??= label;
            if (English.ContainsKey(original.Content))
                content.Content = T(original.Content);
        }
        if (element.ToolTip is string tip)
        {
            original.ToolTip ??= tip;
            if (English.ContainsKey(original.ToolTip))
                element.ToolTip = T(original.ToolTip);
        }
    }
}
