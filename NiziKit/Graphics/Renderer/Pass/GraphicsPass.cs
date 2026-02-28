using DenOfIz;
using NiziKit.Assets;
using NiziKit.Graphics.Resources;

namespace NiziKit.Graphics.Renderer.Pass;

public class GraphicsPass(CommandList commandList) : RenderPass(commandList)
{
    private const int MaxRtAttachments = 8;

    private readonly PinnedArray<RenderingAttachmentDesc> _rtAttachments = new(MaxRtAttachments);
    private readonly PinnedArray<RenderingAttachmentDesc> _depthAttachment = new(1);
    private readonly PinnedArray<RenderingAttachmentDesc> _stencilAttachment = new(1);

    private readonly Texture?[] _rtTextures = new Texture?[MaxRtAttachments];
    private Texture? _depthTexture;
    private Texture? _stencilTexture;

    private uint _rtWidth;
    private uint _rtHeight;

    private int _rtCount;
    private bool _hasDepth;
    private bool _hasStencil;

    private GpuShader? _boundShader;

    private float _viewportX;
    private float _viewportY;
    private float _viewportWidth;
    private float _viewportHeight;
    private bool _hasViewport;

    private float _scissorX;
    private float _scissorY;
    private float _scissorWidth;
    private float _scissorHeight;
    private bool _hasScissor;
    private uint _numLayers = 1;

    public void SetRenderTarget(int slot, CycledTexture renderTarget, LoadOp loadOp = LoadOp.DontCare, StoreOp storeOp = StoreOp.Store)
    {
        if (slot is < 0 or >= MaxRtAttachments)
        {
            throw new ArgumentOutOfRangeException(nameof(slot), $"Slot must be between 0 and {MaxRtAttachments - 1}");
        }

        var texture = renderTarget[GraphicsContext.FrameIndex];
        _rtTextures[slot] = texture;
        _rtAttachments[slot] = new RenderingAttachmentDesc
        {
            Resource = texture,
            ClearColor = renderTarget.ClearColor,
            ClearDepthStencil = renderTarget.ClearDepthStencil,
            LoadOp = loadOp,
            StoreOp = storeOp,
        };

        if (slot >= _rtCount)
        {
            _rtCount = slot + 1;
        }

        if (slot == 0)
        {
            _rtWidth = renderTarget.Width;
            _rtHeight = renderTarget.Height;
        }
    }

    public void SetDepthTarget(CycledTexture depthTarget, LoadOp loadOp = LoadOp.DontCare, StoreOp storeOp = StoreOp.Store)
    {
        var texture = depthTarget[GraphicsContext.FrameIndex];
        _depthTexture = texture;
        _depthAttachment[0] = new RenderingAttachmentDesc
        {
            Resource = texture,
            ClearColor = depthTarget.ClearColor,
            ClearDepthStencil = depthTarget.ClearDepthStencil,
            LoadOp = loadOp,
            StoreOp = storeOp,
        };
        _hasDepth = true;

        if (_rtWidth == 0 || _rtHeight == 0)
        {
            _rtWidth = depthTarget.Width;
            _rtHeight = depthTarget.Height;
        }
    }

    public void SetStencilTarget(CycledTexture stencilTarget, LoadOp loadOp = LoadOp.DontCare, StoreOp storeOp = StoreOp.Store)
    {
        var texture = stencilTarget[GraphicsContext.FrameIndex];
        _stencilTexture = texture;
        _stencilAttachment[0] = new RenderingAttachmentDesc
        {
            Resource = texture,
            ClearColor = stencilTarget.ClearColor,
            ClearDepthStencil = stencilTarget.ClearDepthStencil,
            LoadOp = loadOp,
            StoreOp = storeOp,
        };
        _hasStencil = true;
    }

    public void SetViewport(float x, float y, float width, float height)
    {
        _viewportX = x;
        _viewportY = y;
        _viewportWidth = width;
        _viewportHeight = height;
        _hasViewport = true;
    }

    /// <summary>
    /// Sets the number of render target array layers for layered rendering (e.g. CSM).
    /// Must be called before <see cref="RenderPass.Begin"/>.
    /// </summary>
    public void SetNumLayers(uint numLayers) => _numLayers = numLayers;

    public void SetScissor(float x, float y, float width, float height)
    {
        _scissorX = x;
        _scissorY = y;
        _scissorWidth = width;
        _scissorHeight = height;
        _hasScissor = true;
    }

    public override void Reset()
    {
        _rtCount = 0;
        _rtWidth = 0;
        _rtHeight = 0;
        _hasDepth = false;
        _hasStencil = false;
        _hasViewport = false;
        _hasScissor = false;
        _boundShader = null;
        _numLayers = 1;
        Array.Clear(_rtTextures);
        _depthTexture = null;
        _stencilTexture = null;
    }

    public void BindShader(GpuShader shader)
    {
        _boundShader = shader;
        BindPipeline(shader.Pipeline);
    }

    protected override void BeginInternal()
    {
        for (var i = 0; i < _rtCount; i++)
        {
            var texture = _rtTextures[i];
            if (texture != null)
            {
                GraphicsContext.ResourceTracking.TransitionTexture(
                    _commandList,
                    texture,
                    (uint)ResourceUsageFlagBits.RenderTarget,
                    QueueType.Graphics);
            }
        }

        if (_hasDepth && _depthTexture != null)
        {
            GraphicsContext.ResourceTracking.TransitionTexture(
                _commandList,
                _depthTexture,
                (uint)ResourceUsageFlagBits.DepthWrite,
                QueueType.Graphics);
        }

        if (_hasStencil && _stencilTexture != null)
        {
            GraphicsContext.ResourceTracking.TransitionTexture(
                _commandList,
                _stencilTexture,
                (uint)ResourceUsageFlagBits.DepthWrite,
                QueueType.Graphics);
        }

        var renderingDesc = new RenderingDesc
        {
            NumLayers = _numLayers
        };

        if (_rtCount > 0)
        {
            renderingDesc.RTAttachments = RenderingAttachmentDescArray.FromPinned(_rtAttachments.Handle, _rtCount);
        }

        if (_hasDepth)
        {
            renderingDesc.DepthAttachment = _depthAttachment[0];
        }

        if (_hasStencil)
        {
            renderingDesc.StencilAttachment = _stencilAttachment[0];
        }

        _commandList.BeginRendering(renderingDesc);

        var (defaultWidth, defaultHeight) = GetDefaultViewportSize();

        if (_hasViewport)
        {
            _commandList.BindViewport(_viewportX, _viewportY, _viewportWidth, _viewportHeight);
        }
        else
        {
            _commandList.BindViewport(0, 0, defaultWidth, defaultHeight);
        }

        if (_hasScissor)
        {
            _commandList.BindScissorRect(_scissorX, _scissorY, _scissorWidth, _scissorHeight);
        }
        else
        {
            _commandList.BindScissorRect(0, 0, defaultWidth, defaultHeight);
        }
    }

    protected override void EndInternal()
    {
        _commandList.EndRendering();
    }

    private (float width, float height) GetDefaultViewportSize()
    {
        if (_rtWidth > 0 && _rtHeight > 0)
        {
            return (_rtWidth, _rtHeight);
        }

        return (GraphicsContext.Width, GraphicsContext.Height);
    }

    public void BindVertexBuffer(DenOfIz.Buffer buffer, ulong offset, uint stride, uint slot = 0)
    {
        _commandList.BindVertexBuffer(buffer, offset, stride, slot);
    }

    public void BindIndexBuffer(DenOfIz.Buffer buffer, IndexType indexType, ulong offset = 0)
    {
        _commandList.BindIndexBuffer(buffer, indexType, offset);
    }

    public void Draw(uint vertexCount, uint instanceCount = 1, uint firstVertex = 0, uint firstInstance = 0)
    {
        _commandList.Draw(vertexCount, instanceCount, firstVertex, firstInstance);
    }

    public void DrawIndexed(uint indexCount, uint instanceCount = 1, uint firstIndex = 0, uint vertexOffset = 0, uint firstInstance = 0)
    {
        _commandList.DrawIndexed(indexCount, instanceCount, firstIndex, vertexOffset, firstInstance);
    }

    public void DrawMesh(Mesh mesh, uint instanceCount = 1)
    {
        if (_boundShader == null)
        {
            throw new InvalidOperationException("No shader bound. Call BindShader before DrawMesh.");
        }

        var vb = mesh.GetVertexBuffer(_boundShader.VertexFormat);
        BindVertexBuffer(vb.View.Buffer, vb.View.Offset, vb.Stride, 0);
        BindIndexBuffer(mesh.IndexBuffer.View.Buffer, mesh.IndexBuffer.IndexType, mesh.IndexBuffer.View.Offset);
        DrawIndexed((uint)mesh.NumIndices, instanceCount, 0, 0, 0);
    }

    public void DrawIndirect(DenOfIz.Buffer buffer, ulong offset, uint drawCount, uint stride)
    {
        _commandList.DrawIndirect(buffer, offset, drawCount, stride);
    }

    public void DrawIndexedIndirect(DenOfIz.Buffer buffer, ulong offset, uint drawCount, uint stride)
    {
        _commandList.DrawIndexedIndirect(buffer, offset, drawCount, stride);
    }

    public void DispatchMesh(uint groupCountX, uint groupCountY, uint groupCountZ)
    {
        _commandList.DispatchMesh(groupCountX, groupCountY, groupCountZ);
    }

    public override void Dispose()
    {
        _rtAttachments.Dispose();
        _depthAttachment.Dispose();
        _stencilAttachment.Dispose();
        base.Dispose();
    }
}
