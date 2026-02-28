using DenOfIz;

namespace NiziKit.Build;

public class ShadowSmoothShader(string shaderSourceDir) : OfflineShader(shaderSourceDir)
{
    public override string Name => "ShadowSmoothShader";

    protected override List<ShaderStageDesc> Stages()
    {
        var vsDesc = new ShaderStageDesc
        {
            Path = ShaderPath("FullscreenQuad.VS.hlsl"),
            Stage = (uint)ShaderStageFlagBits.Vertex,
            EntryPoint = StringView.Create("VSMain")
        };

        var psDesc = new ShaderStageDesc
        {
            Path = ShaderPath("ShadowSmooth.PS.hlsl"),
            Stage = (uint)ShaderStageFlagBits.Pixel,
            EntryPoint = StringView.Create("PSMain")
        };

        return [vsDesc, psDesc];
    }
}
