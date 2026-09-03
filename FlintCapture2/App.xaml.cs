using FlintCapture2.Scripts;
using Microsoft.Win32;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using NOTIFYICONDATA = FlintCapture2.Scripts.SystemTrayHandler.NOTIFYICONDATA;

namespace FlintCapture2
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {

        public static bool EnableContextIconMenuBehavior_IDidThisForYouYogurt_THankMeLater = false; // remove this later once ctx menu is finished
        public MainWindow? mainWin;
        public DialogBoxWindow? initDbox;
        public IndicatorWindow? indicatorWin;
        public AppUpdater? appUpdater;
        public ScreenshotHandler.HandlerType SelectedCaptureType;
        public bool WasSnippingToolEnabledBefore = false;
        protected override void OnStartup(StartupEventArgs e)
        {
            try
            {
                //throw new Exception("An exception object was created and thrown in OnStartup(StartupEventArgs e)", new Exception("Startup was blocked by this line of code."));

                SelectedCaptureType = ScreenshotHandler.HandlerType.SelfCapture;
                appUpdater = new();
                indicatorWin = new();

                if (!ExtraUtils.IsAddedToStartMenu()) ExtraUtils.AddToStartMenu();

                if (HelperMethods.PrtScBindedToSnippingTool())
                {
                    WasSnippingToolEnabledBefore = true;
                    initDbox = new(DialogBoxWindow.DialogType.SnippingToolTempDisabledDisclaimer);
                    initDbox.Show();
                }
                else
                {
                    DBoxFlagContinueMainWindow();
                }
            }
            catch (Exception ex)
            {
                
                if (false)
                {
                    string errBody = $"There was an exception:\n\n{ex.Message}";
                    if (ex.InnerException != null) errBody += $"\n\nInner exception states:\n{ex.InnerException.Message}";

                    errBody += "\n\nDo you want to copy this error? (It may help in troubleshooting...or make a report on the GitHub repo lol)";

                    // replace this with a new custom dbox switch case eventually instead of using MessageBox
                    MessageBoxResult msgbox = MessageBox.Show(errBody, "FlintCapture failed to start up...", MessageBoxButton.YesNo, MessageBoxImage.Error);
                    if (msgbox == MessageBoxResult.Yes) Clipboard.SetText(errBody);
                }

                DialogBoxWindow dbox = new(DialogBoxWindow.DialogType.AppFailedToStart)
                {
                    Argument0_Ex_AppFailedToStart = ex
                };
                dbox.Show();
            }

        }
        protected override void OnSessionEnding(SessionEndingCancelEventArgs e)
        {
            base.OnSessionEnding(e);

        }

        public void DBoxFlagContinueMainWindow()
        {
            mainWin = new(SelectedCaptureType);
            mainWin.Show();
            indicatorWin?.ShowIndicator();
            _ = CheckUpdatesAsyncDeferred(); // todo: look into why this makes the mouse stutter. update: making it an awaited task is good practice but its just making a new dbox that lags it
            // ^ possible solution: reserve an updater dbox object and just call .Show() on it when an update is available
        }
        

        public AppUpdater.UpdateInfo? LastFetchedUpdateInfo;
        public async Task CheckUpdates()
        {
            LastFetchedUpdateInfo = await appUpdater!.IsUpdateAvailable();

            bool actualOutput = true;

            if (actualOutput)
            {
                if (LastFetchedUpdateInfo.AvailableUpdate == AppUpdater.UpdateInfo.UpdateStatus.NewerAvailable)
                {
                    initDbox = new(DialogBoxWindow.DialogType.UpdateAvailable);
                    initDbox.Show();
                }
            }
            else
            {
                string result = "";
                result += $"Update available? {LastFetchedUpdateInfo.AvailableUpdate}";
                result += $"\nVersion: {LastFetchedUpdateInfo.Version}";
                if (LastFetchedUpdateInfo.Failed != null) result += $"Failed: {LastFetchedUpdateInfo.Failed}";

                Debug.WriteLine("-- Update stats --\n" + result + "\n------------------");
                MessageBox.Show(result, "Update stats", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        public async Task CheckUpdatesAsyncDeferred()
        {
            // Let layout + first render frame finish
            await Task.Yield();

            await Task.Delay(3000);

            await CheckUpdates();
        }
    }

    public static class PROJCONSTANTS
    {
        public const string AssemblyName = "FlintCapture2";
        public const string PackLocationFormat = $"pack://application:,,,/{AssemblyName};component/";
        public static Version AppVersion =>
            Assembly.GetExecutingAssembly().GetName().Version!;
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
