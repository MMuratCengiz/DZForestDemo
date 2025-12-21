using DenOfIz;

namespace Graphics.Shader;

public sealed class Shader(ShaderRootSignature rootSignature) : IDisposable
{
    private readonly Dictionary<string, ShaderVariant> _variants = new();
    private bool _disposed;

    public ShaderRootSignature RootSignature { get; } = rootSignature;

    public Pipeline GetPipeline(string variantName = "default")
    {
        return _variants[variantName].Pipeline;
    }

    public bool TryGetPipeline(string variantName, out Pipeline pipeline)
    {
        if (_variants.TryGetValue(variantName, out var variant))
        {
            pipeline = variant.Pipeline;
            return true;
        }
        pipeline = null!;
        return false;
    }

    public void AddVariant(string variantName, ShaderVariant variant)
    {
        _variants.Add(variantName, variant);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        foreach (var variant in _variants.Values)
        {
            variant.Pipeline.Dispose();
            variant.Program.Dispose();
        }
        _variants.Clear();
        RootSignature.Dispose();
        GC.SuppressFinalize(this);
    }
}
