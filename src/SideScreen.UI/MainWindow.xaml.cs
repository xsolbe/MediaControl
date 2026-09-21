using System.Windows;
using System.Windows.Interop;
using SideScreen.Infrastructure.Hotkeys;
using SideScreen.UI.ViewModels;

namespace SideScreen.UI;

/// <summary>
/// Shell MVVM + dono do hook WM_HOTKEY (hotkeys registrados no startup via MainViewModel).
/// </summary>
public partial class MainWindow : Window
{
    private const int WmHotkey = 0x0312;
    private HwndSource? _source;
    private bool _hooked;

    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel();
        Loaded += OnLoaded;
        ContentRendered += (_, _) => TryHook("contentRendered");
        Closed += (_, _) => Vm.Detach();
    }

    private MainViewModel Vm => (MainViewModel)DataContext;

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        var hWnd = new WindowInteropHelper(this).Handle;
        if (hWnd == nint.Zero)
        {
            HotkeyLog.Append("attach FAILED (hWnd zero)");
            return;
        }
        Vm.AttachHwnd(hWnd);
        TryHook("loaded");
    }

    private void TryHook(string phase)
    {
        if (_hooked) return;
        try
        {
            var hWnd = new WindowInteropHelper(this).Handle;
            _source = HwndSource.FromHwnd(hWnd);
            if (_source == null)
            {
                HotkeyLog.Append($"HOOK FAILED ({phase}): HwndSource null");
                return;
            }
            _source.AddHook(WndProc);
            _hooked = true;
            HotkeyLog.Append($"hook attached ({phase})");
        }
        catch (Exception ex)
        {
            HotkeyLog.Append($"HOOK FAILED ({phase}): {ex.Message}");
        }
    }

    private IntPtr WndProc(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WmHotkey)
            handled = Vm.HandleHotkey(wParam.ToInt32());
        return IntPtr.Zero;
    }
}
