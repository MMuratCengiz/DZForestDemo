using DenOfIz;

namespace NiziKit.Build;

public class ParticleSystemRasterShader(string shaderSourceDir) : OfflineShader(shaderSourceDir)
{
    public override string Name => "ParticleSystemRasterShader";

    protected override List<ShaderStageDesc> Stages()
    {
        var vsDesc = new ShaderStageDesc
        {
            Path = ShaderPath("Particles/Particle.VS.hlsl"),
            Stage = (uint)ShaderStageFlagBits.Vertex,
            EntryPoint = StringView.Create("VSMain")
        };

        var psDesc = new ShaderStageDesc
        {
            Path = ShaderPath("Particles/Particle.PS.hlsl"),
            Stage = (uint)ShaderStageFlagBits.Pixel,
            EntryPoint = StringView.Create("PSMain")
        };

        return [vsDesc, psDesc];
    }
}
