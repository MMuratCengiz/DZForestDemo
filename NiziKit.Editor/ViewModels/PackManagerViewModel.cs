using System.Collections.ObjectModel;
using System.Text.Json;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NiziKit.Assets.Serde;
using NiziKit.ContentPipeline;
using NiziKit.Editor.Services;

namespace NiziKit.Editor.ViewModels;

public class PackInfo
{
    public required string Name { get; init; }
    public required string Path { get; init; }
    public required string Version { get; init; }
}

public partial class PackAssetEntry : ObservableObject
{
    [ObservableProperty]
    private string _key = "";

    [ObservableProperty]
    private string _path = "";

    public PackAssetEntry()
    {
    }

    public PackAssetEntry(string key, string path)
    {
        _key = key;
        _path = path;
    }
}

public partial class PackManagerViewModel : ObservableObject
{
    private readonly string _packsDirectory;
    private readonly string _assetsDirectory;
    private TopLevel? _topLevel;
    private AssetFileType _pendingBrowseType;
    private ObservableCollection<PackAssetEntry>? _pendingBrowseCollection;

    public PackManagerViewModel()
    {
        _packsDirectory = Path.Combine(Content.ResolvePath(""), "Packs");
        _assetsDirectory = Content.ResolvePath("");
        FileBrowserViewModel = new FileBrowserViewModel(_assetsDirectory);
        FileBrowserViewModel.FileDoubleClicked += OnFileBrowserFileSelected;
        LoadPacks();
    }

    public void SetTopLevel(TopLevel topLevel)
    {
        _topLevel = topLevel;
    }

    [ObservableProperty]
    private ObservableCollection<PackInfo> _packs = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelectedPack))]
    private PackInfo? _selectedPack;

    [ObservableProperty]
    private string _packName = "";

    [ObservableProperty]
    private string _packVersion = "1.0.0";

    [ObservableProperty]
    private ObservableCollection<PackAssetEntry> _textures = [];

    [ObservableProperty]
    private ObservableCollection<PackAssetEntry> _materials = [];

    [ObservableProperty]
    private ObservableCollection<PackAssetEntry> _models = [];

    [ObservableProperty]
    private ObservableCollection<PackAssetEntry> _shaders = [];

    [ObservableProperty]
    private bool _isEditing;

    [ObservableProperty]
    private string _statusMessage = "";

    [ObservableProperty]
    private int _selectedTabIndex;

    [ObservableProperty]
    private bool _isFileBrowserOpen;

    [ObservableProperty]
    private string _fileBrowserTitle = "Select File";

    [ObservableProperty]
    private FileBrowserViewModel _fileBrowserViewModel;

    [ObservableProperty]
    private bool _isAssetEditorOpen;

    [ObservableProperty]
    private string _assetEditorTitle = "Edit Asset";

    [ObservableProperty]
    private PackAssetEntry? _editingAsset;

    [ObservableProperty]
    private AssetFileType _editingAssetType;

    [ObservableProperty]
    private string _editingAssetName = "";

    [ObservableProperty]
    private string _editingAssetShader = "";

    [ObservableProperty]
    private string _editingAssetAlbedo = "";

    [ObservableProperty]
    private string _editingAssetNormal = "";

    [ObservableProperty]
    private string _editingAssetMetallic = "";

    [ObservableProperty]
    private string _editingAssetRoughness = "";

    [ObservableProperty]
    private string _editingShaderName = "";

    [ObservableProperty]
    private string _editingShaderType = "graphics";

    [ObservableProperty]
    private string _editingShaderVertexPath = "";

    [ObservableProperty]
    private string _editingShaderPixelPath = "";

    public bool HasSelectedPack => SelectedPack != null;

    private void LoadPacks()
    {
        Packs.Clear();

        if (!Directory.Exists(_packsDirectory))
        {
            return;
        }

        foreach (var dir in Directory.GetDirectories(_packsDirectory))
        {
            var packFile = Path.Combine(dir, "pack.nizipack.json");
            if (!File.Exists(packFile))
            {
                continue;
            }

            try
            {
                var json = File.ReadAllText(packFile);
                var pack = AssetPackJson.FromJson(json);
                Packs.Add(new PackInfo
                {
                    Name = pack.Name,
                    Path = packFile,
                    Version = pack.Version
                });
            }
            catch
            {
                // Skip invalid pack files
            }
        }
    }

    [RelayCommand]
    public void NewPack()
    {
        PackName = "NewPack";
        PackVersion = "1.0.0";
        Textures.Clear();
        Materials.Clear();
        Models.Clear();
        Shaders.Clear();
        SelectedPack = null;
        IsEditing = true;
        StatusMessage = "Creating new pack...";
    }

    [RelayCommand]
    public void LoadPack(PackInfo? pack)
    {
        if (pack == null)
        {
            return;
        }

        try
        {
            var json = File.ReadAllText(pack.Path);
            var packData = AssetPackJson.FromJson(json);

            PackName = packData.Name;
            PackVersion = packData.Version;

            Textures.Clear();
            foreach (var (key, path) in packData.Textures)
            {
                Textures.Add(new PackAssetEntry(key, path));
            }

            Materials.Clear();
            foreach (var (key, path) in packData.Materials)
            {
                Materials.Add(new PackAssetEntry(key, path));
            }

            Models.Clear();
            foreach (var (key, path) in packData.Models)
            {
                Models.Add(new PackAssetEntry(key, path));
            }

            Shaders.Clear();
            foreach (var (key, path) in packData.Shaders)
            {
                Shaders.Add(new PackAssetEntry(key, path));
            }

            SelectedPack = pack;
            IsEditing = true;
            StatusMessage = $"Loaded pack: {pack.Name}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading pack: {ex.Message}";
        }
    }

    [RelayCommand]
    public void SavePack()
    {
        if (string.IsNullOrWhiteSpace(PackName))
        {
            StatusMessage = "Pack name is required";
            return;
        }

        try
        {
            var packDir = Path.Combine(_packsDirectory, PackName);
            Directory.CreateDirectory(packDir);

            var packData = new AssetPackJson
            {
                Name = PackName.ToLowerInvariant(),
                Version = PackVersion
            };

            foreach (var entry in Textures)
            {
                if (!string.IsNullOrWhiteSpace(entry.Key) && !string.IsNullOrWhiteSpace(entry.Path))
                {
                    packData.Textures[entry.Key] = entry.Path;
                }
            }

            foreach (var entry in Materials)
            {
                if (!string.IsNullOrWhiteSpace(entry.Key) && !string.IsNullOrWhiteSpace(entry.Path))
                {
                    packData.Materials[entry.Key] = entry.Path;
                }
            }

            foreach (var entry in Models)
            {
                if (!string.IsNullOrWhiteSpace(entry.Key) && !string.IsNullOrWhiteSpace(entry.Path))
                {
                    packData.Models[entry.Key] = entry.Path;
                }
            }

            foreach (var entry in Shaders)
            {
                if (!string.IsNullOrWhiteSpace(entry.Key) && !string.IsNullOrWhiteSpace(entry.Path))
                {
                    packData.Shaders[entry.Key] = entry.Path;
                }
            }

            var packFile = Path.Combine(packDir, "pack.nizipack.json");
            var json = packData.ToJson();
            File.WriteAllText(packFile, json);

            StatusMessage = $"Pack saved: {PackName}";
            LoadPacks();
            SelectedPack = Packs.FirstOrDefault(p => p.Name.Equals(PackName, StringComparison.OrdinalIgnoreCase));
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error saving pack: {ex.Message}";
        }
    }

    [RelayCommand]
    public void AddTexture()
    {
        Textures.Add(new PackAssetEntry());
    }

    [RelayCommand]
    public void AddMaterial()
    {
        Materials.Add(new PackAssetEntry());
    }

    [RelayCommand]
    public void AddModel()
    {
        Models.Add(new PackAssetEntry());
    }

    [RelayCommand]
    public void AddShader()
    {
        Shaders.Add(new PackAssetEntry());
    }

    [RelayCommand]
    public void RemoveTexture(PackAssetEntry? entry)
    {
        if (entry != null)
        {
            Textures.Remove(entry);
        }
    }

    [RelayCommand]
    public void RemoveMaterial(PackAssetEntry? entry)
    {
        if (entry != null)
        {
            Materials.Remove(entry);
        }
    }

    [RelayCommand]
    public void RemoveModel(PackAssetEntry? entry)
    {
        if (entry != null)
        {
            Models.Remove(entry);
        }
    }

    [RelayCommand]
    public void RemoveShader(PackAssetEntry? entry)
    {
        if (entry != null)
        {
            Shaders.Remove(entry);
        }
    }

    [RelayCommand]
    public void RefreshPacks()
    {
        LoadPacks();
        StatusMessage = "Packs refreshed";
    }

    [RelayCommand]
    public void BrowseTexture()
    {
        OpenFileBrowser(AssetFileType.Texture, Textures, "Select Texture");
    }

    [RelayCommand]
    public void BrowseMaterial()
    {
        OpenFileBrowser(AssetFileType.Material, Materials, "Select Material");
    }

    [RelayCommand]
    public void BrowseModel()
    {
        OpenFileBrowser(AssetFileType.Model, Models, "Select Model");
    }

    [RelayCommand]
    public void BrowseShader()
    {
        OpenFileBrowser(AssetFileType.Shader, Shaders, "Select Shader");
    }

    private void OpenFileBrowser(AssetFileType type, ObservableCollection<PackAssetEntry> collection, string title)
    {
        _pendingBrowseType = type;
        _pendingBrowseCollection = collection;
        FileBrowserTitle = title;
        FileBrowserViewModel.Filter = type;
        FileBrowserViewModel.SetRootPath(_assetsDirectory);
        IsFileBrowserOpen = true;
    }

    private void OnFileBrowserFileSelected(FileEntry entry)
    {
        if (_pendingBrowseCollection == null)
        {
            return;
        }

        var relativePath = GetRelativePath(entry.FullPath);
        if (relativePath == null)
        {
            StatusMessage = "File must be within the Assets directory";
            IsFileBrowserOpen = false;
            _pendingBrowseCollection = null;
            return;
        }

        var packRelativePath = GetPackRelativePath(relativePath);

        var key = Path.GetFileNameWithoutExtension(entry.Name);
        if (entry.Name.EndsWith(".nizimat.json", StringComparison.OrdinalIgnoreCase))
        {
            key = entry.Name[..^".nizimat.json".Length];
        }
        else if (entry.Name.EndsWith(".nizishp.json", StringComparison.OrdinalIgnoreCase))
        {
            key = entry.Name[..^".nizishp.json".Length];
        }

        if (!_pendingBrowseCollection.Any(e => e.Path == packRelativePath))
        {
            _pendingBrowseCollection.Add(new PackAssetEntry(key, packRelativePath));
            StatusMessage = $"Added: {key}";
        }

        IsFileBrowserOpen = false;
        _pendingBrowseCollection = null;
    }

    private string GetPackRelativePath(string assetsRelativePath)
    {
        if (string.IsNullOrEmpty(PackName))
        {
            return assetsRelativePath;
        }

        var packPrefix = $"Packs/{PackName}/";
        if (assetsRelativePath.StartsWith(packPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return assetsRelativePath[packPrefix.Length..];
        }

        return assetsRelativePath;
    }

    [RelayCommand]
    public void CloseFileBrowser()
    {
        IsFileBrowserOpen = false;
        _pendingBrowseCollection = null;
    }

    [RelayCommand]
    public void OpenAssetEditor(PackAssetEntry entry)
    {
        var fullPath = Path.Combine(_assetsDirectory, entry.Path);
        if (!File.Exists(fullPath))
        {
            StatusMessage = $"File not found: {entry.Path}";
            return;
        }

        EditingAsset = entry;

        if (entry.Path.EndsWith(".nizimat.json", StringComparison.OrdinalIgnoreCase))
        {
            EditingAssetType = AssetFileType.Material;
            AssetEditorTitle = $"Edit Material: {entry.Key}";
            LoadMaterialForEditing(fullPath);
        }
        else if (entry.Path.EndsWith(".nizishp.json", StringComparison.OrdinalIgnoreCase))
        {
            EditingAssetType = AssetFileType.Shader;
            AssetEditorTitle = $"Edit Shader: {entry.Key}";
            LoadShaderForEditing(fullPath);
        }
        else
        {
            StatusMessage = "Only materials and shaders can be edited";
            return;
        }

        IsAssetEditorOpen = true;
    }

    private void LoadMaterialForEditing(string path)
    {
        try
        {
            var json = File.ReadAllText(path);
            var material = MaterialJson.FromJson(json);
            EditingAssetName = material.Name;
            EditingAssetShader = material.Shader;
            EditingAssetAlbedo = material.Textures?.Albedo ?? "";
            EditingAssetNormal = material.Textures?.Normal ?? "";
            EditingAssetMetallic = material.Textures?.Metallic ?? "";
            EditingAssetRoughness = material.Textures?.Roughness ?? "";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading material: {ex.Message}";
        }
    }

    private void LoadShaderForEditing(string path)
    {
        try
        {
            var json = File.ReadAllText(path);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            EditingShaderName = root.TryGetProperty("name", out var name) ? name.GetString() ?? "" : "";
            EditingShaderType = root.TryGetProperty("type", out var type) ? type.GetString() ?? "graphics" : "graphics";

            if (root.TryGetProperty("stages", out var stages) && stages.ValueKind == JsonValueKind.Array)
            {
                foreach (var stage in stages.EnumerateArray())
                {
                    var stageType = stage.TryGetProperty("stage", out var st) ? st.GetString() : "";
                    var stagePath = stage.TryGetProperty("path", out var sp) ? sp.GetString() ?? "" : "";

                    if (stageType == "vertex")
                    {
                        EditingShaderVertexPath = stagePath;
                    }
                    else if (stageType == "pixel")
                    {
                        EditingShaderPixelPath = stagePath;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading shader: {ex.Message}";
        }
    }

    [RelayCommand]
    public void CloseAssetEditor()
    {
        IsAssetEditorOpen = false;
        EditingAsset = null;
    }

    [RelayCommand]
    public void SaveAssetEditor()
    {
        if (EditingAsset == null)
        {
            return;
        }

        var fullPath = Path.Combine(_assetsDirectory, EditingAsset.Path);
        var assetKey = EditingAsset.Key;

        try
        {
            if (EditingAssetType == AssetFileType.Material)
            {
                SaveMaterial(fullPath);
            }
            else if (EditingAssetType == AssetFileType.Shader)
            {
                SaveShader(fullPath);
            }

            StatusMessage = $"Saved: {assetKey}";
            IsAssetEditorOpen = false;
            EditingAsset = null;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error saving: {ex.Message}";
        }
    }

    private void SaveMaterial(string path)
    {
        var material = new MaterialJson
        {
            Name = EditingAssetName,
            Shader = EditingAssetShader,
            Textures = new TexturesJson
            {
                Albedo = string.IsNullOrWhiteSpace(EditingAssetAlbedo) ? null : EditingAssetAlbedo,
                Normal = string.IsNullOrWhiteSpace(EditingAssetNormal) ? null : EditingAssetNormal,
                Metallic = string.IsNullOrWhiteSpace(EditingAssetMetallic) ? null : EditingAssetMetallic,
                Roughness = string.IsNullOrWhiteSpace(EditingAssetRoughness) ? null : EditingAssetRoughness
            }
        };

        var json = material.ToJson();
        File.WriteAllText(path, json);
    }

    private void SaveShader(string path)
    {
        var stages = new List<object>();

        if (!string.IsNullOrWhiteSpace(EditingShaderVertexPath))
        {
            stages.Add(new { stage = "vertex", path = EditingShaderVertexPath, entryPoint = "VSMain" });
        }
        if (!string.IsNullOrWhiteSpace(EditingShaderPixelPath))
        {
            stages.Add(new { stage = "pixel", path = EditingShaderPixelPath, entryPoint = "PSMain" });
        }

        var shader = new
        {
            name = EditingShaderName,
            type = EditingShaderType,
            stages,
            pipeline = new
            {
                primitiveTopology = "triangle",
                cullMode = "backFace",
                fillMode = "solid",
                depthTest = new { enable = true, compareOp = "less", write = true },
                blend = new { enable = false, renderTargetWriteMask = 15 }
            }
        };

        var json = JsonSerializer.Serialize(shader, NiziJsonSerializationOptions.Default);
        File.WriteAllText(path, json);
    }

    private string? GetRelativePath(string fullPath)
    {
        var normalizedFullPath = Path.GetFullPath(fullPath);
        var normalizedAssetsDir = Path.GetFullPath(_assetsDirectory);

        if (!normalizedFullPath.StartsWith(normalizedAssetsDir, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var relative = normalizedFullPath[(normalizedAssetsDir.Length + 1)..];
        return relative.Replace(Path.DirectorySeparatorChar, '/');
    }

    [RelayCommand]
    public void CreateMaterial()
    {
        if (string.IsNullOrWhiteSpace(PackName))
        {
            StatusMessage = "Create a pack first";
            return;
        }

        var packDir = Path.Combine(_packsDirectory, PackName);
        Directory.CreateDirectory(packDir);

        var baseName = $"material_{Materials.Count + 1}";
        var fileName = $"{baseName}.nizimat.json";
        var filePath = Path.Combine(packDir, fileName);

        var material = new MaterialJson
        {
            Name = baseName,
            Shader = "",
            Textures = new TexturesJson()
        };

        var json = material.ToJson();
        File.WriteAllText(filePath, json);

        var relativePath = GetRelativePath(filePath) ?? fileName;
        var entry = new PackAssetEntry(baseName, relativePath);
        Materials.Add(entry);
        StatusMessage = $"Created material: {baseName}";
        OpenAssetEditor(entry);
    }

    [RelayCommand]
    public void CreateShader()
    {
        if (string.IsNullOrWhiteSpace(PackName))
        {
            StatusMessage = "Create a pack first";
            return;
        }

        var packDir = Path.Combine(_packsDirectory, PackName);
        Directory.CreateDirectory(packDir);

        var baseName = $"shader_{Shaders.Count + 1}";
        var fileName = $"{baseName}.nizishp.json";
        var filePath = Path.Combine(packDir, fileName);

        var shader = new
        {
            name = baseName,
            type = "graphics",
            stages = new[]
            {
                new { stage = "vertex", path = "", entryPoint = "VSMain" },
                new { stage = "pixel", path = "", entryPoint = "PSMain" }
            },
            pipeline = new
            {
                primitiveTopology = "triangle",
                cullMode = "backFace",
                fillMode = "solid",
                depthTest = new { enable = true, compareOp = "less", write = true },
                blend = new { enable = false, renderTargetWriteMask = 15 }
            }
        };

        var json = JsonSerializer.Serialize(shader, NiziJsonSerializationOptions.Default);
        File.WriteAllText(filePath, json);

        var relativePath = GetRelativePath(filePath) ?? fileName;
        var entry = new PackAssetEntry(baseName, relativePath);
        Shaders.Add(entry);
        StatusMessage = $"Created shader: {baseName}";
        OpenAssetEditor(entry);
    }

    public void AddAssetEntry(AssetFileType type, string key, string relativePath)
    {
        var entry = new PackAssetEntry(key, relativePath);

        switch (type)
        {
            case AssetFileType.Texture:
                Textures.Add(entry);
                break;
            case AssetFileType.Material:
                Materials.Add(entry);
                break;
            case AssetFileType.Model:
                Models.Add(entry);
                break;
            case AssetFileType.Shader:
                Shaders.Add(entry);
                break;
        }
    }
}
