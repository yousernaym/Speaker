using Microsoft.UI.Xaml;
using System;
using System.Runtime.InteropServices;
using Windows.Graphics.Capture;
using Windows.Graphics.DirectX;
using Microsoft.Graphics.Canvas;
using WinRT.Interop;
using Microsoft.UI.Dispatching;
using System.Threading.Tasks;
using SharpDX.Direct3D11;
using Windows.Graphics.DirectX.Direct3D11;

namespace Speaker
{
    class ScreenCapture
    {
        // P/Invoke constants
        private const int MOD_ALT = 0x0001;
        private const int MOD_CONTROL = 0x0002;
        private const int MOD_SHIFT = 0x0004;
        private const int WM_HOTKEY = 0x0312;
        private const int HOTKEY_ID = 9000;

        // P/Invoke imports
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vk);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        [StructLayout(LayoutKind.Sequential)]
        private struct MSG
        {
            public IntPtr hwnd;
            public uint message;
            public IntPtr wParam;
            public IntPtr lParam;
            public uint time;
            public POINT pt;
            public uint lPrivate;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int x;
            public int y;
        }

        [DllImport("user32.dll")]
        private static extern bool GetMessage(out MSG lpMsg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax);

        [DllImport("user32.dll")]
        private static extern bool TranslateMessage(ref MSG lpMsg);

        [DllImport("user32.dll")]
        private static extern IntPtr DispatchMessage(ref MSG lpMsg);

        public static void InitializeHotKey(object target)
        {
            IntPtr hwnd = WindowNative.GetWindowHandle(target);
            RegisterHotKey(hwnd, HOTKEY_ID, MOD_CONTROL | MOD_SHIFT, (int)Windows.System.VirtualKey.S); // Ctrl+Shift+S

            // Start a background task to process messages
            //Task.Run(() => ProcessMessages(hwnd));
        }

        static void ProcessMessages(IntPtr hwnd)
        {
            MSG msg;
            while (GetMessage(out msg, IntPtr.Zero, 0, 0))
            {
                if (msg.message == WM_HOTKEY && (int)msg.wParam == HOTKEY_ID)
                {
                    // Ensuring that the dispatcher queue is available
                    DispatcherQueue.GetForCurrentThread().TryEnqueue(() =>
                    {
                        TakeScreenshot();
                    });
                }
                TranslateMessage(ref msg);
                DispatchMessage(ref msg);
            }
        }

        public static async Task TakeScreenshot()
        {
            var picker = new GraphicsCapturePicker();
            var item = await picker.PickSingleItemAsync();
            if (item != null)
            {
                var canvasDevice = CanvasDevice.GetSharedDevice();
                var dxgiDevice = CanvasDevice.As<SharpDX.DXGI.Device>();
                var d3dDevice = new SharpDX.Direct3D11.Device(dxgiDevice.NativePointer);

                var framePool = Direct3D11CaptureFramePool.Create(
                    canvasDevice,
                    DirectXPixelFormat.B8G8R8A8UIntNormalized,
                    1,
                    item.Size);
                var session = framePool.CreateCaptureSession(item);
                session.StartCapture();

                framePool.FrameArrived += (sender, args) =>
                {
                    using (var frame = sender.TryGetNextFrame())
                    {
                        // Save or process the frame here
                        // Example: Save to file, process image, etc.
                    }
                };
            }
        }
    }
}
