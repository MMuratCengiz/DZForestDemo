using DenOfIz;
using NiziKit.ContentPipeline;

namespace NiziKit.Graphics;

public class ShaderBuilder
{
    public ShaderProgram CompileGraphics(
        string vertexPath,
        string pixelPath,
        string vsEntry = "VSMain",
        string psEntry = "PSMain",
        Dictionary<string, string?>? defines = null)
    {
        var vsFullPath = ResolvePath(vertexPath);
        var psFullPath = ResolvePath(pixelPath);

        var vsDesc = new ShaderStageDesc
        {
            Stage = (uint)ShaderStageFlagBits.Vertex,
            Path = StringView.Create(vsFullPath),
            EntryPoint = StringView.Create(vsEntry)
        };

        var psDesc = new ShaderStageDesc
        {
            Stage = (uint)ShaderStageFlagBits.Pixel,
            Path = StringView.Create(psFullPath),
            EntryPoint = StringView.Create(psEntry)
        };

        if (defines is { Count: > 0 })
        {
            var definesArray = CreateDefinesArray(defines);
            vsDesc.Defines = definesArray;
            psDesc.Defines = definesArray;
        }

        using var stagesArray = ShaderStageDescArray.Create([vsDesc, psDesc]);
        var programDesc = new ShaderProgramDesc
        {
            ShaderStages = stagesArray
        };

        return new ShaderProgram(programDesc);
    }

    public ShaderProgram CompileCompute(
        string computePath,
        string csEntry = "CSMain",
        Dictionary<string, string?>? defines = null)
    {
        var csFullPath = ResolvePath(computePath);

        var csDesc = new ShaderStageDesc
        {
            Stage = (uint)ShaderStageFlagBits.Compute,
            Path = StringView.Create(csFullPath),
            EntryPoint = StringView.Create(csEntry)
        };

        if (defines is { Count: > 0 })
        {
            csDesc.Defines = CreateDefinesArray(defines);
        }

        using var stagesArray = ShaderStageDescArray.Create([csDesc]);
        var programDesc = new ShaderProgramDesc
        {
            ShaderStages = stagesArray
        };

        return new ShaderProgram(programDesc);
    }

    public Task<ShaderProgram> CompileGraphicsAsync(
        string vertexPath,
        string pixelPath,
        string vsEntry = "VSMain",
        string psEntry = "PSMain",
        Dictionary<string, string?>? defines = null,
        CancellationToken ct = default)
    {
        return Task.Run(() => CompileGraphics(vertexPath, pixelPath, vsEntry, psEntry, defines), ct);
    }

    public Task<ShaderProgram> CompileComputeAsync(
        string computePath,
        string csEntry = "CSMain",
        Dictionary<string, string?>? defines = null,
        CancellationToken ct = default)
    {
        return Task.Run(() => CompileCompute(computePath, csEntry, defines), ct);
    }

    private static string ResolvePath(string shaderPath)
    {
        var fullPath = Path.IsPathRooted(shaderPath) ? shaderPath : Content.ResolvePath(shaderPath);

        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException($"Shader file not found: {fullPath}");
        }

        return fullPath;
    }

    private static StringViewArray CreateDefinesArray(Dictionary<string, string?> defines)
    {
        var defineStrings = new List<StringView>();
        foreach (var (key, value) in defines)
        {
            var defineStr = value != null ? $"{key}={value}" : $"{key}=1";
            defineStrings.Add(StringView.Create(defineStr));
        }
        return StringViewArray.Create(defineStrings.ToArray());
    }
}
