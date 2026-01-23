using System.Collections;
using System.Numerics;
using System.Reflection;
using Avalonia.Controls;
using Avalonia.Layout;
using NiziKit.Animation;
using NiziKit.Components;
using NiziKit.Editor.ViewModels;

namespace NiziKit.Editor.Services;

public class PropertyEditorContext
{
    public required object Instance { get; init; }
    public required PropertyInfo Property { get; init; }
    public required Action OnValueChanged { get; init; }
    public AssetBrowserService? AssetBrowser { get; init; }
    public EditorViewModel? EditorViewModel { get; init; }
}

public static class PropertyEditorRegistry
{
    private static readonly Dictionary<Type, Func<PropertyEditorContext, Control>> TypeEditors = new();

    static PropertyEditorRegistry()
    {
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

    public static Control? CreateEditor(PropertyEditorContext context)
    {
        var prop = context.Property;
        var propType = prop.PropertyType;

        if (propType.IsByRefLike)
        {
            return null;
        }

        var assetRefAttr = prop.GetCustomAttribute<AssetRefAttribute>();
        if (assetRefAttr != null)
        {
            return CreateAssetRefEditor(context, assetRefAttr);
        }

        var animationSelectorAttr = prop.GetCustomAttribute<AnimationSelectorAttribute>();
        if (animationSelectorAttr != null)
        {
            return CreateAnimationSelectorEditor(context, animationSelectorAttr);
        }

        if (propType.IsEnum)
        {
            return CreateEnumEditor(context);
        }

        if (IsListType(propType))
        {
            return CreateListEditor(context);
        }

        if (TypeEditors.TryGetValue(propType, out var factory))
        {
            return factory(context);
        }

        var underlyingType = Nullable.GetUnderlyingType(propType);
        if (underlyingType != null && TypeEditors.TryGetValue(underlyingType, out factory))
        {
            return factory(context);
        }

        if (propType.IsValueType && !propType.IsPrimitive && !propType.IsEnum)
        {
            return CreateStructEditor(context);
        }

        return CreateReadOnlyEditor(context);
    }

    private static bool IsListType(Type type)
    {
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
        {
            return true;
        }

        if (type.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IList<>)))
        {
            return true;
        }

        return false;
    }

    private static Type? GetListElementType(Type listType)
    {
        if (listType.IsGenericType && listType.GetGenericTypeDefinition() == typeof(List<>))
        {
            return listType.GetGenericArguments()[0];
        }

        var listInterface = listType.GetInterfaces()
            .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IList<>));

        return listInterface?.GetGenericArguments()[0];
    }

    private static Control CreateListEditor(PropertyEditorContext context)
    {
        var elementType = GetListElementType(context.Property.PropertyType);

        if (elementType == typeof(AnimationEntry))
        {
            return CreateAnimationListEditor(context);
        }

        return new Views.Editors.ListEditor
        {
            Instance = context.Instance,
            Property = context.Property,
            AssetBrowser = context.AssetBrowser,
            EditorViewModel = context.EditorViewModel,
            OnValueChanged = context.OnValueChanged,
            IsReadOnly = !context.Property.CanWrite
        };
    }

    private static Control CreateAnimationListEditor(PropertyEditorContext context)
    {
        var editor = new Views.Editors.AnimationListEditor
        {
            Instance = context.Instance,
            Property = context.Property,
            AssetBrowser = context.AssetBrowser,
            EditorViewModel = context.EditorViewModel,
            OnValueChanged = context.OnValueChanged,
            IsReadOnly = !context.Property.CanWrite
        };

        return editor;
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
        var rangeAttr = context.Property.GetCustomAttribute<RangeAttribute>();

        if (rangeAttr != null)
        {
            var slider = new Slider
            {
                Minimum = rangeAttr.Min,
                Maximum = rangeAttr.Max,
                Value = floatValue,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };

            if (context.Property.CanWrite)
            {
                slider.ValueChanged += (s, e) =>
                {
                    context.Property.SetValue(context.Instance, (float)slider.Value);
                    context.OnValueChanged();
                };
            }
            else
            {
                slider.IsEnabled = false;
            }

            return slider;
        }

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
            EditorViewModel = context.EditorViewModel,
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

    private static Control CreateAnimationSelectorEditor(PropertyEditorContext context, AnimationSelectorAttribute attr)
    {
        var skeletonProperty = context.Instance.GetType().GetProperty(attr.SkeletonPropertyName);

        return new Views.Editors.AnimationSelectorEditor
        {
            Instance = context.Instance,
            Property = context.Property,
            SkeletonProperty = skeletonProperty,
            IsReadOnly = !context.Property.CanWrite,
            OnValueChanged = context.OnValueChanged
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
