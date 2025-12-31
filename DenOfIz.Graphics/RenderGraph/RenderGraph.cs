using DenOfIz;
using Buffer = DenOfIz.Buffer;
using Semaphore = DenOfIz.Semaphore;

namespace Graphics.RenderGraph;

public struct RenderGraphDesc(LogicalDevice logicalDevice, CommandQueue commandQueue, ResourceTracking resourceTracking)
{
    public LogicalDevice LogicalDevice = logicalDevice;
    public CommandQueue CommandQueue = commandQueue;
    public ResourceTracking ResourceTracking = resourceTracking;
    public uint NumFrames = 3;
    public const uint MaxPasses = 64;
    public const uint MaxResources = 256;
}

public class RenderGraph : IDisposable
{
    private readonly CommandList[][] _cachedCommandLists;
    private readonly CommandListPool[] _commandListPools;
    private readonly CommandQueue _commandQueue;
    private readonly Fence[] _frameFences;
    private readonly List<Semaphore>[] _frameSemaphores;
    private readonly LogicalDevice _logicalDevice;
    private readonly uint _maxPasses;
    private readonly uint _maxResources;
    private readonly Dictionary<string, ResourceHandle> _namedResources;
    private readonly uint _numFrames;
    private readonly List<RenderPassData> _passes;
    private readonly List<RenderGraphResourceEntry> _resources;
    private readonly List<Buffer>[] _transientBuffers;
    private readonly List<Texture>[] _transientTextures;

    // Reusable arrays for submit - avoids allocations per frame
    private readonly CommandList[] _submitCommandLists;
    private readonly Semaphore[] _submitWaitSemaphores;
    private readonly Semaphore[] _submitSignalSemaphores;

    private bool _disposed;
    private bool _isCompiled;
    private int _passCount;
    private int _resourceCount;

    public RenderGraph(RenderGraphDesc desc)
    {
        _logicalDevice = desc.LogicalDevice;
        _commandQueue = desc.CommandQueue;
        _numFrames = desc.NumFrames;
        _maxPasses = RenderGraphDesc.MaxPasses;
        _maxResources = RenderGraphDesc.MaxResources;
        ResourceTracking = desc.ResourceTracking;

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

        // Reusable arrays for submit calls
        _submitCommandLists = new CommandList[1];
        _submitWaitSemaphores = new Semaphore[_maxPasses];
        _submitSignalSemaphores = new Semaphore[1];

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

        GC.SuppressFinalize(this);
    }

    public void SetDimensions(uint width, uint height)
    {
        Width = width;
        Height = height;
    }

    public void BeginFrame(uint frameIndex)
    {
        CurrentFrameIndex = frameIndex;
        _frameFences[CurrentFrameIndex].Wait();
        _frameFences[CurrentFrameIndex].Reset();

        ResetFrame();
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
        AddPass(name, static (ref RenderPassSetupContext _, ref PassBuilder _) => { }, execute);
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

        var outputHandle = CreateTransientTexture(outputDesc);
        passData.ExternalOutputHandle = outputHandle;

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

    public void Compile()
    {
        if (_isCompiled)
        {
            return;
        }

        AllocateTransientResources();
        AssignCommandLists();
        _isCompiled = true;
    }

    public void Execute()
    {
        if (!_isCompiled)
        {
            Compile();
        }

        for (var i = 0; i < _passCount; i++)
        {
            var passData = _passes[i];

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
    }

    private void ResetFrame()
    {
        _resourceCount = 0;
        _namedResources.Clear();
        _passCount = 0;
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
            if (!entry.IsTransient)
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
                        DebugName = StringView.Intern(desc.DebugName ?? "TransientTexture"),
                        ClearColorHint = desc.ClearColorHint,
                        ClearDepthStencilHint = desc.ClearDepthStencilHint
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
        for (var i = 0; i < _passCount; i++)
        {
            var pass = _passes[i];
            if (pass.IsExternal)
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
        for (var i = _passCount - 1; i >= 0; i--)
        {
            var pass = _passes[i];
            if (pass is { IsExternal: false, CommandList: not null })
            {
                lastSubmittedIndex = i;
                break;
            }
        }

        Semaphore? prevSemaphore = null;

        for (var i = 0; i < _passCount; i++)
        {
            var pass = _passes[i];
            if (pass.IsExternal || pass.CommandList == null)
            {
                if (pass.IsExternal)
                {
                    prevSemaphore = pass.ExternalResult.Semaphore;
                }
                continue;
            }

            var waitCount = 0;
            if (prevSemaphore != null)
            {
                _submitWaitSemaphores[waitCount++] = prevSemaphore;
            }

            _submitCommandLists[0] = pass.CommandList;
            _submitSignalSemaphores[0] = pass.CompletionSemaphore!;

            var signalFence = i == lastSubmittedIndex ? _frameFences[CurrentFrameIndex] : null;

            _commandQueue.ExecuteCommandLists(
                _submitCommandLists.AsSpan(0, 1),
                signalFence,
                _submitWaitSemaphores.AsSpan(0, waitCount),
                _submitSignalSemaphores.AsSpan(0, 1));

            prevSemaphore = pass.CompletionSemaphore;
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
