using DenOfIz;
using Microsoft.Extensions.Logging;
using NiziKit.Core;
using NiziKit.Graphics;
using System.Collections.Generic;

namespace NiziKit.Assets;

public class DefaultShader : IDisposable
{
    public GpuShader StaticVariant { get; private set; } = null!;
    public GpuShader SkinnedVariant { get; private set; } = null!;
    public GpuShader ShadowCasterVariant { get; private set; } = null!;
    public GpuShader ShadowCasterSkinnedVariant { get; private set; } = null!;

    private static readonly Dictionary<string, string?> SkinnedDefines = new() { ["SKINNED"] = null };

    public DefaultShader()
    {
        Build();
    }

    public bool Rebuild()
    {
        try
        {
            var newStatic = CreateVariant("DefaultShader",
                "Default/Default.VS.hlsl", "Default/Default.PS.hlsl", null);
            var newSkinned = CreateVariant("DefaultShader_SKINNED",
                "Default/Default.VS.hlsl", "Default/Default.PS.hlsl", SkinnedDefines);
            var newShadow = CreateShadowCasterVariant("ShadowCasterShader",
                "Default/Default.VS.hlsl", "Default/ShadowCaster.PS.hlsl", null);
            var newShadowSkinned = CreateShadowCasterVariant("ShadowCasterShader_SKINNED",
                "Default/Default.VS.hlsl", "Default/ShadowCaster.PS.hlsl", SkinnedDefines);

            var oldStatic = StaticVariant;
            var oldSkinned = SkinnedVariant;
            var oldShadow = ShadowCasterVariant;
            var oldShadowSkinned = ShadowCasterSkinnedVariant;

            StaticVariant = newStatic;
            SkinnedVariant = newSkinned;
            ShadowCasterVariant = newShadow;
            ShadowCasterSkinnedVariant = newShadowSkinned;

            oldStatic.Dispose();
            oldSkinned.Dispose();
            oldShadow.Dispose();
            oldShadowSkinned.Dispose();

            return true;
        }
        catch (Exception ex)
        {
            Log.Get("ShaderHotReload").LogError(ex, "Failed to rebuild DefaultShader");
            return false;
        }
    }

    private void Build()
    {
        StaticVariant = CreateVariant("DefaultShader",
            "Default/Default.VS.hlsl", "Default/Default.PS.hlsl", null);
        SkinnedVariant = CreateVariant("DefaultShader_SKINNED",
            "Default/Default.VS.hlsl", "Default/Default.PS.hlsl", SkinnedDefines);
        ShadowCasterVariant = CreateShadowCasterVariant("ShadowCasterShader",
            "Default/Default.VS.hlsl", "Default/ShadowCaster.PS.hlsl", null);
        ShadowCasterSkinnedVariant = CreateShadowCasterVariant("ShadowCasterShader_SKINNED",
            "Default/Default.VS.hlsl", "Default/ShadowCaster.PS.hlsl", SkinnedDefines);
    }

    private static ShaderProgram LoadProgram(
        string builtinName, string vsRelPath, string psRelPath, Dictionary<string, string?>? defines)
    {
        if (ShaderHotReload.IsEnabled && ShaderHotReload.EngineShaderDirectory != null)
        {
            var vsPath = Path.Combine(ShaderHotReload.EngineShaderDirectory, vsRelPath);
            var psPath = Path.Combine(ShaderHotReload.EngineShaderDirectory, psRelPath);

            if (File.Exists(vsPath) && File.Exists(psPath))
            {
                return CompileFromSource(vsPath, psPath, defines);
            }
        }

        return BuiltinShaderProgram.Load(builtinName)
               ?? throw new InvalidOperationException($"{builtinName} not found");
    }

    private static ShaderProgram CompileFromSource(
        string vsPath, string psPath, Dictionary<string, string?>? defines)
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

        if (defines is { Count: > 0 })
        {
            var definesArray = CreateDefinesArray(defines);
            vsDesc.Defines = definesArray;
            psDesc.Defines = definesArray;
        }

        using var stagesArray = ShaderStageDescArray.Create([vsDesc, psDesc]);
        return new ShaderProgram(new ShaderProgramDesc
        {
            ShaderStages = stagesArray
        });
    }

    private static StringViewArray CreateDefinesArray(Dictionary<string, string?> defines)
    {
        var defineStrings = new List<StringView>();
        foreach (var (key, value) in defines)
        {
            var defineStr = value != null ? $"{key}={value}" : $"{key}=1";
            defineStrings.Add(StringView.Create(defineStr));
        }
        return StringViewArray.Create(defineStrings.ToArray());
    }

    private static GpuShader CreateVariant(
        string builtinName, string vsRel, string psRel, Dictionary<string, string?>? defines)
    {
        var program = LoadProgram(builtinName, vsRel, psRel, defines);

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

    private static GpuShader CreateShadowCasterVariant(
        string builtinName, string vsRel, string psRel, Dictionary<string, string?>? defines)
    {
        var program = LoadProgram(builtinName, vsRel, psRel, defines);

        using var renderTargets = RenderTargetDescArray.Create([]);

        var graphicsDesc = new GraphicsPipelineDesc
        {
            PrimitiveTopology = PrimitiveTopology.Triangle,
            CullMode = CullMode.FrontFace,
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

    public void Dispose()
    {
        StaticVariant.Dispose();
        SkinnedVariant.Dispose();
        ShadowCasterVariant.Dispose();
        ShadowCasterSkinnedVariant.Dispose();
    }
}
