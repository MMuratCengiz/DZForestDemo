using System.Collections.ObjectModel;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NiziKit.ContentPipeline;
using NiziKit.Editor.Services;
using NiziKit.Offline;

namespace NiziKit.Editor.ViewModels;

public enum ImportType
{
    Models,
    Textures,
    Both
}

public partial class ImportViewModel : ObservableObject
{
    public ImportViewModel()
    {
        // Start at the user's home directory or a common location
        var startPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (string.IsNullOrEmpty(startPath))
        {
            startPath = OperatingSystem.IsWindows() ? "C:\\" : "/";
        }

        FileBrowser = new FileBrowserViewModel(startPath);
        FileBrowser.SelectionChanged += OnSelectionChanged;
    }

    public FileBrowserViewModel FileBrowser { get; }

    [ObservableProperty]
    private ImportType _importType = ImportType.Both;

    [ObservableProperty]
    private float _modelScale = 1.0f;

    [ObservableProperty]
    private bool _generateMips = true;

    [ObservableProperty]
    private string _outputDirectory = "Synty";

    [ObservableProperty]
    private string _progressText = "";

    [ObservableProperty]
    private bool _isImporting;

    [ObservableProperty]
    private double _progress;

    [ObservableProperty]
    private ObservableCollection<FileEntry> _selectedFiles = [];

    public event Action? ImportCompleted;
    public event Action? ImportCancelled;

    private CancellationTokenSource? _cancellationTokenSource;

    private void OnSelectionChanged(IReadOnlyList<FileEntry> entries)
    {
        SelectedFiles.Clear();
        foreach (var entry in entries)
        {
            SelectedFiles.Add(entry);
        }
    }

    [RelayCommand]
    public async Task Import()
    {
        if (IsImporting)
        {
            return;
        }

        var filesToImport = SelectedFiles.ToList();
        if (filesToImport.Count == 0 && FileBrowser.SelectedEntry != null)
        {
            filesToImport.Add(FileBrowser.SelectedEntry);
        }

        if (filesToImport.Count == 0)
        {
            ProgressText = "No files selected";
            return;
        }

        IsImporting = true;
        Progress = 0;
        _cancellationTokenSource = new CancellationTokenSource();

        try
        {
            var assetsPath = Content.ResolvePath("");
            var outputPath = Path.Combine(assetsPath, OutputDirectory);

            using var importer = new BulkAssetImporter();

            foreach (var file in filesToImport)
            {
                if (_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    break;
                }

                if (file.IsDirectory)
                {
                    // Import entire directory
                    ProgressText = $"Importing directory: {file.Name}";
                    var settings = new DirectoryImportSettings
                    {
                        SourceDirectory = file.FullPath,
                        OutputDirectory = outputPath,
                        ImportModels = ImportType is ImportType.Models or ImportType.Both,
                        ImportTextures = ImportType is ImportType.Textures or ImportType.Both,
                        ModelScale = ModelScale,
                        GenerateMips = GenerateMips,
                        OnProgress = msg => Dispatcher.UIThread.Post(() => ProgressText = msg)
                    };

                    await Task.Run(() => importer.Import(settings), _cancellationTokenSource.Token);
                }
                else
                {
                    // Import single file
                    ProgressText = $"Importing: {file.Name}";
                    await ImportSingleFile(file, outputPath);
                }

                Progress += 100.0 / filesToImport.Count;
            }

            ProgressText = "Import completed";
            Progress = 100;
            ImportCompleted?.Invoke();
        }
        catch (OperationCanceledException)
        {
            ProgressText = "Import cancelled";
        }
        catch (Exception ex)
        {
            ProgressText = $"Error: {ex.Message}";
        }
        finally
        {
            IsImporting = false;
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
        }
    }

    private async Task ImportSingleFile(FileEntry file, string outputPath)
    {
        var fileType = new AssetFileService().GetFileType(file.FullPath);

        if (fileType == AssetFileType.Model && ImportType is ImportType.Models or ImportType.Both)
        {
            var modelsPath = Path.Combine(outputPath, "Models");
            Directory.CreateDirectory(modelsPath);

            using var exporter = new AssetExporter();
            var desc = new AssetExportDesc
            {
                SourcePath = file.FullPath,
                OutputDirectory = modelsPath,
                AssetName = Path.GetFileNameWithoutExtension(file.Name),
                Format = ExportFormat.Glb,
                Scale = ModelScale,
                EmbedTextures = false,
                OverwriteExisting = true,
                OptimizeMeshes = true,
                GenerateNormals = true,
                CalculateTangents = true,
                TriangulateMeshes = true,
                JoinIdenticalVertices = true,
                SmoothNormals = true,
                SmoothNormalsAngle = 80.0f,
                ExportSkeleton = true,
                ExportAnimations = true
            };

            await Task.Run(() => exporter.Export(desc));
        }
        else if (fileType == AssetFileType.Texture && ImportType is ImportType.Textures or ImportType.Both)
        {
            var texturesPath = Path.Combine(outputPath, "Textures");
            Directory.CreateDirectory(texturesPath);

            var destPath = Path.Combine(texturesPath, file.Name);
            await Task.Run(() => File.Copy(file.FullPath, destPath, overwrite: true));
        }
    }

    [RelayCommand]
    public void Cancel()
    {
        if (IsImporting)
        {
            _cancellationTokenSource?.Cancel();
        }
        else
        {
            ImportCancelled?.Invoke();
        }
    }

    public void SetSourcePath(string path)
    {
        if (Directory.Exists(path))
        {
            FileBrowser.SetRootPath(path);
        }
    }
}
