using FlintCapture2.Scripts;
using Microsoft.Win32;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using NOTIFYICONDATA = FlintCapture2.Scripts.SystemTrayHandler.NOTIFYICONDATA;
using SDM = FlintCapture2.Scripts.SaveDataManagement;

namespace FlintCapture2
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {

        public const bool EnableContextIconMenuBehavior_IDidThisForYouYogurt_THankMeLater = false; // remove this later once ctx menu is finished
        public static (SDM.DataLayouts.UserMetadata MetaData, SDM.DataLayouts.ScreenshotIndexingMetadata ScreenshotIndex) UserData;
        public MainWindow? mainWin;
        public DialogBoxWindow? initDbox;
        public IndicatorWindow? indicatorWin;
        public AppUpdater? appUpdater;
        public ScreenshotHandler.HandlerType SelectedCaptureType;
        public bool WasSnippingToolEnabledBefore = false;
        protected override void OnStartup(StartupEventArgs e)
        {
            try
            {
                //throw new Exception("An exception object was created and thrown in OnStartup(StartupEventArgs e)", new Exception("Startup was blocked by this line of code."));

                SelectedCaptureType = ScreenshotHandler.HandlerType.SelfCapture;
                appUpdater = new();
                indicatorWin = new();

                if (!ExtraUtils.IsAddedToStartMenu()) ExtraUtils.AddToStartMenu();

                if (HelperMethods.PrtScBindedToSnippingTool())
                {
                    WasSnippingToolEnabledBefore = true;
                    initDbox = new(DialogBoxWindow.DialogType.SnippingToolTempDisabledDisclaimer);
                    initDbox.Show();
                }
                else
                {
                    DBoxFlagContinueMainWindow();
                }
            }
            catch (Exception ex)
            {
                bool legacyError = false;

                if (legacyError)
                {
                    string errBody = $"There was an exception:\n\n{ex.Message}";
                    if (ex.InnerException != null) errBody += $"\n\nInner exception states:\n{ex.InnerException.Message}";

                    errBody += "\n\nDo you want to copy this error? (It may help in troubleshooting...or make a report on the GitHub repo lol)";

                    // replace this with a new custom dbox switch case eventually instead of using MessageBox
                    MessageBoxResult msgbox = MessageBox.Show(errBody, "FlintCapture failed to start up...", MessageBoxButton.YesNo, MessageBoxImage.Error);
                    if (msgbox == MessageBoxResult.Yes) Clipboard.SetText(errBody);
                }

                DialogBoxWindow dbox = new(DialogBoxWindow.DialogType.AppFailedToStart)
                {
                    Argument0_Ex_AppFailedToStart = ex
                };
                dbox.Show();
            }

        }


        protected override void OnSessionEnding(SessionEndingCancelEventArgs e) // windows is shutting down or restarting
        {


            base.OnSessionEnding(e);
        }
        protected override void OnExit(ExitEventArgs e) // app is shutting down
        {
            mainWin!.AppSessionRuntime.Stop();
            UserData.MetaData.HoursRan += mainWin.AppSessionRuntime.Elapsed;
            if (UserData.MetaData.LongestSessionRan < mainWin.AppSessionRuntime.Elapsed) UserData.MetaData.LongestSessionRan = mainWin.AppSessionRuntime.Elapsed;
            SDM.Save(UserData.MetaData, "usermeta.json");

            base.OnExit(e);
        }

        public void DBoxFlagContinueMainWindow()
        {
            mainWin = new(SelectedCaptureType);
            mainWin.Show();
            indicatorWin?.ShowIndicator();
            _ = CheckUpdatesAsyncDeferred(); // todo: look into why this makes the mouse stutter. update: making it an awaited task is good practice but its just making a new dbox that lags it
            // ^ possible solution: reserve an updater dbox object and just call .Show() on it when an update is available
        }
        

        public AppUpdater.UpdateInfo? LastFetchedUpdateInfo;
        public async Task CheckUpdates()
        {
            LastFetchedUpdateInfo = await appUpdater!.IsUpdateAvailable();

            bool actualOutput = true;

            if (actualOutput)
            {
                if (LastFetchedUpdateInfo.AvailableUpdate == AppUpdater.UpdateInfo.UpdateStatus.NewerAvailable)
                {
                    initDbox = new(DialogBoxWindow.DialogType.UpdateAvailable);
                    initDbox.Show();
                }
            }
            else
            {
                string result = "";
                result += $"Update available? {LastFetchedUpdateInfo.AvailableUpdate}";
                result += $"\nVersion: {LastFetchedUpdateInfo.Version}";
                if (LastFetchedUpdateInfo.Failed != null) result += $"Failed: {LastFetchedUpdateInfo.Failed}";

                Debug.WriteLine("-- Update stats --\n" + result + "\n------------------");
                MessageBox.Show(result, "Update stats", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        public async Task CheckUpdatesAsyncDeferred()
        {
            // Let layout + first render frame finish
            await Task.Yield();

            await Task.Delay(3000);

            await CheckUpdates();
        }
    }

    public static class PROJCONSTANTS
    {
        public const string AssemblyName = "FlintCapture2";
        public const string PackLocationFormat = $"pack://application:,,,/{AssemblyName};component/";
        public static Version AppVersion =>
            Assembly.GetExecutingAssembly().GetName().Version!;
    }
}
