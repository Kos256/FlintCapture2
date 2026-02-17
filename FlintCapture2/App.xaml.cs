using Microsoft.Win32;
using System.Configuration;
using System.Data;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Data;
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
        public DialogBoxWindow? initDbox;
        protected override void OnStartup(StartupEventArgs e)
        {
            if (HelperMethods.PrtScBindedToSnippingTool())
            {
                initDbox = new(DialogBoxWindow.DialogType.SnippingToolTempDisabledDisclaimer);
                initDbox.Show();
            }
            else
            {
                DBoxFlagContinueMainWindow();
            }
        }
        protected override void OnSessionEnding(SessionEndingCancelEventArgs e)
        {
            base.OnSessionEnding(e);

            if (mainWin != null)
            {
                mainWin.GMouseHook.Dispose();
            }
        }

        public void DBoxFlagContinueMainWindow()
        {
            mainWin = new();
            mainWin.Show();
        }
    }
    public static class PROJCONSTANTS
    {
        public const string AssemblyName = "FlintCapture2";
        public const string PackLocationFormat = $"pack://application:,,,/{AssemblyName};component/";
        public const string AppVersion = "2.0.1";
    }


    public class HelperMethods
    {
        public static bool PrtScBindedToSnippingTool(bool? enabled = null)
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Control Panel\Keyboard", writable: true);

            if (enabled == null)
            {
                object value = key?.GetValue("PrintScreenKeyForSnippingEnabled");
                if (value == null) return false; // probably windows 10, since that feature does not exist on windows 10 so no need to check for it
                return value is int intValue && intValue == 1;
            }
            else
            {
                key?.SetValue(
                    "PrintScreenKeyForSnippingEnabled",
                    enabled.Value ? 1 : 0,
                    RegistryValueKind.DWord
                );
                return enabled.Value;
            }
        }
        public static void CreateFolderIfNonexistent(string path)
        {
            try
            {
                if (!Directory.Exists(path)) Directory.CreateDirectory(path);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "Failed to create directory :(",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }
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
