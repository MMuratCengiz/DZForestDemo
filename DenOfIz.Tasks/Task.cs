namespace DenOfIz.Tasks;

public interface ITask
{
    void Execute();
}

public readonly struct ActionTask(Action action) : ITask
{
    public readonly Action Action = action;

    public void Execute()
    {
        Action?.Invoke();
    }
}

public readonly struct TaskHandle(int index)
{
    public readonly int Index = index;

    public static TaskHandle Invalid => new(-1);

    public bool IsValid => Index >= 0;
}
