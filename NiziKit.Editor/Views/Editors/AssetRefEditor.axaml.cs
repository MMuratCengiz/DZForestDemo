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

namespace NiziKit.Editor.Views.Editors;

public partial class AssetRefEditor : UserControl
{
    private Border? _packSelectorHost;
    private Border? _assetSelectorHost;
    private TextBlock? _packDisplayText;
    private TextBlock? _assetDisplayText;

    private Popup? _selectorPopup;
    private TextBox? _searchBox;
    private ItemsControl? _itemsControl;

    private bool _isUpdating;
    private bool _isSelectingPack;
    private IReadOnlyList<string> _allPacks = Array.Empty<string>();
    private IReadOnlyList<AssetInfo> _allAssets = Array.Empty<AssetInfo>();

    public AssetRefType AssetType { get; set; }
    public AssetBrowserService? AssetBrowser { get; set; }
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

        _selectorPopup = this.FindControl<Popup>("SelectorPopup");
        _searchBox = this.FindControl<TextBox>("SearchBox");
        _itemsControl = this.FindControl<ItemsControl>("ItemsControl");

        if (_packSelectorHost != null)
        {
            _packSelectorHost.PointerPressed += OnPackSelectorHostPressed;
        }
        if (_assetSelectorHost != null)
        {
            _assetSelectorHost.PointerPressed += OnAssetSelectorHostPressed;
        }
        if (_searchBox != null)
        {
            _searchBox.TextChanged += OnSearchTextChanged;
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
        if (IsReadOnly) return;

        _isSelectingPack = true;
        ShowPopup(_packSelectorHost);
        FilterPacks("");
    }

    private void OnAssetSelectorHostPressed(object? sender, PointerPressedEventArgs e)
    {
        if (IsReadOnly) return;

        _isSelectingPack = false;
        ShowPopup(_assetSelectorHost);
        FilterAssets("");
    }

    private void ShowPopup(Control? placementTarget)
    {
        if (_selectorPopup == null || placementTarget == null) return;

        _selectorPopup.PlacementTarget = placementTarget;
        _selectorPopup.IsOpen = true;

        if (_searchBox != null)
        {
            _searchBox.Text = "";
            _searchBox.Focus();
        }
    }

    private void OnSearchTextChanged(object? sender, TextChangedEventArgs e)
    {
        var searchText = _searchBox?.Text ?? "";
        if (_isSelectingPack)
        {
            FilterPacks(searchText);
        }
        else
        {
            FilterAssets(searchText);
        }
    }

    private void FilterPacks(string searchText)
    {
        if (_itemsControl == null) return;

        IEnumerable<string> filtered = string.IsNullOrWhiteSpace(searchText)
            ? _allPacks
            : _allPacks.Where(p => p.Contains(searchText, StringComparison.OrdinalIgnoreCase));

        BuildPackItems(filtered);
    }

    private void FilterAssets(string searchText)
    {
        if (_itemsControl == null) return;

        IEnumerable<AssetInfo> filtered = string.IsNullOrWhiteSpace(searchText)
            ? _allAssets
            : _allAssets.Where(a => a.Name.Contains(searchText, StringComparison.OrdinalIgnoreCase));

        BuildAssetItems(filtered);
    }

    private void BuildPackItems(IEnumerable<string> packs)
    {
        if (_itemsControl == null) return;

        var items = new List<Button>();
        foreach (var pack in packs)
        {
            var button = CreateItemButton(pack, pack);
            button.Click += OnPackItemClicked;
            items.Add(button);
        }
        _itemsControl.ItemsSource = items;
    }

    private void BuildAssetItems(IEnumerable<AssetInfo> assets)
    {
        if (_itemsControl == null) return;

        var items = new List<Button>();
        foreach (var asset in assets)
        {
            var button = CreateItemButton(asset.Name, asset);
            button.Click += OnAssetItemClicked;
            items.Add(button);
        }
        _itemsControl.ItemsSource = items;
    }

    private Button CreateItemButton(string content, object tag)
    {
        return new Button
        {
            Content = content,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            HorizontalContentAlignment = HorizontalAlignment.Left,
            Padding = new Thickness(8, 6),
            Background = Brushes.Transparent,
            Foreground = new SolidColorBrush(Color.Parse("#EEEEEE")),
            BorderThickness = new Thickness(0),
            Tag = tag
        };
    }

    private void OnPackItemClicked(object? sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string pack)
        {
            SelectPack(pack);
            ClosePopup();
        }
    }

    private void OnAssetItemClicked(object? sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is AssetInfo assetInfo)
        {
            SelectAsset(assetInfo);
            ClosePopup();
        }
    }

    private void ClosePopup()
    {
        if (_selectorPopup != null)
        {
            _selectorPopup.IsOpen = false;
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
        if (AssetBrowser == null) return;

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
