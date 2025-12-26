using DenOfIz;
using Graphics.Binding;

namespace Graphics.RenderGraph;

public class RgCommandList
{
    private readonly FrequencyShaderBindingPools _freqBindingPools;
    private readonly BindGroupData _reusableBindGroupData = new();

    private CommandList _commandList = null!;
    private DrawState _drawState;
    private int _currentFrame;
    private Pipeline? _currentPipeline;
    private bool _isRendering;
    private int _drawId;

    public RgCommandList(LogicalDevice logicalDevice)
    {
        _freqBindingPools = new FrequencyShaderBindingPools(logicalDevice);
    }

    public void Begin(CommandList commandList, int frameIndex)
    {
        _commandList = commandList;
        _currentFrame = frameIndex;
        _drawState = new DrawState();
        _drawId = 0;
        _currentPipeline = null;
        _isRendering = false;
    }

    public void End()
    {
        if (_isRendering)
        {
            _commandList.EndRendering();
            _isRendering = false;
        }
        _commandList = null!;
    }

    public int CurrentFrame => _currentFrame;

    public void BeginRendering(RenderingDesc desc)
    {
        if (_isRendering)
        {
            _commandList.EndRendering();
        }
        _commandList.BeginRendering(desc);
        _isRendering = true;
    }

    public void EndRendering()
    {
        if (_isRendering)
        {
            _commandList.EndRendering();
            _isRendering = false;
        }
    }

    public void SetShader(Shader shader, string variant = "default")
    {
        _drawState.Shader = shader;
        _drawState.Variant = variant;
    }

    public void SetData(string name, byte[] data)
    {
        _drawState.Resources[name] = new DrawState.Resource(data);
    }

    public unsafe void SetData<T>(string name, in T value) where T : unmanaged
    {
        var size = sizeof(T);
        var data = new byte[size];
        fixed (byte* ptr = data)
        fixed (T* src = &value)
        {
            System.Buffer.MemoryCopy(src, ptr, size, size);
        }
        _drawState.Resources[name] = new DrawState.Resource(data);
    }

    public unsafe void SetData<T>(string name, ReadOnlySpan<T> values) where T : unmanaged
    {
        var size = sizeof(T) * values.Length;
        var data = new byte[size];
        fixed (byte* dst = data)
        fixed (T* src = values)
        {
            System.Buffer.MemoryCopy(src, dst, size, size);
        }
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
            throw new InvalidOperationException($"Pipeline with variant '{_drawState.Variant}' does not exist.");
        }

        if (_currentPipeline == null || pipeline != _currentPipeline)
        {
            _currentPipeline = pipeline;
            _commandList.BindPipeline(_currentPipeline);
        }

        FlushBindings();

        _commandList.BindVertexBuffer(mesh.VertexBuffer.GetBuffer(), mesh.VertexBuffer.Offset, mesh.VertexStride, 0);
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
        var shaderBindingPools = _freqBindingPools.GetOrCreateBindingPools(rootSignature, _currentFrame);

        foreach (var registerSpace in rootSignature.GetRegisterSpaces())
        {
            var shaderBindingPool = shaderBindingPools[(int)registerSpace];
            if (shaderBindingPool == null)
            {
                throw new InvalidOperationException(
                    $"ShaderBindingPool for register space {registerSpace} does not exist.");
            }

            ShaderBinding shaderBinding;
            _drawState.BuildBindGroupData(rootSignature, registerSpace, _reusableBindGroupData);

            if (registerSpace == (uint)BindingFrequency.PerDraw)
            {
                shaderBinding = shaderBindingPool.GetByIndex(_drawId);
                if (!_reusableBindGroupData.IsEmpty)
                {
                    shaderBinding.ApplyBindGroupData(_reusableBindGroupData);
                }
            }
            else
            {
                if (_reusableBindGroupData.IsEmpty)
                {
                    continue;
                }
                shaderBinding = shaderBindingPool.GetOrCreate(_reusableBindGroupData);
            }

            _commandList.BindResourceGroup(shaderBinding.BindGroup);
        }

        _drawId++;
    }

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
}
