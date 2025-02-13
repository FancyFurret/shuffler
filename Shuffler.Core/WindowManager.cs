using System.Runtime.InteropServices;

namespace Shuffler.Core;

public static class WindowManager
{
    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool BringWindowToTop(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

    private const int SW_HIDE = 0;
    private const int SW_SHOW = 5;
    private const int SW_MINIMIZE = 6;
    private const int SW_RESTORE = 9;
    private const int SW_SHOWDEFAULT = 10;

    private const uint WM_CLOSE = 0x0010;

    public static void BringToFront(IntPtr hWnd)
    {
        SetForegroundWindow(hWnd);
        BringWindowToTop(hWnd);
    }

    public static void Minimize(IntPtr hWnd)
    {
        ShowWindow(hWnd, SW_MINIMIZE);
    }

    public static void Show(IntPtr hWnd)
    {
        ShowWindow(hWnd, SW_SHOW);
    }

    public static void Restore(IntPtr hWnd)
    {
        ShowWindow(hWnd, SW_RESTORE);
    }

    public static void Close(IntPtr hWnd)
    {
        SendMessage(hWnd, WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
    }
}