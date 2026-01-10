using DenOfIz;

namespace NiziKit.Build;

public class DefaultShader(string shaderSourceDir) : OfflineShader(shaderSourceDir)
{
    public override string Name => "DefaultShader";

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
            Path = ShaderPath("Default/Default.PS.hlsl"),
            Stage = (uint)ShaderStageFlagBits.Pixel,
            EntryPoint = StringView.Create("PSMain")
        };

        return [vsDesc, psDesc];
    }
}
