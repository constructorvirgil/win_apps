using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Media;
using System.Windows.Threading;
using WinApps.MessageCenter.Core;

namespace WinApps.MessageCenter;

public partial class OverlayToastWindow : Window
{
    private readonly DispatcherTimer _tickTimer = new() { Interval = TimeSpan.FromMilliseconds(50) };
    private readonly TimeSpan _totalDuration;
    private TimeSpan _remaining;
    private DateTime _deadlineUtc;
    private bool _paused;
    private bool _closing;

    public OverlayToastWindow(OverlayNotification notification)
    {
        InitializeComponent();

        TitleText.Text = notification.Title;
        MessageText.Text = notification.Message;

        Opacity = 0;
        _totalDuration = TimeSpan.FromMilliseconds(Math.Max(800, notification.DurationMs));
        _remaining = _totalDuration;
        _tickTimer.Tick += (_, _) => Tick();

        Loaded += (_, _) =>
        {
            BeginFadeIn();
            StartOrResumeCountdown();
        };

        Closed += (_, _) =>
        {
            _tickTimer.Stop();
        };

        CloseButton.Click += (_, _) => BeginFadeOutAndClose();
        MouseLeftButtonDown += (_, _) => BeginFadeOutAndClose();

        Root.MouseEnter += (_, _) => PauseCountdown();
        Root.MouseLeave += (_, _) => StartOrResumeCountdown();
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
        _tickTimer.Stop();

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

    private void Tick()
    {
        if (_closing)
        {
            _tickTimer.Stop();
            return;
        }

        var remaining = _deadlineUtc - DateTime.UtcNow;
        if (remaining <= TimeSpan.Zero)
        {
            ProgressScale.ScaleX = 0;
            _tickTimer.Stop();
            BeginFadeOutAndClose();
            return;
        }

        var ratio = remaining.TotalMilliseconds / _totalDuration.TotalMilliseconds;
        ProgressScale.ScaleX = Math.Clamp(ratio, 0, 1);
    }

    private void PauseCountdown()
    {
        if (_closing || _paused)
            return;

        _paused = true;
        _remaining = _deadlineUtc - DateTime.UtcNow;
        if (_remaining < TimeSpan.Zero)
            _remaining = TimeSpan.Zero;

        _tickTimer.Stop();
    }

    private void StartOrResumeCountdown()
    {
        if (_closing)
            return;

        _paused = false;
        _deadlineUtc = DateTime.UtcNow + _remaining;
        Tick();
        _tickTimer.Start();
    }
}
