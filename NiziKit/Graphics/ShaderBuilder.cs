using System.Text;
using System.Text.RegularExpressions;
using DenOfIz;
using NiziKit.Assets.Serde;
using NiziKit.ContentPipeline;

namespace NiziKit.Graphics;

public partial class ShaderBuilder
{
    [GeneratedRegex("""#include\s*["<]([^">]+)[">]""", RegexOptions.Multiline)]
    private static partial Regex IncludeRegex();

    public ShaderProgram CompileGraphics(
        string vertexPath,
        string pixelPath,
        string vsEntry = "VSMain",
        string psEntry = "PSMain",
        Dictionary<string, string?>? defines = null)
    {
        var includeHandler = new ShaderIncludeHandler();
        var loadedFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var vsData = LoadShaderWithIncludes(vertexPath, includeHandler, loadedFiles);
        var psData = LoadShaderWithIncludes(pixelPath, includeHandler, loadedFiles);

        var vsDesc = new ShaderStageDesc
        {
            Stage = (uint)ShaderStageFlagBits.Vertex,
            Path = StringView.Create(vertexPath),
            Data = ByteArray.Create(vsData),
            EntryPoint = StringView.Create(vsEntry)
        };

        var psDesc = new ShaderStageDesc
        {
            Stage = (uint)ShaderStageFlagBits.Pixel,
            Path = StringView.Create(pixelPath),
            Data = ByteArray.Create(psData),
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
            ShaderStages = stagesArray,
            IncludeHandler = includeHandler
        };

        return new ShaderProgram(programDesc);
    }

    public ShaderProgram CompileCompute(
        string computePath,
        string csEntry = "CSMain",
        Dictionary<string, string?>? defines = null)
    {
        var includeHandler = new ShaderIncludeHandler();
        var loadedFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var csData = LoadShaderWithIncludes(computePath, includeHandler, loadedFiles);

        var csDesc = new ShaderStageDesc
        {
            Stage = (uint)ShaderStageFlagBits.Compute,
            Path = StringView.Create(computePath),
            Data = ByteArray.Create(csData),
            EntryPoint = StringView.Create(csEntry)
        };

        if (defines is { Count: > 0 })
        {
            csDesc.Defines = CreateDefinesArray(defines);
        }

        using var stagesArray = ShaderStageDescArray.Create([csDesc]);
        var programDesc = new ShaderProgramDesc
        {
            ShaderStages = stagesArray,
            IncludeHandler = includeHandler
        };

        return new ShaderProgram(programDesc);
    }

    public ShaderProgram CompileTessellation(
        string vertexPath,
        string hullPath,
        string domainPath,
        string pixelPath,
        string vsEntry = "VSMain",
        string hsEntry = "HSMain",
        string dsEntry = "DSMain",
        string psEntry = "PSMain",
        Dictionary<string, string?>? defines = null)
    {
        var includeHandler = new ShaderIncludeHandler();
        var loadedFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var vsData = LoadShaderWithIncludes(vertexPath, includeHandler, loadedFiles);
        var hsData = LoadShaderWithIncludes(hullPath, includeHandler, loadedFiles);
        var dsData = LoadShaderWithIncludes(domainPath, includeHandler, loadedFiles);
        var psData = LoadShaderWithIncludes(pixelPath, includeHandler, loadedFiles);

        var stages = new List<ShaderStageDesc>
        {
            new()
            {
                Stage = (uint)ShaderStageFlagBits.Vertex,
                Path = StringView.Create(vertexPath),
                Data = ByteArray.Create(vsData),
                EntryPoint = StringView.Create(vsEntry)
            },
            new()
            {
                Stage = (uint)ShaderStageFlagBits.Hull,
                Path = StringView.Create(hullPath),
                Data = ByteArray.Create(hsData),
                EntryPoint = StringView.Create(hsEntry)
            },
            new()
            {
                Stage = (uint)ShaderStageFlagBits.Domain,
                Path = StringView.Create(domainPath),
                Data = ByteArray.Create(dsData),
                EntryPoint = StringView.Create(dsEntry)
            },
            new()
            {
                Stage = (uint)ShaderStageFlagBits.Pixel,
                Path = StringView.Create(pixelPath),
                Data = ByteArray.Create(psData),
                EntryPoint = StringView.Create(psEntry)
            }
        };

        if (defines is { Count: > 0 })
        {
            var definesArray = CreateDefinesArray(defines);
            for (var i = 0; i < stages.Count; i++)
            {
                var stage = stages[i];
                stage.Defines = definesArray;
                stages[i] = stage;
            }
        }

        using var stagesArray = ShaderStageDescArray.Create(stages.ToArray());
        var programDesc = new ShaderProgramDesc
        {
            ShaderStages = stagesArray,
            IncludeHandler = includeHandler
        };

        return new ShaderProgram(programDesc);
    }

    public ShaderProgram CompileGeometry(
        string vertexPath,
        string geometryPath,
        string pixelPath,
        string vsEntry = "VSMain",
        string gsEntry = "GSMain",
        string psEntry = "PSMain",
        Dictionary<string, string?>? defines = null)
    {
        var includeHandler = new ShaderIncludeHandler();
        var loadedFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var vsData = LoadShaderWithIncludes(vertexPath, includeHandler, loadedFiles);
        var gsData = LoadShaderWithIncludes(geometryPath, includeHandler, loadedFiles);
        var psData = LoadShaderWithIncludes(pixelPath, includeHandler, loadedFiles);

        var stages = new List<ShaderStageDesc>
        {
            new()
            {
                Stage = (uint)ShaderStageFlagBits.Vertex,
                Path = StringView.Create(vertexPath),
                Data = ByteArray.Create(vsData),
                EntryPoint = StringView.Create(vsEntry)
            },
            new()
            {
                Stage = (uint)ShaderStageFlagBits.Geometry,
                Path = StringView.Create(geometryPath),
                Data = ByteArray.Create(gsData),
                EntryPoint = StringView.Create(gsEntry)
            },
            new()
            {
                Stage = (uint)ShaderStageFlagBits.Pixel,
                Path = StringView.Create(pixelPath),
                Data = ByteArray.Create(psData),
                EntryPoint = StringView.Create(psEntry)
            }
        };

        if (defines is { Count: > 0 })
        {
            var definesArray = CreateDefinesArray(defines);
            for (var i = 0; i < stages.Count; i++)
            {
                var stage = stages[i];
                stage.Defines = definesArray;
                stages[i] = stage;
            }
        }

        using var stagesArray = ShaderStageDescArray.Create(stages.ToArray());
        var programDesc = new ShaderProgramDesc
        {
            ShaderStages = stagesArray,
            IncludeHandler = includeHandler
        };

        return new ShaderProgram(programDesc);
    }

    public ShaderProgram CompileMesh(
        string meshPath,
        string pixelPath,
        string? taskPath = null,
        string msEntry = "MSMain",
        string psEntry = "PSMain",
        string asEntry = "ASMain",
        Dictionary<string, string?>? defines = null)
    {
        var includeHandler = new ShaderIncludeHandler();
        var loadedFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var stages = new List<ShaderStageDesc>();

        if (!string.IsNullOrEmpty(taskPath))
        {
            var asData = LoadShaderWithIncludes(taskPath, includeHandler, loadedFiles);
            stages.Add(new ShaderStageDesc
            {
                Stage = (uint)ShaderStageFlagBits.Task,
                Path = StringView.Create(taskPath),
                Data = ByteArray.Create(asData),
                EntryPoint = StringView.Create(asEntry)
            });
        }

        var msData = LoadShaderWithIncludes(meshPath, includeHandler, loadedFiles);
        stages.Add(new ShaderStageDesc
        {
            Stage = (uint)ShaderStageFlagBits.Mesh,
            Path = StringView.Create(meshPath),
            Data = ByteArray.Create(msData),
            EntryPoint = StringView.Create(msEntry)
        });

        var psData = LoadShaderWithIncludes(pixelPath, includeHandler, loadedFiles);
        stages.Add(new ShaderStageDesc
        {
            Stage = (uint)ShaderStageFlagBits.Pixel,
            Path = StringView.Create(pixelPath),
            Data = ByteArray.Create(psData),
            EntryPoint = StringView.Create(psEntry)
        });

        if (defines is { Count: > 0 })
        {
            var definesArray = CreateDefinesArray(defines);
            for (var i = 0; i < stages.Count; i++)
            {
                var stage = stages[i];
                stage.Defines = definesArray;
                stages[i] = stage;
            }
        }

        using var stagesArray = ShaderStageDescArray.Create(stages.ToArray());
        var programDesc = new ShaderProgramDesc
        {
            ShaderStages = stagesArray,
            IncludeHandler = includeHandler
        };

        return new ShaderProgram(programDesc);
    }

    public ShaderProgram CompileRayTracing(
        List<(ShaderStageFlagBits stage, string path, string entryPoint, RayTracingShaderDescJson? rayTracingDesc)> shaderStages,
        ShaderRayTracingDescJson rayTracingConfig,
        Dictionary<string, string?>? defines = null)
    {
        var includeHandler = new ShaderIncludeHandler();
        var loadedFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var stages = new List<ShaderStageDesc>();

        foreach (var (stage, path, entryPoint, rtDesc) in shaderStages)
        {
            var stageData = LoadShaderWithIncludes(path, includeHandler, loadedFiles);
            var stageDesc = new ShaderStageDesc
            {
                Stage = (uint)stage,
                Path = StringView.Create(path),
                Data = ByteArray.Create(stageData),
                EntryPoint = StringView.Create(entryPoint)
            };

            if (rtDesc != null)
            {
                stageDesc.RayTracing = ShaderProgramJson.ConvertRayTracingShaderDesc(rtDesc);
            }

            stages.Add(stageDesc);
        }

        if (defines is { Count: > 0 })
        {
            var definesArray = CreateDefinesArray(defines);
            for (var i = 0; i < stages.Count; i++)
            {
                var stage = stages[i];
                stage.Defines = definesArray;
                stages[i] = stage;
            }
        }

        using var stagesArray = ShaderStageDescArray.Create(stages.ToArray());
        var programDesc = new ShaderProgramDesc
        {
            ShaderStages = stagesArray,
            IncludeHandler = includeHandler,
            RayTracing = new ShaderRayTracingDesc
            {
                MaxNumPayloadBytes = rayTracingConfig.MaxPayloadBytes,
                MaxNumAttributeBytes = rayTracingConfig.MaxAttributeBytes,
                MaxRecursionDepth = rayTracingConfig.MaxRecursionDepth
            }
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

    private static byte[] LoadShaderWithIncludes(
        string shaderPath,
        ShaderIncludeHandler includeHandler,
        HashSet<string> loadedFiles)
    {
        var normalizedPath = shaderPath.Replace('\\', '/');

        if (!loadedFiles.Add(normalizedPath))
        {
            return Content.ReadBytes(normalizedPath);
        }

        if (!Content.Exists(normalizedPath))
        {
            throw new FileNotFoundException($"Shader file not found: {normalizedPath}");
        }

        var shaderText = Content.ReadText(normalizedPath);
        var shaderBytes = Encoding.UTF8.GetBytes(shaderText);

        var baseDir = Path.GetDirectoryName(normalizedPath)?.Replace('\\', '/') ?? "";

        foreach (Match match in IncludeRegex().Matches(shaderText))
        {
            var includePath = match.Groups[1].Value;
            var resolvedIncludePath = ResolveIncludePath(includePath, baseDir);

            if (loadedFiles.Contains(resolvedIncludePath))
            {
                continue;
            }

            if (!Content.Exists(resolvedIncludePath))
            {
                continue;
            }

            var includeBytes = LoadShaderWithIncludes(resolvedIncludePath, includeHandler, loadedFiles);
            includeHandler.AddFile(includePath, includeBytes);
        }

        return shaderBytes;
    }

    private static string ResolveIncludePath(string includePath, string baseDir)
    {
        includePath = includePath.Replace('\\', '/');

        if (includePath.StartsWith('/') || Path.IsPathRooted(includePath))
        {
            return includePath.TrimStart('/');
        }

        if (string.IsNullOrEmpty(baseDir))
        {
            return includePath;
        }

        var combined = Path.Combine(baseDir, includePath).Replace('\\', '/');
        var parts = combined.Split('/').ToList();
        var result = new List<string>();

        foreach (var part in parts)
        {
            if (part == "..")
            {
                if (result.Count > 0)
                {
                    result.RemoveAt(result.Count - 1);
                }
            }
            else if (part != ".")
            {
                result.Add(part);
            }
        }

        return string.Join("/", result);
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
