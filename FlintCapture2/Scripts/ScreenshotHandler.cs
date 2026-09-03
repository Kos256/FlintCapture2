using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace FlintCapture2.Scripts
{
    public class ScreenshotHandler
    {
        #region win32 imports — low-level keyboard hook (WH_KEYBOARD_LL)

        private delegate IntPtr LowLevelKeyboardProc(
            int nCode,
            IntPtr wParam,
            IntPtr lParam);

        [StructLayout(LayoutKind.Sequential)]
        private struct KBDLLHOOKSTRUCT
        {
            public uint vkCode;
            public uint scanCode;
            public uint flags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(
            int idHook,
            LowLevelKeyboardProc lpfn,
            IntPtr hMod,
            uint dwThreadId);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr CallNextHookEx(
            IntPtr hhk,
            int nCode,
            IntPtr wParam,
            IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool UnhookWindowsHookEx(
            IntPtr hhk);

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetModuleHandle(
            string lpModuleName);

        [DllImport("gdi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool DeleteObject(IntPtr hObject);

        private const int WH_KEYBOARD_LL = 13;

        private const int WM_KEYDOWN = 0x0100;
        private const int WM_KEYUP = 0x0101;
        private const int WM_SYSKEYDOWN = 0x0104;
        private const int WM_SYSKEYUP = 0x0105;

        private const uint VK_SNAPSHOT = 0x2C;
        private const uint VK_LWIN = 0x5B;
        private const uint VK_RWIN = 0x5C;
        private const uint VK_SHIFT = 0x10;
        private const uint VK_LSHIFT = 0xA0;
        private const uint VK_RSHIFT = 0xA1;
        private const uint VK_S = 0x53;

        private LowLevelKeyboardProc _hookProc;
        private IntPtr _hookHandle = IntPtr.Zero;

        private bool _winDown = false;
        private bool _shiftDown = false;
        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                var kb = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);
                uint vk = kb.vkCode;
                int msg = wParam.ToInt32();

                bool down = msg == WM_KEYDOWN || msg == WM_SYSKEYDOWN;
                bool up = msg == WM_KEYUP || msg == WM_SYSKEYUP;

                // Track PrtSc
                if (vk == VK_SNAPSHOT)
                {
                    if (down)
                        mainWin.Dispatcher.BeginInvoke(new Action(SelfCaptureOnHotkey));

                    // Non-zero return to swallow the key entirely
                    return (IntPtr)1;
                }

                // Track WIN + SHIFT + S
                if (vk == VK_LWIN || vk == VK_RWIN)
                {
                    if (down) _winDown = true;
                    else if (up) _winDown = false;
                }
                if (vk == VK_SHIFT || vk == VK_LSHIFT || vk == VK_RSHIFT)
                {
                    if (down) _shiftDown = true;
                    else if (up) _shiftDown = false;
                }

                if (vk == VK_S && down)
                {
                    mainWin.Dispatcher.BeginInvoke(new Action(SelfCaptureOnHotkey));
                    return (IntPtr)1;
                }
            }
            return CallNextHookEx(_hookHandle, nCode, wParam, lParam);
        }

        #endregion

        public string ScreenshotDirectory = "";
        private string rawScreenshotDir = "";
        private MainWindow mainWin;
        public HotkeyWindowAssist hotkeySink;

        public enum HandlerType
        {
            Unknown = 0,
            WindowsClipboard = 1,
            SelfCapture = 2,
            Self_DXGI = 3,
            Self_BitBltGDI = 4,
        }
        public HandlerType SelectedHandlerType = 0;

        public ScreenshotHandler(string appdataDirectory, HandlerType handlerType, MainWindow mainWin)
        {
            this.mainWin = mainWin;

            this.SelectedHandlerType = handlerType;
            ScreenshotDirectory = Path.Combine(appdataDirectory, "Screenshots");
            rawScreenshotDir = Path.Combine(ScreenshotDirectory, "Raw");
            HelperMethods.CreateFolderIfNonexistent(rawScreenshotDir);
            string savedEditsDir = Path.Combine(ScreenshotDirectory, "Saved Edits");
            HelperMethods.CreateFolderIfNonexistent(savedEditsDir);

            switch (SelectedHandlerType)
            {
                case HandlerType.WindowsClipboard:
                    CompositionTarget.Rendering += mainWin.OnFramePrtSc; // this one is the yucky legacy one so uhh do not assign this, it's replaced with the keyboard hook below
                    break;

                case HandlerType.SelfCapture:

                    // RegisterTriggerKey() is already triggered in MainWindow if HandlerType is SelfCapture
                    break;
            }
        }

        public bool HotkeyRegistered = false;
        public void RegisterTriggerKey()
        {
            if (HotkeyRegistered) return;

            // Reset modifier tracking on reinstall so a stray Win/Shift event missed 
            // before the hook existed can't leave us thinking a modifier is stuck down or something.
            _winDown = false;
            _shiftDown = false;
            _hookProc = HookCallback;

            using (var process = Process.GetCurrentProcess())
            using (var module = process.MainModule)
            {
                _hookHandle = SetWindowsHookEx(
                    WH_KEYBOARD_LL,
                    _hookProc,
                    GetModuleHandle(module.ModuleName),
                    0);
            }

            if (_hookHandle == IntPtr.Zero)
            {
                int error = Marshal.GetLastWin32Error();
                _hookProc = null;
                throw new Exception($"Failed to install low-level keyboard hook for PrtSc. Win32 Error: {error}");
            }
            HotkeyRegistered = true;
        }
        public void UnregisterTriggerHotkey()
        {
            if (!HotkeyRegistered) return;

            if (_hookHandle != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_hookHandle);
                _hookHandle = IntPtr.Zero;
            }
            _hookProc = null;

            HotkeyRegistered = false;
        }

        private void SelfCaptureOnHotkey()
        {
            _ = HandlePrtScAsync();
        }

        public List<NotificationWindow> notificationWindowQueue = new();
       
        public async Task HandlePrtScAsync()
        {
            try
            {
                var bounds = System.Windows.Forms.SystemInformation.VirtualScreen;

                using var bitmap = new Bitmap(bounds.Width, bounds.Height);

                using (var g = Graphics.FromImage(bitmap))
                {
                    g.CopyFromScreen(bounds.X, bounds.Y, 0, 0, bounds.Size);
                }

                IntPtr hBitmap = bitmap.GetHbitmap();

                try
                {
                    BitmapSource systemCopiedImage =
                        Imaging.CreateBitmapSourceFromHBitmap(
                            hBitmap,
                            IntPtr.Zero,
                            Int32Rect.Empty,
                            BitmapSizeOptions.FromEmptyOptions());

                    systemCopiedImage.Freeze(); // safer if used across threads

                    string timestamp = DateTime.Now.ToString("ddMMyyyy_HHmmss_ffff");
                    string ssImagePath = Path.Combine(ScreenshotDirectory, "Raw", $"copied_image_{timestamp}.png");

                    using (var fileStream = new FileStream(ssImagePath, FileMode.Create))
                    {
                        var encoder = new PngBitmapEncoder();
                        encoder.Frames.Add(BitmapFrame.Create(systemCopiedImage));
                        encoder.Save(fileStream);
                    }

                    Debug.WriteLine($"Saved to {ssImagePath}");

                    NotificationWindow notifWnd = new(mainWin, this, timestamp, ssImagePath);
                    notificationWindowQueue.Add(notifWnd);
                    notifWnd.StartSequences();
                }
                finally
                {
                    DeleteObject(hBitmap); // absolutely mandatory
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Failed to save image: {ex.Message}",
                    "Error in ScreenshotHandler.cs",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                App.Current.Shutdown();
                return;
            }

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ClipboardWaitMS">Waits a specified number of milliseconds for the clipboard to catch up after the 'print screen' key has been pressed.</param>
        /// <returns></returns>
        public async Task LegacyHandlePrtScAsync(int ClipboardWaitMS = 100)
        {
            await Task.Delay(ClipboardWaitMS); // Wait for clipboard to catch up after PrintScreen

            int iAttempts = 0;
            while (iAttempts < 10)
            {
                if (Clipboard.ContainsImage())
                    break;

                iAttempts++;
                await Task.Delay(50); // Retry every 50ms
            }

            Debug.WriteLine($"Image found in clipboard after {iAttempts} attempt(s)");

            if (!Clipboard.ContainsImage() || iAttempts >= 10)
            {
                string reason = (!Clipboard.ContainsImage())
                    ? "but it isn't an image"
                    : "iAttempts exceeded the fetching count limit";
                MessageBox.Show(
                    $"Tried to fetch last item but {reason}.\nProcess will now close.",
                    "Bad clipboard item...", // show which condition triggered error
                    MessageBoxButton.OK, MessageBoxImage.Error
                );
                App.Current.Shutdown();
                return;
            }

            try
            {
                var systemCopiedImage = Clipboard.GetImage();
                if (systemCopiedImage == null)
                    throw new Exception("Failed to retrieve clipboard image.");

                string timestamp = DateTime.Now.ToString("ddMMyyyy_HHmmss_ffff");
                string ssImagePath = Path.Combine(ScreenshotDirectory, "Raw", $"copied_image_{timestamp}.png");
                using (var fileStream = new FileStream(ssImagePath, FileMode.Create))
                {
                    var encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(systemCopiedImage));
                    encoder.Save(fileStream);
                }

                Debug.WriteLine($"Saved to {ssImagePath}");
                NotificationWindow notifWnd = new(mainWin, this, timestamp, ssImagePath);
                notificationWindowQueue.Add(notifWnd);
                notifWnd.StartSequences();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to save image: {ex.Message}", "Error in ScreenshotHandler.cs",
                    MessageBoxButton.OK, MessageBoxImage.Error);

                App.Current.Shutdown();
                return;
            }

            // wait for key release
            while (GetKeyStateAsBool(KeyStateHelper.VK_SNAPSHOT))
            {
                await Task.Delay(300);
            }
        }

        private bool GetKeyStateAsBool(int VK)
        {
            return ((KeyStateHelper.GetAsyncKeyState(VK) & 0x8000) != 0);
        }
    }
}
