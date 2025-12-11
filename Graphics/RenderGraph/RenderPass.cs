using DenOfIz;
using Semaphore = DenOfIz.Semaphore;

namespace Graphics.RenderGraph;

public struct ResourceDependency
{
    public ResourceHandle Handle;
    public ResourceAccess Access;
    public uint UsageFlags;

    public static ResourceDependency Read(ResourceHandle handle, uint usageFlags = (uint)ResourceUsageFlagBits.ShaderResource) =>
        new() { Handle = handle, Access = ResourceAccess.Read, UsageFlags = usageFlags };

    public static ResourceDependency Write(ResourceHandle handle, uint usageFlags = (uint)ResourceUsageFlagBits.RenderTarget) =>
        new() { Handle = handle, Access = ResourceAccess.Write, UsageFlags = usageFlags };

    public static ResourceDependency ReadWrite(ResourceHandle handle, uint usageFlags) =>
        new() { Handle = handle, Access = ResourceAccess.ReadWrite, UsageFlags = usageFlags };
}

public ref struct RenderPassSetupContext
{
    public RenderGraph Graph;
    public uint Width;
    public uint Height;
    public uint FrameIndex;
}

public ref struct RenderPassExecuteContext
{
    public RenderGraph Graph;
    public CommandList CommandList;
    public ResourceTracking ResourceTracking;
    public uint Width;
    public uint Height;
    public uint FrameIndex;

    public TextureResource GetTexture(ResourceHandle handle) => Graph.GetTexture(handle);
    public BufferResource GetBuffer(ResourceHandle handle) => Graph.GetBuffer(handle);
}

public delegate void RenderPassSetupDelegate(ref RenderPassSetupContext context, ref PassBuilder builder);
public delegate void RenderPassExecuteDelegate(ref RenderPassExecuteContext context);

public ref struct PassBuilder
{
    readonly RenderPassData _passData;
    readonly RenderGraph _graph;

    internal PassBuilder(RenderPassData passData, RenderGraph graph)
    {
        _passData = passData;
        _graph = graph;
    }

    public void ReadTexture(ResourceHandle handle, uint usageFlags = (uint)ResourceUsageFlagBits.ShaderResource)
    {
        if (!handle.IsValid) return;
        _passData.AddInput(ResourceDependency.Read(handle, usageFlags));
        _graph.UpdateResourceLifetime(handle, _passData.Index);
    }

    public void WriteTexture(ResourceHandle handle, uint usageFlags = (uint)ResourceUsageFlagBits.RenderTarget)
    {
        if (!handle.IsValid) return;
        _passData.AddOutput(ResourceDependency.Write(handle, usageFlags));
        _graph.UpdateResourceLifetime(handle, _passData.Index);
    }

    public void ReadWriteTexture(ResourceHandle handle, uint usageFlags)
    {
        if (!handle.IsValid) return;
        _passData.AddInput(ResourceDependency.ReadWrite(handle, usageFlags));
        _passData.AddOutput(ResourceDependency.ReadWrite(handle, usageFlags));
        _graph.UpdateResourceLifetime(handle, _passData.Index);
    }

    public void ReadBuffer(ResourceHandle handle, uint usageFlags = (uint)ResourceUsageFlagBits.ShaderResource)
    {
        if (!handle.IsValid) return;
        _passData.AddInput(ResourceDependency.Read(handle, usageFlags));
        _graph.UpdateResourceLifetime(handle, _passData.Index);
    }

    public void WriteBuffer(ResourceHandle handle, uint usageFlags = (uint)ResourceUsageFlagBits.UnorderedAccess)
    {
        if (!handle.IsValid) return;
        _passData.AddOutput(ResourceDependency.Write(handle, usageFlags));
        _graph.UpdateResourceLifetime(handle, _passData.Index);
    }

    public ResourceHandle CreateTransientTexture(TransientTextureDesc desc)
    {
        var handle = _graph.CreateTransientTexture(desc);
        _graph.UpdateResourceLifetime(handle, _passData.Index);
        return handle;
    }

    public ResourceHandle CreateTransientBuffer(TransientBufferDesc desc)
    {
        var handle = _graph.CreateTransientBuffer(desc);
        _graph.UpdateResourceLifetime(handle, _passData.Index);
        return handle;
    }

    public void HasSideEffects() => _passData.HasSideEffects = true;
    public void UseAsyncCompute() => _passData.QueueType = QueueType.Compute;
}

internal class RenderPassData
{
    public int Index;
    public string Name = "";
    public RenderPassExecuteDelegate? Execute;
    public QueueType QueueType = QueueType.Graphics;
    public bool HasSideEffects;
    public bool IsCulled;

    readonly List<ResourceDependency> _inputs = new(8);
    readonly List<ResourceDependency> _outputs = new(8);

    public int ExecutionOrder = -1;
    public readonly List<int> DependsOnPasses = new(8);
    public readonly List<int> DependentPasses = new(8);

    public Semaphore? CompletionSemaphore;
    public CommandList? CommandList;

    public IReadOnlyList<ResourceDependency> Inputs => _inputs;
    public IReadOnlyList<ResourceDependency> Outputs => _outputs;

    public void AddInput(ResourceDependency dep) => _inputs.Add(dep);
    public void AddOutput(ResourceDependency dep) => _outputs.Add(dep);

    public void Reset()
    {
        Name = "";
        Execute = null;
        QueueType = QueueType.Graphics;
        HasSideEffects = false;
        IsCulled = false;
        _inputs.Clear();
        _outputs.Clear();
        ExecutionOrder = -1;
        DependsOnPasses.Clear();
        DependentPasses.Clear();
        CompletionSemaphore = null;
        CommandList = null;
    }
}
