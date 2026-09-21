using System.Runtime.InteropServices;
using SideScreen.Core.Hotkeys;
using SideScreen.Core.Players;

namespace SideScreen.Infrastructure.Hotkeys;

/// <summary>
/// Hotkeys globais via RegisterHotKey (sem hook low-level — não intercepta nada além do combo exato).
/// A UI fornece o HWND da janela principal e encaminha WM_HOTKEY para HandleHotkeyMessage.
/// Guard (Modo Jogo) decide se executa. Double-press Next→Previous é opt-in com delay documentado.
/// </summary>
public sealed class HotkeyService : IDisposable
{
    public const int IdBase = 0xA000;

    private readonly IPlayerController _player;
    private readonly ForegroundGuard _guard;
    private readonly Dictionary<int, string> _idToAction = new();
    private readonly Dictionary<string, int> _actionToId = new();
    private readonly DoublePressTracker _doubleTracker;
    private CancellationTokenSource? _pendingNext;
    private nint _hWnd;
    private bool _started;

    public HotkeyService(IPlayerController player, ForegroundGuard? guard = null, int doublePressWindowMs = 350)
    {
        _player = player;
        _guard = guard ?? new ForegroundGuard();
        _doubleTracker = new DoublePressTracker(doublePressWindowMs);
        DoublePressWindowMs = doublePressWindowMs;
    }

    public string GuardMode { get; set; } = GuardModes.PauseWhenFullscreen;
    public bool DoublePressEnabled { get; set; }
    public int DoublePressWindowMs { get; set; }
    /// <summary>Allowlist de processos (GuardModes.OnlyListed). Ex: ["BNSR"].</summary>
    public List<string> AllowedProcesses { get; set; } = [];
    public IReadOnlyDictionary<string, int> Registered => _actionToId;
    public List<string> Errors { get; } = [];
    public event Action<string>? Triggered;

    /// <summary>Registra todos os atalhos. Retorna false se algum falhou (ver Errors).</summary>
    public bool Start(nint hWnd, IDictionary<string, string> actionToGesture)
    {
        Stop();
        _hWnd = hWnd;
        Errors.Clear();
        HotkeyLog.Append($"Start hWnd=0x{hWnd:X} guard={GuardMode} doublePress={DoublePressEnabled} windowMs={DoublePressWindowMs}");
        _doubleTracker.WindowMs = DoublePressWindowMs;

        // Double-press (F1 x2 = anterior): next e previous compartilham a tecla por design.
        // Registra o F1 uma única vez (via "next"); o 2º toque vira Previous no HandleHotkeyMessage.
        bool sharedDoublePress = DoublePressEnabled
            && actionToGesture.TryGetValue("next", out var nextG)
            && actionToGesture.TryGetValue("previous", out var prevG)
            && HotkeyGesture.TryParse(nextG, out var ng)
            && HotkeyGesture.TryParse(prevG, out var pg)
            && ng.Canonical() == pg.Canonical();

        int i = 0;

        foreach (var (action, text) in actionToGesture)
        {
            i++;
            if (sharedDoublePress && action == "previous")
                continue; // coberto pelo F1 do "next" + DoublePressTracker
            if (!HotkeyGesture.TryParse(text, out var g))
            {
                Errors.Add($"{action}: gesto inválido '{text}'");
                continue;
            }
            if (!KeyMapper.TryGetVk(g.Key, out uint vk))
            {
                Errors.Add($"{action}: tecla '{g.Key}' não suportada para RegisterHotKey");
                continue;
            }

            int id = IdBase + i;
            uint mod = KeyMapper.Modifiers(g);
            bool ok;
            try { ok = Windows.NativeMethods.RegisterHotKey(hWnd, id, mod, vk); }
            catch (Exception ex) { Errors.Add($"{action} ({g}): {ex.Message}"); continue; }

            if (!ok)
            {
                int err = Marshal.GetLastWin32Error();
                // 1409 = já registrado por outro programa (ex: BnS-Multi-Tool); 87 = parâmetro inválido.
                Errors.Add($"{action} ({g}): código {err} (1409=em uso, 87=inválido)");
            }
            else
            {
                _idToAction[id] = action;
                _actionToId[action] = id;
                HotkeyLog.Append($"registered {action} ({g}) id={id}");
            }
        }

        foreach (var e in Errors)
            HotkeyLog.Append($"ERROR {e}");

        _started = true;
        return Errors.Count == 0;
    }

    /// <summary>Chamado pela UI ao receber WM_HOTKEY. Retorna true se era nosso.</summary>
    public bool HandleHotkeyMessage(int id)
    {
        if (!_started || !_idToAction.TryGetValue(id, out var action))
        {
            HotkeyLog.Append($"WM_HOTKEY id={id} desconhecido (started={_started})");
            return false;
        }

        HotkeyLog.Append($"WM_HOTKEY {action} guard={GuardMode}");
        if (!_guard.ShouldExecute(GuardMode, AllowedProcesses))
        {
            Triggered?.Invoke($"[{DateTime.Now:HH:mm:ss}] {action} ignorado pelo guard ({GuardMode})");
            HotkeyLog.Append($"{action} ignorado pelo guard ({GuardMode})");
            return true;
        }

        Execute(action);
        return true;
    }

    private void Execute(string action)
    {
        try
        {
            // Double-press opt-in: Next com delay; 2º toque dentro da janela vira Previous.
            if (DoublePressEnabled && action == "next")
            {
                var r = _doubleTracker.Press(DateTime.UtcNow);
                if (r == DoublePressTracker.Result.First)
                {
                    _pendingNext?.Cancel();
                    _pendingNext = new CancellationTokenSource();
                    var token = _pendingNext.Token;
                    Task.Delay(DoublePressWindowMs, token).ContinueWith(t =>
                    {
                        if (!t.IsCanceled)
                        {
                            _player.Next();
                            Triggered?.Invoke($"[{DateTime.Now:HH:mm:ss}] next (single após {DoublePressWindowMs}ms)");
                        }
                    });
                    return;
                }
                if (r == DoublePressTracker.Result.Second)
                {
                    _pendingNext?.Cancel();
                    _pendingNext = null;
                    _player.Previous();
                    Triggered?.Invoke($"[{DateTime.Now:HH:mm:ss}] previous (double-press)");
                    return;
                }
            }

            switch (action)
            {
                case "playPause": _player.PlayPause(); break;
                case "next": _player.Next(); break;
                case "previous": _player.Previous(); break;
                case "volumeUp": _player.VolumeUp(); break;
                case "volumeDown": _player.VolumeDown(); break;
                case "seekForward": _player.SeekForward(); break;
                case "seekBackward": _player.SeekBackward(); break;
                default: return;
            }
            HotkeyLog.Append($"executed {action}");
            Triggered?.Invoke($"[{DateTime.Now:HH:mm:ss}] {action} → {_player.DisplayName}");
        }
        catch (Exception ex)
        {
            HotkeyLog.Append($"ERROR {action}: {ex.Message}");
            Triggered?.Invoke($"[{DateTime.Now:HH:mm:ss}] {action} erro: {ex.Message}");
        }
    }

    public void Stop()
    {
        _pendingNext?.Cancel();
        _pendingNext = null;
        _doubleTracker.Reset();
        if (_hWnd != nint.Zero)
        {
            foreach (var id in _idToAction.Keys)
            {
                try { Windows.NativeMethods.UnregisterHotKey(_hWnd, id); }
                catch { }
            }
        }
        _idToAction.Clear();
        _actionToId.Clear();
        _hWnd = nint.Zero;
        _started = false;
    }

    public void Dispose() => Stop();
}
