namespace SideScreen.Infrastructure.Players;

/// <summary>
/// IDs do protocolo PotPlayer descobertos via SDK oficial (fórum Daum) + comunidade AHK.
/// Fase 3 vai usar estes IDs com SendMessage. Fase 1: só documenta, não envia nada.
/// </summary>
public static class PotPlayerCommandIds
{
    // WM_COMMAND (0x0111)
    public const int WmCommand = 0x0111;
    public const int CmdPlay = 20001;
    public const int CmdPause = 20000;
    public const int CmdStop = 20002;
    public const int CmdPlayPause = 10014;
    public const int CmdNext = 10124;
    public const int CmdPrevious = 10123;
    public const int CmdVolumeUp = 10035;
    public const int CmdVolumeDown = 10036;

    // POT_COMMAND (WM_USER = 0x0400)
    public const int PotCommand = 0x0400;
    public const int PotGetVolume = 0x5000;      // 0-100
    public const int PotSetVolume = 0x5001;      // 0-100
    public const int PotGetTotalTime = 0x5002;   // ms
    public const int PotGetCurrentTime = 0x5004; // ms
    public const int PotSetCurrentTime = 0x5005; // ms
    public const int PotGetPlayStatus = 0x5006;  // -1:Stopped, 1:Paused, 2:Running
    public const int PotSetPlayStatus = 0x5007;  // 0:Toggle, 1:Paused, 2:Running

    // Classes de janela (32 vs 64-bit)
    public const string WindowClass64 = "PotPlayer64";
    public const string WindowClass32 = "PotPlayer";
}
