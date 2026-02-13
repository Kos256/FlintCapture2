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
        public SystemTrayHandler(MainWindow mainWin, SystemTrayContextMenuWindow ctxMenuWindow)
        {
            this.mainWin = mainWin;
            this.ctxMenuWindow = ctxMenuWindow;
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
                szTip = "FlintCapture Screenshotter"
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
            MessageBox.Show("Context menu function is empty.", "Not implemented yet!", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        public void ShowContextMenuLegacy()
        {
            Point mpos = MouseCoordinatesHelper.GetMousePosition();
            if (ctxMenuWindow == null) ctxMenuWindow = new SystemTrayContextMenuWindow(mainWin);
            try
            {
                ctxMenuWindow.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("has closed")) ctxMenuWindow = new SystemTrayContextMenuWindow(mainWin);
            }

            // todo: replace 144 with a scaleable value, later on
            //double systemDPI = MouseCoordinatesHelper.GetDpiSetting() / MouseCoordinatesHelper.GetDpiSetting() * 1.5;
            double systemDPI = MouseCoordinatesHelper.GetDpiSetting() / 144;

            ctxMenuWindow.Left = mpos.X * systemDPI - ctxMenuWindow.Width;
            ctxMenuWindow.Top = mpos.Y * systemDPI - ctxMenuWindow.Height;
            ctxMenuWindow.Show();
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
