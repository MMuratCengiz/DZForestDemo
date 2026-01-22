using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using NiziKit.Components;
using NiziKit.Editor.Services;
using NiziKit.Editor.ViewModels;

namespace NiziKit.Editor.Views.Editors;

public partial class AssetRefEditor : UserControl
{
    private Border? _packSelectorHost;
    private Border? _assetSelectorHost;
    private TextBlock? _packDisplayText;
    private TextBlock? _assetDisplayText;

    private Popup? _packSelectorPopup;
    private TextBox? _packSearchBox;
    private ItemsControl? _packItemsControl;

    private bool _isUpdating;
    private IReadOnlyList<string> _allPacks = [];
    private IReadOnlyList<AssetInfo> _allAssets = [];

    public AssetRefType AssetType { get; set; }
    public AssetBrowserService? AssetBrowser { get; set; }
    public EditorViewModel? EditorViewModel { get; set; }
    public object? CurrentAsset { get; set; }
    public bool IsReadOnly { get; set; }
    public Action<object?, string?>? OnAssetChanged { get; set; }

    private string? _selectedPack;
    private string? _selectedAssetName;

    public AssetRefEditor()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);

        _packSelectorHost = this.FindControl<Border>("PackSelectorHost");
        _assetSelectorHost = this.FindControl<Border>("AssetSelectorHost");
        _packDisplayText = this.FindControl<TextBlock>("PackDisplayText");
        _assetDisplayText = this.FindControl<TextBlock>("AssetDisplayText");

        _packSelectorPopup = this.FindControl<Popup>("PackSelectorPopup");
        _packSearchBox = this.FindControl<TextBox>("PackSearchBox");
        _packItemsControl = this.FindControl<ItemsControl>("PackItemsControl");

        if (_packSelectorHost != null)
        {
            _packSelectorHost.PointerPressed += OnPackSelectorHostPressed;
        }
        if (_assetSelectorHost != null)
        {
            _assetSelectorHost.PointerPressed += OnAssetSelectorHostPressed;
        }
        if (_packSearchBox != null)
        {
            _packSearchBox.TextChanged += OnPackSearchTextChanged;
        }
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();
        ParseCurrentAsset();
        LoadPacks();
        UpdateReadOnly();
    }

    private void OnPackSelectorHostPressed(object? sender, PointerPressedEventArgs e)
    {
        if (IsReadOnly)
        {
            return;
        }

        if (_packSelectorPopup != null)
        {
            _packSelectorPopup.PlacementTarget = _packSelectorHost;
            _packSelectorPopup.IsOpen = true;

            if (_packSearchBox != null)
            {
                _packSearchBox.Text = "";
                _packSearchBox.Focus();
            }
            FilterPacks("");
        }
    }

    private void OnAssetSelectorHostPressed(object? sender, PointerPressedEventArgs e)
    {
        if (IsReadOnly || EditorViewModel == null)
        {
            return;
        }

        EditorViewModel.OpenAssetPicker(AssetType, _selectedPack, _selectedAssetName, asset =>
        {
            if (asset != null)
            {
                SelectAsset(asset);
            }
        });
    }

    private void OnPackSearchTextChanged(object? sender, TextChangedEventArgs e)
    {
        FilterPacks(_packSearchBox?.Text ?? "");
    }

    private void FilterPacks(string searchText)
    {
        if (_packItemsControl == null)
        {
            return;
        }

        IEnumerable<string> filtered = string.IsNullOrWhiteSpace(searchText)
            ? _allPacks
            : _allPacks.Where(p => p.Contains(searchText, StringComparison.OrdinalIgnoreCase));

        BuildPackItems(filtered);
    }

    private void BuildPackItems(IEnumerable<string> packs)
    {
        if (_packItemsControl == null)
        {
            return;
        }

        var textPrimaryBrush = GetResourceBrush("TextPrimaryBrush") ?? Brushes.White;

        var items = new List<Button>();
        foreach (var pack in packs)
        {
            var button = new Button
            {
                Content = pack,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                HorizontalContentAlignment = HorizontalAlignment.Left,
                Padding = new Thickness(8, 6),
                Background = Brushes.Transparent,
                Foreground = textPrimaryBrush,
                BorderThickness = new Thickness(0),
                Tag = pack
            };
            button.Click += OnPackItemClicked;
            items.Add(button);
        }
        _packItemsControl.ItemsSource = items;
    }

    private IBrush? GetResourceBrush(string key)
    {
        if (this.TryFindResource(key, this.ActualThemeVariant, out var resource))
        {
            if (resource is IBrush brush)
            {
                return brush;
            }
            if (resource is Color color)
            {
                return new SolidColorBrush(color);
            }
        }
        return null;
    }

    private void OnPackItemClicked(object? sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string pack)
        {
            SelectPack(pack);
            if (_packSelectorPopup != null)
            {
                _packSelectorPopup.IsOpen = false;
            }
        }
    }

    private void SelectPack(string pack)
    {
        _selectedPack = pack;
        if (_packDisplayText != null)
        {
            _packDisplayText.Text = pack;
        }
        LoadAssetsForPack();
    }

    private void SelectAsset(AssetInfo assetInfo)
    {
        _selectedAssetName = assetInfo.Name;
        if (_assetDisplayText != null)
        {
            _assetDisplayText.Text = assetInfo.Name;
        }
        if (!_isUpdating && !IsReadOnly)
        {
            ResolveAndNotify(assetInfo);
        }
    }

    private void LoadPacks()
    {
        if (AssetBrowser == null)
        {
            return;
        }

        _isUpdating = true;
        try
        {
            _allPacks = AssetBrowser.GetLoadedPacks();

            if (!string.IsNullOrEmpty(_selectedPack) && _allPacks.Contains(_selectedPack))
            {
                if (_packDisplayText != null)
                {
                    _packDisplayText.Text = _selectedPack;
                }
            }
            else if (!string.IsNullOrEmpty(_selectedAssetName))
            {
                var foundPack = FindPackContainingAsset(_selectedAssetName);
                if (foundPack != null)
                {
                    _selectedPack = foundPack;
                    if (_packDisplayText != null)
                    {
                        _packDisplayText.Text = _selectedPack;
                    }
                }
                else if (_allPacks.Count > 0)
                {
                    _selectedPack = _allPacks[0];
                    if (_packDisplayText != null)
                    {
                        _packDisplayText.Text = _selectedPack;
                    }
                }
            }
            else if (_allPacks.Count > 0)
            {
                _selectedPack = _allPacks[0];
                if (_packDisplayText != null)
                {
                    _packDisplayText.Text = _selectedPack;
                }
            }

            LoadAssetsForPack();
        }
        finally
        {
            _isUpdating = false;
        }
    }

    private string? FindPackContainingAsset(string assetName)
    {
        if (AssetBrowser == null)
        {
            return null;
        }

        foreach (var packName in _allPacks)
        {
            var assets = AssetBrowser.GetAssetsOfType(AssetType, packName);
            if (assets.Any(a => a.Name == assetName))
            {
                return packName;
            }
        }
        return null;
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
            _selectedAssetName = nameProperty.GetValue(CurrentAsset)?.ToString();
        }
    }

    private void LoadAssetsForPack()
    {
        if (AssetBrowser == null || string.IsNullOrEmpty(_selectedPack))
        {
            return;
        }

        _isUpdating = true;
        try
        {
            _allAssets = AssetBrowser.GetAssetsOfType(AssetType, _selectedPack);

            if (!string.IsNullOrEmpty(_selectedAssetName))
            {
                var match = _allAssets.FirstOrDefault(a => a.Name == _selectedAssetName);
                if (_assetDisplayText != null)
                {
                    _assetDisplayText.Text = match?.Name ?? _selectedAssetName;
                }
            }
            else if (_assetDisplayText != null)
            {
                _assetDisplayText.Text = "(None)";
            }
        }
        finally
        {
            _isUpdating = false;
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
        var opacity = IsReadOnly ? 0.5 : 1.0;
        if (_packSelectorHost != null)
        {
            _packSelectorHost.Opacity = opacity;
        }
        if (_assetSelectorHost != null)
        {
            _assetSelectorHost.Opacity = opacity;
        }
    }
}
