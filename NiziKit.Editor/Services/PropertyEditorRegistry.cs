using System.Numerics;
using System.Reflection;
using Avalonia.Controls;
using Avalonia.Layout;
using NiziKit.Components;

namespace NiziKit.Editor.Services;

/// <summary>
/// Context passed to property editor factories containing all information needed to create an editor.
/// </summary>
public class PropertyEditorContext
{
    public required object Instance { get; init; }
    public required PropertyInfo Property { get; init; }
    public required Action OnValueChanged { get; init; }
    public AssetBrowserService? AssetBrowser { get; init; }
}

/// <summary>
/// Central registry that maps property types and attributes to editor factory functions.
/// </summary>
public static class PropertyEditorRegistry
{
    private static readonly Dictionary<Type, Func<PropertyEditorContext, Control>> TypeEditors = new();

    static PropertyEditorRegistry()
    {
        // Register built-in type editors
        RegisterTypeEditor(typeof(string), CreateStringEditor);
        RegisterTypeEditor(typeof(float), CreateFloatEditor);
        RegisterTypeEditor(typeof(double), CreateDoubleEditor);
        RegisterTypeEditor(typeof(int), CreateIntEditor);
        RegisterTypeEditor(typeof(bool), CreateBoolEditor);
        RegisterTypeEditor(typeof(Vector3), CreateVector3Editor);
    }

    public static void RegisterTypeEditor(Type type, Func<PropertyEditorContext, Control> factory)
    {
        TypeEditors[type] = factory;
    }

    public static Control CreateEditor(PropertyEditorContext context)
    {
        var prop = context.Property;
        var propType = prop.PropertyType;

        // Check for AssetRef attribute first (attribute-based takes priority)
        var assetRefAttr = prop.GetCustomAttribute<AssetRefAttribute>();
        if (assetRefAttr != null)
        {
            return CreateAssetRefEditor(context, assetRefAttr);
        }

        // Check for enum types
        if (propType.IsEnum)
        {
            return CreateEnumEditor(context);
        }

        // Check for registered type editors
        if (TypeEditors.TryGetValue(propType, out var factory))
        {
            return factory(context);
        }

        // Check for nullable value types
        var underlyingType = Nullable.GetUnderlyingType(propType);
        if (underlyingType != null && TypeEditors.TryGetValue(underlyingType, out factory))
        {
            return factory(context);
        }

        // Check for value types (structs) that aren't primitives
        if (propType.IsValueType && !propType.IsPrimitive && !propType.IsEnum)
        {
            return CreateStructEditor(context);
        }

        // Fallback to read-only display
        return CreateReadOnlyEditor(context);
    }

    private static Control CreateStringEditor(PropertyEditorContext context)
    {
        var value = context.Property.GetValue(context.Instance)?.ToString() ?? "";
        var textBox = new TextBox { Text = value };

        if (context.Property.CanWrite)
        {
            textBox.TextChanged += (s, e) =>
            {
                context.Property.SetValue(context.Instance, textBox.Text);
                context.OnValueChanged();
            };
        }
        else
        {
            textBox.IsReadOnly = true;
        }

        return textBox;
    }

    private static Control CreateFloatEditor(PropertyEditorContext context)
    {
        var value = context.Property.GetValue(context.Instance);
        var floatValue = value != null ? (float)value : 0f;

        var editor = new Views.DraggableValueEditor
        {
            Value = floatValue,
            Label = "",
            DragSensitivity = 0.01f
        };

        if (context.Property.CanWrite)
        {
            editor.PropertyChanged += (s, e) =>
            {
                if (e.Property.Name == nameof(Views.DraggableValueEditor.Value))
                {
                    context.Property.SetValue(context.Instance, editor.Value);
                    context.OnValueChanged();
                }
            };
        }
        else
        {
            editor.IsEnabled = false;
        }

        return editor;
    }

    private static Control CreateDoubleEditor(PropertyEditorContext context)
    {
        var value = context.Property.GetValue(context.Instance);
        var doubleValue = value != null ? (double)value : 0.0;

        var editor = new Views.DraggableValueEditor
        {
            Value = (float)doubleValue,
            Label = "",
            DragSensitivity = 0.01f
        };

        if (context.Property.CanWrite)
        {
            editor.PropertyChanged += (s, e) =>
            {
                if (e.Property.Name == nameof(Views.DraggableValueEditor.Value))
                {
                    context.Property.SetValue(context.Instance, (double)editor.Value);
                    context.OnValueChanged();
                }
            };
        }
        else
        {
            editor.IsEnabled = false;
        }

        return editor;
    }

    private static Control CreateIntEditor(PropertyEditorContext context)
    {
        var value = context.Property.GetValue(context.Instance);
        var textBox = new TextBox { Text = value?.ToString() ?? "0" };

        if (context.Property.CanWrite)
        {
            textBox.LostFocus += (s, e) =>
            {
                if (int.TryParse(textBox.Text, out var newValue))
                {
                    context.Property.SetValue(context.Instance, newValue);
                    context.OnValueChanged();
                }
            };
        }
        else
        {
            textBox.IsReadOnly = true;
        }

        return textBox;
    }

    private static Control CreateBoolEditor(PropertyEditorContext context)
    {
        var checkBox = new CheckBox
        {
            IsChecked = (bool?)context.Property.GetValue(context.Instance) ?? false
        };

        if (context.Property.CanWrite)
        {
            checkBox.IsCheckedChanged += (s, e) =>
            {
                context.Property.SetValue(context.Instance, checkBox.IsChecked ?? false);
                context.OnValueChanged();
            };
        }
        else
        {
            checkBox.IsEnabled = false;
        }

        return checkBox;
    }

    private static Control CreateEnumEditor(PropertyEditorContext context)
    {
        var propType = context.Property.PropertyType;
        var comboBox = new ComboBox
        {
            ItemsSource = Enum.GetValues(propType),
            SelectedItem = context.Property.GetValue(context.Instance),
            HorizontalAlignment = HorizontalAlignment.Stretch
        };

        if (context.Property.CanWrite)
        {
            comboBox.SelectionChanged += (s, e) =>
            {
                if (comboBox.SelectedItem != null)
                {
                    context.Property.SetValue(context.Instance, comboBox.SelectedItem);
                    context.OnValueChanged();
                }
            };
        }
        else
        {
            comboBox.IsEnabled = false;
        }

        return comboBox;
    }

    private static Control CreateVector3Editor(PropertyEditorContext context)
    {
        var value = context.Property.GetValue(context.Instance);
        var vector = value is Vector3 v ? v : Vector3.Zero;

        return new Views.Editors.Vector3Editor
        {
            Value = vector,
            IsReadOnly = !context.Property.CanWrite,
            OnValueChanged = newValue =>
            {
                if (context.Property.CanWrite)
                {
                    context.Property.SetValue(context.Instance, newValue);
                    context.OnValueChanged();
                }
            }
        };
    }

    private static Control CreateAssetRefEditor(PropertyEditorContext context, AssetRefAttribute assetRefAttr)
    {
        var refPropertyName = context.Property.Name + "Ref";
        var refProperty = context.Instance.GetType().GetProperty(refPropertyName);

        return new Views.Editors.AssetRefEditor
        {
            AssetType = assetRefAttr.AssetType,
            AssetBrowser = context.AssetBrowser,
            CurrentAsset = context.Property.GetValue(context.Instance),
            IsReadOnly = !context.Property.CanWrite,
            OnAssetChanged = (newAsset, assetRef) =>
            {
                if (context.Property.CanWrite)
                {
                    context.Property.SetValue(context.Instance, newAsset);

                    if (refProperty != null && refProperty.CanWrite)
                    {
                        refProperty.SetValue(context.Instance, assetRef);
                    }

                    context.OnValueChanged();
                }
            }
        };
    }

    private static Control CreateStructEditor(PropertyEditorContext context)
    {
        return new Views.Editors.StructEditor
        {
            Instance = context.Instance,
            Property = context.Property,
            AssetBrowser = context.AssetBrowser,
            OnValueChanged = context.OnValueChanged
        };
    }

    private static Control CreateReadOnlyEditor(PropertyEditorContext context)
    {
        var value = context.Property.GetValue(context.Instance);
        return new TextBlock
        {
            Text = value?.ToString() ?? "(null)",
            Classes = { "muted" }
        };
    }
}
