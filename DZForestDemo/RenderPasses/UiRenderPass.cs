using DenOfIz;
using Graphics;
using Graphics.RenderGraph;
using UIFramework;

namespace DZForestDemo.RenderPasses;

public sealed class UiRenderPass : IDisposable
{
    private readonly GraphicsContext _ctx;
    private readonly UiContext _ui;
    private readonly StepTimer _stepTimer;

    private bool _disposed;

    public event Action? OnAddCubeClicked;

    public UiRenderPass(GraphicsContext ctx, StepTimer stepTimer)
    {
        _ctx = ctx;
        _stepTimer = stepTimer;

        _ui = new UiContext(new UiContextDesc
        {
            LogicalDevice = ctx.LogicalDevice,
            ResourceTracking = ctx.RenderGraph.ResourceTracking,
            RenderTargetFormat = ctx.BackBufferFormat,
            NumFrames = ctx.NumFrames,
            Width = ctx.Width,
            Height = ctx.Height,
            MaxNumElements = 8192,
            MaxNumTextMeasureCacheElements = 16384,
            MaxNumFonts = 16
        });
    }

    public void HandleEvent(Event ev)
    {
        _ui.HandleEvent(ev);
        _ui.UpdateScroll((float)_stepTimer.GetDeltaTime());
    }

    public void HandleResize(uint width, uint height)
    {
        _ui.SetViewportSize(width, height);
    }

    public ResourceHandle AddPass(RenderGraph renderGraph)
    {
        return renderGraph.AddExternalPass("UI",
            (ref ExternalPassExecuteContext ctx) =>
            {
                var frame = _ui.BeginFrame();

                using (frame.Root()
                           .Vertical()
                           .Padding(24)
                           .Gap(16)
                           .AlignChildren(UiAlignX.Left, UiAlignY.Top)
                           .Background(UiColor.Rgba(0, 0, 0, 0))
                           .Open())
                {
                    frame.Text("3D Cube Demo", new UiTextStyle
                    {
                        Color = UiColor.Rgb(255, 255, 255),
                        FontSize = 28,
                        Alignment = UiTextAlign.Left
                    });

                    frame.Text("Click 'Add Cube' to spawn cubes", new UiTextStyle
                    {
                        Color = UiColor.Rgb(180, 180, 180),
                        FontSize = 16,
                        Alignment = UiTextAlign.Left
                    });

                    Ui.Spacer(_ui, 8);

                    if (Ui.Button(_ui, "AddCubeBtn", "Add Cube")
                           .Color(UiColor.Rgb(60, 140, 60))
                           .FitContent()
                           .FontSize(18)
                           .CornerRadius(6)
                           .Render())
                    {
                        OnAddCubeClicked?.Invoke();
                    }
                }

                var result = frame.End(ctx.FrameIndex, (float)_stepTimer.GetDeltaTime());
                return new ExternalPassResult
                {
                    Texture = result.Texture!,
                    Semaphore = result.Semaphore!
                };
            },
            new TransientTextureDesc
            {
                Width = _ctx.Width,
                Height = _ctx.Height,
                Format = _ctx.BackBufferFormat,
                Usages = (uint)(ResourceUsageFlagBits.ShaderResource | ResourceUsageFlagBits.CopySrc),
                Descriptor = (uint)ResourceDescriptorFlagBits.Texture,
                DebugName = "UIRT"
            });
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        _ui.Dispose();

        GC.SuppressFinalize(this);
    }
}
