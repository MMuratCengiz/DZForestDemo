using System.Text;
using DenOfIz;
using Graphics;
using Graphics.RenderGraph;
using RuntimeAssets;

namespace DZForestDemo.RenderPasses;

public sealed class CompositeRenderPass : IDisposable
{
    private readonly ResourceBindGroup[] _bindGroups;
    private readonly Texture?[] _boundDebugTextures;

    private readonly Texture?[] _boundSceneTextures;
    private readonly Texture?[] _boundUiTextures;
    private readonly GraphicsResource _ctx;
    private readonly Sampler _linearSampler;
    private readonly Pipeline _pipeline;

    private readonly RootSignature _rootSignature;
    private readonly PinnedArray<RenderingAttachmentDesc> _rtAttachments;

    private bool _disposed;

    public CompositeRenderPass(GraphicsResource ctx)
    {
        _ctx = ctx;
        var logicalDevice = ctx.LogicalDevice;
        var numFrames = ctx.NumFrames;

        _rtAttachments = new PinnedArray<RenderingAttachmentDesc>(1);
        _boundSceneTextures = new Texture?[numFrames];
        _boundUiTextures = new Texture?[numFrames];
        _boundDebugTextures = new Texture?[numFrames];

        var shaderLoader = new ShaderLoader();
        var vsSource = shaderLoader.Load("fullscreen_vs.hlsl");
        var psSource = shaderLoader.Load("composite_ps.hlsl");

        var programDesc = new ShaderProgramDesc
        {
            ShaderStages = ShaderStageDescArray.Create([
                new ShaderStageDesc
                {
                    EntryPoint = StringView.Create("VSMain"),
                    Data = ByteArray.Create(Encoding.UTF8.GetBytes(vsSource)),
                    Stage = (uint)ShaderStageFlagBits.Vertex
                },
                new ShaderStageDesc
                {
                    EntryPoint = StringView.Create("PSMain"),
                    Data = ByteArray.Create(Encoding.UTF8.GetBytes(psSource)),
                    Stage = (uint)ShaderStageFlagBits.Pixel
                }
            ])
        };

        var program = new ShaderProgram(programDesc);
        var reflection = program.Reflect();
        _rootSignature = logicalDevice.CreateRootSignature(reflection.RootSignature);

        _pipeline = logicalDevice.CreatePipeline(new PipelineDesc
        {
            RootSignature = _rootSignature,
            ShaderProgram = program,
            Graphics = new GraphicsPipelineDesc
            {
                PrimitiveTopology = PrimitiveTopology.Triangle,
                RenderTargets = RenderTargetDescArray.Create([
                    new RenderTargetDesc
                    {
                        Format = ctx.BackBufferFormat,
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

        _bindGroups = new ResourceBindGroup[numFrames];
        for (var i = 0; i < numFrames; i++)
        {
            _bindGroups[i] = logicalDevice.CreateResourceBindGroup(new ResourceBindGroupDesc
            {
                RootSignature = _rootSignature
            });
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        foreach (var bindGroup in _bindGroups)
        {
            bindGroup?.Dispose();
        }

        _linearSampler.Dispose();
        _pipeline.Dispose();
        _rootSignature.Dispose();
        _rtAttachments.Dispose();
    }

    public void AddPass(
        RenderGraph renderGraph,
        ResourceHandle sceneRt,
        ResourceHandle uiRt,
        ResourceHandle debugRt,
        ResourceHandle swapchainRt,
        Viewport viewport)
    {
        renderGraph.AddPass("Composite",
            (ref RenderPassSetupContext ctx, ref PassBuilder builder) =>
            {
                builder.ReadTexture(sceneRt);
                builder.ReadTexture(uiRt);
                builder.ReadTexture(debugRt);
                builder.WriteTexture(swapchainRt);
                builder.HasSideEffects();
            },
            (ref RenderPassExecuteContext ctx) => { Execute(ref ctx, sceneRt, uiRt, debugRt, swapchainRt, viewport); });
    }

    private void Execute(
        ref RenderPassExecuteContext ctx,
        ResourceHandle sceneRtHandle,
        ResourceHandle uiRtHandle,
        ResourceHandle debugRtHandle,
        ResourceHandle swapchainRtHandle,
        Viewport viewport)
    {
        var cmd = ctx.CommandList;
        var sceneTexture = ctx.GetTexture(sceneRtHandle);
        var uiTexture = ctx.GetTexture(uiRtHandle);
        var debugTexture = ctx.GetTexture(debugRtHandle);
        var rt = ctx.GetTexture(swapchainRtHandle);

        ctx.ResourceTracking.TransitionTexture(cmd, sceneTexture,
            (uint)ResourceUsageFlagBits.ShaderResource, QueueType.Graphics);
        ctx.ResourceTracking.TransitionTexture(cmd, rt,
            (uint)ResourceUsageFlagBits.RenderTarget, QueueType.Graphics);

        UpdateBindGroupIfNeeded(sceneTexture, uiTexture, debugTexture, ctx.FrameIndex);

        _rtAttachments[0] = new RenderingAttachmentDesc
        {
            Resource = rt,
            LoadOp = LoadOp.DontCare,
        };

        var renderingDesc = new RenderingDesc
        {
            RTAttachments = RenderingAttachmentDescArray.FromPinned(_rtAttachments.Handle, 1),
            NumLayers = 1
        };

        cmd.BeginRendering(renderingDesc);
        cmd.BindPipeline(_pipeline);
        cmd.BindViewport(viewport.X, viewport.Y, viewport.Width, viewport.Height);
        cmd.BindScissorRect(viewport.X, viewport.Y, viewport.Width, viewport.Height);
        cmd.BindResourceGroup(_bindGroups[ctx.FrameIndex]);
        cmd.Draw(3, 1, 0, 0);
        cmd.EndRendering();

        ctx.ResourceTracking.TransitionTexture(cmd, rt,
            (uint)ResourceUsageFlagBits.Present, QueueType.Graphics);
    }

    private void UpdateBindGroupIfNeeded(
        Texture sceneTexture,
        Texture uiTexture,
        Texture debugTexture,
        uint frameIndex)
    {
        if (_boundSceneTextures[frameIndex] == sceneTexture &&
            _boundUiTextures[frameIndex] == uiTexture &&
            _boundDebugTextures[frameIndex] == debugTexture)
        {
            return;
        }

        _boundSceneTextures[frameIndex] = sceneTexture;
        _boundUiTextures[frameIndex] = uiTexture;
        _boundDebugTextures[frameIndex] = debugTexture;

        var bindGroup = _bindGroups[frameIndex];
        bindGroup.BeginUpdate();
        bindGroup.SrvTexture(0, sceneTexture);
        bindGroup.SrvTexture(1, uiTexture);
        bindGroup.SrvTexture(2, debugTexture);
        bindGroup.Sampler(0, _linearSampler);
        bindGroup.EndUpdate();
    }
}