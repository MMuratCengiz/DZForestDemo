using System.Collections.ObjectModel;
using System.Text.Json;
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

    public PackManagerViewModel()
    {
        _packsDirectory = Path.Combine(Content.ResolvePath(""), "Packs");
        LoadPacks();
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
