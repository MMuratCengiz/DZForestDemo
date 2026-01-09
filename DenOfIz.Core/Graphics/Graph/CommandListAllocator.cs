using Semaphore = DenOfIz.Semaphore;

namespace DenOfIz.World.Graphics.Graph;

public class CommandListAllocator : IDisposable
{
    private const int PoolSize = 128;

    public class CommandListBucket(CommandListPool pool, CommandList[] lists, Semaphore[] semaphores)
    {
        public readonly CommandListPool Pool = pool;
        public readonly CommandList[] Lists = lists;
        public readonly Semaphore[] Semaphores = semaphores;
        public int FreeIndex = 0;
    }

    public class CommandQueueData(QueueType type, CommandQueue queue)
    {
        public QueueType Type = type;
        public readonly CommandQueue Queue = queue;
        public readonly List<CommandListBucket> Buckets = [];
        public int CurrentBucket;
    }

    private readonly CommandQueueData[] _graphicsQueueData;
    private readonly CommandQueueData[] _computeQueueData;
    private readonly GraphicsContext _context;
    private bool _disposed;

    public CommandListAllocator(GraphicsContext context, int numFrames)
    {
        _context = context;
        _graphicsQueueData = new CommandQueueData[numFrames];
        _computeQueueData = new CommandQueueData[numFrames];

        for (var i = 0; i < numFrames; i++)
        {
            _graphicsQueueData[i] = CreateCommandQueueData(QueueType.Graphics, context.GraphicsCommandQueue);
            _computeQueueData[i] = CreateCommandQueueData(QueueType.Compute, context.ComputeCommandQueue);
        }

        return;

        CommandQueueData CreateCommandQueueData(QueueType type, CommandQueue commandQueue)
        {
            var queueData = new CommandQueueData(type, commandQueue);
            AddBucket(queueData);
            return queueData;
        }
    }

    public void Reset(uint frameIndex)
    {
        ResetQueueData(_graphicsQueueData[frameIndex]);
        ResetQueueData(_computeQueueData[frameIndex]);

        return;

        static void ResetQueueData(CommandQueueData queueData)
        {
            queueData.CurrentBucket = 0;
            for (var i = 0; i < queueData.Buckets.Count; i++)
            {
                queueData.Buckets[i].FreeIndex = 0;
            }
        }
    }

    public (CommandList, Semaphore) GetCommandList(QueueType type, uint frameIndex)
    {
        var queueData = type switch
        {
            QueueType.Graphics => _graphicsQueueData[frameIndex],
            QueueType.Compute => _computeQueueData[frameIndex],
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };

        var bucket = queueData.Buckets[queueData.CurrentBucket];

        if (bucket.FreeIndex >= bucket.Lists.Length)
        {
            queueData.CurrentBucket++;
            if (queueData.CurrentBucket >= queueData.Buckets.Count)
            {
                AddBucket(queueData);
            }

            bucket = queueData.Buckets[queueData.CurrentBucket];
        }

        var index = bucket.FreeIndex++;
        return (bucket.Lists[index], bucket.Semaphores[index]);
    }

    private void AddBucket(CommandQueueData queueData)
    {
        var commandListPoolDesc = new CommandListPoolDesc
        {
            CommandQueue = queueData.Queue,
            NumCommandLists = PoolSize,
        };
        var commandListPool = _context.LogicalDevice.CreateCommandListPool(commandListPoolDesc);
        var lists = commandListPool.GetCommandLists().ToArray();

        var semaphores = new Semaphore[PoolSize];
        for (var i = 0; i < PoolSize; i++)
        {
            semaphores[i] = _context.LogicalDevice.CreateSemaphore();
        }

        queueData.Buckets.Add(new CommandListBucket(commandListPool, lists, semaphores));
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        DisposeQueueData(_graphicsQueueData);
        DisposeQueueData(_computeQueueData);

        return;

        static void DisposeQueueData(CommandQueueData[] queueDataArray)
        {
            foreach (var queueData in queueDataArray)
            {
                foreach (var bucket in queueData.Buckets)
                {
                    foreach (var semaphore in bucket.Semaphores)
                    {
                        semaphore.Dispose();
                    }

                    bucket.Pool.Dispose();
                }
            }
        }
    }
}