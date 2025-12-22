using DenOfIz;

namespace Graphics.RenderGraph;

public class RGCommandList
{
    private const int NumFrames = 3;

    private CommandQueue _commandQueue;
    private CommandListPool _commandListPool;
    private List<Fence> _fences;
    private Fence? _signalFence;
    private List<CommandList> _commandLists;
    private CommandList _commandList;
    private DrawState _drawState;
    private int _nextFrame = 0;
    private int _currentFrame = 0;
    private Pipeline? _currentPipeline;
    private bool _forceFlushBindings = false;
    private bool _isRendering = false;

    public RGCommandList(LogicalDevice logicalDevice, CommandQueue commandQueue)
    {
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
        _drawState.Data[name] = data;
    }

    public void SetTexture(string name, Texture texture)
    {
        _drawState.Textures[name] = texture;
    }

    public void SetSampler(string name, Sampler sampler)
    {
        _drawState.Samplers[name] = sampler;
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