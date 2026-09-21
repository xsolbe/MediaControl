using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using MediaControl.Infrastructure.Hotkeys;
using MediaControl.UI.ViewModels;

namespace MediaControl.UI;

/// <summary>
/// Shell: cromo próprio (TopBar), navegação e hook WM_HOTKEY (hotkeys no startup via MainViewModel).
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
        DarkenChrome(hWnd);
        Vm.AttachHwnd(hWnd);
        TryHook("loaded");
    }

    /// <summary>Borda e cantos escuros do DWM (sem isso o Windows desenha filete claro).</summary>
    private static void DarkenChrome(nint hWnd)
    {
        try
        {
            int dark = 1, border = 0x00151515, caption = 0x00070707;
            DwmSetWindowAttribute(hWnd, 20, ref dark, 4);    // DWMWA_USE_IMMERSIVE_DARK_MODE
            DwmSetWindowAttribute(hWnd, 92, ref border, 4);  // DWMWA_BORDER_COLOR
            DwmSetWindowAttribute(hWnd, 35, ref caption, 4); // DWMWA_CAPTION_COLOR
        }
        catch { }
    }

    [System.Runtime.InteropServices.DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(nint hWnd, int attr, ref int value, int size);

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
        const int WmHotkey = 0x0312;
        const int WmNcHitTest = 0x0084;
        const int WmGetMinMaxInfo = 0x0024;

        if (msg == WmHotkey)
        {
            handled = Vm.HandleHotkey(wParam.ToInt32());
            return IntPtr.Zero;
        }

        // Cromo próprio: bordas redimensionáveis + maximizado respeitando a taskbar.
        if (msg == WmNcHitTest && WindowState == WindowState.Normal)
        {
            int x = (short)(lParam.ToInt32() & 0xFFFF);
            int y = (short)((lParam.ToInt32() >> 16) & 0xFFFF);
            var p = PointFromScreen(new Point(x, y));
            const int edge = 6;
            bool left = p.X <= edge, right = p.X >= ActualWidth - edge;
            bool top = p.Y <= edge, bottom = p.Y >= ActualHeight - edge;
            int ht = (top, bottom, left, right) switch
            {
                (true, _, true, _) => 13,   // HTTOPLEFT
                (true, _, _, true) => 14,   // HTTOPRIGHT
                (_, true, true, _) => 16,   // HTBOTTOMLEFT
                (_, true, _, true) => 17,   // HTBOTTOMRIGHT
                (true, _, _, _) => 12,      // HTTOP
                (_, true, _, _) => 15,      // HTBOTTOM
                (_, _, true, _) => 10,      // HTLEFT
                (_, _, _, true) => 11,      // HTRIGHT
                _ => 0,
            };
            if (ht != 0)
            {
                handled = true;
                return (IntPtr)ht;
            }
        }

        if (msg == WmGetMinMaxInfo)
        {
            ConstrainMaximized(lParam);
            handled = true;
            return IntPtr.Zero;
        }

        return IntPtr.Zero;
    }

    private void ConstrainMaximized(IntPtr lParam)
    {
        try
        {
            var mmi = System.Runtime.InteropServices.Marshal.PtrToStructure<MinMaxInfo>(lParam);
            var monitor = System.Windows.Forms.Screen.FromHandle(new WindowInteropHelper(this).Handle);
            var work = monitor.WorkingArea;
            var screen = monitor.Bounds;
            mmi.ptMaxPosition.X = work.X - screen.X;
            mmi.ptMaxPosition.Y = work.Y - screen.Y;
            mmi.ptMaxSize.X = work.Width;
            mmi.ptMaxSize.Y = work.Height;
            System.Runtime.InteropServices.Marshal.StructureToPtr(mmi, lParam, true);
        }
        catch { }
    }

    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    private struct MinMaxPoint { public int X; public int Y; }

    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    private struct MinMaxInfo
    {
        public MinMaxPoint ptReserved;
        public MinMaxPoint ptMaxSize;
        public MinMaxPoint ptMaxPosition;
        public MinMaxPoint ptMinTrackSize;
        public MinMaxPoint ptMaxTrackSize;
    }

    // ---- Cromo: arrastar + botões da TopBar ----

    private void TopBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left && e.ClickCount == 2)
            ToggleMaximize();
        else if (e.ChangedButton == MouseButton.Left)
            DragMove();
    }

    private void Min_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

    private void Max_Click(object sender, RoutedEventArgs e) => ToggleMaximize();

    private void Close_Click(object sender, RoutedEventArgs e) => Close();

    private void ToggleMaximize()
    {
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        IcoMax.Visibility = WindowState == WindowState.Maximized ? Visibility.Collapsed : Visibility.Visible;
        IcoRestore.Visibility = WindowState == WindowState.Maximized ? Visibility.Visible : Visibility.Collapsed;
    }
}
