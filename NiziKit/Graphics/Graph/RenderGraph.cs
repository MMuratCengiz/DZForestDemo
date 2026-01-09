using DenOfIz;
using NiziKit.Tasks;
using Semaphore = DenOfIz.Semaphore;

namespace NiziKit.Graphics.Graph;

public class RenderGraph : IDisposable
{
    private class RenderPassTask : ITask
    {
        public RenderPass? Pass;
        public PresentPass? PresentPass;
        public Texture? SwapChainImage;
        public Fence? SignalFence;
        public RenderPassContext Context;
        public CommandList CommandList = null!;
        public Semaphore Semaphore = null!;
        public TaskHandle Handle { get; set; }

        private readonly Semaphore[] _waitSemaphores = new Semaphore[16];
        private int _waitCount;

        public void AddDependency(Semaphore semaphore)
        {
            _waitSemaphores[_waitCount++] = semaphore;
        }

        public void Execute()
        {
            CommandList.Begin();
            if (Pass != null)
            {
                Pass.Execute(ref Context);
            }
            else
            {
                PresentPass!.Execute(ref Context, SwapChainImage!);
            }

            CommandList.End();

            Context.GraphicsContext.GraphicsCommandQueue.ExecuteCommandLists(
                new ReadOnlySpan<CommandList>(ref CommandList),
                SignalFence,
                new ReadOnlySpan<Semaphore>(_waitSemaphores, 0, _waitCount),
                new ReadOnlySpan<Semaphore>(ref Semaphore));
        }

        public void Reset()
        {
            Pass = null;
            PresentPass = null;
            SwapChainImage = null;
            SignalFence = null;
            _waitCount = 0;
        }
    }

    private struct FrameContext
    {
        public Fence Fence;
        public TaskGraph Graph;
        public FrameResources Resources;
        public Dictionary<string, RenderPassTask> WriterLookup;
    }

    private readonly TaskExecutor _executor;
    private readonly GraphicsContext _context;
    private readonly FrameContext[] _frames;
    private readonly CommandListAllocator _commandListAllocator;
    private readonly RenderPassTask[] _taskPool;
    private bool _disposed;
    private uint _frameIndex = 0;
    private uint _nextFrameIndex = 0;

    public uint Width { get; private set; }
    public uint Height { get; private set; }

    public RenderGraph(GraphicsContext context, int numThreads = 0, int numFrames = 3)
    {
        _executor = new TaskExecutor(numThreads);
        _context = context;
        _frames = new FrameContext[numFrames];
        _commandListAllocator = new CommandListAllocator(context, numFrames);
        _taskPool = new RenderPassTask[TaskGraph.MaxTasks];

        for (var i = 0; i < TaskGraph.MaxTasks; i++)
        {
            _taskPool[i] = new RenderPassTask();
        }

        for (var i = 0; i < numFrames; i++)
        {
            _frames[i] = new FrameContext
            {
                Fence = context.LogicalDevice.CreateFence(),
                Graph = new TaskGraph(),
                Resources = new FrameResources(),
                WriterLookup = new Dictionary<string, RenderPassTask>(64)
            };
        }
    }

    public void Execute(ReadOnlySpan<RenderPass> renderPasses, PresentPass presentPass)
    {
        _frameIndex = _nextFrameIndex;
        _nextFrameIndex = (uint)((_nextFrameIndex + 1) % _frames.Length);

        ref var frame = ref _frames[_frameIndex];
        frame.Fence.Wait();

        var graph = frame.Graph;
        var writerLookup = frame.WriterLookup;
        var resources = frame.Resources;

        graph.Reset();
        writerLookup.Clear();
        resources.Clear();
        _commandListAllocator.Reset(_frameIndex);

        var ctx = new RenderPassContext
        {
            GraphicsContext = _context,
            Resources = resources,
            FrameIndex = _frameIndex,
            Width = Width,
            Height = Height
        };

        for (var i = 0; i < renderPasses.Length; i++)
        {
            var pass = renderPasses[i];
            pass.Resize(Width, Height, _frameIndex, resources);

            var (commandList, semaphore) = _commandListAllocator.GetCommandList(QueueType.Graphics, _frameIndex);
            var task = _taskPool[i];
            task.Reset();
            task.Pass = pass;
            task.CommandList = commandList;
            task.Semaphore = semaphore;
            task.Context = ctx;
            task.Context.CommandList = task.CommandList;

            graph.Emplace(task);

            foreach (var write in pass.Writes)
            {
                writerLookup[write.Name] = task;
            }
        }

        for (var i = 0; i < renderPasses.Length; i++)
        {
            var task = _taskPool[i];
            foreach (var read in task.Pass!.Reads)
            {
                if (writerLookup.TryGetValue(read, out var writer) && writer != task)
                {
                    graph.Precede(writer, task);
                    task.AddDependency(writer.Semaphore);
                }
            }
        }

        presentPass.Resize(Width, Height, _frameIndex, resources);

        var presentTaskIndex = renderPasses.Length;
        var (presentCommandList, presentSemaphore) =
            _commandListAllocator.GetCommandList(QueueType.Graphics, _frameIndex);

        var image = _context.SwapChain.AcquireNextImage();

        var presentTask = _taskPool[presentTaskIndex];
        presentTask.Reset();
        presentTask.PresentPass = presentPass;
        presentTask.SwapChainImage = _context.SwapChain.GetRenderTarget(image);
        presentTask.CommandList = presentCommandList;
        presentTask.Semaphore = presentSemaphore;
        presentTask.SignalFence = frame.Fence;
        presentTask.Context = ctx;
        presentTask.Context.CommandList = presentTask.CommandList;

        graph.Emplace(presentTask);

        foreach (var write in presentPass.Writes)
        {
            writerLookup[write.Name] = presentTask;
        }

        foreach (var read in presentPass.Reads)
        {
            if (writerLookup.TryGetValue(read, out var writer))
            {
                graph.Precede(writer, presentTask);
                presentTask.AddDependency(writer.Semaphore);
            }
        }

        graph.Execute(_executor);
        switch (_context.SwapChain.Present(_frameIndex))
        {
            case PresentResult.Success:
            case PresentResult.Suboptimal:
                break;
            case PresentResult.Timeout:
            case PresentResult.DeviceLost:
                _context.WaitIdle();
                break;
        }
    }

    public void Resize(uint width, uint height)
    {
        if (Width == width && Height == height)
        {
            return;
        }

        _context.GraphicsCommandQueue.WaitIdle();
        Width = width;
        Height = height;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        _context.GraphicsCommandQueue.WaitIdle();
        foreach (var frame in _frames)
        {
            frame.Fence.Dispose();
        }

        _commandListAllocator.Dispose();
        _executor.Dispose();
    }
}