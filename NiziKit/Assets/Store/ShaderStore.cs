using NiziKit.Graphics;

namespace NiziKit.Assets.Store;

public class ShaderStore : Store<GpuShader>
{
    public GpuShader? Get(string baseName, IReadOnlyDictionary<string, string?>? variants)
    {
        var fullName = ShaderVariants.EncodeName(baseName, variants);
        return _cache.TryGetValue(fullName, out var shader) ? shader : null;
    }

    public bool Contains(string baseName, IReadOnlyDictionary<string, string?>? variants)
    {
        var fullName = ShaderVariants.EncodeName(baseName, variants);
        return _cache.ContainsKey(fullName);
    }
}
