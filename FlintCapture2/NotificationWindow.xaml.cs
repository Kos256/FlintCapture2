using FlintCapture2.Scripts;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using System.Windows.Threading;
using ESP = FlintCapture2.Scripts.EmbeddedSoundPlayer;
using PathIO = System.IO.Path;

namespace FlintCapture2
{
    /// <summary>
    /// Interaction logic for NotificationWindow.xaml
    /// </summary>
    public partial class NotificationWindow : Window
    {
        public bool _intentionallyClosed = false;
        public bool _dismissed = false;

        private MainWindow mainWin;
        private ScreenshotHandler ssHandler;

        /// <summary>
        /// The timestamp that this notification's corresponding screenshot was taken. This variable was formerly known as _windowID in FlintCapture1, so its usage here in FlintCapture2 has some ID like code...don't blame me lol.
        /// </summary>
        public string Timestamp; // formerly known as _windowID in FlintCapture1
        public string ScreenshotFilePath;
        public string ScreenshotFileName { get; private set; } = "";

        public NotificationWindow(MainWindow mainWin, ScreenshotHandler ssHandler, string timestamp, string ssfp)
        {
            InitializeComponent();
            this.mainWin = mainWin;
            this.ssHandler = ssHandler;
            Timestamp = timestamp;
            ScreenshotFilePath = ssfp;
            ShowActivated = false;

            MainGrid.Margin = new(0, 320, 0, 0);
            Left = SystemParameters.WorkArea.Width /*- 20*/;
            Top = SystemParameters.WorkArea.Height - Height - 20;
            Closing += ApplicationWantsToClose;
            windowClickColliderRect.MouseEnter += NotificationHovered;
            windowClickColliderRect.MouseLeftButtonDown += NotificationPressed;
            dismissBtn.Click += DismissBtn_Click;
            dismissAllBtn.Click += DismissAllBtn_Click;

            Title = Title.Replace("<id>", Timestamp);

            imagePreview.Background = new ImageBrush
            {
                ImageSource = new BitmapImage(new Uri(ScreenshotFilePath)),
                Stretch = Stretch.Uniform
            };

            Show(); // todo: migrate this Show() into PlayNotifyAnim(true) to decrease windows shown when multiple notifications are in the queue
        }

        public void RequestClose()
        {
            _intentionallyClosed = true;
            this.Close();
        }
        private void ApplicationWantsToClose(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!_intentionallyClosed) e.Cancel = true;
        }

        public void StartSequences()
        {
            WaitAndBegin();
        }

        private async void WaitAndBegin()
        {
            Task.Delay(50).Wait(); // waits 50ms for queue list to update because i cannot figure out async sequencing between different instances
            dismissAllBtn.Visibility = Visibility.Hidden;
            if (ssHandler.notificationWindowQueue[0] == this)
            {
                PlayNotifyAnim(true);
            }
            else
            {
                //MessageBox.Show($"Notif {_windowID} waiting in queue");
                WaitForTurnInQueue(); // default ms: 100
            }
        }

        private void WaitForTurnInQueue(double loopIntervalMS = 100)
        {
            DispatcherTimer checkMyStatusInQueue = new DispatcherTimer();
            checkMyStatusInQueue.Interval = TimeSpan.FromMilliseconds(loopIntervalMS);
            checkMyStatusInQueue.Tick += (s, e) =>
            {
                if (ssHandler.notificationWindowQueue.Contains(this))
                {
                    if (ssHandler.notificationWindowQueue[0] == this)
                    {
                        // I'm first in the queue now!
                        checkMyStatusInQueue.Stop();

                        (dismissAllBtn.Content as TextBlock).Text = $"Dismiss All [{ssHandler.notificationWindowQueue.Count} left]";
                        dismissAllBtn.Visibility = (ssHandler.notificationWindowQueue.Count > 1) ? Visibility.Visible : Visibility.Hidden;

                        PlayNotifyAnim(true);
                    }
                }
                else
                {
                    // I was removed from the queue while waiting — rage quit!
                    checkMyStatusInQueue.Stop();
                    RequestClose();
                    // also remove myself from notif queue
                }
            };
            checkMyStatusInQueue.Start();
        }

        private void DismissAllBtn_Click(object sender, RoutedEventArgs e)
        {
            /* // legacy method:
                while (true)
                {
                    if (_mw.notificationWindowQueue[0] != this) _mw.notificationWindowQueue.RemoveAt(0);
                    else _mw.notificationWindowQueue.RemoveAt(1);
                }
                _dismissed = true;
                PlayNotifyAnim(false);
            */

            // todo: uncertain...test it out: see if this method to remove all from list except this one works stable enough? this method makes use of stuff inside WaitForQueue() where it uses .Contains() before going onto checking if item[0] == this...
            ssHandler.notificationWindowQueue =
                ssHandler.notificationWindowQueue 
                .Where(window => window.Timestamp == this.Timestamp) // selectively clear the list
                .ToList();

            _dismissed = true;
            PlayNotifyAnim(false);
            ESP.PlaySound("dismiss all");
        }
        private void DismissBtn_Click(object sender, RoutedEventArgs e)
        {
            _dismissed = true;
            PlayNotifyAnim(false);
            ESP.PlaySound("dismiss");
        }
        private bool alreadyHovered = false;
        private void NotificationHovered(object sender, MouseEventArgs e)
        {
            if (true)
            {
                if (!alreadyHovered)
                {
                    alreadyHovered = true;
                    ESP.PlaySound("hover");
                    MainGrid.BeginAnimation(MarginProperty, mainGridMarginAnim);
                }
            }
            else
            {
                NotificationWindowImagePreview previewWindow = new NotificationWindowImagePreview(this);
                notificationStoryboard.Pause();
            }
        }
        private void NotificationPressed(object sender, MouseButtonEventArgs e)
        {
            _dismissed = true;
            PlayNotifyAnim(false);
            //Process.Start("explorer.exe", ScreenshotFilePath);
            ImageEditWindow window = new(ScreenshotFilePath, mainWin);
            window.Show();
        }

        #region notification animation bits
        private ElasticEase elOut = new ElasticEase
        {
            EasingMode = EasingMode.EaseOut,
            Oscillations = 6,
            Springiness = 15
        };
        private CubicEase cuIn = new CubicEase
        {
            EasingMode = EasingMode.EaseIn
        };
        private ThicknessAnimation? mainGridMarginAnim;
        Storyboard? notificationStoryboard;
        private void PlayNotifyAnim(bool entry)
        {
            if (notificationStoryboard == null) notificationStoryboard = new Storyboard();

            mainGridMarginAnim = new ThicknessAnimation
            {
                From = MainGrid.Margin,
                To = new Thickness(0, 100, 0, 0),
                /*BeginTime = TimeSpan.FromSeconds(1),*/
                Duration = TimeSpan.FromSeconds(2.5),
                EasingFunction = elOut
            };

            DoubleAnimation notifyAnimEnter = new DoubleAnimation
            {
                From = SystemParameters.WorkArea.Width,
                To = SystemParameters.WorkArea.Width - this.Width - 20,
                Duration = TimeSpan.FromSeconds(2),
                EasingFunction = elOut
            };
            DoubleAnimation notifyAnimExit = new DoubleAnimation
            {
                From = SystemParameters.WorkArea.Width - this.Width - 20,
                To = SystemParameters.WorkArea.Width,
                Duration = TimeSpan.FromSeconds(0.5),
                EasingFunction = cuIn
            };

            int notificationLifespan = 5;
            ThicknessAnimation timer = new ThicknessAnimation
            {
                From = new Thickness(0, 0, 0, 0),
                To = new Thickness(0, 0, timerGrid.Width, 0),
                Duration = TimeSpan.FromSeconds(notificationLifespan)
            };
            DoubleAnimation timerWidth = new DoubleAnimation
            {
                From = timerRect.Width,
                To = 0,
                Duration = TimeSpan.FromSeconds(notificationLifespan)
            };
            ColorAnimation timerColor = new ColorAnimation
            {
                From = Colors.White,
                To = Color.FromRgb(0, 0xFF, 0),
                Duration = TimeSpan.FromSeconds(0.3)
            };

            ThicknessAnimation mainGridMargin = new ThicknessAnimation
            {
                From = MainGrid.Margin,
                To = new Thickness(0, 100, 0, 0),
                /*BeginTime = TimeSpan.FromSeconds(1),*/
                Duration = TimeSpan.FromSeconds(2.5),
                EasingFunction = elOut
            };

            timerWidth.Completed += (s, e) =>
            {
                PlayNotifyAnim(false);
                windowClickColliderRect.Visibility = Visibility.Hidden; // lazy fix so that user cant click while exit animation is playing
            };

            notifyAnimExit.Completed += (s, e) =>
            {
                foreach (NotificationWindow i in ssHandler.notificationWindowQueue)
                {
                    if (ScreenshotFilePath == i.ScreenshotFilePath)
                    {
                        ssHandler.notificationWindowQueue.Remove(i);
                        break;
                    }
                }

                RequestClose();
            };

            notificationStoryboard.Children.Clear();

            var timerAnimation = timerWidth;
            var timerPropertyToAnimate = Rectangle.WidthProperty;
            if (entry)
            {
                AddToStoryboard(notificationStoryboard, timerAnimation, timerRect, new PropertyPath(timerPropertyToAnimate));
                AddToStoryboard(notificationStoryboard, notifyAnimEnter, this, new PropertyPath(Window.LeftProperty));
                this.Show();

                //PlaySound("Notification.Default", IntPtr.Zero, SND_ALIAS | SND_ASYNC | SND_APPLICATION);
                ESP.PlaySound("screenshot");
                notificationStoryboard.Begin();
            }
            else
            {
                AddToStoryboard(notificationStoryboard, notifyAnimExit, this, new PropertyPath(Window.LeftProperty));

                SolidColorBrush timerRectFill = new SolidColorBrush(Colors.White);
                timerRect.Fill = timerRectFill;

                notificationStoryboard.Begin();
            }

        }

        private void AddToStoryboard(Storyboard sbd, DependencyObject anim, DependencyObject item, PropertyPath property)
        {
            Storyboard.SetTarget(anim, item);
            Storyboard.SetTargetProperty(anim, property);
            sbd.Children.Add((Timeline)anim);
        }
        #endregion
    }
}
