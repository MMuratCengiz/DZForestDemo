using System.Numerics;
using System.Runtime.CompilerServices;
using DenOfIz;
using NiziKit.Application.Timing;
using NiziKit.Graphics.Renderer.Common;
using NiziKit.Graphics.Resources;
using NiziKit.UI;
using Semaphore = DenOfIz.Semaphore;
using GraphicsContext = NiziKit.Graphics.GraphicsContext;

namespace NiziKit.Graphics.Renderer;

public delegate void UiBuildCallback(UiFrame frame);

public partial class RenderFrame
{
    private UiContext? _uiContext;
    private CycledTexture? _uiRenderTarget;
    private BlitPass? _uiBlitPass;
    private PinnedArray<RenderingAttachmentDesc>? _uiRtAttachment;
    private readonly Semaphore[] _externalSemaphores = new Semaphore[4];
    private int _externalSemaphoreCount;

    public UiContext UiContext => _uiContext ?? throw new InvalidOperationException("UI not enabled. Call EnableUi first.");

    public void EnableUi(UiContextDesc desc)
    {
        _uiContext?.Dispose();
        _uiContext = new UiContext(desc);
        _uiBlitPass ??= new BlitPass();
        _uiRenderTarget = CycledTexture.ColorAttachment("UIRT");
        _uiRtAttachment ??= new PinnedArray<RenderingAttachmentDesc>(1);
    }

    public void EnableUi(UiContext context)
    {
        _uiContext = context;
        _uiBlitPass ??= new BlitPass();
        _uiRtAttachment ??= new PinnedArray<RenderingAttachmentDesc>(1);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void HandleUiEvent(Event ev)
    {
        _uiContext?.HandleEvent(ev);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetUiViewportSize(uint width, uint height)
    {
        _uiContext?.SetViewportSize(width, height);
    }

    public CycledTexture RenderUi(UiBuildCallback buildCallback)
    {
        if (_uiContext == null || _uiRenderTarget == null || _uiRtAttachment == null)
        {
            throw new InvalidOperationException("UI not enabled. Call EnableUi first.");
        }

        _uiContext.UpdateScroll(Time.DeltaTime);
        var frame = _uiContext.BeginFrame();
        using (frame.Root("__UiRoot").Open())
        {
            buildCallback(frame);
        }
        var (texture, semaphore) = frame.End((uint)_currentFrame, Time.DeltaTime);

        if (_externalSemaphoreCount < _externalSemaphores.Length)
        {
            _externalSemaphores[_externalSemaphoreCount++] = semaphore;
        }

        var pass = AllocateBlitPass();
        var uiTexture = _uiRenderTarget[_currentFrame];

        pass.CommandList.Begin();

        GraphicsContext.ResourceTracking.TransitionTexture(
            pass.CommandList,
            uiTexture,
            (uint)ResourceUsageFlagBits.RenderTarget,
            QueueType.Graphics);

        _uiRtAttachment[0] = new RenderingAttachmentDesc
        {
            Resource = uiTexture,
            LoadOp = LoadOp.Clear,
            StoreOp = StoreOp.Store,
            ClearColor = new Vector4(0, 0, 0, 0)
        };

        var renderingDesc = new RenderingDesc
        {
            RTAttachments = RenderingAttachmentDescArray.FromPinned(_uiRtAttachment.Handle, 1),
            NumLayers = 1
        };

        pass.CommandList.BeginRendering(renderingDesc);
        pass.CommandList.EndRendering();

        _uiBlitPass!.Execute(pass.CommandList, texture, _uiRenderTarget);
        pass.CommandList.End();
        return _uiRenderTarget;
    }

    private void ResetUi()
    {
        _externalSemaphoreCount = 0;
    }

    private void DisposeUi()
    {
        _uiRtAttachment?.Dispose();
        _uiRtAttachment = null;
        _uiBlitPass?.Dispose();
        _uiBlitPass = null;
        _uiContext?.Dispose();
        _uiContext = null;
    }
}
