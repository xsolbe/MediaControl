# Aceitação final — SideScreen (Fase 7)

Automatizado: `dotnet test SideScreen.slnx` → **38 testes** (protocolo, gestos, conflitos, KeyMapper,
double-press, guards incl. allowlist, store roundtrip/corrupt/migração, HotkeyService sem Win32).
O resto é manual — marque cada item ao validar.

## 0. Base (sempre antes)

```powershell
dotnet build SideScreen.slnx   # 0 warnings, 0 errors
dotnet test SideScreen.slnx    # 38/38
```

## 1. Dashboard — controle sem foco

Com PotPlayer aberto (playlist de 3 vídeos):
- [ ] `↻ Refresh` mostra `Playing Vol=N HWND=0x...`
- [ ] Play/Pause alterna sem focar o player
- [ ] `|◀` / `▶|` trocam de faixa
- [ ] Slider de volume muda o player; Refresh confirma
- [ ] `-5s` / `+5s` pulam no vídeo
- [ ] Shuffle ON/OFF (ID candidato — se nada mudar, anote via Spy++ e reporte)
- [ ] Feche o player → `Offline`, sem crash; reabra → volta sozinho

## 2. Shortcuts — atalhos contextuais

- [ ] Rodapé mostra `Hotkeys: ON · 6` após Apply (previous compartilha o F1)
- [ ] Captura: clique + pressione gera o gesto (Numpad+, F2, setas…)
- [ ] Gesto duplicado → `Conflito` bloqueia o Apply
- [ ] F1/F2/seta sozinha → aviso amarelo; mídia → sem aviso
- [ ] `Restaurar padrões` volta Numpad/F1/F2/setas + double-press ON
- [ ] Fora do jogo: Numpad±, F2, F1 (após ~350ms), F1 F1 (anterior), ←/→ (±5s)
- [ ] Guard `OnlyListed` + só BNSR marcado → dispara no jogo, silêncio no resto
- [ ] `hotkeys.log` registra `WM_HOTKEY` + `executed` (não é keylogger)

## 3. BNSR em jogo

- [ ] Com guard `Always`: tudo do item 2 funciona em fullscreen
- [ ] Binds do jogo conferidos (o jogo TAMBÉM recebe F1/F2 — diferente do AHK que suprimia)
- [ ] Se silêncio total: ver `1409` no log (outro programa segurou) ou rodar como admin (UIPI)

## 4. Players / Display / Settings

- [ ] Players: troca de seleção persiste após reopen (controle segue no PotPlayer até a fase MPC-HC)
- [ ] Display: lista os monitores reais; seleção persiste
- [ ] Settings: versão/runtime/config path coerentes

## 5. Persistência e robustez

- [ ] Mudanças em todas as telas sobrevivem a fechar/reabrir
- [ ] Config corrompido → abre com padrões + cria `config.corrupt-*.json`
- [ ] 2ª instância / MusicControl rodando → aviso no Status (não duplo-registro silencioso)
- [ ] Player fechado em qualquer tela → Offline, sem exceção

## Debug

- Classe: `PotPlayer64` (Spy++ / `Find Window`); HWND muda por sessão, classe não
- `%AppData%\SideScreen\hotkeys.log`, `config.json`, `config.corrupt-*`
- `dotnet --version` → 10.x
