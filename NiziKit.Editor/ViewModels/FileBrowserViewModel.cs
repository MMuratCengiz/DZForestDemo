using NiziKit.Editor.Services;

namespace NiziKit.Editor.ViewModels;

public class FileBrowserViewModel
{
    private readonly AssetFileService _fileService;

    public FileBrowserViewModel() : this(new AssetFileService())
    {
    }

    public FileBrowserViewModel(AssetFileService fileService)
    {
        _fileService = fileService;
        RootPath = _fileService.AssetsPath;
        CurrentPath = RootPath;
        Refresh();
    }

    public FileBrowserViewModel(string rootPath)
    {
        _fileService = new AssetFileService(rootPath);
        RootPath = rootPath;
        CurrentPath = rootPath;
        Refresh();
    }

    public string RootPath { get; set; }
    public string CurrentPath { get; set; }
    public List<FileEntry> Entries { get; set; } = [];
    public FileEntry? SelectedEntry { get; set; }
    public List<FileEntry> SelectedEntries { get; set; } = [];
    public AssetFileType Filter { get; set; } = AssetFileType.All;

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

    public void NavigateTo(string path)
    {
        if (!Directory.Exists(path))
        {
            return;
        }

        CurrentPath = path;
        Refresh();
    }

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
