using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using SideScreen.UI.ViewModels;

namespace SideScreen.UI.Views;

public partial class ShortcutsView : UserControl
{
    private HwndSource? _source;
    private const int WmHotkey = 0x0312;

    public ShortcutsView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private ShortcutsViewModel Vm => (ShortcutsViewModel)DataContext;

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        var window = Window.GetWindow(this);
        if (window == null) return;
        var hWnd = new WindowInteropHelper(window).Handle;
        if (hWnd == nint.Zero) return;

        Vm.AttachHwnd(hWnd);
        _source = HwndSource.FromHwnd(hWnd);
        _source?.AddHook(WndProc);
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        _source?.RemoveHook(WndProc);
        _source = null;
    }

    private IntPtr WndProc(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WmHotkey)
        {
            handled = Vm.HandleHotkey(wParam.ToInt32());
        }
        return IntPtr.Zero;
    }

    /// <summary>Captura o combo pressionado e escreve o gesto canônico na linha (ex: Ctrl+Alt+P).</summary>
    private void GestureBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (sender is not TextBox box || box.DataContext is not ShortcutEdit row)
            return;

        var gesture = BuildGesture(e);
        if (gesture is null)
            return; // Tab/Enter/Escape/modificador puro: deixa o comportamento padrão.

        row.Gesture = gesture;
        e.Handled = true;
    }

    private static string? BuildGesture(KeyEventArgs e)
    {
        Key main = e.Key == Key.System ? e.SystemKey : e.Key;

        // Modificadores puros: espera a tecla principal.
        if (main is Key.LeftCtrl or Key.RightCtrl or Key.LeftAlt or Key.RightAlt
            or Key.LeftShift or Key.RightShift or Key.LWin or Key.RWin or Key.System)
            return null;

        // Navegação/cancelamento: não captura.
        if (main is Key.Tab or Key.Enter or Key.Escape)
            return null;

        var mods = Keyboard.Modifiers;
        var parts = new List<string>();
        if (mods.HasFlag(ModifierKeys.Control)) parts.Add("Ctrl");
        if (mods.HasFlag(ModifierKeys.Alt)) parts.Add("Alt");
        if (mods.HasFlag(ModifierKeys.Shift)) parts.Add("Shift");
        if (mods.HasFlag(ModifierKeys.Windows)) parts.Add("Win");

        var keyName = MapKey(main);
        if (keyName is null) return null;
        parts.Add(keyName);
        return string.Join("+", parts);
    }

    private static string? MapKey(Key k)
    {
        // Letras A-Z
        if (k is >= Key.A and <= Key.Z) return k.ToString();
        // Dígitos D0-D9
        if (k is >= Key.D0 and <= Key.D9) return k.ToString()[1..];
        // NumPad
        if (k is >= Key.NumPad0 and <= Key.NumPad9) return k.ToString().Replace("NumPad", "");
        // F1-F24
        if (k is >= Key.F1 and <= Key.F24) return k.ToString();
        return k switch
        {
            Key.Up => "Up",
            Key.Down => "Down",
            Key.Left => "Left",
            Key.Right => "Right",
            Key.Space => "Space",
            Key.Insert => "Insert",
            Key.Delete => "Delete",
            Key.Home => "Home",
            Key.End => "End",
            Key.PageUp => "PageUp",
            Key.PageDown => "PageDown",
            Key.Add => "NumpadAdd",
            Key.Subtract => "NumpadSub",
            Key.Multiply => "NumpadMult",
            Key.Divide => "NumpadDiv",
            Key.VolumeUp => "VolumeUp",
            Key.VolumeDown => "VolumeDown",
            Key.VolumeMute => "VolumeMute",
            Key.MediaNextTrack => "MediaNext",
            Key.MediaPreviousTrack => "MediaPrev",
            Key.MediaPlayPause => "MediaPlayPause",
            _ => null,
        };
    }
}
