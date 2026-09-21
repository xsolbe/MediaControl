using System.Runtime.InteropServices;
using SideScreen.Core.Players;
using SideScreen.Infrastructure.Windows;

namespace SideScreen.Infrastructure.Display;

/// <summary>
/// "Lock" por reposicionamento (não intercepta arrasto — menos intrusivo).
/// Polling 750ms: se o player ficar fora do monitor alvo por 3 leituras seguidas (~2,25s),
/// devolve preservando tamanho/offset, sem roubar foco (SWP_NOACTIVATE).
/// Nunca move maximizado/fullscreen; player fechado = Offline.
/// </summary>
public sealed class MonitorLockService : IDisposable
{
    private readonly IPlayerController _player;
    private readonly Func<List<DisplayMonitor>> _listMonitors;
    private System.Threading.Timer? _timer;
    private string _targetDevice = "";
    private int _offCount;
    private (int x, int y)? _anchor; // lugar exato no monitor alvo (atualizado enquanto estável por lá)
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
        _offCount = 0;
        _anchor = null; // reaprende a posição quando estabilizar no alvo
        IsRunning = true;
        SetStatus($"Vigiando {targetDevice}.");
        _timer = new System.Threading.Timer(_ => Tick(), null, 0, 750);
    }

    public void Stop()
    {
        _timer?.Dispose();
        _timer = null;
        IsRunning = false;
        _offCount = 0;
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
                _offCount = 0;
                SetStatus("Player fechado (Offline) — aguardando abrir.");
                return;
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

            // Maximizado? Não move.
            if (IsMaximized(h))
            {
                _offCount = 0;
                SetStatus($"Maximizado — não movo (desmaximize para travar).");
                return;
            }

            if (current is not null && current.DeviceKey == target.DeviceKey)
            {
                _offCount = 0;
                _anchor = (rc.Left, rc.Top); // memoriza o lugar exato enquanto estável no alvo
                SetStatus($"OK no {target.Label.Split('—')[0].Trim()} (posição travada).");
                return;
            }

            // Fullscreen/borderless cobrindo o monitor atual? Não move, só avisa.
            if (current is not null && MonitorLayout.CoversMonitor(rc.Left, rc.Top, w, hgt, current))
            {
                _offCount = 0;
                SetStatus("Fullscreen detectado — não movo (saia do fullscreen para travar).");
                return;
            }

            _offCount++;
            if (_offCount < 3)
            {
                SetStatus($"Fora do alvo ({_offCount}/3) — aguardando estabilizar (arrasto em curso?).");
                return;
            }

            _offCount = 0;
            var from = current ?? target;
            var (nx, ny) = MonitorLayout.RestorePosition(rc.Left, rc.Top, w, hgt, _anchor, from.Left, from.Top, target.Left, target.Top, target.Width, target.Height);
            bool ok = NativeMethods.SetWindowPos(h, nint.Zero, nx, ny, 0, 0,
                NativeMethods.SwpNoSize | NativeMethods.SwpNoZOrder | NativeMethods.SwpNoActivate);
            SetStatus(ok ? $"Devolvido à posição travada no {target.Label.Split('—')[0].Trim()}." : "Falha ao reposicionar (acesso negado?).");
        }
        catch (Exception ex)
        {
            SetStatus($"Erro no vigia: {ex.Message}");
        }
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
