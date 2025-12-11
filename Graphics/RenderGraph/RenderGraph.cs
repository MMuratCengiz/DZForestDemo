using System.Runtime.CompilerServices;
using DenOfIz;
using Semaphore = DenOfIz.Semaphore;

namespace Graphics.RenderGraph;

public struct RenderGraphDesc
{
    public LogicalDevice LogicalDevice;
    public CommandQueue CommandQueue;
    public uint NumFrames = 3;
    public uint MaxPasses = 64;
    public uint MaxResources = 256;

    public RenderGraphDesc() { }
}

public class RenderGraph : IDisposable
{
    readonly LogicalDevice _logicalDevice;
    readonly CommandQueue _commandQueue;
    readonly ResourceTracking _resourceTracking;
    readonly uint _numFrames;
    readonly uint _maxPasses;
    readonly uint _maxResources;

    readonly List<RenderGraphResourceEntry> _resources;
    readonly Dictionary<string, ResourceHandle> _namedResources;
    int _resourceCount;

    readonly List<RenderPassData> _passes;
    readonly List<int> _executionOrder;
    int _passCount;

    readonly CommandListPool[] _commandListPools;
    readonly List<Semaphore>[] _frameSemaphores;
    readonly Fence[] _frameFences;
    uint _currentFrameIndex;
    bool _isFrameActive;
    bool _isCompiled;

    readonly List<TextureResource>[] _transientTextures;
    readonly List<BufferResource>[] _transientBuffers;

    uint _width;
    uint _height;

    readonly Semaphore[] _waitSemaphoresBuffer;
    readonly Semaphore[] _signalSemaphoresBuffer;

    bool _disposed;

    public RenderGraph(RenderGraphDesc desc)
    {
        _logicalDevice = desc.LogicalDevice;
        _commandQueue = desc.CommandQueue;
        _numFrames = desc.NumFrames;
        _maxPasses = desc.MaxPasses;
        _maxResources = desc.MaxResources;

        _resourceTracking = new ResourceTracking();

        _resources = new List<RenderGraphResourceEntry>((int)_maxResources);
        for (var i = 0; i < _maxResources; i++)
            _resources.Add(new RenderGraphResourceEntry());
        _namedResources = new Dictionary<string, ResourceHandle>((int)_maxResources);

        _passes = new List<RenderPassData>((int)_maxPasses);
        for (var i = 0; i < _maxPasses; i++)
            _passes.Add(new RenderPassData { Index = i });
        _executionOrder = new List<int>((int)_maxPasses);

        _commandListPools = new CommandListPool[_numFrames];
        _frameSemaphores = new List<Semaphore>[_numFrames];
        _frameFences = new Fence[_numFrames];
        _transientTextures = new List<TextureResource>[_numFrames];
        _transientBuffers = new List<BufferResource>[_numFrames];

        for (var i = 0; i < _numFrames; i++)
        {
            _commandListPools[i] = _logicalDevice.CreateCommandListPool(new CommandListPoolDesc
            {
                CommandQueue = _commandQueue,
                NumCommandLists = _maxPasses
            });
            _frameSemaphores[i] = new List<Semaphore>((int)_maxPasses);
            for (var j = 0; j < _maxPasses; j++)
                _frameSemaphores[i].Add(_logicalDevice.CreateSemaphore());
            _frameFences[i] = _logicalDevice.CreateFence();
            _transientTextures[i] = new List<TextureResource>(32);
            _transientBuffers[i] = new List<BufferResource>(32);
        }

        _waitSemaphoresBuffer = new Semaphore[_maxPasses];
        _signalSemaphoresBuffer = new Semaphore[1];
    }

    public uint Width => _width;
    public uint Height => _height;
    public uint CurrentFrameIndex => _currentFrameIndex;
    public ResourceTracking ResourceTracking => _resourceTracking;

    public void SetDimensions(uint width, uint height)
    {
        _width = width;
        _height = height;
    }

    public void BeginFrame(uint frameIndex)
    {
        if (_isFrameActive)
            throw new InvalidOperationException("Frame already active. Call Execute() first.");

        _currentFrameIndex = frameIndex;
        _frameFences[_currentFrameIndex].Wait();
        _frameFences[_currentFrameIndex].Reset();

        ResetFrame();
        _isFrameActive = true;
        _isCompiled = false;
    }

    public ResourceHandle ImportTexture(string name, TextureResource texture)
    {
        if (_resourceCount >= _maxResources)
            throw new InvalidOperationException("Maximum resource count exceeded");

        var entry = _resources[_resourceCount];
        entry.Reset();
        entry.Type = RenderGraphResourceType.Texture;
        entry.IsImported = true;
        entry.Texture = texture;

        var handle = new ResourceHandle(_resourceCount, entry.Version);
        _namedResources[name] = handle;
        _resourceCount++;
        return handle;
    }

    public ResourceHandle ImportBuffer(string name, BufferResource buffer)
    {
        if (_resourceCount >= _maxResources)
            throw new InvalidOperationException("Maximum resource count exceeded");

        var entry = _resources[_resourceCount];
        entry.Reset();
        entry.Type = RenderGraphResourceType.Buffer;
        entry.IsImported = true;
        entry.Buffer = buffer;

        var handle = new ResourceHandle(_resourceCount, entry.Version);
        _namedResources[name] = handle;
        _resourceCount++;
        return handle;
    }

    internal ResourceHandle CreateTransientTexture(TransientTextureDesc desc)
    {
        if (_resourceCount >= _maxResources)
            throw new InvalidOperationException("Maximum resource count exceeded");

        var entry = _resources[_resourceCount];
        entry.Reset();
        entry.Type = RenderGraphResourceType.Texture;
        entry.IsTransient = true;
        entry.TextureDesc = desc;

        var handle = new ResourceHandle(_resourceCount, entry.Version);
        if (!string.IsNullOrEmpty(desc.DebugName))
            _namedResources[desc.DebugName] = handle;
        _resourceCount++;
        return handle;
    }

    internal ResourceHandle CreateTransientBuffer(TransientBufferDesc desc)
    {
        if (_resourceCount >= _maxResources)
            throw new InvalidOperationException("Maximum resource count exceeded");

        var entry = _resources[_resourceCount];
        entry.Reset();
        entry.Type = RenderGraphResourceType.Buffer;
        entry.IsTransient = true;
        entry.BufferDesc = desc;

        var handle = new ResourceHandle(_resourceCount, entry.Version);
        if (!string.IsNullOrEmpty(desc.DebugName))
            _namedResources[desc.DebugName] = handle;
        _resourceCount++;
        return handle;
    }

    public ResourceHandle GetResource(string name) =>
        _namedResources.TryGetValue(name, out var handle) ? handle : ResourceHandle.Invalid;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TextureResource GetTexture(ResourceHandle handle)
    {
        if (!handle.IsValid || handle.Index >= _resourceCount)
            throw new ArgumentException("Invalid resource handle");

        var entry = _resources[handle.Index];
        if (entry.Version != handle.Version)
            throw new ArgumentException("Stale resource handle");

        return entry.Texture ?? throw new InvalidOperationException("Resource is not a texture or not allocated");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public BufferResource GetBuffer(ResourceHandle handle)
    {
        if (!handle.IsValid || handle.Index >= _resourceCount)
            throw new ArgumentException("Invalid resource handle");

        var entry = _resources[handle.Index];
        if (entry.Version != handle.Version)
            throw new ArgumentException("Stale resource handle");

        return entry.Buffer ?? throw new InvalidOperationException("Resource is not a buffer or not allocated");
    }

    public void AddPass(string name, RenderPassSetupDelegate setup, RenderPassExecuteDelegate execute)
    {
        if (_passCount >= _maxPasses)
            throw new InvalidOperationException("Maximum pass count exceeded");

        var passData = _passes[_passCount];
        passData.Reset();
        passData.Index = _passCount;
        passData.Name = name;
        passData.Execute = execute;

        var setupContext = new RenderPassSetupContext
        {
            Graph = this,
            Width = _width,
            Height = _height,
            FrameIndex = _currentFrameIndex
        };

        var builder = new PassBuilder(passData, this);
        setup(ref setupContext, ref builder);
        _passCount++;
    }

    public void AddPass(string name, RenderPassExecuteDelegate execute)
    {
        AddPass(name, static (ref RenderPassSetupContext _, ref PassBuilder builder) =>
        {
            builder.HasSideEffects();
        }, execute);
    }

    internal void UpdateResourceLifetime(ResourceHandle handle, int passIndex)
    {
        if (!handle.IsValid || handle.Index >= _resourceCount)
            return;

        var entry = _resources[handle.Index];
        if (entry.FirstPassIndex < 0)
            entry.FirstPassIndex = passIndex;
        entry.LastPassIndex = passIndex;
    }

    public void Compile()
    {
        if (!_isFrameActive)
            throw new InvalidOperationException("No active frame. Call BeginFrame() first.");

        if (_isCompiled)
            return;

        BuildDependencyGraph();
        CullPasses();
        TopologicalSort();
        AllocateTransientResources();
        AssignCommandLists();
        _isCompiled = true;
    }

    public void Execute()
    {
        if (!_isFrameActive)
            throw new InvalidOperationException("No active frame. Call BeginFrame() first.");

        if (!_isCompiled)
            Compile();

        for (var i = 0; i < _executionOrder.Count; i++)
        {
            var passData = _passes[_executionOrder[i]];
            if (passData.IsCulled || passData.Execute == null || passData.CommandList == null)
                continue;

            var commandList = passData.CommandList;
            commandList.Begin();

            var context = new RenderPassExecuteContext
            {
                Graph = this,
                CommandList = commandList,
                ResourceTracking = _resourceTracking,
                Width = _width,
                Height = _height,
                FrameIndex = _currentFrameIndex
            };

            passData.Execute(ref context);
            commandList.End();
        }

        SubmitCommandLists();
        _isFrameActive = false;
    }

    void ResetFrame()
    {
        _resourceCount = 0;
        _namedResources.Clear();
        _passCount = 0;
        _executionOrder.Clear();
    }

    void BuildDependencyGraph()
    {
        for (var i = 0; i < _passCount; i++)
        {
            _passes[i].DependsOnPasses.Clear();
            _passes[i].DependentPasses.Clear();
        }

        Span<int> lastWriter = stackalloc int[_resourceCount];
        lastWriter.Fill(-1);

        for (var passIdx = 0; passIdx < _passCount; passIdx++)
        {
            var pass = _passes[passIdx];

            foreach (var input in pass.Inputs)
            {
                var writerPass = lastWriter[input.Handle.Index];
                if (writerPass >= 0 && writerPass != passIdx && !pass.DependsOnPasses.Contains(writerPass))
                {
                    pass.DependsOnPasses.Add(writerPass);
                    _passes[writerPass].DependentPasses.Add(passIdx);
                }
            }

            foreach (var output in pass.Outputs)
                lastWriter[output.Handle.Index] = passIdx;
        }
    }

    void CullPasses()
    {
        for (var i = 0; i < _passCount; i++)
            _passes[i].IsCulled = true;

        var toProcess = new Queue<int>();
        for (var i = 0; i < _passCount; i++)
        {
            if (_passes[i].HasSideEffects)
            {
                _passes[i].IsCulled = false;
                toProcess.Enqueue(i);
            }
        }

        while (toProcess.Count > 0)
        {
            var passIdx = toProcess.Dequeue();
            foreach (var depIdx in _passes[passIdx].DependsOnPasses)
            {
                if (_passes[depIdx].IsCulled)
                {
                    _passes[depIdx].IsCulled = false;
                    toProcess.Enqueue(depIdx);
                }
            }
        }
    }

    void TopologicalSort()
    {
        _executionOrder.Clear();

        Span<int> inDegree = stackalloc int[_passCount];
        for (var i = 0; i < _passCount; i++)
        {
            if (_passes[i].IsCulled)
            {
                inDegree[i] = -1;
                continue;
            }

            var count = 0;
            foreach (var dep in _passes[i].DependsOnPasses)
                if (!_passes[dep].IsCulled)
                    count++;
            inDegree[i] = count;
        }

        var queue = new Queue<int>();
        for (var i = 0; i < _passCount; i++)
            if (inDegree[i] == 0)
                queue.Enqueue(i);

        var order = 0;
        while (queue.Count > 0)
        {
            var passIdx = queue.Dequeue();
            var pass = _passes[passIdx];

            pass.ExecutionOrder = order++;
            _executionOrder.Add(passIdx);

            foreach (var depIdx in pass.DependentPasses)
            {
                if (_passes[depIdx].IsCulled)
                    continue;

                inDegree[depIdx]--;
                if (inDegree[depIdx] == 0)
                    queue.Enqueue(depIdx);
            }
        }

        var culledCount = 0;
        for (var i = 0; i < _passCount; i++)
            if (_passes[i].IsCulled) culledCount++;

        if (_executionOrder.Count < _passCount - culledCount)
            throw new InvalidOperationException("Circular dependency detected in render graph");
    }

    void AllocateTransientResources()
    {
        var transientTextures = _transientTextures[_currentFrameIndex];
        var transientBuffers = _transientBuffers[_currentFrameIndex];
        var textureIndex = 0;
        var bufferIndex = 0;

        for (var i = 0; i < _resourceCount; i++)
        {
            var entry = _resources[i];
            if (!entry.IsTransient || entry.FirstPassIndex < 0 || _passes[entry.FirstPassIndex].IsCulled)
                continue;

            if (entry.Type == RenderGraphResourceType.Texture)
            {
                if (textureIndex < transientTextures.Count)
                {
                    entry.Texture = transientTextures[textureIndex];
                }
                else
                {
                    var desc = entry.TextureDesc;
                    var texture = _logicalDevice.CreateTextureResource(new TextureDesc
                    {
                        Width = desc.Width > 0 ? desc.Width : _width,
                        Height = desc.Height > 0 ? desc.Height : _height,
                        Depth = desc.Depth > 0 ? desc.Depth : 1,
                        Format = desc.Format,
                        MipLevels = desc.MipLevels > 0 ? desc.MipLevels : 1,
                        ArraySize = desc.ArraySize > 0 ? desc.ArraySize : 1,
                        Usages = desc.Usages,
                        Descriptor = desc.Descriptor,
                        HeapType = HeapType.Gpu,
                        InitialUsage = (uint)ResourceUsageFlagBits.Common,
                        DebugName = StringView.Create(desc.DebugName ?? "TransientTexture")
                    });
                    _resourceTracking.TrackTexture(texture, (uint)ResourceUsageFlagBits.Common, QueueType.Graphics);
                    transientTextures.Add(texture);
                    entry.Texture = texture;
                }
                textureIndex++;
            }
            else if (entry.Type == RenderGraphResourceType.Buffer)
            {
                if (bufferIndex < transientBuffers.Count)
                {
                    entry.Buffer = transientBuffers[bufferIndex];
                }
                else
                {
                    var desc = entry.BufferDesc;
                    var buffer = _logicalDevice.CreateBufferResource(new BufferDesc
                    {
                        NumBytes = desc.NumBytes,
                        Usages = desc.Usages,
                        Descriptor = desc.Descriptor,
                        HeapType = desc.HeapType,
                        DebugName = StringView.Create(desc.DebugName ?? "TransientBuffer")
                    });
                    transientBuffers.Add(buffer);
                    entry.Buffer = buffer;
                }
                bufferIndex++;
            }
        }
    }

    void AssignCommandLists()
    {
        var commandLists = _commandListPools[_currentFrameIndex].GetCommandLists().ToArray();
        var semaphores = _frameSemaphores[_currentFrameIndex];

        var cmdIndex = 0;
        for (var i = 0; i < _executionOrder.Count; i++)
        {
            var pass = _passes[_executionOrder[i]];
            if (pass.IsCulled)
                continue;

            pass.CommandList = commandLists[cmdIndex];
            pass.CompletionSemaphore = semaphores[cmdIndex];
            cmdIndex++;
        }
    }

    void SubmitCommandLists()
    {
        for (var i = 0; i < _executionOrder.Count; i++)
        {
            var pass = _passes[_executionOrder[i]];
            if (pass.IsCulled || pass.CommandList == null)
                continue;

            var waitCount = 0;
            foreach (var depIdx in pass.DependsOnPasses)
            {
                var depPass = _passes[depIdx];
                if (!depPass.IsCulled && depPass.CompletionSemaphore != null)
                    _waitSemaphoresBuffer[waitCount++] = depPass.CompletionSemaphore;
            }

            _signalSemaphoresBuffer[0] = pass.CompletionSemaphore!;

            using var commandListArray = CommandListArray.Create([pass.CommandList]);
            using var waitSemaphores = SemaphoreArray.Create(_waitSemaphoresBuffer[..waitCount]);
            using var signalSemaphores = SemaphoreArray.Create(_signalSemaphoresBuffer);

            var submitDesc = new ExecuteCommandListsDesc
            {
                CommandLists = commandListArray.Value,
                WaitSemaphores = waitSemaphores.Value,
                SignalSemaphores = signalSemaphores.Value
            };

            if (i == _executionOrder.Count - 1)
                submitDesc.Signal = _frameFences[_currentFrameIndex];

            _commandQueue.ExecuteCommandLists(submitDesc);
        }
    }

    public void WaitIdle()
    {
        _commandQueue.WaitIdle();
        for (var i = 0; i < _numFrames; i++)
            _frameFences[i].Wait();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        WaitIdle();

        for (var i = 0; i < _numFrames; i++)
        {
            foreach (var texture in _transientTextures[i])
                texture.Dispose();
            foreach (var buffer in _transientBuffers[i])
                buffer.Dispose();
            foreach (var semaphore in _frameSemaphores[i])
                semaphore.Dispose();
            _frameFences[i].Dispose();
            _commandListPools[i].Dispose();
        }

        _resourceTracking.Dispose();
        GC.SuppressFinalize(this);
    }
}
