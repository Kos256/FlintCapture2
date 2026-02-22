using ESP = FlintCapture2.Scripts.EmbeddedSoundPlayer;
using System;
using System.IO;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Animation;
using NAudio.Wasapi.CoreAudioApi.Interfaces;
using FlintCapture2.Scripts;
using System.Diagnostics;

namespace FlintCapture2
{
    /// <summary>
    /// Interaction logic for DialogBoxWindow.xaml
    /// </summary>
    public partial class DialogBoxWindow : Window
    {
        public enum DialogType
        {
            Unknown = 0,
            SnippingToolEnabledWarning = 1,         // snipping tool error v2 (attempts to change setting on its own)
            SnippingToolEnabledError = 2,           // snipping tool error v1 (tells the user to change setting)
            SnippingToolEnabled = 3,                // snipping tool error v3 (v2 but custom dbox)
            SnippingToolTempDisabledDisclaimer = 4, // RegisterHotkey has disabled snipping tool on PrtSc until the app closes
            UpdateAvailable = 5,
            UpdateConfirm = 6,
        };
        public DialogType dboxType;
        public DialogBoxWindow(DialogType dboxType)
        {
            InitializeComponent();

            this.dboxType = dboxType;

            ((TextBlock)dboxBtnPrimary.Content).Inlines.Clear();
            ((TextBlock)dboxBtnSecondary.Content).Inlines.Clear();
            dboxBtnSecondary.Visibility = Visibility.Hidden;

            TextBlock tooltip = new();
            tooltip.Inlines.Add(new Run("Pressing this will "));
            tooltip.Inlines.Add(new Underline(new Run("close FlintCapture")) { FontFamily = (FontFamily)App.Current.Resources["ExoBold"] });
            tooltip.Inlines.Add(new Run("."));
            closeBtn.ToolTip = tooltip;

            Activated += (s, e) =>
            {
                dboxBorder.BorderBrush.Opacity = 1;
                dboxTitle.FontFamily = (FontFamily)App.Current.Resources["ExoBold"];
            };
            Deactivated += (s, e) =>
            {
                dboxBorder.BorderBrush.Opacity = 0;
                dboxTitle.FontFamily = (FontFamily)App.Current.Resources["ExoRegular"];
            };

            Loaded += DialogBoxWindow_Loaded;
        }

        private void DialogBoxWindow_Loaded(object sender, RoutedEventArgs e)
        {
            switch (dboxType)
            {
                case DialogType.SnippingToolEnabledWarning:
                    LegacyWarnSnippingTool(true);
                    ((App)Application.Current).DBoxFlagContinueMainWindow();
                    Close();
                    break;

                case DialogType.SnippingToolEnabledError:
                    LegacyWarnSnippingTool(false);
                    ((App)Application.Current).DBoxFlagContinueMainWindow();
                    Close();
                    break;

                case DialogType.SnippingToolEnabled:
                    //bodyMsg.Text = "The system settings allow PrtSc to open snipping tool. FlintCapture needs that setting turned off. Do you want to turn that setting off? (You can always turn it back on in windows settings)";
                    bodyMsg.Text = "";
                    bodyMsg.Inlines.Add(new Run("The system settings allow PrtSc to open snipping tool. FlintCapture needs that setting "));
                    bodyMsg.Inlines.Add(new Underline(new Run("turned off.")) { Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF00B0FF")), FontFamily = (FontFamily)App.Current.Resources["ExoBold"] });
                    bodyMsg.Inlines.Add(new LineBreak() { FontSize = 7 });
                    bodyMsg.Inlines.Add(new Run("Do you want to disable that setting?"));
                    bodyMsg.Inlines.Add(new Run("\n(Don't worry! You can always turn it back on in windows settings)") { Foreground = Brushes.LightGreen, FontSize = 14 });
                    
                    dboxIcon.Source = new Uri(Path.Combine(PROJCONSTANTS.PackLocationFormat, "assets", "icons", "snipping tool reject.svg"));
                    
                    closeBtn.Click += dboxClose_Generic;
                    dboxBtnPrimary.Click += dboxPrimary_SnippingTool;
                    
                    ((TextBlock)dboxBtnPrimary.Content).Inlines.Add(new Run("Disable") { Foreground = Brushes.Lime, FontFamily = (FontFamily)App.Current.Resources["ExoBold"] });
                    ((TextBlock)dboxBtnPrimary.Content).Inlines.Add(new Run(" snipping tool!") { Foreground = Brushes.Lime });

                    DialogBoxIntro();
                    break;

                case DialogType.SnippingToolTempDisabledDisclaimer:
                    bodyMsg.Text = "";
                    bodyMsg.Inlines.Add(new Run("Disclaimer! ") { Foreground = Brushes.Yellow, FontFamily = (FontFamily)App.Current.Resources["ExoBold"] });
                    bodyMsg.Inlines.Add(new Run("Pressing PrtSc will NOT open snipping tool and will instead take a screenshot with FlintCapture. \nDue to your current user settings in FlintCapture, "));
                    bodyMsg.Inlines.Add(new Run("this behavior is temporary. ") { Foreground = Brushes.Orange });
                    //bodyMsg.Inlines.Add(new Run("Once FlintCapture closes, it's back to normal.") { Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF00B0FF")) });
                    bodyMsg.Inlines.Add(new Run("Once FlintCapture closes, you may turn it back on if you wish :)") { Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF00B0FF")) });

                    dboxIcon.Source = new Uri(Path.Combine(PROJCONSTANTS.PackLocationFormat, "assets", "icons", "snipping tool reject.svg"));
                    
                    closeBtn.Click += dboxClose_Generic;
                    dboxBtnPrimary.Click += dboxPrimary_SnippingTool;
                    dboxBtnSecondary.Visibility = Visibility.Visible;

                    ((TextBlock)dboxBtnPrimary.Content).Inlines.Add(new Run("Got it!"));
                    ((TextBlock)dboxBtnSecondary.Content).Inlines.Add(new Run("Don't show again") { FontFamily = (FontFamily)App.Current.Resources["ExoBold"] });

                    DialogBoxIntro();
                    break;

                case DialogType.UpdateAvailable:
                    bodyMsg.Text = "";
                    bodyMsg.Inlines.Add(new Italic(new Run("Pssssssst... ") { FontFamily = (FontFamily)App.Current.Resources["ExoItalic"] }));
                    bodyMsg.Inlines.Add(new Run("there is a newer version of FlintCapture available!"));
                    bodyMsg.Inlines.Add(new LineBreak());
                    bodyMsg.Inlines.Add(new LineBreak());
                    bodyMsg.Inlines.Add(new Run("You currently have: "));
                    bodyMsg.Inlines.Add(new Run($"v{PROJCONSTANTS.AppVersion.ToString()}") { Foreground = Brushes.Orange, FontFamily = (FontFamily)App.Current.Resources["ExoItalic"] });
                    bodyMsg.Inlines.Add(new Run("\nYou can install: "));
                    bodyMsg.Inlines.Add(new Run($"v{((App)Application.Current).LastFetchedUpdateInfo?.Version}") { Foreground = Brushes.Lime, FontFamily = (FontFamily)App.Current.Resources["ExoItalic"] });

                    string tooltip = "";
                    int tooltipSelector = Random.Shared.Next() % 2;
                    switch (tooltipSelector)
                    {
                        case 0:
                            tooltip = "Dismiss for now";
                            break;

                        case 1:
                            tooltip = "GRRR DON'T BOTHER ME I AM HAPPY WITH MY CURRENT VERSION GO AWAYYYY";
                            break;
                    }
                    closeBtn.ToolTip = tooltip;

                    closeBtn.Click += dboxDismiss_Generic;
                    //dboxBtnPrimary.Click += dboxPrimary_SnippingTool; // todo: add updater logic later
                    dboxBtnSecondary.Click += dboxDismiss_Generic;
                    dboxBtnSecondary.Visibility = Visibility.Visible;

                    ((TextBlock)dboxBtnPrimary.Content).Inlines.Add(new Run("Sure!") { Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF00B0FF")), FontFamily = (FontFamily)App.Current.Resources["ExoBold"] });
                    ((TextBlock)dboxBtnSecondary.Content).Inlines.Add(new Run("Meh I'll pass..."));

                    DialogBoxIntro("dbox in alt");
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(dboxType));
            }
        }

        private async void DialogBoxIntro(string overrideSound = "")
        {
            ESP.PlaySound((overrideSound == "") ? "dbox in" : overrideSound);

            bodyMsg.Opacity = 0;
            dboxIcon.Opacity = 0;
            RootGrid.Opacity = 0;
            btnContainerGrid.Opacity = 0;
            closeBtn.Opacity = 0;

            await Task.Delay(50); // a little delay to match up visuals with the SFX

            double savedRootGridHeight = RootGrid.ActualHeight;
            RootGrid.Height = savedRootGridHeight;

            RootGrid.BeginAnimation(OpacityProperty, new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromSeconds(0.1),
                EasingFunction = new QuinticEase { EasingMode = EasingMode.EaseIn },
            });
            RootGrid.BeginAnimation(WidthProperty, new DoubleAnimation
            {
                From = 0,
                To = RootGrid.Width,
                Duration = TimeSpan.FromSeconds(0.5),
                EasingFunction = new QuinticEase { EasingMode = EasingMode.EaseIn },
            });
            await Task.Delay(500);
            RootGrid.BeginAnimation(WidthProperty, new DoubleAnimation
            {
                From = RootGrid.Width + 50,
                To = RootGrid.Width,
                Duration = TimeSpan.FromSeconds(1),
                EasingFunction = new ElasticEase { Oscillations = 2 },
            });

            DoubleAnimation elemFadeIn = new DoubleAnimation
            {
                To = 1,
                Duration = TimeSpan.FromSeconds(1),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut },
            };

            await Task.Delay(100);
            dboxIcon.BeginAnimation(OpacityProperty, elemFadeIn);
            await Task.Delay(100);
            bodyMsg.BeginAnimation(OpacityProperty, elemFadeIn);
            await Task.Delay(100);
            btnContainerGrid.BeginAnimation(OpacityProperty, elemFadeIn);
            await Task.Delay(100);
            closeBtn.BeginAnimation(OpacityProperty, elemFadeIn);

            RootGrid.Height = double.NaN;
        }

        SoundInstance? dboxOutroSoundInstance;
        private async void DialogBoxOutro()
        {
            DoubleAnimation fadeOutHalfSec = new DoubleAnimation
            {
                To = 0,
                Duration = TimeSpan.FromSeconds(0.4),
                EasingFunction = new QuinticEase { EasingMode = EasingMode.EaseInOut },
            };

            //closeBtn.IsEnabled = false;
            //dboxBtnPrimary.IsEnabled = false;
            //dboxBtnSecondary.IsEnabled = false;
            foreach (Button b in btnContainerGrid.Children) b.IsEnabled = false;

            dboxOutroSoundInstance = ESP.PlayTracked("dbox out");

            //await Task.Delay(50); // a little delay to match up visuals with the SFX

            double savedRootGridHeight = RootGrid.ActualHeight;

            bodyMsg.BeginAnimation(OpacityProperty, fadeOutHalfSec);
            dboxIcon.BeginAnimation(OpacityProperty, fadeOutHalfSec);
            btnContainerGrid.BeginAnimation(OpacityProperty, fadeOutHalfSec);
            RootGrid.BeginAnimation(WidthProperty, new DoubleAnimation
            {
                To = 0,
                Duration = TimeSpan.FromSeconds(0.5),
                EasingFunction = new QuinticEase { EasingMode = EasingMode.EaseIn },
            });

            await Task.Delay(1000);
            Close();

            RootGrid.Height = savedRootGridHeight;
        }

        private void LegacyWarnSnippingTool(bool allowChangeSettingOption = true)
        {
            if (allowChangeSettingOption)
            {
                var result = MessageBox.Show("The system settings allow PrtSc to open snipping tool. FlintCapture needs that setting turned off (because it's a snipping tool replacement, duh)." +
                    "\n\nDo you want to turn that setting off? (You can always turn it back on in windows settings)",
                    "FlintCapture",
                    MessageBoxButton.OKCancel,
                    MessageBoxImage.Warning
                );
                if (result == MessageBoxResult.OK)
                {
                    try
                    {
                        HelperMethods.PrtScBindedToSnippingTool(false);
                        if (HelperMethods.PrtScBindedToSnippingTool()) // if the setting is still true, quit
                        {
                            throw new Exception("Failed to change setting, the user will have to change it themselves.");
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message,
                            "FlintCapture",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error
                        );
                        App.Current.Shutdown();
                        return;
                    }
                }
                else
                {
                    MessageBox.Show("FlintCapture cannot continue if PrtSc is binded to snipping tool!\nExiting...",
                        "FlintCapture",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error
                    );
                    App.Current.Shutdown();
                    return;
                }
            }
            else
            {
                while (HelperMethods.PrtScBindedToSnippingTool())
                {
                    var result = MessageBox.Show("The system settings allow PrtSc to open snipping tool. FlintCapture needs that setting turned off (you might need to sign out and log back into windows)." +
                        "\n\nPlease turn it off and retry. Or hit cancel to quit FlintCapture.\n",
                        "FlintCapture",
                        MessageBoxButton.RetryCancel,
                        MessageBoxImage.Warning
                    );
                    if (result == MessageBoxResult.Retry)
                    {
                        // do nothing, and let the loop run again
                    }
                    else
                    {
                        App.Current.Shutdown();
                        return;
                    }
                }
            }
        }

        enum DBoxCloseStrategy
        {
            FixedDelay,
            WaitForSound,
            WaitForSoundWithTimeout,
            ChatGPT
        }
        private async void dboxClose_Generic(object sender, RoutedEventArgs e)
        {
            // hardcodedWait - false: depends on sound to fully end first. true: 4.0s no matter what, and is more reliable (sometimes sound driver may not be enabled and we dont want to create a depend)

            DBoxCloseStrategy hardcodedWait = DBoxCloseStrategy.WaitForSoundWithTimeout;
            /*
             * 0 = close after 4.0s no matter what
             * 1 = wait for the sound to end, then close
             * 2 = wait for the sound to end, but if it takes longer than 10s, force close
             * 3 = chatgpt's implementation of 2
             */
           
            DialogBoxOutro();
            
            switch (hardcodedWait)
            {
                case (DBoxCloseStrategy.FixedDelay):
                    await Task.Delay(4000);
                    break;

                case (DBoxCloseStrategy.WaitForSound):
                    if (dboxOutroSoundInstance != null)
                    {
                        while (dboxOutroSoundInstance.Position < dboxOutroSoundInstance.Duration)
                            await Task.Delay(500); // prevent freezing, loop runs in 0.5s interval
                    }
                    break;

                case (DBoxCloseStrategy.WaitForSoundWithTimeout):
                    Stopwatch timer = new();
                    timer.Start();

                    if (dboxOutroSoundInstance != null)
                    {
                        while (dboxOutroSoundInstance.Position < dboxOutroSoundInstance.Duration)
                        {
                            await Task.Delay(500); // prevent freezing, loop runs in 0.5s interval
                            if (timer.Elapsed > TimeSpan.FromSeconds(10)) break;
                        }
                    }
                    break;

                case (DBoxCloseStrategy.ChatGPT):
                    if (dboxOutroSoundInstance != null)
                    {
                        var timeout = hardcodedWait == DBoxCloseStrategy.WaitForSoundWithTimeout
                            ? TimeSpan.FromSeconds(10)
                            : Timeout.InfiniteTimeSpan;

                        var start = DateTime.UtcNow;

                        while (dboxOutroSoundInstance.Position < dboxOutroSoundInstance.Duration)
                        {
                            if (timeout != Timeout.InfiniteTimeSpan &&
                                DateTime.UtcNow - start > timeout)
                                break;

                            await Task.Delay(200);
                        }
                    }
                    break;
            }

            App.Current.Shutdown();
        }
        private async void dboxDismiss_GenericContinueFlags(object sender, RoutedEventArgs e)
        {
            DialogBoxOutro();
            await Task.Delay(1000);
            ((App)Application.Current).DBoxFlagContinueMainWindow();
        }
        private async void dboxDismiss_Generic(object sender, RoutedEventArgs e)
        {
            DialogBoxOutro();
        }

        private async void dboxPrimary_SnippingTool(object sender, RoutedEventArgs e)
        {
            try
            {
                HelperMethods.PrtScBindedToSnippingTool(false);
                if (HelperMethods.PrtScBindedToSnippingTool()) // if the setting is still true, quit
                {
                    throw new Exception("Failed to change setting, the user will have to change it themselves.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message,
                    "FlintCapture",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
                App.Current.Shutdown();
                return;
            }

            DialogBoxOutro();
            await Task.Delay(1000);
            ((App)Application.Current).DBoxFlagContinueMainWindow();

        }
    }
}
