using DenOfIz;

namespace NiziKit.Build;

public class Present(string shaderSourceDir) : OfflineShader(shaderSourceDir)
{
    public override string Name { get; } = "PresentShader";

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
            Path = ShaderPath("Present.PS.hlsl"),
            Stage = (uint)ShaderStageFlagBits.Pixel,
            EntryPoint = StringView.Create("PSMain")
        };

        return
        [
            vsDesc,
            psDesc
        ];
    }
}
