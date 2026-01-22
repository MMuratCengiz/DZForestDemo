namespace NiziKit.Build;

public static class ExporterExtensions
{
    private const char Separator = '_';

    public static void Export(this ShaderExporter exporter, OfflineShader offlineShader)
    {
        exporter.Export(offlineShader.ProgramDesc(), offlineShader.Name);
    }

    public static void Export(this ShaderExporter exporter, OfflineShader offlineShader, Dictionary<string, string?> defines)
    {
        var variantName = EncodeName(offlineShader.Name, defines);
        exporter.Export(offlineShader.ProgramDesc(defines), variantName);
    }

    private static string EncodeName(string baseName, IReadOnlyDictionary<string, string?>? variants)
    {
        if (variants == null || variants.Count == 0)
        {
            return baseName;
        }
        var sortedKeys = variants.Keys.OrderBy(k => k, StringComparer.Ordinal).ToList();
        return baseName + Separator + string.Join(Separator, sortedKeys);
    }
}
