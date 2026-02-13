using FlintCapture2.Scripts;
using NOTIFYICONDATA = FlintCapture2.Scripts.SystemTrayHandler.NOTIFYICONDATA;
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
using Point = System.Windows.Point;

namespace FlintCapture2
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        //private string _tempPath = PathIO.GetTempPath(); // check if any other stuff from FlintCapture1 relies on this before removing

        // windows:
        public MainAppWindow _appGuiWindow;
        public SystemTrayContextMenuWindow ctxMenuWindow;
        public List<NotificationWindow> notificationWindowQueue;

        // scripts:
        public SystemTrayHandler SystemTray;
        /// <summary>
        /// Screenshot handler script
        /// </summary>
        public ScreenshotHandler SSHandler;

        private List<CancellationTokenSource> cancelTokenSources;
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

            cancelTokenSources = new List<CancellationTokenSource>()
            {
                new() // OnFrame PrtSc listener loop
            };

            Closing += AppWantsToClose;

            Loaded += (s, e) =>
            {
                Show();
                Debug.WriteLine("App is running in background...");
                Hide();
            };

            globalStopwatch = new();
            globalStopwatch.Start();

            notificationWindowQueue = new List<NotificationWindow>();
            _appGuiWindow = new MainAppWindow();

            ctxMenuWindow = new(this);
            SystemTray = new(this, ctxMenuWindow);
            SystemTray.SetupTrayIcon();

            SSHandler = new(
                PathIO.Combine(PathIO.GetTempPath(), "FlintCapture Temp"), // feed screenshot directory
                this
            );

            CompositionTarget.Rendering += OnFrame;
        }

        public bool isHandlingPrintScreen = false;
        private async void OnFrame(object? sender, EventArgs e)
        {
            if (isHandlingPrintScreen) return; // prevent multiple triggers
            if (cancelTokenSources[0].IsCancellationRequested)
            {
                CompositionTarget.Rendering -= OnFrame;
                return;
            }

            if (GetKeyStateAsBool(KeyStateHelper.VK_SNAPSHOT))
            {
                isHandlingPrintScreen = true;
                try
                {
                    await SSHandler.HandlePrtScAsync();
                }
                finally
                {
                    isHandlingPrintScreen = false;
                }
            }
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
                    case NativeSystemMethods.WM_RBUTTONDOWN:
                        SystemTray.ShowContextMenu(); // leftoff: SHOW CONTEXT MENU
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

    
}