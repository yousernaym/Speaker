using System;
using System.Runtime.InteropServices;
using Microsoft.UI.Dispatching;
using Windows.Foundation;
using Windows.Graphics;

namespace Speaker;

public static class ScreenCapture
{
    public static void CopyRectToClipboard(RectInt32 rect)
    {
        DispatcherQueue dispatcher = DispatcherQueue.GetForCurrentThread();
        if (!dispatcher.HasThreadAccess)
        {
            dispatcher.TryEnqueue(() => CopyRectToClipboard(rect));
            return;
        }

        IntPtr hScreenDC = GetDC(IntPtr.Zero);
        IntPtr hMemDC = CreateCompatibleDC(hScreenDC);
        IntPtr hBitmap = CreateCompatibleBitmap(hScreenDC, rect.Width, rect.Height);
        IntPtr hPrevBitmap = SelectObject(hMemDC, hBitmap);

        const int SRCCOPY = 0x00CC0020;
        BitBlt(hMemDC, 0, 0, rect.Width, rect.Height,
               hScreenDC, rect.X, rect.Y, SRCCOPY);

        SelectObject(hMemDC, hPrevBitmap);

        OpenClipboard(IntPtr.Zero);
        try
        {
            EmptyClipboard();

            /* --------- FIX: use CF_BITMAP instead of CF_DIB --------- */
            if (SetClipboardData(ClipboardFormat.CF_BITMAP, hBitmap) == IntPtr.Zero)
                throw new System.ComponentModel.Win32Exception(
                    Marshal.GetLastWin32Error(), "SetClipboardData failed");

            hBitmap = IntPtr.Zero;      // clipboard now owns it
        }
        finally
        {
            CloseClipboard();
            if (hBitmap != IntPtr.Zero) DeleteObject(hBitmap);
            DeleteDC(hMemDC);
            ReleaseDC(IntPtr.Zero, hScreenDC);
        }
    }

    /* ===== P/Invoke glue ===== */
    private enum ClipboardFormat : uint { CF_BITMAP = 2 }

    [DllImport("user32.dll")] private static extern IntPtr GetDC(IntPtr hWnd);
    [DllImport("gdi32.dll")] private static extern IntPtr CreateCompatibleDC(IntPtr hDC);
    [DllImport("gdi32.dll")] private static extern IntPtr CreateCompatibleBitmap(IntPtr hDC, int w, int h);
    [DllImport("gdi32.dll")] private static extern IntPtr SelectObject(IntPtr hDC, IntPtr hObject);
    [DllImport("gdi32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool BitBlt(IntPtr hDestDC, int x, int y, int cx, int cy,
                                      IntPtr hSrcDC, int xSrc, int ySrc, int rop);
    [DllImport("gdi32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DeleteObject(IntPtr hObject);
    [DllImport("gdi32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DeleteDC(IntPtr hDC);
    [DllImport("user32.dll")] private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

    [DllImport("user32.dll")] private static extern bool OpenClipboard(IntPtr hWndNewOwner);
    [DllImport("user32.dll")] private static extern bool EmptyClipboard();
    [DllImport("user32.dll")] private static extern IntPtr SetClipboardData(ClipboardFormat fmt, IntPtr hMem);
    [DllImport("user32.dll")] private static extern bool CloseClipboard();
}
