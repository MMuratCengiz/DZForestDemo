using DenOfIz;
using Graphics;
using Graphics.RenderGraph;

namespace DZForestDemo.RenderPasses;

public sealed class DebugRenderPass : IDisposable
{
    private readonly GraphicsContext _ctx;
    private readonly FrameDebugRenderer _debugRenderer;
    private readonly PinnedArray<RenderingAttachmentDesc> _rtAttachments;

    private bool _disposed;

    public DebugRenderPass(GraphicsContext ctx)
    {
        _ctx = ctx;
        _debugRenderer = new FrameDebugRenderer(new FrameDebugRendererDesc
        {
            Enabled = true,
            LogicalDevice = ctx.LogicalDevice,
            ScreenWidth = ctx.Width,
            ScreenHeight = ctx.Height,
            FontSize = 24,
            TextColor = new Float4 { X = 1.0f, Y = 1.0f, Z = 1.0f, W = 1.0f }
        });
        _rtAttachments = new PinnedArray<RenderingAttachmentDesc>(1);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        _debugRenderer.Dispose();
        _rtAttachments.Dispose();

        GC.SuppressFinalize(this);
    }

    public void SetScreenSize(uint width, uint height)
    {
        _debugRenderer.SetScreenSize(width, height);
    }

    public ResourceHandle AddPass(RenderGraph renderGraph)
    {
        var debugRt = renderGraph.CreateTransientTexture(new TransientTextureDesc
        {
            Width = _ctx.Width,
            Height = _ctx.Height,
            Format = _ctx.BackBufferFormat,
            Usages = (uint)(ResourceUsageFlagBits.RenderTarget | ResourceUsageFlagBits.ShaderResource),
            Descriptor = (uint)(ResourceDescriptorFlagBits.RenderTarget | ResourceDescriptorFlagBits.Texture),
            DebugName = "DebugRT"
        });

        var viewport = _ctx.SwapChain.GetViewport();

        renderGraph.AddPass("Debug",
            (ref RenderPassSetupContext ctx, ref PassBuilder builder) =>
            {
                builder.WriteTexture(debugRt);
                builder.HasSideEffects();
            },
            (ref RenderPassExecuteContext ctx) => { Execute(ref ctx, debugRt, viewport); });

        return debugRt;
    }

    private void Execute(
        ref RenderPassExecuteContext ctx,
        ResourceHandle debugRtHandle,
        Viewport viewport)
    {
        var cmd = ctx.CommandList;
        var rt = ctx.GetTexture(debugRtHandle);

        ctx.ResourceTracking.TransitionTexture(cmd, rt,
            (uint)ResourceUsageFlagBits.RenderTarget, QueueType.Graphics);

        _rtAttachments[0] = new RenderingAttachmentDesc
        {
            Resource = rt,
            LoadOp = LoadOp.Clear,
            ClearColor = new Float4 { X = 0, Y = 0, Z = 0, W = 0 }
        };

        var renderingDesc = new RenderingDesc
        {
            RTAttachments = RenderingAttachmentDescArray.FromPinned(_rtAttachments.Handle, 1),
            NumLayers = 1
        };

        cmd.BeginRendering(renderingDesc);
        cmd.BindViewport(viewport.X, viewport.Y, viewport.Width, viewport.Height);
        cmd.BindScissorRect(viewport.X, viewport.Y, viewport.Width, viewport.Height);
        _debugRenderer.Render(cmd);
        cmd.EndRendering();
    }
}