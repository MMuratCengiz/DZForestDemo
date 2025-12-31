using DenOfIz;
using Buffer = DenOfIz.Buffer;
using Semaphore = DenOfIz.Semaphore;

namespace Graphics.RenderGraph;

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

    public Texture GetTexture(ResourceHandle handle)
    {
        return Graph.GetTexture(handle);
    }

    public Buffer GetBuffer(ResourceHandle handle)
    {
        return Graph.GetBuffer(handle);
    }
}

public delegate void RenderPassSetupDelegate(ref RenderPassSetupContext context, ref PassBuilder builder);

public delegate void RenderPassExecuteDelegate(ref RenderPassExecuteContext context);

public struct ExternalPassResult
{
    public Texture Texture;
    public Semaphore Semaphore;
}

public delegate ExternalPassResult ExternalPassExecuteDelegate(ref ExternalPassExecuteContext context);

public ref struct ExternalPassExecuteContext
{
    public RenderGraph Graph;
    public uint Width;
    public uint Height;
    public uint FrameIndex;

    public Texture GetTexture(ResourceHandle handle)
    {
        return Graph.GetTexture(handle);
    }

    public Buffer GetBuffer(ResourceHandle handle)
    {
        return Graph.GetBuffer(handle);
    }
}

public readonly ref struct PassBuilder
{
    private readonly RenderPassData _passData;
    private readonly RenderGraph _graph;

    internal PassBuilder(RenderPassData passData, RenderGraph graph)
    {
        _passData = passData;
        _graph = graph;
    }

    public ResourceHandle CreateTransientTexture(TransientTextureDesc desc)
    {
        return _graph.CreateTransientTexture(desc);
    }

    public ResourceHandle CreateTransientBuffer(TransientBufferDesc desc)
    {
        return _graph.CreateTransientBuffer(desc);
    }
}

internal class RenderPassData
{
    public CommandList? CommandList;
    public Semaphore? CompletionSemaphore;
    public RenderPassExecuteDelegate? Execute;
    public ExternalPassExecuteDelegate? ExternalExecute;
    public ResourceHandle ExternalOutputHandle;
    public ExternalPassResult ExternalResult;
    public int Index;
    public bool IsExternal;
    public string Name = "";

    public void Reset()
    {
        Name = "";
        Execute = null;
        ExternalExecute = null;
        IsExternal = false;
        CompletionSemaphore = null;
        CommandList = null;
        ExternalOutputHandle = ResourceHandle.Invalid;
        ExternalResult = default;
    }
}
