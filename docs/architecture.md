# Arquitetura SideScreen

## Princípio

`UI -> Core (interfaces) <- Infrastructure (Win32 + players)`

- **Core** não referencia UI nem `user32.dll`. Só modelos + interfaces. É testável com xUnit puro.
- **Infrastructure** implementa `IPlayerController` por player + serviços Win32.
- **UI** só conhece `IPlayerController`. Trocar player = injeção de dependência, zero `if` na UI.

## Módulos

| Projeto | Responsabilidade | Pode usar |
|---|---|---|
| `SideScreen.Core` | `IPlayerController`, `PlayerStatus`, `AppConfig` | Só BCL |
| `SideScreen.Infrastructure` | `PotPlayerController`, `PotPlayerCommandIds`, futuro `WindowFinder`, `HotkeyService`, `MonitorService` | Core + P/Invoke |
| `SideScreen.UI` | WPF, MVVM, Design System `#070707` | Core + Infrastructure |
| `SideScreen.Core.Tests` | Contratos + defaults seguros | Core + Infrastructure |

## Como adicionar um novo player (ex: MPC-HC)

1. Criar `src/SideScreen.Infrastructure/Players/MpcHcController.cs : IPlayerController` com `Id="mpc-hc"`.
2. Implementar `IsRunning/GetWindowHandle` via `FindWindow` da classe do MPC-HC.
3. Implementar comandos via `WM_COMMAND` do MPC-HC (ele tem tabela pública, mais fácil que PotPlayer).
4. Registrar no seletor de players (Fase 2). Nenhuma mudança no Dashboard.

## Decisões Fase 1

- `.slnx` (novo formato solution XML do .NET 10) em vez de `.sln` clássico — suportado pelo `dotnet` CLI e VS 2022 17.13+.
- `net10.0` para Core/Infra/Tests, `net10.0-windows` + `UseWPF` para UI.
- `PotPlayerController` é stub proposital — `SendMessage` real só na Fase 3 após validar com Spy++.

## Regras XAML (aprendidas com 2 crashes em produção, 2026-09-21)

1. **Nunca `BasedOn="{StaticResource ...}"` dentro de DataTemplate** — vira `NamedObject` e crasha
   (`InvalidCastException` em `OnItemContainerStyleChanged` ao navegar). Estilos com BasedOn vivem
   no `DesignSystem.xaml` e são referenciados prontos.
2. **Nunca referência `StaticResource` para frente no mesmo dicionário** (usar antes de definir) —
   mesma classe de crash (caso real: `SettingCombo` → `ComboItem`). Definir dependências primeiro.
3. Verificação: sonda STA temporária que instancia as 5 telas com dados reais
   (`Measure`+`Arrange`+`UpdateLayout`) — smoke test não navega, então não pega isso.
