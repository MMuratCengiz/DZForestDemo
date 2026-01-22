using DenOfIz;
using BinaryWriter = DenOfIz.BinaryWriter;

namespace NiziKit.Build;

public class ShaderExporter(string output)
{
    public void Export(ShaderProgramDesc programDesc, string relativeOutputPath)
    {
        ExportInternal(programDesc, relativeOutputPath);
    }

    private void ExportInternal(ShaderProgramDesc programDesc, string relativeOutputPath)
    {
        var program = new ShaderProgram(programDesc);
        try
        {
            var compiledShaderStages = program.CompiledShaders();

            var reflection = program.Reflect();

            var compiledShader = new CompiledShader
            {
                Stages = compiledShaderStages,
                ReflectDesc = reflection,
                RayTracing = new ShaderRayTracingDesc() // TODO
            };

            var outputPath = Path.Combine(output, relativeOutputPath);
            var outputDir = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }

            var asset = ShaderAssetWriter.CreateFromCompiledShader(compiledShader);
            asset.SetPath(StringView.Create(outputPath));

            var binaryWriter = BinaryWriter.CreateFromFile(StringView.Create(outputPath));
            var assetWriterDesc = new ShaderAssetWriterDesc
            {
                Writer = binaryWriter,
            };
            var assetWriter = new ShaderAssetWriter(assetWriterDesc);
            assetWriter.Write(asset);
            assetWriter.End();
            assetWriter.Dispose();
            binaryWriter.Dispose();
        }
        finally
        {
            program.Dispose();
        }
    }

}
