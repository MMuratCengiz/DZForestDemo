using DenOfIz;
using NiziKit.Assets;
using NiziKit.Graphics.Binding;
using NiziKit.Graphics.Graph;

namespace NiziKit.Graphics.Renderer.Common;

public class BlittingPresentPass : PresentPass
{
    private static readonly string[] ReadAttachments = ["SceneColor"];

    private readonly GpuShader _blitShader;
    private readonly CachedBindGroup _bindGroups;
    private readonly Sampler _linearSampler;
    private readonly PinnedArray<RenderingAttachmentDesc> _rtAttachments = new(1);

    public override string Name => "Blitting Present Pass";
    public override ReadOnlySpan<string> Reads => ReadAttachments;

    public BlittingPresentPass(GraphicsContext context) : base(context)
    {
        var shaderProgram = BuiltinShader.Load("BlitShader")
                            ?? throw new InvalidOperationException("BlitShader not found");

        var blendDesc = new BlendDesc
        {
            Enable = false,
            RenderTargetWriteMask = 0x0F
        };

        var renderTarget = new RenderTargetDesc
        {
            Format = context.BackBufferFormat,
            Blend = blendDesc
        };

        using var renderTargets = RenderTargetDescArray.Create([renderTarget]);

        var graphicsDesc = new GraphicsPipelineDesc
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
        };

        _blitShader = GpuShader.Graphics(context, shaderProgram, graphicsDesc);

        _linearSampler = context.LogicalDevice.CreateSampler(new SamplerDesc
        {
            AddressModeU = SamplerAddressMode.ClampToEdge,
            AddressModeV = SamplerAddressMode.ClampToEdge,
            AddressModeW = SamplerAddressMode.ClampToEdge,
            MinFilter = Filter.Linear,
            MagFilter = Filter.Linear,
            MipmapMode = MipmapMode.Nearest
        });

        var reflection = shaderProgram.Reflect();
        var bindGroupLayoutDescs = reflection.BindGroupLayouts.ToArray();
        var blitBindGroupLayout = context.LogicalDevice.CreateBindGroupLayout(bindGroupLayoutDescs[0]);

        _bindGroups = new CachedBindGroup(context.LogicalDevice, blitBindGroupLayout, (int)context.NumFrames, 4);
   
    }

    public override void Execute(ref RenderPassContext ctx, Texture swapChainImage)
    {
        var cmd = ctx.CommandList;
        var sceneColor = ctx.GetTexture("SceneColor");
        
        ctx.ResourceTracking.TransitionTexture(
            cmd,
            sceneColor,
            (uint)ResourceUsageFlagBits.ShaderResource,
            QueueType.Graphics);
        
        ctx.ResourceTracking.TransitionTexture(
            cmd,
            swapChainImage,
            (uint)ResourceUsageFlagBits.RenderTarget,
            QueueType.Graphics);

        _bindGroups.BeginUpdate((int)ctx.FrameIndex);
        _bindGroups.SrvTexture(0, sceneColor);
        _bindGroups.SrvTexture(1, Context.NullTexture.Texture);
        _bindGroups.Sampler(0, _linearSampler);
        var bindGroup = _bindGroups.EndUpdate();

        _rtAttachments[0] = new RenderingAttachmentDesc
        {
            Resource = swapChainImage,
            LoadOp = LoadOp.DontCare,
            StoreOp = StoreOp.Store
        };

        var renderingDesc = new RenderingDesc
        {
            RTAttachments = RenderingAttachmentDescArray.FromPinned(_rtAttachments.Handle, 1),
            NumLayers = 1
        };

        cmd.BeginRendering(renderingDesc);

        cmd.BindViewport(0, 0, ctx.Width, ctx.Height);
        cmd.BindScissorRect(0, 0, ctx.Width, ctx.Height);

        cmd.BindPipeline(_blitShader.Pipeline);
        cmd.BindGroup(bindGroup);

        cmd.Draw(3, 1, 0, 0);

        cmd.EndRendering();

        ctx.ResourceTracking.TransitionTexture(
            cmd,
            swapChainImage,
            (uint)ResourceUsageFlagBits.Present,
            QueueType.Graphics);
    }

    public override void Dispose()
    {
        _blitShader.Dispose();
        _linearSampler.Dispose();
        _rtAttachments.Dispose();
        _bindGroups.Dispose();
        base.Dispose();
    }
}