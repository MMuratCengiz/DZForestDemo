using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace NiziKit.Application.Timing;

public sealed class Time
{
    private static Time? _instance;
    private static Time Instance => _instance ?? throw new InvalidOperationException("Time not initialized");

    public static float DeltaTime
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Instance._deltaTime;
    }

    public static float TotalTime
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Instance._totalTime;
    }

    public static ulong FrameCount
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Instance._frameCount;
    }

    public static float FramesPerSecond
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Instance._deltaTime > 0 ? 1.0f / Instance._deltaTime : 0;
    }

    public static float TimeScale
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Instance._timeScale;
        set => Instance._timeScale = value;
    }

    public static float UnscaledDeltaTime
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Instance._unscaledDeltaTime;
    }

    public static float UnscaledTotalTime
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Instance._unscaledTotalTime;
    }

    private readonly Stopwatch _stopwatch = new();
    private readonly double _tickFrequency = 1.0 / Stopwatch.Frequency;
    private long _currentTicks;
    private long _previousTicks;

    private float _deltaTime;
    private float _totalTime;
    private float _unscaledDeltaTime;
    private float _unscaledTotalTime;
    private ulong _frameCount;
    private float _timeScale = 1.0f;

    internal static void Start() => Instance._Start();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void Tick() => Instance._Tick();

    public Time()
    {
        _instance = this;
    }

    private void _Start()
    {
        _stopwatch.Start();
        _previousTicks = _stopwatch.ElapsedTicks;
        _currentTicks = _previousTicks;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void _Tick()
    {
        _previousTicks = _currentTicks;
        _currentTicks = _stopwatch.ElapsedTicks;

        var deltaTicks = _currentTicks - _previousTicks;
        _unscaledDeltaTime = (float)(deltaTicks * _tickFrequency);
        _unscaledTotalTime = (float)(_currentTicks * _tickFrequency);

        _deltaTime = _unscaledDeltaTime * _timeScale;
        _totalTime += _deltaTime;
        _frameCount++;
    }
}
