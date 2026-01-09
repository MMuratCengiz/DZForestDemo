using System.Text;
using DenOfIz;
using NiziKit.Offline;

namespace NiziKit.Build;

/// <summary>
/// Builds shaders for DenOfIz.Graphics library.
/// Run this project to regenerate built-in shaders.
/// </summary>
public static class Program
{
    // Paths relative to this project's location
    private static readonly string ProjectDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", ".."));
    private static readonly string ShaderSourceDir = Path.Combine(ProjectDir, "Shaders");
    private static readonly string OutputDir = Path.Combine(ProjectDir, "..", "DenOfIz.Graphics", "BuiltInShaders");

    public static int Main(string[] args)
    {
        Console.WriteLine("DenOfIz.Graphics Shader Build Tool");
        Console.WriteLine("===================================");
        Console.WriteLine($"Source Directory: {ShaderSourceDir}");
        Console.WriteLine($"Output Directory: {OutputDir}");
        Console.WriteLine();

        Directory.CreateDirectory(OutputDir);

        var exporter = new ShaderExporter(ShaderSourceDir, OutputDir);
        var success = true;

        // ===========================================
        // Define your built-in shaders here
        // ===========================================

        // Composite shader (blends scene, UI, and debug layers)
        success &= BuildShader(exporter, "Composite", new ShaderBuildDesc
        {
            VertexShader = new ShaderStage("fullscreen_vs.hlsl", "VSMain"),
            PixelShader = new ShaderStage("composite_ps.hlsl", "PSMain")
        });

        Console.WriteLine();
        if (success)
        {
            Console.WriteLine("All shaders built successfully!");
            return 0;
        }

        Console.WriteLine("Some shaders failed to build.");
        return 1;
    }

    private static bool BuildShader(ShaderExporter exporter, string name, ShaderBuildDesc desc)
    {
        Console.WriteLine($"Building shader: {name}");

        try
        {
            var stages = new List<ShaderStageDesc>();

            if (desc.VertexShader != null)
            {
                var source = File.ReadAllText(Path.Combine(ShaderSourceDir, desc.VertexShader.FilePath));
                stages.Add(new ShaderStageDesc
                {
                    Stage = (uint)ShaderStageFlagBits.Vertex,
                    Data = ByteArray.Create(Encoding.UTF8.GetBytes(source)),
                    EntryPoint = StringView.Create(desc.VertexShader.EntryPoint)
                });
            }

            if (desc.PixelShader != null)
            {
                var source = File.ReadAllText(Path.Combine(ShaderSourceDir, desc.PixelShader.FilePath));
                stages.Add(new ShaderStageDesc
                {
                    Stage = (uint)ShaderStageFlagBits.Pixel,
                    Data = ByteArray.Create(Encoding.UTF8.GetBytes(source)),
                    EntryPoint = StringView.Create(desc.PixelShader.EntryPoint)
                });
            }

            if (desc.ComputeShader != null)
            {
                var source = File.ReadAllText(Path.Combine(ShaderSourceDir, desc.ComputeShader.FilePath));
                stages.Add(new ShaderStageDesc
                {
                    Stage = (uint)ShaderStageFlagBits.Compute,
                    Data = ByteArray.Create(Encoding.UTF8.GetBytes(source)),
                    EntryPoint = StringView.Create(desc.ComputeShader.EntryPoint)
                });
            }

            var programDesc = new ShaderProgramDesc
            {
                ShaderStages = ShaderStageDescArray.Create(stages.ToArray())
            };

            exporter.Export(programDesc, $"{name}.dzshader");
            Console.WriteLine($"  -> Success: {name}.dzshader");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  -> FAILED: {ex.Message}");
            return false;
        }
    }
}

public record ShaderStage(string FilePath, string EntryPoint);

public class ShaderBuildDesc
{
    public ShaderStage? VertexShader { get; init; }
    public ShaderStage? PixelShader { get; init; }
    public ShaderStage? ComputeShader { get; init; }
}
