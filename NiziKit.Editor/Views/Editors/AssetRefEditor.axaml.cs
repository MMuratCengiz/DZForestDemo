using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using NiziKit.Components;
using NiziKit.Editor.Services;

namespace NiziKit.Editor.Views.Editors;

public partial class AssetRefEditor : UserControl
{
    private Border? _packSelectorHost;
    private Border? _packSelectorPanel;
    private TextBlock? _packDisplayText;
    private TextBox? _packSearchBox;
    private ItemsControl? _packItemsControl;

    private Border? _assetSelectorHost;
    private Border? _assetSelectorPanel;
    private TextBlock? _assetDisplayText;
    private TextBox? _assetSearchBox;
    private ItemsControl? _assetItemsControl;

    private bool _isUpdating;
    private IReadOnlyList<string> _allPacks = Array.Empty<string>();
    private IReadOnlyList<AssetInfo> _allAssets = Array.Empty<AssetInfo>();

    public AssetRefType AssetType { get; set; }
    public AssetBrowserService? AssetBrowser { get; set; }
    public object? CurrentAsset { get; set; }
    public bool IsReadOnly { get; set; }
    public Action<object?>? OnAssetChanged { get; set; }

    private string? _selectedPack;
    private string? _selectedAssetName;

    public AssetRefEditor()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);

        // Pack selector elements
        _packSelectorHost = this.FindControl<Border>("PackSelectorHost");
        _packSelectorPanel = this.FindControl<Border>("PackSelectorPanel");
        _packDisplayText = this.FindControl<TextBlock>("PackDisplayText");
        _packSearchBox = this.FindControl<TextBox>("PackSearchBox");
        _packItemsControl = this.FindControl<ItemsControl>("PackItemsControl");

        // Asset selector elements
        _assetSelectorHost = this.FindControl<Border>("AssetSelectorHost");
        _assetSelectorPanel = this.FindControl<Border>("AssetSelectorPanel");
        _assetDisplayText = this.FindControl<TextBlock>("AssetDisplayText");
        _assetSearchBox = this.FindControl<TextBox>("AssetSearchBox");
        _assetItemsControl = this.FindControl<ItemsControl>("AssetItemsControl");

        // Wire up click handlers for opening selectors
        if (_packSelectorHost != null)
        {
            _packSelectorHost.PointerPressed += OnPackSelectorHostPressed;
        }
        if (_assetSelectorHost != null)
        {
            _assetSelectorHost.PointerPressed += OnAssetSelectorHostPressed;
        }

        // Wire up search box handlers
        if (_packSearchBox != null)
        {
            _packSearchBox.TextChanged += OnPackSearchTextChanged;
        }
        if (_assetSearchBox != null)
        {
            _assetSearchBox.TextChanged += OnAssetSearchTextChanged;
        }
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();
        // Parse current asset FIRST to get the name before loading packs
        ParseCurrentAsset();
        // Now load packs (this will use _selectedAssetName to find the correct pack)
        LoadPacks();
        UpdateReadOnly();
    }

    private void OnPackSelectorHostPressed(object? sender, PointerPressedEventArgs e)
    {
        if (IsReadOnly) return;

        // Toggle pack selector panel
        if (_packSelectorPanel != null)
        {
            _packSelectorPanel.IsVisible = !_packSelectorPanel.IsVisible;
            if (_packSelectorPanel.IsVisible)
            {
                // Close asset panel if open
                if (_assetSelectorPanel != null) _assetSelectorPanel.IsVisible = false;
                // Focus search box
                _packSearchBox?.Focus();
                // Reset search
                if (_packSearchBox != null) _packSearchBox.Text = "";
                FilterPacks("");
            }
        }
    }

    private void OnAssetSelectorHostPressed(object? sender, PointerPressedEventArgs e)
    {
        if (IsReadOnly) return;

        // Toggle asset selector panel
        if (_assetSelectorPanel != null)
        {
            _assetSelectorPanel.IsVisible = !_assetSelectorPanel.IsVisible;
            if (_assetSelectorPanel.IsVisible)
            {
                // Close pack panel if open
                if (_packSelectorPanel != null) _packSelectorPanel.IsVisible = false;
                // Focus search box
                _assetSearchBox?.Focus();
                // Reset search
                if (_assetSearchBox != null) _assetSearchBox.Text = "";
                FilterAssets("");
            }
        }
    }

    private void OnPackSearchTextChanged(object? sender, TextChangedEventArgs e)
    {
        FilterPacks(_packSearchBox?.Text ?? "");
    }

    private void OnAssetSearchTextChanged(object? sender, TextChangedEventArgs e)
    {
        FilterAssets(_assetSearchBox?.Text ?? "");
    }

    private void FilterPacks(string searchText)
    {
        if (_packItemsControl == null) return;

        IEnumerable<string> filtered = string.IsNullOrWhiteSpace(searchText)
            ? _allPacks
            : _allPacks.Where(p => p.Contains(searchText, StringComparison.OrdinalIgnoreCase));

        BuildPackItems(filtered);
    }

    private void FilterAssets(string searchText)
    {
        if (_assetItemsControl == null) return;

        IEnumerable<AssetInfo> filtered = string.IsNullOrWhiteSpace(searchText)
            ? _allAssets
            : _allAssets.Where(a => a.Name.Contains(searchText, StringComparison.OrdinalIgnoreCase));

        BuildAssetItems(filtered);
    }

    private void BuildPackItems(IEnumerable<string> packs)
    {
        if (_packItemsControl == null) return;

        var items = new List<Button>();
        foreach (var pack in packs)
        {
            var button = new Button
            {
                Content = pack,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch,
                HorizontalContentAlignment = Avalonia.Layout.HorizontalAlignment.Left,
                Padding = new Thickness(8, 6),
                Background = Avalonia.Media.Brushes.Transparent,
                Foreground = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#EEEEEE")),
                BorderThickness = new Thickness(0),
                Tag = pack
            };
            button.Click += OnPackItemClicked;
            items.Add(button);
        }
        _packItemsControl.ItemsSource = items;
    }

    private void BuildAssetItems(IEnumerable<AssetInfo> assets)
    {
        if (_assetItemsControl == null) return;

        var items = new List<Button>();
        foreach (var asset in assets)
        {
            var button = new Button
            {
                Content = asset.Name,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch,
                HorizontalContentAlignment = Avalonia.Layout.HorizontalAlignment.Left,
                Padding = new Thickness(8, 6),
                Background = Avalonia.Media.Brushes.Transparent,
                Foreground = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#EEEEEE")),
                BorderThickness = new Thickness(0),
                Tag = asset
            };
            button.Click += OnAssetItemClicked;
            items.Add(button);
        }
        _assetItemsControl.ItemsSource = items;
    }

    private void OnPackItemClicked(object? sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string pack)
        {
            SelectPack(pack);
            if (_packSelectorPanel != null) _packSelectorPanel.IsVisible = false;
        }
    }

    private void OnAssetItemClicked(object? sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is AssetInfo assetInfo)
        {
            SelectAsset(assetInfo);
            if (_assetSelectorPanel != null) _assetSelectorPanel.IsVisible = false;
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
        if (AssetBrowser == null) return;

        _isUpdating = true;
        try
        {
            _allPacks = AssetBrowser.GetLoadedPacks();

            if (!string.IsNullOrEmpty(_selectedPack) && _allPacks.Contains(_selectedPack))
            {
                if (_packDisplayText != null) _packDisplayText.Text = _selectedPack;
            }
            else if (!string.IsNullOrEmpty(_selectedAssetName))
            {
                // Find which pack contains this asset
                var foundPack = FindPackContainingAsset(_selectedAssetName);
                if (foundPack != null)
                {
                    _selectedPack = foundPack;
                    if (_packDisplayText != null) _packDisplayText.Text = _selectedPack;
                }
                else if (_allPacks.Count > 0)
                {
                    _selectedPack = _allPacks[0];
                    if (_packDisplayText != null) _packDisplayText.Text = _selectedPack;
                }
            }
            else if (_allPacks.Count > 0)
            {
                _selectedPack = _allPacks[0];
                if (_packDisplayText != null) _packDisplayText.Text = _selectedPack;
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
        if (AssetBrowser == null) return null;

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
        if (CurrentAsset == null) return;

        var assetType = CurrentAsset.GetType();
        var nameProperty = assetType.GetProperty("Name");
        if (nameProperty != null)
        {
            _selectedAssetName = nameProperty.GetValue(CurrentAsset)?.ToString();
        }
    }

    private void LoadAssetsForPack()
    {
        if (AssetBrowser == null || string.IsNullOrEmpty(_selectedPack)) return;

        _isUpdating = true;
        try
        {
            _allAssets = AssetBrowser.GetAssetsOfType(AssetType, _selectedPack);

            // Try to find and select the current asset
            if (!string.IsNullOrEmpty(_selectedAssetName))
            {
                var match = _allAssets.FirstOrDefault(a => a.Name == _selectedAssetName);
                if (_assetDisplayText != null)
                {
                    // Show the asset name if found in this pack, otherwise show the name anyway
                    // (it might be from a different pack or unresolved)
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
        if (AssetBrowser == null) return;

        var resolvedAsset = AssetBrowser.ResolveAsset(AssetType, assetInfo.FullReference);
        OnAssetChanged?.Invoke(resolvedAsset);
    }

    private void UpdateReadOnly()
    {
        var opacity = IsReadOnly ? 0.5 : 1.0;
        if (_packSelectorHost != null) _packSelectorHost.Opacity = opacity;
        if (_assetSelectorHost != null) _assetSelectorHost.Opacity = opacity;
    }
}
