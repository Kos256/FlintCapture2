using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Media;
using static FlintCapture2.Scripts.SystemTrayHandler;

namespace FlintCapture2.Scripts
{
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

        /// <summary>
        /// Attempts to convert a color code in hex form to a Color object. I was too lazy to use the long ahh built in line so here's a shorter one invoking the longer one basically.
        /// </summary>
        /// <param name="hexInput">This should be obvious</param>
        /// <param name="softException">Default to true. Function reports the exception in the HexColor().Exception format while outputting a magenta color (like broken materials in games). If set to false, the exception will be thrown like usual instead of reported by the variable. Which means YOU have to handle the exception now.</param>
        /// <returns></returns>
        public static (Color Result, Exception? Exception) HexColor(string hexInput, bool softException = true)
        {
            if (!hexInput.StartsWith("#")) hexInput.Prepend('#');

            Color result = Color.FromRgb(255, 0, 255); // magenta color from games where the texture not found results in material being pink/magenta
            Exception? exceptionResult = null;

            if (softException) // safe branch
            {
                try
                {
                    result = (Color)ColorConverter.ConvertFromString(hexInput);
                }
                catch (Exception ex)
                {
                    exceptionResult = ex;
                }
            }
            else result = (Color)ColorConverter.ConvertFromString(hexInput); // daredevil branch


            return (result, exceptionResult);
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
        static extern bool Shell_NotifyIcon(uint dwMessage, ref NOTIFYICONDATA lpData);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static extern bool DestroyIcon(IntPtr handle);

        public const uint NIM_ADD = 0x00000000;
        public const uint NIM_MODIFY = 0x00000001;
        public const uint NIM_DELETE = 0x00000002;
    }
}
