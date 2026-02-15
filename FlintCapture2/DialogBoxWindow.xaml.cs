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
            SnippingToolEnabledWarning = 1,     // snipping tool error v2 (attempts to change setting on its own)
            SnippingToolEnabledError = 2,       // snipping tool error v1 (tells the user to change setting)
            SnippingToolEnabled = 3,            // snipping tool error v3 (v2 but custom dbox)
        };
        public DialogType dboxType;
        public DialogBoxWindow(DialogType dboxType)
        {
            InitializeComponent();

            this.dboxType = dboxType;

            ((TextBlock)dboxBtnPrimary.Content).Inlines.Clear();
            ((TextBlock)dboxBtnSecondary.Content).Inlines.Clear();
            dboxBtnSecondary.Visibility = Visibility.Hidden;

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
                    bodyMsg.Inlines.Add(new Underline(new Run("turned off.")) { FontFamily = (FontFamily)App.Current.Resources["ExoBold"] });
                    bodyMsg.Inlines.Add(new LineBreak() { FontSize = 7 });
                    bodyMsg.Inlines.Add(new Run("Do you want to disable that setting?"));
                    bodyMsg.Inlines.Add(new Run("\n(Don't worry! You can always turn it back on in windows settings)") { Foreground = Brushes.LightGreen, FontSize = 14 });
                    dboxIcon.Source = new Uri(Path.Combine(PROJCONSTANTS.PackLocationFormat, "assets", "icons", "snipping tool reject.svg"));
                    TextBlock tooltip = new();
                    tooltip.Inlines.Add(new Run("Pressing this will "));
                    tooltip.Inlines.Add(new Underline(new Run("close FlintCapture")) { Foreground = Brushes.Black, FontFamily = (FontFamily)App.Current.Resources["ExoBold"] });
                    tooltip.Inlines.Add(new Run("."));
                    closeBtn.ToolTip = tooltip;
                    closeBtn.Click += dboxClose_Generic;
                    dboxBtnPrimary.Click += dboxPrimary_SnippingTool;
                    ((TextBlock)dboxBtnPrimary.Content).Inlines.Add(new Run("Disable") { Foreground = Brushes.Lime, FontFamily = (FontFamily)App.Current.Resources["ExoBold"] });
                    ((TextBlock)dboxBtnPrimary.Content).Inlines.Add(new Run(" snipping tool!") { Foreground = Brushes.Lime });

                    DialogBoxIntro();
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(dboxType));
            }
        }


        private async void DialogBoxIntro()
        {
            ESP.PlaySound("dbox in");

            bodyMsg.Opacity = 0;
            dboxIcon.Opacity = 0;
            RootGrid.Opacity = 0;

            await Task.Delay(50); // a little delay to match up visuals with the SFX

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

            await Task.Delay(200);
            dboxIcon.BeginAnimation(OpacityProperty, new DoubleAnimation
            {
                To = 1,
                Duration = TimeSpan.FromSeconds(1),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut },
            });
            await Task.Delay(300);
            bodyMsg.BeginAnimation(OpacityProperty, new DoubleAnimation
            {
                To = 1,
                Duration = TimeSpan.FromSeconds(1),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut },
            });
        }

        private async void DialogBoxOutro()
        {
            DoubleAnimation fadeOutHalfSec = new DoubleAnimation
            {
                To = 0,
                Duration = TimeSpan.FromSeconds(0.4),
                EasingFunction = new QuinticEase { EasingMode = EasingMode.EaseInOut },
            };

            ESP.PlaySound("dbox out");

            //await Task.Delay(50); // a little delay to match up visuals with the SFX

            bodyMsg.BeginAnimation(OpacityProperty, fadeOutHalfSec);
            dboxIcon.BeginAnimation(OpacityProperty, fadeOutHalfSec);
            btnContainerGrid.BeginAnimation(OpacityProperty, fadeOutHalfSec);
            RootGrid.BeginAnimation(WidthProperty, new DoubleAnimation
            {
                To = 0,
                Duration = TimeSpan.FromSeconds(0.5),
                EasingFunction = new QuinticEase { EasingMode = EasingMode.EaseIn },
            });
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

        private async void dboxClose_Generic(object sender, RoutedEventArgs e)
        {
            DialogBoxOutro();
            await Task.Delay(4000);
            App.Current.Shutdown();
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
            ((App)Application.Current).DBoxFlagContinueMainWindow();

        }
    }
}
