using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;

namespace NiziKit.Editor.Views;

public class SmoothScrollViewer : ScrollViewer
{
    protected override Type StyleKeyOverride => typeof(ScrollViewer);

    private double _targetOffsetY;
    private bool _isAnimating;
    private DispatcherTimer? _timer;
    private const double ScrollStep = 50.0;
    private const double LerpSpeed = 0.25;
    private const double Threshold = 0.5;

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        var maxY = Extent.Height - Viewport.Height;
        if (maxY <= 0)
        {
            base.OnPointerWheelChanged(e);
            return;
        }

        e.Handled = true;

        if (!_isAnimating)
        {
            _targetOffsetY = Offset.Y;
        }

        _targetOffsetY = Math.Clamp(_targetOffsetY - e.Delta.Y * ScrollStep, 0, maxY);

        var diff = _targetOffsetY - Offset.Y;
        var step = Math.Abs(diff) < Threshold ? diff : diff * LerpSpeed;
        Offset = new Vector(Offset.X, Offset.Y + step);

        if (!_isAnimating && Math.Abs(_targetOffsetY - Offset.Y) > Threshold)
        {
            _isAnimating = true;
            _timer ??= new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
            _timer.Tick += OnTick;
            _timer.Start();
        }
    }

    private void OnTick(object? sender, EventArgs e)
    {
        var diff = _targetOffsetY - Offset.Y;
        if (Math.Abs(diff) < Threshold)
        {
            Offset = new Vector(Offset.X, _targetOffsetY);
            StopAnimation();
            return;
        }
        Offset = new Vector(Offset.X, Offset.Y + diff * LerpSpeed);
    }

    private void StopAnimation()
    {
        _isAnimating = false;
        if (_timer != null)
        {
            _timer.Tick -= OnTick;
            _timer.Stop();
        }
    }

    public void ScrollToTop()
    {
        _targetOffsetY = 0;
        StopAnimation();
        Offset = new Vector(Offset.X, 0);
    }
}
