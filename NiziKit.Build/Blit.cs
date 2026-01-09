using DenOfIz;

namespace NiziKit.Build;

public class Blit(string shaderSourceDir) : OfflineShader(shaderSourceDir)
{
    public override string Name { get; } = "BlitShader";

    protected override List<ShaderStageDesc> Stages()
    {
        var vsDesc = new ShaderStageDesc
        {
            Path = ShaderPath("FullscreenQuad.hlsl"),
            Stage = (uint)ShaderStageFlagBits.Vertex,
            EntryPoint = StringView.Create("VSMain")
        };

        var psDesc = new ShaderStageDesc
        {
            Path = ShaderPath("Blit.PS.hlsl"),
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