using ESP = FlintCapture2.Scripts.EmbeddedSoundPlayer;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Media.Animation;

namespace FlintCapture2
{
    /// <summary>
    /// Interaction logic for IndicatorWindow.xaml
    /// </summary>
    public partial class IndicatorWindow : Window
    {
        public IndicatorWindow()
        {
            InitializeComponent();

            ShowActivated = false;
            Left = SystemParameters.WorkArea.Width - Width;
            Top = SystemParameters.PrimaryScreenHeight - Height;

            popupBox.Margin = new(0, 0, 0, -popupBox.Height - 10);
        }

        public async void ShowIndicator()
        {
            DoubleAnimation scaleOut = new DoubleAnimation
            {
                To = 0,
                BeginTime = TimeSpan.FromSeconds(0.5),
                Duration = TimeSpan.FromSeconds(1),
                EasingFunction = new QuinticEase { EasingMode = EasingMode.EaseIn }
            };

            Show();
            await Task.Delay(500);
            ESP.PlaySound("app open");

            popupBox.BeginAnimation(MarginProperty, new ThicknessAnimation
            {
                To = new(0, 0, 0, 90),
                Duration = TimeSpan.FromSeconds(1.5),
                EasingFunction = new QuinticEase { EasingMode = EasingMode.EaseOut }
            });

            popupRotation.BeginAnimation(RotateTransform.AngleProperty, new DoubleAnimation
            {
                From = 0,
                To = 270,
                BeginTime = scaleOut.BeginTime,
                Duration = scaleOut.Duration,
                EasingFunction = scaleOut.EasingFunction
            });
            popupScale.BeginAnimation(ScaleTransform.ScaleXProperty, scaleOut);
            popupScale.BeginAnimation(ScaleTransform.ScaleYProperty, scaleOut);

            await Task.Delay(2500); // change as needed
            Close();
        }
    }
}
