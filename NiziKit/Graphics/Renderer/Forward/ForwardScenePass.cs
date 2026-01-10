using DenOfIz;
using NiziKit.Assets;
using NiziKit.Graphics.Batching;
using NiziKit.Graphics.Binding;
using NiziKit.Graphics.Graph;

namespace NiziKit.Graphics.Renderer.Forward;

public class ForwardScenePass : RenderPass
{
    private readonly AttachmentDesc[] _attachments;
    private readonly AttachmentDesc _sceneColorDesc = AttachmentDesc.Color("SceneColor");
    private readonly AttachmentDesc _sceneDepthDesc = AttachmentDesc.Depth("SceneDepth");

    private readonly RenderScene _renderScene;
    private readonly GpuView _gpuView;
    private readonly GpuDrawBatcher _drawBatcher;
    private readonly DefaultMaterial _defaultMaterial;

    private readonly PinnedArray<RenderingAttachmentDesc> _colorAttachments = new(1);
    private readonly PinnedArray<RenderingAttachmentDesc> _depthAttachments = new(1);

    public override string Name => "Forward Scene Pass";
    public override ReadOnlySpan<AttachmentDesc> Writes => _attachments.AsSpan();

    public NiziKit.Assets.Assets? Assets { get; set; }

    public ForwardScenePass(
        GraphicsContext context,
        RenderScene renderScene,
        GpuView gpuView,
        GpuDrawBatcher drawBatcher,
        DefaultMaterial defaultMaterial) : base(context)
    {
        _renderScene = renderScene;
        _gpuView = gpuView;
        _drawBatcher = drawBatcher;
        _defaultMaterial = defaultMaterial;

        _attachments = [_sceneColorDesc, _sceneDepthDesc];
    }

    public override void Execute(ref RenderPassContext ctx)
    {
        if (Assets == null || _defaultMaterial.GpuShader == null)
        {
            return;
        }

        var cmd = ctx.CommandList;
        var sceneColor = ctx.GetTexture("SceneColor");

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

        var gpuShader = _defaultMaterial.GpuShader;
        cmd.BindPipeline(gpuShader.Pipeline);

        var cameraBindGroup = _gpuView.GetBindGroup(ctx.FrameIndex);
        cmd.BindGroup(cameraBindGroup);

        var drawBindGroup = _drawBatcher.GetInstanceBindGroup(ctx.FrameIndex);
        cmd.BindGroup(drawBindGroup);

        foreach (var draw in _drawBatcher.StaticDraws)
        {
            var mesh = Assets.GetMeshById(draw.Mesh);
            if (mesh == null)
            {
                continue;
            }

            cmd.BindVertexBuffer(mesh.VertexBuffer.View.Buffer, 0, mesh.VertexBuffer.Stride, mesh.VertexBuffer.View.Offset);
            cmd.BindIndexBuffer(mesh.IndexBuffer.View.Buffer, mesh.IndexBuffer.IndexType, mesh.IndexBuffer.View.Offset);

            cmd.DrawIndexed(
                (uint)mesh.IndexCount,
                (uint)draw.InstanceCount,
                0,
                0,
                (uint)draw.InstanceOffset);
        }

        var skinnedBindGroup = _drawBatcher.GetSkinnedBindGroup(ctx.FrameIndex);
        cmd.BindGroup(skinnedBindGroup);

        foreach (var draw in _drawBatcher.SkinnedDraws)
        {
            var mesh = Assets.GetMeshById(draw.Mesh);
            if (mesh == null)
            {
                continue;
            }

            cmd.BindVertexBuffer(mesh.VertexBuffer.View.Buffer, 0, mesh.VertexBuffer.Stride, mesh.VertexBuffer.View.Offset);
            cmd.BindIndexBuffer(mesh.IndexBuffer.View.Buffer, mesh.IndexBuffer.IndexType, mesh.IndexBuffer.View.Offset);

            cmd.DrawIndexed(
                (uint)mesh.IndexCount,
                (uint)draw.InstanceCount,
                0,
                0,
                (uint)draw.InstanceOffset);
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
