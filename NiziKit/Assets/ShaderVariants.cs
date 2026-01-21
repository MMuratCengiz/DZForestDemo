namespace NiziKit.Assets;

public static class ShaderVariants
{
    private const char Separator = '_';

    public static string EncodeName(string baseName, params string[] defines)
    {
        if (defines.Length == 0)
        {
            return baseName;
        }

        var sorted = defines.OrderBy(k => k, StringComparer.Ordinal);
        return baseName + Separator + string.Join(Separator, sorted);
    }

    public static string EncodeName(string baseName, IReadOnlyDictionary<string, string?>? defines)
    {
        if (defines == null || defines.Count == 0)
        {
            return baseName;
        }

        var sorted = defines.Keys.OrderBy(k => k, StringComparer.Ordinal);
        return baseName + Separator + string.Join(Separator, sorted);
    }

    public static Dictionary<string, string?> ToDefines(params string[] keys)
    {
        var dict = new Dictionary<string, string?>();
        foreach (var key in keys)
        {
            dict[key] = null;
        }
        return dict;
    }

    public static Dictionary<string, string?> ToDefines(IEnumerable<string> keys)
    {
        var dict = new Dictionary<string, string?>();
        foreach (var key in keys)
        {
            dict[key] = null;
        }
        return dict;
    }
}
