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

namespace FlintCapture2
{
    /// <summary>
    /// Interaction logic for SystemTrayContextMenuWindow.xaml
    /// </summary>
    public partial class SystemTrayContextMenuWindow : Window
    {
        public MainWindow mainWin;
        private Dictionary<string, Action> contextMenuActions;
        public SystemTrayContextMenuWindow(MainWindow mainWin)
        {
            InitializeComponent();
            this.mainWin = mainWin;
            Hide();

            contextMenuActions = new Dictionary<string, Action>
            {
                { "Open app", () => MIOpenApp() },
                { "Screenshot history", () => MIHistory() },
                { "Exit", () => MIExit() }
            };
        }

        public void ShowMenu()
        {
            Show();// leftoff: dont use Deactivated+= to hide context menu, hook onto left click, OnLeftClick => IsActivated? If so, then do nothing. If not, then Hide()
            Activate();
        }

        // MI<func> = Menu Item func
        private void MIOpenApp()
        {
            //mainWin._appGuiWindow.ResetWindowOpenFlags();
            //mainWin._appGuiWindow.RequestShowWindow();

            MenuItemClicked();
        }
        private void MIHistory()
        {
            //mainWin._appGuiWindow.AddWindowOpenFlag("history");
            //mainWin._appGuiWindow.RequestShowWindow();

            MenuItemClicked();
        }
        private void MIExit()
        {
            MenuItemClicked();
            App.Current.Shutdown();
        }
        private void MenuItemClicked()
        {
            Hide();
        }

        private void CtxMenuItem_MouseEnter(object sender, MouseEventArgs e)
        {
            AnimateMenuItem(sender, "text", true);
            AnimateMenuItem(sender, "bg", true);
        }
        private void CtxMenuItem_MouseLeave(object sender, MouseEventArgs e)
        {
            AnimateMenuItem(sender, "text", false);
            AnimateMenuItem(sender, "bg", false);
        }
        private void CtxMenuItem_MouseDown(object sender, MouseEventArgs e)
        {
            if (sender is Border b)
            {
                contextMenuActions[(b.Child as TextBlock).Text]();
            }
        }

        private void AnimateMenuItem(object sender, string property, bool state)
        {
            Thickness textDefaultMargin = new Thickness(5, 0, 0, 0);
            ThicknessAnimation textHoverAnim = new ThicknessAnimation
            {
                From = textDefaultMargin,
                To = new Thickness(8, 0, 0, 0),
                Duration = TimeSpan.FromSeconds(0.5),
                EasingFunction = new QuarticEase { EasingMode = EasingMode.EaseOut }
            };
            ThicknessAnimation textHoverEndedAnim = new ThicknessAnimation
            {
                From = new Thickness(8, 0, 0, 0),
                To = textDefaultMargin,
                Duration = TimeSpan.FromSeconds(0.5),
                EasingFunction = new QuarticEase { EasingMode = EasingMode.EaseOut }
            };

            Color bgDefaultColor = (Color)ColorConverter.ConvertFromString("#7F00B2FF");
            ColorAnimation bgHoverAnim = new ColorAnimation
            {
                From = bgDefaultColor,
                To = Color.FromArgb(0xFF, 0x00, 0xC8, 0xFF),
                Duration = TimeSpan.FromSeconds(0.5),
                EasingFunction = new QuarticEase { EasingMode = EasingMode.EaseOut }
            };
            ColorAnimation bgHoverEndedAnim = new ColorAnimation
            {
                From = Color.FromArgb(0xFF, 0x00, 0xC8, 0xFF),
                To = bgDefaultColor,
                Duration = TimeSpan.FromSeconds(0.5),
                EasingFunction = new QuarticEase { EasingMode = EasingMode.EaseOut }
            };

            Color textDefaultColor = Colors.White;
            ColorAnimation textColorHoverAnim = new ColorAnimation
            {
                From = textDefaultColor,
                To = Colors.Black,
                Duration = TimeSpan.FromSeconds(0.3),
                EasingFunction = new QuarticEase { EasingMode = EasingMode.EaseOut }
            };
            ColorAnimation textColorHoverEndedAnim = new ColorAnimation
            {
                From = Colors.Black,
                To = textDefaultColor,
                Duration = TimeSpan.FromSeconds(0.3),
                EasingFunction = new QuarticEase { EasingMode = EasingMode.EaseOut }
            };

            SolidColorBrush menuItemBgBrush = new SolidColorBrush(bgDefaultColor);
            SolidColorBrush menuItemTextBrush = new SolidColorBrush(textDefaultColor);

            if (sender is Border b)
            {
                b.Background = menuItemBgBrush;
                (b.Child as TextBlock).Foreground = menuItemTextBrush;

                if (property == "bg")
                {
                    menuItemBgBrush.BeginAnimation(SolidColorBrush.ColorProperty, state ? bgHoverAnim : bgHoverEndedAnim);
                    menuItemTextBrush.BeginAnimation(SolidColorBrush.ColorProperty, state ? textColorHoverAnim : textColorHoverEndedAnim);
                }

                if (property == "text")
                    (b.Child as TextBlock).BeginAnimation(TextBlock.MarginProperty, state ? textHoverAnim : textHoverEndedAnim);
            }
        }
    }
}
