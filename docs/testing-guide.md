# Como testar cada funcionalidade localmente

## Fase 1 (atual)

```powershell
dotnet build SideScreen.slnx
dotnet test SideScreen.slnx
dotnet run --project src/SideScreen.UI/SideScreen.UI.csproj
```

- [ ] Build sem erro nem warning
- [ ] 2 testes passam (`PotPlayerController_Stub`, `AppConfig_Defaults`)
- [ ] Janela `#070707` abre com “Offline (stub Fase 1)”

## Fase 2 (UI)

- Abrir cada tela: Dashboard, Players, Shortcuts, Display, Settings com dados fake.

## Fase 3 (PotPlayer — atual, validado live 2026-09-21)

Leitura (segura, sem interromper):
- [x] `WindowFinder` acha `0xA760ACE` classe `PotPlayer64`
- [x] `GET_VOLUME` retorna ex.: 21, `GET_STATUS` retorna Playing
- [ ] No Dashboard: `↻ Refresh` mostra `Playing Vol=21 HWND=0x...` (confira com seu player aberto)

Ações (teste você, pois mudam reprodução — faça com playlist de 3 vídeos):
1. Abrir PotPlayer com 1 vídeo + playlist de 3 vídeos.
2. No Dashboard, clicar Play/Pause → alterna sem focar o player.
3. Next/Prev → troca faixa.
4. Arrastar slider volume → volume do player muda junto; Refresh confirma.
5. Fechar player → Dashboard mostra Offline, sem crash. Reabrir → volta a Playing.
6. Shuffle ON/OFF → verificar se playlist alterna; se nada acontecer, o ID 10125 não é o da sua versão — anote via Spy++ e me passe.

## Fase 4 (Atalhos) — roteiro BNSR (2026-09-21)

Pré-requisito: PotPlayer aberto + SideScreen aberto.

1. Shortcuts → **Restaurar padrões** → marque `Enable global hotkeys` → Guard = `Always` → **Apply**.
   Status esperado: `Registrados 6 hotkeys (guard=Always, double-press=ON 350ms).` (previous compartilha o F1)
2. Fora do jogo (Bloco de Notas pequeno focado): `Numpad+`/`Numpad-` → volume muda; `F2` → pausa/retoma; `F1` → próximo (após ~350ms); `F1 F1` rápido → anterior; `→`/`←` → ±5s.
3. No BNSR fullscreen: mesmos testes — agora funcionam (guard `Always`).
   ⚠️ O jogo TAMBÉM recebe as teclas (diferente do AHK que suprimia). Confira os binds de F1/F2 no BNSR.
4. Se nada disparar: confira se o SideScreen continua aberto (só 1 instância registra) e o Status da tela.
   Fallback: rode o SideScreen como administrador (botão direito → Executar como administrador).

## Fase 5 (Monitores)

Ver `monitors.md` (trava removida; tela informativa).

## Fase 6 (Persistência — implementado)

Dono único: `MainViewModel.UpdateConfig()` (único caminho de escrita). Arquivo versionado com migrações.

- [ ] Mude shuffle (Dashboard), player (Players), monitor (Display), atalhos (Shortcuts) → feche e reabra → tudo igual.
- [ ] Corrompa o config (apague metade no Bloco de Notas) → app abre com padrões e cria `config.corrupt-*.json`.
- [ ] `dotnet test` cobre roundtrip, corrupt+backup e migração v1→v4.

## Debug

- Spy++ (`Find Window`) para confirmar classe `PotPlayer64`.
- `%AppData%\SideScreen\hotkeys.log` (hotkeys) e `config.json` (+ `config.corrupt-*`).
- `dotnet --version` deve ser 10.x.
