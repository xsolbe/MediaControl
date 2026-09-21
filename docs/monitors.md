# Monitores — lock via reposicionamento (Fase 5)

## Por que não "travar arrasto" de verdade

Interceptar `WM_MOVING`/`WM_WINDOWPOSCHANGING` é intrusivo, quebra fullscreen/maximizado e pode piscar com aceleração de hardware.

## Abordagem SideScreen

1. Listar com `Screen.AllScreens` / `EnumDisplayMonitors`.
2. Usuário escolhe `Monitor 2` + `[x] Lock`.
3. Monitorar via `WinEventHook(EVENT_OBJECT_LOCATIONCHANGE)` + polling fallback 500ms-1s.
4. Se `MonitorFromWindow(hWnd) != alvo`: `GetWindowPlacement` + `SetWindowPos(SWP_NOZORDER|SWP_NOACTIVATE)` com debounce 800ms.
5. Nunca roubar foco (`NOACTIVATE`). Se fullscreen: só sinalizar "fora de posição", não mover.
6. Player fechado: status Offline, sem erro.

## Teste manual (Fase 5)

1. Abrir PotPlayer no Monitor 2, ativar Lock.
2. Arrastar para Monitor 1 → em ~1s deve voltar sozinho, sem foco roubado.
3. Fechar player → Dashboard mostra Offline.
