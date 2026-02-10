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

public class ImportViewModel
{
    private static string? _lastBrowsedPath;

    private static readonly HashSet<string> ModelExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".fbx", ".glb", ".gltf", ".obj", ".dae", ".blend"
    };

    private static readonly HashSet<string> TextureExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".png", ".jpg", ".jpeg", ".tga", ".bmp", ".dds"
    };

    public ImportViewModel()
    {
        var startPath = _lastBrowsedPath
                        ?? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (string.IsNullOrEmpty(startPath))
        {
            startPath = OperatingSystem.IsWindows() ? "C:\\" : "/";
        }

        FileBrowser = new FileBrowserViewModel(startPath);
        FileBrowser.SelectionChanged += OnSelectionChanged;
    }

    public FileBrowserViewModel FileBrowser { get; }

    public ImportType ImportType { get; set; } = ImportType.Both;
    public float ModelScale { get; set; } = 1.0f;
    public bool GenerateMips { get; set; } = true;
    public string OutputDirectory { get; set; } = "";
    public string ProgressText { get; set; } = "";
    public bool IsImporting { get; set; }
    public double Progress { get; set; }
    public bool ImportSucceeded { get; set; }
    public int ImportedCount { get; set; }
    public int FailedCount { get; set; }

    public List<FileEntry> SelectedFiles { get; set; } = [];
    public List<ImportAssetItemViewModel> ImportItems { get; } = [];

    public ImportAssetItemViewModel? SelectedImportItem { get; set; }
    public bool HasSelectedItem => SelectedImportItem != null;

    public event Action? ImportCompleted;
    public event Action? ImportCancelled;

    private CancellationTokenSource? _cancellationTokenSource;

    public static bool IsModelFile(string path)
    {
        var ext = Path.GetExtension(path);
        return ModelExtensions.Contains(ext);
    }

    public static bool IsTextureFile(string path)
    {
        var ext = Path.GetExtension(path);
        return TextureExtensions.Contains(ext);
    }

    public static bool IsImportableFile(string path)
    {
        return IsModelFile(path) || IsTextureFile(path);
    }

    private void OnSelectionChanged(IReadOnlyList<FileEntry> entries)
    {
        SelectedFiles.Clear();
        foreach (var entry in entries)
        {
            SelectedFiles.Add(entry);
        }
    }

    public void AddToQueue()
    {
        var filesToAdd = SelectedFiles.ToList();
        if (filesToAdd.Count == 0 && FileBrowser.SelectedEntry != null)
        {
            filesToAdd.Add(FileBrowser.SelectedEntry);
        }

        foreach (var file in filesToAdd)
        {
            if (file.IsDirectory || ImportItems.Any(i => i.FilePath == file.FullPath))
            {
                continue;
            }

            var item = new ImportAssetItemViewModel
            {
                FileName = file.Name,
                FilePath = file.FullPath,
                AssetName = Path.GetFileNameWithoutExtension(file.Name),
                Scale = ModelScale,
                FileType = file.Type,
                GenerateMips = GenerateMips
            };

            ImportItems.Add(item);
            SelectedImportItem ??= item;

            if (item.IsModel)
            {
                ScanItemAsync(item);
            }
        }
    }

    public void RemoveFromQueue(ImportAssetItemViewModel? item)
    {
        if (item == null)
        {
            return;
        }

        ImportItems.Remove(item);
        if (SelectedImportItem == item)
        {
            SelectedImportItem = ImportItems.FirstOrDefault();
        }
    }

    public void AddFilesToQueue(IEnumerable<string> paths)
    {
        foreach (var path in paths)
        {
            if (!File.Exists(path))
            {
                continue;
            }

            if (ImportItems.Any(i => i.FilePath == path))
            {
                continue;
            }

            var name = Path.GetFileName(path);
            var fileType = IsModelFile(path) ? AssetFileType.Model
                         : IsTextureFile(path) ? AssetFileType.Texture
                         : AssetFileType.Other;

            var item = new ImportAssetItemViewModel
            {
                FileName = name,
                FilePath = path,
                AssetName = Path.GetFileNameWithoutExtension(name),
                Scale = ModelScale,
                FileType = fileType,
                GenerateMips = GenerateMips
            };

            ImportItems.Add(item);
            SelectedImportItem ??= item;

            if (item.IsModel)
            {
                ScanItemAsync(item);
            }
        }
    }

    private async void ScanItemAsync(ImportAssetItemViewModel item)
    {
        item.IsScanning = true;
        try
        {
            var result = await Task.Run(() =>
            {
                using var introspector = new AssetIntrospector();
                return introspector.Introspect(item.FilePath);
            });

            item.ApplyIntrospection(result);
        }
        catch
        {
            item.ScanComplete = true;
            item.IsScanning = false;
        }
    }

    public void Import()
    {
        if (IsImporting)
        {
            return;
        }

        if (ImportItems.Count > 0)
        {
            ImportFromQueue();
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
        var ct = _cancellationTokenSource.Token;

        var assetsPath = Content.ResolvePath("");
        var outputPath = Path.Combine(assetsPath, OutputDirectory);

        Task.Run(() => RunImport(filesToImport, outputPath, ct), ct);
    }

    private void ImportFromQueue()
    {
        IsImporting = true;
        ImportSucceeded = false;
        Progress = 0;
        _cancellationTokenSource = new CancellationTokenSource();
        var ct = _cancellationTokenSource.Token;

        var assetsPath = Content.ResolvePath("");
        var outputPath = Path.Combine(assetsPath, OutputDirectory);

        var items = ImportItems.ToList();

        Task.Run(() =>
        {
            try
            {
                var total = items.Count;
                for (var i = 0; i < total; i++)
                {
                    if (ct.IsCancellationRequested)
                    {
                        break;
                    }

                    var item = items[i];
                    item.IsImporting = true;
                    ProgressText = $"Importing: {item.FileName}";

                    try
                    {
                        ImportQueueItem(item, outputPath);
                        item.IsImporting = false;
                        item.ImportComplete = true;
                    }
                    catch (Exception ex)
                    {
                        item.IsImporting = false;
                        item.ImportComplete = true;
                        item.ImportError = ex.Message;
                    }

                    Progress = (i + 1) * 100.0 / total;
                }

                var succeeded = items.Count(i => i is { ImportComplete: true, ImportError: null });
                var failed = items.Count(i => i.ImportError != null);

                ImportedCount = succeeded;
                FailedCount = failed;
                ProgressText = failed > 0
                    ? $"Imported {succeeded} of {items.Count} ({failed} failed)"
                    : $"Successfully imported {succeeded} asset(s)";
                Progress = 100;
                IsImporting = false;
                ImportSucceeded = true;

                var done = ImportItems.Where(i => i is { ImportComplete: true, ImportError: null }).ToList();
                foreach (var d in done)
                {
                    ImportItems.Remove(d);
                }
                SelectedImportItem = ImportItems.FirstOrDefault();

                ImportCompleted?.Invoke();
            }
            catch (OperationCanceledException)
            {
                ProgressText = "Import cancelled";
                IsImporting = false;
            }
            catch (Exception ex)
            {
                ProgressText = $"Error: {ex.Message}";
                IsImporting = false;
            }
            finally
            {
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
            }
        }, ct);
    }

    private void ImportQueueItem(ImportAssetItemViewModel item, string outputPath)
    {
        if (item.IsTexture)
        {
            ImportTextureItem(item, outputPath);
            return;
        }

        ImportModelItem(item, outputPath);
    }

    private void ImportTextureItem(ImportAssetItemViewModel item, string outputPath)
    {
        var outDir = string.IsNullOrEmpty(item.OutputSubdirectory)
            ? outputPath
            : Path.Combine(outputPath, item.OutputSubdirectory);
        var texturesPath = Path.Combine(outDir, "Textures");
        Directory.CreateDirectory(texturesPath);

        using var exporter = new TextureExporter();
        var result = exporter.Export(new TextureExportSettings
        {
            SourcePath = item.FilePath,
            OutputDirectory = texturesPath,
            AssetName = item.AssetName,
            GenerateMips = item.GenerateMips
        });

        if (!result.Success)
        {
            throw new Exception(result.ErrorMessage ?? "Texture export failed");
        }
    }

    private void ImportModelItem(ImportAssetItemViewModel item, string outputPath)
    {
        var desc = item.ToExportDesc(outputPath);
        Directory.CreateDirectory(desc.OutputDirectory);

        using var exporter = new AssetExporter();
        var result = exporter.Export(desc);

        if (!result.Success)
        {
            throw new Exception(result.ErrorMessage ?? "Export failed");
        }

        foreach (var meshVm in item.Meshes)
        {
            if (!meshVm.IsEnabled)
            {
                continue;
            }

            if (meshVm.ExportName == meshVm.OriginalName)
            {
                continue;
            }

            var originalFile = Path.Combine(desc.OutputDirectory, $"{meshVm.OriginalName}.nizimesh");
            var newFile = Path.Combine(desc.OutputDirectory, $"{meshVm.ExportName}.nizimesh");
            if (File.Exists(originalFile) && originalFile != newFile)
            {
                File.Move(originalFile, newFile, overwrite: true);
            }
        }

        foreach (var animVm in item.Animations)
        {
            if (!animVm.IsEnabled)
            {
                continue;
            }

            if (animVm.ExportName == animVm.OriginalName)
            {
                continue;
            }

            var originalFile = Path.Combine(desc.OutputDirectory, $"{animVm.OriginalName}.ozzanim");
            var newFile = Path.Combine(desc.OutputDirectory, $"{animVm.ExportName}.ozzanim");
            if (File.Exists(originalFile) && originalFile != newFile)
            {
                File.Move(originalFile, newFile, overwrite: true);
            }
        }
    }

    private void RunImport(List<FileEntry> filesToImport, string outputPath, CancellationToken ct)
    {
        try
        {
            using var importer = new BulkAssetImporter();

            foreach (var file in filesToImport)
            {
                if (ct.IsCancellationRequested)
                {
                    break;
                }

                if (file.IsDirectory)
                {
                    ProgressText = $"Importing directory: {file.Name}";
                    var settings = new BulkImportDesc
                    {
                        SourceDirectory = file.FullPath,
                        OutputDirectory = outputPath,
                        ImportModels = ImportType is ImportType.Models or ImportType.Both,
                        ImportTextures = ImportType is ImportType.Textures or ImportType.Both,
                        ModelScale = ModelScale,
                        GenerateMips = GenerateMips,
                        OnProgress = msg => ProgressText = msg
                    };

                    importer.Import(settings);
                }
                else
                {
                    ProgressText = $"Importing: {file.Name}";
                    ImportSingleFile(file, outputPath);
                }

                var progressIncrement = 100.0 / filesToImport.Count;
                Progress += progressIncrement;
            }

            ProgressText = "Import completed";
            Progress = 100;
            IsImporting = false;
            ImportCompleted?.Invoke();
        }
        catch (OperationCanceledException)
        {
            ProgressText = "Import cancelled";
            IsImporting = false;
        }
        catch (Exception ex)
        {
            ProgressText = $"Error: {ex.Message}";
            IsImporting = false;
        }
        finally
        {
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
        }
    }

    private void ImportSingleFile(FileEntry file, string outputPath)
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

            exporter.Export(desc);
        }
        else if (fileType == AssetFileType.Texture && ImportType is ImportType.Textures or ImportType.Both)
        {
            var texturesPath = Path.Combine(outputPath, "Textures");
            Directory.CreateDirectory(texturesPath);

            var destPath = Path.Combine(texturesPath, file.Name);
            File.Copy(file.FullPath, destPath, overwrite: true);
        }
    }

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
