using DenOfIz;

namespace Graphics.RenderGraph;

public static class RenderGraphExtensions
{
    public static ResourceHandle CreateScreenRenderTarget(this ref PassBuilder builder, Format format, string name = "")
    {
        return builder.CreateTransientTexture(new TransientTextureDesc
        {
            Format = format,
            Usages = (uint)(ResourceUsageFlagBits.RenderTarget | ResourceUsageFlagBits.ShaderResource),
            Descriptor = (uint)(ResourceDescriptorFlagBits.RenderTarget | ResourceDescriptorFlagBits.Texture),
            DebugName = name
        });
    }

    public static ResourceHandle CreateScreenDepthBuffer(this ref PassBuilder builder, Format format = Format.D32Float, string name = "")
    {
        return builder.CreateTransientTexture(new TransientTextureDesc
        {
            Format = format,
            Usages = (uint)(ResourceUsageFlagBits.DepthWrite | ResourceUsageFlagBits.DepthRead),
            Descriptor = (uint)ResourceDescriptorFlagBits.DepthStencil,
            DebugName = name
        });
    }
}

public static class CommonPasses
{
    public static void AddClearPass(this RenderGraph graph, ResourceHandle target, Float4 clearColor)
    {
        var pinnedAttachments = new PinnedArray<RenderingAttachmentDesc>(1);

        graph.AddPass("Clear",
            (ref RenderPassSetupContext ctx, ref PassBuilder builder) =>
            {
                builder.WriteTexture(target, (uint)ResourceUsageFlagBits.RenderTarget);
            },
            (ref RenderPassExecuteContext ctx) =>
            {
                var cmd = ctx.CommandList;
                var rt = ctx.GetTexture(target);

                ctx.ResourceTracking.TransitionTexture(cmd, rt,
                    (uint)ResourceUsageFlagBits.RenderTarget, QueueType.Graphics);

                pinnedAttachments[0] = new RenderingAttachmentDesc
                {
                    Resource = rt,
                    LoadOp = LoadOp.Clear,
                    ClearColor = clearColor
                };

                var renderingDesc = new RenderingDesc
                {
                    RTAttachments = RenderingAttachmentDescArray.FromPinned(pinnedAttachments.Handle, 1),
                    NumLayers = 1
                };

                cmd.BeginRendering(renderingDesc);
                cmd.EndRendering();
            });
    }

    public static void AddPresentPass(this RenderGraph graph, ResourceHandle swapchainRt)
    {
        graph.AddPass("Present",
            (ref RenderPassSetupContext ctx, ref PassBuilder builder) =>
            {
                builder.ReadTexture(swapchainRt, (uint)ResourceUsageFlagBits.Present);
                builder.HasSideEffects();
            },
            (ref RenderPassExecuteContext ctx) =>
            {
                var cmd = ctx.CommandList;
                var rt = ctx.GetTexture(swapchainRt);

                ctx.ResourceTracking.TransitionTexture(cmd, rt,
                    (uint)ResourceUsageFlagBits.Present, QueueType.Graphics);
            });
    }
}
