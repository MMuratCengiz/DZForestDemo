using System.Numerics;
using DenOfIz;
using NiziKit.Assets;
using NiziKit.Core;
using NiziKit.Graphics.Binding;
using NiziKit.Graphics.Graph;

namespace NiziKit.Graphics.Renderer.Forward;

public class ForwardScenePass : RenderPass
{
    private readonly AttachmentDesc[] _attachments;
    private readonly AttachmentDesc _sceneColorDesc = AttachmentDesc.Color("SceneColor");
    private readonly AttachmentDesc _sceneDepthDesc = AttachmentDesc.Depth("SceneDepth");

    private readonly ViewData _viewData;

    private readonly PinnedArray<RenderingAttachmentDesc> _colorAttachments = new(1);
    private readonly PinnedArray<RenderingAttachmentDesc> _depthAttachments = new(1);

    public override string Name => "Forward Scene Pass";
    public override ReadOnlySpan<AttachmentDesc> Writes => _attachments.AsSpan();

    public ForwardScenePass(ViewData viewData)
    {
        _viewData = viewData;
        _attachments = [_sceneColorDesc, _sceneDepthDesc];
        CreateTextures(_sceneColorDesc);
        CreateTextures(_sceneDepthDesc);
    }

    public override void Execute(ref RenderPassContext ctx)
    {
        var renderWorld = World.RenderWorld;
        var cmd = ctx.CommandList;
        var sceneColor = GetAttachment("SceneColor");
        var sceneDepth = GetAttachment("SceneDepth");

        ctx.ResourceTracking.TransitionTexture(
            cmd,
            sceneDepth,
            (uint)ResourceUsageFlagBits.DepthWrite,
            QueueType.Graphics
        );

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
            DepthAttachment = new RenderingAttachmentDesc
            {
                Resource = sceneDepth,
                LoadOp = LoadOp.Clear,
                ClearDepthStencil = new Vector2(1, 0)
            },
            NumLayers = 1
        };

        cmd.BeginRendering(renderingDesc);

        cmd.BindViewport(0, 0, ctx.Width, ctx.Height);
        cmd.BindScissorRect(0, 0, ctx.Width, ctx.Height);

        var viewBinding = GpuBinding.Get<ViewBinding>(_viewData);
        cmd.BindGroup(viewBinding.BindGroup);

        foreach (var material in renderWorld.GetMaterials())
        {
            var gpuShader = material.GpuShader;
            if (gpuShader == null)
            {
                continue;
            }
            cmd.BindPipeline(gpuShader.Pipeline);

            var materialBinding = GpuBinding.Get<MaterialBinding>(material);
            materialBinding.Update(material);
            cmd.BindGroup(materialBinding.BindGroup);

            foreach (var draw in renderWorld.GetObjects(material))
            {
                var drawBinding = GpuBinding.Get<DrawBinding>(draw.Owner);
                drawBinding.Update(draw.Owner);
                cmd.BindGroup(drawBinding.BindGroup);

                var mesh = draw.Mesh;

                cmd.BindVertexBuffer(mesh.VertexBuffer.View.Buffer, mesh.VertexBuffer.View.Offset, mesh.VertexBuffer.Stride, 0);
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
