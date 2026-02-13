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
using System.Windows.Media.Media3D;
using System.Windows.Shapes;

namespace FlintCapture2
{
    /// <summary>
    /// Interaction logic for NotificationWindowImagePreview.xaml
    /// </summary>
    public partial class NotificationWindowImagePreview : Window
    {
        private NotificationWindow _notifWindow;
        public NotificationWindowImagePreview(NotificationWindow notificationWindowInput)
        {
            InitializeComponent();
            _notifWindow = notificationWindowInput;

            this.Width = SystemParameters.WorkArea.Width;
            this.Height = SystemParameters.PrimaryScreenHeight;

            AdjustAspectRatio();
        }

        private void Begin3DAnimation()
        {

        }

        private void AdjustAspectRatio()
        {
            double aspectRatio = SystemParameters.PrimaryScreenWidth / SystemParameters.PrimaryScreenHeight;

            var mesh = ((GeometryModel3D)((ModelVisual3D)viewport3D.Children[0]).Content).Geometry as MeshGeometry3D;
            mesh.Positions = new Point3DCollection
            {
                new Point3D(-aspectRatio, 1, 0), // Top-left
                new Point3D(aspectRatio, 1, 0),  // Top-right
                new Point3D(aspectRatio, -1, 0), // Bottom-right
                new Point3D(-aspectRatio, -1, 0) // Bottom-left
            };
        }

        private ImageSource ConvertFileToImageSource(string filePath)
        {
            BitmapImage bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.UriSource = new Uri(filePath);
            bitmapImage.EndInit();
            return bitmapImage;
        }
    }
}
