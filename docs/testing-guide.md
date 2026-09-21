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

## Fase 3 (PotPlayer)

1. Abrir PotPlayer com 1 vídeo + playlist de 3 vídeos.
2. Clicar Play/Pause → alterna sem focar o player.
3. Next/Prev → troca faixa.
4. Slider volume → `GET_VOLUME` reflete.
5. Fechar player → Offline, sem crash.

## Fase 4 (Atalhos)

1. Com jogo fullscreen em foco, apertar `Ctrl+Alt+P` → pausa sem afetar jogo.
2. Tentar cadastrar `F1` sozinho → warning exibido.
3. Double-press desligado + Modo Jogo → sem delay.

## Fase 5 (Monitores)

Ver `monitors.md`.

## Debug

- Spy++ (`Find Window`) para confirmar classe `PotPlayer64`.
- `%AppData%\SideScreen\logs\` (Fase 6+).
- `dotnet --version` deve ser 10.x.
