using System.Configuration;
using System.Data;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using NOTIFYICONDATA = FlintCapture2.Scripts.SystemTrayHandler.NOTIFYICONDATA;

namespace FlintCapture2
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public MainWindow? mainWin;
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

    public class KeyStateHelper
    {
        public const int VK_SNAPSHOT = 0x2C;

        [DllImport("user32.dll")]
        public static extern short GetAsyncKeyState(int vKey);
    }

    public class MouseCoordinatesHelper
    {
        [DllImport("gdi32.dll")]
        static extern int GetDeviceCaps(IntPtr hdc, int nIndex);

        public static Point GetScreenMouseCoordinates()
        {
            int dpiX = GetDeviceCaps(IntPtr.Zero, 88);
            int dpiY = GetDeviceCaps(IntPtr.Zero, 89);

            Point mousePosition = Mouse.GetPosition(null); // get the current mouse position
            return new Point((int)(mousePosition.X * (dpiX / 96.0)), (int)(mousePosition.Y * (dpiY / 96.0)));
        }

        [DllImport("user32.dll")]
        public static extern short GetAsyncKeyState(int vKey);
        const int VK_SNAPSHOT = 0x2C;

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetCursorPos(out POINT lpPoint);

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int X;
            public int Y;
        }

        public static Point GetMousePosition()
        {
            GetCursorPos(out POINT p);
            return new Point(p.X, p.Y);
        }
        public static Point GetMousePositionDPIAware(Window window)
        {
            GetCursorPos(out POINT point);

            var source = PresentationSource.FromVisual(window);
            if (source?.CompositionTarget != null)
            {
                double dpiX = source.CompositionTarget.TransformFromDevice.M11;
                double dpiY = source.CompositionTarget.TransformFromDevice.M22;

                return new Point((int)(point.X / dpiX), (int)(point.Y / dpiY));
            }

            return new Point(point.X, point.Y);
        }

        public static double GetDpiSetting()
        {
            IntPtr deviceContext = Marshal.AllocHGlobal(4);
            try
            {
                int dpiX = GetDeviceCaps(deviceContext, 88); // DPI for X-axis (96)
                int dpiY = GetDeviceCaps(deviceContext, 90); // DPI for Y-axis (96)

                if (dpiX == 0 || dpiY == 0)
                    return 96.0; // default to 96 DPI

                return Math.Round((double)Math.Max(dpiX, dpiY), 2);
            }
            finally
            {
                Marshal.FreeHGlobal(deviceContext);
            }
        }
    }
}
