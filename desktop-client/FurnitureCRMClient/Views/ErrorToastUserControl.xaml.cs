using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace FurnitureCRMClient.Views
{
    public partial class ErrorToastUserControl : UserControl
    {
        private readonly DispatcherTimer _timer;

        public ErrorToastUserControl()
        {
            InitializeComponent();

            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(4)
            };
            _timer.Tick += (s, e) => HideAnimated();
        }

        public void Show(string message, TimeSpan? duration = null)
        {
            if (duration.HasValue)
            {
                _timer.Interval = duration.Value;
            }
            else
            {
                _timer.Interval = TimeSpan.FromSeconds(4);
            }

            MessageTextBlock.Text = message;

            // Останавливаем предыдущие анимации
            ToastBorder.BeginAnimation(OpacityProperty, null);

            ToastBorder.Opacity = 0;
            ToastBorder.Visibility = Visibility.Visible;

            var fadeIn = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromMilliseconds(200)
            };
            ToastBorder.BeginAnimation(OpacityProperty, fadeIn);

            _timer.Stop();
            _timer.Start();
        }

        private void HideAnimated()
        {
            _timer.Stop();

            ToastBorder.BeginAnimation(OpacityProperty, null);

            var fadeOut = new DoubleAnimation
            {
                From = ToastBorder.Opacity,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(200)
            };

            fadeOut.Completed += (s, e) =>
            {
                ToastBorder.Visibility = Visibility.Collapsed;
            };

            ToastBorder.BeginAnimation(OpacityProperty, fadeOut);
        }

        public void HideInstant()
        {
            _timer.Stop();
            ToastBorder.BeginAnimation(OpacityProperty, null);
            ToastBorder.Opacity = 0;
            ToastBorder.Visibility = Visibility.Collapsed;
        }

        private void ToastBorder_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            HideInstant();
        }
    }
}


