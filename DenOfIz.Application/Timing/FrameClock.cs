using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Application.Timing;

public sealed class FrameClock
{
    private readonly Stopwatch _stopwatch = new();
    private readonly double _tickFrequency = 1.0 / Stopwatch.Frequency;
    private long _currentTicks;

    private long _previousTicks;

    public double DeltaTime
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get;
        private set;
    }

    public double TotalTime
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get;
        private set;
    }

    public ulong FrameCount
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get;
        private set;
    }

    public double FramesPerSecond
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => DeltaTime > 0 ? 1.0 / DeltaTime : 0;
    }

    public void Start()
    {
        _stopwatch.Start();
        _previousTicks = _stopwatch.ElapsedTicks;
        _currentTicks = _previousTicks;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Tick()
    {
        _previousTicks = _currentTicks;
        _currentTicks = _stopwatch.ElapsedTicks;

        var deltaTicks = _currentTicks - _previousTicks;
        DeltaTime = deltaTicks * _tickFrequency;
        TotalTime = _currentTicks * _tickFrequency;
        FrameCount++;
    }

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