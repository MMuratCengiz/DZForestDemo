using System.Runtime.CompilerServices;

namespace Application.Timing;

/// <summary>
/// Manages fixed timestep updates independent of frame rate.
/// Accumulates time and provides iteration count for fixed updates.
/// </summary>
public sealed class FixedTimestep
{
    private double _accumulator;
    private int _maxStepsPerFrame;

    /// <summary>The fixed delta time in seconds.</summary>
    public double FixedDeltaTime
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get;
        private set;
    }

    /// <summary>The target update rate in Hz.</summary>
    public double UpdateRate
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => FixedDeltaTime > 0 ? 1.0 / FixedDeltaTime : 0;
    }

    /// <summary>Maximum fixed steps per frame to prevent spiral of death.</summary>
    public int MaxStepsPerFrame
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _maxStepsPerFrame;
        set => _maxStepsPerFrame = Math.Max(1, value);
    }

    /// <summary>
    /// Creates a fixed timestep manager.
    /// </summary>
    /// <param name="updateRate">Target update rate in Hz (e.g., 60 for 60Hz).</param>
    /// <param name="maxStepsPerFrame">Maximum fixed steps per frame. Default is 8.</param>
    public FixedTimestep(double updateRate, int maxStepsPerFrame = 8)
    {
        SetUpdateRate(updateRate);
        _maxStepsPerFrame = Math.Max(1, maxStepsPerFrame);
    }

    /// <summary>
    /// Sets the target update rate.
    /// </summary>
    /// <param name="updateRate">Target update rate in Hz.</param>
    public void SetUpdateRate(double updateRate)
    {
        FixedDeltaTime = updateRate > 0 ? 1.0 / updateRate : 0;
    }

    /// <summary>
    /// Accumulates frame time and returns number of fixed steps to execute.
    /// </summary>
    /// <param name="deltaTime">Frame delta time in seconds.</param>
    /// <returns>Number of fixed update steps to execute this frame.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Accumulate(double deltaTime)
    {
        if (FixedDeltaTime <= 0)
        {
            return 0;
        }

        _accumulator += deltaTime;

        int steps = (int)(_accumulator / FixedDeltaTime);
        steps = Math.Min(steps, _maxStepsPerFrame);

        _accumulator -= steps * FixedDeltaTime;

        // Prevent accumulator from growing unbounded if we're always clamping
        if (_accumulator > FixedDeltaTime * _maxStepsPerFrame)
        {
            _accumulator = FixedDeltaTime;
        }

        return steps;
    }

    /// <summary>
    /// Resets the accumulator.
    /// </summary>
    public void Reset()
    {
        _accumulator = 0;
    }
}
