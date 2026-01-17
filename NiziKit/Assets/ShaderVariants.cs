namespace NiziKit.Assets;

public static class ShaderVariants
{
    private const char Separator = '_';

    public static class Keys
    {
        public const string Skinned = "SKINNED";
    }

    public static string EncodeName(string baseName, IReadOnlyDictionary<string, string?>? variants)
    {
        if (variants == null || variants.Count == 0)
        {
            return baseName;
        }

        var sortedKeys = variants.Keys.OrderBy(k => k, StringComparer.Ordinal).ToList();
        return baseName + Separator + string.Join(Separator, sortedKeys);
    }

    public static Dictionary<string, string?> FromKeys(params string[] keys)
    {
        var dict = new Dictionary<string, string?>();
        foreach (var key in keys)
        {
            dict[key] = null;
        }
        return dict;
    }

    public static Dictionary<string, string?> Skinned() => FromKeys(Keys.Skinned);

    public static Dictionary<string, string?> Static() => new();
}
