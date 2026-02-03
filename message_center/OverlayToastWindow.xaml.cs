using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using WinApps.MessageCenter.Core;

namespace WinApps.MessageCenter;

public partial class OverlayToastWindow : Window
{
    private readonly DispatcherTimer _closeTimer = new();

    public OverlayToastWindow(OverlayNotification notification)
    {
        InitializeComponent();

        TitleText.Text = notification.Title;
        MessageText.Text = notification.Message;

        Opacity = 0;
        _closeTimer.Interval = TimeSpan.FromMilliseconds(Math.Max(800, notification.DurationMs));
        _closeTimer.Tick += (_, _) =>
        {
            _closeTimer.Stop();
            BeginFadeOutAndClose();
        };

        Loaded += (_, _) =>
        {
            BeginFadeIn();
            _closeTimer.Start();
        };

        CloseButton.Click += (_, _) => Close();
        MouseLeftButtonDown += (_, _) => Close();
    }

    public void SetBounds(double left, double top, double width, double height)
    {
        Left = left;
        Top = top;
        Width = width;
        Height = height;
    }

    private void BeginFadeIn()
    {
        var anim = new DoubleAnimation(0, 1, new Duration(TimeSpan.FromMilliseconds(160)))
        {
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
        };
        BeginAnimation(OpacityProperty, anim);
    }

    private void BeginFadeOutAndClose()
    {
        var anim = new DoubleAnimation(Opacity, 0, new Duration(TimeSpan.FromMilliseconds(180)))
        {
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
        };
        anim.Completed += (_, _) => Close();
        BeginAnimation(OpacityProperty, anim);
    }
}
