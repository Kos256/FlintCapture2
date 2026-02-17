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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace FlintCapture2.Scripts.Controls
{
    /// <summary>
    /// Interaction logic for KosToggleButton.xaml
    /// </summary>
    public partial class KosToggleButton : UserControl
    {
        public KosToggleButton()
        {
            InitializeComponent();
        }

        #region Styling brushes
        public Brush ToggleBackground
        {
            get => (Brush)GetValue(ToggleBackgroundProperty);
            set => SetValue(ToggleBackgroundProperty, value);
        }

        public static readonly DependencyProperty ToggleBackgroundProperty =
            DependencyProperty.Register(
                nameof(ToggleBackground),
                typeof(Brush),
                typeof(KosToggleButton),
                new PropertyMetadata(Brushes.Red));

        public Brush ToggleBorderBrush
        {
            get => (Brush)GetValue(ToggleBorderBrushProperty);
            set => SetValue(ToggleBorderBrushProperty, value);
        }

        public static readonly DependencyProperty ToggleBorderBrushProperty =
            DependencyProperty.Register(
                nameof(ToggleBorderBrush),
                typeof(Brush),
                typeof(KosToggleButton),
                new PropertyMetadata(Brushes.DarkRed));

        public Brush ToggleSliderBackground
        {
            get => (Brush)GetValue(ToggleSliderBackgroundProperty);
            set => SetValue(ToggleSliderBackgroundProperty, value);
        }

        public static readonly DependencyProperty ToggleSliderBackgroundProperty =
            DependencyProperty.Register(
                nameof(ToggleSliderBackground),
                typeof(Brush),
                typeof(KosToggleButton),
                new PropertyMetadata(Brushes.White));

        public Brush ToggleSliderBorderBrush
        {
            get => (Brush)GetValue(ToggleSliderBorderBrushProperty);
            set => SetValue(ToggleSliderBorderBrushProperty, value);
        }

        public static readonly DependencyProperty ToggleSliderBorderBrushProperty =
            DependencyProperty.Register(
                nameof(ToggleSliderBorderBrush),
                typeof(Brush),
                typeof(KosToggleButton),
                new PropertyMetadata(Brushes.Gray));

        #endregion

        public bool State
        {
            get => (bool)GetValue(StateProperty);
            set => SetValue(StateProperty, value);
        }

        public static readonly DependencyProperty StateProperty =
        DependencyProperty.Register(
            nameof(State),
            typeof(bool),
            typeof(KosToggleButton),
            new PropertyMetadata(false, OnStateChanged));
        private static void OnStateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (KosToggleButton)d;
            control.UpdateVisualState();
        }
        private void UpdateVisualState()
        {
            if (State)
            {
                // animate to green/right
            }
            else
            {
                // animate to red/left
            }
        }
        private void Grid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {

        }
    }
}
