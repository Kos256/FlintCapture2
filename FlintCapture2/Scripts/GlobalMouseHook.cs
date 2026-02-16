using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

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
}
