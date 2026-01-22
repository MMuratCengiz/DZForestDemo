namespace NiziKit.Tasks;

public class TaskGraph
{
    public const int MaxTasks = 256;
    public const int MaxEdges = 1024;

    internal readonly ITask?[] Tasks = new ITask?[MaxTasks];
    private readonly int[] _edgeFrom = new int[MaxEdges];
    private readonly int[] _edgeTo = new int[MaxEdges];
    private readonly int[] _inDegree = new int[MaxTasks];
    private readonly int[] _executionOrder = new int[MaxTasks];
    private readonly int[] _adjacencyOffset = new int[MaxTasks + 1];
    private readonly int[] _adjacency = new int[MaxEdges];
    private readonly int[] _queue = new int[MaxTasks];

    internal int TaskCount;
    private int _executionCount;
    private bool _isCompiled;

    internal int EdgeCount { get; private set; }

    internal ReadOnlySpan<int> EdgeFrom => new(_edgeFrom, 0, EdgeCount);
    internal ReadOnlySpan<int> EdgeTo => new(_edgeTo, 0, EdgeCount);

    public void Reset()
    {
        for (var i = 0; i < TaskCount; i++)
        {
            Tasks[i] = null;
            _inDegree[i] = 0;
        }

        TaskCount = 0;
        EdgeCount = 0;
        _executionCount = 0;
        _isCompiled = false;
    }

    public TaskHandle Emplace(ITask task)
    {
        var id = TaskCount;
        var handle = new TaskHandle(id);
        task.Handle = handle;
        Tasks[id] = task;
        TaskCount++;
        return handle;
    }

    public void Precede(ITask first, ITask second)
    {
        Precede(first.Handle, second.Handle);
    }

    public void Precede(TaskHandle first, TaskHandle second)
    {
        _edgeFrom[EdgeCount] = first.Index;
        _edgeTo[EdgeCount] = second.Index;
        EdgeCount++;
    }

    public void Precede(TaskHandle first, TaskHandle second, TaskHandle third)
    {
        Precede(first, second);
        Precede(first, third);
    }

    public void Precede(TaskHandle first, TaskHandle second, TaskHandle third, TaskHandle fourth)
    {
        Precede(first, second);
        Precede(first, third);
        Precede(first, fourth);
    }

    public void Succeed(TaskHandle first, TaskHandle second)
    {
        _edgeFrom[EdgeCount] = second.Index;
        _edgeTo[EdgeCount] = first.Index;
        EdgeCount++;
    }

    public void Succeed(TaskHandle first, TaskHandle second, TaskHandle third)
    {
        Succeed(first, second);
        Succeed(first, third);
    }

    public void Succeed(TaskHandle first, TaskHandle second, TaskHandle third, TaskHandle fourth)
    {
        Succeed(first, second);
        Succeed(first, third);
        Succeed(first, fourth);
    }

    public void Compile()
    {
        if (_isCompiled)
        {
            return;
        }

        BuildAdjacency();
        TopologicalSort();
        _isCompiled = true;
    }

    public void Execute()
    {
        if (!_isCompiled)
        {
            Compile();
        }

        for (var i = 0; i < _executionCount; i++)
        {
            Tasks[_executionOrder[i]]?.Execute();
        }
    }

    public void Execute(TaskExecutor executor)
    {
        executor.Execute(this);
    }

    private void BuildAdjacency()
    {
        for (var i = 0; i <= TaskCount; i++)
        {
            _adjacencyOffset[i] = 0;
        }

        for (var e = 0; e < EdgeCount; e++)
        {
            _adjacencyOffset[_edgeFrom[e] + 1]++;
        }

        for (var i = 1; i <= TaskCount; i++)
        {
            _adjacencyOffset[i] += _adjacencyOffset[i - 1];
        }

        Span<int> currentOffset = stackalloc int[MaxTasks];
        for (var i = 0; i < TaskCount; i++)
        {
            currentOffset[i] = _adjacencyOffset[i];
        }

        for (var e = 0; e < EdgeCount; e++)
        {
            var from = _edgeFrom[e];
            var to = _edgeTo[e];
            _adjacency[currentOffset[from]++] = to;
        }
    }

    private void TopologicalSort()
    {
        for (var i = 0; i < TaskCount; i++)
        {
            _inDegree[i] = 0;
        }

        for (var e = 0; e < EdgeCount; e++)
        {
            _inDegree[_edgeTo[e]]++;
        }

        var queueHead = 0;
        var queueTail = 0;

        for (var i = 0; i < TaskCount; i++)
        {
            if (_inDegree[i] == 0)
            {
                _queue[queueTail++] = i;
            }
        }

        _executionCount = 0;
        while (queueHead < queueTail)
        {
            var current = _queue[queueHead++];
            _executionOrder[_executionCount++] = current;

            var start = _adjacencyOffset[current];
            var end = _adjacencyOffset[current + 1];

            for (var i = start; i < end; i++)
            {
                var successor = _adjacency[i];
                _inDegree[successor]--;
                if (_inDegree[successor] == 0)
                {
                    _queue[queueTail++] = successor;
                }
            }
        }
    }
}
