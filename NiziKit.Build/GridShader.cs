using DenOfIz;

namespace NiziKit.Build;

public class GridShader(string shaderSourceDir) : OfflineShader(shaderSourceDir)
{
    public override string Name { get; } = "GridShader";

    protected override List<ShaderStageDesc> Stages()
    {
        var vsDesc = new ShaderStageDesc
        {
            Path = ShaderPath("Editor/Gizmo.VS.hlsl"),
            Stage = (uint)ShaderStageFlagBits.Vertex,
            EntryPoint = StringView.Create("VSMain")
        };

        var psDesc = new ShaderStageDesc
        {
            Path = ShaderPath("Editor/Grid.PS.hlsl"),
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
