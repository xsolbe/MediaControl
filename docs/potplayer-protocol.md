# Protocolo PotPlayer (Fase 3 — validado live)

> Fonte: SDK oficial Daum (fórum Pot-Tool) + biblioteca AHK `PotPlayer64` + Unified Remote.
> Validação live 2026-09-21 nesta máquina: PID 42252 `PotPlayerMini64.exe`, HWND `0xA760ACE`, classe `PotPlayer64`, título com `- PotPlayer`. Leitura: `GET_VOLUME=21`, `GET_STATUS=Playing`. Só leitura — sem pausar/trocar seu vídeo.

## Alvo (`WindowFinder.FindPotPlayer`)

1. `FindWindow("PotPlayer64", null)` → 64-bit (caminho principal — validado: retornou `0xA760ACE`)
2. `FindWindow("PotPlayer", null)` → 32-bit fallback
3. Fallback: processos `PotPlayerMini64`/`PotPlayerMini`/`PotPlayer` → 1º `MainWindowHandle != 0`
- Se `HWND == 0`: player fechado → UI mostra Offline, comandos viram no-op, sem exceção.

## Família 1b — WM_APPCOMMAND (0x0319) — caminho do MusicControl.ahk original ⭐

Extraído do `MusicControl.exe` (strings do binário, 2026-09-21) e adotado como padrão:

| Comando | lParam | Origem AHK |
|---|---|---|
| Play/Pause | `0xE0000` (14 << 16) | `PauseTrack()`: `PostMessage 0x0319, 0, 0xE0000` |
| Next | `0xB0000` (11 << 16) | `NextTrack()`: `PostMessage 0x0319, 0, 0xB0000` |
| Previous | `0xC0000` (12 << 16) | análogo (MEDIA_PREVTRACK) |

Volume no AHK era `WM_COMMAND 10035/10036` (passos) — também adotado para `VolumeUp/Down`.
Slider usa `POT_SET_VOLUME` (roundtrip validado live: 16→16→18→16).

## Família 1 — WM_COMMAND (0x0111) — alternativa documentada

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
| GET_STATUS | 0x5006 | `-1:Stopped, 1:Paused, 2:Running` (validado: Playing=2 ao vivo; aceita 0 e -1 como Stopped) |
| SET_STATUS | 0x5007 | `0:Toggle, 1:Paused, 2:Running` |

## Shuffle

Sem `POT_*` oficial. Estado Fase 3 (honesto):
- `SetShuffle` envia `WM_COMMAND 10125` (candidato). O PotPlayer ignora IDs desconhecidos, então é seguro mas pode não alternar nada nessa versão.
- Para achar o ID real: Spy++ → Messages no HWND → alternar Shuffle na playlist → anotar `wParam` de `WM_COMMAND` → trocar `CmdShuffleToggleCandidate`.
- Alternativas se não houver ID: ler `PotPlayerMini64.ini` ou toggle de playlist. Simular tecla só como último recurso, documentando motivo.

## Comandos validados vs a validar (nesta máquina)

| Comando | Método | Resultado |
|---|---|---|
| FindWindow + IsRunning | `FindWindowW PotPlayer64` | ✅ `0xA760ACE`, `IsRunning=True` |
| GET_VOLUME / GET_STATUS | `SendMessage 0x0400` | ✅ `Vol=21`, `Playing` (só leitura) |
| Play/Pause, Next, Prev | `SendMessage 0x0111` | 🟡 Implementado, não disparado aqui para não interromper sua reprodução — teste via Dashboard |
| SET_VOLUME | `SendMessage 0x0400/0x5001` | 🟡 Implementado (slider), teste arrastando no Dashboard e conferindo no player |
| Shuffle | `WM_COMMAND 10125` | 🟡 Candidato — confirmar se alterna; se não, fazer Spy++ |

## Regra de ouro

Nunca `SendKeys` / `keybd_event` se existir `SendMessage` equivalente. `SendMessage` funciona minimizado e sem foco — essencial para não atrapalhar o jogo.
