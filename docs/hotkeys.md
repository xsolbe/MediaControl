# Hotkeys — por que não F1/F2 globais

## Problema

`RegisterHotKey(F1)` global rouba F1 de todo jogo/programa. Anticheat pode sinalizar hooks low-level.

## Estratégia SideScreen (Fase 4)

1. **Default: zero hotkey global.** Controle por botão + `SendMessage` sem foco já resolve 80%.
2. **Padrões do usuário (script MusicControl):** `VolumeUp`/`VolumeDown` (teclas de mídia — seguras, o Windows lida sem roubar foco), `F2` = Play/Pause, `F1` = próximo, `F1 x2` = anterior (double-press compartilha o F1; `previous` nem é registrado separadamente).
3. **ForegroundGuard:** antes de executar, `GetForegroundWindow()`. Modos:
   - `Always` — executa sempre (explicitamente escolhido)
   - `PauseWhenFullscreen` (default) — se janela em foco ocupa tela toda, ignora
   - `OnlyWhenPlayerFocused` — mais restritivo
4. **Double-press (`F1 x2 = anterior`):** ligado por padrão a pedido do usuário, janela 350ms. Custo honesto: o `Next` simples atrasa ~350ms (espera o possível 2º toque). Se o delay incomodar, desligue e use `Shift+F1` para anterior (sem delay).
5. **Implementação:** `RegisterHotKey` (não bloqueia outras teclas) > `WH_KEYBOARD_LL` (só se necessário, com aviso).

## Tela Shortcuts (Fase 4 — implementado)

- Captura de combo (clique + pressione), detecção de conflito (gestos duplicados), warning para F1/F2/teclas sozinhas, `Apply` + `Restaurar padrões`, guard combo + double-press 200-500ms.
- Persistência em `%AppData%\SideScreen\config.json` via `JsonSettingsStore` (criado no 1º Apply).
- Sem `WH_KEYBOARD_LL` — só `RegisterHotKey` no HWND da janela + hook `WM_HOTKEY` na View.

## Como testar (sem se expor em jogo)

1. Com PotPlayer aberto, abra Shortcuts → marque `Enable global hotkeys` → Apply (guard default `PauseWhenFullscreen`).
2. Com SideScreen focado ou Bloco de Notas focado (janela pequena): `Ctrl+Alt+P` → pausa/retoma o vídeo sem focar o player.
3. Abra um jogo/programa fullscreen → `Ctrl+Alt+P` deve ser **ignorado** (Status mostra `ignorado pelo guard`).
4. Tente cadastrar `F1` sozinho → warning amarelo. Duplicar um gesto → `Conflito` bloqueia o Apply.
5. Double-press: ative, Apply, pressione `Ctrl+Alt+Right` 1x (aguarda ~350ms → Next) e 2x rápido (→ Previous). Note o delay — por isso é experimental e desligado por padrão.
