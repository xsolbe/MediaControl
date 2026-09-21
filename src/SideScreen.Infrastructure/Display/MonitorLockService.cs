using System.Runtime.InteropServices;
using SideScreen.Core.Players;
using SideScreen.Infrastructure.Windows;

namespace SideScreen.Infrastructure.Display;

/// <summary>
/// Lock "parede magnética" (sem interceptar arrasto — sem injeção, sem hook global).
/// Polling 100ms: no alvo, memoriza a posição (âncora); fora do alvo durante arrasto,
/// segura na borda (a janela desliza sem sair); fora com mouse solto, restaura a âncora na hora.
/// Sem roubar foco (SWP_NOACTIVATE). Nunca move maximizado/fullscreen; player fechado = Offline.
/// </summary>
public sealed class MonitorLockService : IDisposable
{
    public enum LockAction { UpdateAnchor, HoldEdge, RestoreAnchor, Skip }

    public static LockAction Decide(bool onTarget, bool blocked, bool dragging) =>
        onTarget ? LockAction.UpdateAnchor
        : blocked ? LockAction.Skip
        : dragging ? LockAction.HoldEdge
        : LockAction.RestoreAnchor;

    private const int VkLButton = 0x01;

    private readonly IPlayerController _player;
    private readonly Func<List<DisplayMonitor>> _listMonitors;
    private System.Threading.Timer? _timer;
    private string _targetDevice = "";
    private (int x, int y)? _anchor; // lugar exato no monitor alvo (atualizado enquanto estável por lá)
    private bool _shielded; // click-through aplicado?
    private bool _disposed;

    public MonitorLockService(IPlayerController player) : this(player, MonitorService.List) { }

    internal MonitorLockService(IPlayerController player, Func<List<DisplayMonitor>> listMonitors)
    {
        _player = player;
        _listMonitors = listMonitors;
    }

    public bool IsRunning { get; private set; }
    public string Status { get; private set; } = "Lock desligado.";
    public event Action? StatusChanged;

    public void Start(string targetDevice)
    {
        Stop();
        _targetDevice = targetDevice ?? "";
        _anchor = null; // reaprende a posição quando estabilizar no alvo
        _shielded = false;
        IsRunning = true;
        SetStatus($"Vigiando {targetDevice} (blindado + posição travada).");
        _timer = new System.Threading.Timer(_ => Tick(), null, 0, 100);
    }

    public void Stop()
    {
        RemoveShield();
        _timer?.Dispose();
        _timer = null;
        IsRunning = false;
        _anchor = null;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Stop();
    }

    private void Tick()
    {
        try
        {
            var h = _player.GetWindowHandle();
            if (h == nint.Zero)
            {
                _anchor = null;
                SetStatus("Player fechado (Offline) — aguardando abrir.");
                return;
            }

            // Blindagem: sem ela o mouse agarra a janela. Tenta até aplicar.
            if (!_shielded)
            {
                _shielded = WindowInteraction.SetClickThrough(h, true);
                if (!_shielded)
                {
                    SetStatus("Ativando blindagem do mouse...");
                    return;
                }
            }

            var monitors = _listMonitors();
            var target = monitors.FirstOrDefault(m => m.DeviceKey == _targetDevice)
                ?? monitors.FirstOrDefault(m => m.IsPrimary);
            if (target is null)
            {
                SetStatus("Nenhum monitor encontrado.");
                return;
            }

            if (!NativeMethods.GetWindowRect(h, out var rc))
            {
                SetStatus("Janela inacessível no momento.");
                return;
            }

            int w = rc.Right - rc.Left, hgt = rc.Bottom - rc.Top;
            var current = monitors.FirstOrDefault(m => IsOnMonitor(h, m))
                ?? Nearest(monitors, rc.Left + w / 2, rc.Top + hgt / 2);

            bool onTarget = current is not null && current.DeviceKey == target.DeviceKey;
            bool blocked = IsMaximized(h)
                || (current is not null && MonitorLayout.CoversMonitor(rc.Left, rc.Top, w, hgt, current));
            bool dragging = IsDragging();

            switch (Decide(onTarget, blocked, dragging))
            {
                case LockAction.UpdateAnchor:
                    _anchor = (rc.Left, rc.Top); // memoriza o lugar exato enquanto estável no alvo
                    SetStatus($"Blindado no {target.Label.Split('—')[0].Trim()} (mouse atravessa; posição travada).");
                    return;

                case LockAction.Skip:
                    SetStatus(blocked && IsMaximized(h)
                        ? "Maximizado — não movo (desmaximize para travar)."
                        : "Fullscreen detectado — não movo (saia do fullscreen para travar).");
                    return;

                case LockAction.HoldEdge:
                {
                    // Parede: prende no ponto mais próximo DENTRO do alvo enquanto arrasta.
                    var (nx, ny) = MonitorLayout.ClampIntoBounds(
                        rc.Left, rc.Top, w, hgt,
                        current?.Left ?? target.Left, current?.Top ?? target.Top,
                        target.Left, target.Top, target.Width, target.Height);
                    if (nx != rc.Left || ny != rc.Top)
                        NativeMethods.SetWindowPos(h, nint.Zero, nx, ny, 0, 0,
                            NativeMethods.SwpNoSize | NativeMethods.SwpNoZOrder | NativeMethods.SwpNoActivate);
                    SetStatus("Segurando na borda do monitor (solte para fixar).");
                    return;
                }

                default: // RestoreAnchor
                {
                    var from = current ?? target;
                    var (rx, ry) = MonitorLayout.RestorePosition(
                        rc.Left, rc.Top, w, hgt, _anchor,
                        from.Left, from.Top, target.Left, target.Top, target.Width, target.Height);
                    if (rx != rc.Left || ry != rc.Top)
                    {
                        bool ok = NativeMethods.SetWindowPos(h, nint.Zero, rx, ry, 0, 0,
                            NativeMethods.SwpNoSize | NativeMethods.SwpNoZOrder | NativeMethods.SwpNoActivate);
                        SetStatus(ok ? $"Devolvido à posição travada no {target.Label.Split('—')[0].Trim()}." : "Falha ao reposicionar (acesso negado?).");
                    }
                    return;
                }
            }
        }
        catch (Exception ex)
        {
            SetStatus($"Erro no vigia: {ex.Message}");
        }
    }

    private static bool IsDragging()
    {
        try { return (NativeMethods.GetAsyncKeyState(VkLButton) & 0x8000) != 0; }
        catch { return false; }
    }

    private void RemoveShield()
    {
        if (!_shielded) return;
        _shielded = false;
        try
        {
            var h = _player.GetWindowHandle();
            if (h != nint.Zero)
                WindowInteraction.SetClickThrough(h, false);
        }
        catch { }
    }

    private static bool IsOnMonitor(nint hWnd, DisplayMonitor m)
    {
        try
        {
            var found = NativeMethods.MonitorFromWindow(hWnd, NativeMethods.MonitorDefaultToNearest);
            if (found == nint.Zero) return false;
            var mi = new NativeMethods.MONITORINFO { cbSize = Marshal.SizeOf<NativeMethods.MONITORINFO>() };
            if (!NativeMethods.GetMonitorInfo(found, ref mi)) return false;
            return mi.rcMonitor.Left == m.Left && mi.rcMonitor.Top == m.Top
                && (mi.rcMonitor.Right - mi.rcMonitor.Left) == m.Width
                && (mi.rcMonitor.Bottom - mi.rcMonitor.Top) == m.Height;
        }
        catch { return false; }
    }

    private static DisplayMonitor? Nearest(List<DisplayMonitor> monitors, int x, int y)
    {
        DisplayMonitor? best = null;
        double bestDist = double.MaxValue;
        foreach (var m in monitors)
        {
            int cx = Math.Clamp(x, m.Left, m.Left + m.Width);
            int cy = Math.Clamp(y, m.Top, m.Top + m.Height);
            double d = Math.Pow(x - cx, 2) + Math.Pow(y - cy, 2);
            if (d < bestDist) { bestDist = d; best = m; }
        }
        return best;
    }

    private static bool IsMaximized(nint h)
    {
        try
        {
            var wp = new NativeMethods.WINDOWPLACEMENT { length = Marshal.SizeOf<NativeMethods.WINDOWPLACEMENT>() };
            if (!NativeMethods.GetWindowPlacement(h, ref wp)) return false;
            return wp.showCmd == NativeMethods.SwShowMaximized;
        }
        catch { return false; }
    }

    private void SetStatus(string s)
    {
        Status = $"[{DateTime.Now:HH:mm:ss}] {s}";
        try { StatusChanged?.Invoke(); }
        catch { }
    }
}
