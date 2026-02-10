using NiziKit.ContentPipeline;
using NiziKit.Editor.Services;

namespace NiziKit.Editor.ViewModels;

public class AssetBrowserViewModel
{
    public AssetBrowserViewModel()
    {
        var assetsPath = Content.ResolvePath("");
        FileBrowser = new FileBrowserViewModel(assetsPath);
        FileBrowser.FileDoubleClicked += OnFileDoubleClicked;
        FileBrowser.FileSelected += OnFileSelected;
    }

    public FileBrowserViewModel FileBrowser { get; }

    public FileEntry? SelectedAsset { get; set; }

    public event Action<FileEntry>? AssetDoubleClicked;

    private void OnFileDoubleClicked(FileEntry entry)
    {
        AssetDoubleClicked?.Invoke(entry);
    }

    private void OnFileSelected(FileEntry entry)
    {
        SelectedAsset = entry;
    }

    public void Refresh()
    {
        FileBrowser.Refresh();
    }
}
