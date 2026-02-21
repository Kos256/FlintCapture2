using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Shapes;
using ESP = FlintCapture2.Scripts.EmbeddedSoundPlayer;
using PathIO = System.IO.Path;

namespace FlintCapture2
{
    /// <summary>
    /// Interaction logic for ImageEditWindow.xaml
    /// </summary>
    public partial class ImageEditWindow : Window
    {
        /// <summary>
        /// Screenshot File Path
        /// </summary>
        public string ScreenshotFilePath = "";
        public BitmapImage _ssImage;
        public BitmapImage _ssEditedImage;
        private MainWindow mainWin;

        private SolidColorBrush cropBoundsFillBrush;

        public ImageEditWindow(string ssfpInput, MainWindow mainWin)
        {
            InitializeComponent();
            Closing += ImageEditWindow_Closing;
            winbarCollider.MouseLeftButtonDown += (s, e) => DragMove();

            ScreenshotFilePath = ssfpInput;
            Title = $"New Screenshot {PathIO.GetFileName(ScreenshotFilePath).Replace("copied_image_", "")}";

            Activated += (s, e) => windowFocusBorder.Opacity = 1;
            Deactivated += (s, e) => windowFocusBorder.Opacity = 0;

            this.mainWin = mainWin;

            _ssImage = new BitmapImage(new Uri(ScreenshotFilePath));
            _ssEditedImage = _ssImage.Clone();
            imgPreview.Source = _ssImage;

            cropBoundsFillBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#7F000000"));
            cropOverlayPath.Fill = cropBoundsFillBrush;

            const double MIN_CROPSIZE = 10;

            tlHandle.DragDelta += (s, e) =>
            {
                Rect imgBounds = GetImageBoundsInCropCanvas();

                double left = Canvas.GetLeft(selectionRect);
                double top = Canvas.GetTop(selectionRect);

                double right = left + selectionRect.Width;
                double bottom = top + selectionRect.Height;

                double newLeft = left + e.HorizontalChange;
                double newTop = top + e.VerticalChange;

                // Clamp to image bounds
                if (newLeft < imgBounds.Left)
                    newLeft = imgBounds.Left;

                if (newTop < imgBounds.Top)
                    newTop = imgBounds.Top;

                // Prevent crossing fixed bottom-right
                if (right - newLeft < MIN_CROPSIZE)
                    newLeft = right - MIN_CROPSIZE;

                if (bottom - newTop < MIN_CROPSIZE)
                    newTop = bottom - MIN_CROPSIZE;

                selectionRect.Width = right - newLeft;
                selectionRect.Height = bottom - newTop;

                Canvas.SetLeft(selectionRect, newLeft);
                Canvas.SetTop(selectionRect, newTop);

                UpdateHandles();
                UpdateCropOverlay();
            };
            brHandle.DragDelta += (s, e) =>
            {
                Rect imgBounds = GetImageBoundsInCropCanvas();

                double left = Canvas.GetLeft(selectionRect);
                double top = Canvas.GetTop(selectionRect);

                // Current mouse-adjusted bottom-right position
                double newRight = left + selectionRect.Width + e.HorizontalChange;
                double newBottom = top + selectionRect.Height + e.VerticalChange;

                // Clamp to image bounds
                if (newRight > imgBounds.Right)
                    newRight = imgBounds.Right;

                if (newBottom > imgBounds.Bottom)
                    newBottom = imgBounds.Bottom;

                double newWidth = newRight - left;
                double newHeight = newBottom - top;

                if (newWidth < MIN_CROPSIZE) newWidth = MIN_CROPSIZE;
                if (newHeight < MIN_CROPSIZE) newHeight = MIN_CROPSIZE;

                selectionRect.Width = newWidth;
                selectionRect.Height = newHeight;

                UpdateHandles();
                UpdateCropOverlay();
            };

            RootGrid.Opacity = 0;
            imgPreview.Opacity = 0;
            Loaded += ImageEditWindow_Loaded;
        }

        private async void ImageEditWindow_Loaded(object sender, RoutedEventArgs e)
        {
            statusTextRun.Text = "Window loaded. Waiting for image...";
            ESP.PlaySound("editor open");
            //annotationCanvas.Width = imgPreview.Width;
            annotationCanvas.Visibility = Visibility.Visible;
            cropCanvas.Visibility = Visibility.Hidden;

            RootGrid.BeginAnimation(OpacityProperty, new DoubleAnimation
            {
                To = 1,
                Duration = TimeSpan.FromSeconds(0.5),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            });
            BeginAnimation(TopProperty, new DoubleAnimation
            {
                From = Top + 100,
                To = Top,
                Duration = TimeSpan.FromSeconds(0.5),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            });
            imgPreview.BeginAnimation(OpacityProperty, new DoubleAnimation
            {
                To = 1,
                BeginTime = TimeSpan.FromSeconds(0.05),
                Duration = TimeSpan.FromSeconds(0.7),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
            });

            await Task.Delay(500);
            statusTextRun.Text = "Ready!";
        }

        bool _savedWork = false;
        bool _intentionallyClosed = false;
        bool _isAnimatingClose = false;
        private void ImageEditWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            if (_isAnimatingClose)
                return;

            if (!_savedWork)
            {
                e.Cancel = true;

                var result = MessageBox.Show(
                    "Are you sure you want to exit? You have unsaved edits to this image.",
                    "Unsaved edits",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result != MessageBoxResult.Yes)
                    return;
            }

            e.Cancel = true;
            _isAnimatingClose = true;

            ESP.PlaySound("editor close");

            var fadeAnim = new DoubleAnimation
            {
                To = 0,
                Duration = TimeSpan.FromSeconds(0.5),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
            };

            fadeAnim.Completed += (s, _) =>
            {
                Close(); // SAFE now
            };

            RootGrid.BeginAnimation(OpacityProperty, fadeAnim);

            var rootGridScale = new ScaleTransform(1, 1);
            rootGridScale.CenterX = Width / 2;
            rootGridScale.CenterY = Height / 2;
            var rootGridRotation = new RotateTransform(0);
            rootGridRotation.CenterX = Width / 2;
            rootGridRotation.CenterY = Height / 2;
            var rootGridTransform = new TransformGroup();
            rootGridTransform.Children.Add(rootGridScale);
            rootGridTransform.Children.Add(rootGridRotation);
            RootGrid.RenderTransform = rootGridTransform;

            var scaleAnim = new DoubleAnimation
            {
                From = 1,
                To = 0,
                Duration = TimeSpan.FromSeconds(0.5),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
            };
            var rotAnim = new DoubleAnimation
            {
                From = 0,
                To = 90,
                Duration = TimeSpan.FromSeconds(0.5),
                EasingFunction = new QuarticEase { EasingMode = EasingMode.EaseIn }
            };

            rootGridScale.BeginAnimation(ScaleTransform.ScaleXProperty, scaleAnim);
            rootGridScale.BeginAnimation(ScaleTransform.ScaleYProperty, scaleAnim);
            rootGridRotation.BeginAnimation(RotateTransform.AngleProperty, rotAnim);
        }

        private bool windowExists = false;

        private void winCtrlClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
            //this.Hide();
            windowExists = false;
        }
        private void winCtrlMinimize_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        public void RequestClose()
        {
            this.Close();
        }


        SolidColorBrush borderBrushFlashing = Brushes.Yellow.Clone();
        private async void menuBtnClicked(object sender, RoutedEventArgs e)
        {
            Button btn = (Button)sender;

            loadingImgText.Visibility = Visibility.Hidden;

            if (btn.Name.Contains("crop"))
            {
                // leftoff: make a dedicated "cancel crop" button
                if (((string)btn.Content).ToLower().Contains("crop"))
                {
                    annotationCanvas.Visibility = Visibility.Hidden;
                    cropCanvas.Visibility = Visibility.Visible;

                    cropBtn.Content = "Done";
                    cropBtn.BorderBrush = borderBrushFlashing;
                    borderBrushFlashing.BeginAnimation(SolidColorBrush.ColorProperty, new ColorAnimation
                    {
                        From = Colors.Yellow,
                        To = Colors.DarkGoldenrod,
                        Duration = TimeSpan.FromSeconds(0.25),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut },
                        AutoReverse = true,
                        RepeatBehavior = RepeatBehavior.Forever,
                    });

                    cropBoundsFillBrush.BeginAnimation(SolidColorBrush.ColorProperty, new ColorAnimation
                    {
                        From = Color.FromArgb(0, 0, 0, 0),
                        To = (Color)ColorConverter.ConvertFromString("#7F000000"),
                        Duration = TimeSpan.FromSeconds(0.25),
                        EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
                    });

                    copyBtn.Content = "Copy Region";
                    UpdateCropOverlay();

                    ESP.PlaySound("crop start");
                    return;
                }
                if (((string)btn.Content).ToLower().Contains("done"))
                {
                    CropImageToSelection();

                    annotationCanvas.Visibility = Visibility.Visible;
                    cropCanvas.Visibility = Visibility.Hidden;

                    cropBtn.Content = "Crop";
                    cropBtn.ClearValue(Button.BorderBrushProperty);

                    copyBtn.Content = "Copy Image";
                    return;
                }
            }
            if (btn.Name.Contains("copy"))
            {
                bool regionMode = false;
                //if (
                //    (((string)btn.Content).ToLower() != "copy image")
                //    ||
                //    (((string)btn.Content).ToLower() != "copy region")) return;

                if (((string)btn.Content) != "Copy Image")
                {
                    regionMode = true;
                    if (((string)btn.Content) != "Copy Region")
                    {
                        return;
                    }
                }

                string originalBtnContent = (string)copyBtn.Content;

                copyBtn.Content = "Copying...";
                await Task.Delay(100);
                
                try
                {
                    if (!regionMode) // normal crop
                    {
                        Clipboard.SetImage(_ssEditedImage);
                        //throw new Exception(); // test exception
                    }
                    else // regional crop
                    {
                        Clipboard.SetImage(CropImageToSelection(false) ?? _ssEditedImage);
                        //throw new Exception(); // test exception
                    }
                    copyBtn.Content = "Copied!";
                    copyBtn.Background = new SolidColorBrush(Color.FromRgb(0, 50, 0));
                    copyBtn.BorderBrush = Brushes.Lime;
                }
                catch (Exception ex)
                {
                    copyBtn.Content = "Failed to copy :(";
                    copyBtn.Background = new SolidColorBrush(Color.FromRgb(50, 0, 0));
                    copyBtn.BorderBrush = Brushes.Red;
                    MessageBox.Show($"{ex.Message}", "Error copying image", MessageBoxButton.OK, MessageBoxImage.Error);
                }

                await Task.Delay(1000);
                copyBtn.ClearValue(Border.BackgroundProperty);
                copyBtn.ClearValue(Border.BorderBrushProperty);
                copyBtn.Content = originalBtnContent;
            }
            if (btn.Name.Contains("save"))
            {
                // Create encoder
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(_ssEditedImage));

                string dir = PathIO.GetDirectoryName(ScreenshotFilePath) ?? ".";
                dir = PathIO.Combine(dir, "..", "Saved Edits");
                string fileName = $"FlintCapture_{PathIO.GetFileName(ScreenshotFilePath).Replace("copied_image_", "")}";
                string filePath = PathIO.Combine(dir, fileName);

                // Write to file
                using (var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                {
                    encoder.Save(stream);
                }

                // leftoff: make a dedicated button for opening the image in file explorer instead of binding it to the save button
                mainWin.ShowSavedScreenshotsDirectoryFileExplorer(filePath);
                _savedWork = true;
                RequestClose();
            }
        }
        #region Cropping logic
        private void UpdateHandles()
        {
            double left = Canvas.GetLeft(selectionRect);
            double top = Canvas.GetTop(selectionRect);

            double right = left + selectionRect.Width;
            double bottom = top + selectionRect.Height;

            const double HANDLE_SIZE = 20;
            const double HALF = HANDLE_SIZE / 2;

            Canvas.SetLeft(tlHandle, left - HALF);
            Canvas.SetTop(tlHandle, top - HALF);

            Canvas.SetLeft(trHandle, right - HALF);
            Canvas.SetTop(trHandle, top - HALF);

            Canvas.SetLeft(blHandle, left - HALF);
            Canvas.SetTop(blHandle, bottom - HALF);

            Canvas.SetLeft(brHandle, right - HALF);
            Canvas.SetTop(brHandle, bottom - HALF);

        }
        private void UpdateCropOverlay()
        {
            if (imgPreview.ActualWidth <= 0 || imgPreview.ActualHeight <= 0 || _ssImage == null)
                return;

            Rect imgBounds = GetImageBoundsInCropCanvas();

            // Canvas space (for overlay drawing)
            double canvasLeft = Canvas.GetLeft(selectionRect);
            double canvasTop = Canvas.GetTop(selectionRect);
            double width = selectionRect.Width;
            double height = selectionRect.Height;

            // Image-relative space (for pixel math)
            double imageLeft = canvasLeft - imgBounds.X;
            double imageTop = canvasTop - imgBounds.Y;

            double scaleX = _ssImage.PixelWidth / imgPreview.ActualWidth;
            double scaleY = _ssImage.PixelHeight / imgPreview.ActualHeight;

            int imageX = (int)Math.Round(imageLeft * scaleX);
            int imageY = (int)Math.Round(imageTop * scaleY);
            int imageW = (int)Math.Round(width * scaleX);
            int imageH = (int)Math.Round(height * scaleY);

            statusTextRun.Text =
                $"Selected {imageW} x {imageH} at ({imageX}, {imageY}).";

            var fullRect = new RectangleGeometry(
                new Rect(0, 0, cropCanvas.ActualWidth, cropCanvas.ActualHeight));

            // IMPORTANT: use canvas coordinates here
            var selection = new RectangleGeometry(
                new Rect(canvasLeft, canvasTop, width, height));

            var mask = new CombinedGeometry(
                GeometryCombineMode.Exclude, fullRect, selection);

            cropOverlayPath.Data = mask;
        }
        private BitmapImage? CropImageToSelection(bool applyCrop = true)
        {
            if (_ssImage == null) return null;

            Rect imgBounds = GetImageBoundsInCropCanvas();

            double left = Canvas.GetLeft(selectionRect) - imgBounds.X;
            double top = Canvas.GetTop(selectionRect) - imgBounds.Y;
            double width = selectionRect.Width;
            double height = selectionRect.Height;

            double scaleX = _ssImage.PixelWidth / imgPreview.ActualWidth;
            double scaleY = _ssImage.PixelHeight / imgPreview.ActualHeight;

            int x = (int)(left * scaleX);
            int y = (int)(top * scaleY);
            int w = (int)(width * scaleX);
            int h = (int)(height * scaleY);

            // Initial bounds clamp
            x = Math.Max(0, Math.Min(x, _ssImage.PixelWidth - 1));
            y = Math.Max(0, Math.Min(y, _ssImage.PixelHeight - 1));
            if (x + w > _ssImage.PixelWidth) w = _ssImage.PixelWidth - x;
            if (y + h > _ssImage.PixelHeight) h = _ssImage.PixelHeight - y;

            const int ABSOLUTE_MIN_LIMIT = 1;
            //byte cropSizeLimitClamped = (byte)((((w <= ABSOLUTE_MIN_LIMIT) ? 1 : 0) << 1) | ((h <= ABSOLUTE_MIN_LIMIT) ? 1 : 0)); // 0bAB A: Width B: Height
            byte cropSizeLimitClamped = 0b00; // more readable version below:
            if (w <= ABSOLUTE_MIN_LIMIT) cropSizeLimitClamped |= 0b10; // width flag
            if (h <= ABSOLUTE_MIN_LIMIT) cropSizeLimitClamped |= 0b01; // height flag

            w = Math.Max(w, ABSOLUTE_MIN_LIMIT);
            h = Math.Max(h, ABSOLUTE_MIN_LIMIT);

            if (x + w > _ssImage.PixelWidth)
                w = _ssImage.PixelWidth - x;

            if (y + h > _ssImage.PixelHeight)
                h = _ssImage.PixelHeight - y;

            if (w <= 0 || h <= 0)
            {
                if (applyCrop)
                    statusTextRun.Text = "Crop safety check shows bounding is zero or negative?";
                return null;
            }

            try
            {
                var cb = new CroppedBitmap(_ssImage, new Int32Rect(x, y, w, h));

                BitmapImage bitmap;
                using (var stream = new MemoryStream())
                {
                    var encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(cb));
                    encoder.Save(stream);

                    bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.StreamSource = new MemoryStream(stream.ToArray());
                    bitmap.EndInit();
                    bitmap.Freeze();
                }

                if (applyCrop)
                {
                    _ssEditedImage = bitmap;
                    _ssImage = bitmap;
                    imgPreview.Source = bitmap;

                    if (cropSizeLimitClamped > 0)
                    {
                        string clampedDimension = cropSizeLimitClamped switch
                        {
                            0b10 => "width",
                            0b01 => "height",
                            0b11 => "width and height",
                            _ => ""
                        };
                        statusTextRun.Text = $"Crop {clampedDimension} was clamped to minimum limit. ({w} x {h})";
                    }
                    else
                        statusTextRun.Text = $"Image cropped to {w} x {h}.";

                    ESP.PlaySound(cropSizeLimitClamped > 0 ? "crop end clamp" : "crop end alt");
                }

                return bitmap;
            }
            catch (Exception ex)
            {
                if (applyCrop)
                {
                    MessageBox.Show($"{ex.Message}", "Exception while cropping :(",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    _intentionallyClosed = true;
                    App.Current.Shutdown();
                }

                return null;
            }
        }
        private Rect GetImageBoundsInCropCanvas()
        {
            GeneralTransform transform = imgPreview.TransformToVisual(cropCanvas);
            Point topLeft = transform.Transform(new Point(0, 0));

            return new Rect(
                topLeft.X,
                topLeft.Y,
                imgPreview.ActualWidth,
                imgPreview.ActualHeight);
        }
        #endregion
    }
}
