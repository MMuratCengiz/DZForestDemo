using System.Text.Json;
using System.Text.Json.Serialization;
using DenOfIz;

namespace NiziKit.Assets.Serde;

public enum PipelineType
{
    Graphics,
    Compute,
    RayTracing,
    Mesh
}

#region Bindless Configuration

public sealed class BindlessSlotJson
{
    [JsonPropertyName("descriptor")]
    public ResourceDescriptorFlagBits Descriptor { get; set; }

    [JsonPropertyName("binding")]
    public uint Binding { get; set; }

    [JsonPropertyName("registerSpace")]
    public uint RegisterSpace { get; set; }

    [JsonPropertyName("maxArraySize")]
    public uint MaxArraySize { get; set; }
}

public sealed class BindlessDescJson
{
    [JsonPropertyName("bindlessArrays")]
    public List<BindlessSlotJson> BindlessArrays { get; set; } = [];
}

#endregion

#region Ray Tracing Configuration

public sealed class ResourceBindingSlotJson
{
    [JsonPropertyName("type")]
    public ResourceBindingType Type { get; set; }

    [JsonPropertyName("binding")]
    public uint Binding { get; set; }

    [JsonPropertyName("registerSpace")]
    public uint RegisterSpace { get; set; }
}

public sealed class RayTracingShaderDescJson
{
    [JsonPropertyName("hitGroupType")]
    public HitGroupType HitGroupType { get; set; } = HitGroupType.Triangles;

    [JsonPropertyName("localBindings")]
    public List<ResourceBindingSlotJson>? LocalBindings { get; set; }
}

public sealed class ShaderRayTracingDescJson
{
    [JsonPropertyName("maxPayloadBytes")]
    public uint MaxPayloadBytes { get; set; } = 32;

    [JsonPropertyName("maxAttributeBytes")]
    public uint MaxAttributeBytes { get; set; } = 8;

    [JsonPropertyName("maxRecursionDepth")]
    public uint MaxRecursionDepth { get; set; } = 1;
}

public sealed class HitGroupDescJson
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("intersectionShaderIndex")]
    public int IntersectionShaderIndex { get; set; } = -1;

    [JsonPropertyName("anyHitShaderIndex")]
    public int AnyHitShaderIndex { get; set; } = -1;

    [JsonPropertyName("closestHitShaderIndex")]
    public int ClosestHitShaderIndex { get; set; } = -1;

    [JsonPropertyName("localRootSignatureIndex")]
    public int LocalRootSignatureIndex { get; set; } = -1;

    [JsonPropertyName("type")]
    public HitGroupType Type { get; set; } = HitGroupType.Triangles;
}

#endregion

#region Shader Stage Configuration

public sealed class ThreadGroupDescJson
{
    [JsonPropertyName("x")]
    public uint X { get; set; } = 1;

    [JsonPropertyName("y")]
    public uint Y { get; set; } = 1;

    [JsonPropertyName("z")]
    public uint Z { get; set; } = 1;
}

public sealed class ShaderStageJson
{
    [JsonPropertyName("stage")]
    public ShaderStageFlagBits Stage { get; set; }

    [JsonPropertyName("path")]
    public string Path { get; set; } = string.Empty;

    [JsonPropertyName("entryPoint")]
    public string EntryPoint { get; set; } = "main";

    [JsonPropertyName("defines")]
    public Dictionary<string, string>? Defines { get; set; }

    [JsonPropertyName("rayTracing")]
    public RayTracingShaderDescJson? RayTracing { get; set; }

    [JsonPropertyName("bindless")]
    public BindlessDescJson? Bindless { get; set; }

    [JsonPropertyName("threadGroup")]
    public ThreadGroupDescJson? ThreadGroup { get; set; }
}

#endregion

#region Pipeline State Configuration

public sealed class DepthTestJson
{
    [JsonPropertyName("enable")]
    public bool Enable { get; set; } = true;

    [JsonPropertyName("compareOp")]
    public CompareOp CompareOp { get; set; } = CompareOp.Less;

    [JsonPropertyName("write")]
    public bool Write { get; set; } = true;
}

public sealed class StencilFaceJson
{
    [JsonPropertyName("compareOp")]
    public CompareOp CompareOp { get; set; } = CompareOp.Always;

    [JsonPropertyName("failOp")]
    public StencilOp FailOp { get; set; } = StencilOp.Keep;

    [JsonPropertyName("passOp")]
    public StencilOp PassOp { get; set; } = StencilOp.Keep;

    [JsonPropertyName("depthFailOp")]
    public StencilOp DepthFailOp { get; set; } = StencilOp.Keep;
}

public sealed class StencilTestJson
{
    [JsonPropertyName("enable")]
    public bool Enable { get; set; }

    [JsonPropertyName("writeMask")]
    public uint WriteMask { get; set; } = 0xFF;

    [JsonPropertyName("readMask")]
    public uint ReadMask { get; set; } = 0xFF;

    [JsonPropertyName("frontFace")]
    public StencilFaceJson? FrontFace { get; set; }

    [JsonPropertyName("backFace")]
    public StencilFaceJson? BackFace { get; set; }
}

public sealed class BlendJson
{
    [JsonPropertyName("enable")]
    public bool Enable { get; set; }

    [JsonPropertyName("srcBlend")]
    public Blend SrcBlend { get; set; } = Blend.SrcAlpha;

    [JsonPropertyName("dstBlend")]
    public Blend DstBlend { get; set; } = Blend.InvSrcAlpha;

    [JsonPropertyName("blendOp")]
    public BlendOp BlendOp { get; set; } = BlendOp.Add;

    [JsonPropertyName("srcBlendAlpha")]
    public Blend SrcBlendAlpha { get; set; } = Blend.One;

    [JsonPropertyName("dstBlendAlpha")]
    public Blend DstBlendAlpha { get; set; } = Blend.InvSrcAlpha;

    [JsonPropertyName("blendOpAlpha")]
    public BlendOp BlendOpAlpha { get; set; } = BlendOp.Add;

    [JsonPropertyName("renderTargetWriteMask")]
    public int RenderTargetWriteMask { get; set; } = 15;
}

public sealed class RenderTargetJson
{
    [JsonPropertyName("format")]
    public Format? Format { get; set; }

    [JsonPropertyName("blend")]
    public BlendJson Blend { get; set; } = new();
}

public sealed class RasterizationJson
{
    [JsonPropertyName("depthBias")]
    public int DepthBias { get; set; }

    [JsonPropertyName("depthBiasClamp")]
    public float DepthBiasClamp { get; set; }

    [JsonPropertyName("slopeScaledDepthBias")]
    public float SlopeScaledDepthBias { get; set; }

    [JsonPropertyName("frontCounterClockwise")]
    public bool FrontCounterClockwise { get; set; }
}

public sealed class GraphicsPipelineJson
{
    [JsonPropertyName("primitiveTopology")]
    public PrimitiveTopology PrimitiveTopology { get; set; } = PrimitiveTopology.Triangle;

    [JsonPropertyName("cullMode")]
    public CullMode CullMode { get; set; } = CullMode.BackFace;

    [JsonPropertyName("fillMode")]
    public FillMode FillMode { get; set; } = FillMode.Solid;

    [JsonPropertyName("depthTest")]
    public DepthTestJson DepthTest { get; set; } = new();

    [JsonPropertyName("stencilTest")]
    public StencilTestJson? StencilTest { get; set; }

    [JsonPropertyName("blend")]
    public BlendJson Blend { get; set; } = new();

    [JsonPropertyName("renderTargets")]
    public List<RenderTargetJson>? RenderTargets { get; set; }

    [JsonPropertyName("rasterization")]
    public RasterizationJson? Rasterization { get; set; }

    [JsonPropertyName("alphaToCoverageEnable")]
    public bool AlphaToCoverageEnable { get; set; }

    [JsonPropertyName("independentBlendEnable")]
    public bool IndependentBlendEnable { get; set; }

    [JsonPropertyName("blendLogicOpEnable")]
    public bool BlendLogicOpEnable { get; set; }

    [JsonPropertyName("blendLogicOp")]
    public LogicOp BlendLogicOp { get; set; } = LogicOp.NoOp;

    [JsonPropertyName("msaaSampleCount")]
    public MSAASampleCount MSAASampleCount { get; set; } = MSAASampleCount._0;

    [JsonPropertyName("depthStencilFormat")]
    public Format? DepthStencilFormat { get; set; }
}

public sealed class ComputePipelineJson
{
    [JsonPropertyName("threadGroup")]
    public ThreadGroupDescJson? ThreadGroup { get; set; }
}

public sealed class RayTracingPipelineJson
{
    [JsonPropertyName("hitGroups")]
    public List<HitGroupDescJson> HitGroups { get; set; } = [];

    [JsonPropertyName("maxPayloadBytes")]
    public uint MaxPayloadBytes { get; set; } = 32;

    [JsonPropertyName("maxAttributeBytes")]
    public uint MaxAttributeBytes { get; set; } = 8;

    [JsonPropertyName("maxRecursionDepth")]
    public uint MaxRecursionDepth { get; set; } = 1;
}

public sealed class MeshPipelineJson
{
    [JsonPropertyName("primitiveTopology")]
    public PrimitiveTopology PrimitiveTopology { get; set; } = PrimitiveTopology.Triangle;

    [JsonPropertyName("cullMode")]
    public CullMode CullMode { get; set; } = CullMode.BackFace;

    [JsonPropertyName("fillMode")]
    public FillMode FillMode { get; set; } = FillMode.Solid;

    [JsonPropertyName("depthTest")]
    public DepthTestJson DepthTest { get; set; } = new();

    [JsonPropertyName("stencilTest")]
    public StencilTestJson? StencilTest { get; set; }

    [JsonPropertyName("blend")]
    public BlendJson Blend { get; set; } = new();

    [JsonPropertyName("renderTargets")]
    public List<RenderTargetJson>? RenderTargets { get; set; }

    [JsonPropertyName("rasterization")]
    public RasterizationJson? Rasterization { get; set; }

    [JsonPropertyName("msaaSampleCount")]
    public MSAASampleCount MSAASampleCount { get; set; } = MSAASampleCount._0;

    [JsonPropertyName("depthStencilFormat")]
    public Format? DepthStencilFormat { get; set; }
}

#endregion

#region Main Shader Program

public sealed class ShaderProgramJson
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public PipelineType? Type { get; set; }

    [JsonPropertyName("stages")]
    public List<ShaderStageJson> Stages { get; set; } = [];

    [JsonPropertyName("defines")]
    public Dictionary<string, string>? Defines { get; set; }

    [JsonPropertyName("rayTracing")]
    public ShaderRayTracingDescJson? RayTracing { get; set; }

    [JsonPropertyName("pipeline")]
    public GraphicsPipelineJson? Pipeline { get; set; }

    [JsonPropertyName("computePipeline")]
    public ComputePipelineJson? ComputePipeline { get; set; }

    [JsonPropertyName("rayTracingPipeline")]
    public RayTracingPipelineJson? RayTracingPipeline { get; set; }

    [JsonPropertyName("meshPipeline")]
    public MeshPipelineJson? MeshPipeline { get; set; }

    public static ShaderProgramJson FromJson(string json)
        => JsonSerializer.Deserialize<ShaderProgramJson>(json, NiziJsonSerializationOptions.Default)
           ?? throw new InvalidOperationException("Failed to deserialize shader program JSON");

    public static async Task<ShaderProgramJson> FromJsonAsync(Stream stream, CancellationToken ct = default)
        => await JsonSerializer.DeserializeAsync<ShaderProgramJson>(stream, NiziJsonSerializationOptions.Default, ct)
           ?? throw new InvalidOperationException("Failed to deserialize shader program JSON");

    public string ToJson() => JsonSerializer.Serialize(this, NiziJsonSerializationOptions.Default);

    public PipelineType DetectPipelineType()
    {
        if (Type.HasValue)
        {
            return Type.Value;
        }

        foreach (var stage in Stages)
        {
            switch (stage.Stage)
            {
                case ShaderStageFlagBits.RayGen:
                case ShaderStageFlagBits.AnyHit:
                case ShaderStageFlagBits.ClosestHit:
                case ShaderStageFlagBits.Miss:
                case ShaderStageFlagBits.Intersection:
                case ShaderStageFlagBits.Callable:
                    return PipelineType.RayTracing;

                case ShaderStageFlagBits.Mesh:
                case ShaderStageFlagBits.Task:
                    return PipelineType.Mesh;

                case ShaderStageFlagBits.Compute:
                    return PipelineType.Compute;
            }
        }

        return PipelineType.Graphics;
    }

    public Dictionary<string, string?>? GetDefines()
    {
        if (Defines == null || Defines.Count == 0)
        {
            return null;
        }

        return Defines.ToDictionary(kv => kv.Key, kv => (string?)kv.Value);
    }

    public Dictionary<string, string?>? GetDefinesForStage(ShaderStageJson stage)
    {
        var globalDefines = GetDefines();
        if (stage.Defines == null || stage.Defines.Count == 0)
        {
            return globalDefines;
        }

        var merged = globalDefines ?? new Dictionary<string, string?>();
        foreach (var (key, value) in stage.Defines)
        {
            merged[key] = value;
        }

        return merged.Count > 0 ? merged : null;
    }

    public (string? vertexPath, string? pixelPath, string? computePath) GetStagePaths()
    {
        string? vertexPath = null;
        string? pixelPath = null;
        string? computePath = null;

        foreach (var stage in Stages)
        {
            switch (stage.Stage)
            {
                case ShaderStageFlagBits.Vertex:
                    vertexPath = stage.Path;
                    break;
                case ShaderStageFlagBits.Pixel:
                    pixelPath = stage.Path;
                    break;
                case ShaderStageFlagBits.Compute:
                    computePath = stage.Path;
                    break;
            }
        }

        return (vertexPath, pixelPath, computePath);
    }

    public IEnumerable<ShaderStageJson> GetStagesOfType(ShaderStageFlagBits type)
        => Stages.Where(s => s.Stage == type);

    public (string? path, string entryPoint) GetStageInfo(ShaderStageFlagBits type)
    {
        var stage = Stages.FirstOrDefault(s => s.Stage == type);
        return stage != null ? (stage.Path, stage.EntryPoint) : (null, "main");
    }

    #region Conversion to Native Descriptors

    public GraphicsPipelineDesc ToGraphicsPipelineDesc(Format backBufferFormat, Format depthBufferFormat)
    {
        var pipeline = Pipeline ?? new GraphicsPipelineJson();

        var blendDesc = ConvertBlendDesc(pipeline.Blend);
        var renderTarget = new RenderTargetDesc
        {
            Format = backBufferFormat,
            Blend = blendDesc
        };

        using var renderTargets = pipeline.RenderTargets != null && pipeline.RenderTargets.Count > 0
            ? RenderTargetDescArray.Create(pipeline.RenderTargets.Select(rt => new RenderTargetDesc
            {
                Format = rt.Format ?? backBufferFormat,
                Blend = ConvertBlendDesc(rt.Blend)
            }).ToArray())
            : RenderTargetDescArray.Create([renderTarget]);

        return new GraphicsPipelineDesc
        {
            PrimitiveTopology = pipeline.PrimitiveTopology,
            CullMode = pipeline.CullMode,
            FillMode = pipeline.FillMode,
            DepthTest = ConvertDepthTest(pipeline.DepthTest),
            StencilTest = ConvertStencilTest(pipeline.StencilTest),
            DepthStencilAttachmentFormat = pipeline.DepthStencilFormat ?? depthBufferFormat,
            RenderTargets = renderTargets,
            Rasterization = ConvertRasterization(pipeline.Rasterization),
            AlphaToCoverageEnable = pipeline.AlphaToCoverageEnable,
            IndependentBlendEnable = pipeline.IndependentBlendEnable,
            BlendLogicOpEnable = pipeline.BlendLogicOpEnable,
            BlendLogicOp = pipeline.BlendLogicOp,
            MSAASampleCount = pipeline.MSAASampleCount
        };
    }

    public GraphicsPipelineDesc ToMeshPipelineDesc(Format backBufferFormat, Format depthBufferFormat)
    {
        var pipeline = MeshPipeline ?? new MeshPipelineJson();

        var blendDesc = ConvertBlendDesc(pipeline.Blend);
        var renderTarget = new RenderTargetDesc
        {
            Format = backBufferFormat,
            Blend = blendDesc
        };

        using var renderTargets = pipeline.RenderTargets != null && pipeline.RenderTargets.Count > 0
            ? RenderTargetDescArray.Create(pipeline.RenderTargets.Select(rt => new RenderTargetDesc
            {
                Format = rt.Format ?? backBufferFormat,
                Blend = ConvertBlendDesc(rt.Blend)
            }).ToArray())
            : RenderTargetDescArray.Create([renderTarget]);

        return new GraphicsPipelineDesc
        {
            PrimitiveTopology = pipeline.PrimitiveTopology,
            CullMode = pipeline.CullMode,
            FillMode = pipeline.FillMode,
            DepthTest = ConvertDepthTest(pipeline.DepthTest),
            StencilTest = ConvertStencilTest(pipeline.StencilTest),
            DepthStencilAttachmentFormat = pipeline.DepthStencilFormat ?? depthBufferFormat,
            RenderTargets = renderTargets,
            Rasterization = ConvertRasterization(pipeline.Rasterization),
            MSAASampleCount = pipeline.MSAASampleCount
        };
    }

    public ShaderRayTracingDesc ToShaderRayTracingDesc()
    {
        if (RayTracing != null)
        {
            return new ShaderRayTracingDesc
            {
                MaxNumPayloadBytes = RayTracing.MaxPayloadBytes,
                MaxNumAttributeBytes = RayTracing.MaxAttributeBytes,
                MaxRecursionDepth = RayTracing.MaxRecursionDepth
            };
        }

        if (RayTracingPipeline != null)
        {
            return new ShaderRayTracingDesc
            {
                MaxNumPayloadBytes = RayTracingPipeline.MaxPayloadBytes,
                MaxNumAttributeBytes = RayTracingPipeline.MaxAttributeBytes,
                MaxRecursionDepth = RayTracingPipeline.MaxRecursionDepth
            };
        }

        return new ShaderRayTracingDesc
        {
            MaxNumPayloadBytes = 32,
            MaxNumAttributeBytes = 8,
            MaxRecursionDepth = 1
        };
    }

    public HitGroupDescArray ToHitGroupDescArray()
    {
        if (RayTracingPipeline == null || RayTracingPipeline.HitGroups.Count == 0)
        {
            return HitGroupDescArray.Create([]);
        }

        var hitGroups = RayTracingPipeline.HitGroups.Select(hg => new HitGroupDesc
        {
            Name = StringView.Create(hg.Name),
            IntersectionShaderIndex = hg.IntersectionShaderIndex,
            AnyHitShaderIndex = hg.AnyHitShaderIndex,
            ClosestHitShaderIndex = hg.ClosestHitShaderIndex,
            Type = hg.Type
        }).ToArray();

        return HitGroupDescArray.Create(hitGroups);
    }

    #endregion

    #region Static Conversion Helpers

    public static string GetDefaultEntryPoint(ShaderStageFlagBits type) => type switch
    {
        ShaderStageFlagBits.Vertex => "VSMain",
        ShaderStageFlagBits.Pixel => "PSMain",
        ShaderStageFlagBits.Geometry => "GSMain",
        ShaderStageFlagBits.Hull => "HSMain",
        ShaderStageFlagBits.Domain => "DSMain",
        ShaderStageFlagBits.Compute => "CSMain",
        ShaderStageFlagBits.RayGen => "RayGenMain",
        ShaderStageFlagBits.AnyHit => "AnyHitMain",
        ShaderStageFlagBits.ClosestHit => "ClosestHitMain",
        ShaderStageFlagBits.Miss => "MissMain",
        ShaderStageFlagBits.Intersection => "IntersectionMain",
        ShaderStageFlagBits.Callable => "CallableMain",
        ShaderStageFlagBits.Task => "TaskMain",
        ShaderStageFlagBits.Mesh => "MeshMain",
        _ => "main"
    };

    private static DepthTest ConvertDepthTest(DepthTestJson? json)
    {
        if (json == null)
        {
            return new DepthTest { Enable = true, CompareOp = CompareOp.Less, Write = true };
        }

        return new DepthTest
        {
            Enable = json.Enable,
            CompareOp = json.CompareOp,
            Write = json.Write
        };
    }

    private static StencilTest ConvertStencilTest(StencilTestJson? json)
    {
        if (json == null)
        {
            return new StencilTest { Enable = false };
        }

        return new StencilTest
        {
            Enable = json.Enable,
            WriteMask = json.WriteMask,
            ReadMask = json.ReadMask,
            FrontFace = ConvertStencilFace(json.FrontFace),
            BackFace = ConvertStencilFace(json.BackFace)
        };
    }

    private static StencilFace ConvertStencilFace(StencilFaceJson? json)
    {
        if (json == null)
        {
            return new StencilFace
            {
                CompareOp = CompareOp.Always,
                FailOp = StencilOp.Keep,
                PassOp = StencilOp.Keep,
                DepthFailOp = StencilOp.Keep
            };
        }

        return new StencilFace
        {
            CompareOp = json.CompareOp,
            FailOp = json.FailOp,
            PassOp = json.PassOp,
            DepthFailOp = json.DepthFailOp
        };
    }

    private static BlendDesc ConvertBlendDesc(BlendJson? json)
    {
        if (json == null)
        {
            return new BlendDesc { Enable = false, RenderTargetWriteMask = 15 };
        }

        return new BlendDesc
        {
            Enable = json.Enable,
            SrcBlend = json.SrcBlend,
            DstBlend = json.DstBlend,
            BlendOp = json.BlendOp,
            SrcBlendAlpha = json.SrcBlendAlpha,
            DstBlendAlpha = json.DstBlendAlpha,
            BlendOpAlpha = json.BlendOpAlpha,
            RenderTargetWriteMask = (byte)json.RenderTargetWriteMask
        };
    }

    private static RasterizationDesc ConvertRasterization(RasterizationJson? json)
    {
        if (json == null)
        {
            return new RasterizationDesc();
        }

        return new RasterizationDesc
        {
            DepthBias = json.DepthBias,
            DepthBiasClamp = json.DepthBiasClamp,
            SlopeScaledDepthBias = json.SlopeScaledDepthBias,
            FrontCounterClockwise = json.FrontCounterClockwise
        };
    }

    public static BindlessDesc ConvertBindlessDesc(BindlessDescJson? json)
    {
        if (json == null || json.BindlessArrays.Count == 0)
        {
            return new BindlessDesc();
        }

        var slots = json.BindlessArrays.Select(slot => new BindlessSlot
        {
            Descriptor = (uint)slot.Descriptor,
            Binding = slot.Binding,
            RegisterSpace = slot.RegisterSpace,
            MaxArraySize = slot.MaxArraySize
        }).ToArray();

        return new BindlessDesc
        {
            BindlessArrays = BindlessSlotArray.Create(slots)
        };
    }

    public static RayTracingShaderDesc ConvertRayTracingShaderDesc(RayTracingShaderDescJson? json)
    {
        if (json == null)
        {
            return new RayTracingShaderDesc { HitGroupType = HitGroupType.Triangles };
        }

        var desc = new RayTracingShaderDesc
        {
            HitGroupType = json.HitGroupType
        };

        if (json.LocalBindings != null && json.LocalBindings.Count > 0)
        {
            var bindings = json.LocalBindings.Select(b => new ResourceBindingSlot
            {
                Type = b.Type,
                Binding = b.Binding,
                RegisterSpace = b.RegisterSpace
            }).ToArray();

            desc.LocalBindings = ResourceBindingSlotArray.Create(bindings);
        }

        return desc;
    }

    #endregion
}

#endregion
