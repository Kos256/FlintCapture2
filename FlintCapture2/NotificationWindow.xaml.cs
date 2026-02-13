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

namespace FlintCapture2
{
    /// <summary>
    /// Interaction logic for NotificationWindow.xaml
    /// </summary>
    public partial class NotificationWindow : Window
    {
        private MainWindow mainWin;
        public string Timestamp;
        public string ScreenshotFilePath;
        public NotificationWindow(MainWindow mainWin, string timestamp, string ssfp)
        {
            InitializeComponent();
            this.mainWin = mainWin;
            Timestamp = timestamp;
            ScreenshotFilePath = ssfp;

            Show();
        }
    }
}
