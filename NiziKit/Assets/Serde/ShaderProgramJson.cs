using System.Text.Json;
using System.Text.Json.Serialization;
using DenOfIz;

namespace NiziKit.Assets.Serde;

public enum ShaderStageType
{
    Vertex,
    Pixel,
    Compute
}

public enum PrimitiveTopologyType
{
    Triangle,
    Line,
    Point
}

public enum CullModeType
{
    None,
    FrontFace,
    BackFace
}

public enum FillModeType
{
    Solid,
    Wireframe
}

public enum CompareOpType
{
    Never,
    Less,
    Equal,
    LessOrEqual,
    Greater,
    NotEqual,
    GreaterOrEqual,
    Always
}

public sealed class ShaderStageJson
{
    [JsonPropertyName("stage")]
    public ShaderStageType Stage { get; set; }

    [JsonPropertyName("path")]
    public string Path { get; set; } = string.Empty;

    [JsonPropertyName("entryPoint")]
    public string EntryPoint { get; set; } = "main";
}

public sealed class DepthTestJson
{
    [JsonPropertyName("enable")]
    public bool Enable { get; set; } = true;

    [JsonPropertyName("compareOp")]
    public CompareOpType CompareOp { get; set; } = CompareOpType.Less;

    [JsonPropertyName("write")]
    public bool Write { get; set; } = true;
}

public sealed class StencilTestJson
{
    [JsonPropertyName("enable")]
    public bool Enable { get; set; }
}

public sealed class BlendJson
{
    [JsonPropertyName("enable")]
    public bool Enable { get; set; }

    [JsonPropertyName("renderTargetWriteMask")]
    public int RenderTargetWriteMask { get; set; } = 15;
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
    public PrimitiveTopologyType PrimitiveTopology { get; set; } = PrimitiveTopologyType.Triangle;

    [JsonPropertyName("cullMode")]
    public CullModeType CullMode { get; set; } = CullModeType.BackFace;

    [JsonPropertyName("fillMode")]
    public FillModeType FillMode { get; set; } = FillModeType.Solid;

    [JsonPropertyName("depthTest")]
    public DepthTestJson DepthTest { get; set; } = new();

    [JsonPropertyName("stencilTest")]
    public StencilTestJson? StencilTest { get; set; }

    [JsonPropertyName("blend")]
    public BlendJson Blend { get; set; } = new();

    [JsonPropertyName("rasterization")]
    public RasterizationJson? Rasterization { get; set; }
}

public sealed class ShaderProgramJson
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("stages")]
    public List<ShaderStageJson> Stages { get; set; } = [];

    [JsonPropertyName("defines")]
    public Dictionary<string, string>? Defines { get; set; }

    [JsonPropertyName("pipeline")]
    public GraphicsPipelineJson Pipeline { get; set; } = new();

    public static ShaderProgramJson FromJson(string json)
        => JsonSerializer.Deserialize<ShaderProgramJson>(json, AssetJsonDesc.Default)
           ?? throw new InvalidOperationException("Failed to deserialize shader program JSON");

    public static async Task<ShaderProgramJson> FromJsonAsync(Stream stream, CancellationToken ct = default)
        => await JsonSerializer.DeserializeAsync<ShaderProgramJson>(stream, AssetJsonDesc.Default, ct)
           ?? throw new InvalidOperationException("Failed to deserialize shader program JSON");

    public string ToJson() => JsonSerializer.Serialize(this, AssetJsonDesc.Default);

    public Dictionary<string, string?>? GetDefines()
    {
        if (Defines == null || Defines.Count == 0)
        {
            return null;
        }

        return Defines.ToDictionary(kv => kv.Key, kv => (string?)kv.Value);
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
                case ShaderStageType.Vertex:
                    vertexPath = stage.Path;
                    break;
                case ShaderStageType.Pixel:
                    pixelPath = stage.Path;
                    break;
                case ShaderStageType.Compute:
                    computePath = stage.Path;
                    break;
            }
        }

        return (vertexPath, pixelPath, computePath);
    }

    public GraphicsPipelineDesc ToGraphicsPipelineDesc(Format backBufferFormat, Format depthBufferFormat)
    {
        var blendDesc = new BlendDesc
        {
            Enable = Pipeline.Blend.Enable,
            RenderTargetWriteMask = (byte)Pipeline.Blend.RenderTargetWriteMask
        };

        var renderTarget = new RenderTargetDesc
        {
            Format = backBufferFormat,
            Blend = blendDesc
        };

        using var renderTargets = RenderTargetDescArray.Create([renderTarget]);

        return new GraphicsPipelineDesc
        {
            PrimitiveTopology = ConvertPrimitiveTopology(Pipeline.PrimitiveTopology),
            CullMode = ConvertCullMode(Pipeline.CullMode),
            FillMode = ConvertFillMode(Pipeline.FillMode),
            DepthTest = new DepthTest
            {
                Enable = Pipeline.DepthTest.Enable,
                CompareOp = ConvertCompareOp(Pipeline.DepthTest.CompareOp),
                Write = Pipeline.DepthTest.Write
            },
            DepthStencilAttachmentFormat = depthBufferFormat,
            RenderTargets = renderTargets
        };
    }

    private static PrimitiveTopology ConvertPrimitiveTopology(PrimitiveTopologyType type) => type switch
    {
        PrimitiveTopologyType.Triangle => PrimitiveTopology.Triangle,
        PrimitiveTopologyType.Line => PrimitiveTopology.Line,
        PrimitiveTopologyType.Point => PrimitiveTopology.Point,
        _ => PrimitiveTopology.Triangle
    };

    private static CullMode ConvertCullMode(CullModeType type) => type switch
    {
        CullModeType.None => CullMode.None,
        CullModeType.FrontFace => CullMode.FrontFace,
        CullModeType.BackFace => CullMode.BackFace,
        _ => CullMode.BackFace
    };

    private static FillMode ConvertFillMode(FillModeType type) => type switch
    {
        FillModeType.Solid => FillMode.Solid,
        FillModeType.Wireframe => FillMode.Wireframe,
        _ => FillMode.Solid
    };

    private static CompareOp ConvertCompareOp(CompareOpType type) => type switch
    {
        CompareOpType.Never => CompareOp.Never,
        CompareOpType.Less => CompareOp.Less,
        CompareOpType.Equal => CompareOp.Equal,
        CompareOpType.LessOrEqual => CompareOp.LessOrEqual,
        CompareOpType.Greater => CompareOp.Greater,
        CompareOpType.NotEqual => CompareOp.NotEqual,
        CompareOpType.GreaterOrEqual => CompareOp.GreaterOrEqual,
        CompareOpType.Always => CompareOp.Always,
        _ => CompareOp.Less
    };
}
