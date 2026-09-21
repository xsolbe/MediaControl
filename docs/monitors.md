# Monitores — lock via reposicionamento (Fase 5 — implementado e validado)

Mapa real desta máquina (2026-09-21): `DISPLAY1` 1440x2560 portrait em (-1440,-464, não-primário) + `DISPLAY2` 2560x1440 em (0,0, primário).

## Por que não "travar arrasto" de verdade

Interceptar `WM_MOVING`/`WM_WINDOWPOSCHANGING` é intrusivo, quebra fullscreen/maximizado e pode piscar com aceleração de hardware.

## Abordagem SideScreen — blindagem click-through + âncora (escolha do usuário)

Vetar o arrasto de verdade exigiria injeção de DLL no PotPlayer (fora de questão: frágil + anticheat).
Snap-back com polling causava flashes e briga durante o arrasto. Solução adotada:

1. **Blindagem:** com lock ativo, liga `WS_EX_TRANSPARENT` na janela do player — o mouse atravessa,
   impossível agarrar/arrastar. Troca de estado do window manager (sem injeção, sem hook).
   O vídeo continua renderizando; clique no player não funciona travado (use botões/atalhos).
2. **Âncora:** enquanto no alvo, memoriza a posição exata; se algo mover via teclado
   (`Win+Shift+Seta`), restaura na hora com o tamanho atual.
3. `SetWindowPos(SWP_NOSIZE|SWP_NOZORDER|SWP_NOACTIVATE)`: nunca rouba foco.
4. Ignora maximizado/fullscreen (só avisa); player fechado = Offline.
5. **Segurança:** a blindagem é removida no unlock, no Stop e ao fechar o app.
   Se o app travar/crashar com lock ativo, a janela fica click-through até reabrir o PotPlayer.

Validado live (2026-09-21, HWND 0xA760ACE): ON→verificado→OFF→verificado, `SHIELD=PASS`.
Parede magnética por polling mantida como reserva (decisão `Decide()`), mas com blindagem o arrasto nem inicia.
4. Nunca rouba foco; ignora maximizado/fullscreen (só avisa); player fechado = Offline.

## Teste manual

1. Display → escolha o monitor do vídeo → marque o lock (Status: `Blindado no Monitor ...`).
2. Tente clicar/arrastar o PotPlayer: o mouse atravessa, impossível mover (sem flashes, sem briga).
3. `Win+Shift+Seta` para o outro monitor: volta ao ponto travado na hora.
4. Desmarque o lock: o mouse volta a funcionar no player imediatamente.
5. Maximize o player → Status avisa que não move; desmaximize → volta a vigiar.
6. Feche o player → `Offline — aguardando abrir`; reabra → retoma sozinho.
