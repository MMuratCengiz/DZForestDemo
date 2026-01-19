using DenOfIz;
using NiziKit.Assets.Serde;
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

    public ShaderProgram CompileFromJson(ShaderProgramJson shaderJson, string basePath)
    {
        var stages = new List<ShaderStageDesc>();
        var globalDefines = shaderJson.GetDefines();

        foreach (var stageJson in shaderJson.Stages)
        {
            var stagePath = Path.IsPathRooted(stageJson.Path)
                ? stageJson.Path
                : Path.Combine(basePath, stageJson.Path);
            stagePath = ResolvePath(stagePath);

            var stageDesc = new ShaderStageDesc
            {
                Stage = (uint)stageJson.Stage,
                Path = StringView.Create(stagePath),
                EntryPoint = StringView.Create(stageJson.EntryPoint)
            };

            var mergedDefines = shaderJson.GetDefinesForStage(stageJson);
            if (mergedDefines is { Count: > 0 })
            {
                stageDesc.Defines = CreateDefinesArray(mergedDefines);
            }

            if (stageJson.RayTracing != null)
            {
                stageDesc.RayTracing = ShaderProgramJson.ConvertRayTracingShaderDesc(stageJson.RayTracing);
            }

            if (stageJson.Bindless != null)
            {
                stageDesc.Bindless = ShaderProgramJson.ConvertBindlessDesc(stageJson.Bindless);
            }

            stages.Add(stageDesc);
        }

        using var stagesArray = ShaderStageDescArray.Create(stages.ToArray());
        var programDesc = new ShaderProgramDesc
        {
            ShaderStages = stagesArray
        };

        var pipelineType = shaderJson.DetectPipelineType();
        if (pipelineType == PipelineType.RayTracing)
        {
            programDesc.RayTracing = shaderJson.ToShaderRayTracingDesc();
        }

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
        var vsFullPath = ResolvePath(vertexPath);
        var hsFullPath = ResolvePath(hullPath);
        var dsFullPath = ResolvePath(domainPath);
        var psFullPath = ResolvePath(pixelPath);

        var stages = new List<ShaderStageDesc>
        {
            new()
            {
                Stage = (uint)ShaderStageFlagBits.Vertex,
                Path = StringView.Create(vsFullPath),
                EntryPoint = StringView.Create(vsEntry)
            },
            new()
            {
                Stage = (uint)ShaderStageFlagBits.Hull,
                Path = StringView.Create(hsFullPath),
                EntryPoint = StringView.Create(hsEntry)
            },
            new()
            {
                Stage = (uint)ShaderStageFlagBits.Domain,
                Path = StringView.Create(dsFullPath),
                EntryPoint = StringView.Create(dsEntry)
            },
            new()
            {
                Stage = (uint)ShaderStageFlagBits.Pixel,
                Path = StringView.Create(psFullPath),
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
            ShaderStages = stagesArray
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
        var vsFullPath = ResolvePath(vertexPath);
        var gsFullPath = ResolvePath(geometryPath);
        var psFullPath = ResolvePath(pixelPath);

        var stages = new List<ShaderStageDesc>
        {
            new()
            {
                Stage = (uint)ShaderStageFlagBits.Vertex,
                Path = StringView.Create(vsFullPath),
                EntryPoint = StringView.Create(vsEntry)
            },
            new()
            {
                Stage = (uint)ShaderStageFlagBits.Geometry,
                Path = StringView.Create(gsFullPath),
                EntryPoint = StringView.Create(gsEntry)
            },
            new()
            {
                Stage = (uint)ShaderStageFlagBits.Pixel,
                Path = StringView.Create(psFullPath),
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
            ShaderStages = stagesArray
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
        var msFullPath = ResolvePath(meshPath);
        var psFullPath = ResolvePath(pixelPath);

        var stages = new List<ShaderStageDesc>();

        if (!string.IsNullOrEmpty(taskPath))
        {
            var asFullPath = ResolvePath(taskPath);
            stages.Add(new ShaderStageDesc
            {
                Stage = (uint)ShaderStageFlagBits.Task,
                Path = StringView.Create(asFullPath),
                EntryPoint = StringView.Create(asEntry)
            });
        }

        stages.Add(new ShaderStageDesc
        {
            Stage = (uint)ShaderStageFlagBits.Mesh,
            Path = StringView.Create(msFullPath),
            EntryPoint = StringView.Create(msEntry)
        });

        stages.Add(new ShaderStageDesc
        {
            Stage = (uint)ShaderStageFlagBits.Pixel,
            Path = StringView.Create(psFullPath),
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
            ShaderStages = stagesArray
        };

        return new ShaderProgram(programDesc);
    }

    public ShaderProgram CompileRayTracing(
        List<(ShaderStageFlagBits stage, string path, string entryPoint, RayTracingShaderDescJson? rayTracingDesc)> shaderStages,
        ShaderRayTracingDescJson rayTracingConfig,
        Dictionary<string, string?>? defines = null)
    {
        var stages = new List<ShaderStageDesc>();

        foreach (var (stage, path, entryPoint, rtDesc) in shaderStages)
        {
            var fullPath = ResolvePath(path);
            var stageDesc = new ShaderStageDesc
            {
                Stage = (uint)stage,
                Path = StringView.Create(fullPath),
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

    public Task<ShaderProgram> CompileFromJsonAsync(
        ShaderProgramJson shaderJson,
        string basePath,
        CancellationToken ct = default)
    {
        return Task.Run(() => CompileFromJson(shaderJson, basePath), ct);
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
