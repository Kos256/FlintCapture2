using System;
using System.IO;
using System.Text.Json;
using System.Windows;

namespace FlintCapture2.Scripts
{
    public class SaveDataManagement
    {
        public static string? DefaultPath { get; set; }
        public static bool Initialized { get; private set; } = false;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            AllowDuplicateProperties = false,
            AllowTrailingCommas = true,
        };


        public static void Initialize(string path)
        {
            Initialized = true;
            DefaultPath = path;
        }


        public static void Save<T>(T data, string fileName, string? path = null)
        {
            if (!Initialized) MessageBox.Show("SDM is not initialized!", "Init error", MessageBoxButton.OK, MessageBoxImage.Error);

            path ??= DefaultPath;

            Directory.CreateDirectory(path);

            string filePath = Path.Combine(path, fileName);

            string json = JsonSerializer.Serialize(data, JsonOptions);

            File.WriteAllText(filePath, json);
        }


        public static T? Load<T>(string fileName, string? path = null)
        {
            if (!Initialized) MessageBox.Show("SDM is not initialized!", "Init error", MessageBoxButton.OK, MessageBoxImage.Error);

            path ??= DefaultPath;

            string filePath = Path.Combine(path, fileName);

            if (!File.Exists(filePath))
                return default;

            string json = File.ReadAllText(filePath);

            return JsonSerializer.Deserialize<T>(json, JsonOptions);
        }


        public static bool Exists(string fileName, string? path = null)
        {
            if (!Initialized) MessageBox.Show("SDM is not initialized!", "Init error", MessageBoxButton.OK, MessageBoxImage.Error);

            path ??= DefaultPath;

            return File.Exists(Path.Combine(path, fileName));
        }


        public static void Delete(string fileName, string? path = null)
        {
            if (!Initialized) MessageBox.Show("SDM is not initialized!", "Init error", MessageBoxButton.OK, MessageBoxImage.Error);

            path ??= DefaultPath;

            string filePath = Path.Combine(path, fileName);

            if (File.Exists(filePath))
                File.Delete(filePath);
        }


        #region defined data layouts for saving
        public class DataLayouts
        {
            public class Legacy // legacy data layouts
            {

            } 

            public class UserMetadata
            {
                public int SchemaVersion { get; set; } = 1;

                public bool IsFirstTime { get; set; } = true;
                public Version? LastVersionRan { get; set; } = null;


                // fun metrics:
                public TimeSpan HoursRan { get; set; } = TimeSpan.Zero;
                public TimeSpan LongestSessionRan { get; set; } = TimeSpan.Zero;
                public int LaunchCount { get; set; } = 0;
                public int ScreenshotTriggerCount { get; set; } = 0;
            }
        }
        #endregion
    }
}