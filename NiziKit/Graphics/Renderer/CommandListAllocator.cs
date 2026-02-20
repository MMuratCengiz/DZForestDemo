using DenOfIz;
using Semaphore = DenOfIz.Semaphore;

namespace NiziKit.Graphics.Renderer;

public class CommandListAllocator : IDisposable
{
    private const int GraphicsPoolSize = 64;
    private const int ComputePoolSize = 8;

    public class CommandListBucket(CommandListPool pool, CommandList[] lists, Semaphore[] semaphores)
    {
        public readonly CommandListPool Pool = pool;
        public readonly CommandList[] Lists = lists;
        public readonly Semaphore[] Semaphores = semaphores;
        public int FreeIndex = 0;
    }

    public class CommandQueueData(QueueType type, CommandQueue queue, int poolSize)
    {
        public QueueType Type = type;
        public readonly CommandQueue Queue = queue;
        public readonly int PoolSize = poolSize;
        public readonly List<CommandListBucket> Buckets = [];
        public int CurrentBucket;
    }

    private readonly CommandQueueData[] _graphicsQueueData;
    private readonly CommandQueueData[] _computeQueueData;

    public CommandListAllocator()
    {
        var numFrames = (int)GraphicsContext.NumFrames;
        _graphicsQueueData = new CommandQueueData[numFrames];
        _computeQueueData = new CommandQueueData[numFrames];

        for (var i = 0; i < numFrames; i++)
        {
            _graphicsQueueData[i] = CreateCommandQueueData(QueueType.Graphics, GraphicsContext.GraphicsCommandQueue, GraphicsPoolSize);
            _computeQueueData[i] = CreateCommandQueueData(QueueType.Compute, GraphicsContext.ComputeCommandQueue, ComputePoolSize);
        }

        return;

        CommandQueueData CreateCommandQueueData(QueueType type, CommandQueue commandQueue, int poolSize)
        {
            var queueData = new CommandQueueData(type, commandQueue, poolSize);
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
        var poolSize = queueData.PoolSize;
        var commandListPoolDesc = new CommandListPoolDesc
        {
            CommandQueue = queueData.Queue,
            NumCommandLists = (uint)poolSize,
        };
        var commandListPool = GraphicsContext.Device.CreateCommandListPool(commandListPoolDesc);
        var lists = commandListPool.GetCommandLists().ToArray();

        var semaphores = new Semaphore[poolSize];
        for (var i = 0; i < poolSize; i++)
        {
            semaphores[i] = GraphicsContext.Device.CreateSemaphore();
        }

        queueData.Buckets.Add(new CommandListBucket(commandListPool, lists, semaphores));
    }

    public void Dispose()
    {
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
