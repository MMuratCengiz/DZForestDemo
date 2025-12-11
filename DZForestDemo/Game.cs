using System.Runtime.InteropServices;
using System.Text;
using DenOfIz;

namespace DZForestDemo;

public class Game
{
    private readonly LogicalDevice _logicalDevice;
    private readonly ResourceTracking _resourceTracking;
    private readonly FrameDebugRenderer _frameDebugRenderer;
    private InputLayout _inputLayout = null!;
    private RootSignature _rootSignature = null!;
    private Pipeline _pipeline = null!;
    private BufferResource _vertexBuffer = null!;
    private readonly StepTimer _stepTimer = new();
    private readonly Clay _clay;

    private readonly PinnedArray<RenderingAttachmentDesc> _rtAttachments = new(1);

    public Game(LogicalDevice logicalDevice, ResourceTracking resourceTracking, uint screenWidth = 1920, uint screenHeight = 1080)
    {
        ClayDesc clayDesc = new()
        {
            LogicalDevice = _logicalDevice,
            ResourceTracking = _resourceTracking,
            RenderTargetFormat = Format.B8G8R8A8Unorm,
            NumFrames = 3,
            Width = screenWidth,
            Height = screenHeight,
            MaxNumElements = 8192,
            MaxNumTextMeasureCacheElements = 16384,
            MaxNumFonts = 16
        };
        _clay = new Clay(clayDesc);

        _logicalDevice = logicalDevice;
        _resourceTracking = resourceTracking;
        _frameDebugRenderer = new FrameDebugRenderer(new FrameDebugRendererDesc
        {
            Enabled = true,
            LogicalDevice = logicalDevice,
            ScreenWidth = screenWidth,
            ScreenHeight = screenHeight,
            FontSize = 24,
            TextColor = new Float4 { X = 1.0f, Y = 1.0f, Z = 1.0f, W = 1.0f }
        });
        CreateBuffers();
        CreatePipeline();
    }

    public void HandleEvent(Event ev)
    {
        _clay.HandleEvent(ev);
        _clay.UpdateScrollContainers(true, new Float2{ X = 0.0f, Y = 0.0f }, (float)_stepTimer.GetDeltaTime());
    }
    
    public void Render(Viewport viewport, CommandList commandList, TextureResource renderTarget, uint frameIndex)
    {
        _stepTimer.Tick();

        commandList.Begin();

        _resourceTracking.TransitionTexture(commandList, renderTarget,
            (uint)ResourceUsageFlagBits.RenderTarget, QueueType.Graphics);

        _rtAttachments[0] = new RenderingAttachmentDesc
        {
            Resource = renderTarget,
            ClearDepthStencil = new Float2 { X = 1.0f, Y = 0.0f }
        };

        RenderingDesc renderingDesc = new()
        {
            RTAttachments = RenderingAttachmentDescArray.FromPinned(_rtAttachments.Handle, 1),
            NumLayers = 1
        };

        commandList.BeginRendering(renderingDesc);
        RenderContent(viewport, commandList, frameIndex);
        commandList.EndRendering();

        _resourceTracking.TransitionTexture(commandList, renderTarget,
            (uint)ResourceUsageFlagBits.CopyDst, QueueType.Graphics);
        
        _clay.BeginLayout();

        var uiRender = _clay.EndLayout(frameIndex, (float)_stepTimer.GetDeltaTime());

        CopyTextureRegionDesc copyRegion = new();
        copyRegion.SrcTexture = uiRender.Texture;
        copyRegion.DstTexture = renderTarget;
        copyRegion.Width      = (uint)(viewport.Width - viewport.X);
        copyRegion.Height     = (uint)(viewport.Height - viewport.Y);
        copyRegion.Depth      = 1;
        
        _resourceTracking.TransitionTexture(commandList, renderTarget,
            (uint)ResourceUsageFlagBits.Present, QueueType.Graphics);

        commandList.End();
    }

    public void SetScreenSize(uint width, uint height)
    {
        _frameDebugRenderer.SetScreenSize(width, height);
    }

    public void RenderContent(Viewport viewport, CommandList commandList, uint frameIndex)
    {
        _stepTimer.Tick();
        commandList.BindPipeline(_pipeline);
        commandList.BindViewport(viewport.X, viewport.Y, viewport.Width, viewport.Height);
        commandList.BindScissorRect(viewport.X, viewport.Y, viewport.Width, viewport.Height);
        commandList.BindVertexBuffer(_vertexBuffer, 0, 0, 0);
        commandList.Draw(3, 1, 0, 0);
        _frameDebugRenderer.Render(commandList);
    }


    private void CreateBuffers()
    {
        var bufferDesc = new BufferDesc
        {
            NumBytes = (ulong)Triangle.Vertices.Length * sizeof(float),
            Usages = (uint)ResourceUsageFlagBits.VertexAndConstantBuffer,
            HeapType = HeapType.Gpu
        };
        _vertexBuffer = _logicalDevice.CreateBufferResource(bufferDesc);

        var batchCopy = new BatchResourceCopy(new BatchResourceCopyDesc
        {
            Device = _logicalDevice,
            IssueBarriers = false
        });

        batchCopy.Begin();

        var data = new byte[Triangle.Vertices.Length * sizeof(float)];
        Buffer.BlockCopy(Triangle.Vertices, 0, data, 0, data.Length);
        var handle = GCHandle.Alloc(data, GCHandleType.Pinned);

        batchCopy.CopyToGPUBuffer(new CopyToGpuBufferDesc()
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
    }

    private void CreatePipeline()
    {
        var programDesc = new ShaderProgramDesc();

        var vsDesc = new ShaderStageDesc
        {
            EntryPoint = StringView.Create("VSMain"),
            Data = ByteArray.Create(Encoding.UTF8.GetBytes(Shaders.VertexShaderSource)),
            Stage = ShaderStage.Vertex
        };

        var psDesc = new ShaderStageDesc
        {
            EntryPoint = StringView.Create("PSMain"),
            Data = ByteArray.Create(Encoding.UTF8.GetBytes(Shaders.PixelShaderSource)),
            Stage = ShaderStage.Pixel
        };

        programDesc.ShaderStages = ShaderStageDescArray.Create([vsDesc, psDesc]);

        var program = new ShaderProgram(programDesc);
        var reflection = program.Reflect();
        _inputLayout = _logicalDevice.CreateInputLayout(reflection.InputLayout);
        _rootSignature = _logicalDevice.CreateRootSignature(reflection.RootSignature);

        var pipelineDesc = new PipelineDesc
        {
            InputLayout = _inputLayout,
            RootSignature = _rootSignature,
            ShaderProgram = program,
            Graphics = new GraphicsPipelineDesc
            {
                PrimitiveTopology = PrimitiveTopology.Triangle,
                RenderTargets = RenderTargetDescArray.Create(
                    [
                        new RenderTargetDesc
                        {
                            Format = Format.B8G8R8A8Unorm,
                            Blend = new BlendDesc
                            {
                                RenderTargetWriteMask = 0x0F
                            }
                        }
                    ]
                )
            },
        };
        _pipeline = _logicalDevice.CreatePipeline(pipelineDesc);
    }
}