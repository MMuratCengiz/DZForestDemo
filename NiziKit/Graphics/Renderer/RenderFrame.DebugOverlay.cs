using System.Numerics;
using System.Runtime.CompilerServices;
using DenOfIz;
using NiziKit.Graphics.Renderer.Common;
using NiziKit.Graphics.Resources;

namespace NiziKit.Graphics.Renderer;

public struct DebugOverlayConfig
{
    public ulong FontAsset;
    public Vector4 TextColor;
    public float RefreshRate;
    public uint FontSize;
    public TextDirection Direction;
    public bool Enabled;

    public static DebugOverlayConfig Default => new()
    {
        FontAsset = 0,
        TextColor = new Vector4(0.8f, 1.0f, 0.8f, 1.0f),
        RefreshRate = 0.5f,
        FontSize = 24,
        Direction = TextDirection.LeftToRight,
        Enabled = true
    };
}

public partial class RenderFrame
{
    private FrameDebugRenderer? _debugRenderer;
    private CycledTexture? _debugRenderTarget;
    private AlphaBlitPass? _debugBlitPass;
    private PinnedArray<RenderingAttachmentDesc>? _debugRtAttachment;

    public bool DebugOverlayEnabled
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _debugRenderer?.IsEnabled() ?? false;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => _debugRenderer?.SetEnabled(value);
    }

    public void EnableDebugOverlay(DebugOverlayConfig config)
    {
        DisposeDebugOverlay();

        _debugRenderTarget = CycledTexture.ColorAttachment("DebugOverlayRT");
        _debugRenderer = new FrameDebugRenderer(new FrameDebugRendererDesc
        {
            GraphicsApi = GraphicsContext.GraphicsApi,
            LogicalDevice = GraphicsContext.Device,
            ScreenWidth = GraphicsContext.Width,
            ScreenHeight = GraphicsContext.Height,
            FontAsset = config.FontAsset,
            TextColor = config.TextColor,
            RefreshRate = config.RefreshRate > 0 ? config.RefreshRate : 0.5f,
            FontSize = config.FontSize > 0 ? config.FontSize : 14,
            Direction = config.Direction,
            Enabled = config.Enabled
        });

        _debugBlitPass = new AlphaBlitPass();
        _debugRtAttachment = new PinnedArray<RenderingAttachmentDesc>(1);
    }

    public CycledTexture RenderDebugOverlay()
    {
        if (_debugRenderer == null || _debugRenderTarget == null || _debugRtAttachment == null)
        {
            throw new InvalidOperationException("Debug overlay not enabled. Call EnableDebugOverlay first.");
        }

        if (!_debugRenderer.IsEnabled())
        {
            return _debugRenderTarget;
        }

        var pass = AllocateBlitPass();
        var texture = _debugRenderTarget[_currentFrame];

        pass.CommandList.Begin();

        GraphicsContext.ResourceTracking.TransitionTexture(
            pass.CommandList,
            texture,
            (uint)ResourceUsageFlagBits.RenderTarget,
            QueueType.Graphics);

        _debugRtAttachment[0] = new RenderingAttachmentDesc
        {
            Resource = texture,
            LoadOp = LoadOp.Clear,
            StoreOp = StoreOp.Store,
            ClearColor = new Vector4(0, 0, 0, 0)
        };

        var renderingDesc = new RenderingDesc
        {
            RTAttachments = RenderingAttachmentDescArray.FromPinned(_debugRtAttachment.Handle, 1),
            NumLayers = 1
        };

        pass.CommandList.BeginRendering(renderingDesc);
        pass.CommandList.BindViewport(0, 0, GraphicsContext.Width, GraphicsContext.Height);
        pass.CommandList.BindScissorRect(0, 0, GraphicsContext.Width, GraphicsContext.Height);

        _debugRenderer.Render(pass.CommandList);

        pass.CommandList.EndRendering();

        GraphicsContext.ResourceTracking.TransitionTexture(
            pass.CommandList,
            texture,
            (uint)ResourceUsageFlagBits.ShaderResource,
            QueueType.Graphics);

        pass.CommandList.End();

        return _debugRenderTarget;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddDebugLine(string text, Vector4 color)
    {
        _debugRenderer?.AddDebugLine(StringView.Intern(text), color);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ClearDebugLines()
    {
        _debugRenderer?.ClearCustomDebugLines();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ToggleDebugOverlay()
    {
        _debugRenderer?.ToggleVisibility();
    }

    private void DisposeDebugOverlay()
    {
        _debugRtAttachment?.Dispose();
        _debugRtAttachment = null;

        _debugBlitPass?.Dispose();
        _debugBlitPass = null;

        _debugRenderTarget?.Dispose();
        _debugRenderTarget = null;

        _debugRenderer?.Dispose();
        _debugRenderer = null;
    }
}
