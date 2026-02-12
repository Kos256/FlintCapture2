using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using PathIO = System.IO.Path;
using Point2D = System.Windows.Point;

namespace FlintCapture2
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private NOTIFYICONDATA _trayIcon;
        private string _tempPath = PathIO.GetTempPath();
        private string _screenshotDir;

        public MainAppWindow _appGuiWindow;
        public List<NotificationWindow> notificationWindowQueue;

        private Stopwatch globalStopwatch;

        /*
        App idea:
        - Runs as a background process
        - Uses copied image from clipboard
        - Relies on AHK to launch this program
        - Sends a notification, waits for press within 3m
        - If no press within 3m it quits auto
        - or maybe i should run it as a background process if it doesn't take up too much CPU?
        - when notif pressed, custom editor window is opened to edit screenshot

        It should have a control panel window too :) (Toggle between wait for 3m of inactivity and background process)

        another idea: it should have a thing in settings where it glows if you have less than 1GB of disk space. 
        - "Running out of storage? You can move the Temp folder to another drive on the system"
        - "Make sure this drive isn't removable! Otherwise FlintCapture could break."
        - "If it is a removable drive, make sure not to plug or unplug that drive after opening or before closing FlintCapture"
        */

        public MainWindow()
        {
            InitializeComponent();

            Closing += AppWantsToClose;

            globalStopwatch = new();
            globalStopwatch.Start();

            _screenshotDir = PathIO.Combine(_tempPath, "FlintCapture Temp");

            notificationWindowQueue = new List<NotificationWindow>();
            _appGuiWindow = new MainAppWindow();

            Show();
            Debug.WriteLine("App is running in background...");
            Hide();

            if (Directory.Exists(_screenshotDir)) Directory.CreateDirectory(_screenshotDir);

            SetupTrayIcon();

            CompositionTarget.Rendering += OnFrame;
        }

        private void OnFrame(object? sender, EventArgs e)
        {
            
        }

        private void SetupTrayIcon()
        {
            _trayIcon = new NOTIFYICONDATA
            {
                cbSize = (uint)Marshal.SizeOf(typeof(NOTIFYICONDATA)),
                hWnd = new System.Windows.Interop.WindowInteropHelper(this).Handle,
                uID = 1,
                uFlags = NativeSystemMethods.NIF_MESSAGE | NativeSystemMethods.NIF_ICON | NativeSystemMethods.NIF_TIP,
                uCallbackMessage = NativeSystemMethods.WM_TRAYICON,
                szTip = "FlintCapture Screenshotter"
            };

            // Load icon from Resource (pack:// URI)
            using (var iconStream = Application.GetResourceStream(new Uri($"{PROJCONSTANTS.PackLocationFormat}assets/icons/appNew.ico")).Stream)
            {
                _trayIcon.hIcon = new System.Drawing.Icon(iconStream).Handle;
            }
            _trayIcon.uFlags = NativeSystemMethods.NIF_MESSAGE | NativeSystemMethods.NIF_ICON | NativeSystemMethods.NIF_TIP;
            _trayIcon.uCallbackMessage = NativeSystemMethods.WM_TRAYICON; // Custom-defined message

            NativeSystemMethods.Shell_NotifyIcon(NativeSystemMethods.NIM_ADD, ref _trayIcon);
        }
        private void AppWantsToClose(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == NativeSystemMethods.WM_TRAYICON)
            {
                switch ((uint)lParam)
                {

                    //case NativeMethods.WM_LBUTTONDOWN:
                    //    MessageBox.Show("Left click detected!"); // Example action
                    //    handled = true;
                    //    break;


                    case NativeSystemMethods.WM_RBUTTONDOWN:
                        //ShowContextMenu(); // leftoff: SHOW CONTEXT MENU
                        handled = true;
                        break;

                    //case NativeMethods.WM_LBUTTONDBLCLK:
                    case NativeSystemMethods.WM_LBUTTONDOWN:
                        try
                        {
                            _appGuiWindow.Visibility = Visibility.Visible;
                        }
                        catch (Exception ex)
                        {
                            if (ex.Message.Contains("has closed")) _appGuiWindow = new MainAppWindow();
                        }
                        _appGuiWindow.RequestShowWindow();
                        handled = true;
                        break;
                }
            }

            return IntPtr.Zero;
        }
        SystemTrayContextMenuWindow? ctxMenuWindow = null;
        private void ShowContextMenu()
        {
            Point2D mpos = MouseCoordinatesHelper.GetMousePosition();
            if (ctxMenuWindow == null) ctxMenuWindow = new SystemTrayContextMenuWindow(this);
            try
            {
                ctxMenuWindow.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("has closed")) ctxMenuWindow = new SystemTrayContextMenuWindow(this);
            }

            // todo: replace 144 with a scaleable value, later on
            //double systemDPI = MouseCoordinatesHelper.GetDpiSetting() / MouseCoordinatesHelper.GetDpiSetting() * 1.5;
            double systemDPI = MouseCoordinatesHelper.GetDpiSetting() / 144;

            ctxMenuWindow.Left = mpos.X * systemDPI - ctxMenuWindow.Width;
            ctxMenuWindow.Top = mpos.Y * systemDPI - ctxMenuWindow.Height;
            ctxMenuWindow.Show();
        }
        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            var hwndSource = PresentationSource.FromVisual(this) as HwndSource;
            hwndSource?.AddHook(WndProc); // Hook into WndProc to capture tray events
        }
        // leftoff at prtsc handler function
        private bool GetKeyStateAsBool(int VK)
        {
            return ((KeyStateHelper.GetAsyncKeyState(VK) & 0x8000) != 0);
        }
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

        public static Point2D GetScreenMouseCoordinates()
        {
            int dpiX = GetDeviceCaps(IntPtr.Zero, 88);
            int dpiY = GetDeviceCaps(IntPtr.Zero, 89);

            Point2D mousePosition = Mouse.GetPosition(null); // get the current mouse position
            return new Point2D((int)(mousePosition.X * (dpiX / 96.0)), (int)(mousePosition.Y * (dpiY / 96.0)));
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

        public static Point2D GetMousePosition()
        {
            GetCursorPos(out POINT p);
            return new Point2D(p.X, p.Y);
        }
        public static Point2D GetMousePositionDPIAware(Window window)
        {
            GetCursorPos(out POINT point);

            var source = PresentationSource.FromVisual(window);
            if (source?.CompositionTarget != null)
            {
                double dpiX = source.CompositionTarget.TransformFromDevice.M11;
                double dpiY = source.CompositionTarget.TransformFromDevice.M22;

                return new Point2D((int)(point.X / dpiX), (int)(point.Y / dpiY));
            }

            return new Point2D(point.X, point.Y);
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