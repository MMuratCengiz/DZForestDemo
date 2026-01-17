using NiziKit.Assets;
using NiziKit.Offline;

namespace NiziKit.Build;

public static class ExporterExtensions
{
    public static void Export(this ShaderExporter exporter, OfflineShader offlineShader)
    {
        exporter.Export(offlineShader.ProgramDesc(), offlineShader.Name);
    }

    public static void Export(this ShaderExporter exporter, OfflineShader offlineShader, Dictionary<string, string?> defines)
    {
        var variantName = ShaderVariants.EncodeName(offlineShader.Name, defines);
        exporter.Export(offlineShader.ProgramDesc(defines), variantName);
    }
}