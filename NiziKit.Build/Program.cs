using System.Runtime.InteropServices;
using DenOfIz;
using NiziKit.Build;

if (args.Length > 0 && args[0] == "build-packs")
{
    if (args.Length < 3)
    {
        Console.WriteLine("Usage: NiziKit.Build build-packs <assets-dir> <output-dir>");
        return 1;
    }

    NiziPackBuilder.BuildAll(args[1], args[2]);
    return 0;
}

if (args.Length > 0 && args[0] == "import-font")
{
    if (args.Length < 3)
    {
        Console.WriteLine("Usage: NiziKit.Build import-font <source-font.otf|ttf> <output-dir> [font-size]");
        return 1;
    }

    DenOfIzRuntime.Initialize();

    var fontSourcePath = args[1];
    var fontOutputDir = args[2];
    var fontSize = args.Length > 3 ? uint.Parse(args[3]) : 32u;

    Console.WriteLine($"Importing font: {fontSourcePath}");
    Console.WriteLine($"Output directory: {fontOutputDir}");
    Console.WriteLine($"Font size: {fontSize}");

    Directory.CreateDirectory(fontOutputDir);

    using var importer = new FontImporter();

    // FontAwesome Unicode ranges
    var ranges = new UnicodeRange[]
    {
        new() { Start = 0x0020, End = 0x007F },   // Basic ASCII (space, etc.)
        new() { Start = 0xE000, End = 0xE0FF },   // Private Use Area (some FA icons)
        new() { Start = 0xE100, End = 0xE5FF },   // Extended Private Use Area
        new() { Start = 0xF000, End = 0xF8FF }    // Main FontAwesome range
    };

    var rangesHandle = GCHandle.Alloc(ranges, GCHandleType.Pinned);
    try
    {
        var importDesc = new FontImportDesc
        {
            SourceFilePath = StringView.Create(fontSourcePath),
            TargetDirectory = StringView.Create(fontOutputDir),
            AssetNamePrefix = StringView.Create(""),
            InitialFontSize = fontSize,
            AtlasWidth = 0,
            AtlasHeight = 0,
            CustomRanges = new UnicodeRangeArray
            {
                Elements = rangesHandle.AddrOfPinnedObject(),
                NumElements = (ulong)ranges.Length
            }
        };

        var result = importer.Import(importDesc);
        if (result.ResultCode != ImporterResultCode.Success)
        {
            Console.WriteLine($"ERROR: Failed to import font: {result.ErrorMessage}");
            return 1;
        }

        Console.WriteLine("Font imported successfully!");
        Console.WriteLine($"  Output: {fontOutputDir}");
    }
    finally
    {
        rangesHandle.Free();
    }

    return 0;
}

DenOfIzRuntime.Initialize();

var projectDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", ".."));
var shaderSourceDir = Path.Combine(projectDir, "..", "NiziKit", "Shaders");
var outputDir = Path.Combine(projectDir, "..", "NiziKit", "Graphics", "BuiltInShaders");

Directory.CreateDirectory(outputDir);

var exporter = new ShaderExporter(outputDir);
exporter.Export(new Blit(shaderSourceDir));
exporter.Export(new Present(shaderSourceDir));

var defaultShaderOffline = new DefaultShader(shaderSourceDir);
exporter.Export(defaultShaderOffline);
exporter.Export(defaultShaderOffline, new Dictionary<string, string?> { ["SKINNED"] = null });

exporter.Export(new ShadowCasterShader(shaderSourceDir));
exporter.Export(new ShadowCasterShader(shaderSourceDir), new Dictionary<string, string?> { ["SKINNED"] = null });

exporter.Export(new ShadowSmoothShader(shaderSourceDir));
exporter.Export(new SkyboxShader(shaderSourceDir));
exporter.Export(new GizmoShader(shaderSourceDir));
exporter.Export(new GridShader(shaderSourceDir));
exporter.Export(new ParticleSystemComputeShader(shaderSourceDir));
exporter.Export(new ParticleSystemRasterShader(shaderSourceDir));
exporter.Export(new SpriteShader(shaderSourceDir));

return 0;
