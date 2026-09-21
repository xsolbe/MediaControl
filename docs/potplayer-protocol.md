# Protocolo PotPlayer (para Fase 3)

> Fonte: SDK oficial Daum (fórum Pot-Tool) + biblioteca AHK `PotPlayer64` + Unified Remote. Validar com Spy++ na sua versão.

## Alvo

- 64-bit: `ahk_class PotPlayer64` → `FindWindow("PotPlayer64", null)`
- 32-bit: `ahk_class PotPlayer` → fallback
- Se `HWND == 0`: player fechado → UI mostra Offline, sem exceção.

## Família 1 — WM_COMMAND (0x0111)

`SendMessage(hWnd, 0x0111, CMD_ID, 0)` — sem precisar de foco.

| Comando | ID | Notas |
|---|---|---|
| Play/Pause toggle | 10014 | Botão único |
| Play explícito | 20001 | Determinístico |
| Pause explícito | 20000 | Determinístico |
| Next | 10124 | Depende de playlist/pasta |
| Previous | 10123 | idem |
| VolumeUp/Down | 10035 / 10036 | Step interno; preferir SET_VOLUME |

## Família 2 — POT_COMMAND (WM_USER 0x0400)

| Comando | ID | Uso |
|---|---|---|
| GET_VOLUME | 0x5000 | Retorna 0-100 |
| SET_VOLUME | 0x5001 | `SendMessage(hWnd, 0x0400, 0x5001, volume)` — ideal para slider |
| GET_STATUS | 0x5006 | `-1:Stopped, 1:Paused, 2:Running` (validar 0 vs -1) |
| SET_STATUS | 0x5007 | `0:Toggle, 1:Paused, 2:Running` |

## Shuffle

Sem `POT_*` oficial. Plano Fase 3:
1. Spy++ no menu Shuffle/Repeat do PotPlayer para achar `WM_COMMAND ID`.
2. Se não achar ID estável: ler `PotPlayerMini64.ini` ou usar toggle de playlist.
3. Simular tecla só como último recurso, documentando motivo.

## Regra de ouro

Nunca `SendKeys` / `keybd_event` se existir `SendMessage` equivalente. `SendMessage` funciona minimizado e sem foco — essencial para não atrapalhar o jogo.
