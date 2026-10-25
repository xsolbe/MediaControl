# MediaControl

MediaControl is a compact Windows controller for [PotPlayer](https://potplayer.daum.net/). It lets you skip tracks, pause, seek, and change PotPlayer's volume while another app or game is in the foreground. It is based on the `MusicControl.ahk` workflow, with configurable hotkeys, a program allowlist, and a system tray icon.

The interface supports **English (default)** and **Português**. Change it anytime in **General**.

## Screenshots

| Playback | Allowed programs |
| --- | --- |
| ![Playback controls](docs/screenshots/playback-en.png) | ![Program allowlist](docs/screenshots/programs-en.png) |

| Hotkeys | Language |
| --- | --- |
| ![Configurable hotkeys](docs/screenshots/hotkeys-en.png) | ![English and Portuguese settings](docs/screenshots/general-en.png) |

[Open window picker](docs/screenshots/window-picker-en.png) · [Interface em português](docs/screenshots/general-pt-BR.png)

## Download and use

1. Download `MediaControl.exe` from the [latest release](https://github.com/xsolbe/MediaControl/releases/latest).
2. Start PotPlayer, then run MediaControl. The app requests administrator permission so its hotkeys also work while an elevated game is active.
3. In **Programs**, add a running window or choose an executable. Adding a program switches to **Listed only** automatically. MediaControl then responds to hotkeys only while a listed program is in the foreground. Outside the list, those keys remain available to the active app.
4. Use **Hotkeys** to change a binding, then click **Apply**. Use **General** to change the interface language.

The **−** button sends MediaControl to the system tray. Click the tray icon to reopen it, or right-click for **Open MediaControl** and **Exit**. The **×** button exits. Settings are saved in `%AppData%\MediaControl\settings.json`.

### Default hotkeys

| Key | PotPlayer action |
| --- | --- |
| F1 | Next track |
| F2 | Play / pause |
| Numpad + | Increase volume |
| Numpad − | Decrease volume |
| ← | Seek backward |
| → | Seek forward |

The playback panel also has a previous-track button, a timeline, and volume and mute controls. The default commands use the same PotPlayer messages as the original AHK script. The icons are from [Lucide](https://lucide.dev/); its license is included in [`Assets/Lucide/LUCIDE-LICENSE.txt`](Assets/Lucide/LUCIDE-LICENSE.txt).

## Português

O MediaControl controla músicas no PotPlayer enquanto você usa outro programa ou joga. F1 passa para a próxima faixa, F2 pausa ou continua, as teclas do teclado numérico ajustam o volume e as setas avançam ou voltam na música. Os atalhos podem ser alterados na aba **Atalhos**.

Em **Programas**, adicione um jogo pela janela aberta ou pelo arquivo executável. Isso ativa **Somente lista**: os atalhos controlam o PotPlayer apenas quando um programa cadastrado está em primeiro plano. Fora da lista, as teclas continuam disponíveis para o programa ativo. Na aba **Geral**, selecione **Português**. O botão **−** envia o app para a bandeja; **×** fecha o app.

## Build

Requires the .NET 10 SDK on Windows. The release executable is self-contained:

```powershell
dotnet publish MediaControl.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o publish
```

The result is `publish\MediaControl.exe`.
