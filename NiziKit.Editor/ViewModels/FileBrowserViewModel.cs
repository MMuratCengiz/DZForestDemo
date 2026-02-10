using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NiziKit.Editor.Services;

namespace NiziKit.Editor.ViewModels;

public partial class FileBrowserViewModel : ObservableObject
{
    private readonly AssetFileService _fileService;

    public FileBrowserViewModel() : this(new AssetFileService())
    {
    }

    public FileBrowserViewModel(AssetFileService fileService)
    {
        _fileService = fileService;
        _rootPath = _fileService.AssetsPath;
        _currentPath = _rootPath;
        Refresh();
    }

    public FileBrowserViewModel(string rootPath)
    {
        _fileService = new AssetFileService(rootPath);
        _rootPath = rootPath;
        _currentPath = rootPath;
        Refresh();
    }

    [ObservableProperty]
    private string _rootPath;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(BreadcrumbParts))]
    [NotifyPropertyChangedFor(nameof(CanNavigateUp))]
    private string _currentPath;

    [ObservableProperty]
    private ObservableCollection<FileEntry> _entries = [];

    [ObservableProperty]
    private FileEntry? _selectedEntry;

    [ObservableProperty]
    private ObservableCollection<FileEntry> _selectedEntries = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FilterIndex))]
    private AssetFileType _filter = AssetFileType.All;

    public int FilterIndex
    {
        get => (int)Filter;
        set
        {
            if (value is >= 0 and <= 5)
            {
                Filter = (AssetFileType)value;
            }
        }
    }

    public bool CanNavigateUp
    {
        get
        {
            if (string.IsNullOrEmpty(CurrentPath))
            {
                return false;
            }

            var parent = Directory.GetParent(CurrentPath);
            return parent != null;
        }
    }

    public IReadOnlyList<BreadcrumbItem> BreadcrumbParts
    {
        get
        {
            var parts = new List<BreadcrumbItem>();

            if (string.IsNullOrEmpty(CurrentPath))
            {
                return parts;
            }

            var pathSegments = new List<(string Name, string FullPath)>();
            var current = CurrentPath;

            while (!string.IsNullOrEmpty(current))
            {
                var name = Path.GetFileName(current);
                if (string.IsNullOrEmpty(name))
                {
                    name = current;
                }
                pathSegments.Add((name, current));

                var parent = Directory.GetParent(current);
                current = parent?.FullName;
            }

            pathSegments.Reverse();

            foreach (var (name, fullPath) in pathSegments)
            {
                parts.Add(new BreadcrumbItem(name, fullPath));
            }

            return parts;
        }
    }

    public event Action<FileEntry>? FileDoubleClicked;
    public event Action<FileEntry>? FileSelected;
    public event Action<IReadOnlyList<FileEntry>>? SelectionChanged;

    partial void OnFilterChanged(AssetFileType value)
    {
        Refresh();
    }

    [RelayCommand]
    public void NavigateUp()
    {
        if (!CanNavigateUp)
        {
            return;
        }

        var parent = Directory.GetParent(CurrentPath);
        if (parent != null)
        {
            NavigateTo(parent.FullName);
        }
    }

    [RelayCommand]
    public void NavigateTo(string path)
    {
        if (!Directory.Exists(path))
        {
            return;
        }

        CurrentPath = path;
        Refresh();
    }

    [RelayCommand]
    public void Refresh()
    {
        Entries.Clear();
        SelectedEntry = null;
        SelectedEntries.Clear();

        var entries = _fileService.GetEntries(CurrentPath, Filter);
        foreach (var entry in entries)
        {
            Entries.Add(entry);
        }
    }

    public void HandleDoubleClick(FileEntry entry)
    {
        if (entry.IsDirectory)
        {
            NavigateTo(entry.FullPath);
        }
        else
        {
            FileDoubleClicked?.Invoke(entry);
        }
    }

    public void HandleSelection(FileEntry entry)
    {
        SelectedEntry = entry;
        FileSelected?.Invoke(entry);
    }

    public void HandleMultiSelection(IReadOnlyList<FileEntry> entries)
    {
        SelectedEntries.Clear();
        foreach (var entry in entries)
        {
            SelectedEntries.Add(entry);
        }
        SelectedEntry = entries.Count > 0 ? entries[0] : null;
        SelectionChanged?.Invoke(entries);
    }

    public void SetRootPath(string path)
    {
        if (!Directory.Exists(path))
        {
            return;
        }

        RootPath = path;
        CurrentPath = path;
        Refresh();
    }
}

public record BreadcrumbItem(string Name, string Path);
