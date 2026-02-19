using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Interop;

namespace FlintCapture2.Scripts
{
    public class HotkeyWindowAssist : IDisposable
    {
        #region win32 imports

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool RegisterHotKey(
            IntPtr hWnd,
            int id,
            uint fsModifiers,
            uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(
            IntPtr hWnd,
            int id);

        #endregion

        private HwndSource _source;
        private const int HOTKEY_ID = 9000;
        private const int WM_HOTKEY = 0x0312;

        public event Action? HotkeyPressed;

        public HotkeyWindowAssist()
        {
            var parameters = new HwndSourceParameters("HotkeySink")
            {
                WindowStyle = 0x800000, // WS_OVERLAPPED
                Width = 0,
                Height = 0,
                ParentWindow = IntPtr.Zero
            };

            _source = new HwndSource(parameters);
            _source.AddHook(WndProc);

            RegisterHotKey(_source.Handle, HOTKEY_ID, 0, 0x2C);
        }

        private IntPtr WndProc(
            IntPtr hwnd,
            int msg,
            IntPtr wParam,
            IntPtr lParam,
            ref bool handled)
        {
            if (msg == WM_HOTKEY && wParam.ToInt32() == HOTKEY_ID)
            {
                handled = true;
                HotkeyPressed?.Invoke();
            }

            return IntPtr.Zero;
        }

        public void Dispose()
        {
            UnregisterHotKey(_source.Handle, HOTKEY_ID);
            _source.Dispose();
        }
    }
}
