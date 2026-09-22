# MediaControl

Controle o **PotPlayer no segundo monitor sem sair do jogo**.

O MediaControl é um utilitário Windows que pilota o seu player de vídeo em segundo plano:
pause, troque de vídeo, ajuste o volume ou pule 5 segundos com um toque no teclado —
mesmo com o jogo em tela cheia no monitor principal.

Ele nasceu para aposentar um script de AutoHotkey (`MusicControl`), virando um app
standalone com interface própria, atalhos configuráveis e instalação simples.

## Funcionalidades

| Recurso | Detalhe |
|---|---|
| Play / Pause | `F2` (padrão) |
| Próximo vídeo | `F1` |
| Vídeo anterior | `F1` duas vezes |
| Volume | `Numpad +` / `Numpad -` e slider na tela |
| Avançar / voltar 5s | Setas `→` / `←` e botões na tela |
| Aleatório | Toggle do modo shuffle do player |
| Atalhos globais | Todos reconfiguráveis, com detecção de conflito |
| Modo jogo | `Guard`: dispara sempre, pausa em fullscreen, só no player ou só nos programas que você marcar |
| Mover para o monitor | Escolha o monitor e o player vai para ele (posição lembrada por monitor) |
| Status ao vivo | Tocando / Pausado / Parado, volume e janela detectados sem roubar foco |
| Interface | Dark e Light, português (BR) e inglês |

## Como funciona

Em vez de simular teclas, o app conversa direto com o PotPlayer via mensagens do Windows
(`SendMessage`) — funciona minimizado, sem foco e sem interferir no que você está fazendo.
Atalhos usam `RegisterHotKey` (sem hooks invasivos) e nada sai da sua máquina:
sem conta, sem rede, sem telemetria. Config em `%AppData%\MediaControl\config.json`.

## Requisitos

- Windows 10/11 64-bit
- [PotPlayer](https://potplayer.daum.net/) instalado (primeiro player suportado; MPC-HC no roadmap)

## Instalação

1. Baixe o `MediaControl.exe` na [**última release**](https://github.com/xsolbe/MediaControl/releases/latest).
2. Coloque onde quiser e execute. Não precisa instalar nem ter .NET (já vai embutido).
3. Abra o PotPlayer, depois o MediaControl.

## Uso rápido

1. Na tela **Shortcuts**, marque *Enable global hotkeys* → **Apply** (rodapé mostra `Hotkeys: ON · 6`).
2. Jogando: `F2` pausa, `F1` próximo, `Numpad +/-` volume, setas ±5s.
3. Em **Display**, escolha o monitor para levar o player até ele.
4. Feche e reabra quando quiser — tudo é salvo automaticamente.

> Se algum atalho não disparar no jogo: confira se outro programa o registrou (o app avisa
> `código 1409`), ou rode como administrador caso o jogo seja elevado.

## Roadmap

- [ ] MPC-HC e outros players (arquitetura pronta: um controller por player)
- [ ] Minimizar para a bandeja
- [ ] Instalador com atalho no menu iniciar

## Licença

MIT — ver [LICENSE](LICENSE).
