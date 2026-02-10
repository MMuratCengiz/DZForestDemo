using System.Collections.Specialized;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using NiziKit.Components;
using NiziKit.Editor.ViewModels;

namespace NiziKit.Editor.Views;

public class ComponentTypeEntry
{
    public Type Type { get; }
    public string DisplayName { get; }
    public string Category { get; }

    private static readonly Dictionary<string, string> CategoryMap = new()
    {
        ["Rigidbody"] = "Physics",
        ["BoxCollider"] = "Physics",
        ["SphereCollider"] = "Physics",
        ["CapsuleCollider"] = "Physics",
        ["CylinderCollider"] = "Physics",
        ["WheelColliderComponent"] = "Physics",
        ["CharacterController"] = "Physics",
        ["MeshComponent"] = "Rendering",
        ["MaterialComponent"] = "Rendering",
        ["Animator"] = "Animation",
        ["CameraComponent"] = "Camera",
        ["OrbitController"] = "Camera",
        ["FreeFlyController"] = "Camera",
    };

    public ComponentTypeEntry(Type type, bool isEngineType)
    {
        Type = type;
        var name = type.Name;
        if (name.EndsWith("Component"))
        {
            name = name[..^9];
        }
        DisplayName = name;
        Category = isEngineType
            ? CategoryMap.GetValueOrDefault(type.Name, "Misc")
            : "Scripts";
    }
}

public partial class AddComponentPanel : UserControl
{
    // Defines display order for categories; Scripts always last
    private static readonly string[] CategoryOrder = ["Physics", "Rendering", "Animation", "Camera", "Misc", "Scripts"];

    private StackPanel? _categoryView;
    private StackPanel? _itemsView;
    private StackPanel? _itemsList;
    private StackPanel? _searchResultsView;
    private Button? _backButton;
    private TextBlock? _categoryTitle;
    private TextBox? _searchBox;
    private GameObjectViewModel? _currentViewModel;

    private List<ComponentTypeEntry> _allEntries = new();
    private string? _selectedCategory;

    public AddComponentPanel()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
        _categoryView = this.FindControl<StackPanel>("CategoryView");
        _itemsView = this.FindControl<StackPanel>("ItemsView");
        _itemsList = this.FindControl<StackPanel>("ItemsList");
        _searchResultsView = this.FindControl<StackPanel>("SearchResultsView");
        _backButton = this.FindControl<Button>("BackButton");
        _categoryTitle = this.FindControl<TextBlock>("CategoryTitle");
        _searchBox = this.FindControl<TextBox>("SearchBox");

        if (_searchBox != null)
        {
            _searchBox.TextChanged += OnSearchTextChanged;
        }
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        if (_currentViewModel != null)
        {
            _currentViewModel.Components.CollectionChanged -= OnComponentsCollectionChanged;
        }

        _currentViewModel = DataContext as GameObjectViewModel;

        if (_currentViewModel != null)
        {
            _currentViewModel.Components.CollectionChanged += OnComponentsCollectionChanged;
        }

        _selectedCategory = null;
        RefreshComponentTypes();
    }

    private void OnComponentsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        RefreshComponentTypes();
    }

    private void OnSearchTextChanged(object? sender, TextChangedEventArgs e)
    {
        UpdateView();
    }

    public void RefreshComponentTypes()
    {
        if (DataContext is not GameObjectViewModel vm)
        {
            return;
        }

        var niziKitAssembly = typeof(IComponent).Assembly;
        _allEntries = vm.GetAvailableComponentTypes()
            .DistinctBy(t => t)
            .Select(t => new ComponentTypeEntry(t, t.Assembly == niziKitAssembly))
            .OrderBy(e => e.DisplayName)
            .ToList();

        UpdateView();
    }

    private void UpdateView()
    {
        if (_categoryView == null || _itemsView == null || _searchResultsView == null)
        {
            return;
        }

        var filter = _searchBox?.Text?.Trim() ?? string.Empty;
        var isSearching = !string.IsNullOrEmpty(filter);

        var filtered = isSearching
            ? _allEntries.Where(e => e.DisplayName.Contains(filter, StringComparison.OrdinalIgnoreCase)).ToList()
            : _allEntries;

        if (isSearching)
        {
            _categoryView.IsVisible = false;
            _itemsView.IsVisible = false;
            _searchResultsView.IsVisible = true;
            PopulateSearchResults(filtered);
        }
        else if (_selectedCategory != null)
        {
            _categoryView.IsVisible = false;
            _itemsView.IsVisible = true;
            _searchResultsView.IsVisible = false;

            if (_categoryTitle != null)
            {
                _categoryTitle.Text = _selectedCategory;
            }

            var items = filtered.Where(e => e.Category == _selectedCategory).ToList();
            PopulateItemsList(items);
        }
        else
        {
            _categoryView.IsVisible = true;
            _itemsView.IsVisible = false;
            _searchResultsView.IsVisible = false;
            PopulateCategoryView(filtered);
        }
    }

    private void PopulateCategoryView(List<ComponentTypeEntry> entries)
    {
        if (_categoryView == null)
        {
            return;
        }

        _categoryView.Children.Clear();

        var groups = entries.GroupBy(e => e.Category)
            .ToDictionary(g => g.Key, g => g.Count());

        foreach (var category in CategoryOrder)
        {
            if (!groups.TryGetValue(category, out var count))
            {
                continue;
            }

            var nameBlock = new TextBlock { Text = category, VerticalAlignment = VerticalAlignment.Center };
            var countBlock = new TextBlock
            {
                Text = $"({count})",
                VerticalAlignment = VerticalAlignment.Center
            };
            countBlock.Bind(TextBlock.ForegroundProperty,
                countBlock.GetResourceObservable("EditorTextSecondary")!);

            var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto") };
            grid.Children.Add(nameBlock);
            Grid.SetColumn(countBlock, 1);
            grid.Children.Add(countBlock);

            var btn = new Button
            {
                Content = grid,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                HorizontalContentAlignment = HorizontalAlignment.Left,
                Padding = new Thickness(14, 12),
                Tag = category
            };
            btn.Click += OnCategoryClicked;
            _categoryView.Children.Add(btn);
        }
    }

    private void PopulateItemsList(List<ComponentTypeEntry> items)
    {
        if (_itemsList == null)
        {
            return;
        }

        _itemsList.Children.Clear();

        foreach (var entry in items)
        {
            _itemsList.Children.Add(CreateComponentButton(entry));
        }
    }

    private void PopulateSearchResults(List<ComponentTypeEntry> filtered)
    {
        if (_searchResultsView == null)
        {
            return;
        }

        _searchResultsView.Children.Clear();

        var groups = filtered.GroupBy(e => e.Category);
        var ordered = groups.OrderBy(g => Array.IndexOf(CategoryOrder, g.Key) is var idx && idx < 0 ? int.MaxValue : idx);

        foreach (var group in ordered)
        {
            var header = new TextBlock
            {
                Text = group.Key,
                FontWeight = FontWeight.SemiBold,
                Margin = new Thickness(4, 8, 0, 4)
            };
            header.Bind(TextBlock.ForegroundProperty,
                header.GetResourceObservable("EditorTextSecondary")!);
            _searchResultsView.Children.Add(header);

            foreach (var entry in group)
            {
                _searchResultsView.Children.Add(CreateComponentButton(entry));
            }
        }
    }

    private Button CreateComponentButton(ComponentTypeEntry entry)
    {
        var btn = new Button
        {
            Content = entry.DisplayName,
            Tag = entry,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            HorizontalContentAlignment = HorizontalAlignment.Left,
            Padding = new Thickness(14, 12),
            Margin = new Thickness(0, 2)
        };
        btn.Click += OnComponentTypeClicked;
        return btn;
    }

    private void OnCategoryClicked(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string category })
        {
            _selectedCategory = category;
            UpdateView();
        }
    }

    private void OnBackClicked(object? sender, RoutedEventArgs e)
    {
        _selectedCategory = null;
        UpdateView();
    }

    private void OnComponentTypeClicked(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: ComponentTypeEntry entry })
        {
            if (DataContext is GameObjectViewModel vm)
            {
                vm.AddComponentOfType(entry.Type);
            }
        }
    }
}
