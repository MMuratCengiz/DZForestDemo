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

        var newPerShaderData = new PerShaderData();
        _perShaderData[rootSignature] = newPerShaderData;
        newPerShaderData.RootSignature = rootSignature;
        newPerShaderData.BindingPools = [];
        for (var i = 0; i < NumFrames; i++)
        {
            newPerShaderData.BindingPools.AddRange(CreateBindingPool(rootSignature));
        }

        return newPerShaderData.BindingPools[frameIndex];
    }

    private List<ShaderBindingPool?> CreateBindingPool(ShaderRootSignature rootSignature)
    {
        List<ShaderBindingPool?> bindingPools = [null, null, null, null];
        foreach (var registerSpace in rootSignature.GetRegisterSpaces())
        {
            switch (registerSpace)
            {
                case (int)BindingFrequency.Never:
                    bindingPools[(int)BindingFrequency.Never] =
                        new ShaderBindingPool(logicalDevice, rootSignature, registerSpace, 8);
                    break;
                case (int)BindingFrequency.PerCamera:
                    bindingPools[(int)BindingFrequency.Never] =
                        new ShaderBindingPool(logicalDevice, rootSignature, registerSpace, 16);
                    break;
                case (int)BindingFrequency.PerMaterial:
                    bindingPools[(int)BindingFrequency.Never] =
                        new ShaderBindingPool(logicalDevice, rootSignature, registerSpace, 64);
                    break;
                case (int)BindingFrequency.PerDraw:
                    bindingPools[(int)BindingFrequency.PerDraw] =
                        new ShaderBindingPool(logicalDevice, rootSignature, registerSpace, 512);
                    break;
            }
        }

        return bindingPools;
    }
}