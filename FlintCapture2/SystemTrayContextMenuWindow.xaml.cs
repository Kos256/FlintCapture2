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
    /// Interaction logic for SystemTrayContextMenuWindow.xaml
    /// </summary>
    public partial class SystemTrayContextMenuWindow : Window
    {
        public MainWindow mainWin;
        public SystemTrayContextMenuWindow(MainWindow mainWin)
        {
            InitializeComponent();
            this.mainWin = mainWin;
        }
    }
}
