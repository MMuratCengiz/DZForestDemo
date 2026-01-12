using DenOfIz;

namespace NiziKit.Offline;

public sealed class TextureExportSettings
{
    public string SourcePath { get; set; } = string.Empty;
    public string OutputDirectory { get; set; } = string.Empty;
    public string AssetName { get; set; } = string.Empty;
    public bool GenerateMips { get; set; } = true;
    public bool NormalizeNormalMaps { get; set; } = false;
    public bool FlipY { get; set; } = false;

    internal TextureImportDesc ToImportDesc()
    {
        return new TextureImportDesc
        {
            SourceFilePath = StringView.Create(SourcePath),
            TargetDirectory = StringView.Create(OutputDirectory),
            AssetNamePrefix = StringView.Create(AssetName),
            GenerateMips = GenerateMips,
            NormalizeNormalMaps = NormalizeNormalMaps,
            FlipY = FlipY
        };
    }
}

public sealed class TextureExportResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public string? OutputPath { get; init; }

    public static TextureExportResult Failed(string error)
    {
        return new TextureExportResult
        {
            Success = false,
            ErrorMessage = error
        };
    }

    public static TextureExportResult Succeeded(string outputPath)
    {
        return new TextureExportResult
        {
            Success = true,
            OutputPath = outputPath
        };
    }
}

public sealed class TextureExporter : IDisposable
{
    private readonly TextureImporter _textureImporter = new();

    public IReadOnlyList<string> SupportedExtensions
    {
        get
        {
            var extensions = _textureImporter.GetSupportedExtensions();
            var result = new List<string>((int)extensions.NumElements);
            for (var i = 0u; i < extensions.NumElements; i++)
            {
                var ext = extensions.ToArray()[i];
                result.Add(ext.ToString());
            }

            return result;
        }
    }

    public void Dispose()
    {
        _textureImporter.Dispose();
    }

    public bool CanProcess(string filePath)
    {
        var extension = Path.GetExtension(filePath);
        return _textureImporter.CanProcessFileExtension(StringView.Create(extension));
    }

    public bool ValidateFile(string filePath)
    {
        return _textureImporter.ValidateFile(StringView.Create(filePath));
    }

    public TextureExportResult Export(TextureExportSettings settings)
    {
        if (string.IsNullOrEmpty(settings.SourcePath))
        {
            return TextureExportResult.Failed("Source path is required.");
        }

        if (!File.Exists(settings.SourcePath))
        {
            return TextureExportResult.Failed($"Source file not found: {settings.SourcePath}");
        }

        if (string.IsNullOrEmpty(settings.OutputDirectory))
        {
            return TextureExportResult.Failed("Output directory is required.");
        }

        if (string.IsNullOrEmpty(settings.AssetName))
        {
            settings.AssetName = Path.GetFileNameWithoutExtension(settings.SourcePath);
        }

        Directory.CreateDirectory(settings.OutputDirectory);

        var desc = settings.ToImportDesc();
        var result = _textureImporter.Import(in desc);

        if (result.ResultCode == ImporterResultCode.Success)
        {
            // Output filename pattern: {AssetNamePrefix}_{OriginalFileName}.dztex
            var originalFileName = Path.GetFileNameWithoutExtension(settings.SourcePath);
            var outputFileName = string.IsNullOrEmpty(settings.AssetName)
                ? $"{originalFileName}.dztex"
                : $"{settings.AssetName}_{originalFileName}.dztex";
            var outputPath = Path.Combine(settings.OutputDirectory, outputFileName);
            return TextureExportResult.Succeeded(outputPath);
        }

        var error = result.ErrorMessage.ToString();
        return TextureExportResult.Failed(string.IsNullOrEmpty(error) ? "Texture import failed." : error);
    }
}
