using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;

namespace FlintCapture2.Scripts
{
    public class SystemTrayHandler
    {
        public NOTIFYICONDATA TrayIconData;
        public MainWindow mainWin;
        public SystemTrayContextMenuWindow ctxMenuWindow;
        public SystemTrayHandler(MainWindow mainWin)
        {
            this.mainWin = mainWin;
            ctxMenuWindow = new(mainWin);

        }

        public void SetupTrayIcon()
        {
            TrayIconData = new NOTIFYICONDATA
            {
                cbSize = (uint)Marshal.SizeOf(typeof(NOTIFYICONDATA)),
                hWnd = new System.Windows.Interop.WindowInteropHelper(mainWin).Handle,
                uID = 1,
                uFlags = NativeSystemMethods.NIF_MESSAGE | NativeSystemMethods.NIF_ICON | NativeSystemMethods.NIF_TIP,
                uCallbackMessage = NativeSystemMethods.WM_TRAYICON,
                szTip = "FlintCapture"
            };

            // Load icon from Resource (pack:// URI)
            using (var iconStream = Application.GetResourceStream(new Uri($"{PROJCONSTANTS.PackLocationFormat}assets/icons/appNew.ico")).Stream)
            {
                TrayIconData.hIcon = new System.Drawing.Icon(iconStream).Handle;
            }
            TrayIconData.uFlags = NativeSystemMethods.NIF_MESSAGE | NativeSystemMethods.NIF_ICON | NativeSystemMethods.NIF_TIP;
            TrayIconData.uCallbackMessage = NativeSystemMethods.WM_TRAYICON; // Custom-defined message

            NativeSystemMethods.Shell_NotifyIcon(NativeSystemMethods.NIM_ADD, ref TrayIconData);
        }
        public void ShowContextMenu()
        {
            Point mpos = MouseCoordinatesHelper.GetScaledMousePosition();
            try
            {
                ctxMenuWindow.Left = mpos.X;
                ctxMenuWindow.Top = mpos.Y;
                ctxMenuWindow.ShowMenu();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Exception while calling ShowMenu() in context menu window: {ex.Message}", "SystemTrayHandler.ShowContextMenu() failed", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct NOTIFYICONDATA
        {
            public uint cbSize;
            public IntPtr hWnd;
            public uint uID;
            public uint uFlags;
            public uint uCallbackMessage;
            public IntPtr hIcon;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string szTip;
            public uint dwState;
            public uint dwStateMask;
            public string szInfo;
            public uint uTimeoutOrVersion;
            public string szInfoTitle;
            public uint dwInfoFlags;
            public Guid guidItem;
            public IntPtr hBalloonIcon;
        }
    }
}
