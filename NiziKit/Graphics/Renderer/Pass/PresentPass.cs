using DenOfIz;
using NiziKit.Assets;
using NiziKit.Graphics.Resources;

namespace NiziKit.Graphics.Renderer.Pass;

public class PresentPass : IDisposable
{
    private readonly ShaderProgram _program;
    private readonly RootSignature _rootSignature;
    private readonly InputLayout _inputLayout;
    private readonly Pipeline _pipeline;
    private readonly BindGroupLayout _bindGroupLayout;
    private readonly Sampler _linearSampler;

    private readonly BindGroup[] _bindGroups;
    private readonly Texture?[] _boundTextures;

    private readonly PinnedArray<RenderingAttachmentDesc> _rtAttachment;

    public PresentPass()
    {
        _program = BuiltinShaderProgram.Load("PresentShader")
                   ?? throw new InvalidOperationException("PresentShader not found");

        var reflection = _program.Reflect();
        var bindGroupLayoutDescs = reflection.BindGroupLayouts.ToArray();
        _bindGroupLayout = GraphicsContext.Device.CreateBindGroupLayout(bindGroupLayoutDescs[0]);

        var rootSigDesc = new RootSignatureDesc
        {
            BindGroupLayouts = BindGroupLayoutArray.Create([_bindGroupLayout]),
        };
        _rootSignature = GraphicsContext.Device.CreateRootSignature(rootSigDesc);
        _inputLayout = GraphicsContext.Device.CreateInputLayout(reflection.InputLayout);

        var blendDesc = new BlendDesc
        {
            Enable = false,
            RenderTargetWriteMask = 0x0F
        };

        var renderTarget = new RenderTargetDesc
        {
            Format = GraphicsContext.BackBufferFormat,
            Blend = blendDesc
        };

        using var renderTargets = RenderTargetDescArray.Create([renderTarget]);

        var pipelineDesc = new PipelineDesc
        {
            RootSignature = _rootSignature,
            InputLayout = _inputLayout,
            ShaderProgram = _program,
            BindPoint = BindPoint.Graphics,
            Graphics = new GraphicsPipelineDesc
            {
                PrimitiveTopology = PrimitiveTopology.Triangle,
                CullMode = CullMode.None,
                FillMode = FillMode.Solid,
                DepthTest = new DepthTest
                {
                    Enable = false,
                    CompareOp = CompareOp.Always,
                    Write = false
                },
                RenderTargets = renderTargets
            }
        };

        _pipeline = GraphicsContext.Device.CreatePipeline(pipelineDesc);

        _linearSampler = GraphicsContext.Device.CreateSampler(new SamplerDesc
        {
            AddressModeU = SamplerAddressMode.ClampToEdge,
            AddressModeV = SamplerAddressMode.ClampToEdge,
            AddressModeW = SamplerAddressMode.ClampToEdge,
            MinFilter = Filter.Linear,
            MagFilter = Filter.Linear,
            MipmapMode = MipmapMode.Nearest
        });

        var numFrames = (int)GraphicsContext.NumFrames;
        _bindGroups = new BindGroup[numFrames];
        _boundTextures = new Texture?[numFrames];

        for (var i = 0; i < numFrames; i++)
        {
            _bindGroups[i] = GraphicsContext.Device.CreateBindGroup(new BindGroupDesc
            {
                Layout = _bindGroupLayout
            });
        }

        _rtAttachment = new PinnedArray<RenderingAttachmentDesc>(1);
    }

    public void Execute(CommandList commandList, CycledTexture sourceTexture)
    {
        var frameIndex = GraphicsContext.FrameIndex;
        var texture = sourceTexture[frameIndex];

        UpdateBindGroup(frameIndex, texture);

        var swapChainImageIndex = GraphicsContext.SwapChain.AcquireNextImage();
        var swapChainImage = GraphicsContext.SwapChain.GetRenderTarget(swapChainImageIndex);

        commandList.Begin();

        GraphicsContext.ResourceTracking.TransitionTexture(
            commandList,
            texture,
            (uint)ResourceUsageFlagBits.ShaderResource,
            QueueType.Graphics);

        GraphicsContext.ResourceTracking.TransitionTexture(
            commandList,
            swapChainImage,
            (uint)ResourceUsageFlagBits.RenderTarget,
            QueueType.Graphics);

        _rtAttachment[0] = new RenderingAttachmentDesc
        {
            Resource = swapChainImage,
            LoadOp = LoadOp.DontCare,
            StoreOp = StoreOp.Store
        };

        var renderingDesc = new RenderingDesc
        {
            RTAttachments = RenderingAttachmentDescArray.FromPinned(_rtAttachment.Handle, 1),
            NumLayers = 1
        };

        commandList.BeginRendering(renderingDesc);

        commandList.BindViewport(0, 0, GraphicsContext.Width, GraphicsContext.Height);
        commandList.BindScissorRect(0, 0, GraphicsContext.Width, GraphicsContext.Height);

        commandList.BindPipeline(_pipeline);
        commandList.BindGroup(_bindGroups[frameIndex]);

        commandList.Draw(3, 1, 0, 0);

        commandList.EndRendering();

        GraphicsContext.ResourceTracking.TransitionTexture(
            commandList,
            swapChainImage,
            (uint)ResourceUsageFlagBits.Present,
            QueueType.Graphics);

        commandList.End();
    }

    private void UpdateBindGroup(int frameIndex, Texture texture)
    {
        if (_boundTextures[frameIndex] == texture)
        {
            return;
        }

        var bindGroup = _bindGroups[frameIndex];
        bindGroup.BeginUpdate();
        bindGroup.SrvTexture(0, texture);
        bindGroup.Sampler(0, _linearSampler);
        bindGroup.EndUpdate();

        _boundTextures[frameIndex] = texture;
    }

    public void Dispose()
    {
        _rtAttachment.Dispose();

        foreach (var bindGroup in _bindGroups)
        {
            bindGroup.Dispose();
        }

        _linearSampler.Dispose();
        _pipeline.Dispose();
        _inputLayout.Dispose();
        _rootSignature.Dispose();
        _bindGroupLayout.Dispose();
        _program.Dispose();
    }
}
