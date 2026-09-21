# Monitores — post-mortem: trava de posição REMOVIDA (2026-09-21, pedido do usuário)

Mapa real desta máquina: `DISPLAY1` 1440x2560 portrait em (-1440,-464, não-primário) + `DISPLAY2` 2560x1440 em (0,0, primário).

## O que foi tentado (tudo validado parcialmente, tudo falhou no teste real)

1. **Snap-back por polling** (750ms + 3 leituras): voltava, mas com delay e flashes — sensação amadora.
2. **Âncora de posição**: voltava ao ponto exato, mas mantinha o snap-back.
3. **Parede magnética** (100ms + segura na borda): teoria boa, mas na prática brigava com o arrasto.
4. **Click-through (`WS_EX_TRANSPARENT`)**: parecia perfeito no papel e passava em verificação de bit
   (`SHIELD=PASS`), mas no uso real os **controles morriam e o arrasto continuava**.

## Por que o click-through falhou (evidência)

* O PotPlayer tem **3 top-levels visíveis**: `PotPlayer64` (0xA760ACE) + 2 frames `Afx:` sem título
  (0x6C00C06, 0x200EAE) — o arrasto acontece nos frames.
* Descoberta decisiva: os frames `Afx:` são **`WS_EX_LAYERED` (0x80000)** — janelas de skin renderizadas
  pelo próprio motor do PotPlayer, que gerencia o estilo continuamente.
* Resultado prático observado: a blindagem vencia a corrida nos instantes de clique (controles mortos)
  e perdia durante o arrasto contínuo (janela livre) — exatamente o inverso do desejado.
* Sem resíduos: com o app fechado, nenhuma janela mantém o bit (verificado ao vivo).

## Decisão

Sistema de lock **removido completamente** do app (serviço, interação, layout, toggle, testes).
Restou na tela Display: lista real de monitores + seleção persistida (informativo).

## Alternativa recomendada (sem código)

**Fullscreen no monitor desejado** (F5 ou botão no PotPlayer): em fullscreen não existe arrasto,
não há o que travar. É o que players de quiosque fazem — e funciona com qualquer skin.
