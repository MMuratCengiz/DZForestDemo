using NiziKit.Offline;

namespace NiziKit.Build;

public static class ExporterExtensions
{
    public static void Export(this ShaderExporter exporter, OfflineShader offlineShader)
    {
        exporter.Export(offlineShader.ProgramDesc(), offlineShader.Name);
    }
}