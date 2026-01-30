using NiziKit.Build;

if (args.Length > 0 && args[0] == "generate-manifest")
{
    if (args.Length < 3)
    {
        Console.WriteLine("Usage: NiziKit.Build generate-manifest <assets-dir> <output-dir>");
        return 1;
    }

    ManifestGenerator.Generate(args[1], args[2]);
    return 0;
}

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

DenOfIz.DenOfIzRuntime.Initialize();

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

exporter.Export(new SkyboxShader(shaderSourceDir));
exporter.Export(new GizmoShader(shaderSourceDir));
exporter.Export(new GridShader(shaderSourceDir));

return 0;
