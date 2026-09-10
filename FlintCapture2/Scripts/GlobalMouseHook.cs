using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Input;

namespace FlintCapture2.Scripts
{

    public class GlobalMouseHook : IDisposable
    {
        private IntPtr _hookId = IntPtr.Zero;
        private HookProc? _hookCallback;

        public event EventHandler<MouseHookEventArgs>? LeftMouseDown;
        public event EventHandler<MouseHookEventArgs>? LeftMouseUp;
        public event EventHandler<MouseHookEventArgs>? MiddleMouseDown;
        public event EventHandler<MouseHookEventArgs>? MiddleMouseUp;
        public event EventHandler<MouseHookEventArgs>? RightMouseDown;
        public event EventHandler<MouseHookEventArgs>? RightMouseUp;

        public bool IsLeftPressed { get; private set; }
        public bool IsMiddlePressed { get; private set; }
        public bool IsRightPressed { get; private set; }

        public GlobalMouseHook()
        {
            _hookCallback = HookCallback;
            _hookId = SetHook(_hookCallback);
        }

        private IntPtr SetHook(HookProc proc)
        {
            using var curProcess = Process.GetCurrentProcess();
            using var curModule = curProcess.MainModule!;

            return SetWindowsHookEx(
                WH_MOUSE_LL,
                proc,
                GetModuleHandle(curModule.ModuleName),
                0);
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                int msg = wParam.ToInt32();
                var hookStruct = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);

                var args = new MouseHookEventArgs
                {
                    X = hookStruct.pt.x,
                    Y = hookStruct.pt.y
                };

                switch (msg)
                {
                    case WM_LBUTTONDOWN:
                        IsLeftPressed = true;
                        LeftMouseDown?.Invoke(this, args);
                        break;

                    case WM_LBUTTONUP:
                        IsLeftPressed = false;
                        LeftMouseUp?.Invoke(this, args);
                        break;

                    case WM_MBUTTONDOWN:
                        IsMiddlePressed = true;
                        MiddleMouseDown?.Invoke(this, args);
                        break;

                    case WM_MBUTTONUP:
                        IsMiddlePressed = false;
                        MiddleMouseUp?.Invoke(this, args);
                        break;

                    case WM_RBUTTONDOWN:
                        IsRightPressed = true;
                        RightMouseDown?.Invoke(this, args);
                        break;

                    case WM_RBUTTONUP:
                        IsRightPressed = false;
                        RightMouseUp?.Invoke(this, args);
                        break;
                }
            }

            return CallNextHookEx(_hookId, nCode, wParam, lParam);
        }

        public void Dispose()
        {
            if (_hookId != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_hookId);
                _hookId = IntPtr.Zero;
            }

            GC.SuppressFinalize(this);
        }

        ~GlobalMouseHook()
        {
            Dispose();
        }

        #region win32 imports

        private delegate IntPtr HookProc(int nCode, IntPtr wParam, IntPtr lParam);

        private const int WH_MOUSE_LL = 14;

        private const int WM_LBUTTONDOWN = 0x0201;
        private const int WM_LBUTTONUP = 0x0202;
        private const int WM_RBUTTONDOWN = 0x0204;
        private const int WM_RBUTTONUP = 0x0205;
        private const int WM_MBUTTONDOWN = 0x0207;
        private const int WM_MBUTTONUP = 0x0208;

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(
            int idHook,
            HookProc lpfn,
            IntPtr hMod,
            uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(
            IntPtr hhk,
            int nCode,
            IntPtr wParam,
            IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr GetModuleHandle(string? lpModuleName);

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int x;
            public int y;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MSLLHOOKSTRUCT
        {
            public POINT pt;
            public uint mouseData;
            public uint flags;
            public uint time;
            public IntPtr dwExtraInfo;
        }
        #endregion
    }

    public class MouseHookEventArgs : EventArgs
    {
        public int X { get; set; }
        public int Y { get; set; }
    }

    public class KeyStateHelper
    {
        public const int VK_SNAPSHOT = 0x2C;

        [DllImport("user32.dll")]
        public static extern short GetAsyncKeyState(int vKey);
    }

    public class MouseCoordinatesHelper
    {
        #region required imports
        // imports
        [DllImport("gdi32.dll")]
        static extern int GetDeviceCaps(IntPtr hdc, int nIndex);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetCursorPos(out POINT lpPoint);

        [DllImport("Shcore.dll")]
        private static extern int GetDpiForMonitor(
            IntPtr hmonitor,
            MonitorDpiType dpiType,
            out uint dpiX,
            out uint dpiY
        );

        [DllImport("User32.dll")]
        private static extern IntPtr MonitorFromPoint(POINT pt, uint dwFlags);

        // constants
        private const uint MONITOR_DEFAULTTONEAREST = 2;

        // enums and structs
        private enum MonitorDpiType
        {
            MDT_EFFECTIVE_DPI = 0,
            MDT_ANGULAR_DPI = 1,
            MDT_RAW_DPI = 2,
            MDT_DEFAULT = MDT_EFFECTIVE_DPI
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int X;
            public int Y;
        }
        #endregion

        public static Point GetScreenMouseCoordinates()
        {
            int dpiX = GetDeviceCaps(IntPtr.Zero, 88);
            int dpiY = GetDeviceCaps(IntPtr.Zero, 89);

            Point mousePosition = Mouse.GetPosition(null); // get the current mouse position
            return new Point((int)(mousePosition.X * (dpiX / 96.0)), (int)(mousePosition.Y * (dpiY / 96.0)));
        }
        public static Point GetMousePos()
        {
            GetCursorPos(out POINT p);
            return new Point(p.X, p.Y);
        }
        public static Point GetScaledMousePosition()
        {
            GetCursorPos(out POINT p);

            IntPtr monitor = MonitorFromPoint(p, MONITOR_DEFAULTTONEAREST);

            GetDpiForMonitor(monitor, MonitorDpiType.MDT_EFFECTIVE_DPI,
                out uint dpiX, out uint dpiY);

            double scaleX = dpiX / 96;
            double scaleY = dpiY / 96;

            return new Point(p.X / scaleX, p.Y / scaleY);
        }
        public static Point GetScaledMousePosition(Window hwnd)
        {
            Point mposRaw = GetMousePos();
            var source = PresentationSource.FromVisual(hwnd);
            var transform = source.CompositionTarget.TransformFromDevice;
            Point scaledPos = transform.Transform(mposRaw);

            //Debug.WriteLine($"x:{mpos.X}, y:{mpos.Y}");
            return scaledPos;
        }
    }
}
