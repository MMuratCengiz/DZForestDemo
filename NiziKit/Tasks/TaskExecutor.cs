using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace NiziKit.Tasks;

public sealed class TaskExecutor : IDisposable
{
    private const int CacheLineSize = 64;

    private readonly int _threadCount;
    private readonly Thread[] _workers;
    private readonly WorkStealingDeque[] _localQueues;
    private readonly ConcurrentQueue<int>[] _incomingQueues;
    private readonly int[] _remainingDependencies;
    private readonly int[] _adjacencyOffset;
    private readonly int[] _adjacency;
    private readonly ITask?[] _tasks;
    private readonly CancellationTokenSource _cts;

    private PaddedInt _remainingTasks;
    private volatile bool _hasWork;
    private int _nextWorker;

    [StructLayout(LayoutKind.Explicit, Size = 2 * CacheLineSize)]
    private struct PaddedInt
    {
        [FieldOffset(CacheLineSize)]
        public int Value;
    }

    public TaskExecutor(int threadCount = 0)
    {
        if (threadCount <= 0)
        {
            threadCount = Environment.ProcessorCount;
        }

        _threadCount = threadCount;
        _workers = new Thread[threadCount];
        _localQueues = new WorkStealingDeque[threadCount];
        _incomingQueues = new ConcurrentQueue<int>[threadCount];
        _remainingDependencies = new int[TaskGraph.MaxTasks];
        _adjacencyOffset = new int[TaskGraph.MaxTasks + 1];
        _adjacency = new int[TaskGraph.MaxEdges];
        _tasks = new ITask?[TaskGraph.MaxTasks];
        _cts = new CancellationTokenSource();

        for (var i = 0; i < threadCount; i++)
        {
            _localQueues[i] = new WorkStealingDeque();
            _incomingQueues[i] = new ConcurrentQueue<int>();

            var workerId = i;
            _workers[i] = new Thread(() => WorkerLoop(workerId, _cts.Token))
            {
                IsBackground = true,
                Name = $"TaskWorker-{i}"
            };
            _workers[i].Start();
        }
    }

    public void Execute(TaskGraph graph)
    {
        if (graph.TaskCount == 0)
        {
            return;
        }

        graph.Compile();
        PrepareExecution(graph);

        _hasWork = true;

        SpinWait spinner = default;
        while (Volatile.Read(ref _remainingTasks.Value) > 0)
        {
            spinner.SpinOnce();
        }

        _hasWork = false;
    }

    private void PrepareExecution(TaskGraph graph)
    {
        var taskCount = graph.TaskCount;
        var edgeCount = graph.EdgeCount;

        for (var i = 0; i <= taskCount; i++)
        {
            _adjacencyOffset[i] = 0;
        }

        for (var e = 0; e < edgeCount; e++)
        {
            _adjacencyOffset[graph.EdgeFrom[e] + 1]++;
        }

        for (var i = 1; i <= taskCount; i++)
        {
            _adjacencyOffset[i] += _adjacencyOffset[i - 1];
        }

        Span<int> currentOffset = stackalloc int[TaskGraph.MaxTasks];
        for (var i = 0; i < taskCount; i++)
        {
            currentOffset[i] = _adjacencyOffset[i];
            _remainingDependencies[i] = 0;
            _tasks[i] = graph.Tasks[i];
        }

        for (var e = 0; e < edgeCount; e++)
        {
            var from = graph.EdgeFrom[e];
            var to = graph.EdgeTo[e];
            _adjacency[currentOffset[from]++] = to;
            _remainingDependencies[to]++;
        }

        _remainingTasks.Value = taskCount;

        for (var i = 0; i < _threadCount; i++)
        {
            _localQueues[i].Clear();
            while (_incomingQueues[i].TryDequeue(out _)) { }
        }

        for (var i = 0; i < taskCount; i++)
        {
            if (_remainingDependencies[i] != 0)
            {
                continue;
            }

            var worker = _nextWorker;
            _incomingQueues[worker].Enqueue(i);
            _nextWorker = (_nextWorker + 1) % _threadCount;
        }
    }

    private void WorkerLoop(int workerId, CancellationToken token)
    {
        var localQueue = _localQueues[workerId];
        var incomingQueue = _incomingQueues[workerId];

        while (!token.IsCancellationRequested)
        {
            if (!_hasWork)
            {
                Thread.Yield();
                continue;
            }

            while (localQueue.Count < 32 && incomingQueue.TryDequeue(out var incoming))
            {
                localQueue.PushBottom(incoming);
            }

            if (localQueue.TryPopBottom(out var taskIndex))
            {
                ExecuteTask(taskIndex, workerId);
                continue;
            }

            var found = false;
            for (var i = 0; i < _threadCount; i++)
            {
                if (i == workerId)
                {
                    continue;
                }

                if (_localQueues[i].TrySteal(out taskIndex))
                {
                    ExecuteTask(taskIndex, workerId);
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                Thread.Yield();
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ExecuteTask(int taskIndex, int workerId)
    {
        _tasks[taskIndex]?.Execute();

        var start = _adjacencyOffset[taskIndex];
        var end = _adjacencyOffset[taskIndex + 1];

        for (var i = start; i < end; i++)
        {
            var successor = _adjacency[i];
            var remaining = Interlocked.Decrement(ref _remainingDependencies[successor]);
            if (remaining == 0)
            {
                _localQueues[workerId].PushBottom(successor);
            }
        }

        Interlocked.Decrement(ref _remainingTasks.Value);
    }

    public void Dispose()
    {
        _cts.Cancel();

        foreach (var worker in _workers)
        {
            worker.Join(100);
        }

        _cts.Dispose();
    }
}
