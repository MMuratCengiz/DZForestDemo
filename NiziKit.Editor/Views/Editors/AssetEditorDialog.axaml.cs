using Avalonia.Controls;
using Avalonia.Interactivity;
using NiziKit.Editor.ViewModels;

namespace NiziKit.Editor.Views.Editors;

public partial class AssetEditorDialog : UserControl
{
    private bool _formLoaded;
    private JsonAssetSchema? _currentSchema;

    public AssetEditorDialog()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        EditorTabs.SelectionChanged += OnTabSelectionChanged;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is ContentBrowserViewModel vm)
        {
            vm.PropertyChanged += (s, args) =>
            {
                if (args.PropertyName == nameof(vm.IsAssetEditorOpen) && vm.IsAssetEditorOpen)
                {
                    LoadFormEditor();
                }
            };

            // Load immediately if already open
            if (vm.IsAssetEditorOpen)
            {
                LoadFormEditor();
            }
        }
    }

    private void LoadFormEditor()
    {
        if (DataContext is not ContentBrowserViewModel vm || vm.EditingItem == null)
        {
            return;
        }

        _currentSchema = JsonAssetSchema.GetSchemaForFile(vm.EditingItem.FullPath);
        FormEditor.LoadJson(vm.EditingJson, _currentSchema, vm.EditingItem.FullPath);
        _formLoaded = true;

        // Reset to form tab
        EditorTabs.SelectedIndex = 0;
    }

    private void OnTabSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (DataContext is not ContentBrowserViewModel vm || !_formLoaded)
        {
            return;
        }

        // Sync data when switching tabs
        if (EditorTabs.SelectedIndex == 0) // Switching to Form
        {
            // Update form from JSON text (if JSON was edited)
            FormEditor.LoadJson(vm.EditingJson, _currentSchema, vm.EditingItem?.FullPath);
        }
        else if (EditorTabs.SelectedIndex == 1) // Switching to JSON
        {
            // Update JSON text from form
            vm.EditingJson = FormEditor.GetJson();
        }
    }

    private void OnSaveClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not ContentBrowserViewModel vm)
        {
            return;
        }

        // If on form tab, get the JSON from the form
        if (EditorTabs.SelectedIndex == 0 && _formLoaded)
        {
            vm.EditingJson = FormEditor.GetJson();
        }

        // Execute the save command
        vm.SaveAssetEditorCommand.Execute(null);
    }
}
