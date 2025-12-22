using DenOfIz;
using Graphics.Binding;

namespace Graphics.RenderGraph;

public class RgCommandList
{
    private const int NumFrames = 3;

    private readonly CommandQueue _commandQueue;

    // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
    // Needs to be kept alive for the lifetime of the CommandListPool
    private readonly CommandListPool _commandListPool;
    private readonly List<Fence> _fences;
    private Fence? _signalFence;
    private readonly List<CommandList> _commandLists;
    private CommandList _commandList;
    private DrawState _drawState;
    private int _nextFrame = 0;
    private int _currentFrame = 0;
    private Pipeline? _currentPipeline;
    private readonly FrequencyShaderBindingPools _freqBindingPools;
    private bool _isRendering = false;
    private int _drawId = 0;

    public RgCommandList(LogicalDevice logicalDevice, CommandQueue commandQueue)
    {
        _freqBindingPools = new FrequencyShaderBindingPools(logicalDevice);
        _commandQueue = commandQueue;
        CommandListPoolDesc commandListPoolDesc = new()
        {
            CommandQueue = commandQueue,
            NumCommandLists = 3
        };

        _commandListPool = logicalDevice.CreateCommandListPool(commandListPoolDesc);
        _commandLists = new List<CommandList>(_commandListPool.GetCommandLists().ToArray()!);
        _commandList = _commandLists[_currentFrame];
        _fences = [];
        for (var i = 0; i < 3; i++)
        {
            _fences.Add(logicalDevice.CreateFence());
        }
    }

    public void NextFrame()
    {
        _isRendering = false;
        _currentFrame = _nextFrame;
        _nextFrame = (_nextFrame + 1) % NumFrames;
        _fences[_currentFrame].Wait();
        _drawState = new DrawState();
        _drawId = 0;
        _commandList = _commandLists[_currentFrame];
        _currentPipeline = null;
        _signalFence?.Reset();
        _signalFence = null;
        _commandList.Begin();
    }

    public void BeginRendering(RenderingDesc desc)
    {
        if (_isRendering)
        {
            _commandList.EndRendering();
        }

        _commandList.BeginRendering(desc);
        _isRendering = true;
    }

    public void SetShader(Shader shader, string variant)
    {
        _drawState.Shader = shader;
        _drawState.Variant = variant;
    }

    public void SetData(string name, byte[] data)
    {
        _drawState.Resources[name] = new DrawState.Resource(data);
    }

    public void SetTexture(string name, Texture texture)
    {
        _drawState.Resources[name] = new DrawState.Resource(texture);
    }

    public void SetSampler(string name, Sampler sampler)
    {
        _drawState.Resources[name] = new DrawState.Resource(sampler);
    }

    public void SetBuffer(string name, DenOfIz.Buffer buffer, ulong offset = 0, ulong size = 0)
    {
        _drawState.Resources[name] = new DrawState.Resource(buffer, offset, size);
    }

    public void SetBuffer(string name, GPUBufferView bufferView)
    {
        _drawState.Resources[name] = new DrawState.Resource(bufferView);
    }

    public void DrawMesh(GPUMesh mesh, uint instances = 1)
    {
        Pipeline? pipeline = null;
        var newPipeline = _drawState.Shader?.TryGetPipeline(_drawState.Variant, out pipeline);
        if (!newPipeline.HasValue || !newPipeline.Value)
        {
            throw new InvalidOperationException($"Pipeline with variant{_drawState.Variant} does not exist.");
        }

        if (_currentPipeline == null || pipeline != _currentPipeline)
        {
            _currentPipeline = pipeline;
            _commandList.BindPipeline(_currentPipeline);
        }

        FlushBindings();

        _commandList.BindVertexBuffer(mesh.VertexBuffer.GetBuffer(), mesh.VertexBuffer.Offset, 0, 0);
        if (mesh.NumIndices > 0)
        {
            _commandList.BindIndexBuffer(mesh.IndexBuffer.GetBuffer(), mesh.IndexType, mesh.IndexBuffer.Offset);
            _commandList.DrawIndexed(mesh.NumIndices, instances, 0, 0, 0);
        }
        else
        {
            _commandList.Draw(mesh.NumVertices, instances, 0, 0);
        }
    }

    private void FlushBindings()
    {
        var shader = _drawState.Shader;
        if (shader == null)
        {
            return;
        }

        var rootSignature = shader.RootSignature;
        var shaderBindingPools =
            _freqBindingPools.GetOrCreateBindingPools(rootSignature, _currentFrame);

        foreach (var registerSpace in rootSignature.GetRegisterSpaces())
        {
            var shaderBindingPool = shaderBindingPools[(int)registerSpace];
            if (shaderBindingPool == null)
            {
                throw new InvalidOperationException(
                    $"ShaderBindingPool for register space {registerSpace} does not exist.");
            }

            ShaderBinding shaderBinding;
            if (registerSpace == (uint)BindingFrequency.PerDraw)
            {
                shaderBinding = shaderBindingPool.GetByIndex(_drawId);
                var bindGroupData = _drawState.BuildBindGroupData(rootSignature, registerSpace);
                if (!bindGroupData.IsEmpty)
                {
                    shaderBinding.ApplyBindGroupData(bindGroupData);
                }
            }
            else
            {
                var bindGroupData = _drawState.BuildBindGroupData(rootSignature, registerSpace);
                if (bindGroupData.IsEmpty)
                {
                    continue;
                }

                shaderBinding = shaderBindingPool.GetOrCreate(bindGroupData);
            }

            _commandList.BindResourceGroup(shaderBinding.BindGroup);
        }

        _drawId++;
    }
    // TODO DrawMeshIndirect

    // Forwarded CommandList methods
    public void BindViewport(float x, float y, float width, float height)
    {
        _commandList.BindViewport(x, y, width, height);
    }

    public void BindScissorRect(float x, float y, float width, float height)
    {
        _commandList.BindScissorRect(x, y, width, height);
    }

    public void PipelineBarrier(in PipelineBarrierDesc barrier)
    {
        _commandList.PipelineBarrier(in barrier);
    }

    public void CopyBufferRegion(in CopyBufferRegionDesc copyBufferRegionDesc)
    {
        _commandList.CopyBufferRegion(in copyBufferRegionDesc);
    }

    public void CopyTextureRegion(in CopyTextureRegionDesc copyTextureRegionDesc)
    {
        _commandList.CopyTextureRegion(in copyTextureRegionDesc);
    }

    public void CopyBufferToTexture(in CopyBufferToTextureDesc copyBufferToTexture)
    {
        _commandList.CopyBufferToTexture(in copyBufferToTexture);
    }

    public void CopyTextureToBuffer(in CopyTextureToBufferDesc copyTextureToBuffer)
    {
        _commandList.CopyTextureToBuffer(in copyTextureToBuffer);
    }

    public void UpdateTopLevelAS(in UpdateTopLevelASDesc updateDesc)
    {
        _commandList.UpdateTopLevelAS(in updateDesc);
    }

    public void BuildTopLevelAS(in BuildTopLevelASDesc buildTopLevelASDesc)
    {
        _commandList.BuildTopLevelAS(in buildTopLevelASDesc);
    }

    public void BuildBottomLevelAS(in BuildBottomLevelASDesc buildBottomLevelASDesc)
    {
        _commandList.BuildBottomLevelAS(in buildBottomLevelASDesc);
    }

    public void DispatchRays(in DispatchRaysDesc dispatchRaysDesc)
    {
        _commandList.DispatchRays(in dispatchRaysDesc);
    }

    public void Dispatch(uint groupCountX, uint groupCountY, uint groupCountZ)
    {
        _commandList.Dispatch(groupCountX, groupCountY, groupCountZ);
    }

    public void DispatchMesh(uint groupCountX, uint groupCountY, uint groupCountZ)
    {
        _commandList.DispatchMesh(groupCountX, groupCountY, groupCountZ);
    }

    public void DrawIndirect(DenOfIz.Buffer? buffer, ulong offset, uint drawCount, uint stride)
    {
        _commandList.DrawIndirect(buffer, offset, drawCount, stride);
    }

    public void DrawIndexedIndirect(DenOfIz.Buffer? buffer, ulong offset, uint drawCount, uint stride)
    {
        _commandList.DrawIndexedIndirect(buffer, offset, drawCount, stride);
    }

    public void DispatchIndirect(DenOfIz.Buffer? buffer, ulong offset)
    {
        _commandList.DispatchIndirect(buffer, offset);
    }

    public void BeginDebugMarker(float r, float g, float b, StringView name)
    {
        _commandList.BeginDebugMarker(r, g, b, name);
    }

    public void EndDebugMarker()
    {
        _commandList.EndDebugMarker();
    }

    public void InsertDebugMarker(float r, float g, float b, StringView name)
    {
        _commandList.InsertDebugMarker(r, g, b, name);
    }

    public void BeginQuery(QueryPool? queryPool, in QueryDesc queryDesc)
    {
        _commandList.BeginQuery(queryPool, in queryDesc);
    }

    public void EndQuery(QueryPool? queryPool, in QueryDesc queryDesc)
    {
        _commandList.EndQuery(queryPool, in queryDesc);
    }

    public void ResolveQuery(QueryPool? queryPool, uint startQuery, uint queryCount)
    {
        _commandList.ResolveQuery(queryPool, startQuery, queryCount);
    }

    public void ResetQuery(QueryPool? queryPool, uint startQuery, uint queryCount)
    {
        _commandList.ResetQuery(queryPool, startQuery, queryCount);
    }

    public QueueType GetQueueType()
    {
        return _commandList.GetQueueType();
    }

    public void Submit(SemaphoreArray? waitOnSemaphores = null, SemaphoreArray? signalSemaphores = null,
        Fence? fence = null)
    {
        _commandList.End();
        if (_isRendering)
        {
            _commandList.EndRendering();
        }

        _signalFence = fence;

        ExecuteCommandListsDesc executeCommandListsDesc = new()
        {
            CommandLists = CommandListArray.Create([_commandList]),
            Signal = _fences[_currentFrame],
            SignalSemaphores = signalSemaphores ?? SemaphoreArray.Create([]),
            WaitSemaphores = waitOnSemaphores ?? SemaphoreArray.Create([])
        };

        _commandQueue.ExecuteCommandLists(executeCommandListsDesc);
    }
}