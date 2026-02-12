using System.Configuration;
using System.Data;
using System.Runtime.InteropServices;
using System.Windows;

namespace FlintCapture2
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public MainWindow mainWin;
        protected override void OnStartup(StartupEventArgs e)
        {
            mainWin = new();
            mainWin.Show();
        }
    }
    public static class PROJCONSTANTS
    {
        public const string AssemblyName = "FlintCapture2";
        public const string PackLocationFormat = $"pack://application:,,,/{AssemblyName};component/";
        public const string AppVersion = "2.0.0";
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

    public static class NativeSystemMethods
    {
        public const uint WM_TRAYICON = 0x0400 + 1; // Custom message ID for tray events
        public const uint NIF_MESSAGE = 0x01;
        public const uint NIF_ICON = 0x02;
        public const uint NIF_TIP = 0x04;

        public const uint WM_LBUTTONDOWN = 0x0201;
        public const uint WM_RBUTTONDOWN = 0x0204;
        public const uint WM_LBUTTONDBLCLK = 0x0203;
        public const uint WM_USER = 0x0400;
        //public const uint WM_TRAYICON = WM_USER + 1;

        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        public static extern bool Shell_NotifyIcon(uint dwMessage, ref NOTIFYICONDATA lpData);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public extern static bool DestroyIcon(IntPtr handle);

        public const uint NIM_ADD = 0x00000000;
        public const uint NIM_MODIFY = 0x00000001;
        public const uint NIM_DELETE = 0x00000002;
    }
}
