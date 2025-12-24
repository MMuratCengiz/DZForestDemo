using DenOfIz;

namespace Graphics.Binding;

public class FrequencyShaderBindingPools(LogicalDevice logicalDevice)
{
    private const int NumFrames = 3;

    private struct PerShaderData
    {
        public ShaderRootSignature RootSignature;
        public List<List<ShaderBindingPool?>> BindingPools;
    }

    private readonly Dictionary<ShaderRootSignature, PerShaderData> _perShaderData = new();

    public List<ShaderBindingPool?> GetOrCreateBindingPools(ShaderRootSignature rootSignature, int frameIndex)
    {
        if (_perShaderData.TryGetValue(rootSignature, out var perShaderData))
        {
            return perShaderData.BindingPools[frameIndex];
        }

        var newPerShaderData = new PerShaderData
        {
            RootSignature = rootSignature,
            BindingPools = []
        };

        for (var i = 0; i < NumFrames; i++)
        {
            newPerShaderData.BindingPools.Add(CreateBindingPool(rootSignature));
        }

        _perShaderData[rootSignature] = newPerShaderData;
        return newPerShaderData.BindingPools[frameIndex];
    }

    // Space 5 is reserved for samplers (uses Never frequency behavior)
    private const int SamplerSpace = 5;

    private List<ShaderBindingPool?> CreateBindingPool(ShaderRootSignature rootSignature)
    {
        // 6 slots: 0=Never, 1=PerCamera, 2=PerMaterial, 3=PerDraw, 4=unused, 5=Samplers
        List<ShaderBindingPool?> bindingPools = [null, null, null, null, null, null];
        foreach (var registerSpace in rootSignature.GetRegisterSpaces())
        {
            switch (registerSpace)
            {
                case (int)BindingFrequency.Never:
                    bindingPools[(int)BindingFrequency.Never] =
                        new ShaderBindingPool(logicalDevice, rootSignature, registerSpace, 8);
                    break;
                case (int)BindingFrequency.PerCamera:
                    bindingPools[(int)BindingFrequency.PerCamera] =
                        new ShaderBindingPool(logicalDevice, rootSignature, registerSpace, 16);
                    break;
                case (int)BindingFrequency.PerMaterial:
                    bindingPools[(int)BindingFrequency.PerMaterial] =
                        new ShaderBindingPool(logicalDevice, rootSignature, registerSpace, 64);
                    break;
                case (int)BindingFrequency.PerDraw:
                    bindingPools[(int)BindingFrequency.PerDraw] =
                        new ShaderBindingPool(logicalDevice, rootSignature, registerSpace, 512);
                    break;
                case SamplerSpace:
                    bindingPools[SamplerSpace] =
                        new ShaderBindingPool(logicalDevice, rootSignature, registerSpace, 8);
                    break;
            }
        }

        return bindingPools;
    }
}