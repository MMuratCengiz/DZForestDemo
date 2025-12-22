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
    private bool _forceFlushBindings = false;
    private bool _isRendering = false;
    
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

    public void DrawMesh(GPUMesh mesh, uint instances = 1)
    {
        Pipeline? pipeline = null;
        var newPipeline = _drawState.Shader?.TryGetPipeline(_drawState.Variant, out pipeline);
        if (!newPipeline.HasValue || !newPipeline.Value)
        {
            throw new InvalidOperationException($"Pipeline with variant{_drawState.Variant} does not exist.");
        }

        // Only bind new pipeline if its different from the existing one
        if (_currentPipeline == null || pipeline != _currentPipeline)
        {
            _currentPipeline = pipeline;
            _commandList.BindPipeline(_currentPipeline);
            _forceFlushBindings = true; // Pipeline changed, we need to rebind
        }

        FlushBindings();

        _commandList.BindVertexBuffer(mesh.VertexBuffer.GetBuffer(), mesh.VertexBuffer.Offset, 0, 0);
        if (mesh.NumIndices > 0)
        {
            _commandList.BindIndexBuffer(mesh.IndexBuffer.GetBuffer(), mesh.IndexType, mesh.IndexBuffer.Offset);
            _commandList.DrawIndexed(mesh.NumIndices, instances, (uint)mesh.IndexBuffer.Offset, 0, 0);
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
            
            var slots = rootSignature.GetSlotsForSpace(registerSpace);

            
        }
    }
    // TODO DrawMeshIndirect

    public void Submit(ExecuteCommandListsDesc desc)
    {
        _commandList.End();
        if (_isRendering)
        {
            _commandList.EndRendering();
        }

        _signalFence = desc.GetSignal();

        var executeCommandListsDesc = desc;
        executeCommandListsDesc.Signal = _fences[_currentFrame];

        _commandQueue.ExecuteCommandLists(executeCommandListsDesc);
    }
}