using NiziKit.Build;
using NiziKit.Offline;

DenOfIz.DenOfIzRuntime.Initialize();

var projectDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", ".."));
var shaderSourceDir = Path.Combine(projectDir, "Shaders");
var outputDir = Path.Combine(projectDir, "..", "NiziKit", "Graphics", "BuiltInShaders");

Directory.CreateDirectory(outputDir);

var exporter = new ShaderExporter(outputDir);
exporter.Export(new Blit(shaderSourceDir));
exporter.Export(new DefaultShader(shaderSourceDir));
exporter.Export(new Present(shaderSourceDir));