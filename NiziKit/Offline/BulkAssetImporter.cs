using System.Collections.Concurrent;

namespace NiziKit.Offline;

public sealed class DirectoryImportSettings
{
    public required string SourceDirectory { get; set; }
    public required string OutputDirectory { get; set; }
    public bool ImportModels { get; set; } = true;
    public bool ImportTextures { get; set; } = true;
    public bool PreserveDirectoryStructure { get; set; } = true;
    public float ModelScale { get; set; } = 1.0f;
    public bool GenerateMips { get; set; } = true;
    public int MaxDegreeOfParallelism { get; set; } = Environment.ProcessorCount;
    public Action<string>? OnProgress { get; set; }
}

public sealed class DirectoryImportResult
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
    private readonly AssetExporter _assetExporter = new();

    private static readonly string[] ModelExtensions = [".fbx", ".gltf", ".glb", ".obj", ".dae", ".blend"];
    private static readonly string[] TextureExtensions = [".png", ".jpg", ".jpeg", ".tga", ".bmp"];

    public void Dispose()
    {
        _assetExporter.Dispose();
    }

    public DirectoryImportResult Import(DirectoryImportSettings settings)
    {
        if (!Directory.Exists(settings.SourceDirectory))
        {
            return new DirectoryImportResult
            {
                Errors = [$"Source directory not found: {settings.SourceDirectory}"]
            };
        }

        Directory.CreateDirectory(settings.OutputDirectory);

        var modelFiles = new List<string>();
        var textureFiles = new List<string>();

        foreach (var file in Directory.EnumerateFiles(settings.SourceDirectory, "*.*", SearchOption.AllDirectories))
        {
            var ext = Path.GetExtension(file).ToLowerInvariant();

            if (settings.ImportModels && ModelExtensions.Contains(ext))
            {
                modelFiles.Add(file);
            }
            else if (settings.ImportTextures && TextureExtensions.Contains(ext))
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
            MaxDegreeOfParallelism = settings.MaxDegreeOfParallelism
        };

        if (settings.ImportModels && modelFiles.Count > 0)
        {
            settings.OnProgress?.Invoke($"Exporting {modelFiles.Count} models...");

            foreach (var file in modelFiles)
            {
                var result = ExportModel(file, settings);
                if (result.Success)
                {
                    modelsExported++;
                    settings.OnProgress?.Invoke($"  Model: {Path.GetFileName(file)}");
                }
                else
                {
                    modelsFailed++;
                    errors.Add($"Model '{Path.GetFileName(file)}': {result.ErrorMessage}");
                }
            }
        }

        if (settings.ImportTextures && textureFiles.Count > 0)
        {
            settings.OnProgress?.Invoke($"Exporting {textureFiles.Count} textures...");

            Parallel.ForEach(textureFiles, parallelOptions, file =>
            {
                var result = ExportTexture(file, settings);
                if (result.Success)
                {
                    Interlocked.Increment(ref texturesExported);
                    settings.OnProgress?.Invoke($"  Texture: {Path.GetFileName(file)}");
                }
                else
                {
                    Interlocked.Increment(ref texturesFailed);
                    errors.Add($"Texture '{Path.GetFileName(file)}': {result.ErrorMessage}");
                }
            });
        }

        return new DirectoryImportResult
        {
            ModelsExported = modelsExported,
            ModelsFailed = modelsFailed,
            TexturesExported = texturesExported,
            TexturesFailed = texturesFailed,
            Errors = errors.ToList()
        };
    }

    private AssetExportResult ExportModel(string sourceFile, DirectoryImportSettings settings)
    {
        var relativePath = Path.GetRelativePath(settings.SourceDirectory, sourceFile);
        var relativeDir = Path.GetDirectoryName(relativePath) ?? "";
        var assetName = Path.GetFileNameWithoutExtension(sourceFile);

        var outputDir = settings.PreserveDirectoryStructure
            ? Path.Combine(settings.OutputDirectory, "Models", relativeDir)
            : Path.Combine(settings.OutputDirectory, "Models");

        Directory.CreateDirectory(outputDir);

        var exportDesc = new AssetExportDesc
        {
            SourcePath = sourceFile,
            OutputDirectory = outputDir,
            AssetName = assetName,
            Format = ExportFormat.Glb,
            Scale = settings.ModelScale,
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
            ExportAnimations = true
        };

        using var exporter = new AssetExporter();
        return exporter.Export(exportDesc);
    }

    private TextureExportResult ExportTexture(string sourceFile, DirectoryImportSettings settings)
    {
        var relativePath = Path.GetRelativePath(settings.SourceDirectory, sourceFile);
        var relativeDir = Path.GetDirectoryName(relativePath) ?? "";
        var fileName = Path.GetFileName(sourceFile);

        var outputDir = settings.PreserveDirectoryStructure
            ? Path.Combine(settings.OutputDirectory, "Textures", relativeDir)
            : Path.Combine(settings.OutputDirectory, "Textures");

        Directory.CreateDirectory(outputDir);

        // Copy texture as-is since Texture2d.Load supports PNG/JPG/TGA/BMP directly
        // (TextureData.CreateFromData does not support .dztex format)
        var outputPath = Path.Combine(outputDir, fileName);
        try
        {
            File.Copy(sourceFile, outputPath, overwrite: true);
            return TextureExportResult.Succeeded(outputPath);
        }
        catch (Exception ex)
        {
            return TextureExportResult.Failed(ex.Message);
        }
    }
}