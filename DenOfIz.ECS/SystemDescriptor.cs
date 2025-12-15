namespace ECS;

public class SystemDescriptor(ISystem system, Schedule schedule)
{
    public ISystem System { get; } = system;
    public Schedule Schedule { get; } = schedule;
    public List<Type> RunBefore { get; } = [];
    public List<Type> RunAfter { get; } = [];

    public SystemDescriptor Before<T>() where T : ISystem
    {
        RunBefore.Add(typeof(T));
        return this;
    }

    public SystemDescriptor After<T>() where T : ISystem
    {
        RunAfter.Add(typeof(T));
        return this;
    }
}
