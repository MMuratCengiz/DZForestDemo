using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using NiziKit.Editor.ViewModels;

namespace NiziKit.Editor.Views;

public partial class SceneTreeView : UserControl
{
    private TreeView? _sceneTree;
    private Border? _deleteConfirmPanel;
    private TextBlock? _deleteConfirmText;
    private Button? _cancelDeleteButton;
    private Button? _confirmDeleteButton;

    public SceneTreeView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);

        _sceneTree = this.FindControl<TreeView>("SceneTree");
        _deleteConfirmPanel = this.FindControl<Border>("DeleteConfirmPanel");
        _deleteConfirmText = this.FindControl<TextBlock>("DeleteConfirmText");
        _cancelDeleteButton = this.FindControl<Button>("CancelDeleteButton");
        _confirmDeleteButton = this.FindControl<Button>("ConfirmDeleteButton");

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
