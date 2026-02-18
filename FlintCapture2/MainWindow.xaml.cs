using FlintCapture2.Scripts;
using Microsoft.Win32;
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
using NOTIFYICONDATA = FlintCapture2.Scripts.SystemTrayHandler.NOTIFYICONDATA;
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
        private string FlintCaptureDataPath;
        public ScreenshotHandler.HandlerType SelectedCaptureType = ScreenshotHandler.HandlerType.Unknown;

        // windows:
        public MainAppWindow _appGuiWindow;
        public SystemTrayContextMenuWindow? ctxMenuWindow;

        // scripts:
        public SystemTrayHandler? SystemTray;
        /// <summary>
        /// Screenshot handler script
        /// </summary>
        public ScreenshotHandler SSHandler;
        public GlobalMouseHook GMouseHook;

        private List<CancellationTokenSource> cancelTokenSources;
        private Stopwatch globalStopwatch;

        /*
        It should have a control panel window too :) (Toggle between wait for 3m of inactivity and background process)

        another idea: it should have a thing in settings where it glows if you have less than 1GB of disk space. 
        - "Running out of storage? You can move the Temp folder to another drive on the system"
        - "Make sure this drive isn't removable! Otherwise FlintCapture could break."
        - "If it is a removable drive, make sure not to plug or unplug that drive after opening or before closing FlintCapture"
        */

        public MainWindow(ScreenshotHandler.HandlerType SelectedCaptureType)
        {
            InitializeComponent();

            cancelTokenSources = new List<CancellationTokenSource>()
            {
                new() // OnFramePrtSc PrtSc listener loop
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

            this.SelectedCaptureType = SelectedCaptureType;

            _appGuiWindow = new MainAppWindow();

            string appdataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            FlintCaptureDataPath = PathIO.Combine(appdataPath, "FlintCapture");

            SSHandler = new(
                FlintCaptureDataPath, // feed screenshot directory
                SelectedCaptureType,
                this
            );
        }

        public void ShowSavedScreenshotsDirectoryFileExplorer(string? path)
        {
            string filePath = path ?? SSHandler.ScreenshotDirectory;

            Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = $"/select,\"{filePath}\"",
                UseShellExecute = true
            });
        }


        public bool isHandlingPrintScreen = false;
        public async void OnFramePrtSc(object? sender, EventArgs e)
        {
            if (isHandlingPrintScreen) return; // prevent multiple triggers
            if (cancelTokenSources[0].IsCancellationRequested)
            {
                CompositionTarget.Rendering -= OnFramePrtSc;
                return;
            }

            if (GetKeyStateAsBool(KeyStateHelper.VK_SNAPSHOT))
            {
                isHandlingPrintScreen = true;
                try
                {
                    await SSHandler.LegacyHandlePrtScAsync();
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

            GMouseHook = new();
            SystemTray = new(this);
            SystemTray.SetupTrayIcon();
            ctxMenuWindow = SystemTray.ctxMenuWindow;
        }
        private bool GetKeyStateAsBool(int VK)
        {
            return ((KeyStateHelper.GetAsyncKeyState(VK) & 0x8000) != 0);
        }


    }
}