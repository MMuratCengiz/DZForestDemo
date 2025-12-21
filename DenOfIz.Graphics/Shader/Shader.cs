using DenOfIz;

namespace Graphics.Shader;

public class Shader(ShaderRootSignature rootSignature)
{
    private readonly Dictionary<string, ShaderVariant> _variants = new();

    public ShaderRootSignature RootSignature { get; } = rootSignature;

    public Pipeline GetPipeline(string variantName)
    {
        return _variants[variantName].Pipeline;
    }
    
    public void AddVariant(string variantName, ShaderVariant variant)
    {
        _variants.Add(variantName, variant);
    }
}