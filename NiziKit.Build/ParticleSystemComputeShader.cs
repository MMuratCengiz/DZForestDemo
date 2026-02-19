using DenOfIz;

namespace NiziKit.Build;

public class ParticleSystemComputeShader(string shaderSourceDir) : OfflineShader(shaderSourceDir)
{
    public override string Name => "ParticleSystemComputeShader";

    protected override List<ShaderStageDesc> Stages()
    {
        var stageDesc = new ShaderStageDesc
        {
            Path = ShaderPath("Particles/Compute.hlsl"),
            Stage = (uint)ShaderStageFlagBits.Compute,
            EntryPoint = StringView.Create("CSMain")
        };
        return [stageDesc];
    }
}
