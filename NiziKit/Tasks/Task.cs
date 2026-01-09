namespace NiziKit.Tasks;

public interface ITask
{
    TaskHandle Handle { get; set; }
    void Execute();
}

public struct TaskHandle(int index)
{
    public int Index = index;

    public static TaskHandle Invalid => new(-1);

    public bool IsValid => Index >= 0;
}
