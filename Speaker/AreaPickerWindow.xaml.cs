using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System;
using System.Runtime.InteropServices;
using Windows.Foundation;
using Windows.Graphics;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Speaker
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class AreaPickerWindow : Window
    {
        public event Action<RectInt32> AreaSelected;

        Point _startDip;   // first corner in DIPs
        IntPtr _hwnd;
        double _dipScale;  // pixels per DIP for primary monitor
        //const int SW_SHOWNOACTIVATE = 4;

        public AreaPickerWindow()
        {
            InitializeComponent();

            _hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            var id = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(_hwnd);
            var appWindow = AppWindow.GetFromWindowId(id);
            var presenter = FullScreenPresenter.Create();
            appWindow.SetPresenter(presenter);
            appWindow.IsShownInSwitchers = false;


            appWindow.IsShownInSwitchers = false;     // hide from Alt‑Tab

            // Enable layered style so we can control opacity
            int ex = Native.GetWindowLong(_hwnd, Native.GWL_EXSTYLE);
            Native.SetWindowLong(_hwnd, Native.GWL_EXSTYLE, ex | Native.WS_EX_LAYERED);
            Native.SetLayeredWindowAttributes(_hwnd, 0, 128, Native.LWA_ALPHA);

            // DPI factor once for DIP→pixel conversion
            var dpi = PInvoke.User32.GetDpiForWindow(_hwnd);
            _dipScale = dpi / 96.0;
        }

        /* ---------- pointer handling ---------- */

        void OnPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            _startDip = e.GetCurrentPoint(RootCanvas).Position;
            RootCanvas.CapturePointer(e.Pointer);

            // prepare rectangle
            Canvas.SetLeft(SelectionRect, _startDip.X);
            Canvas.SetTop(SelectionRect, _startDip.Y);
            SelectionRect.Width = SelectionRect.Height = 0;
            SelectionRect.Visibility = Visibility.Visible;
        }

        void OnPointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (RootCanvas.PointerCaptures is null) return;

            var pos = e.GetCurrentPoint(RootCanvas).Position;
            double x = Math.Min(pos.X, _startDip.X);
            double y = Math.Min(pos.Y, _startDip.Y);
            double w = Math.Abs(pos.X - _startDip.X);
            double h = Math.Abs(pos.Y - _startDip.Y);

            Canvas.SetLeft(SelectionRect, x);
            Canvas.SetTop(SelectionRect, y);
            SelectionRect.Width = w;
            SelectionRect.Height = h;
        }

        void OnPointerReleased(object sender, PointerRoutedEventArgs e)
        {
            RootCanvas.ReleasePointerCapture(e.Pointer);

            var leftDip = Canvas.GetLeft(SelectionRect);
            var topDip = Canvas.GetTop(SelectionRect);
            var widthDip = SelectionRect.Width;
            var heightDip = SelectionRect.Height;

            // Convert to physical pixels (ints) and raise the event
            var rect = new RectInt32
            {
                X = (int)Math.Round(leftDip * _dipScale),
                Y = (int)Math.Round(topDip * _dipScale),
                Width = (int)Math.Round(widthDip * _dipScale),
                Height = (int)Math.Round(heightDip * _dipScale)
            };

            AreaSelected?.Invoke(rect);
            Close();
        }

        private void Window_Activated(object sender, WindowActivatedEventArgs args)
        {
            Native.SetWindowPos(
                _hwnd,
                Native.HWND_TOPMOST,
                0, 0, 0, 0,
                Native.SWP_NOMOVE | Native.SWP_NOSIZE |
                Native.SWP_NOACTIVATE | Native.SWP_SHOWWINDOW);
        }

        private void Grid_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Escape)
            {
                Close();
                e.Handled = true;
            }            
        }
    }

    internal static class Native
    {
        public const int GWL_EXSTYLE = -20;
        public const int WS_EX_LAYERED = 0x00080000;
        public const int LWA_ALPHA = 0x00000002;
        public static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        public const uint SWP_NOMOVE = 0x0002;
        public const uint SWP_NOSIZE = 0x0001;
        public const uint SWP_NOACTIVATE = 0x0010;
        public const uint SWP_SHOWWINDOW = 0x0040;

        [DllImport("user32.dll", SetLastError = true)]
        public static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetLayeredWindowAttributes(
            IntPtr hwnd,
            uint crKey,   // 0 = no color‑key transparency
            byte bAlpha,  // 0..255
            uint dwFlags  // always LWA_ALPHA for our purpose
        );

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool SetWindowPos(
             IntPtr hWnd,
             IntPtr hWndInsertAfter,
             int X, int Y, int cx, int cy,
             uint uFlags);
    }
}
