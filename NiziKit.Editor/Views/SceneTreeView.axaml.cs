using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using FluentAvalonia.UI.Controls;
using NiziKit.Editor.ViewModels;

namespace NiziKit.Editor.Views;

public partial class SceneTreeView : UserControl
{
    private TreeView? _sceneTree;
    private Border? _sceneRootNode;
    private Border? _deleteConfirmPanel;
    private TextBlock? _deleteConfirmText;
    private Button? _cancelDeleteButton;
    private Button? _confirmDeleteButton;
    private InlineContextMenu? _inlineMenu;

    public SceneTreeView()
    {
        InitializeComponent();
        PointerPressed += OnViewPointerPressed;
        DataContextChanged += OnDataContextSet;
    }

    private void OnDataContextSet(object? sender, EventArgs e)
    {
        if (DataContext is EditorViewModel vm)
        {
            vm.PropertyChanged += OnViewModelPropertyChanged;
        }
    }

    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(EditorViewModel.HasSelection))
        {
            if (DataContext is EditorViewModel vm)
            {
                UpdateSceneRootSelection(!vm.HasSelection);
            }
        }
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);

        _sceneTree = this.FindControl<TreeView>("SceneTree");
        _sceneRootNode = this.FindControl<Border>("SceneRootNode");
        _deleteConfirmPanel = this.FindControl<Border>("DeleteConfirmPanel");

        if (_sceneRootNode != null)
        {
            _sceneRootNode.PointerPressed += OnSceneRootNodePressed;
        }
        _deleteConfirmText = this.FindControl<TextBlock>("DeleteConfirmText");
        _cancelDeleteButton = this.FindControl<Button>("CancelDeleteButton");
        _confirmDeleteButton = this.FindControl<Button>("ConfirmDeleteButton");
        _inlineMenu = this.FindControl<InlineContextMenu>("InlineMenu");

        KeyDown += OnKeyDown;

        if (_cancelDeleteButton != null)
        {
            _cancelDeleteButton.Click += OnCancelDeleteClick;
        }

        if (_confirmDeleteButton != null)
        {
            _confirmDeleteButton.Click += OnConfirmDeleteClick;
        }
    }

    private void OnSceneRootNodePressed(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is EditorViewModel vm)
        {
            vm.SelectObject(null);
            if (_sceneTree != null)
            {
                _sceneTree.UnselectAll();
            }
            UpdateSceneRootSelection(true);
        }
        e.Handled = true;
    }

    private void UpdateSceneRootSelection(bool selected)
    {
        if (_sceneRootNode == null) return;
        if (selected)
            _sceneRootNode.Classes.Add("selected");
        else
            _sceneRootNode.Classes.Remove("selected");
    }

    private void OnViewPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var point = e.GetCurrentPoint(this);
        if (point.Properties.IsLeftButtonPressed && _inlineMenu?.IsVisible == true)
        {
            _inlineMenu.Hide();
        }
    }

    private void OnTreeViewPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var point = e.GetCurrentPoint(this);
        if (point.Properties.IsRightButtonPressed)
        {
            ShowContextMenu(point.Position);
            e.Handled = true;
        }
    }

    private void ShowContextMenu(Point position)
    {
        if (DataContext is not EditorViewModel vm || _inlineMenu == null)
        {
            return;
        }

        var items = new List<InlineMenuItem>
        {
            new()
            {
                Header = "New Object",
                Icon = Symbol.Add,
                Command = vm.NewObjectCommand
            },
            new()
            {
                Header = "New Child",
                Icon = Symbol.Add,
                Command = vm.NewChildObjectCommand
            },
            InlineMenuItem.Separator(),
            new()
            {
                Header = "Delete",
                Icon = Symbol.Delete,
                Command = vm.DeleteObjectCommand
            }
        };

        _inlineMenu.Show(position, items);
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Delete && DataContext is EditorViewModel vm && vm.HasSelection)
        {
            ShowDeleteConfirmation(vm);
            e.Handled = true;
        }
        else if (e.Key == Key.Escape && _deleteConfirmPanel?.IsVisible == true)
        {
            HideDeleteConfirmation();
            e.Handled = true;
        }
    }

    private void OnCancelDeleteClick(object? sender, RoutedEventArgs e)
    {
        HideDeleteConfirmation();
    }

    private void OnConfirmDeleteClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is EditorViewModel vm)
        {
            vm.DeleteObjectCommand.Execute(null);
        }
        HideDeleteConfirmation();
    }

    private void ShowDeleteConfirmation(EditorViewModel vm)
    {
        if (_deleteConfirmPanel != null && _deleteConfirmText != null)
        {
            var selectedName = vm.SelectedGameObject?.Name ?? "selected object";
            _deleteConfirmText.Text = $"Delete \"{selectedName}\"?";
            _deleteConfirmPanel.IsVisible = true;
        }
    }

    private void HideDeleteConfirmation()
    {
        if (_deleteConfirmPanel != null)
        {
            _deleteConfirmPanel.IsVisible = false;
        }
    }
}

public class BoolToForegroundConverter : IValueConverter
{
    public static readonly BoolToForegroundConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        if (value is bool isActive)
        {
            return isActive ? Brushes.White : new SolidColorBrush(Color.FromArgb(128, 255, 255, 255));
        }
        return Brushes.White;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
