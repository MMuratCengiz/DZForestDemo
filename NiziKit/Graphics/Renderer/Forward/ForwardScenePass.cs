using NiziKit.Graphics.Binding;
using NiziKit.Graphics.Graph;

namespace NiziKit.Graphics.Renderer.Forward;

public class ForwardScenePass : RenderPass
{
    private readonly AttachmentDesc[] _attachments;
    private AttachmentDesc SceneColorDesc { get; set; } = AttachmentDesc.Color("SceneColor");
    private AttachmentDesc SceneDepthDesc { get; set; } = AttachmentDesc.Depth("SceneDepth");

    public override string Name => "Forward Scene Pass";

    private GpuView _gpuView;
    public override ReadOnlySpan<AttachmentDesc> Writes => _attachments.AsSpan();

    public ForwardScenePass(GraphicsContext context) : base(context)
    {
        _attachments =
        [
            SceneColorDesc, SceneDepthDesc
        ];
    }

    public override void Execute(ref RenderPassContext ctx)
    {
    }
}