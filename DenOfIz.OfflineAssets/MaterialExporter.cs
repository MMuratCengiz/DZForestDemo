using System.Numerics;
using SharpGLTF.Schema2;

namespace OfflineAssets;

public static class DzMatFormat
{
    public const string Magic = "DZMAT";
    public const byte CurrentVersion = 1;
}

public sealed class MaterialExportSettings
{
    public required string OutputPath { get; set; }
    public bool CopyTextures { get; set; } = true;
    public string? TextureOutputDirectory { get; set; }
}

public sealed class MaterialExportResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public string? OutputPath { get; init; }
    public IReadOnlyList<string> CopiedTextures { get; init; } = [];

    public static MaterialExportResult Failed(string error) => new()
    {
        Success = false,
        ErrorMessage = error
    };

    public static MaterialExportResult Succeeded(string path, IReadOnlyList<string> copiedTextures) => new()
    {
        Success = true,
        OutputPath = path,
        CopiedTextures = copiedTextures
    };
}

public sealed class MaterialExporter
{
    public MaterialExportResult ExportMaterial(string gltfPath, int materialIndex, MaterialExportSettings settings)
    {
        if (!File.Exists(gltfPath))
            return MaterialExportResult.Failed($"File not found: {gltfPath}");

        try
        {
            var model = ModelRoot.Load(gltfPath);

            if (materialIndex < 0 || materialIndex >= model.LogicalMaterials.Count)
                return MaterialExportResult.Failed($"Invalid material index: {materialIndex}");

            var material = model.LogicalMaterials[materialIndex];
            var gltfDirectory = Path.GetDirectoryName(gltfPath) ?? "";

            var copiedTextures = new List<string>();

            var baseColorChannel = material.FindChannel("BaseColor");
            var metallicRoughnessChannel = material.FindChannel("MetallicRoughness");
            var normalChannel = material.FindChannel("Normal");

            var baseColor = baseColorChannel?.Color ?? Vector4.One;
            float metallic = 1.0f;
            float roughness = 1.0f;

            if (metallicRoughnessChannel != null)
            {
                foreach (var param in metallicRoughnessChannel.Value.Parameters)
                {
                    if (param.Name == "MetallicFactor")
                        metallic = (float)param.Value;
                    else if (param.Name == "RoughnessFactor")
                        roughness = (float)param.Value;
                }
            }

            string? baseColorTexturePath = null;
            string? normalTexturePath = null;
            string? metallicRoughnessTexturePath = null;

            if (settings.CopyTextures)
            {
                var textureDir = settings.TextureOutputDirectory
                    ?? Path.GetDirectoryName(settings.OutputPath)
                    ?? "";

                Directory.CreateDirectory(textureDir);

                baseColorTexturePath = CopyTexture(baseColorChannel?.Texture, gltfDirectory, textureDir, copiedTextures);
                normalTexturePath = CopyTexture(normalChannel?.Texture, gltfDirectory, textureDir, copiedTextures);
                metallicRoughnessTexturePath = CopyTexture(metallicRoughnessChannel?.Texture, gltfDirectory, textureDir, copiedTextures);
            }
            else
            {
                baseColorTexturePath = GetTexturePath(baseColorChannel?.Texture, gltfDirectory);
                normalTexturePath = GetTexturePath(normalChannel?.Texture, gltfDirectory);
                metallicRoughnessTexturePath = GetTexturePath(metallicRoughnessChannel?.Texture, gltfDirectory);
            }

            WriteDzMat(settings.OutputPath, material.Name ?? "Material", baseColor, metallic, roughness,
                baseColorTexturePath, normalTexturePath, metallicRoughnessTexturePath);

            return MaterialExportResult.Succeeded(settings.OutputPath, copiedTextures);
        }
        catch (Exception ex)
        {
            return MaterialExportResult.Failed($"Failed to export material: {ex.Message}");
        }
    }

    public IReadOnlyList<MaterialExportResult> ExportAllMaterials(
        string gltfPath,
        string outputDirectory,
        bool copyTextures = true)
    {
        var results = new List<MaterialExportResult>();

        if (!File.Exists(gltfPath))
        {
            results.Add(MaterialExportResult.Failed($"File not found: {gltfPath}"));
            return results;
        }

        try
        {
            var model = ModelRoot.Load(gltfPath);
            Directory.CreateDirectory(outputDirectory);

            for (int i = 0; i < model.LogicalMaterials.Count; i++)
            {
                var material = model.LogicalMaterials[i];
                var materialName = material.Name ?? $"Material_{i}";
                var outputPath = Path.Combine(outputDirectory, $"{materialName}.dzmat");

                var result = ExportMaterial(gltfPath, i, new MaterialExportSettings
                {
                    OutputPath = outputPath,
                    CopyTextures = copyTextures,
                    TextureOutputDirectory = Path.Combine(outputDirectory, "Textures")
                });
                results.Add(result);
            }
        }
        catch (Exception ex)
        {
            results.Add(MaterialExportResult.Failed($"Failed to load glTF: {ex.Message}"));
        }

        return results;
    }

    private static string? CopyTexture(
        SharpGLTF.Schema2.Texture? texture,
        string gltfDirectory,
        string outputDirectory,
        List<string> copiedTextures)
    {
        if (texture?.PrimaryImage?.Content == null)
            return null;

        var sourceUri = texture.PrimaryImage.Content.SourcePath;
        if (string.IsNullOrEmpty(sourceUri))
            return null;

        var sourcePath = Path.IsPathRooted(sourceUri)
            ? sourceUri
            : Path.Combine(gltfDirectory, sourceUri);

        if (!File.Exists(sourcePath))
            return null;

        var fileName = Path.GetFileName(sourcePath);
        var destPath = Path.Combine(outputDirectory, fileName);

        if (!File.Exists(destPath))
        {
            File.Copy(sourcePath, destPath);
            copiedTextures.Add(destPath);
        }

        return fileName;
    }

    private static string? GetTexturePath(SharpGLTF.Schema2.Texture? texture, string gltfDirectory)
    {
        if (texture?.PrimaryImage?.Content == null)
            return null;

        var sourceUri = texture.PrimaryImage.Content.SourcePath;
        if (string.IsNullOrEmpty(sourceUri))
            return null;

        return Path.IsPathRooted(sourceUri) ? sourceUri : Path.Combine(gltfDirectory, sourceUri);
    }

    private static void WriteDzMat(
        string outputPath,
        string name,
        Vector4 baseColor,
        float metallic,
        float roughness,
        string? baseColorTexture,
        string? normalTexture,
        string? metallicRoughnessTexture)
    {
        var directory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        using var stream = File.Create(outputPath);
        using var writer = new BinaryWriter(stream);

        writer.Write(System.Text.Encoding.ASCII.GetBytes(DzMatFormat.Magic));
        writer.Write(DzMatFormat.CurrentVersion);

        writer.Write(baseColor.X);
        writer.Write(baseColor.Y);
        writer.Write(baseColor.Z);
        writer.Write(baseColor.W);
        writer.Write(metallic);
        writer.Write(roughness);

        WriteLengthPrefixedString(writer, baseColorTexture);
        WriteLengthPrefixedString(writer, normalTexture);
        WriteLengthPrefixedString(writer, metallicRoughnessTexture);
    }

    private static void WriteLengthPrefixedString(BinaryWriter writer, string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            writer.Write((ushort)0);
            return;
        }

        var bytes = System.Text.Encoding.UTF8.GetBytes(value);
        writer.Write((ushort)bytes.Length);
        writer.Write(bytes);
    }
}
