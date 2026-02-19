using SharpVectors.Dom.Svg;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using static FlintCapture2.Scripts.ScreenshotHandler;

namespace FlintCapture2.Scripts
{
    public class ScreenshotHandler
    {
        #region win32 imports
        private const int HOTKEY_ID = 9000; // PrtSc
        private const int WM_HOTKEY = 0x0312;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool RegisterHotKey(
            IntPtr hWnd,
            int id,
            uint fsModifiers,
            uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(
            IntPtr hWnd,
            int id);

        private IntPtr HwndHook(
            IntPtr hwnd,
            int msg,
            IntPtr wParam,
            IntPtr lParam,
            ref bool handled
        )
        {
            if (msg == WM_HOTKEY && wParam.ToInt32() == HOTKEY_ID)
            {
                handled = true;
                SelfCaptureOnHotkey();
            }

            return IntPtr.Zero;
        }

        [DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr hObject);

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
                    CompositionTarget.Rendering += mainWin.OnFramePrtSc; // this one is the yucky legacy one so uhh do not assign this, it's replaced with PInvoke RegisterHotkey()
                    break;

                case HandlerType.SelfCapture:

                    // RegisterHotkey() is already triggered in MainWindow if HandlerType is SelfCapture
                    break;
            }
        }

        public bool HotkeyRegistered = false;
        private HwndSource hotkeySource;
        public void InitalizeTriggerHotkey()
        {
            var helper = new WindowInteropHelper(mainWin);
            hotkeySource = HwndSource.FromHwnd(helper.Handle);
            hotkeySource.AddHook(HwndHook);
        }
        public void RegisterTriggerKey()
        {
            if (HotkeyRegistered) return;

            var helper = new WindowInteropHelper(mainWin);

            bool success = RegisterHotKey(helper.Handle, HOTKEY_ID, 0, 0x2C);
            if (!success) {
                int error = Marshal.GetLastWin32Error();
                string reason = "";
                switch (error)
                {
                    case 1409:
                        reason = "The hotkey is already registered by another app.";
                        break;
                }
                if (reason != "") reason = $"\n({reason})";
                throw new Exception($"Failed to register PrtSc HKey. Win32 Error: {error}" + reason);
            }
            HotkeyRegistered = true;
        }
        public void UnregisterTriggerHotkey()
        {
            if (!HotkeyRegistered) return;

            var helper = new WindowInteropHelper(mainWin);
            UnregisterHotKey(helper.Handle, HOTKEY_ID);

        }

        private async void SelfCaptureOnHotkey()
        {
            await HandlePrtScAsync();
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
