namespace NiziKit.Assets.Serde;

public static class JsonSchemaRegistry
{
    private static readonly Dictionary<string, string> ExtensionToSchema = new(StringComparer.OrdinalIgnoreCase)
    {
        { ".niziscene.json", "niziscene" },
        { ".nizishp.json", "nizishp" },
        { ".nizimat.json", "nizimat" },
        { ".nizipack.json", "nizipack" }
    };

    private static readonly Dictionary<string, string> SchemaCache = new();

    public static string? GetSchemaForFile(string filePath)
    {
        var fileName = Path.GetFileName(filePath);

        foreach (var (ext, schemaName) in ExtensionToSchema)
        {
            if (fileName.EndsWith(ext, StringComparison.OrdinalIgnoreCase))
            {
                return GetSchema(schemaName);
            }
        }

        return null;
    }

    public static string? GetSchema(string schemaName)
    {
        if (SchemaCache.TryGetValue(schemaName, out var cached))
        {
            return cached;
        }

        var resourceName = $"NiziKit.Schemas.{schemaName}.schema.json";
        var assembly = typeof(JsonSchemaRegistry).Assembly;

        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
        {
            return null;
        }

        using var reader = new StreamReader(stream);
        var schemaJson = reader.ReadToEnd();
        SchemaCache[schemaName] = schemaJson;
        return schemaJson;
    }

    public static IEnumerable<string> GetAvailableSchemas()
    {
        return ExtensionToSchema.Values.Distinct();
    }
}
