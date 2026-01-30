using DenOfIz;

namespace NiziKit.Build;

public class ShadowCasterShader(string shaderSourceDir) : OfflineShader(shaderSourceDir)
{
    public override string Name => "ShadowCasterShader";

    protected override List<ShaderStageDesc> Stages()
    {
        var vsDesc = new ShaderStageDesc
        {
            Path = ShaderPath("Default/Default.VS.hlsl"),
            Stage = (uint)ShaderStageFlagBits.Vertex,
            EntryPoint = StringView.Create("VSMain")
        };

        var psDesc = new ShaderStageDesc
        {
            Path = ShaderPath("Default/ShadowCaster.PS.hlsl"),
            Stage = (uint)ShaderStageFlagBits.Pixel,
            EntryPoint = StringView.Create("PSMain")
        };

        return [vsDesc, psDesc];
    }
}
