using DenOfIz;
using NiziKit.Graphics;

namespace NiziKit.Assets;

public class DefaultShader
{
    public GpuShader StaticVariant { get; } = CreateVariant("DefaultShader");
    public GpuShader SkinnedVariant { get; } = CreateVariant(ShaderVariants.EncodeName("DefaultShader", ShaderVariants.Skinned()));

    private static GpuShader CreateVariant(string shaderName)
    {
        var program = BuiltinShaderProgram.Load(shaderName)
                   ?? throw new InvalidOperationException($"{shaderName} not found");

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
            DepthStencilAttachmentFormat = GraphicsContext.DepthBufferFormat,
            RenderTargets = renderTargets
        };

        return GpuShader.Graphics(program, graphicsDesc);
    }
}