using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using NiziKit.Offline;

namespace NiziKit.Editor.ViewModels;

public partial class MeshItemViewModel : ObservableObject
{
    [ObservableProperty] private bool _isEnabled = true;
    [ObservableProperty] private string _exportName = string.Empty;
    [ObservableProperty] private string _originalName = string.Empty;
    [ObservableProperty] private uint _vertexCount;
    [ObservableProperty] private uint _indexCount;
    [ObservableProperty] private bool _hasSkin;

    public string DisplayInfo => $"{VertexCount:N0} verts, {IndexCount:N0} idx{(HasSkin ? ", skinned" : "")}";
}

public partial class AnimationItemViewModel : ObservableObject
{
    [ObservableProperty] private bool _isEnabled = true;
    [ObservableProperty] private string _exportName = string.Empty;
    [ObservableProperty] private string _originalName = string.Empty;
    [ObservableProperty] private double _duration;
    [ObservableProperty] private uint _channelCount;

    public string DisplayInfo => $"{Duration:F1}s, {ChannelCount} channels";
}

public partial class ImportAssetItemViewModel : ObservableObject
{
    [ObservableProperty] private string _fileName = string.Empty;
    [ObservableProperty] private string _filePath = string.Empty;
    [ObservableProperty] private string _assetName = string.Empty;
    [ObservableProperty] private string _outputSubdirectory = string.Empty;

    [ObservableProperty] private bool _isScanning;
    [ObservableProperty] private bool _scanComplete;
    [ObservableProperty] private string? _scanError;

    public ObservableCollection<MeshItemViewModel> Meshes { get; } = [];
    public ObservableCollection<AnimationItemViewModel> Animations { get; } = [];

    [ObservableProperty] private bool _hasSkeleton;
    [ObservableProperty] private int _jointCount;
    [ObservableProperty] private string _rootJointName = string.Empty;

    [ObservableProperty] private bool _exportSkeleton = true;
    [ObservableProperty] private bool _exportAnimations = true;

    [ObservableProperty] private float _scale = 1.0f;
    [ObservableProperty] private ExportFormat _format = ExportFormat.Glb;
    [ObservableProperty] private bool _optimizeMeshes = true;
    [ObservableProperty] private bool _generateNormals = true;
    [ObservableProperty] private bool _calculateTangents = true;
    [ObservableProperty] private bool _triangulateMeshes = true;
    [ObservableProperty] private bool _joinIdenticalVertices = true;
    [ObservableProperty] private bool _smoothNormals = true;
    [ObservableProperty] private float _smoothNormalsAngle = 80.0f;
    [ObservableProperty] private bool _limitBoneWeights = true;
    [ObservableProperty] private uint _maxBoneWeightsPerVertex = 4;

    [ObservableProperty] private bool _isImporting;
    [ObservableProperty] private bool _importComplete;
    [ObservableProperty] private string? _importError;

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

            return "Pending";
        }
    }

    partial void OnIsScanningChanged(bool value) => OnPropertyChanged(nameof(StatusText));
    partial void OnScanCompleteChanged(bool value) => OnPropertyChanged(nameof(StatusText));
    partial void OnScanErrorChanged(string? value) => OnPropertyChanged(nameof(StatusText));
    partial void OnIsImportingChanged(bool value) => OnPropertyChanged(nameof(StatusText));
    partial void OnImportCompleteChanged(bool value) => OnPropertyChanged(nameof(StatusText));
    partial void OnImportErrorChanged(string? value) => OnPropertyChanged(nameof(StatusText));

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
