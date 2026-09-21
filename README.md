# SideScreen

Controle minimalista do player de vídeo no segundo monitor — sem interferir em jogos.

**Stack:** C# + .NET 10 LTS + WPF (MVVM) · **Player v1:** PotPlayer via `SendMessage` (sem simular teclas) · **Status:** Design System v1 aplicado (38 testes)

## Por que este stack?

- Standalone Windows, baixo consumo (~15-25MB single-file, ~40-60MB RAM idle)
- Acesso direto a Win32 (`FindWindow`, `SendMessage`, `RegisterHotKey`, `MonitorFromWindow`)
- Single-file `.exe` + instalador MSIX/WiX na Fase 8
- `Core` independente de UI → adicionar MPC-HC = criar 1 classe nova

## Estrutura

```text
SideScreen/
├── docs/                  # arquitetura, protocolos, guias de teste
├── src/
│   ├── SideScreen.Core/           # IPlayerController, PlayerStatus, AppConfig (sem UI, sem Win32)
│   ├── SideScreen.Infrastructure/ # PotPlayerController, PotPlayerCommandIds, (Fase 3-5: Win32)
│   └── SideScreen.UI/             # WPF MVVM, Dark #070707
├── tests/
│   └── SideScreen.Core.Tests/     # xUnit
└── SideScreen.slnx
```

## Como rodar localmente (Fase 2)

```powershell
# 1. Pré-requisito: .NET 10 SDK
dotnet --version  # deve mostrar 10.x

# 2. Build
dotnet build SideScreen.slnx

# 3. Testes
dotnet test SideScreen.slnx

# 4. Rodar UI
dotnet run --project src/SideScreen.UI/SideScreen.UI.csproj
```

Esperado: janela escura `#070707` com sidebar (Dashboard, Players, Shortcuts, Display, Settings).

## Config futura (Fase 6)

`%AppData%\SideScreen\config.json` — player selecionado, atalhos, shuffle, monitor, lock.

## Roadmap

- [x] Fase 0 — Planejamento
- [x] Fase 1 — Projeto inicial
- [x] Fase 2 — Interface (Dashboard, Players, Shortcuts, Display, Settings)
- [x] Fase 3 — PotPlayer (`FindWindow` + `SendMessage`) (leitura validada, ações p/ testar no Dashboard)
- [x] Fase 4 — Atalhos contextuais + Modo Jogo + process scope
- [x] Fase 5 — Monitores (lista real; trava REMOVIDA após validação — post-mortem em docs/monitors.md)
- [x] Fase 6 — Persistência central (dono único + backup + migrações) (esta versão)
- [ ] Fase 6 — Persistência
- [ ] Fase 7 — Testes
- [ ] Fase 8 — Build `.exe` + instalador
- [x] Fase 9 — GitHub `xsolbe/SideScreen` (conectado, push Fase 1 OK)

## Docs

- `docs/architecture.md` — responsabilidades de cada módulo
- `docs/potplayer-protocol.md` — como falamos com o PotPlayer
- `docs/hotkeys.md` — por que não usamos F1/F2 globais
- `docs/monitors.md` — lock via reposicionamento
- `docs/testing-guide.md` — como testar cada funcionalidade
