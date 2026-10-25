using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using MediaControl.Interop;
using MediaControl.Models;
using MediaControl.Localization;

namespace MediaControl.Services;

internal sealed class GlobalHotkeys : IDisposable
{
    private const int PauseHotkeyId = 100;
    private const uint F9VirtualKey = 0x78;
    private const uint ModNoRepeat = 0x4000;
    private readonly IntPtr _window;
    private readonly HwndSource _source;
    private readonly Action<string> _onAction;
    private readonly Action _onTogglePause;
    private readonly Dictionary<int, string> _registered = new();
    private Dictionary<string, HotkeyGesture> _active = new();
    private bool _suspended;
    private bool _scopeEnabled;
    private readonly bool _pauseHotkeyRegistered;

    public GlobalHotkeys(Window owner, Action<string> onAction, Action onTogglePause, bool scopeEnabled = true)
    {
        _window = new WindowInteropHelper(owner).EnsureHandle();
        _source = HwndSource.FromHwnd(_window)
            ?? throw new InvalidOperationException(UiText.T("Janela do MediaControl indisponível."));
        _source.AddHook(WndProc);
        _onAction = onAction;
        _onTogglePause = onTogglePause;
        _scopeEnabled = scopeEnabled;
        _pauseHotkeyRegistered = NativeMethods.RegisterHotKey(_window, PauseHotkeyId, ModNoRepeat, F9VirtualKey);
        PauseHotkeyError = _pauseHotkeyRegistered ? "" :
            $"{UiText.T("Não foi possível registrar F9. Código Windows: ")}{Marshal.GetLastWin32Error()}.";
    }

    public int RegisteredCount => _registered.Count;
    public string PauseHotkeyError { get; }

    public bool TryApply(IReadOnlyDictionary<string, HotkeyGesture> bindings, out string error)
    {
        if (!Validate(bindings, out error))
            return false;

        if (!_scopeEnabled || _suspended)
        {
            _active = Copy(bindings);
            error = "";
            return true;
        }

        var previous = Copy(_active);
        UnregisterAll();
        _suspended = false;

        if (Register(bindings, out error))
        {
            _active = Copy(bindings);
            return true;
        }

        UnregisterAll();
        _active = previous;
        if (previous.Count != 0 && !Register(previous, out string rollbackError))
            error += $" {UiText.T("Também falhou a restauração: ")}{rollbackError}";
        return false;
    }

    public void Suspend()
    {
        if (_suspended)
            return;
        UnregisterAll();
        _suspended = true;
    }

    public bool Resume(out string error)
    {
        error = "";
        if (!_suspended)
            return true;
        _suspended = false;
        if (!_scopeEnabled || _active.Count == 0)
            return true;
        if (Register(_active, out error))
            return true;
        UnregisterAll();
        _scopeEnabled = false;
        return false;
    }

    public bool SetScopeEnabled(bool enabled, out string error)
    {
        error = "";
        if (enabled == _scopeEnabled)
            return true;
        if (!enabled)
        {
            UnregisterAll();
            _scopeEnabled = false;
            return true;
        }
        if (_suspended || _active.Count == 0)
        {
            _scopeEnabled = true;
            return true;
        }
        if (Register(_active, out error))
        {
            _scopeEnabled = true;
            return true;
        }
        UnregisterAll();
        return false;
    }

    private static Dictionary<string, HotkeyGesture> Copy(IReadOnlyDictionary<string, HotkeyGesture> source) =>
        source.ToDictionary(pair => pair.Key, pair => pair.Value.Copy());

    private static bool Validate(IReadOnlyDictionary<string, HotkeyGesture> bindings, out string error)
    {
        var used = new HashSet<(uint, uint)>();
        foreach (var (id, label) in ControlActions.All)
        {
            if (!bindings.TryGetValue(id, out var gesture) || !gesture.IsValid)
            {
                error = $"{UiText.T("Atalho inválido para ")}{UiText.T(label)}.";
                return false;
            }
            if (gesture.Modifiers == 0 && gesture.VirtualKey == F9VirtualKey)
            {
                error = UiText.T("F9 está reservado para pausar os atalhos.");
                return false;
            }
            if (!used.Add((gesture.Modifiers, gesture.VirtualKey)))
            {
                error = $"{UiText.T("A tecla ")}{gesture.Display}{UiText.T(" aparece em duas ações.")}";
                return false;
            }
        }
        error = "";
        return true;
    }

    private bool Register(IReadOnlyDictionary<string, HotkeyGesture> bindings, out string error)
    {
        int id = 1;
        foreach (var (action, label) in ControlActions.All)
        {
            var gesture = bindings[action];
            if (!NativeMethods.RegisterHotKey(_window, id, gesture.Modifiers, gesture.VirtualKey))
            {
                int code = Marshal.GetLastWin32Error();
                error = $"{UiText.T(label)} ({gesture.Display}) {UiText.T("não pôde ser registrado. Código Windows: ")}{code}.";
                return false;
            }
            _registered[id++] = action;
        }
        error = "";
        return true;
    }

    private IntPtr WndProc(IntPtr window, int message, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (message == NativeMethods.WmHotkey && wParam.ToInt32() == PauseHotkeyId)
        {
            handled = true;
            _onTogglePause();
            return IntPtr.Zero;
        }
        if (message == NativeMethods.WmHotkey && _registered.TryGetValue(wParam.ToInt32(), out string? action))
        {
            handled = true;
            _onAction(action);
        }
        return IntPtr.Zero;
    }

    private void UnregisterAll()
    {
        foreach (int id in _registered.Keys)
            NativeMethods.UnregisterHotKey(_window, id);
        _registered.Clear();
    }

    public void Dispose()
    {
        UnregisterAll();
        if (_pauseHotkeyRegistered)
            NativeMethods.UnregisterHotKey(_window, PauseHotkeyId);
        _source.RemoveHook(WndProc);
    }
}
