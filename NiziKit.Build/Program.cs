using NiziKit.Build;

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

exporter.Export(new GizmoShader(shaderSourceDir));
