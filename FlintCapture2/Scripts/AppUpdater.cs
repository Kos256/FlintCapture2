using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows.Threading;

namespace FlintCapture2.Scripts
{
    /// <summary>
    /// Script responsible for automatically updating the entire app upon closing!
    /// </summary>
    public class AppUpdater
    {
        private HttpClient client = new();

        public async Task<UpdateInfo> IsUpdateAvailable()
        {
            client.DefaultRequestHeaders.UserAgent.ParseAdd("FlintCapture2");

            string url = "https://api.github.com/repos/Kos256/FlintCapture2/releases/latest";

            var result = new UpdateInfo();

            try
            {
                string json = await client.GetStringAsync(url);

                using JsonDocument doc = JsonDocument.Parse(json);

                string tag = doc.RootElement.GetProperty("tag_name").GetString()!;
                string publishedAt = doc.RootElement.GetProperty("published_at").GetString()!;

                // Extract version from something like:
                // FlintCapture-v3.1.64.298 or FlintCapture-v.X.XX.XXX.XXXX blah blah blah the digit count doesn't matter I hope...
                Match match = Regex.Match(tag, @"\d+(\.\d+){1,3}");

                if (!match.Success)
                    return new UpdateInfo($"Invalid version format in tag: {tag}");

                Version latestVersion = new Version(match.Value);
                Version currentVersion = PROJCONSTANTS.AppVersion;

                result.Version = latestVersion;
                result.ReleaseDate = DateTime.Parse(publishedAt);

                if (latestVersion > currentVersion)
                    result.AvailableUpdate = UpdateInfo.UpdateStatus.NewerAvailable;
                else
                    result.AvailableUpdate = UpdateInfo.UpdateStatus.LatestInstalled;

                return result;
            }
            catch (HttpRequestException ex)
            {
                return new UpdateInfo($"Network error: {ex.Message}");
            }
            catch (JsonException ex)
            {
                return new UpdateInfo($"Invalid GitHub response: {ex.Message}");
            }
            catch (Exception ex)
            {
                return new UpdateInfo($"Unexpected error: {ex.Message}");
            }
        }

        public class UpdateInfo
        {
            public string? Failed { get; set; }
            public UpdateStatus AvailableUpdate { get; set; }
            public Version? Version { get; set; }
            public DateTime? ReleaseDate { get; set; }

            public UpdateInfo(string? FailedReason = null)
            {
                if (FailedReason != null)
                {
                    Failed = FailedReason;
                }
                AvailableUpdate = UpdateStatus.Unknown;
            }

            public enum UpdateStatus
            {
                Unknown = 0,
                NewerAvailable = 1,
                LatestInstalled = 2,
            }
        }
    }
}
