using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using NiziKit.Components;
using NiziKit.Editor.Services;
using NiziKit.Editor.ViewModels;

namespace NiziKit.Editor.Views;

public partial class GenericComponentView : UserControl
{
    private ItemsControl? _propertiesControl;
    private AssetBrowserService? _assetBrowser;

    public GenericComponentView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
        _propertiesControl = this.FindControl<ItemsControl>("PropertiesControl");
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        if (DataContext is ComponentViewModel vm && _propertiesControl != null)
        {
            // Get the AssetBrowserService from the EditorViewModel
            _assetBrowser = vm.Owner?.Editor?.AssetBrowser;
            BuildPropertyControls(vm.Component);
        }
    }

    private void BuildPropertyControls(IComponent component)
    {
        if (_propertiesControl == null)
        {
            return;
        }

        var items = new List<Control>();
        var type = component.GetType();
        var typeName = type.Name;
        var displayName = typeName.EndsWith("Component") ? typeName[..^9] : typeName;

        var assetRefProperties = new HashSet<string>();
        foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (prop.GetCustomAttribute<AssetRefAttribute>() != null)
            {
                assetRefProperties.Add(prop.Name + "Ref");
            }
        }

        foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (prop.Name == "Owner" || !prop.CanRead)
            {
                continue;
            }

            if (assetRefProperties.Contains(prop.Name))
            {
                continue;
            }

            var skipLabel = prop.Name == displayName;
            var control = CreatePropertyControl(component, prop, skipLabel);
            if (control != null)
            {
                items.Add(control);
            }
        }

        _propertiesControl.ItemsSource = items;
    }

    private Control? CreatePropertyControl(IComponent component, PropertyInfo prop, bool skipLabel = false)
    {
        var panel = new StackPanel { Spacing = 4, Margin = new Thickness(0, 0, 0, 6) };

        if (prop.PropertyType != typeof(bool) && !skipLabel)
        {
            var label = new TextBlock
            {
                Text = prop.Name,
                Classes = { "label" }
            };
            panel.Children.Add(label);
        }

        var context = new PropertyEditorContext
        {
            Instance = component,
            Property = prop,
            AssetBrowser = _assetBrowser,
            OnValueChanged = () => component.Owner?.NotifyComponentChanged(component)
        };

        var editor = PropertyEditorRegistry.CreateEditor(context);

        if (prop.PropertyType == typeof(bool) && !skipLabel)
        {
            var boolPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 8
            };
            var label = new TextBlock
            {
                Text = prop.Name,
                Classes = { "label" },
                VerticalAlignment = VerticalAlignment.Center
            };
            boolPanel.Children.Add(editor);
            boolPanel.Children.Add(label);
            panel.Children.Add(boolPanel);
        }
        else if (prop.PropertyType == typeof(bool))
        {
            panel.Children.Add(editor);
        }
        else
        {
            panel.Children.Add(editor);
        }

        return panel;
    }
}
