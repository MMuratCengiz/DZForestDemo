using DenOfIz;
using Graphics;
using Graphics.RenderGraph;
using UIFramework;

namespace DZForestDemo.RenderPasses;

public sealed class UiRenderPass : IDisposable
{
    private readonly GraphicsContext _ctx;
    private readonly StepTimer _stepTimer;
    private readonly UiContext _ui;
    private int _cubeCount;
    private string _cubeName = "MyCube";

    private bool _disposed;
    private string _notes = "";
    private bool _showStats;

    private readonly string[] _renderModes = ["Default", "Wireframe", "Normals", "Depth"];
    private int _selectedRenderMode;

    private readonly string[] _qualityLevels = ["Low", "Medium", "High", "Ultra"];
    private int _selectedQuality = 2;

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

    public bool ShowWireframe { get; private set; }

    public bool EnableShadows { get; private set; } = true;

    public string CubeName => _cubeName;

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

    public event Action? OnAddCubeClicked;

    public void HandleEvent(Event ev)
    {
        _ui.HandleEvent(ev);
        _ui.RecordEvent(ev);
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
                var deltaTime = (float)_stepTimer.GetDeltaTime();
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
                        Color = UiColor.White,
                        FontSize = 28
                    });
                    using (Ui.Card(_ui, "SettingsCard")
                               .Background(UiColor.Rgba(30, 30, 35, 220))
                               .Border(1, UiColor.Rgb(60, 60, 65))
                               .Padding(16)
                               .Gap(12)
                               .Width(280)
                               .Open())
                    {
                        frame.Text("Settings", new UiTextStyle
                        {
                            Color = UiColor.White,
                            FontSize = 18
                        });

                        Ui.Divider(_ui, UiColor.Rgb(60, 60, 65));
                        if (Ui.Checkbox(_ui, "chkWireframe", "Wireframe Mode", ShowWireframe)
                            .LabelColor(UiColor.LightGray)
                            .Show())
                        {
                            ShowWireframe = !ShowWireframe;
                        }

                        if (Ui.Checkbox(_ui, "chkShadows", "Enable Shadows", EnableShadows)
                            .LabelColor(UiColor.LightGray)
                            .CheckColor(UiColor.Rgb(100, 200, 100))
                            .Show())
                        {
                            EnableShadows = !EnableShadows;
                        }

                        if (Ui.Checkbox(_ui, "chkStats", "Show Statistics", _showStats)
                            .LabelColor(UiColor.LightGray)
                            .Show())
                        {
                            _showStats = !_showStats;
                        }

                        Ui.VerticalSpacer(_ui, 8);

                        frame.Text("Render Mode:", new UiTextStyle
                        {
                            Color = UiColor.LightGray,
                            FontSize = 14
                        });

                        Ui.Dropdown(_ui, "ddRenderMode", _renderModes)
                            .GrowWidth()
                            .Placeholder("Select mode...")
                            .Show(ref _selectedRenderMode);

                        frame.Text("Quality:", new UiTextStyle
                        {
                            Color = UiColor.LightGray,
                            FontSize = 14
                        });

                        Ui.Dropdown(_ui, "ddQuality", _qualityLevels)
                            .GrowWidth()
                            .SelectedItemColor(UiColor.Rgb(60, 140, 60))
                            .Show(ref _selectedQuality);
                    }

                    using (Ui.Card(_ui, "CubeCard")
                               .Background(UiColor.Rgba(30, 30, 35, 220))
                               .Border(1, UiColor.Rgb(60, 60, 65))
                               .Padding(16)
                               .Gap(12)
                               .Width(280)
                               .Open())
                    {
                        frame.Text("Cube Creator", new UiTextStyle
                        {
                            Color = UiColor.White,
                            FontSize = 18
                        });

                        Ui.Divider(_ui, UiColor.Rgb(60, 60, 65));
                        frame.Text("Name:", new UiTextStyle
                        {
                            Color = UiColor.LightGray,
                            FontSize = 14
                        });

                        Ui.TextField(_ui, "tfCubeName", ref _cubeName)
                            .Placeholder("Enter cube name...")
                            .GrowWidth()
                            .Show(ref _cubeName, deltaTime);

                        Ui.VerticalSpacer(_ui, 4);
                        using (frame.Row("BtnRow").Gap(12).Open())
                        {
                            if (Ui.Button(_ui, "AddCubeBtn", "Add Cube")
                                .Color(UiColor.Rgb(60, 140, 60))
                                .Padding(12, 8)
                                .Show())
                            {
                                _cubeCount++;
                                OnAddCubeClicked?.Invoke();
                            }

                            frame.Text($"Count: {_cubeCount}", new UiTextStyle
                            {
                                Color = UiColor.Gray,
                                FontSize = 14
                            });
                        }
                    }

                    using (Ui.Card(_ui, "NotesCard")
                               .Background(UiColor.Rgba(30, 30, 35, 220))
                               .Border(1, UiColor.Rgb(60, 60, 65))
                               .Padding(16)
                               .Gap(8)
                               .Width(280)
                               .Open())
                    {
                        frame.Text("Notes", new UiTextStyle
                        {
                            Color = UiColor.White,
                            FontSize = 18
                        });

                        Ui.TextField(_ui, "tfNotes", ref _notes)
                            .Multiline()
                            .Placeholder("Add notes here...")
                            .GrowWidth()
                            .Height(80)
                            .Show(ref _notes, deltaTime);
                    }

                    if (_showStats)
                    {
                        using (Ui.Card(_ui, "StatsCard")
                                   .Background(UiColor.Rgba(20, 60, 20, 200))
                                   .Border(1, UiColor.Rgb(40, 100, 40))
                                   .Padding(12)
                                   .Gap(4)
                                   .Open())
                        {
                            frame.Text($"FPS: {1.0 / deltaTime:F0}", new UiTextStyle
                            {
                                Color = UiColor.Rgb(150, 255, 150),
                                FontSize = 14
                            });
                            frame.Text($"Wireframe: {(ShowWireframe ? "ON" : "OFF")}", new UiTextStyle
                            {
                                Color = UiColor.Rgb(150, 255, 150),
                                FontSize = 14
                            });
                            frame.Text($"Shadows: {(EnableShadows ? "ON" : "OFF")}", new UiTextStyle
                            {
                                Color = UiColor.Rgb(150, 255, 150),
                                FontSize = 14
                            });
                        }
                    }
                }

                _ui.ClearFrameEvents();

                var result = frame.End(ctx.FrameIndex, deltaTime);
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
}