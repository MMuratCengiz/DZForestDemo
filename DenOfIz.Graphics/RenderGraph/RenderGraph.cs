using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using DenOfIz;
using Buffer = DenOfIz.Buffer;
using Semaphore = DenOfIz.Semaphore;

namespace Graphics.RenderGraph;

public struct RenderGraphDesc(LogicalDevice logicalDevice, CommandQueue commandQueue)
{
    public LogicalDevice LogicalDevice = logicalDevice;
    public CommandQueue CommandQueue = commandQueue;
    public uint NumFrames = 3;
    public const uint MaxPasses = 64;
    public const uint MaxResources = 256;
}

public class RenderGraph : IDisposable
{
    private readonly Queue<int> _algorithmQueue;
    private readonly CommandList[][] _cachedCommandLists;
    private readonly ulong[] _commandListHandles;

    private readonly CommandListPool[] _commandListPools;
    private readonly CommandQueue _commandQueue;
    private readonly List<int> _executionOrder;
    private readonly Fence[] _frameFences;
    private readonly List<Semaphore>[] _frameSemaphores;
    private readonly LogicalDevice _logicalDevice;
    private readonly uint _maxPasses;
    private readonly uint _maxResources;
    private readonly Dictionary<string, ResourceHandle> _namedResources;
    private readonly uint _numFrames;

    private readonly List<RenderPassData> _passes;

    private readonly List<RenderGraphResourceEntry> _resources;
    private readonly ulong[] _signalSemaphoreHandles;
    private readonly Semaphore[] _signalSemaphoresBuffer;
    private readonly List<Buffer>[] _transientBuffers;

    private readonly List<Texture>[] _transientTextures;
    private readonly ulong[] _waitSemaphoreHandles;

    private readonly Semaphore[] _waitSemaphoresBuffer;
    private GCHandle _commandListPin;

    private bool _disposed;
    private bool _isCompiled;
    private bool _isFrameActive;
    private int _passCount;
    private int _resourceCount;
    private GCHandle _signalSemaphorePin;
    private GCHandle _waitSemaphorePin;

    public RenderGraph(RenderGraphDesc desc)
    {
        _logicalDevice = desc.LogicalDevice;
        _commandQueue = desc.CommandQueue;
        _numFrames = desc.NumFrames;
        _maxPasses = RenderGraphDesc.MaxPasses;
        _maxResources = RenderGraphDesc.MaxResources;

        ResourceTracking = new ResourceTracking();

        _resources = new List<RenderGraphResourceEntry>((int)_maxResources);
        for (var i = 0; i < _maxResources; i++)
        {
            _resources.Add(new RenderGraphResourceEntry());
        }

        _namedResources = new Dictionary<string, ResourceHandle>((int)_maxResources);

        _passes = new List<RenderPassData>((int)_maxPasses);
        for (var i = 0; i < _maxPasses; i++)
        {
            _passes.Add(new RenderPassData { Index = i });
        }

        _executionOrder = new List<int>((int)_maxPasses);

        _commandListPools = new CommandListPool[_numFrames];
        _frameSemaphores = new List<Semaphore>[_numFrames];
        _frameFences = new Fence[_numFrames];
        _transientTextures = new List<Texture>[_numFrames];
        _transientBuffers = new List<Buffer>[_numFrames];

        for (var i = 0; i < _numFrames; i++)
        {
            _commandListPools[i] = _logicalDevice.CreateCommandListPool(new CommandListPoolDesc
            {
                CommandQueue = _commandQueue,
                NumCommandLists = _maxPasses
            });
            _frameSemaphores[i] = new List<Semaphore>((int)_maxPasses);
            for (var j = 0; j < _maxPasses; j++)
            {
                _frameSemaphores[i].Add(_logicalDevice.CreateSemaphore());
            }

            _frameFences[i] = _logicalDevice.CreateFence();
            _transientTextures[i] = new List<Texture>(32);
            _transientBuffers[i] = new List<Buffer>(32);
        }

        _waitSemaphoresBuffer = new Semaphore[_maxPasses];
        _signalSemaphoresBuffer = new Semaphore[1];
        _commandListHandles = new ulong[1];
        _waitSemaphoreHandles = new ulong[_maxPasses];
        _signalSemaphoreHandles = new ulong[1];
        _commandListPin = GCHandle.Alloc(_commandListHandles, GCHandleType.Pinned);
        _waitSemaphorePin = GCHandle.Alloc(_waitSemaphoreHandles, GCHandleType.Pinned);
        _signalSemaphorePin = GCHandle.Alloc(_signalSemaphoreHandles, GCHandleType.Pinned);
        _algorithmQueue = new Queue<int>((int)_maxPasses);
        _cachedCommandLists = new CommandList[_numFrames][];
        for (var i = 0; i < _numFrames; i++)
        {
            _cachedCommandLists[i] = _commandListPools[i].GetCommandLists().ToArray()!;
        }
    }

    public uint Width { get; private set; }

    public uint Height { get; private set; }

    public uint CurrentFrameIndex { get; private set; }

    public ResourceTracking ResourceTracking { get; }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        WaitIdle();

        for (var i = 0; i < _numFrames; i++)
        {
            foreach (var texture in _transientTextures[i])
            {
                texture.Dispose();
            }

            foreach (var buffer in _transientBuffers[i])
            {
                buffer.Dispose();
            }

            foreach (var semaphore in _frameSemaphores[i])
            {
                semaphore.Dispose();
            }

            _frameFences[i].Dispose();
            _commandListPools[i].Dispose();
        }

        ResourceTracking.Dispose();
        if (_commandListPin.IsAllocated)
        {
            _commandListPin.Free();
        }

        if (_waitSemaphorePin.IsAllocated)
        {
            _waitSemaphorePin.Free();
        }

        if (_signalSemaphorePin.IsAllocated)
        {
            _signalSemaphorePin.Free();
        }

        GC.SuppressFinalize(this);
    }

    public void SetDimensions(uint width, uint height)
    {
        Width = width;
        Height = height;
    }

    public void BeginFrame(uint frameIndex)
    {
        if (_isFrameActive)
        {
            throw new InvalidOperationException("Frame already active. Call Execute() first.");
        }

        CurrentFrameIndex = frameIndex;
        _frameFences[CurrentFrameIndex].Wait();
        _frameFences[CurrentFrameIndex].Reset();

        ResetFrame();
        _isFrameActive = true;
        _isCompiled = false;
    }

    public ResourceHandle ImportTexture(string name, Texture texture)
    {
        if (_resourceCount >= _maxResources)
        {
            throw new InvalidOperationException("Maximum resource count exceeded");
        }

        var entry = _resources[_resourceCount];
        entry.Reset();
        entry.Type = RenderGraphResourceType.Texture;
        entry.IsImported = true;
        entry.Texture = texture;

        var handle = new ResourceHandle((uint)_resourceCount, entry.Version);
        _namedResources[name] = handle;
        _resourceCount++;
        return handle;
    }

    public ResourceHandle ImportBuffer(string name, Buffer buffer)
    {
        if (_resourceCount >= _maxResources)
        {
            throw new InvalidOperationException("Maximum resource count exceeded");
        }

        var entry = _resources[_resourceCount];
        entry.Reset();
        entry.Type = RenderGraphResourceType.Buffer;
        entry.IsImported = true;
        entry.Buffer = buffer;

        var handle = new ResourceHandle((uint)_resourceCount, entry.Version);
        _namedResources[name] = handle;
        _resourceCount++;
        return handle;
    }

    public ResourceHandle CreateTransientTexture(TransientTextureDesc desc)
    {
        if (_resourceCount >= _maxResources)
        {
            throw new InvalidOperationException("Maximum resource count exceeded");
        }

        var entry = _resources[_resourceCount];
        entry.Reset();
        entry.Type = RenderGraphResourceType.Texture;
        entry.IsTransient = true;
        entry.TextureDesc = desc;

        var handle = new ResourceHandle((uint)_resourceCount, entry.Version);
        if (!string.IsNullOrEmpty(desc.DebugName))
        {
            _namedResources[desc.DebugName] = handle;
        }

        _resourceCount++;
        return handle;
    }

    internal ResourceHandle CreateTransientBuffer(TransientBufferDesc desc)
    {
        if (_resourceCount >= _maxResources)
        {
            throw new InvalidOperationException("Maximum resource count exceeded");
        }

        var entry = _resources[_resourceCount];
        entry.Reset();
        entry.Type = RenderGraphResourceType.Buffer;
        entry.IsTransient = true;
        entry.BufferDesc = desc;

        var handle = new ResourceHandle((uint)_resourceCount, entry.Version);
        if (!string.IsNullOrEmpty(desc.DebugName))
        {
            _namedResources[desc.DebugName] = handle;
        }

        _resourceCount++;
        return handle;
    }

    public ResourceHandle GetResource(string name)
    {
        return _namedResources.TryGetValue(name, out var handle) ? handle : ResourceHandle.Invalid;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Texture GetTexture(ResourceHandle handle)
    {
        if (!handle.IsValid || handle.Index >= _resourceCount)
        {
            throw new ArgumentException("Invalid resource handle");
        }

        var entry = _resources[(int)handle.Index];
        if (entry.Version != handle.Version)
        {
            throw new ArgumentException("Stale resource handle");
        }

        return entry.Texture ?? throw new InvalidOperationException("Resource is not a texture or not allocated");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Buffer GetBuffer(ResourceHandle handle)
    {
        if (!handle.IsValid || handle.Index >= _resourceCount)
        {
            throw new ArgumentException("Invalid resource handle");
        }

        var entry = _resources[(int)handle.Index];
        if (entry.Version != handle.Version)
        {
            throw new ArgumentException("Stale resource handle");
        }

        return entry.Buffer ?? throw new InvalidOperationException("Resource is not a buffer or not allocated");
    }

    public void AddPass(string name, RenderPassSetupDelegate setup, RenderPassExecuteDelegate execute)
    {
        if (_passCount >= _maxPasses)
        {
            throw new InvalidOperationException("Maximum pass count exceeded");
        }

        var passData = _passes[_passCount];
        passData.Reset();
        passData.Index = _passCount;
        passData.Name = name;
        passData.Execute = execute;

        var setupContext = new RenderPassSetupContext
        {
            Graph = this,
            Width = Width,
            Height = Height,
            FrameIndex = CurrentFrameIndex
        };

        var builder = new PassBuilder(passData, this);
        setup(ref setupContext, ref builder);
        _passCount++;
    }

    public void AddPass(string name, RenderPassExecuteDelegate execute)
    {
        AddPass(name, static (ref RenderPassSetupContext _, ref PassBuilder builder) => { builder.HasSideEffects(); },
            execute);
    }

    public ResourceHandle AddExternalPass(string name, ExternalPassExecuteDelegate execute,
        TransientTextureDesc outputDesc)
    {
        if (_passCount >= _maxPasses)
        {
            throw new InvalidOperationException("Maximum pass count exceeded");
        }

        var passData = _passes[_passCount];
        passData.Reset();
        passData.Index = _passCount;
        passData.Name = name;
        passData.ExternalExecute = execute;
        passData.IsExternal = true;
        passData.HasSideEffects = true;

        var outputHandle = CreateTransientTexture(outputDesc);
        passData.ExternalOutputHandle = outputHandle;
        UpdateResourceLifetime(outputHandle, _passCount);

        _passCount++;
        return outputHandle;
    }

    public ResourceHandle AddExternalPass(string name, RenderPassSetupDelegate setup,
        ExternalPassExecuteDelegate execute, TransientTextureDesc outputDesc)
    {
        if (_passCount >= _maxPasses)
        {
            throw new InvalidOperationException("Maximum pass count exceeded");
        }

        var passData = _passes[_passCount];
        passData.Reset();
        passData.Index = _passCount;
        passData.Name = name;
        passData.ExternalExecute = execute;
        passData.IsExternal = true;
        passData.HasSideEffects = true;

        var setupContext = new RenderPassSetupContext
        {
            Graph = this,
            Width = Width,
            Height = Height,
            FrameIndex = CurrentFrameIndex
        };

        var builder = new PassBuilder(passData, this);
        setup(ref setupContext, ref builder);

        var outputHandle = CreateTransientTexture(outputDesc);
        passData.ExternalOutputHandle = outputHandle;
        UpdateResourceLifetime(outputHandle, _passCount - 1);

        _passCount++;
        return outputHandle;
    }

    internal void SetExternalTexture(ResourceHandle handle, Texture texture)
    {
        if (!handle.IsValid || handle.Index >= _resourceCount)
        {
            return;
        }

        var entry = _resources[(int)handle.Index];
        entry.Texture = texture;
    }

    internal Semaphore? GetExternalSemaphore(ResourceHandle handle)
    {
        for (var i = 0; i < _passCount; i++)
        {
            var pass = _passes[i];
            if (pass.IsExternal && pass.ExternalOutputHandle == handle)
            {
                return pass.ExternalResult.Semaphore;
            }
        }

        return null;
    }

    internal void UpdateResourceLifetime(ResourceHandle handle, int passIndex)
    {
        if (!handle.IsValid || handle.Index >= _resourceCount)
        {
            return;
        }

        var entry = _resources[(int)handle.Index];
        if (entry.FirstPassIndex < 0)
        {
            entry.FirstPassIndex = passIndex;
        }

        entry.LastPassIndex = passIndex;
    }

    public void Compile()
    {
        if (!_isFrameActive)
        {
            throw new InvalidOperationException("No active frame. Call BeginFrame() first.");
        }

        if (_isCompiled)
        {
            return;
        }

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
        {
            throw new InvalidOperationException("No active frame. Call BeginFrame() first.");
        }

        if (!_isCompiled)
        {
            Compile();
        }

        for (var i = 0; i < _executionOrder.Count; i++)
        {
            var passData = _passes[_executionOrder[i]];
            if (passData.IsCulled)
            {
                continue;
            }

            if (passData.IsExternal)
            {
                if (passData.ExternalExecute == null)
                {
                    continue;
                }

                var externalContext = new ExternalPassExecuteContext
                {
                    Graph = this,
                    Width = Width,
                    Height = Height,
                    FrameIndex = CurrentFrameIndex
                };

                passData.ExternalResult = passData.ExternalExecute(ref externalContext);
                SetExternalTexture(passData.ExternalOutputHandle, passData.ExternalResult.Texture);
                continue;
            }

            if (passData.Execute == null || passData.CommandList == null)
            {
                continue;
            }

            var commandList = passData.CommandList;
            commandList.Begin();

            var context = new RenderPassExecuteContext
            {
                Graph = this,
                CommandList = commandList,
                ResourceTracking = ResourceTracking,
                Width = Width,
                Height = Height,
                FrameIndex = CurrentFrameIndex
            };

            passData.Execute(ref context);
            commandList.End();
        }

        SubmitCommandLists();
        _isFrameActive = false;
    }

    private void ResetFrame()
    {
        _resourceCount = 0;
        _namedResources.Clear();
        _passCount = 0;
        _executionOrder.Clear();
    }

    private void BuildDependencyGraph()
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
                var writerPass = lastWriter[(int)input.Handle.Index];
                if (writerPass >= 0 && writerPass != passIdx && !pass.DependsOnPasses.Contains(writerPass))
                {
                    pass.DependsOnPasses.Add(writerPass);
                    _passes[writerPass].DependentPasses.Add(passIdx);
                }
            }

            foreach (var output in pass.Outputs)
            {
                lastWriter[(int)output.Handle.Index] = passIdx;
            }
        }
    }

    private void CullPasses()
    {
        for (var i = 0; i < _passCount; i++)
        {
            _passes[i].IsCulled = true;
        }

        _algorithmQueue.Clear();
        for (var i = 0; i < _passCount; i++)
        {
            if (_passes[i].HasSideEffects)
            {
                _passes[i].IsCulled = false;
                _algorithmQueue.Enqueue(i);
            }
        }

        while (_algorithmQueue.Count > 0)
        {
            var passIdx = _algorithmQueue.Dequeue();
            foreach (var depIdx in _passes[passIdx].DependsOnPasses)
            {
                if (_passes[depIdx].IsCulled)
                {
                    _passes[depIdx].IsCulled = false;
                    _algorithmQueue.Enqueue(depIdx);
                }
            }
        }
    }

    private void TopologicalSort()
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
            {
                if (!_passes[dep].IsCulled)
                {
                    count++;
                }
            }

            inDegree[i] = count;
        }

        _algorithmQueue.Clear();
        for (var i = 0; i < _passCount; i++)
        {
            if (inDegree[i] == 0)
            {
                _algorithmQueue.Enqueue(i);
            }
        }

        var order = 0;
        while (_algorithmQueue.Count > 0)
        {
            var passIdx = _algorithmQueue.Dequeue();
            var pass = _passes[passIdx];

            pass.ExecutionOrder = order++;
            _executionOrder.Add(passIdx);

            foreach (var depIdx in pass.DependentPasses)
            {
                if (_passes[depIdx].IsCulled)
                {
                    continue;
                }

                inDegree[depIdx]--;
                if (inDegree[depIdx] == 0)
                {
                    _algorithmQueue.Enqueue(depIdx);
                }
            }
        }

        var culledCount = 0;
        for (var i = 0; i < _passCount; i++)
        {
            if (_passes[i].IsCulled)
            {
                culledCount++;
            }
        }

        if (_executionOrder.Count < _passCount - culledCount)
        {
            throw new InvalidOperationException("Circular dependency detected in render graph");
        }
    }

    private void AllocateTransientResources()
    {
        var transientTextures = _transientTextures[CurrentFrameIndex];
        var transientBuffers = _transientBuffers[CurrentFrameIndex];
        var textureIndex = 0;
        var bufferIndex = 0;

        for (var i = 0; i < _resourceCount; i++)
        {
            var entry = _resources[i];
            if (!entry.IsTransient || entry.FirstPassIndex < 0 || _passes[entry.FirstPassIndex].IsCulled)
            {
                continue;
            }

            if (entry.Type == RenderGraphResourceType.Texture)
            {
                if (textureIndex < transientTextures.Count)
                {
                    entry.Texture = transientTextures[textureIndex];
                }
                else
                {
                    var desc = entry.TextureDesc;
                    var texture = _logicalDevice.CreateTexture(new TextureDesc
                    {
                        Width = desc.Width > 0 ? desc.Width : Width,
                        Height = desc.Height > 0 ? desc.Height : Height,
                        Depth = desc.Depth > 0 ? desc.Depth : 1,
                        Format = desc.Format,
                        MipLevels = desc.MipLevels > 0 ? desc.MipLevels : 1,
                        ArraySize = desc.ArraySize > 0 ? desc.ArraySize : 1,
                        Usage = desc.Usage,
                        HeapType = HeapType.Gpu,
                        DebugName = StringView.Intern(desc.DebugName ?? "TransientTexture")
                    });
                    ResourceTracking.TrackTexture(texture, QueueType.Graphics);
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
                    var buffer = _logicalDevice.CreateBuffer(new BufferDesc
                    {
                        NumBytes = desc.NumBytes,
                        Usage = desc.Usage,
                        HeapType = desc.HeapType,
                        DebugName = StringView.Intern(desc.DebugName ?? "TransientBuffer")
                    });
                    transientBuffers.Add(buffer);
                    entry.Buffer = buffer;
                }

                bufferIndex++;
            }
        }
    }

    private void AssignCommandLists()
    {
        var commandLists = _cachedCommandLists[CurrentFrameIndex];
        var semaphores = _frameSemaphores[CurrentFrameIndex];

        var cmdIndex = 0;
        for (var i = 0; i < _executionOrder.Count; i++)
        {
            var pass = _passes[_executionOrder[i]];
            if (pass.IsCulled)
            {
                continue;
            }

            pass.CommandList = commandLists[cmdIndex];
            pass.CompletionSemaphore = semaphores[cmdIndex];
            cmdIndex++;
        }
    }

    private void SubmitCommandLists()
    {
        var lastSubmittedIndex = -1;
        for (var i = _executionOrder.Count - 1; i >= 0; i--)
        {
            var pass = _passes[_executionOrder[i]];
            if (pass is { IsCulled: false, IsExternal: false, CommandList: not null })
            {
                lastSubmittedIndex = i;
                break;
            }
        }

        for (var i = 0; i < _executionOrder.Count; i++)
        {
            var pass = _passes[_executionOrder[i]];
            if (pass.IsCulled || pass.IsExternal || pass.CommandList == null)
            {
                continue;
            }

            var waitCount = 0;
            foreach (var depIdx in pass.DependsOnPasses)
            {
                var depPass = _passes[depIdx];
                if (depPass.IsCulled)
                {
                    continue;
                }

                if (depPass is { IsExternal: true, ExternalResult.Semaphore: not null })
                {
                    _waitSemaphoresBuffer[waitCount++] = depPass.ExternalResult.Semaphore;
                }
                else if (depPass is { IsExternal: false, CompletionSemaphore: not null })
                {
                    _waitSemaphoresBuffer[waitCount++] = depPass.CompletionSemaphore;
                }
            }

            _signalSemaphoresBuffer[0] = pass.CompletionSemaphore!;
            _commandListHandles[0] = pass.CommandList;
            for (var w = 0; w < waitCount; w++)
            {
                _waitSemaphoreHandles[w] = _waitSemaphoresBuffer[w];
            }

            _signalSemaphoreHandles[0] = pass.CompletionSemaphore!;
            var commandListArray = CommandListArray.FromPinned(_commandListPin, 1);
            var waitSemaphores = SemaphoreArray.FromPinned(_waitSemaphorePin, waitCount);
            var signalSemaphores = SemaphoreArray.FromPinned(_signalSemaphorePin, 1);

            var submitDesc = new ExecuteCommandListsDesc
            {
                CommandLists = commandListArray,
                WaitSemaphores = waitSemaphores,
                SignalSemaphores = signalSemaphores
            };

            if (i == lastSubmittedIndex)
            {
                submitDesc.Signal = _frameFences[CurrentFrameIndex];
            }

            _commandQueue.ExecuteCommandLists(submitDesc);
        }
    }

    public void WaitIdle()
    {
        _commandQueue.WaitIdle();
        for (var i = 0; i < _numFrames; i++)
        {
            _frameFences[i].Wait();
        }
    }
}