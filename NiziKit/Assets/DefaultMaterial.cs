using DenOfIz;
using NiziKit.Graphics;

namespace NiziKit.Assets;

public class DefaultMaterial : Material
{
    public DefaultMaterial()
    {
        Name = "Default";
    }

    protected override ShaderProgram LoadShaderProgram()
    {
        return BuiltinShader.Load("DefaultShader")
               ?? throw new InvalidOperationException("DefaultShader not found");
    }

    protected override GraphicsPipelineDesc ConfigurePipeline(GraphicsContext context)
    {
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

        return new GraphicsPipelineDesc
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
    }
}
