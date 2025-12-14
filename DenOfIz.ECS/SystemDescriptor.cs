namespace ECS;

public class SystemDescriptor
{
    public ISystem System { get; }
    public Schedule Schedule { get; }
    public List<Type> RunBefore { get; } = [];
    public List<Type> RunAfter { get; } = [];

    public SystemDescriptor(ISystem system, Schedule schedule)
    {
        System = system;
        Schedule = schedule;
    }

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
