using DenOfIz;

namespace NiziKit.Offline;

public enum ExportFormat
{
    Glb,
    Gltf
}

public enum Handedness
{
    Right,
    Left
}

public sealed class AssetExportDesc
{
    public string SourcePath { get; set; } = string.Empty;
    public string OutputDirectory { get; set; } = string.Empty;
    public string AssetName { get; set; } = string.Empty;
    public ExportFormat Format { get; set; } = ExportFormat.Glb;
    public float Scale { get; set; } = 1.0f;
    public bool EmbedTextures { get; set; } = true;
    public bool OverwriteExisting { get; set; } = true;
    public bool OptimizeMeshes { get; set; } = true;
    public bool GenerateNormals { get; set; } = true;
    public bool CalculateTangents { get; set; } = true;
    public bool TriangulateMeshes { get; set; } = true;
    public bool JoinIdenticalVertices { get; set; } = true;
    public bool LimitBoneWeights { get; set; } = true;
    public uint MaxBoneWeightsPerVertex { get; set; } = 4;
    public bool SmoothNormals { get; set; } = true;
    public float SmoothNormalsAngle { get; set; } = 80.0f;
    public bool ExportSkeleton { get; set; } = true;
    public bool ExportAnimations { get; set; } = true;
    public OzzSkeleton? ExternalSkeleton { get; set; }

    internal GltfExportDesc ToGltfExportDesc()
    {
        return new GltfExportDesc
        {
            SourceFilePath = SourcePath,
            TargetDirectory = OutputDirectory,
            AssetNamePrefix = AssetName,
            OutputFormat = Format == ExportFormat.Glb ? GltfExportFormat.Glb : GltfExportFormat.Gltf,
            ScaleFactor = Scale,
            OptimizeMeshes = OptimizeMeshes,
            GenerateNormals = GenerateNormals,
            CalculateTangentSpace = CalculateTangents,
            TriangulateMeshes = TriangulateMeshes,
            JoinIdenticalVertices = JoinIdenticalVertices,
            LimitBoneWeights = LimitBoneWeights,
            MaxBoneWeightsPerVertex = MaxBoneWeightsPerVertex,
            SmoothNormals = SmoothNormals,
            SmoothNormalsAngle = SmoothNormalsAngle,
            PreTransformVertices = false,
            RemoveRedundantMaterials = true,
            MergeMeshes = false,
            OptimizeGraph = false,
            DropNormals = false,
            FixInfacingNormals = false
        };
    }

    internal OzzExportDesc ToOzzExportDesc(byte[] gltfData, string basePath)
    {
        return new OzzExportDesc
        {
            GltfSourceData = ByteArrayView.Create(gltfData),
            GltfSourceBasePath = StringView.Create(basePath),
            OutputDirectory = StringView.Create(OutputDirectory),
            AssetNamePrefix = StringView.Create(AssetName),
            ExportSkeleton = ExportSkeleton,
            ExportAnimations = ExportAnimations,
            OverwriteExisting = OverwriteExisting,
            ExternalSkeleton = ExternalSkeleton ?? default
        };
    }
}
