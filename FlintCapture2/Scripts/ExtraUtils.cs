using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;

namespace FlintCapture2.Scripts
{
    public class ExtraUtils
    {
        public static string PickWeightedMessage(Dictionary<string, float> messages)
        {
            // Sum up all probabilities
            float total = messages.Values.Sum();

            // Pick a random number between 0 and total
            float roll = (float)(Random.Shared.NextDouble() * total);

            float cumulative = 0f;
            foreach (var kvp in messages)
            {
                cumulative += kvp.Value;
                if (roll <= cumulative)
                {
                    return kvp.Key;
                }
            }

            // Fallback (should never happen if probabilities > 0)
            return messages.Keys.Last();
        }

        private const string ShortcutName = "FlintCapture.lnk";

        /// <summary>
        /// Checks if the Start Menu shortcut already exists and points to this exe.
        /// </summary>
        public static bool IsAddedToStartMenu()
        {
            string exePath = Assembly.GetExecutingAssembly().Location;
            string shortcutPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.StartMenu),
                "Programs",
                ShortcutName
            );

            if (!File.Exists(shortcutPath))
                return false;

            // Optionally verify it points to our exe using PowerShell
            try
            {
                string psCommand = $@"
$WshShell = New-Object -ComObject WScript.Shell;
$Shortcut = $WshShell.CreateShortcut('{shortcutPath}');
Write-Output $Shortcut.TargetPath
";

                var psi = new ProcessStartInfo("powershell", $"-NoProfile -Command \"{psCommand}\"")
                {
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(psi);
                string target = process.StandardOutput.ReadToEnd().Trim();
                process.WaitForExit();

                return string.Equals(target, exePath, StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Creates a Start Menu shortcut pointing to this WPF exe.
        /// </summary>
        public static void AddToStartMenu()
        {
            string exePath = Process.GetCurrentProcess().MainModule!.FileName!;
            string exeDir = Path.GetDirectoryName(exePath);
            string shortcutPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.StartMenu),
                "Programs",
                ShortcutName
            );

            // Only create if it doesn't exist or points somewhere else
            if (IsAddedToStartMenu())
                return;

            string psCommand = $@"
$WshShell = New-Object -ComObject WScript.Shell;
$Shortcut = $WshShell.CreateShortcut('{shortcutPath}');
$Shortcut.TargetPath = '{exePath}';
$Shortcut.WorkingDirectory = '{exeDir}';
$Shortcut.IconLocation = '{exePath},0';
$Shortcut.Description = 'FlintCapture2';
$Shortcut.Save();
";

            var psi = new ProcessStartInfo
            {
                FileName = "powershell",
                Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{psCommand}\"",
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            };

            Process.Start(psi);
        }
    }
}
