using DenOfIz;

namespace Graphics.Shaders;

public class Shader(RootSignature rootSignature)
{
    private readonly Dictionary<string, ShaderVariant> _variants = new();

    public RootSignature RootSignature { get; } = rootSignature;

    public Pipeline GetPipeline(string variantName)
    {
        return _variants[variantName].Pipeline;
    }
    
    public void AddVariant(string variantName, ShaderVariant variant)
    {
        _variants.Add(variantName, variant);
    }
}