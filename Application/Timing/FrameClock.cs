using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Application.Timing;

/// <summary>
/// High-precision frame timing and delta time calculation.
/// Provides consistent time values throughout a frame.
/// </summary>
public sealed class FrameClock
{
    private readonly Stopwatch _stopwatch = new();
    private readonly double _tickFrequency = 1.0 / Stopwatch.Frequency;

    private long _previousTicks;
    private long _currentTicks;

    /// <summary>Time elapsed since last frame in seconds.</summary>
    public double DeltaTime
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get;
        private set;
    }

    /// <summary>Total time elapsed since Start() in seconds.</summary>
    public double TotalTime
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get;
        private set;
    }

    /// <summary>Number of frames since Start().</summary>
    public ulong FrameCount
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get;
        private set;
    }

    /// <summary>Frames per second based on current delta time.</summary>
    public double FramesPerSecond
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => DeltaTime > 0 ? 1.0 / DeltaTime : 0;
    }

    /// <summary>
    /// Starts the clock. Call once at application startup.
    /// </summary>
    public void Start()
    {
        _stopwatch.Start();
        _previousTicks = _stopwatch.ElapsedTicks;
        _currentTicks = _previousTicks;
    }

    /// <summary>
    /// Updates timing for a new frame. Call once at the start of each frame.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Tick()
    {
        _previousTicks = _currentTicks;
        _currentTicks = _stopwatch.ElapsedTicks;

        long deltaTicks = _currentTicks - _previousTicks;
        DeltaTime = deltaTicks * _tickFrequency;
        TotalTime = _currentTicks * _tickFrequency;
        FrameCount++;
    }

    /// <summary>
    /// Resets the clock to initial state.
    /// </summary>
    public void Reset()
    {
        _stopwatch.Restart();
        _previousTicks = 0;
        _currentTicks = 0;
        DeltaTime = 0;
        TotalTime = 0;
        FrameCount = 0;
    }
}
