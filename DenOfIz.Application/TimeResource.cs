using Application.Timing;
using ECS;
using RuntimeAssets;

namespace Application;

public sealed class TimeResource : IContext, ITimeResource
{
    private readonly FrameClock _clock;

    public TimeResource(FrameClock clock)
    {
        _clock = clock;
    }

    public float DeltaTime => (float)_clock.DeltaTime;
    public double TotalTime => _clock.TotalTime;
}
