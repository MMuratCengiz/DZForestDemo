using System.Runtime.CompilerServices;
using DenOfIz;
using NiziKit.Application.Timing;
using NiziKit.Graphics.Resources;
using NiziKit.UI;

namespace NiziKit.Graphics.Renderer;

public delegate void UiBuildCallback(UiFrame frame);

public partial class RenderFrame
{
    private UiContext? _uiContext;
    private CycledTexture? _uiRenderTarget;
    private CycledTexture? _uiDepthTarget;

    public UiContext UiContext => _uiContext ?? throw new InvalidOperationException("UI not enabled. Call EnableUi first.");

    public void EnableUi(UiContextDesc desc)
    {
        _uiContext?.Dispose();
        _uiContext = new UiContext(desc);
        _uiRenderTarget = CycledTexture.ColorAttachment("UIRT");
        _uiDepthTarget = CycledTexture.DepthAttachment("UIDepth");
        GraphicsContext.OnResize += OnUiResize;
    }

    private void OnUiResize(uint width, uint height)
    {
        // _uiContext?.SetViewportSize(width, height);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void HandleUiEvent(Event ev)
    {
        _uiContext?.HandleEvent(ev);
    }

    public CycledTexture RenderUi(UiBuildCallback buildCallback)
    {
        if (_uiContext == null || _uiRenderTarget == null || _uiDepthTarget == null)
        {
            throw new InvalidOperationException("UI not enabled. Call EnableUi first.");
        }

        _uiContext.UpdateScroll(Time.DeltaTime);
        var uiFrame = _uiContext.BeginFrame();
        using (uiFrame.Root("__UiRoot").Vertical().Gap(0).Open())
        {
            buildCallback(uiFrame);
        }

        var pass = BeginGraphicsPass();
        pass.SetRenderTarget(0, _uiRenderTarget, LoadOp.Clear);
        pass.SetDepthTarget(_uiDepthTarget!, LoadOp.Clear);

        pass.Begin();
        uiFrame.End((uint)_currentFrame, Time.DeltaTime, pass.CommandList);
        pass.End();

        return _uiRenderTarget;
    }

    private void ResetUi()
    {
    }

    private void DisposeUi()
    {
        GraphicsContext.OnResize -= OnUiResize;
        _uiDepthTarget?.Dispose();
        _uiDepthTarget = null;
        _uiRenderTarget?.Dispose();
        _uiRenderTarget = null;
        _uiContext?.Dispose();
        _uiContext = null;
    }
}
