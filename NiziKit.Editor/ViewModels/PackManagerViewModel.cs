using System.Collections.ObjectModel;
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

    public PackManagerViewModel()
    {
        _packsDirectory = Path.Combine(Content.ResolvePath(""), "Packs");
        _assetsDirectory = Content.ResolvePath("");
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
    public async Task SavePack()
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
            await File.WriteAllTextAsync(packFile, json);

            StatusMessage = $"Pack saved: {PackName}";
            LoadPacks();

            // Select the saved pack
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
    public async Task BrowseTexture()
    {
        await BrowseAsset(AssetFileType.Texture, Textures);
    }

    [RelayCommand]
    public async Task BrowseMaterial()
    {
        await BrowseAsset(AssetFileType.Material, Materials);
    }

    [RelayCommand]
    public async Task BrowseModel()
    {
        await BrowseAsset(AssetFileType.Model, Models);
    }

    [RelayCommand]
    public async Task BrowseShader()
    {
        await BrowseAsset(AssetFileType.Shader, Shaders);
    }

    private async Task BrowseAsset(AssetFileType type, ObservableCollection<PackAssetEntry> collection)
    {
        if (_topLevel == null)
        {
            StatusMessage = "Cannot open file browser";
            return;
        }

        var filters = GetFileFilters(type);
        var options = new FilePickerOpenOptions
        {
            Title = $"Select {type}",
            AllowMultiple = true,
            FileTypeFilter = filters,
            SuggestedStartLocation = await _topLevel.StorageProvider.TryGetFolderFromPathAsync(_assetsDirectory)
        };

        var files = await _topLevel.StorageProvider.OpenFilePickerAsync(options);

        foreach (var file in files)
        {
            var fullPath = file.Path.LocalPath;
            var relativePath = GetRelativePath(fullPath);
            var key = Path.GetFileNameWithoutExtension(fullPath);

            // Avoid duplicates
            if (!collection.Any(e => e.Path == relativePath))
            {
                collection.Add(new PackAssetEntry(key, relativePath));
            }
        }

        if (files.Count > 0)
        {
            StatusMessage = $"Added {files.Count} {type}(s)";
        }
    }

    private List<FilePickerFileType> GetFileFilters(AssetFileType type)
    {
        return type switch
        {
            AssetFileType.Texture => [new FilePickerFileType("Images") { Patterns = ["*.png", "*.jpg", "*.jpeg", "*.tga", "*.bmp", "*.dds"] }],
            AssetFileType.Material => [new FilePickerFileType("Materials") { Patterns = ["*.nizimat.json"] }],
            AssetFileType.Model => [new FilePickerFileType("Models") { Patterns = ["*.fbx", "*.glb", "*.gltf", "*.obj", "*.dae"] }],
            AssetFileType.Shader => [new FilePickerFileType("Shaders") { Patterns = ["*.nizishp.json"] }],
            _ => [new FilePickerFileType("All Files") { Patterns = ["*.*"] }]
        };
    }

    private string GetRelativePath(string fullPath)
    {
        if (fullPath.StartsWith(_assetsDirectory, StringComparison.OrdinalIgnoreCase))
        {
            var relative = fullPath[_assetsDirectory.Length..].TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            return relative.Replace(Path.DirectorySeparatorChar, '/');
        }
        return fullPath.Replace(Path.DirectorySeparatorChar, '/');
    }

    [RelayCommand]
    public async Task CreateMaterial()
    {
        if (_topLevel == null || string.IsNullOrWhiteSpace(PackName))
        {
            StatusMessage = "Create a pack first";
            return;
        }

        var packDir = Path.Combine(_packsDirectory, PackName);
        Directory.CreateDirectory(packDir);

        var baseName = $"material_{Materials.Count + 1}";
        var fileName = $"{baseName}.nizimat.json";
        var filePath = Path.Combine(packDir, fileName);

        // Create default material JSON
        var materialJson = """
{
  "shader": "default",
  "parameters": {}
}
""";

        await File.WriteAllTextAsync(filePath, materialJson);

        var relativePath = GetRelativePath(filePath);
        Materials.Add(new PackAssetEntry(baseName, relativePath));
        StatusMessage = $"Created material: {baseName}";
    }

    [RelayCommand]
    public async Task CreateShader()
    {
        if (_topLevel == null || string.IsNullOrWhiteSpace(PackName))
        {
            StatusMessage = "Create a pack first";
            return;
        }

        var packDir = Path.Combine(_packsDirectory, PackName);
        Directory.CreateDirectory(packDir);

        var baseName = $"shader_{Shaders.Count + 1}";
        var fileName = $"{baseName}.nizishp.json";
        var filePath = Path.Combine(packDir, fileName);

        // Create default shader JSON
        var shaderJson = """
{
  "name": "custom_shader",
  "vertex": "default.vert",
  "fragment": "default.frag",
  "parameters": []
}
""";

        await File.WriteAllTextAsync(filePath, shaderJson);

        var relativePath = GetRelativePath(filePath);
        Shaders.Add(new PackAssetEntry(baseName, relativePath));
        StatusMessage = $"Created shader: {baseName}";
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
