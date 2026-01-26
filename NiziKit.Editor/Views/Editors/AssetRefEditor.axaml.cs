using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using NiziKit.Components;
using NiziKit.Editor.Services;
using NiziKit.Editor.ViewModels;

namespace NiziKit.Editor.Views.Editors;

public partial class AssetRefEditor : UserControl
{
    private Border? _assetSelectorHost;
    private TextBlock? _assetDisplayText;

    private bool _isUpdating;
    private string? _selectedAssetPath;

    public AssetRefType AssetType { get; set; }
    public AssetBrowserService? AssetBrowser { get; set; }
    public EditorViewModel? EditorViewModel { get; set; }
    public object? CurrentAsset { get; set; }
    public bool IsReadOnly { get; set; }
    public Action<object?, string?>? OnAssetChanged { get; set; }

    public AssetRefEditor()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);

        _assetSelectorHost = this.FindControl<Border>("AssetSelectorHost");
        _assetDisplayText = this.FindControl<TextBlock>("AssetDisplayText");

        if (_assetSelectorHost != null)
        {
            _assetSelectorHost.PointerPressed += OnAssetSelectorHostPressed;
        }
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();
        ParseCurrentAsset();
        UpdateDisplay();
        UpdateReadOnly();
    }

    private void OnAssetSelectorHostPressed(object? sender, PointerPressedEventArgs e)
    {
        if (IsReadOnly || EditorViewModel == null)
        {
            return;
        }

        EditorViewModel.OpenAssetPicker(AssetType, _selectedAssetPath, asset =>
        {
            if (asset != null)
            {
                SelectAsset(asset);
            }
        });
    }

    private void SelectAsset(AssetInfo assetInfo)
    {
        _selectedAssetPath = assetInfo.Path;
        UpdateDisplay();
        if (!_isUpdating && !IsReadOnly)
        {
            ResolveAndNotify(assetInfo);
        }
    }

    private void ParseCurrentAsset()
    {
        if (CurrentAsset == null)
        {
            return;
        }

        var assetType = CurrentAsset.GetType();
        var nameProperty = assetType.GetProperty("Name");
        if (nameProperty != null)
        {
            _selectedAssetPath = nameProperty.GetValue(CurrentAsset)?.ToString();
        }
    }

    private void UpdateDisplay()
    {
        if (_assetDisplayText == null)
        {
            return;
        }

        if (string.IsNullOrEmpty(_selectedAssetPath))
        {
            _assetDisplayText.Text = "(None)";
        }
        else
        {
            var fileName = System.IO.Path.GetFileName(_selectedAssetPath);
            _assetDisplayText.Text = fileName;
            ToolTip.SetTip(_assetDisplayText, _selectedAssetPath);
        }
    }

    private void ResolveAndNotify(AssetInfo assetInfo)
    {
        if (AssetBrowser == null)
        {
            return;
        }

        var resolvedAsset = AssetBrowser.ResolveAsset(AssetType, assetInfo.FullReference);
        OnAssetChanged?.Invoke(resolvedAsset, assetInfo.FullReference);
    }

    private void UpdateReadOnly()
    {
        if (_assetSelectorHost != null)
        {
            _assetSelectorHost.Opacity = IsReadOnly ? 0.5 : 1.0;
        }
    }
}
