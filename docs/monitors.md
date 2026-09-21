# Monitores — lock via reposicionamento (Fase 5 — implementado e validado)

Mapa real desta máquina (2026-09-21): `DISPLAY1` 1440x2560 portrait em (-1440,-464, não-primário) + `DISPLAY2` 2560x1440 em (0,0, primário).

## Por que não "travar arrasto" de verdade

Interceptar `WM_MOVING`/`WM_WINDOWPOSCHANGING` é intrusivo, quebra fullscreen/maximizado e pode piscar com aceleração de hardware.

## Abordagem SideScreen (`MonitorLockService`)

1. Lista com `EnumDisplayMonitors`/`GetMonitorInfo`; escolha persistida por device key (`\\.\DISPLAY2`).
2. Vigia a cada 750ms; exige 3 leituras fora do alvo (~2,25s, tolera arrasto em curso).
3. Devolve para a **posição travada (âncora)**: enquanto estável no alvo, memoriza o lugar exato a cada leitura; ao voltar, restaura a âncora com o tamanho atual (o PotPlayer muda o tamanho conforme o vídeo). Sem âncora ainda, usa offset relativo.
4. Nunca rouba foco; ignora maximizado/fullscreen (só avisa); player fechado = Offline.

## Teste manual

1. Display → escolha o monitor do vídeo → marque o lock (Status: `Vigiando ...`).
2. Arraste o PotPlayer para o outro monitor e segure >3s → ele volta sozinho.
3. Maximize o player → Status avisa que não move; desmaximize → volta a vigiar.
4. Feche o player → `Offline — aguardando abrir`; reabra → retoma sozinho.
