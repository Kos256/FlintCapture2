using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using ESP = FlintCapture2.Scripts.EmbeddedSoundPlayer;

namespace FlintCapture2
{
    /// <summary>
    /// Interaction logic for UpdateCelebrationWindow.xaml
    /// </summary>
    public partial class UpdateCelebrationWindow : Window
    {
        MainWindow? mainWin;
        (Version Previous, Version Current) ReportedVersion;

        public UpdateCelebrationWindow(MainWindow mainWin, Version oldVersion, Version newVersion)
        {
            InitializeComponent();

            this.mainWin = mainWin;
            ReportedVersion.Previous = oldVersion;
            ReportedVersion.Current = newVersion;

            Width = SystemParameters.WorkArea.Width;
            Height = SystemParameters.WorkArea.Height;

            RootGrid.Opacity = 0;

            Loaded += UpdateCelebrationWindow_Loaded;
        }

        private void UpdateCelebrationWindow_Loaded(object sender, RoutedEventArgs e)
        {
            _ = TriggerAnimSequence();
        }

        private async Task TriggerAnimSequence()
        {
            await Task.Delay(2000);

            var celebrationSound = ESP.PlayTracked("update celebration");

            RootGrid.BeginAnimation(OpacityProperty, new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromSeconds(0.3),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
            });
        }
    }
}
