using System.Runtime.CompilerServices;

namespace DenOfIz.World.Application.Timing;

public sealed class FixedTimestep
{
    private double _accumulator;
    private int _maxStepsPerFrame;

    public FixedTimestep(double updateRate, int maxStepsPerFrame = 8)
    {
        SetUpdateRate(updateRate);
        _maxStepsPerFrame = Math.Max(1, maxStepsPerFrame);
    }

    public double FixedDeltaTime
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get;
        private set;
    }

    public double UpdateRate
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => FixedDeltaTime > 0 ? 1.0 / FixedDeltaTime : 0;
    }

    public int MaxStepsPerFrame
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _maxStepsPerFrame;
        set => _maxStepsPerFrame = Math.Max(1, value);
    }

    private void SetUpdateRate(double updateRate)
    {
        FixedDeltaTime = updateRate > 0 ? 1.0 / updateRate : 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Accumulate(double deltaTime)
    {
        if (FixedDeltaTime <= 0)
        {
            return 0;
        }

        _accumulator += deltaTime;

        var steps = (int)(_accumulator / FixedDeltaTime);
        steps = Math.Min(steps, _maxStepsPerFrame);

        _accumulator -= steps * FixedDeltaTime;
        if (_accumulator > FixedDeltaTime * _maxStepsPerFrame)
        {
            _accumulator = FixedDeltaTime;
        }

        return steps;
    }

    public void Reset()
    {
        _accumulator = 0;
    }
}