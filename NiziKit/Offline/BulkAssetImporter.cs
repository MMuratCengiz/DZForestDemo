using System.Collections.Concurrent;
using DenOfIz;

namespace NiziKit.Offline;

public sealed class BulkImportDesc
{
    public required string SourceDirectory { get; set; }
    public required string OutputDirectory { get; set; }
    public bool ImportModels { get; set; } = true;
    public bool ImportTextures { get; set; } = true;
    public bool PreserveDirectoryStructure { get; set; } = true;
    public float ModelScale { get; set; } = 1.0f;
    public bool GenerateMips { get; set; } = true;
    public int MaxDegreeOfParallelism { get; set; } = Environment.ProcessorCount;
    public OzzSkeleton? ExternalSkeleton { get; set; }
    public List<string> ExcludeDirectories { get; set; } = [];
    public Action<string>? OnProgress { get; set; }
}

public sealed class BulkImportResult
{
    public int ModelsExported { get; init; }
    public int ModelsFailed { get; init; }
    public int TexturesExported { get; init; }
    public int TexturesFailed { get; init; }
    public List<string> Errors { get; init; } = [];

    public bool Success => ModelsFailed == 0 && TexturesFailed == 0;
    public int TotalExported => ModelsExported + TexturesExported;
    public int TotalFailed => ModelsFailed + TexturesFailed;
}

public sealed class BulkAssetImporter : IDisposable
{
    private static readonly string[] ModelExtensions = [".fbx", ".gltf", ".glb", ".obj", ".dae", ".blend"];
    private static readonly string[] TextureExtensions = [".png", ".jpg", ".jpeg", ".tga", ".bmp"];

    public void Dispose()
    {
    }

    public BulkImportResult Import(BulkImportDesc desc)
    {
        if (!Directory.Exists(desc.SourceDirectory))
        {
            return new BulkImportResult
            {
                Errors = [$"Source directory not found: {desc.SourceDirectory}"]
            };
        }

        Directory.CreateDirectory(desc.OutputDirectory);

        var modelFiles = new List<string>();
        var textureFiles = new List<string>();

        var excludeDirs = desc.ExcludeDirectories
            .Select(d => Path.GetFullPath(Path.Combine(desc.SourceDirectory, d)))
            .ToList();

        foreach (var file in Directory.EnumerateFiles(desc.SourceDirectory, "*.*", SearchOption.AllDirectories))
        {
            var fullPath = Path.GetFullPath(file);
            if (excludeDirs.Any(d => fullPath.StartsWith(d, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            var ext = Path.GetExtension(file).ToLowerInvariant();

            if (desc.ImportModels && ModelExtensions.Contains(ext))
            {
                modelFiles.Add(file);
            }
            else if (desc.ImportTextures && TextureExtensions.Contains(ext))
            {
                textureFiles.Add(file);
            }
        }

        var errors = new ConcurrentBag<string>();
        var modelsExported = 0;
        var modelsFailed = 0;
        var texturesExported = 0;
        var texturesFailed = 0;

        var parallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = desc.MaxDegreeOfParallelism
        };

        if (desc.ImportModels && modelFiles.Count > 0)
        {
            Parallel.ForEach(modelFiles, parallelOptions, () => new AssetExporter(), (file, _, exporter) =>
            {
                var result = ExportModel(file, desc, exporter);
                if (result.Success)
                {
                    Interlocked.Increment(ref modelsExported);
                }
                else
                {
                    Interlocked.Increment(ref modelsFailed);
                    errors.Add($"Model '{Path.GetFileName(file)}': {result.ErrorMessage}");
                }
                return exporter;
            }, exporter => exporter.Dispose());
        }

        if (desc.ImportTextures && textureFiles.Count > 0)
        {
            Parallel.ForEach(textureFiles, parallelOptions, () => new TextureExporter(), (file, _, exporter) =>
            {
                var result = ExportTexture(file, desc, exporter);
                if (result.Success)
                {
                    Interlocked.Increment(ref texturesExported);
                }
                else
                {
                    Interlocked.Increment(ref texturesFailed);
                    errors.Add($"Texture '{Path.GetFileName(file)}': {result.ErrorMessage}");
                }
                return exporter;
            }, exporter => exporter.Dispose());
        }

        return new BulkImportResult
        {
            ModelsExported = modelsExported,
            ModelsFailed = modelsFailed,
            TexturesExported = texturesExported,
            TexturesFailed = texturesFailed,
            Errors = errors.ToList()
        };
    }

    private AssetExportResult ExportModel(string sourceFile, BulkImportDesc desc, AssetExporter exporter)
    {
        var relativePath = Path.GetRelativePath(desc.SourceDirectory, sourceFile);
        var relativeDir = Path.GetDirectoryName(relativePath) ?? "";
        var assetName = Path.GetFileNameWithoutExtension(sourceFile);

        var outputDir = desc.PreserveDirectoryStructure
            ? Path.Combine(desc.OutputDirectory, "Models", relativeDir)
            : Path.Combine(desc.OutputDirectory, "Models");

        Directory.CreateDirectory(outputDir);

        var exportDesc = new AssetExportDesc
        {
            SourcePath = sourceFile,
            OutputDirectory = outputDir,
            AssetName = assetName,
            Format = ExportFormat.Glb,
            Scale = desc.ModelScale,
            EmbedTextures = false,
            OverwriteExisting = true,
            OptimizeMeshes = true,
            GenerateNormals = true,
            CalculateTangents = true,
            TriangulateMeshes = true,
            JoinIdenticalVertices = true,
            SmoothNormals = true,
            SmoothNormalsAngle = 80.0f,
            ExportSkeleton = true,
            ExportAnimations = true,
            ExternalSkeleton = desc.ExternalSkeleton
        };

        return exporter.Export(exportDesc);
    }

    private TextureExportResult ExportTexture(string sourceFile, BulkImportDesc desc, TextureExporter exporter)
    {
        var relativePath = Path.GetRelativePath(desc.SourceDirectory, sourceFile);
        var relativeDir = Path.GetDirectoryName(relativePath) ?? "";

        var outputDir = desc.PreserveDirectoryStructure
            ? Path.Combine(desc.OutputDirectory, "Textures", relativeDir)
            : Path.Combine(desc.OutputDirectory, "Textures");

        return exporter.Export(new TextureExportSettings
        {
            SourcePath = sourceFile,
            OutputDirectory = outputDir,
            AssetName = "",
            GenerateMips = desc.GenerateMips
        });
    }
}
