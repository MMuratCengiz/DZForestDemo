using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using NiziKit.Components;
using NiziKit.Editor.Services;

namespace NiziKit.Editor.Views.Editors;

public partial class AssetPickerDialog : UserControl
{
    private TextBlock? _headerText;
    private TextBox? _searchBox;
    private ComboBox? _packComboBox;
    private ItemsControl? _assetItemsControl;
    private TextBlock? _selectionText;
    private Button? _cancelButton;
    private Button? _selectButton;

    private AssetBrowserService? _assetBrowser;
    private AssetRefType _assetType;
    private IReadOnlyList<string> _allPacks = Array.Empty<string>();
    private IReadOnlyList<AssetInfo> _allAssets = Array.Empty<AssetInfo>();
    private AssetInfo? _selectedAsset;
    private string? _initialPack;
    private string? _initialAssetName;

    public event Action<AssetInfo?>? AssetSelected;
    public event Action? Cancelled;

    public AssetPickerDialog()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);

        _headerText = this.FindControl<TextBlock>("HeaderText");
        _searchBox = this.FindControl<TextBox>("SearchBox");
        _packComboBox = this.FindControl<ComboBox>("PackComboBox");
        _assetItemsControl = this.FindControl<ItemsControl>("AssetItemsControl");
        _selectionText = this.FindControl<TextBlock>("SelectionText");
        _cancelButton = this.FindControl<Button>("CancelButton");
        _selectButton = this.FindControl<Button>("SelectButton");

        if (_searchBox != null)
        {
            _searchBox.TextChanged += OnSearchTextChanged;
        }
        if (_packComboBox != null)
        {
            _packComboBox.SelectionChanged += OnPackSelectionChanged;
        }
        if (_cancelButton != null)
        {
            _cancelButton.Click += OnCancelClicked;
        }
        if (_selectButton != null)
        {
            _selectButton.Click += OnSelectClicked;
        }
    }

    public void Initialize(AssetBrowserService assetBrowser, AssetRefType assetType, string? currentPack, string? currentAssetName)
    {
        _assetBrowser = assetBrowser;
        _assetType = assetType;
        _initialPack = currentPack;
        _initialAssetName = currentAssetName;

        if (_headerText != null)
        {
            _headerText.Text = $"Select {assetType}";
        }

        LoadPacks();
    }

    private void LoadPacks()
    {
        if (_assetBrowser == null || _packComboBox == null)
        {
            return;
        }

        _allPacks = _assetBrowser.GetLoadedPacks();
        _packComboBox.ItemsSource = _allPacks;

        if (!string.IsNullOrEmpty(_initialPack) && _allPacks.Contains(_initialPack))
        {
            _packComboBox.SelectedItem = _initialPack;
        }
        else if (_allPacks.Count > 0)
        {
            _packComboBox.SelectedIndex = 0;
        }
    }

    private void OnPackSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        LoadAssets();
    }

    private void OnSearchTextChanged(object? sender, TextChangedEventArgs e)
    {
        FilterAssets();
    }

    private void LoadAssets()
    {
        if (_assetBrowser == null || _packComboBox?.SelectedItem is not string selectedPack)
        {
            return;
        }

        _allAssets = _assetBrowser.GetAssetsOfType(_assetType, selectedPack);
        FilterAssets();
    }

    private void FilterAssets()
    {
        if (_assetItemsControl == null)
        {
            return;
        }

        var searchText = _searchBox?.Text ?? "";
        IEnumerable<AssetInfo> filtered = string.IsNullOrWhiteSpace(searchText)
            ? _allAssets
            : _allAssets.Where(a => a.Name.Contains(searchText, StringComparison.OrdinalIgnoreCase));

        BuildAssetGrid(filtered);
    }

    private void BuildAssetGrid(IEnumerable<AssetInfo> assets)
    {
        if (_assetItemsControl == null)
        {
            return;
        }

        var items = new List<Border>();
        foreach (var asset in assets)
        {
            var item = CreateAssetItem(asset);
            items.Add(item);
        }
        _assetItemsControl.ItemsSource = items;
    }

    private Border CreateAssetItem(AssetInfo asset)
    {
        var iconData = GetIconForAssetType(_assetType);
        var isSelected = asset.Name == _initialAssetName;

        var border = new Border
        {
            Width = 176,
            Height = 156,
            Margin = new Thickness(4),
            Padding = new Thickness(12),
            CornerRadius = new CornerRadius(4),
            Background = isSelected ? new SolidColorBrush(Color.Parse("#40569CD6")) : Brushes.Transparent,
            Cursor = new Cursor(StandardCursorType.Hand),
            Tag = asset
        };

        var stack = new StackPanel
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Spacing = 10
        };

        var icon = new PathIcon
        {
            Data = PathGeometry.Parse(iconData),
            Width = 56,
            Height = 56,
            Foreground = new SolidColorBrush(Color.Parse("#AAAAAA"))
        };

        var text = new TextBlock
        {
            Text = asset.Name,
            TextWrapping = TextWrapping.Wrap,
            TextAlignment = TextAlignment.Center,
            MaxWidth = 152,
            MaxHeight = 48,
            Foreground = new SolidColorBrush(Color.Parse("#EEEEEE"))
        };
        // Use dynamic resource for font size
        if (this.TryFindResource("EditorFontSizeSmall", this.ActualThemeVariant, out var fontSize) && fontSize is double size)
        {
            text.FontSize = size;
        }
        ToolTip.SetTip(text, asset.Name);

        stack.Children.Add(icon);
        stack.Children.Add(text);
        border.Child = stack;

        border.PointerPressed += (s, e) => OnAssetItemPressed(asset, border);
        border.DoubleTapped += (s, e) => OnAssetItemDoubleTapped(asset);

        if (isSelected)
        {
            _selectedAsset = asset;
            UpdateSelectionText();
        }

        return border;
    }

    private void OnAssetItemPressed(AssetInfo asset, Border border)
    {
        // Clear previous selection
        if (_assetItemsControl?.ItemsSource is IEnumerable<Border> items)
        {
            foreach (var item in items)
            {
                item.Background = Brushes.Transparent;
            }
        }

        // Select this item
        border.Background = new SolidColorBrush(Color.Parse("#40569CD6"));
        _selectedAsset = asset;
        UpdateSelectionText();
    }

    private void OnAssetItemDoubleTapped(AssetInfo asset)
    {
        _selectedAsset = asset;
        AssetSelected?.Invoke(_selectedAsset);
    }

    private void UpdateSelectionText()
    {
        if (_selectionText != null)
        {
            _selectionText.Text = _selectedAsset?.Name ?? "";
        }
    }

    private void OnCancelClicked(object? sender, RoutedEventArgs e)
    {
        Cancelled?.Invoke();
    }

    private void OnSelectClicked(object? sender, RoutedEventArgs e)
    {
        AssetSelected?.Invoke(_selectedAsset);
    }

    private string GetIconForAssetType(AssetRefType assetType)
    {
        return assetType switch
        {
            // 3D cube icon for meshes
            AssetRefType.Mesh => "M21,16.5C21,16.88 20.79,17.21 20.47,17.38L12.57,21.82C12.41,21.94 12.21,22 12,22C11.79,22 11.59,21.94 11.43,21.82L3.53,17.38C3.21,17.21 3,16.88 3,16.5V7.5C3,7.12 3.21,6.79 3.53,6.62L11.43,2.18C11.59,2.06 11.79,2 12,2C12.21,2 12.41,2.06 12.57,2.18L20.47,6.62C20.79,6.79 21,7.12 21,7.5V16.5M12,4.15L5,8.09V15.91L12,19.85L19,15.91V8.09L12,4.15Z",
            // Image icon for textures
            AssetRefType.Texture => "M8.5,13.5L11,16.5L14.5,12L19,18H5M21,19V5C21,3.89 20.1,3 19,3H5A2,2 0 0,0 3,5V19A2,2 0 0,0 5,21H19A2,2 0 0,0 21,19Z",
            // Color palette for materials
            AssetRefType.Material => "M17.5,12A1.5,1.5 0 0,1 16,10.5A1.5,1.5 0 0,1 17.5,9A1.5,1.5 0 0,1 19,10.5A1.5,1.5 0 0,1 17.5,12M14.5,8A1.5,1.5 0 0,1 13,6.5A1.5,1.5 0 0,1 14.5,5A1.5,1.5 0 0,1 16,6.5A1.5,1.5 0 0,1 14.5,8M9.5,8A1.5,1.5 0 0,1 8,6.5A1.5,1.5 0 0,1 9.5,5A1.5,1.5 0 0,1 11,6.5A1.5,1.5 0 0,1 9.5,8M6.5,12A1.5,1.5 0 0,1 5,10.5A1.5,1.5 0 0,1 6.5,9A1.5,1.5 0 0,1 8,10.5A1.5,1.5 0 0,1 6.5,12M12,3A9,9 0 0,0 3,12A9,9 0 0,0 12,21A1.5,1.5 0 0,0 13.5,19.5C13.5,19.11 13.35,18.76 13.11,18.5C12.88,18.23 12.73,17.88 12.73,17.5A1.5,1.5 0 0,1 14.23,16H16A5,5 0 0,0 21,11C21,6.58 16.97,3 12,3Z",
            // Code/script icon for shaders
            AssetRefType.Shader => "M14.6,16.6L19.2,12L14.6,7.4L16,6L22,12L16,18L14.6,16.6M9.4,16.6L4.8,12L9.4,7.4L8,6L2,12L8,18L9.4,16.6Z",
            // Bone icon for skeletons
            AssetRefType.Skeleton => "M19,3H5A2,2 0 0,0 3,5V19A2,2 0 0,0 5,21H19A2,2 0 0,0 21,19V5A2,2 0 0,0 19,3M13.09,16.59L12,18.42L10.91,16.59L9.08,17.5L9.59,15.42L7.5,15L9.08,13.91L7.5,12.59L9.59,12.08L9.08,10L10.91,10.91L12,9.08L13.09,10.91L14.92,10L14.41,12.08L16.5,12.59L14.92,13.91L16.5,15L14.41,15.42L14.92,17.5L13.09,16.59Z",
            // Play/film icon for animations
            AssetRefType.Animation => "M4,2H14V4H4V14H2V4C2,2.89 2.89,2 4,2M8,6H18V8H8V18H6V8C6,6.89 6.89,6 8,6M12,10H20C21.11,10 22,10.89 22,12V20C22,21.11 21.11,22 20,22H12C10.89,22 10,21.11 10,20V12C10,10.89 10.89,10 12,10M14,12V20L20,16L14,12Z",
            // Generic file icon
            _ => "M14,2H6A2,2 0 0,0 4,4V20A2,2 0 0,0 6,22H18A2,2 0 0,0 20,20V8L14,2M18,20H6V4H13V9H18V20Z"
        };
    }
}
