using DenOfIz;
using NiziKit.Graphics;

namespace NiziKit.Assets;

public class DefaultShader
{
    public GpuShader Value { get; }

    public DefaultShader(GraphicsContext context)
    {
        var program = BuiltinShaderProgram.Load("DefaultShader")
                   ?? throw new InvalidOperationException("DefaultShader not found");
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
            CullMode = CullMode.BackFace,
            FillMode = FillMode.Solid,
            DepthTest = new DepthTest
            {
                Enable = true,
                CompareOp = CompareOp.Less,
                Write = true
            },
            DepthStencilAttachmentFormat = context.DepthBufferFormat,
            RenderTargets = renderTargets
        };

        Value = GpuShader.Graphics(context, program, graphicsDesc);
    }
}