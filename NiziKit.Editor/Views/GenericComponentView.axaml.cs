using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using NiziKit.Animation;
using NiziKit.Components;
using NiziKit.Editor.Services;
using NiziKit.Editor.ViewModels;
using NiziKit.Editor.Views.Editors;

namespace NiziKit.Editor.Views;

public partial class GenericComponentView : UserControl
{
    private ItemsControl? _propertiesControl;
    private AssetBrowserService? _assetBrowser;
    private EditorViewModel? _editorViewModel;
    private AnimationPreviewEditor? _animationPreviewEditor;
    private readonly List<AnimationSelectorEditor> _animationSelectorEditors = new();
    private readonly List<AnimationListEditor> _animationListEditors = new();

    public AnimationPreviewEditor? AnimationPreviewEditor => _animationPreviewEditor;

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
            _assetBrowser = vm.Owner?.Editor?.AssetBrowser;
            _editorViewModel = vm.Owner?.Editor;
            BuildPropertyControls(vm.Component);
        }
    }

    private void BuildPropertyControls(IComponent component)
    {
        if (_propertiesControl == null)
        {
            return;
        }

        _animationSelectorEditors.Clear();
        _animationListEditors.Clear();

        var items = new List<Control>();
        var type = component.GetType();

        if (component is Animator animator)
        {
            _animationPreviewEditor = new AnimationPreviewEditor();
            _animationPreviewEditor.SetAnimator(animator, _editorViewModel);
            items.Add(_animationPreviewEditor);
        }
        else
        {
            _animationPreviewEditor = null;
        }
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

            if (!ShouldShowProperty(prop))
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

        WireUpAnimationEditors();
        _propertiesControl.ItemsSource = items;
    }

    private void WireUpAnimationEditors()
    {
        if (_animationSelectorEditors.Count == 0 || _animationListEditors.Count == 0)
        {
            return;
        }

        foreach (var listEditor in _animationListEditors)
        {
            listEditor.OnAnimationsChanged = RefreshAnimationSelectors;
        }
    }

    private void RefreshAnimationSelectors()
    {
        foreach (var selector in _animationSelectorEditors)
        {
            selector.RefreshAnimations();
        }

        _animationPreviewEditor?.RefreshAnimations();
    }

    private static bool ShouldShowProperty(PropertyInfo prop)
    {
        if (prop.GetCustomAttribute<DontSerializeAttribute>() != null)
        {
            return false;
        }

        if (prop.GetCustomAttribute<HideInInspectorAttribute>() != null)
        {
            return false;
        }

        if (prop.PropertyType.IsByRefLike)
        {
            return false;
        }

        var hasSerializeAttr = prop.GetCustomAttribute<SerializeFieldAttribute>() != null;
        var hasJsonAttr = prop.GetCustomAttribute<JsonPropertyAttribute>() != null;
        var hasAssetRef = prop.GetCustomAttribute<AssetRefAttribute>() != null;

        if (hasSerializeAttr || hasJsonAttr || hasAssetRef)
        {
            return true;
        }

        return prop.CanWrite && prop.GetSetMethod() != null;
    }

    private static string FormatPropertyName(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return name;
        }

        var sb = new System.Text.StringBuilder();
        sb.Append(name[0]);
        for (int i = 1; i < name.Length; i++)
        {
            if (char.IsUpper(name[i]) && !char.IsUpper(name[i - 1]))
            {
                sb.Append(' ');
            }
            sb.Append(name[i]);
        }
        return sb.ToString();
    }

    private Control? CreatePropertyControl(IComponent component, PropertyInfo prop, bool skipLabel = false)
    {
        var panel = new StackPanel { Spacing = 3, Margin = new Thickness(0, 0, 0, 6) };
        var displayName = FormatPropertyName(prop.Name);

        if (prop.PropertyType != typeof(bool) && !skipLabel)
        {
            var label = new TextBlock
            {
                Text = displayName,
                Classes = { "label" }
            };
            panel.Children.Add(label);
        }

        var context = new PropertyEditorContext
        {
            Instance = component,
            Property = prop,
            AssetBrowser = _assetBrowser,
            EditorViewModel = _editorViewModel,
            OnValueChanged = () => component.Owner?.NotifyComponentChanged(component)
        };

        var editor = PropertyEditorRegistry.CreateEditor(context);
        if (editor == null)
        {
            return null;
        }

        if (editor is AnimationSelectorEditor selectorEditor)
        {
            _animationSelectorEditors.Add(selectorEditor);
        }
        else if (editor is AnimationListEditor listEditor)
        {
            _animationListEditors.Add(listEditor);
        }

        if (prop.PropertyType == typeof(bool) && !skipLabel)
        {
            var boolPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 8
            };
            var label = new TextBlock
            {
                Text = displayName,
                Classes = { "label" },
                VerticalAlignment = VerticalAlignment.Center
            };
            boolPanel.Children.Add(editor);
            boolPanel.Children.Add(label);
            panel.Children.Add(boolPanel);
        }
        else
        {
            panel.Children.Add(editor);
        }

        return panel;
    }
}
