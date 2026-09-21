using System.Windows;
using System.Windows.Interop;
using SideScreen.UI.ViewModels;

namespace SideScreen.UI;

/// <summary>
/// Shell MVVM + dono do hook WM_HOTKEY (hotkeys registrados no startup via MainViewModel).
/// </summary>
public partial class MainWindow : Window
{
    private const int WmHotkey = 0x0312;
    private HwndSource? _source;

    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel();
        Loaded += OnLoaded;
        Closed += (_, _) => Vm.Detach();
    }

    private MainViewModel Vm => (MainViewModel)DataContext;

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        var hWnd = new WindowInteropHelper(this).Handle;
        if (hWnd == nint.Zero) return;
        Vm.AttachHwnd(hWnd);
        _source = HwndSource.FromHwnd(hWnd);
        _source?.AddHook(WndProc);
    }

    private IntPtr WndProc(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WmHotkey)
            handled = Vm.HandleHotkey(wParam.ToInt32());
        return IntPtr.Zero;
    }
}
