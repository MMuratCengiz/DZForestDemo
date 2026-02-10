using NiziKit.Editor.Services;
using NiziKit.Offline;

namespace NiziKit.Editor.ViewModels;

public class MeshItemViewModel
{
    public bool IsEnabled { get; set; } = true;
    public string ExportName { get; set; } = string.Empty;
    public string OriginalName { get; set; } = string.Empty;
    public uint VertexCount { get; set; }
    public uint IndexCount { get; set; }
    public bool HasSkin { get; set; }

    public string DisplayInfo => $"{VertexCount:N0} verts, {IndexCount:N0} idx{(HasSkin ? ", skinned" : "")}";
}

public class AnimationItemViewModel
{
    public bool IsEnabled { get; set; } = true;
    public string ExportName { get; set; } = string.Empty;
    public string OriginalName { get; set; } = string.Empty;
    public double Duration { get; set; }
    public uint ChannelCount { get; set; }

    public string DisplayInfo => $"{Duration:F1}s, {ChannelCount} channels";
}

public class ImportAssetItemViewModel
{
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string AssetName { get; set; } = string.Empty;
    public string OutputSubdirectory { get; set; } = string.Empty;
    public AssetFileType FileType { get; set; } = AssetFileType.Other;

    public bool IsModel => FileType == AssetFileType.Model;
    public bool IsTexture => FileType == AssetFileType.Texture;

    public bool GenerateMips { get; set; } = true;
    public int EmbeddedTextureCount { get; set; }
    public bool IsScanning { get; set; }
    public bool ScanComplete { get; set; }
    public string? ScanError { get; set; }

    public List<MeshItemViewModel> Meshes { get; } = [];
    public List<AnimationItemViewModel> Animations { get; } = [];

    public bool HasSkeleton { get; set; }
    public int JointCount { get; set; }
    public string RootJointName { get; set; } = string.Empty;

    public bool ExportSkeleton { get; set; } = true;
    public bool ExportAnimations { get; set; } = true;

    public float Scale { get; set; } = 1.0f;
    public ExportFormat Format { get; set; } = ExportFormat.Glb;
    public bool OptimizeMeshes { get; set; } = true;
    public bool GenerateNormals { get; set; } = true;
    public bool CalculateTangents { get; set; } = true;
    public bool TriangulateMeshes { get; set; } = true;
    public bool JoinIdenticalVertices { get; set; } = true;
    public bool SmoothNormals { get; set; } = true;
    public float SmoothNormalsAngle { get; set; } = 80.0f;
    public bool LimitBoneWeights { get; set; } = true;
    public uint MaxBoneWeightsPerVertex { get; set; } = 4;

    public bool IsImporting { get; set; }
    public bool ImportComplete { get; set; }
    public string? ImportError { get; set; }

    public string StatusText
    {
        get
        {
            if (IsImporting)
            {
                return "Importing...";
            }

            if (ImportComplete)
            {
                return ImportError != null ? $"Error: {ImportError}" : "Done";
            }

            if (IsScanning)
            {
                return "Scanning...";
            }

            if (ScanError != null)
            {
                return $"Scan error: {ScanError}";
            }

            if (ScanComplete)
            {
                return $"{Meshes.Count} meshes, {Animations.Count} anims";
            }

            if (IsTexture)
            {
                return "Texture";
            }

            return "Pending";
        }
    }

    public void ApplyIntrospection(AssetIntrospectionResult result)
    {
        if (result.HasError)
        {
            ScanError = result.Error;
            ScanComplete = true;
            IsScanning = false;
            return;
        }

        Meshes.Clear();
        foreach (var mesh in result.Meshes)
        {
            Meshes.Add(new MeshItemViewModel
            {
                IsEnabled = true,
                ExportName = mesh.Name,
                OriginalName = mesh.Name,
                VertexCount = mesh.VertexCount,
                IndexCount = mesh.IndexCount,
                HasSkin = mesh.HasSkin
            });
        }

        Animations.Clear();
        foreach (var anim in result.Animations)
        {
            Animations.Add(new AnimationItemViewModel
            {
                IsEnabled = true,
                ExportName = anim.Name,
                OriginalName = anim.Name,
                Duration = anim.Duration,
                ChannelCount = anim.ChannelCount
            });
        }

        if (result.Skeleton != null)
        {
            HasSkeleton = true;
            JointCount = result.Skeleton.JointCount;
            RootJointName = result.Skeleton.RootJointName;
        }

        EmbeddedTextureCount = result.EmbeddedTextureCount;

        ScanComplete = true;
        IsScanning = false;
    }

    public AssetExportDesc ToExportDesc(string outputDirectory)
    {
        var outDir = string.IsNullOrEmpty(OutputSubdirectory)
            ? outputDirectory
            : Path.Combine(outputDirectory, OutputSubdirectory);

        return new AssetExportDesc
        {
            SourcePath = FilePath,
            OutputDirectory = Path.Combine(outDir, "Models"),
            AssetName = AssetName,
            Format = Format,
            Scale = Scale,
            EmbedTextures = false,
            OverwriteExisting = true,
            OptimizeMeshes = OptimizeMeshes,
            GenerateNormals = GenerateNormals,
            CalculateTangents = CalculateTangents,
            TriangulateMeshes = TriangulateMeshes,
            JoinIdenticalVertices = JoinIdenticalVertices,
            SmoothNormals = SmoothNormals,
            SmoothNormalsAngle = SmoothNormalsAngle,
            LimitBoneWeights = LimitBoneWeights,
            MaxBoneWeightsPerVertex = MaxBoneWeightsPerVertex,
            ExportSkeleton = ExportSkeleton && HasSkeleton,
            ExportAnimations = ExportAnimations && Animations.Count > 0
        };
    }
}
