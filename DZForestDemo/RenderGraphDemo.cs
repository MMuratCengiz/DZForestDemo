using System.Runtime.InteropServices;
using System.Text;
using DenOfIz;
using Graphics.RenderGraph;
using RuntimeAssets;
using Buffer = DenOfIz.Buffer;

namespace DZForestDemo;

public class RenderGraphDemo : IDisposable
{
    private readonly CommandQueue _commandQueue;
    private readonly LogicalDevice _logicalDevice;
    private readonly uint _numFrames = 3;
    private readonly RenderGraph _renderGraph;

    private readonly PinnedArray<RenderingAttachmentDesc> _rtAttachments = new(1);
    private readonly StepTimer _stepTimer = new();
    private readonly SwapChain _swapChain;
    private readonly Viewport _viewport;
    private readonly Window _window;

    private uint _currentFrameIndex;
    private uint _currentImageIndex;
    private bool _disposed;

    private InputLayout _inputLayout = null!;
    private Pipeline _pipeline = null!;
    private RootSignature _rootSignature = null!;
    private Buffer _vertexBuffer = null!;

    public RenderGraphDemo(LogicalDevice logicalDevice, uint width, uint height, string title)
    {
        _logicalDevice = logicalDevice;

        _window = new Window(new WindowDesc
        {
            Width = (int)width,
            Height = (int)height,
            Title = StringView.Create(title)
        });

        _commandQueue = _logicalDevice.CreateCommandQueue(new CommandQueueDesc
        {
            QueueType = QueueType.Graphics
        });

        _swapChain = _logicalDevice.CreateSwapChain(new SwapChainDesc
        {
            AllowTearing = true,
            BackBufferFormat = Format.B8G8R8A8Unorm,
            DepthBufferFormat = Format.D32Float,
            CommandQueue = _commandQueue,
            WindowHandle = _window.GetGraphicsWindowHandle(),
            Width = width,
            Height = height,
            NumBuffers = _numFrames
        });

        _renderGraph = new RenderGraph(new RenderGraphDesc
        {
            LogicalDevice = _logicalDevice,
            CommandQueue = _commandQueue,
            NumFrames = _numFrames
        });
        _renderGraph.SetDimensions(width, height);

        for (uint i = 0; i < _numFrames; ++i)
            _renderGraph.ResourceTracking.TrackTexture(
                _swapChain.GetRenderTarget(i),
                (uint)ResourceUsageFlagBits.Common,
                QueueType.Graphics
            );

        _window.Show();
        _viewport = _swapChain.GetViewport();

        CreateBuffers();
        CreatePipeline();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        _renderGraph.WaitIdle();
        _commandQueue.WaitIdle();

        _vertexBuffer.Dispose();
        _pipeline.Dispose();
        _rootSignature.Dispose();
        _inputLayout.Dispose();

        _rtAttachments.Dispose();
        _renderGraph.Dispose();
        _swapChain.Dispose();
        _commandQueue.Dispose();

        GC.SuppressFinalize(this);
    }

    public bool PollAndRender()
    {
        while (InputSystem.PollEvent(out var ev))
        {
            switch (ev.Type)
            {
                case EventType.Quit:
                case EventType.KeyDown when ev.Key.KeyCode == KeyCode.Escape:
                    return false;
                case EventType.WindowEvent when ev.Window.Event == WindowEventType.Resized:
                    HandleResize((uint)ev.Window.Data1, (uint)ev.Window.Data2);
                    break;
            }
        }

        _stepTimer.Tick();

        _currentFrameIndex = (_currentFrameIndex + 1) % _numFrames;
        _renderGraph.BeginFrame(_currentFrameIndex);

        _currentImageIndex = _swapChain.AcquireNextImage();
        var renderTarget = _swapChain.GetRenderTarget(_currentImageIndex);

        var swapchainRt = _renderGraph.ImportTexture("SwapchainRT", renderTarget);

        _renderGraph.AddPass("MainRender",
            (ref RenderPassSetupContext ctx, ref PassBuilder builder) =>
            {
                builder.WriteTexture(swapchainRt);
                builder.HasSideEffects();
            },
            (ref RenderPassExecuteContext ctx) =>
            {
                var cmd = ctx.CommandList;
                var rt = ctx.GetTexture(swapchainRt);

                ctx.ResourceTracking.TransitionTexture(cmd, rt,
                    (uint)ResourceUsageFlagBits.RenderTarget, QueueType.Graphics);

                _rtAttachments[0] = new RenderingAttachmentDesc
                {
                    Resource = rt,
                    ClearDepthStencil = new Float2 { X = 1.0f, Y = 0.0f }
                };

                var renderingDesc = new RenderingDesc
                {
                    RTAttachments = RenderingAttachmentDescArray.FromPinned(_rtAttachments.Handle, 1),
                    NumLayers = 1
                };

                cmd.BeginRendering(renderingDesc);
                cmd.BindPipeline(_pipeline);
                cmd.BindViewport(_viewport.X, _viewport.Y, _viewport.Width, _viewport.Height);
                cmd.BindScissorRect(_viewport.X, _viewport.Y, _viewport.Width, _viewport.Height);
                cmd.BindVertexBuffer(_vertexBuffer, 0, 0, 0);
                cmd.Draw(3, 1, 0, 0);
                cmd.EndRendering();

                ctx.ResourceTracking.TransitionTexture(cmd, rt,
                    (uint)ResourceUsageFlagBits.Present, QueueType.Graphics);
            });

        _renderGraph.Compile();
        _renderGraph.Execute();

        switch (_swapChain.Present(_currentFrameIndex))
        {
            case PresentResult.Success:
            case PresentResult.Suboptimal:
                break;
            case PresentResult.Timeout:
            case PresentResult.DeviceLost:
                _logicalDevice.WaitIdle();
                break;
        }

        return true;
    }

    private void HandleResize(uint width, uint height)
    {
        _renderGraph.WaitIdle();
        _logicalDevice.WaitIdle();
        _commandQueue.WaitIdle();

        _swapChain.Resize(width, height);
        _renderGraph.SetDimensions(width, height);

        for (uint i = 0; i < _numFrames; ++i)
            _renderGraph.ResourceTracking.TrackTexture(
                _swapChain.GetRenderTarget(i),
                (uint)ResourceUsageFlagBits.Common,
                QueueType.Graphics
            );
    }

    private void CreateBuffers()
    {
        _vertexBuffer = _logicalDevice.CreateBuffer(new BufferDesc
        {
            NumBytes = (ulong)Triangle.Vertices.Length * sizeof(float),
            Usages = (uint)ResourceUsageFlagBits.VertexAndConstantBuffer,
            HeapType = HeapType.Gpu
        });

        var batchCopy = new BatchResourceCopy(new BatchResourceCopyDesc
        {
            Device = _logicalDevice,
            IssueBarriers = false
        });

        batchCopy.Begin();

        var data = new byte[Triangle.Vertices.Length * sizeof(float)];
        System.Buffer.BlockCopy(Triangle.Vertices, 0, data, 0, data.Length);
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

        var semaphore = _logicalDevice.CreateSemaphore();
        batchCopy.Submit(semaphore);
        handle.Free();
    }

    private void CreatePipeline()
    {
        var shaderLoader = new ShaderLoader();
        var vsSource = shaderLoader.Load("debug_vs.hlsl");
        var psSource = shaderLoader.Load("debug_ps.hlsl");

        var programDesc = new ShaderProgramDesc
        {
            ShaderStages = ShaderStageDescArray.Create([
                new ShaderStageDesc
                {
                    EntryPoint = StringView.Create("VSMain"),
                    Data = ByteArray.Create(Encoding.UTF8.GetBytes(vsSource)),
                    Stage = ShaderStage.Vertex
                },
                new ShaderStageDesc
                {
                    EntryPoint = StringView.Create("PSMain"),
                    Data = ByteArray.Create(Encoding.UTF8.GetBytes(psSource)),
                    Stage = ShaderStage.Pixel
                }
            ])
        };

        var program = new ShaderProgram(programDesc);
        var reflection = program.Reflect();
        _inputLayout = _logicalDevice.CreateInputLayout(reflection.InputLayout);
        _rootSignature = _logicalDevice.CreateRootSignature(reflection.RootSignature);

        _pipeline = _logicalDevice.CreatePipeline(new PipelineDesc
        {
            InputLayout = _inputLayout,
            RootSignature = _rootSignature,
            ShaderProgram = program,
            Graphics = new GraphicsPipelineDesc
            {
                PrimitiveTopology = PrimitiveTopology.Triangle,
                RenderTargets = RenderTargetDescArray.Create([
                    new RenderTargetDesc
                    {
                        Format = Format.B8G8R8A8Unorm,
                        Blend = new BlendDesc { RenderTargetWriteMask = 0x0F }
                    }
                ])
            }
        });
    }
}