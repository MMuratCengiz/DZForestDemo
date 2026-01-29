using System.Collections.ObjectModel;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NiziKit.Assets.Pack;
using NiziKit.Assets.Serde;
using NiziKit.ContentPipeline;
using NiziKit.Editor.Services;

namespace NiziKit.Editor.ViewModels;

public partial class FolderTreeNode : ObservableObject
{
    [ObservableProperty]
    private string _name = "";

    [ObservableProperty]
    private string _fullPath = "";

    [ObservableProperty]
    private bool _isExpanded;

    [ObservableProperty]
    private bool _isSelected;

    [ObservableProperty]
    private bool _isPack;

    [ObservableProperty]
    private string _packName = "";

    [ObservableProperty]
    private AssetFileType _assetType = AssetFileType.Folder;

    public ObservableCollection<FolderTreeNode> Children { get; } = [];

    public string IconData => IsPack
        ? "M20 6h-8l-2-2H4c-1.1 0-2 .9-2 2v12c0 1.1.9 2 2 2h16c1.1 0 2-.9 2-2V8c0-1.1-.9-2-2-2zm-6 10H6v-2h8v2zm4-4H6v-2h12v2z"
        : AssetType switch
        {
            AssetFileType.Model => "M12 2L2 7l10 5 10-5-10-5zM2 17l10 5 10-5M2 12l10 5 10-5",
            AssetFileType.Texture => "M21 19V5c0-1.1-.9-2-2-2H5c-1.1 0-2 .9-2 2v14c0 1.1.9 2 2 2h14c1.1 0 2-.9 2-2zM8.5 13.5l2.5 3.01L14.5 12l4.5 6H5l3.5-4.5z",
            AssetFileType.Material => "M12 22C6.49 22 2 17.51 2 12S6.49 2 12 2s10 4.04 10 9c0 3.31-2.69 6-6 6h-1.77c-.28 0-.5.22-.5.5 0 .12.05.23.13.33.41.47.64 1.06.64 1.67A2.5 2.5 0 0 1 12 22z",
            AssetFileType.Shader => "M9.4 16.6L4.8 12l4.6-4.6L8 6l-6 6 6 6 1.4-1.4zm5.2 0l4.6-4.6-4.6-4.6L16 6l6 6-6 6-1.4-1.4z",
            _ => "M10 4H4c-1.1 0-1.99.9-1.99 2L2 18c0 1.1.9 2 2 2h16c1.1 0 2-.9 2-2V8c0-1.1-.9-2-2-2h-8l-2-2z"
        };
}

public partial class ContentTab : ObservableObject
{
    [ObservableProperty]
    private string _title = "";

    [ObservableProperty]
    private string _rootPath = "";

    [ObservableProperty]
    private string _currentPath = "";

    [ObservableProperty]
    private bool _isPack;

    [ObservableProperty]
    private string _packName = "";

    [ObservableProperty]
    private bool _isCloseable;

    public ObservableCollection<ContentItem> Items { get; } = [];
    public ObservableCollection<BreadcrumbItem> Breadcrumbs { get; } = [];
}

public partial class ContentItem : ObservableObject
{
    [ObservableProperty]
    private string _name = "";

    [ObservableProperty]
    private string _fullPath = "";

    [ObservableProperty]
    private string _relativePath = "";

    [ObservableProperty]
    private bool _isDirectory;

    [ObservableProperty]
    private AssetFileType _type;

    [ObservableProperty]
    private string _key = "";

    [ObservableProperty]
    private int _meshCount;

    [ObservableProperty]
    private int _animationCount;

    [ObservableProperty]
    private bool _hasSkeleton;

    [ObservableProperty]
    private int _boneCount;

    [ObservableProperty]
    private bool _hasModelInfo;

    public string IconData => Type switch
    {
        AssetFileType.Folder => "M10 4H4c-1.1 0-1.99.9-1.99 2L2 18c0 1.1.9 2 2 2h16c1.1 0 2-.9 2-2V8c0-1.1-.9-2-2-2h-8l-2-2z",
        AssetFileType.Model => "M12 2L2 7l10 5 10-5-10-5zM2 17l10 5 10-5M2 12l10 5 10-5",
        AssetFileType.Texture => "M21 19V5c0-1.1-.9-2-2-2H5c-1.1 0-2 .9-2 2v14c0 1.1.9 2 2 2h14c1.1 0 2-.9 2-2zM8.5 13.5l2.5 3.01L14.5 12l4.5 6H5l3.5-4.5z",
        AssetFileType.Material => "M12 22C6.49 22 2 17.51 2 12S6.49 2 12 2s10 4.04 10 9c0 3.31-2.69 6-6 6h-1.77c-.28 0-.5.22-.5.5 0 .12.05.23.13.33.41.47.64 1.06.64 1.67A2.5 2.5 0 0 1 12 22z",
        AssetFileType.Shader => "M9.4 16.6L4.8 12l4.6-4.6L8 6l-6 6 6 6 1.4-1.4zm5.2 0l4.6-4.6-4.6-4.6L16 6l6 6-6 6-1.4-1.4z",
        AssetFileType.Scene => "M12 5.69l5 4.5V18h-2v-6H9v6H7v-7.81l5-4.5M12 3L2 12h3v8h6v-6h2v6h6v-8h3L12 3z",
        AssetFileType.Pack => "M20 6h-8l-2-2H4c-1.1 0-2 .9-2 2v12c0 1.1.9 2 2 2h16c1.1 0 2-.9 2-2V8c0-1.1-.9-2-2-2zm-6 10H6v-2h8v2zm4-4H6v-2h12v2z",
        _ => "M14 2H6c-1.1 0-1.99.9-1.99 2L4 20c0 1.1.89 2 1.99 2H18c1.1 0 2-.9 2-2V8l-6-6zM6 20V4h7v5h5v11H6z"
    };

    public string TypeLabel => Type switch
    {
        AssetFileType.Model when HasModelInfo => $"Model - {MeshCount} mesh{(MeshCount != 1 ? "es" : "")}{(HasSkeleton ? $", {BoneCount} bones, {AnimationCount} anim{(AnimationCount != 1 ? "s" : "")}" : "")}",
        AssetFileType.Model => "Model",
        AssetFileType.Texture => "Texture",
        AssetFileType.Material => "Material",
        AssetFileType.Shader => "Shader",
        AssetFileType.Scene => "Scene",
        AssetFileType.Pack => "Asset Pack",
        AssetFileType.Folder => "Folder",
        _ => "File"
    };
}

public partial class ContentBrowserViewModel : ObservableObject
{
    private readonly string _assetsDirectory;
    private readonly string _packsDirectory;
    private readonly AssetFileService _fileService;

    public EditorViewModel? EditorViewModel { get; set; }
    public AssetBrowserService AssetBrowser { get; } = new();

    public ContentBrowserViewModel()
    {
        _assetsDirectory = Content.ResolvePath("");
        _packsDirectory = Path.Combine(_assetsDirectory, "Packs");
        _fileService = new AssetFileService(_assetsDirectory);

        var assetsTab = new ContentTab
        {
            Title = "Assets",
            RootPath = _assetsDirectory,
            CurrentPath = _assetsDirectory,
            IsPack = false,
            IsCloseable = false
        };
        Tabs.Add(assetsTab);
        SelectedTab = assetsTab;

        LoadFolderTree();
        RefreshCurrentTab();
    }

    public ObservableCollection<FolderTreeNode> FolderTree { get; } = [];
    public ObservableCollection<FolderTreeNode> ScenePackTree { get; } = [];
    public ObservableCollection<FolderTreeNode> AvailablePackTree { get; } = [];

    public ObservableCollection<ContentTab> Tabs { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanNavigateUp))]
    private ContentTab? _selectedTab;

    [ObservableProperty]
    private ContentItem? _selectedItem;

    [ObservableProperty]
    private FolderTreeNode? _selectedFolder;

    [ObservableProperty]
    private FolderTreeNode? _selectedPack;

    [ObservableProperty]
    private int _filterIndex;

    [ObservableProperty]
    private string _searchText = "";

    [ObservableProperty]
    private string _statusMessage = "";

    [ObservableProperty]
    private bool _isAssetEditorOpen;

    [ObservableProperty]
    private string _assetEditorTitle = "";

    [ObservableProperty]
    private ContentItem? _editingItem;

    [ObservableProperty]
    private string _editingJson = "";

    public bool CanNavigateUp => SelectedTab != null && SelectedTab.CurrentPath != SelectedTab.RootPath;

    public AssetFileType Filter
    {
        get => (AssetFileType)FilterIndex;
        set => FilterIndex = (int)value;
    }

    partial void OnSelectedTabChanged(ContentTab? value)
    {
        if (value != null)
        {
            RefreshCurrentTab();
        }
    }

    partial void OnFilterIndexChanged(int value)
    {
        RefreshCurrentTab();
    }

    partial void OnSearchTextChanged(string value)
    {
        RefreshCurrentTab();
    }

    partial void OnSelectedFolderChanged(FolderTreeNode? value)
    {
        if (value != null && SelectedTab != null)
        {
            if (value.IsPack)
            {
                SelectedPack = value;
                StatusMessage = $"Selected pack: {value.PackName}";
            }
            else
            {
                SelectedPack = null;
                NavigateTo(value.FullPath);
            }
        }
    }

    private void LoadFolderTree()
    {
        var previousPackName = SelectedPack?.PackName;

        FolderTree.Clear();
        ScenePackTree.Clear();
        AvailablePackTree.Clear();

        if (Directory.Exists(_assetsDirectory))
        {
            var root = CreateFolderNode(_assetsDirectory);
            root.Name = "Assets";
            root.IsExpanded = true;
            FolderTree.Add(root);
        }

        if (Directory.Exists(_packsDirectory))
        {
            foreach (var packFile in Directory.GetFiles(_packsDirectory, "*.nizipack.json"))
            {
                try
                {
                    var json = File.ReadAllText(packFile);
                    var pack = AssetPackJson.FromJson(json);
                    var packNode = CreatePackNode(pack, packFile);

                    if (AssetPacks.IsLoaded(pack.Name))
                    {
                        ScenePackTree.Add(packNode);
                    }
                    else
                    {
                        AvailablePackTree.Add(packNode);
                    }
                }
                catch
                {
                }
            }
        }

        if (previousPackName != null)
        {
            SelectedPack = ScenePackTree.Concat(AvailablePackTree)
                .FirstOrDefault(p => p.PackName == previousPackName);
        }
    }

    private FolderTreeNode CreatePackNode(AssetPackJson pack, string packFilePath)
    {
        var packNode = new FolderTreeNode
        {
            Name = pack.Name,
            FullPath = packFilePath,
            IsPack = true,
            PackName = pack.Name,
            IsExpanded = false
        };

        foreach (var meshPath in pack.Meshes)
        {
            packNode.Children.Add(new FolderTreeNode
            {
                Name = Path.GetFileNameWithoutExtension(meshPath),
                FullPath = Path.Combine(_assetsDirectory, meshPath),
                IsPack = false,
                PackName = pack.Name,
                AssetType = AssetFileType.Model
            });
        }

        foreach (var texturePath in pack.Textures)
        {
            packNode.Children.Add(new FolderTreeNode
            {
                Name = Path.GetFileNameWithoutExtension(texturePath),
                FullPath = Path.Combine(_assetsDirectory, texturePath),
                IsPack = false,
                PackName = pack.Name,
                AssetType = AssetFileType.Texture
            });
        }

        foreach (var materialPath in pack.Materials)
        {
            packNode.Children.Add(new FolderTreeNode
            {
                Name = Path.GetFileNameWithoutExtension(materialPath),
                FullPath = Path.Combine(_assetsDirectory, materialPath),
                IsPack = false,
                PackName = pack.Name,
                AssetType = AssetFileType.Material
            });
        }

        foreach (var shaderPath in pack.Shaders)
        {
            packNode.Children.Add(new FolderTreeNode
            {
                Name = Path.GetFileNameWithoutExtension(shaderPath),
                FullPath = Path.Combine(_assetsDirectory, shaderPath),
                IsPack = false,
                PackName = pack.Name,
                AssetType = AssetFileType.Shader
            });
        }

        return packNode;
    }

    private FolderTreeNode CreateFolderNode(string path)
    {
        var node = new FolderTreeNode
        {
            Name = Path.GetFileName(path),
            FullPath = path
        };

        try
        {
            foreach (var dir in Directory.GetDirectories(path))
            {
                if (Path.GetFileName(dir).Equals("Packs", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                node.Children.Add(CreateFolderNode(dir));
            }
        }
        catch
        {
        }

        return node;
    }

    [RelayCommand]
    public void RefreshCurrentTab()
    {
        if (SelectedTab == null)
        {
            return;
        }

        SelectedTab.Items.Clear();
        SelectedTab.Breadcrumbs.Clear();

        var pathSegments = new List<(string Name, string FullPath)>();
        var current = SelectedTab.CurrentPath;
        var root = SelectedTab.RootPath;

        while (!string.IsNullOrEmpty(current) && current.Length >= root.Length)
        {
            var name = Path.GetFileName(current);
            if (string.IsNullOrEmpty(name))
            {
                name = SelectedTab.IsPack ? SelectedTab.PackName : "Assets";
            }
            pathSegments.Add((name, current));

            if (current == root)
            {
                break;
            }

            var parent = Directory.GetParent(current);
            current = parent?.FullName ?? "";
        }

        pathSegments.Reverse();
        foreach (var (name, fullPath) in pathSegments)
        {
            SelectedTab.Breadcrumbs.Add(new BreadcrumbItem(name, fullPath));
        }

        var filter = Filter;
        var search = SearchText?.ToLowerInvariant() ?? "";

        if (!Directory.Exists(SelectedTab.CurrentPath))
        {
            return;
        }

        foreach (var dir in Directory.GetDirectories(SelectedTab.CurrentPath))
        {
            var name = Path.GetFileName(dir);
            if (!string.IsNullOrEmpty(search) && !name.ToLowerInvariant().Contains(search))
            {
                continue;
            }

            SelectedTab.Items.Add(new ContentItem
            {
                Name = name,
                FullPath = dir,
                RelativePath = GetRelativePath(dir),
                IsDirectory = true,
                Type = AssetFileType.Folder
            });
        }

        foreach (var file in Directory.GetFiles(SelectedTab.CurrentPath))
        {
            var fileType = _fileService.GetFileType(file);
            var name = Path.GetFileName(file);

            if (filter != AssetFileType.All && fileType != filter)
            {
                continue;
            }

            if (!string.IsNullOrEmpty(search) && !name.ToLowerInvariant().Contains(search))
            {
                continue;
            }

            var item = new ContentItem
            {
                Name = name,
                FullPath = file,
                RelativePath = GetRelativePath(file),
                IsDirectory = false,
                Type = fileType
            };

            if (fileType == AssetFileType.Model)
            {
                LoadModelInfo(item);
            }

            SelectedTab.Items.Add(item);
        }
    }

    private void LoadModelInfo(ContentItem item)
    {
        try
        {
            var packsType = typeof(AssetPacks);
            var packsField = packsType.GetField("_packs", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            if (packsField?.GetValue(null) is Dictionary<string, AssetPack> packs)
            {
                foreach (var pack in packs.Values)
                {
                    var mesh = pack.Meshes.Values.FirstOrDefault(m => item.FullPath.EndsWith(m.Name + ".nizimesh"));
                    if (mesh != null)
                    {
                        item.MeshCount = 1;
                        item.HasModelInfo = true;
                        return;
                    }
                }
            }
        }
        catch
        {
        }
    }

    [RelayCommand]
    public void NavigateUp()
    {
        if (SelectedTab == null || !CanNavigateUp)
        {
            return;
        }

        var parent = Directory.GetParent(SelectedTab.CurrentPath);
        if (parent != null && parent.FullName.Length >= SelectedTab.RootPath.Length)
        {
            NavigateTo(parent.FullName);
        }
    }

    [RelayCommand]
    public void NavigateTo(string path)
    {
        if (SelectedTab == null || !Directory.Exists(path))
        {
            return;
        }

        SelectedTab.CurrentPath = path;
        OnPropertyChanged(nameof(CanNavigateUp));
        RefreshCurrentTab();
    }

    [RelayCommand]
    public void HandleItemDoubleClick(ContentItem? item)
    {
        if (item == null)
        {
            return;
        }

        if (item.IsDirectory)
        {
            NavigateTo(item.FullPath);
        }
        else if (item.Type == AssetFileType.Shader)
        {
            OpenAssetEditor(item);
        }
    }

    public void OpenPack(string packName, string packPath)
    {
        var existingTab = Tabs.FirstOrDefault(t => t.IsPack && t.PackName == packName);
        if (existingTab != null)
        {
            SelectedTab = existingTab;
            return;
        }

        var newTab = new ContentTab
        {
            Title = packName,
            RootPath = packPath,
            CurrentPath = packPath,
            IsPack = true,
            PackName = packName,
            IsCloseable = true
        };

        Tabs.Add(newTab);
        SelectedTab = newTab;
    }

    [RelayCommand]
    public void CloseTab(ContentTab? tab)
    {
        if (tab == null || !tab.IsCloseable)
        {
            return;
        }

        var index = Tabs.IndexOf(tab);
        Tabs.Remove(tab);

        if (SelectedTab == tab)
        {
            SelectedTab = Tabs[Math.Max(0, index - 1)];
        }
    }

    [RelayCommand]
    public void CreateFolder()
    {
        if (SelectedTab == null)
        {
            return;
        }

        var baseName = "New Folder";
        var path = Path.Combine(SelectedTab.CurrentPath, baseName);
        var counter = 1;

        while (Directory.Exists(path))
        {
            path = Path.Combine(SelectedTab.CurrentPath, $"{baseName} {counter++}");
        }

        Directory.CreateDirectory(path);
        StatusMessage = $"Created folder: {Path.GetFileName(path)}";
        RefreshCurrentTab();
        RefreshFolderTree();
    }

    [RelayCommand]
    public void CreateShader()
    {
        if (SelectedTab == null)
        {
            return;
        }

        var baseName = "NewShader";
        var fileName = $"{baseName}.nizishp.json";
        var path = Path.Combine(SelectedTab.CurrentPath, fileName);
        var counter = 1;

        while (File.Exists(path))
        {
            fileName = $"{baseName}_{counter++}.nizishp.json";
            path = Path.Combine(SelectedTab.CurrentPath, fileName);
        }

        var schemaPath = GetRelativeSchemaPath(path, "nizishp.schema.json");
        var shader = new Dictionary<string, object>
        {
            ["$schema"] = schemaPath,
            ["name"] = Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(fileName)),
            ["type"] = "graphics",
            ["stages"] = new[]
            {
                new Dictionary<string, object> { ["stage"] = "vertex", ["path"] = "", ["entryPoint"] = "VSMain" },
                new Dictionary<string, object> { ["stage"] = "pixel", ["path"] = "", ["entryPoint"] = "PSMain" }
            },
            ["pipeline"] = new Dictionary<string, object>
            {
                ["primitiveTopology"] = "triangle",
                ["cullMode"] = "backFace",
                ["fillMode"] = "solid",
                ["depthTest"] = new Dictionary<string, object> { ["enable"] = true, ["compareOp"] = "less", ["write"] = true },
                ["blend"] = new Dictionary<string, object> { ["enable"] = false, ["renderTargetWriteMask"] = 15 }
            }
        };

        var json = JsonSerializer.Serialize(shader, NiziJsonSerializationOptions.Default);
        File.WriteAllText(path, json);
        StatusMessage = $"Created shader: {fileName}";
        RefreshCurrentTab();

        var item = SelectedTab.Items.FirstOrDefault(i => i.FullPath == path);
        if (item != null)
        {
            OpenAssetEditor(item);
        }
    }

    [RelayCommand]
    public void CreatePack()
    {
        Directory.CreateDirectory(_packsDirectory);

        var baseName = "newpack";
        var packFile = Path.Combine(_packsDirectory, $"{baseName}.nizipack.json");
        var counter = 1;

        while (File.Exists(packFile))
        {
            packFile = Path.Combine(_packsDirectory, $"{baseName}_{counter++}.nizipack.json");
        }

        var packName = Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(packFile));

        var packData = new AssetPackJson
        {
            Name = packName,
            Version = "1.0.0"
        };

        File.WriteAllText(packFile, packData.ToJson());

        var relativePath = Path.GetRelativePath(_assetsDirectory, packFile).Replace('\\', '/');
        try
        {
            AssetPack.Load(relativePath);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Created pack but failed to load: {ex.Message}";
        }

        StatusMessage = $"Created pack: {packName}";
        LoadFolderTree();
        RefreshCurrentTab();
    }

    [RelayCommand]
    public async Task RenamePack(FolderTreeNode? packNode)
    {
        if (packNode == null || !packNode.IsPack)
        {
            return;
        }

        if (!File.Exists(packNode.FullPath))
        {
            StatusMessage = "Pack manifest not found";
            return;
        }

        IsRenameDialogOpen = true;
        RenameDialogTitle = "Rename Pack";
        RenameDialogValue = packNode.PackName;
        _renamingPack = packNode;
    }

    [RelayCommand]
    public void ConfirmRename()
    {
        if (_renamingPack == null || string.IsNullOrWhiteSpace(RenameDialogValue))
        {
            IsRenameDialogOpen = false;
            return;
        }

        try
        {
            var packFile = _renamingPack.FullPath;
            var json = File.ReadAllText(packFile);
            var packData = AssetPackJson.FromJson(json);

            var newName = RenameDialogValue.Trim().ToLowerInvariant();
            var oldName = packData.Name;

            if (newName == oldName)
            {
                IsRenameDialogOpen = false;
                return;
            }

            var existingPack = ScenePackTree.Concat(AvailablePackTree).FirstOrDefault(p => p.PackName.Equals(newName, StringComparison.OrdinalIgnoreCase) && p != _renamingPack);
            if (existingPack != null)
            {
                StatusMessage = $"A pack named '{newName}' already exists";
                return;
            }

            packData.Name = newName;
            File.WriteAllText(packFile, packData.ToJson());

            var newPackFile = Path.Combine(_packsDirectory, $"{newName}.nizipack.json");
            if (packFile != newPackFile)
            {
                File.Move(packFile, newPackFile);
            }

            if (AssetPacks.IsLoaded(oldName))
            {
                AssetPacks.Unregister(oldName);
                AssetPack.Load(Path.GetRelativePath(_assetsDirectory, newPackFile).Replace('\\', '/'));
            }

            StatusMessage = $"Renamed pack '{oldName}' to '{newName}'";
            LoadFolderTree();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error renaming pack: {ex.Message}";
        }
        finally
        {
            IsRenameDialogOpen = false;
            _renamingPack = null;
        }
    }

    [RelayCommand]
    public void CancelRename()
    {
        IsRenameDialogOpen = false;
        _renamingPack = null;
    }

    private FolderTreeNode? _renamingPack;

    [ObservableProperty]
    private bool _isRenameDialogOpen;

    [ObservableProperty]
    private string _renameDialogTitle = "";

    [ObservableProperty]
    private string _renameDialogValue = "";

    [RelayCommand]
    public void DeleteSelected()
    {
        if (SelectedItem == null)
        {
            return;
        }

        try
        {
            if (SelectedItem.IsDirectory)
            {
                Directory.Delete(SelectedItem.FullPath, true);
            }
            else
            {
                File.Delete(SelectedItem.FullPath);
            }

            StatusMessage = $"Deleted: {SelectedItem.Name}";
            SelectedItem = null;
            RefreshCurrentTab();
            RefreshFolderTree();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error deleting: {ex.Message}";
        }
    }

    [RelayCommand]
    public void ShowInExplorer()
    {
        if (SelectedItem == null)
        {
            return;
        }

        try
        {
            System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{SelectedItem.FullPath}\"");
        }
        catch
        {
        }
    }

    [RelayCommand]
    public void ShowPackInExplorer(FolderTreeNode? packNode)
    {
        if (packNode == null)
        {
            return;
        }

        try
        {
            System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{packNode.FullPath}\"");
        }
        catch
        {
        }
    }

    [RelayCommand]
    public void AddPackToScene(FolderTreeNode? packNode)
    {
        if (packNode == null || !packNode.IsPack)
        {
            return;
        }

        if (AssetPacks.IsLoaded(packNode.PackName))
        {
            StatusMessage = $"Pack '{packNode.PackName}' is already in the scene";
            return;
        }

        try
        {
            var relativePath = Path.GetRelativePath(_assetsDirectory, packNode.FullPath).Replace('\\', '/');
            AssetPack.Load(relativePath);
            StatusMessage = $"Added pack '{packNode.PackName}' to scene";
            LoadFolderTree();
            RefreshCurrentTab();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error adding pack: {ex.Message}";
        }
    }

    [RelayCommand]
    public void RemovePackFromScene(FolderTreeNode? packNode)
    {
        if (packNode == null || !packNode.IsPack)
        {
            return;
        }

        if (!AssetPacks.IsLoaded(packNode.PackName))
        {
            StatusMessage = $"Pack '{packNode.PackName}' is not in the scene";
            return;
        }

        try
        {
            AssetPacks.Unregister(packNode.PackName);
            StatusMessage = $"Removed pack '{packNode.PackName}' from scene";
            LoadFolderTree();
            RefreshCurrentTab();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error removing pack: {ex.Message}";
        }
    }

    [RelayCommand]
    public void RefreshFolderTree()
    {
        LoadFolderTree();
    }

    public bool CanAddToSelectedPack => SelectedPack != null && SelectedItem != null && !SelectedItem.IsDirectory && SelectedItem.Type != AssetFileType.Pack;

    [RelayCommand]
    public void AddToSelectedPack()
    {
        if (SelectedPack == null || SelectedItem == null)
        {
            StatusMessage = "No pack or item selected";
            return;
        }

        if (SelectedItem.IsDirectory || SelectedItem.Type == AssetFileType.Pack)
        {
            StatusMessage = "Cannot add folders or packs to a pack";
            return;
        }

        try
        {
            var packFile = SelectedPack.FullPath;
            if (!File.Exists(packFile))
            {
                StatusMessage = "Pack manifest not found";
                return;
            }

            var json = File.ReadAllText(packFile);
            var packData = AssetPackJson.FromJson(json);

            var relativePath = Path.GetRelativePath(_assetsDirectory, SelectedItem.FullPath).Replace('\\', '/');
            var assetKey = Path.GetFileNameWithoutExtension(SelectedItem.Name);
            if (assetKey.EndsWith(".nizimat") || assetKey.EndsWith(".nizishp"))
            {
                assetKey = Path.GetFileNameWithoutExtension(assetKey);
            }

            switch (SelectedItem.Type)
            {
                case AssetFileType.Model:
                    if (packData.Meshes.Contains(relativePath))
                    {
                        StatusMessage = $"Model '{assetKey}' already exists in pack";
                        return;
                    }
                    packData.Meshes.Add(relativePath);
                    break;

                case AssetFileType.Texture:
                    if (packData.Textures.Contains(relativePath))
                    {
                        StatusMessage = $"Texture '{assetKey}' already exists in pack";
                        return;
                    }
                    packData.Textures.Add(relativePath);
                    break;

                case AssetFileType.Material:
                    if (packData.Materials.Contains(relativePath))
                    {
                        StatusMessage = $"Material '{assetKey}' already exists in pack";
                        return;
                    }
                    packData.Materials.Add(relativePath);
                    break;

                case AssetFileType.Shader:
                    if (packData.Shaders.Contains(relativePath))
                    {
                        StatusMessage = $"Shader '{assetKey}' already exists in pack";
                        return;
                    }
                    packData.Shaders.Add(relativePath);
                    break;

                default:
                    StatusMessage = $"Cannot add {SelectedItem.Type} to pack";
                    return;
            }

            NormalizePackPaths(packData);
            File.WriteAllText(packFile, packData.ToJson());

            if (AssetPacks.IsLoaded(SelectedPack.PackName))
            {
                AssetPacks.Reload(SelectedPack.PackName);
            }

            StatusMessage = $"Added '{assetKey}' to pack '{SelectedPack.PackName}'";
            LoadFolderTree();
            RefreshCurrentTab();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error adding to pack: {ex.Message}";
        }
    }

    [RelayCommand]
    public void RemoveFromPack(ContentItem? item)
    {
        if (SelectedPack == null || item == null)
        {
            return;
        }

        try
        {
            var packFile = SelectedPack.FullPath;
            if (!File.Exists(packFile))
            {
                StatusMessage = "Pack manifest not found";
                return;
            }

            var json = File.ReadAllText(packFile);
            var packData = AssetPackJson.FromJson(json);

            var relativePath = GetRelativePath(item.FullPath) ?? item.Name;
            var assetKey = Path.GetFileNameWithoutExtension(item.Name);
            if (assetKey.EndsWith(".nizimat") || assetKey.EndsWith(".nizishp"))
            {
                assetKey = Path.GetFileNameWithoutExtension(assetKey);
            }

            var removed = false;
            switch (item.Type)
            {
                case AssetFileType.Model:
                    removed = packData.Meshes.Remove(relativePath);
                    break;
                case AssetFileType.Texture:
                    removed = packData.Textures.Remove(relativePath);
                    break;
                case AssetFileType.Material:
                    removed = packData.Materials.Remove(relativePath);
                    break;
                case AssetFileType.Shader:
                    removed = packData.Shaders.Remove(relativePath);
                    break;
            }

            if (removed)
            {
                NormalizePackPaths(packData);
                File.WriteAllText(packFile, packData.ToJson());

                if (AssetPacks.IsLoaded(SelectedPack.PackName))
                {
                    AssetPacks.Reload(SelectedPack.PackName);
                }

                StatusMessage = $"Removed '{assetKey}' from pack '{SelectedPack.PackName}'";
                LoadFolderTree();
            }
            else
            {
                StatusMessage = $"'{assetKey}' not found in pack";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error removing from pack: {ex.Message}";
        }
    }

    public void OpenAssetEditor(ContentItem item)
    {
        if (!File.Exists(item.FullPath))
        {
            StatusMessage = $"File not found: {item.Name}";
            return;
        }

        try
        {
            EditingItem = item;
            EditingJson = File.ReadAllText(item.FullPath);
            AssetEditorTitle = $"Edit {item.Type}: {item.Name}";
            IsAssetEditorOpen = true;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error opening file: {ex.Message}";
        }
    }

    [RelayCommand]
    public void CloseAssetEditor()
    {
        IsAssetEditorOpen = false;
        EditingItem = null;
        EditingJson = "";
    }

    [RelayCommand]
    public void SaveAssetEditor()
    {
        if (EditingItem == null)
        {
            return;
        }

        try
        {
            JsonDocument.Parse(EditingJson);

            File.WriteAllText(EditingItem.FullPath, EditingJson);
            StatusMessage = $"Saved: {EditingItem.Name}";
            IsAssetEditorOpen = false;
            EditingItem = null;
            EditingJson = "";
        }
        catch (JsonException ex)
        {
            StatusMessage = $"Invalid JSON: {ex.Message}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error saving: {ex.Message}";
        }
    }

    private string GetRelativePath(string fullPath)
    {
        return Path.GetRelativePath(_assetsDirectory, fullPath);
    }

    private string GetRelativeSchemaPath(string filePath, string schemaFileName)
    {
        var fileDir = Path.GetDirectoryName(filePath) ?? _assetsDirectory;
        var schemaDir = Path.GetFullPath(Path.Combine(_assetsDirectory, "..", "NiziKit", "Assets", "Serde", "Schemas"));
        var schemaPath = Path.Combine(schemaDir, schemaFileName);
        var relativePath = Path.GetRelativePath(fileDir, schemaPath).Replace('\\', '/');
        return relativePath;
    }

    private static void NormalizePackPaths(AssetPackJson pack)
    {
        NormalizePathList(pack.Meshes);
        NormalizePathList(pack.Textures);
        NormalizePathList(pack.Materials);
        NormalizePathList(pack.Shaders);
    }

    private static void NormalizePathList(List<string> paths)
    {
        for (var i = 0; i < paths.Count; i++)
        {
            paths[i] = paths[i].Replace('\\', '/');
        }
    }
}
