# Hotkeys — por que não F1/F2 globais

## Problema

`RegisterHotKey(F1)` global rouba F1 de todo jogo/programa. Anticheat pode sinalizar hooks low-level.

## Estratégia SideScreen (Fase 4)

1. **Default: zero hotkey global.** Controle por botão + `SendMessage` sem foco já resolve 80%.
2. **Padrões do usuário (script MusicControl):** `NumpadAdd`/`NumpadSub` (volume), `F2` = Play/Pause, `F1` = próximo, `F1 x2` = anterior (double-press compartilha o F1; `previous` nem é registrado separadamente).
3. **Guard default = `Always`:** o app existe para controlar o vídeo ENQUANTO joga (uso confirmado no BNSR fullscreen). Quem preferir segurança total pode trocar para `PauseWhenFullscreen` na tela — foi esse guard que bloqueou tudo no primeiro teste em jogo.
4. **Double-press (`F1 x2 = anterior`):** ligado por padrão a pedido do usuário, janela 350ms. Custo honesto: o `Next` simples atrasa ~350ms (espera o possível 2º toque). Se o delay incomodar, desligue e use `Shift+F1` para anterior (sem delay).
5. **Implementação:** `RegisterHotKey` (não bloqueia outras teclas) > `WH_KEYBOARD_LL` (só se necessário, com aviso).
6. **Diferença real vs AHK:** o `F1::` do AutoHotkey SUPRIME a tecla (o jogo nunca recebe F1). O `RegisterHotKey` NÃO suprime — com guard `Always`, o BNSR também recebe F1/F2/Numpad. Verifique os binds do jogo (F1/F2 podem disparar algo lá também); Numpad +/- normalmente é seguro. Se precisar de supressão real, o caminho seria hook low-level — evitado por causa de anticheat (BNS usa XignCode).

## Tela Shortcuts (Fase 4 — implementado)

- Captura de combo (clique + pressione), detecção de conflito (gestos duplicados), warning para F1/F2/teclas sozinhas, `Apply` + `Restaurar padrões`, guard combo + double-press 200-500ms.
- Persistência em `%AppData%\SideScreen\config.json` via `JsonSettingsStore` (criado no 1º Apply).
- Sem `WH_KEYBOARD_LL` — só `RegisterHotKey` no HWND da janela + hook `WM_HOTKEY` na View.

## Diagnóstico remoto — `hotkeys.log` (2026-09-21)

`%AppData%\SideScreen\hotkeys.log` registra attach/start/WM_HOTKEY/executed (só nossos hotkeys — não é keylogger).
Cadeia provada ao vivo: tecla sintética NumpadAdd → `WM_HOTKEY volumeUp` → `executed` → volume 16→21; NumpadSub → 21→16.

## Como testar (roteiro BNSR)

1. Com PotPlayer aberto, abra Shortcuts → marque `Enable global hotkeys` → Apply (guard default `PauseWhenFullscreen`).
2. Com SideScreen focado ou Bloco de Notas focado (janela pequena): `Ctrl+Alt+P` → pausa/retoma o vídeo sem focar o player.
3. Abra um jogo/programa fullscreen → `Ctrl+Alt+P` deve ser **ignorado** (Status mostra `ignorado pelo guard`).
4. Tente cadastrar `F1` sozinho → warning amarelo. Duplicar um gesto → `Conflito` bloqueia o Apply.
5. Double-press: ative, Apply, pressione `Ctrl+Alt+Right` 1x (aguarda ~350ms → Next) e 2x rápido (→ Previous). Note o delay — por isso é experimental e desligado por padrão.

## ⚠ Disclaimer — uso em jogos (pedido do usuário)

Atalhos globais disparam mesmo com o jogo em foco (guard=`Always`) e **podem suprimir ou duplicar a tecla dentro do jogo** — ex: F1/F2 também chegam ao BNSR, podendo acionar algo lá. Efeitos por tecla:

| Tecla | Risco em jogo |
|---|---|
| Numpad + / - | Baixo — jogos raramente usam; seguro na prática |
| F1 / F2 | Alto — confira os binds do jogo antes; teste fora de ranked |
| Setas ← → | Médio — podem mover personagem/menus se o jogo receber junto |

Recomendação: confira os atalhos do jogo, teste em área segura e, se F1/F2 causarem ação dupla, troque para combos com modificador (`Ctrl+F1`) ou volte o guard para `PauseWhenFullscreen`.
