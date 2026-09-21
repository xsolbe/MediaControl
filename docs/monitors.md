# Monitores — lock via reposicionamento (Fase 5 — implementado e validado)

Mapa real desta máquina (2026-09-21): `DISPLAY1` 1440x2560 portrait em (-1440,-464, não-primário) + `DISPLAY2` 2560x1440 em (0,0, primário).

## Por que não "travar arrasto" de verdade

Interceptar `WM_MOVING`/`WM_WINDOWPOSCHANGING` é intrusivo, quebra fullscreen/maximizado e pode piscar com aceleração de hardware.

## Abordagem SideScreen — parede magnética (`MonitorLockService`, 100ms)

Vetar o arrasto de verdade exigiria injeção de DLL no PotPlayer (fora de questão: frágil + anticheat).
Em vez disso, o lock segura na borda em tempo real:

1. Lista com `EnumDisplayMonitors`/`GetMonitorInfo`; escolha persistida por device key (`\\.\DISPLAY2`).
2. A cada 100ms: no alvo, memoriza a posição (âncora); fora do alvo **com botão arrastando**
   (`GetAsyncKeyState`), prende no ponto mais próximo **dentro** do alvo — a janela desliza na borda sem sair;
   fora com botão **solto** (ex: `Win+Shift+Seta`), restaura a âncora na hora.
3. `SetWindowPos(SWP_NOSIZE|SWP_NOZORDER|SWP_NOACTIVATE)`: nunca rouba foco; só move se a posição mudou.
4. Ignora maximizado/fullscreen (só avisa); player fechado = Offline.
4. Nunca rouba foco; ignora maximizado/fullscreen (só avisa); player fechado = Offline.

## Teste manual

1. Display → escolha o monitor do vídeo → marque o lock (Status: `Preso no Monitor ...`).
2. Tente arrastar o PotPlayer para fora: ele desliza na borda sem sair (parede magnética).
3. `Win+Shift+Seta` para o outro monitor e solte: volta ao ponto travado na hora.
4. Maximize o player → Status avisa que não move; desmaximize → volta a vigiar.
5. Feche o player → `Offline — aguardando abrir`; reabra → retoma sozinho.
