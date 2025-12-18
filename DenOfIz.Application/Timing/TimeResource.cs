using ECS;
using RuntimeAssets;

namespace Application.Timing;

public sealed class TimeResource(FrameClock clock) : IResource, ITimeResource
{
    public float DeltaTime => (float)clock.DeltaTime;
    public double TotalTime => clock.TotalTime;
}
