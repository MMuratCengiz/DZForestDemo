using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NiziKit.ContentPipeline;
using NiziKit.Editor.Services;

namespace NiziKit.Editor.ViewModels;

public partial class AssetBrowserViewModel : ObservableObject
{
    public AssetBrowserViewModel()
    {
        var assetsPath = Content.ResolvePath("");
        FileBrowser = new FileBrowserViewModel(assetsPath);
        FileBrowser.FileDoubleClicked += OnFileDoubleClicked;
        FileBrowser.FileSelected += OnFileSelected;
    }

    public FileBrowserViewModel FileBrowser { get; }

    [ObservableProperty]
    private FileEntry? _selectedAsset;

    public event Action<FileEntry>? AssetDoubleClicked;

    private void OnFileDoubleClicked(FileEntry entry)
    {
        AssetDoubleClicked?.Invoke(entry);
    }

    private void OnFileSelected(FileEntry entry)
    {
        SelectedAsset = entry;
    }

    [RelayCommand]
    public void Refresh()
    {
        FileBrowser.Refresh();
    }
}
