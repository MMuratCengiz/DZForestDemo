using DenOfIz;
using NiziKit.Assets;
using NiziKit.Graphics.Binding;
using NiziKit.Graphics.Graph;

namespace NiziKit.Graphics.Renderer.Forward;

public class ForwardScenePass : RenderPass
{
    private readonly AttachmentDesc[] _attachments;
    private readonly AttachmentDesc _sceneColorDesc = AttachmentDesc.Color("SceneColor");
    private readonly AttachmentDesc _sceneDepthDesc = AttachmentDesc.Depth("SceneDepth");

    private readonly GpuView _gpuView;

    private readonly PinnedArray<RenderingAttachmentDesc> _colorAttachments = new(1);
    private readonly PinnedArray<RenderingAttachmentDesc> _depthAttachments = new(1);
    private readonly GraphicsContext _graphicsContext;

    public override string Name => "Forward Scene Pass";
    public override ReadOnlySpan<AttachmentDesc> Writes => _attachments.AsSpan();

    public NiziKit.Assets.Assets? Assets { get; set; }

    public ForwardScenePass(GraphicsContext context, GpuView gpuView) : base(context)
    {
        _graphicsContext = context;
        _gpuView = gpuView;
        _attachments = [_sceneColorDesc, _sceneDepthDesc];
        CreateTextures(_sceneColorDesc);
        CreateTextures(_sceneDepthDesc);
    }

    public override void Execute(ref RenderPassContext ctx)
    {
        if (Assets == null)
        {
            return;
        }

        var renderWorld = ctx.RenderWorld;
        var cmd = ctx.CommandList;
        var sceneColor = GetAttachment("SceneColor");

        ctx.ResourceTracking.TransitionTexture(
            cmd,
            sceneColor,
            (uint)ResourceUsageFlagBits.RenderTarget,
            QueueType.Graphics);

        _colorAttachments[0] = new RenderingAttachmentDesc
        {
            Resource = sceneColor,
            LoadOp = LoadOp.Clear
        };

        var renderingDesc = new RenderingDesc
        {
            RTAttachments = RenderingAttachmentDescArray.FromPinned(_colorAttachments.Handle, 1),
            NumLayers = 1
        };

        cmd.BeginRendering(renderingDesc);

        cmd.BindViewport(0, 0, ctx.Width, ctx.Height);
        cmd.BindScissorRect(0, 0, ctx.Width, ctx.Height);
        
        var cameraBindGroup = _gpuView.GetBindGroup(ctx.FrameIndex);
        cmd.BindGroup(cameraBindGroup);

        foreach (var material in renderWorld.GetMaterials())
        {
            var gpuShader = material.GpuShader;
            if (gpuShader == null)
            {
                continue;
            }
            cmd.BindPipeline(gpuShader.Pipeline);
            
            var materialBindGroup = GpuMaterial.Get(_graphicsContext, material);
            cmd.BindGroup(materialBindGroup.BindGroup);

            foreach (var draw in renderWorld.GetObjects(material))
            {
                var drawBindGroup = GpuDraw.Get(_graphicsContext, (int)ctx.FrameIndex, draw.Owner);
                cmd.BindGroup(drawBindGroup.Get((int)ctx.FrameIndex));

                var mesh = draw.Mesh;
                
                cmd.BindVertexBuffer(mesh.VertexBuffer.View.Buffer, 0, mesh.VertexBuffer.Stride,
                    mesh.VertexBuffer.View.Offset);
                cmd.BindIndexBuffer(mesh.IndexBuffer.View.Buffer, mesh.IndexBuffer.IndexType, mesh.IndexBuffer.View.Offset);

                cmd.DrawIndexed(
                    (uint)mesh.NumIndices,
                    1,
                    0,
                    0,
                    0);
            }
        }
        cmd.EndRendering();

        ctx.ResourceTracking.TransitionTexture(
            cmd,
            sceneColor,
            (uint)ResourceUsageFlagBits.ShaderResource,
            QueueType.Graphics);
    }

    public override void Dispose()
    {
        _colorAttachments.Dispose();
        _depthAttachments.Dispose();
        base.Dispose();
    }
}