using System.Runtime.CompilerServices;
using DenOfIz;
using NiziKit.Application.Timing;
using NiziKit.Graphics.Resources;
using NiziKit.UI;

namespace NiziKit.Graphics.Renderer;

public delegate void UiBuildCallback(UiFrame frame);

public partial class RenderFrame
{
    private CycledTexture? _uiRenderTarget;
    private CycledTexture? _uiDepthTarget;

    public UiContext UiContext => NiziUi.Ctx;

    public void EnableUi(UiContextDesc desc)
    {
        NiziUi.Initialize(desc);
        _uiRenderTarget = CycledTexture.ColorAttachment("UIRT");
        _uiDepthTarget = CycledTexture.DepthAttachment("UIDepth");
    }

    public CycledTexture RenderUi(Action buildCallback)
    {
        if (_uiRenderTarget == null || _uiDepthTarget == null)
        {
            throw new InvalidOperationException("UI not enabled. Call EnableUi first.");
        }

        var ctx = NiziUi.Ctx;
        ctx.UpdateScroll(Time.DeltaTime);
        var uiFrame = ctx.BeginFrame();
        using (NiziUi.Root("__UiRoot").Vertical().Gap(0).Open())
        {
            buildCallback();
        }

        var pass = BeginGraphicsPass();
        pass.SetRenderTarget(0, _uiRenderTarget, LoadOp.Clear);
        pass.SetDepthTarget(_uiDepthTarget!, LoadOp.Clear);

        pass.Begin();
        uiFrame.End((uint)_currentFrame, Time.DeltaTime, pass.CommandList);
        pass.End();

        return _uiRenderTarget;
    }

    public CycledTexture RenderUi(UiBuildCallback buildCallback)
    {
        if (_uiRenderTarget == null || _uiDepthTarget == null)
        {
            throw new InvalidOperationException("UI not enabled. Call EnableUi first.");
        }

        var ctx = NiziUi.Ctx;
        ctx.UpdateScroll(Time.DeltaTime);
        var uiFrame = ctx.BeginFrame();
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
        _uiDepthTarget?.Dispose();
        _uiDepthTarget = null;
        _uiRenderTarget?.Dispose();
        _uiRenderTarget = null;
        NiziUi.Shutdown();
    }
}
