# Hotkeys — por que não F1/F2 globais

## Problema

`RegisterHotKey(F1)` global rouba F1 de todo jogo/programa. Anticheat pode sinalizar hooks low-level.

## Estratégia SideScreen (Fase 4)

1. **Default: zero hotkey global.** Controle por botão + `SendMessage` sem foco já resolve 80%.
2. **Opt-in com combos raros:** `Ctrl+Alt+P`, `Ctrl+Alt+Setas`, teclas de mídia (`Media_PlayPause`). Nunca F1/F2 sozinhos.
3. **ForegroundGuard:** antes de executar, `GetForegroundWindow()`. Modos:
   - `Always` — executa sempre (explicitamente escolhido)
   - `PauseWhenFullscreen` (default) — se janela em foco ocupa tela toda, ignora
   - `OnlyWhenPlayerFocused` — mais restritivo
4. **Double-press (`F1 x2 = anterior`):** possível com timer 300-400ms, mas atrasa o single-press ou causa duplo disparo. Deixar experimental, desligado com Modo Jogo. Recomendado: `F1=Next, Shift+F1=Prev` (sem delay).
5. **Implementação:** `RegisterHotKey` (não bloqueia outras teclas) > `WH_KEYBOARD_LL` (só se necessário, com aviso).

## Tela Shortcuts (Fase 4)

- Captura de combo, detecção de conflito, warning para F1/F2/Jogos, `Restaurar padrões`, slider double-press 200-500ms.
