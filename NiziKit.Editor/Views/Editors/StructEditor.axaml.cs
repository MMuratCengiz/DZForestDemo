using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using NiziKit.Editor.Services;

namespace NiziKit.Editor.Views.Editors;

public partial class StructEditor : UserControl
{
    private ItemsControl? _fieldsControl;

    public object? Instance { get; set; }
    public PropertyInfo? Property { get; set; }
    public AssetBrowserService? AssetBrowser { get; set; }
    public Action? OnValueChanged { get; set; }

    public StructEditor()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
        _fieldsControl = this.FindControl<ItemsControl>("FieldsControl");
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();
        BuildFieldEditors();
    }

    private void BuildFieldEditors()
    {
        if (_fieldsControl == null || Instance == null || Property == null)
        {
            return;
        }

        var structValue = Property.GetValue(Instance);
        if (structValue == null)
        {
            return;
        }

        var structType = Property.PropertyType;
        var controls = new List<Control>();

        foreach (var field in structType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (!field.CanRead)
            {
                continue;
            }

            if (field.PropertyType.IsByRefLike)
            {
                continue;
            }

            var panel = new StackPanel { Spacing = 4, Margin = new Thickness(0, 0, 0, 6) };

            var label = new TextBlock { Text = field.Name, Classes = { "label" } };
            panel.Children.Add(label);

            var context = new PropertyEditorContext
            {
                Instance = structValue,
                Property = field,
                AssetBrowser = AssetBrowser,
                OnValueChanged = () => OnStructFieldChanged(structValue)
            };

            var editor = PropertyEditorRegistry.CreateEditor(context);
            if (editor == null)
            {
                continue;
            }

            panel.Children.Add(editor);
            controls.Add(panel);
        }

        _fieldsControl.ItemsSource = controls;
    }

    private void OnStructFieldChanged(object structValue)
    {
        // When a field within the struct changes, we need to reassign
        // the entire struct back to the parent property (since structs are value types)
        if (Instance != null && Property != null && Property.CanWrite)
        {
            Property.SetValue(Instance, structValue);
            OnValueChanged?.Invoke();
        }
    }
}
