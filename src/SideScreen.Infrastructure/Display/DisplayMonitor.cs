namespace SideScreen.Infrastructure.Display;

/// <summary>Monitor físico (de EnumDisplayMonitors + GetMonitorInfo).</summary>
public sealed record DisplayMonitor(
    string DeviceKey,   // ex: "\\.\DISPLAY2" — estável entre reboots, usado para persistir a escolha
    string Label,       // ex: "Monitor 2 — 1440x2560"
    int Left, int Top, int Width, int Height,
    bool IsPrimary);
