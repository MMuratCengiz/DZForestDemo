using DenOfIz;
using NiziKit.Graphics.Binding;

namespace NiziKit.Graphics.Renderer.Pass;

public abstract class RenderPass : IDisposable
{
    protected readonly CommandList _commandList;
    protected bool _isRecording;

    private readonly PinnedArray<MemoryBarrierDesc> _uavMemoryBarrier;
    private readonly PipelineBarrierDesc _uavBarrierDesc;

    public CommandList CommandList => _commandList;

    protected RenderPass(CommandList commandList)
    {
        _commandList = commandList;

        _uavMemoryBarrier = new PinnedArray<MemoryBarrierDesc>(1);
        _uavMemoryBarrier[0] = new MemoryBarrierDesc
        {
            OldState = (uint)ResourceUsageFlagBits.UnorderedAccess,
            NewState = (uint)ResourceUsageFlagBits.UnorderedAccess
        };

        _uavBarrierDesc = new PipelineBarrierDesc
        {
            MemoryBarriers = MemoryBarrierDescArray.FromPinned(_uavMemoryBarrier.Handle, 1)
        };
    }

    public void Begin()
    {
        if (_isRecording)
        {
            throw new InvalidOperationException("Pass is already recording");
        }

        _commandList.Begin();
        _isRecording = true;
        BeginInternal();
    }

    public void End()
    {
        if (!_isRecording)
        {
            throw new InvalidOperationException("Pass is not recording");
        }

        EndInternal();
        _commandList.End();
        _isRecording = false;
    }

    protected virtual void BeginInternal()
    {
    }

    protected virtual void EndInternal()
    {
    }

    public void Bind(IShaderBinding shaderBinding)
    {
        Bind(shaderBinding.BindGroup);
    }

    public void Bind(BindGroup bindGroup)
    {
        _commandList.BindGroup(bindGroup);
    }

    public void BindPipeline(Pipeline pipeline)
    {
        _commandList.BindPipeline(pipeline);
    }

    public void SetRootConstants<T>(uint binding, in T data) where T : unmanaged
    {
        _commandList.SetRootConstants(binding, in data);
    }

    public void PipelineBarrier(in PipelineBarrierDesc barrier)
    {
        _commandList.PipelineBarrier(in barrier);
    }

    public void TransitionTexture(Texture texture, uint newState, QueueType queueType = QueueType.Graphics)
    {
        GraphicsContext.ResourceTracking.TransitionTexture(_commandList, texture, newState, queueType);
    }

    public void TransitionBuffer(DenOfIz.Buffer buffer, uint newState, QueueType queueType = QueueType.Graphics)
    {
        GraphicsContext.ResourceTracking.TransitionBuffer(_commandList, buffer, newState, queueType);
    }

    public void UavBarrier()
    {
        _commandList.PipelineBarrier(in _uavBarrierDesc);
    }

    public void UavBarrier(Texture texture)
    {
        TransitionTexture(texture, (uint)ResourceUsageFlagBits.UnorderedAccess);
    }

    public void UavBarrier(DenOfIz.Buffer buffer)
    {
        TransitionBuffer(buffer, (uint)ResourceUsageFlagBits.UnorderedAccess);
    }

    public void CopyBufferRegion(in CopyBufferRegionDesc desc)
    {
        _commandList.CopyBufferRegion(in desc);
    }

    public void CopyTextureRegion(in CopyTextureRegionDesc desc)
    {
        _commandList.CopyTextureRegion(in desc);
    }

    public void CopyBufferToTexture(in CopyBufferToTextureDesc desc)
    {
        _commandList.CopyBufferToTexture(in desc);
    }

    public void CopyTextureToBuffer(in CopyTextureToBufferDesc desc)
    {
        _commandList.CopyTextureToBuffer(in desc);
    }

    public void BeginDebugMarker(float r, float g, float b, string name)
    {
        _commandList.BeginDebugMarker(r, g, b, StringView.Intern(name));
    }

    public void EndDebugMarker()
    {
        _commandList.EndDebugMarker();
    }

    public void InsertDebugMarker(float r, float g, float b, string name)
    {
        _commandList.InsertDebugMarker(r, g, b, StringView.Intern(name));
    }

    public void BeginQuery(QueryPool queryPool, in QueryDesc queryDesc)
    {
        _commandList.BeginQuery(queryPool, in queryDesc);
    }

    public void EndQuery(QueryPool queryPool, in QueryDesc queryDesc)
    {
        _commandList.EndQuery(queryPool, in queryDesc);
    }

    public void ResolveQuery(QueryPool queryPool, uint startQuery, uint queryCount)
    {
        _commandList.ResolveQuery(queryPool, startQuery, queryCount);
    }

    public void ResetQuery(QueryPool queryPool, uint startQuery, uint queryCount)
    {
        _commandList.ResetQuery(queryPool, startQuery, queryCount);
    }

    public abstract void Reset();

    public virtual void Dispose()
    {
        _uavMemoryBarrier.Dispose();
    }
}
