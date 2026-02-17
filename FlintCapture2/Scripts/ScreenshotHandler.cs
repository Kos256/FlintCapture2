using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using static FlintCapture2.Scripts.ScreenshotHandler;

namespace FlintCapture2.Scripts
{
    public class ScreenshotHandler
    {
        public string ScreenshotDirectory = "";
        private string rawScreenshotDir = "";
        private MainWindow mainWin;

        public enum HandlerType
        {
            Unknown = 0,
            WindowsClipboard = 1,
            SelfCapture = 2,
            Self_DXGI = 3,
            Self_Blt = 4,
            Self_IForgot = 5,
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
                    CompositionTarget.Rendering += mainWin.OnFramePrtSc; // do not assign this, this is the legacy method that is replaced with RegisterHotkey
                    break;

                case HandlerType.SelfCapture:

                    // leftoff latest!!!
                    break;
            }
        }

        public void RegisterKey()
        {

        }

        public List<NotificationWindow> notificationWindowQueue = new();
       
        public async Task HandlePrtScAsync()
        {

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
