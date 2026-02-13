using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Media.Imaging;

namespace FlintCapture2.Scripts
{
    public class ScreenshotHandler
    {
        public string ScreenshotDirectory = "";
        private MainWindow mainWin;
        public ScreenshotHandler(string screenshotDirectory, MainWindow mainWin)
        {
            this.mainWin = mainWin;

            ScreenshotDirectory = screenshotDirectory;
            if (Directory.Exists(ScreenshotDirectory)) Directory.CreateDirectory(ScreenshotDirectory);
        }

        public List<NotificationWindow> notificationWindowQueue = new();
        /// <summary>
        /// 
        /// </summary>
        /// <param name="ClipboardWaitMS">Waits a specified number of milliseconds for the clipboard to catch up after the 'print screen' key has been pressed.</param>
        /// <returns></returns>
        public async Task HandlePrtScAsync(int ClipboardWaitMS = 100)
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
                string ssImagePath = Path.Combine(ScreenshotDirectory, $"copied_image_{timestamp}.png");

                using (var fileStream = new FileStream(ssImagePath, FileMode.Create))
                {
                    var encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(systemCopiedImage));
                    encoder.Save(fileStream);
                }

                Debug.WriteLine($"Saved to {ssImagePath}");
                notificationWindowQueue.Add(new NotificationWindow(mainWin, timestamp, ssImagePath));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to save image: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);

                App.Current.Shutdown();
                return;
            }

            // wait for key release
            while (GetKeyStateAsBool(KeyStateHelper.VK_SNAPSHOT))
            {
                await Task.Delay(100);
            }
        }

        private bool GetKeyStateAsBool(int VK)
        {
            return ((KeyStateHelper.GetAsyncKeyState(VK) & 0x8000) != 0);
        }
    }
}
