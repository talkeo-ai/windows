namespace Talkeo.Windows.Hooks;

using System.Runtime.InteropServices;

internal sealed class MouseHook : IDisposable
{
    private const int WH_MOUSE_LL    = 14;
    private const int WM_LBUTTONDOWN = 0x0201;
    private const int WM_LBUTTONUP   = 0x0202;

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT { public int x, y; }

    [StructLayout(LayoutKind.Sequential)]
    private struct MSLLHOOKSTRUCT
    {
        public POINT pt; public uint mouseData, flags, time; public IntPtr dwExtraInfo;
    }

    private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint threadId);
    [DllImport("user32.dll")] private static extern bool UnhookWindowsHookEx(IntPtr hhk);
    [DllImport("user32.dll")] private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)] private static extern IntPtr GetModuleHandle(string? name);
    [DllImport("user32.dll")] private static extern uint GetDoubleClickTime();

    private IntPtr             _handle;
    private LowLevelMouseProc? _proc;       // must be a field — prevents GC from collecting the delegate
    private System.Drawing.Point _downPoint;
    private System.Drawing.Point _lastUpPoint;
    private DateTime           _lastUpTime = DateTime.MinValue;

    private readonly Action<System.Drawing.Point>  _onGesture;
    private readonly Action<System.Drawing.Point>? _onAnyDown;

    public MouseHook(Action<System.Drawing.Point> onGesture,
                     Action<System.Drawing.Point>? onAnyDown = null)
    {
        _onGesture = onGesture;
        _onAnyDown = onAnyDown;
    }

    public void Install()
    {
        if (_handle != IntPtr.Zero) return;
        _proc   = HookCallback;
        var mod = System.Diagnostics.Process.GetCurrentProcess().MainModule!;
        _handle = SetWindowsHookEx(WH_MOUSE_LL, _proc, GetModuleHandle(mod.ModuleName), 0);
        if (_handle == IntPtr.Zero)
            throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
    }

    public void Uninstall()
    {
        if (_handle == IntPtr.Zero) return;
        UnhookWindowsHookEx(_handle);
        _handle = IntPtr.Zero;
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            var data = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);
            var msg  = (int)wParam;
            var pt   = new System.Drawing.Point(data.pt.x, data.pt.y);

            if (msg == WM_LBUTTONDOWN)
            {
                _downPoint = pt;
                _onAnyDown?.Invoke(pt);
            }
            else if (msg == WM_LBUTTONUP)
            {
                bool isDrag     = Distance(_downPoint, pt) > 5;
                bool isDblClick = (DateTime.UtcNow - _lastUpTime).TotalMilliseconds < GetDoubleClickTime()
                                  && Distance(_lastUpPoint, pt) < 8;
                _lastUpPoint = pt;
                _lastUpTime  = DateTime.UtcNow;

                if (isDrag || isDblClick)
                    _onGesture(pt);
            }
        }
        return CallNextHookEx(_handle, nCode, wParam, lParam); // siempre llamar esto
    }

    private static double Distance(System.Drawing.Point a, System.Drawing.Point b)
    {
        double dx = a.X - b.X, dy = a.Y - b.Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    public void Dispose() => Uninstall();
}
