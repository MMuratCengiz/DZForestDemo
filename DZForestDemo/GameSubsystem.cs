using System.Runtime.InteropServices;
using System.Text;
using DenOfIz;
using ECS;
using Graphics;
using Graphics.RenderGraph;
using UIFramework;

namespace DZForestDemo;

public sealed class GameSystem : ISystem
{
    private World _world = null!;
    private GraphicsSystem _graphics = null!;
    private GraphicsContext _ctx = null!;

    private StepTimer _stepTimer = null!;
    private UiContext _ui = null!;
    private FrameDebugRenderer _frameDebugRenderer = null!;

    private InputLayout _triangleInputLayout = null!;
    private RootSignature _triangleRootSignature = null!;
    private Pipeline _trianglePipeline = null!;
    private BufferResource _vertexBuffer = null!;

    private RootSignature _compositeRootSignature = null!;
    private Pipeline _compositePipeline = null!;
    private Sampler _linearSampler = null!;
    private ResourceBindGroup[] _compositeBindGroups = null!;

    private PinnedArray<RenderingAttachmentDesc> _rtAttachments = null!;

    private ResourceHandle _sceneRt;
    private ResourceHandle _uiRt;

    private TextureResource?[] _boundSceneTextures = null!;
    private TextureResource?[] _boundUiTextures = null!;

    private bool _disposed;

    public void Initialize(World world)
    {
        _world = world;
        _graphics = world.GetSystem<GraphicsSystem>() ?? throw new InvalidOperationException("GraphicsSystem not found");
        _ctx = world.GetContext<GraphicsContext>();

        var logicalDevice = _ctx.LogicalDevice;
        var numFrames = _ctx.NumFrames;

        _stepTimer = new StepTimer();
        _rtAttachments = new PinnedArray<RenderingAttachmentDesc>(1);
        _boundSceneTextures = new TextureResource?[numFrames];
        _boundUiTextures = new TextureResource?[numFrames];

        _ui = new UiContext(new UiContextDesc
        {
            LogicalDevice = logicalDevice,
            ResourceTracking = _graphics.RenderGraph.ResourceTracking,
            RenderTargetFormat = _ctx.BackBufferFormat,
            NumFrames = numFrames,
            Width = _ctx.Width,
            Height = _ctx.Height,
            MaxNumElements = 8192,
            MaxNumTextMeasureCacheElements = 16384,
            MaxNumFonts = 16
        });

        _frameDebugRenderer = new FrameDebugRenderer(new FrameDebugRendererDesc
        {
            Enabled = true,
            LogicalDevice = logicalDevice,
            ScreenWidth = _ctx.Width,
            ScreenHeight = _ctx.Height,
            FontSize = 24,
            TextColor = new Float4 { X = 1.0f, Y = 1.0f, Z = 1.0f, W = 1.0f }
        });

        CreateBuffers();
        CreateTrianglePipeline();
        CreateCompositePipeline();
    }

    public bool OnEvent(ref Event ev)
    {
        _ui.HandleEvent(ev);
        _ui.UpdateScroll((float)_stepTimer.GetDeltaTime());

        if (ev is { Type: EventType.WindowEvent, Window.Event: WindowEventType.Resized })
        {
            HandleResize((uint)ev.Window.Data1, (uint)ev.Window.Data2);
        }

        return false;
    }

    private void HandleResize(uint width, uint height)
    {
        if (width == 0 || height == 0)
        {
            return;
        }

        _frameDebugRenderer.SetScreenSize(width, height);
        _ui.SetViewportSize(width, height);
    }

    public void Render(double deltaTime)
    {
        _stepTimer.Tick();

        _graphics.BeginFrame();

        var renderGraph = _graphics.RenderGraph;
        var swapchainRt = _graphics.SwapchainRenderTarget;
        var viewport = _ctx.SwapChain.GetViewport();

        _sceneRt = renderGraph.CreateTransientTexture(new TransientTextureDesc
        {
            Width = _ctx.Width,
            Height = _ctx.Height,
            Format = _ctx.BackBufferFormat,
            Usages = (uint)(ResourceUsageFlagBits.RenderTarget | ResourceUsageFlagBits.ShaderResource),
            Descriptor = (uint)(ResourceDescriptorFlagBits.RenderTarget | ResourceDescriptorFlagBits.Texture),
            DebugName = "SceneRT"
        });

        AddTrianglePass(renderGraph, viewport);
        AddUiPass(renderGraph);
        AddCompositePass(renderGraph, swapchainRt, viewport);

        _graphics.EndFrame();
    }

    private void AddTrianglePass(RenderGraph renderGraph, Viewport viewport)
    {
        renderGraph.AddPass("Triangle",
            (ref RenderPassSetupContext ctx, ref PassBuilder builder) =>
            {
                builder.WriteTexture(_sceneRt, (uint)ResourceUsageFlagBits.RenderTarget);
                builder.HasSideEffects();
            },
            (ref RenderPassExecuteContext ctx) =>
            {
                var cmd = ctx.CommandList;
                var rt = ctx.GetTexture(_sceneRt);

                ctx.ResourceTracking.TransitionTexture(cmd, rt,
                    (uint)ResourceUsageFlagBits.RenderTarget, QueueType.Graphics);

                _rtAttachments[0] = new RenderingAttachmentDesc
                {
                    Resource = rt,
                    LoadOp = LoadOp.Clear,
                    ClearColor = new Float4 { X = 0.1f, Y = 0.1f, Z = 0.1f, W = 1.0f }
                };

                var renderingDesc = new RenderingDesc
                {
                    RTAttachments = RenderingAttachmentDescArray.FromPinned(_rtAttachments.Handle, 1),
                    NumLayers = 1
                };

                cmd.BeginRendering(renderingDesc);
                cmd.BindPipeline(_trianglePipeline);
                cmd.BindViewport(viewport.X, viewport.Y, viewport.Width, viewport.Height);
                cmd.BindScissorRect(viewport.X, viewport.Y, viewport.Width, viewport.Height);
                cmd.BindVertexBuffer(_vertexBuffer, 0, 0, 0);
                cmd.Draw(3, 1, 0, 0);
                _frameDebugRenderer.Render(cmd);
                cmd.EndRendering();
            });
    }

    private void AddUiPass(RenderGraph renderGraph)
    {
        _uiRt = renderGraph.AddExternalPass("UI",
            (ref ExternalPassExecuteContext ctx) =>
            {
                var frame = _ui.BeginFrame();
                using (frame.Root()
                           .Vertical()
                           .Padding(24)
                           .Gap(24)
                           .AlignChildren(UiAlignX.Center, UiAlignY.Top)
                           .Background(UiColor.Rgba(0, 0, 0, 0))
                           .Open())
                {
                    frame.Text("UI Example", new UiTextStyle
                    {
                        Color = UiColor.Rgb(255, 255, 255),
                        FontSize = 28,
                        Alignment = UiTextAlign.Center
                    });
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

    private void AddCompositePass(RenderGraph renderGraph, ResourceHandle swapchainRt, Viewport viewport)
    {
        renderGraph.AddPass("Composite",
            (ref RenderPassSetupContext ctx, ref PassBuilder builder) =>
            {
                builder.ReadTexture(_sceneRt, (uint)ResourceUsageFlagBits.ShaderResource);
                builder.ReadTexture(_uiRt, (uint)ResourceUsageFlagBits.ShaderResource);
                builder.WriteTexture(swapchainRt, (uint)ResourceUsageFlagBits.RenderTarget);
                builder.HasSideEffects();
            },
            (ref RenderPassExecuteContext ctx) =>
            {
                var cmd = ctx.CommandList;
                var sceneTexture = ctx.GetTexture(_sceneRt);
                var uiTexture = ctx.GetTexture(_uiRt);
                var rt = ctx.GetTexture(swapchainRt);

                ctx.ResourceTracking.TransitionTexture(cmd, sceneTexture,
                    (uint)ResourceUsageFlagBits.ShaderResource, QueueType.Graphics);
                ctx.ResourceTracking.TransitionTexture(cmd, rt,
                    (uint)ResourceUsageFlagBits.RenderTarget, QueueType.Graphics);

                UpdateBindGroupIfNeeded(sceneTexture, uiTexture, ctx.FrameIndex);

                _rtAttachments[0] = new RenderingAttachmentDesc
                {
                    Resource = rt,
                    LoadOp = LoadOp.DontCare
                };

                var renderingDesc = new RenderingDesc
                {
                    RTAttachments = RenderingAttachmentDescArray.FromPinned(_rtAttachments.Handle, 1),
                    NumLayers = 1
                };

                cmd.BeginRendering(renderingDesc);
                cmd.BindPipeline(_compositePipeline);
                cmd.BindViewport(viewport.X, viewport.Y, viewport.Width, viewport.Height);
                cmd.BindScissorRect(viewport.X, viewport.Y, viewport.Width, viewport.Height);
                cmd.BindResourceGroup(_compositeBindGroups[ctx.FrameIndex]);
                cmd.Draw(3, 1, 0, 0);
                cmd.EndRendering();

                ctx.ResourceTracking.TransitionTexture(cmd, rt,
                    (uint)ResourceUsageFlagBits.Present, QueueType.Graphics);
            });
    }

    private void UpdateBindGroupIfNeeded(TextureResource sceneTexture, TextureResource uiTexture, uint frameIndex)
    {
        if (_boundSceneTextures[frameIndex] == sceneTexture && _boundUiTextures[frameIndex] == uiTexture)
        {
            return;
        }

        _boundSceneTextures[frameIndex] = sceneTexture;
        _boundUiTextures[frameIndex] = uiTexture;

        var bindGroup = _compositeBindGroups[frameIndex];
        bindGroup.BeginUpdate();
        bindGroup.SrvTexture(0, sceneTexture);
        bindGroup.SrvTexture(1, uiTexture);
        bindGroup.Sampler(0, _linearSampler);
        bindGroup.EndUpdate();
    }

    private void CreateBuffers()
    {
        var logicalDevice = _ctx.LogicalDevice;

        _vertexBuffer = logicalDevice.CreateBufferResource(new BufferDesc
        {
            NumBytes = (ulong)Triangle.Vertices.Length * sizeof(float),
            Usages = (uint)ResourceUsageFlagBits.VertexAndConstantBuffer,
            HeapType = HeapType.Gpu
        });

        var batchCopy = new BatchResourceCopy(new BatchResourceCopyDesc
        {
            Device = logicalDevice,
            IssueBarriers = false
        });

        batchCopy.Begin();

        var data = new byte[Triangle.Vertices.Length * sizeof(float)];
        Buffer.BlockCopy(Triangle.Vertices, 0, data, 0, data.Length);
        var handle = GCHandle.Alloc(data, GCHandleType.Pinned);

        batchCopy.CopyToGPUBuffer(new CopyToGpuBufferDesc
        {
            Data = new ByteArrayView
            {
                Elements = handle.AddrOfPinnedObject(),
                NumElements = (ulong)data.Length
            },
            DstBuffer = _vertexBuffer,
            DstBufferOffset = 0
        });

        var semaphore = logicalDevice.CreateSemaphore();
        batchCopy.Submit(semaphore);
        handle.Free();
    }

    private void CreateTrianglePipeline()
    {
        var logicalDevice = _ctx.LogicalDevice;

        var programDesc = new ShaderProgramDesc
        {
            ShaderStages = ShaderStageDescArray.Create([
                new ShaderStageDesc
                {
                    EntryPoint = StringView.Create("VSMain"),
                    Data = ByteArray.Create(Encoding.UTF8.GetBytes(Shaders.VertexShaderSource)),
                    Stage = ShaderStage.Vertex
                },
                new ShaderStageDesc
                {
                    EntryPoint = StringView.Create("PSMain"),
                    Data = ByteArray.Create(Encoding.UTF8.GetBytes(Shaders.PixelShaderSource)),
                    Stage = ShaderStage.Pixel
                }
            ])
        };

        var program = new ShaderProgram(programDesc);
        var reflection = program.Reflect();
        _triangleInputLayout = logicalDevice.CreateInputLayout(reflection.InputLayout);
        _triangleRootSignature = logicalDevice.CreateRootSignature(reflection.RootSignature);

        _trianglePipeline = logicalDevice.CreatePipeline(new PipelineDesc
        {
            InputLayout = _triangleInputLayout,
            RootSignature = _triangleRootSignature,
            ShaderProgram = program,
            Graphics = new GraphicsPipelineDesc
            {
                PrimitiveTopology = PrimitiveTopology.Triangle,
                RenderTargets = RenderTargetDescArray.Create([
                    new RenderTargetDesc
                    {
                        Format = _ctx.BackBufferFormat,
                        Blend = new BlendDesc { RenderTargetWriteMask = 0x0F }
                    }
                ])
            }
        });
    }

    private void CreateCompositePipeline()
    {
        var logicalDevice = _ctx.LogicalDevice;
        var numFrames = _ctx.NumFrames;

        var programDesc = new ShaderProgramDesc
        {
            ShaderStages = ShaderStageDescArray.Create([
                new ShaderStageDesc
                {
                    EntryPoint = StringView.Create("VSMain"),
                    Data = ByteArray.Create(Encoding.UTF8.GetBytes(Shaders.FullscreenVertexShader)),
                    Stage = ShaderStage.Vertex
                },
                new ShaderStageDesc
                {
                    EntryPoint = StringView.Create("PSMain"),
                    Data = ByteArray.Create(Encoding.UTF8.GetBytes(Shaders.CompositePixelShader)),
                    Stage = ShaderStage.Pixel
                }
            ])
        };

        var program = new ShaderProgram(programDesc);
        var reflection = program.Reflect();
        _compositeRootSignature = logicalDevice.CreateRootSignature(reflection.RootSignature);

        _compositePipeline = logicalDevice.CreatePipeline(new PipelineDesc
        {
            RootSignature = _compositeRootSignature,
            ShaderProgram = program,
            Graphics = new GraphicsPipelineDesc
            {
                PrimitiveTopology = PrimitiveTopology.Triangle,
                RenderTargets = RenderTargetDescArray.Create([
                    new RenderTargetDesc
                    {
                        Format = _ctx.BackBufferFormat,
                        Blend = new BlendDesc { RenderTargetWriteMask = 0x0F }
                    }
                ])
            }
        });

        _linearSampler = logicalDevice.CreateSampler(new SamplerDesc
        {
            MinFilter = Filter.Linear,
            MagFilter = Filter.Linear,
            AddressModeU = SamplerAddressMode.ClampToEdge,
            AddressModeV = SamplerAddressMode.ClampToEdge,
            AddressModeW = SamplerAddressMode.ClampToEdge
        });

        _compositeBindGroups = new ResourceBindGroup[numFrames];
        for (var i = 0; i < numFrames; i++)
        {
            _compositeBindGroups[i] = logicalDevice.CreateResourceBindGroup(new ResourceBindGroupDesc
            {
                RootSignature = _compositeRootSignature
            });
        }
    }

    public void Shutdown()
    {
        _graphics.RenderGraph.WaitIdle();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        for (var i = 0; i < _ctx.NumFrames; i++)
        {
            _compositeBindGroups[i]?.Dispose();
        }

        _linearSampler.Dispose();
        _compositePipeline.Dispose();
        _compositeRootSignature.Dispose();

        _vertexBuffer.Dispose();
        _trianglePipeline.Dispose();
        _triangleRootSignature.Dispose();
        _triangleInputLayout.Dispose();

        _rtAttachments.Dispose();
        _ui.Dispose();
        _frameDebugRenderer.Dispose();

        GC.SuppressFinalize(this);
    }
}
