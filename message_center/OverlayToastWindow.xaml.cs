using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Media;
using System.Windows.Threading;
using WinApps.MessageCenter.Core;

namespace WinApps.MessageCenter;

public partial class OverlayToastWindow : Window
{
    private readonly DispatcherTimer _closeTimer = new();
    private readonly TimeSpan _duration;
    private bool _closing;

    public OverlayToastWindow(OverlayNotification notification)
    {
        InitializeComponent();

        TitleText.Text = notification.Title;
        MessageText.Text = notification.Message;

        Opacity = 0;
        _duration = TimeSpan.FromMilliseconds(Math.Max(800, notification.DurationMs));
        _closeTimer.Interval = _duration;
        _closeTimer.Tick += (_, _) =>
        {
            _closeTimer.Stop();
            BeginFadeOutAndClose();
        };

        Loaded += (_, _) =>
        {
            BeginFadeIn();
            BeginProgress();
            _closeTimer.Start();
        };

        Closed += (_, _) =>
        {
            _closeTimer.Stop();
        };

        CloseButton.Click += (_, _) => BeginFadeOutAndClose();
        MouseLeftButtonDown += (_, _) => BeginFadeOutAndClose();
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

        var slide = new DoubleAnimation(SlideTransform.X, 0, new Duration(TimeSpan.FromMilliseconds(220)))
        {
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
        };
        SlideTransform.BeginAnimation(TranslateTransform.XProperty, slide);
    }

    private void BeginFadeOutAndClose()
    {
        if (_closing)
            return;

        _closing = true;
        _closeTimer.Stop();
        ProgressScale.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleXProperty, null);

        var anim = new DoubleAnimation(Opacity, 0, new Duration(TimeSpan.FromMilliseconds(180)))
        {
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
        };
        anim.Completed += (_, _) => Close();
        BeginAnimation(OpacityProperty, anim);

        var slide = new DoubleAnimation(SlideTransform.X, 22, new Duration(TimeSpan.FromMilliseconds(180)))
        {
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
        };
        SlideTransform.BeginAnimation(TranslateTransform.XProperty, slide);
    }

    private void BeginProgress()
    {
        var anim = new DoubleAnimation(1, 0, new Duration(_duration))
        {
            EasingFunction = null
        };
        ProgressScale.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleXProperty, anim);
    }
}
