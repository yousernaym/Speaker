using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics;
using Windows.System;

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

        public AreaPickerWindow()
        {
            InitializeComponent();

            // 1. Make the window borderless + full‑screen + always on top
            _hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            var id = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(_hwnd);
            var appWindow = AppWindow.GetFromWindowId(id);

            appWindow.SetPresenter(FullScreenPresenter.Create());
            appWindow.IsShownInSwitchers = false;                  // keeps Alt‑Tab list clean
            appWindow.TitleBar.ExtendsContentIntoTitleBar = true;  // hide caption bar

            //// 2. Handle transparency
            //var attr = PInvoke.User32.GetWindowLong(_hwnd, PInvoke.User32.WindowLongIndexFlags.GWL_EXSTYLE);
            //PInvoke.User32.SetWindowLong(_hwnd,
            //                             PInvoke.User32.WindowLongIndexFlags.GWL_EXSTYLE,
            //                             attr | (int)PInvoke.User32.WindowStylesEx.WS_EX_LAYERED);
            //// 0x01 alpha keeps the overlay click‑opaque but visually 
            //PInvoke.User32.SetLayeredWindowAttributes(_hwnd, 0, 0x01, 0x02);

            // 3. DPI – we only need the scale factor once
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

        void OnEscape(KeyboardAccelerator sender,
                KeyboardAcceleratorInvokedEventArgs args)
        {
            Close();
            args.Handled = true;   // prevent the beep
        }
    }
}
