using DenOfIz;
using Microsoft.Extensions.Logging;
using NiziKit.Core;
using NiziKit.Graphics;

namespace NiziKit.Assets;

public class SpriteShader : IDisposable
{
    public GpuShader Shader { get; private set; } = null!;

    public SpriteShader()
    {
        Build();
    }

    public bool Rebuild()
    {
        try
        {
            var newShader = CreateVariant("SpriteShader",
                "Sprite/Sprite.VS.hlsl", "Sprite/Sprite.PS.hlsl");

            var oldShader = Shader;
            Shader = newShader;
            oldShader.Dispose();

            return true;
        }
        catch (Exception ex)
        {
            Log.Get("ShaderHotReload").LogError(ex, "Failed to rebuild SpriteShader");
            return false;
        }
    }

    private void Build()
    {
        Shader = CreateVariant("SpriteShader",
            "Sprite/Sprite.VS.hlsl", "Sprite/Sprite.PS.hlsl");
    }

    private static ShaderProgram LoadProgram(string builtinName, string vsRelPath, string psRelPath)
    {
        if (ShaderHotReload.IsEnabled && ShaderHotReload.EngineShaderDirectory != null)
        {
            var vsPath = Path.Combine(ShaderHotReload.EngineShaderDirectory, vsRelPath);
            var psPath = Path.Combine(ShaderHotReload.EngineShaderDirectory, psRelPath);

            if (File.Exists(vsPath) && File.Exists(psPath))
            {
                return CompileFromSource(vsPath, psPath);
            }
        }

        return BuiltinShaderProgram.Load(builtinName)
               ?? throw new InvalidOperationException($"{builtinName} not found");
    }

    private static ShaderProgram CompileFromSource(string vsPath, string psPath)
    {
        var vsDesc = new ShaderStageDesc
        {
            Stage = (uint)ShaderStageFlagBits.Vertex,
            Path = StringView.Create(vsPath),
            EntryPoint = StringView.Create("VSMain")
        };

        var psDesc = new ShaderStageDesc
        {
            Stage = (uint)ShaderStageFlagBits.Pixel,
            Path = StringView.Create(psPath),
            EntryPoint = StringView.Create("PSMain")
        };

        using var stagesArray = ShaderStageDescArray.Create([vsDesc, psDesc]);
        return new ShaderProgram(new ShaderProgramDesc
        {
            ShaderStages = stagesArray
        });
    }

    private static GpuShader CreateVariant(string builtinName, string vsRel, string psRel)
    {
        var program = LoadProgram(builtinName, vsRel, psRel);

        var blendDesc = new BlendDesc
        {
            Enable = true,
            SrcBlend = Blend.SrcAlpha,
            DstBlend = Blend.InvSrcAlpha,
            BlendOp = BlendOp.Add,
            SrcBlendAlpha = Blend.One,
            DstBlendAlpha = Blend.InvSrcAlpha,
            BlendOpAlpha = BlendOp.Add,
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
            CullMode = CullMode.None,
            FillMode = FillMode.Solid,
            DepthTest = new DepthTest
            {
                Enable = true,
                CompareOp = CompareOp.LessOrEqual,
                Write = true
            },
            DepthStencilAttachmentFormat = GraphicsContext.DepthBufferFormat,
            RenderTargets = renderTargets
        };

        return GpuShader.Graphics(program, graphicsDesc);
    }

    public void Dispose()
    {
        Shader.Dispose();
    }
}
