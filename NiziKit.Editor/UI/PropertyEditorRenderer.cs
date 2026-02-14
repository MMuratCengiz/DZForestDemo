using System.Collections;
using System.Numerics;
using System.Reflection;
using DenOfIz;
using NiziKit.Animation;
using NiziKit.Assets;
using NiziKit.Components;
using NiziKit.Editor.Services;
using NiziKit.Editor.Theme;
using NiziKit.Editor.ViewModels;
using NiziKit.UI;

namespace NiziKit.Editor.UI;

public static class PropertyEditorRenderer
{
    private static readonly HashSet<string> SkippedProperties =
    [
        "Owner", "IsEnabled", "IsActive", "Transform",
        "gameObject", "GameObject", "enabled"
    ];

    public static void RenderProperties(UiFrame ui, UiContext ctx, string prefix,
        object instance, EditorViewModel editorVm, Action? onChanged = null)
    {
        var t = EditorTheme.Current;
        var type = instance.GetType();
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && !SkippedProperties.Contains(p.Name) && !p.PropertyType.IsByRefLike
                        && p.GetCustomAttribute<HideInInspectorAttribute>() == null)
            .OrderBy(p => p.MetadataToken)
            .ToArray();

        using (var grid = Ui.PropertyGrid(ctx, prefix + "_PropGrid")
            .LabelWidth(75)
            .FontSize(t.FontSizeCaption)
            .RowHeight(22)
            .Gap(1)
            .LabelColor(t.TextSecondary)
            .Open())
        {
            foreach (var prop in properties)
            {
                if (IsCollection(prop.PropertyType))
                {
                    continue;
                }

                var propId = prefix + "_" + prop.Name;
                RenderProperty(ui, ctx, grid, propId, prop, instance, editorVm, onChanged);
            }
        }

        foreach (var prop in properties)
        {
            if (!IsCollection(prop.PropertyType))
            {
                continue;
            }

            var propId = prefix + "_" + prop.Name;
            if (IsDictionary(prop.PropertyType))
            {
                RenderDictionaryEditor(ui, ctx, propId, prop, instance, editorVm, onChanged);
            }
            else
            {
                RenderListEditor(ui, ctx, propId, prop, instance, editorVm, onChanged);
            }
        }
    }

    private static void RenderProperty(UiFrame ui, UiContext ctx, UiPropertyGridScope grid,
        string id, PropertyInfo prop, object instance, EditorViewModel editorVm, Action? onChanged)
    {
        var t = EditorTheme.Current;
        var propType = prop.PropertyType;
        var canWrite = prop.CanWrite && prop.GetSetMethod() != null
                      && prop.GetCustomAttribute<ReadOnlyAttribute>() == null;

        using var row = grid.Row(FormatPropertyName(prop.Name));

        if (propType == typeof(string) && prop.GetCustomAttribute<AssetRefAttribute>() != null)
        {
            RenderAssetRefEditor(ui, ctx, id, prop, instance, editorVm, onChanged);
        }
        else if (propType == typeof(string))
        {
            RenderStringEditor(ctx, id, prop, instance, canWrite, editorVm, onChanged);
        }
        else if (propType == typeof(float))
        {
            RenderFloatEditor(ctx, id, prop, instance, canWrite, editorVm, onChanged);
        }
        else if (propType == typeof(double))
        {
            RenderDoubleEditor(ctx, id, prop, instance, canWrite, editorVm, onChanged);
        }
        else if (propType == typeof(int))
        {
            RenderIntEditor(ctx, id, prop, instance, canWrite, editorVm, onChanged);
        }
        else if (propType == typeof(uint))
        {
            RenderUIntEditor(ctx, id, prop, instance, canWrite, editorVm, onChanged);
        }
        else if (propType == typeof(bool))
        {
            RenderBoolEditor(ctx, id, prop, instance, canWrite, editorVm, onChanged);
        }
        else if (propType == typeof(Vector2))
        {
            RenderVector2Editor(ctx, id, prop, instance, canWrite, editorVm, onChanged);
        }
        else if (propType == typeof(Vector3) && prop.GetCustomAttribute<ColorAttribute>() != null)
        {
            RenderColor3Editor(ctx, id, prop, instance, canWrite, editorVm, onChanged);
        }
        else if (propType == typeof(Vector4) && prop.GetCustomAttribute<ColorAttribute>() != null)
        {
            RenderColor4Editor(ctx, id, prop, instance, canWrite, editorVm, onChanged);
        }
        else if (propType == typeof(Vector3))
        {
            RenderVector3Editor(ctx, id, prop, instance, canWrite, editorVm, onChanged);
        }
        else if (propType == typeof(Vector4))
        {
            RenderVector4Editor(ctx, id, prop, instance, canWrite, editorVm, onChanged);
        }
        else if (propType.IsEnum)
        {
            RenderEnumEditor(ctx, id, prop, instance, canWrite, editorVm, onChanged);
        }
        else if (prop.GetCustomAttribute<AnimationSelectorAttribute>() != null)
        {
            RenderAnimationSelectorEditor(ctx, id, prop, instance, editorVm, onChanged);
        }
        else if (prop.GetCustomAttribute<AssetRefAttribute>() != null)
        {
            RenderAssetRefEditor(ui, ctx, id, prop, instance, editorVm, onChanged);
        }
        else
        {
            var value = prop.GetValue(instance);
            ui.Text(value?.ToString() ?? "(null)", new UiTextStyle { Color = t.TextMuted, FontSize = t.FontSizeCaption });
        }
    }

    private static void RenderStringEditor(UiContext ctx, string id, PropertyInfo prop,
        object instance, bool canWrite, EditorViewModel editorVm, Action? onChanged)
    {
        var t = EditorTheme.Current;
        var oldValue = prop.GetValue(instance)?.ToString() ?? "";
        var value = oldValue;

        var changed = Ui.TextField(ctx, id, ref value)
            .BackgroundColor(t.InputBackground, t.InputBackgroundFocused)
            .TextColor(t.TextPrimary)
            .BorderColor(t.Border, t.Accent)
            .FontSize(t.FontSizeCaption)
            .CornerRadius(t.RadiusSmall)
            .Padding(4, 3)
            .GrowWidth()
            .ReadOnly(!canWrite)
            .Show(ref value);

        if (changed && canWrite)
        {
            prop.SetValue(instance, value);
            editorVm.UndoSystem.Execute(
                new PropertyChangeAction(instance, prop, oldValue, value),
                $"Prop_String_{prop.Name}");
            onChanged?.Invoke();
        }
    }

    private static void RenderAnimationSelectorEditor(UiContext ctx, string id, PropertyInfo prop,
        object instance, EditorViewModel editorVm, Action? onChanged)
    {
        var t = EditorTheme.Current;
        var attr = prop.GetCustomAttribute<AnimationSelectorAttribute>()!;

        // Resolve the skeleton property to get available animation names
        var skeletonProp = instance.GetType().GetProperty(attr.SkeletonPropertyName,
            BindingFlags.Public | BindingFlags.Instance);
        var skeleton = skeletonProp?.GetValue(instance) as Skeleton;
        var animNames = skeleton?.AnimationNames;

        if (animNames == null || animNames.Count == 0)
        {
            var value = prop.GetValue(instance)?.ToString() ?? "(no skeleton)";
            Ui.TextField(ctx, id, ref value)
                .BackgroundColor(t.InputBackground, t.InputBackgroundFocused)
                .TextColor(t.TextMuted)
                .BorderColor(t.Border, t.Accent)
                .FontSize(t.FontSizeCaption)
                .CornerRadius(t.RadiusSmall)
                .Padding(4, 3)
                .GrowWidth()
                .ReadOnly(true)
                .Show(ref value);
            return;
        }

        var names = new string[animNames.Count];
        for (var i = 0; i < animNames.Count; i++)
        {
            names[i] = animNames[i];
        }

        var oldValue = prop.GetValue(instance)?.ToString() ?? "";
        var selectedIndex = Array.IndexOf(names, oldValue);
        if (selectedIndex < 0)
        {
            selectedIndex = 0;
        }

        if (Ui.Dropdown(ctx, id, names)
            .Background(t.SurfaceInset, t.Hover)
            .TextColor(t.TextPrimary)
            .FontSize(t.FontSizeCaption)
            .CornerRadius(t.RadiusSmall)
            .Padding(4, 3)
            .GrowWidth()
            .ItemHoverColor(t.Hover)
            .DropdownBackground(t.PanelBackground)
            .Placeholder("Select animation...")
            .Show(ref selectedIndex))
        {
            var newValue = names[selectedIndex];
            if (newValue != oldValue)
            {
                prop.SetValue(instance, newValue);
                editorVm.UndoSystem.Execute(
                    new PropertyChangeAction(instance, prop, oldValue, newValue),
                    $"Prop_AnimSel_{prop.Name}");
                onChanged?.Invoke();
            }
        }
    }

    private static void RenderFloatEditor(UiContext ctx, string id, PropertyInfo prop,
        object instance, bool canWrite, EditorViewModel editorVm, Action? onChanged)
    {
        var t = EditorTheme.Current;
        var oldValue = prop.GetValue(instance) ?? 0f;
        var value = (float)oldValue;
        var rangeAttr = prop.GetCustomAttribute<RangeAttribute>();

        bool changed;
        if (rangeAttr != null)
        {
            changed = Ui.Slider(ctx, id)
                .Range(rangeAttr.Min, rangeAttr.Max)
                .TrackColor(t.SurfaceInset)
                .FillColor(t.Accent)
                .ThumbColor(t.TextPrimary, t.Accent)
                .ShowValue(true, "F2")
                .ValueColor(t.TextSecondary)
                .FontSize(t.FontSizeCaption)
                .GrowWidth()
                .Show(ref value);
        }
        else
        {
            changed = Ui.DraggableValue(ctx, id)
                .LabelWidth(0)
                .Sensitivity(0.01f)
                .Format("F3")
                .FontSize(t.FontSizeCaption)
                .Width(UiSizing.Grow())
                .ValueColor(t.InputBackground)
                .ValueTextColor(t.InputText)
                .Show(ref value);
        }

        if (changed && canWrite)
        {
            prop.SetValue(instance, value);
            editorVm.UndoSystem.Execute(
                new PropertyChangeAction(instance, prop, oldValue, value),
                $"Prop_Float_{prop.Name}");
            onChanged?.Invoke();
        }
    }

    private static void RenderDoubleEditor(UiContext ctx, string id, PropertyInfo prop,
        object instance, bool canWrite, EditorViewModel editorVm, Action? onChanged)
    {
        var t = EditorTheme.Current;
        var oldValue = prop.GetValue(instance) ?? 0.0;
        var floatValue = (float)(double)oldValue;

        var changed = Ui.DraggableValue(ctx, id)
            .LabelWidth(0)
            .Sensitivity(0.01f)
            .Format("F3")
            .FontSize(t.FontSizeCaption)
            .Width(UiSizing.Grow())
            .ValueColor(t.InputBackground)
            .ValueTextColor(t.InputText)
            .Show(ref floatValue);

        if (changed && canWrite)
        {
            var newValue = (double)floatValue;
            prop.SetValue(instance, newValue);
            editorVm.UndoSystem.Execute(
                new PropertyChangeAction(instance, prop, oldValue, newValue),
                $"Prop_Double_{prop.Name}");
            onChanged?.Invoke();
        }
    }

    private static void RenderIntEditor(UiContext ctx, string id, PropertyInfo prop,
        object instance, bool canWrite, EditorViewModel editorVm, Action? onChanged)
    {
        var t = EditorTheme.Current;
        var oldValue = prop.GetValue(instance) ?? 0;
        var floatValue = (float)(int)oldValue;

        var changed = Ui.DraggableValue(ctx, id)
            .LabelWidth(0)
            .Sensitivity(1f)
            .Format("F0")
            .FontSize(t.FontSizeCaption)
            .Width(UiSizing.Grow())
            .ValueColor(t.InputBackground)
            .ValueTextColor(t.InputText)
            .Show(ref floatValue);

        if (changed && canWrite)
        {
            var newValue = (int)floatValue;
            prop.SetValue(instance, newValue);
            editorVm.UndoSystem.Execute(
                new PropertyChangeAction(instance, prop, oldValue, newValue),
                $"Prop_Int_{prop.Name}");
            onChanged?.Invoke();
        }
    }

    private static void RenderUIntEditor(UiContext ctx, string id, PropertyInfo prop,
        object instance, bool canWrite, EditorViewModel editorVm, Action? onChanged)
    {
        var t = EditorTheme.Current;
        var oldValue = prop.GetValue(instance) ?? 0u;
        var floatValue = (float)(uint)oldValue;

        var changed = Ui.DraggableValue(ctx, id)
            .LabelWidth(0)
            .Sensitivity(1f)
            .Format("F0")
            .FontSize(t.FontSizeCaption)
            .Width(UiSizing.Grow())
            .ValueColor(t.InputBackground)
            .ValueTextColor(t.InputText)
            .Show(ref floatValue);

        if (changed && canWrite)
        {
            var newValue = (uint)Math.Max(0, floatValue);
            prop.SetValue(instance, newValue);
            editorVm.UndoSystem.Execute(
                new PropertyChangeAction(instance, prop, oldValue, newValue),
                $"Prop_UInt_{prop.Name}");
            onChanged?.Invoke();
        }
    }

    private static void RenderBoolEditor(UiContext ctx, string id, PropertyInfo prop,
        object instance, bool canWrite, EditorViewModel editorVm, Action? onChanged)
    {
        var t = EditorTheme.Current;
        var value = (bool)(prop.GetValue(instance) ?? false);

        var checkColor = canWrite ? t.Accent : t.TextDisabled;
        var boxHover = canWrite ? t.Hover : t.SurfaceInset;
        var newValue = Ui.Checkbox(ctx, id, "", value)
            .BoxColor(t.SurfaceInset, boxHover)
            .CheckColor(checkColor)
            .BorderColor(t.Border)
            .BoxSize(14)
            .CornerRadius(t.RadiusSmall)
            .Show();

        if (newValue != value && canWrite)
        {
            prop.SetValue(instance, newValue);
            editorVm.UndoSystem.Execute(
                new PropertyChangeAction(instance, prop, value, newValue));
            onChanged?.Invoke();
        }
    }

    private static void RenderVector2Editor(UiContext ctx, string id, PropertyInfo prop,
        object instance, bool canWrite, EditorViewModel editorVm, Action? onChanged)
    {
        var t = EditorTheme.Current;
        var oldValue = prop.GetValue(instance) ?? Vector2.Zero;
        var value = (Vector2)oldValue;
        var x = value.X;
        var y = value.Y;

        if (Ui.Vec2Editor(ctx, id, ref x, ref y, 0.1f, "F2",
            t.AxisX, t.AxisY, t.InputBackground, t.InputBackgroundFocused, t.InputText) && canWrite)
        {
            var newValue = new Vector2(x, y);
            prop.SetValue(instance, newValue);
            editorVm.UndoSystem.Execute(
                new PropertyChangeAction(instance, prop, oldValue, newValue),
                $"Prop_Vec2_{prop.Name}");
            onChanged?.Invoke();
        }
    }

    private static void RenderVector3Editor(UiContext ctx, string id, PropertyInfo prop,
        object instance, bool canWrite, EditorViewModel editorVm, Action? onChanged)
    {
        var t = EditorTheme.Current;
        var oldValue = prop.GetValue(instance) ?? Vector3.Zero;
        var value = (Vector3)oldValue;
        var x = value.X;
        var y = value.Y;
        var z = value.Z;

        if (Ui.Vec3Editor(ctx, id, ref x, ref y, ref z, 0.1f, "F2",
            t.AxisX, t.AxisY, t.AxisZ, t.InputBackground, t.InputBackgroundFocused, t.InputText) && canWrite)
        {
            var newValue = new Vector3(x, y, z);
            prop.SetValue(instance, newValue);
            editorVm.UndoSystem.Execute(
                new PropertyChangeAction(instance, prop, oldValue, newValue),
                $"Prop_Vec3_{prop.Name}");
            onChanged?.Invoke();
        }
    }

    private static void RenderVector4Editor(UiContext ctx, string id, PropertyInfo prop,
        object instance, bool canWrite, EditorViewModel editorVm, Action? onChanged)
    {
        var t = EditorTheme.Current;
        var oldValue = prop.GetValue(instance) ?? Vector4.Zero;
        var value = (Vector4)oldValue;
        var x = value.X;
        var y = value.Y;
        var z = value.Z;
        var w = value.W;

        if (Ui.Vec4Editor(ctx, id, ref x, ref y, ref z, ref w, 0.1f, "F2",
            t.AxisX, t.AxisY, t.AxisZ, UiColor.Rgb(200, 180, 50),
            t.InputBackground, t.InputBackgroundFocused, t.InputText) && canWrite)
        {
            var newValue = new Vector4(x, y, z, w);
            prop.SetValue(instance, newValue);
            editorVm.UndoSystem.Execute(
                new PropertyChangeAction(instance, prop, oldValue, newValue),
                $"Prop_Vec4_{prop.Name}");
            onChanged?.Invoke();
        }
    }

    private static void RenderColor3Editor(UiContext ctx, string id, PropertyInfo prop,
        object instance, bool canWrite, EditorViewModel editorVm, Action? onChanged)
    {
        var t = EditorTheme.Current;
        var oldValue = prop.GetValue(instance) ?? Vector3.Zero;
        var value = (Vector3)oldValue;
        var r = value.X;
        var g = value.Y;
        var b = value.Z;

        var changed = Ui.ColorPicker(ctx, id)
            .FontSize(t.FontSizeCaption)
            .CornerRadius(t.RadiusSmall)
            .BorderColor(t.Border)
            .PanelBackground(t.PanelBackground)
            .LabelColor(t.TextSecondary)
            .ValueTextColor(t.TextMuted)
            .GrowWidth()
            .Show(ref r, ref g, ref b);

        if (changed && canWrite)
        {
            var newValue = new Vector3(r, g, b);
            prop.SetValue(instance, newValue);
            editorVm.UndoSystem.Execute(
                new PropertyChangeAction(instance, prop, oldValue, newValue),
                $"Prop_Color3_{prop.Name}");
            onChanged?.Invoke();
        }
    }

    private static void RenderColor4Editor(UiContext ctx, string id, PropertyInfo prop,
        object instance, bool canWrite, EditorViewModel editorVm, Action? onChanged)
    {
        var t = EditorTheme.Current;
        var oldValue = prop.GetValue(instance) ?? Vector4.Zero;
        var value = (Vector4)oldValue;
        var r = value.X;
        var g = value.Y;
        var b = value.Z;
        var a = value.W;

        var changed = Ui.ColorPicker(ctx, id)
            .HasAlpha(true)
            .FontSize(t.FontSizeCaption)
            .CornerRadius(t.RadiusSmall)
            .BorderColor(t.Border)
            .PanelBackground(t.PanelBackground)
            .LabelColor(t.TextSecondary)
            .ValueTextColor(t.TextMuted)
            .GrowWidth()
            .Show(ref r, ref g, ref b, ref a);

        if (changed && canWrite)
        {
            var newValue = new Vector4(r, g, b, a);
            prop.SetValue(instance, newValue);
            editorVm.UndoSystem.Execute(
                new PropertyChangeAction(instance, prop, oldValue, newValue),
                $"Prop_Color4_{prop.Name}");
            onChanged?.Invoke();
        }
    }

    private static void RenderEnumEditor(UiContext ctx, string id, PropertyInfo prop,
        object instance, bool canWrite, EditorViewModel editorVm, Action? onChanged)
    {
        var t = EditorTheme.Current;
        var propType = prop.PropertyType;
        var names = Enum.GetNames(propType);
        var oldValue = prop.GetValue(instance);
        var selectedIndex = oldValue != null ? Array.IndexOf(Enum.GetValues(propType), oldValue) : 0;

        if (Ui.Dropdown(ctx, id, names)
            .Background(t.SurfaceInset, t.Hover)
            .TextColor(t.TextPrimary)
            .FontSize(t.FontSizeCaption)
            .CornerRadius(t.RadiusSmall)
            .Padding(4, 3)
            .GrowWidth()
            .ItemHoverColor(t.Hover)
            .DropdownBackground(t.PanelBackground)
            .Show(ref selectedIndex) && canWrite)
        {
            var values = Enum.GetValues(propType);
            var newValue = values.GetValue(selectedIndex);
            prop.SetValue(instance, newValue);
            editorVm.UndoSystem.Execute(
                new PropertyChangeAction(instance, prop, oldValue, newValue));
            onChanged?.Invoke();
        }
    }

    private static void RenderAssetRefEditor(UiFrame ui, UiContext ctx, string id,
        PropertyInfo prop, object instance, EditorViewModel editorVm, Action? onChanged)
    {
        var t = EditorTheme.Current;
        var assetRefAttr = prop.GetCustomAttribute<AssetRefAttribute>()!;
        var currentValue = prop.GetValue(instance);
        var assetPath = GetAssetPath(currentValue);
        var hasAsset = !string.IsNullOrEmpty(assetPath);
        var rawText = hasAsset ? assetPath! : "(none)";

        var buttonId = ctx.GetElementId(id);
        var bbox = ctx.Clay.GetElementBoundingBox(buttonId);
        var availWidth = bbox.Width > 12 ? bbox.Width - 12 : 120;
        var displayText = TruncateToFit(ctx.Clay, rawText, t.FontSizeCaption, availWidth);

        if (Ui.Button(ctx, id, displayText)
            .Color(t.SurfaceInset, t.Hover, t.Active)
            .TextColor(t.TextPrimary)
            .FontSize(t.FontSizeCaption)
            .CornerRadius(t.RadiusSmall)
            .Padding(4, 3)
            .GrowWidth()
            .Show())
        {
            var oldAssetValue = prop.GetValue(instance);
            editorVm.OpenAssetPicker(assetRefAttr.AssetType, assetPath, asset =>
            {
                if (asset != null && prop.CanWrite)
                {
                    if (prop.PropertyType == typeof(string))
                    {
                        prop.SetValue(instance, asset.Path);
                        editorVm.UndoSystem.Execute(
                            new PropertyChangeAction(instance, prop, oldAssetValue, asset.Path));
                    }
                    else
                    {
                        var resolved = editorVm.AssetBrowser.ResolveAsset(assetRefAttr.AssetType, asset.Path);
                        if (resolved != null)
                        {
                            prop.SetValue(instance, resolved);
                            editorVm.UndoSystem.Execute(
                                new PropertyChangeAction(instance, prop, oldAssetValue, resolved));
                        }
                    }
                    onChanged?.Invoke();
                }
            });
        }

        if (hasAsset && prop.CanWrite)
        {
            if (EditorUi.IconButton(ctx, id + "_Clear", FontAwesome.Xmark))
            {
                var oldAssetValue = prop.GetValue(instance);
                prop.SetValue(instance, null);
                editorVm.UndoSystem.Execute(
                    new PropertyChangeAction(instance, prop, oldAssetValue, null));
                onChanged?.Invoke();
            }
        }
    }

    private static bool IsCollection(Type type)
    {
        if (type.IsArray)
        {
            return true;
        }

        if (IsDictionary(type))
        {
            return true;
        }

        if (!type.IsGenericType)
        {
            return false;
        }

        var genericDef = type.GetGenericTypeDefinition();
        return genericDef == typeof(List<>)
               || genericDef == typeof(IReadOnlyList<>)
               || genericDef == typeof(IList<>);
    }

    private static bool IsDictionary(Type type)
    {
        return typeof(IDictionary).IsAssignableFrom(type);
    }

    private static (Type keyType, Type valueType) GetDictionaryKeyValueTypes(Type type)
    {
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>))
        {
            var args = type.GetGenericArguments();
            return (args[0], args[1]);
        }

        foreach (var iface in type.GetInterfaces())
        {
            if (iface.IsGenericType && iface.GetGenericTypeDefinition() == typeof(IDictionary<,>))
            {
                var args = iface.GetGenericArguments();
                return (args[0], args[1]);
            }
        }

        return (typeof(object), typeof(object));
    }

    private static Type? GetCollectionElementType(Type type)
    {
        if (type.IsArray)
        {
            return type.GetElementType();
        }

        if (type.IsGenericType)
        {
            return type.GetGenericArguments()[0];
        }

        foreach (var iface in type.GetInterfaces())
        {
            if (iface.IsGenericType && iface.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            {
                return iface.GetGenericArguments()[0];
            }
        }

        return null;
    }

    private static (string[] items, int count) CollectItems(object collection)
    {
        if (collection is IList list)
        {
            var items = new string[list.Count];
            for (var i = 0; i < list.Count; i++)
            {
                items[i] = list[i]?.ToString() ?? "(null)";
            }

            return (items, list.Count);
        }

        if (collection is IEnumerable enumerable)
        {
            var result = new List<string>();
            foreach (var item in enumerable)
            {
                result.Add(item?.ToString() ?? "(null)");
            }

            return (result.ToArray(), result.Count);
        }

        return ([], 0);
    }

    private static object[] SnapshotList(IList list)
    {
        var snapshot = new object[list.Count];
        for (var i = 0; i < list.Count; i++)
        {
            snapshot[i] = list[i]!;
        }

        return snapshot;
    }

    private static AssetRefType? GetListElementAssetRefType(Type elementType)
    {
        if (elementType == typeof(AnimationEntry))
        {
            return AssetRefType.Animation;
        }

        foreach (var p in elementType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            var attr = p.GetCustomAttribute<AssetRefAttribute>();
            if (attr != null)
            {
                return attr.AssetType;
            }
        }

        return null;
    }

    private static void RenderListEditor(UiFrame ui, UiContext ctx, string id,
        PropertyInfo prop, object instance, EditorViewModel editorVm, Action? onChanged)
    {
        var t = EditorTheme.Current;
        var value = prop.GetValue(instance);
        if (value == null)
        {
            return;
        }

        var elementType = GetCollectionElementType(prop.PropertyType);
        if (elementType == null)
        {
            return;
        }

        var mutableList = value as IList;
        var declaredType = prop.PropertyType;
        var isMutableType = declaredType.IsGenericType && declaredType.GetGenericTypeDefinition() == typeof(List<>);
        var isMutable = mutableList != null && isMutableType;
        var (items, count) = CollectItems(value);
        var assetRefType = isMutable ? GetListElementAssetRefType(elementType) : null;
        var title = FormatPropertyName(prop.Name);

        var selectedIndex = -1;
        var listId = id + "_List";

        var contentHeight = 32 + count * 26;
        var listHeight = Math.Clamp(contentHeight, 60, 200);

        var editor = Ui.ListEditor(ctx, listId)
            .Title(title)
            .Background(t.SurfaceInset)
            .SelectedColor(UiColor.Rgb(50, 80, 120))
            .HoverColor(t.Hover)
            .TextColor(t.TextPrimary)
            .FontSize(t.FontSizeCaption)
            .ItemHeight(26)
            .Width(UiSizing.Grow())
            .Height(UiSizing.Fixed(listHeight))
            .ShowAdd(isMutable)
            .ShowRemove(isMutable);

        if (assetRefType.HasValue)
        {
            editor = editor.ItemAction(FontAwesome.FolderOpen, t.Accent, t.Hover);
        }

        var result = editor.Show(items, ref selectedIndex);

        if (!isMutable || mutableList == null)
        {
            return;
        }

        if (result.Added)
        {
            if (assetRefType.HasValue)
            {
                var oldSnapshot = SnapshotList(mutableList);
                editorVm.OpenAssetPicker(assetRefType.Value, null, asset =>
                {
                    if (asset == null)
                    {
                        return;
                    }

                    var newEntry = CreateEntryFromAsset(elementType, asset);
                    if (newEntry == null)
                    {
                        return;
                    }

                    mutableList.Add(newEntry);
                    editorVm.UndoSystem.Execute(
                        new ListChangeAction(mutableList, oldSnapshot, SnapshotList(mutableList), $"Add {title} Entry"));
                    onChanged?.Invoke();
                });
            }
            else
            {
                var oldSnapshot = SnapshotList(mutableList);
                var newItem = elementType == typeof(string) ? (object)"" : Activator.CreateInstance(elementType);
                mutableList.Add(newItem);
                editorVm.UndoSystem.Execute(
                    new ListChangeAction(mutableList, oldSnapshot, SnapshotList(mutableList), $"Add {title} Entry"));
                onChanged?.Invoke();
            }
        }

        if (result.Removed && result.RemovedIndex >= 0 && result.RemovedIndex < mutableList.Count)
        {
            var oldSnapshot = SnapshotList(mutableList);
            mutableList.RemoveAt(result.RemovedIndex);
            editorVm.UndoSystem.Execute(
                new ListChangeAction(mutableList, oldSnapshot, SnapshotList(mutableList), $"Remove {title} Entry"));
            onChanged?.Invoke();
        }

        if (result.ActionClicked && result.ActionClickedIndex >= 0
            && result.ActionClickedIndex < mutableList.Count && assetRefType.HasValue)
        {
            var actionIndex = result.ActionClickedIndex;
            var oldSnapshot = SnapshotList(mutableList);
            var currentSource = GetEntrySourcePath(mutableList[actionIndex]);

            editorVm.OpenAssetPicker(assetRefType.Value, currentSource, asset =>
            {
                if (asset == null)
                {
                    return;
                }

                var updated = UpdateEntryFromAsset(elementType, mutableList[actionIndex], asset);
                if (updated != null)
                {
                    mutableList[actionIndex] = updated;
                }

                editorVm.UndoSystem.Execute(
                    new ListChangeAction(mutableList, oldSnapshot, SnapshotList(mutableList), $"Change {title} Source"));
                onChanged?.Invoke();
            });
        }
    }

    private static KeyValuePair<object, object>[] SnapshotDictionary(IDictionary dict)
    {
        var snapshot = new KeyValuePair<object, object>[dict.Count];
        var i = 0;
        foreach (DictionaryEntry entry in dict)
        {
            snapshot[i++] = new KeyValuePair<object, object>(entry.Key, entry.Value!);
        }
        return snapshot;
    }

    private static void RenderDictionaryEditor(UiFrame ui, UiContext ctx, string id,
        PropertyInfo prop, object instance, EditorViewModel editorVm, Action? onChanged)
    {
        var t = EditorTheme.Current;
        var value = prop.GetValue(instance);
        if (value is not IDictionary dict)
        {
            return;
        }

        var title = FormatPropertyName(prop.Name);

        var entryKeys = new string[dict.Count];
        var entryValues = new string[dict.Count];
        var idx = 0;
        foreach (DictionaryEntry entry in dict)
        {
            entryKeys[idx] = entry.Key.ToString() ?? "";
            entryValues[idx] = entry.Value?.ToString() ?? "";
            idx++;
        }

        var count = entryKeys.Length;

        using (ui.Panel(id + "_DictC")
            .Vertical()
            .GrowWidth()
            .FitHeight()
            .Background(t.SurfaceInset)
            .Border(1, UiColor.Rgb(55, 55, 60))
            .CornerRadius(4)
            .Open())
        {
            using (ui.Panel(id + "_DictH")
                .Horizontal()
                .GrowWidth()
                .FixedHeight(30)
                .Padding(8, 4, 4, 4)
                .Gap(4)
                .AlignChildrenY(UiAlignY.Center)
                .Background(UiColor.Rgb(40, 40, 45))
                .Open())
            {
                ui.Text(title, new UiTextStyle { Color = t.TextPrimary, FontSize = t.FontSizeCaption });

                using (ui.Panel(id + "_DictSpc").GrowWidth().Open()) { }

                var addBtn = ui.Panel(id + "_DictAdd")
                    .FixedWidth(22)
                    .FixedHeight(22)
                    .CenterChildren()
                    .CornerRadius(3);

                if (addBtn.IsHovered())
                {
                    addBtn = addBtn.Background(UiColor.Rgb(60, 60, 65));
                }

                using (addBtn.Open())
                {
                    ui.Icon(FontAwesome.Plus, UiColor.Rgb(100, 200, 100), 12);
                }

                if (addBtn.WasClicked())
                {
                    var oldSnapshot = SnapshotDictionary(dict);
                    var (keyType, valueType) = GetDictionaryKeyValueTypes(prop.PropertyType);

                    object newKey;
                    if (keyType == typeof(string))
                    {
                        var baseKey = "NewKey";
                        var suffix = 0;
                        var candidate = baseKey;
                        while (dict.Contains(candidate))
                        {
                            candidate = baseKey + (++suffix);
                        }
                        newKey = candidate;
                    }
                    else
                    {
                        newKey = Activator.CreateInstance(keyType)!;
                    }

                    var newVal = valueType == typeof(string) ? (object)"" : Activator.CreateInstance(valueType)!;
                    dict[newKey] = newVal;
                    editorVm.UndoSystem.Execute(
                        new DictionaryChangeAction(dict, oldSnapshot, SnapshotDictionary(dict), $"Add {title} Entry"));
                    onChanged?.Invoke();
                }
            }

            for (var i = 0; i < count; i++)
            {
                using (ui.Panel(id + "_DR" + i)
                    .Horizontal()
                    .GrowWidth()
                    .FixedHeight(26)
                    .Padding(4, 0)
                    .Gap(4)
                    .AlignChildrenY(UiAlignY.Center)
                    .Open())
                {
                    var keyFieldId = id + "_DK" + i;
                    var keyVal = entryKeys[i];
                    var oldKey = keyVal;

                    var keyChanged = Ui.TextField(ctx, keyFieldId, ref keyVal)
                        .BackgroundColor(t.InputBackground, t.InputBackgroundFocused)
                        .TextColor(t.TextPrimary)
                        .BorderColor(t.Border, t.Accent)
                        .FontSize(t.FontSizeCaption)
                        .CornerRadius(t.RadiusSmall)
                        .Padding(4, 2)
                        .Width(UiSizing.Percent(0.4f))
                        .Show(ref keyVal);

                    if (keyChanged && keyVal != oldKey && !dict.Contains(keyVal))
                    {
                        var oldSnapshot = SnapshotDictionary(dict);
                        var existingValue = dict[oldKey];
                        dict.Remove(oldKey);
                        dict[keyVal] = existingValue;
                        editorVm.UndoSystem.Execute(
                            new DictionaryChangeAction(dict, oldSnapshot, SnapshotDictionary(dict),
                                $"Rename {title} Key"));
                        onChanged?.Invoke();
                    }

                    var valFieldId = id + "_DV" + i;
                    var valVal = entryValues[i];
                    var oldVal = valVal;

                    var valChanged = Ui.TextField(ctx, valFieldId, ref valVal)
                        .BackgroundColor(t.InputBackground, t.InputBackgroundFocused)
                        .TextColor(t.TextPrimary)
                        .BorderColor(t.Border, t.Accent)
                        .FontSize(t.FontSizeCaption)
                        .CornerRadius(t.RadiusSmall)
                        .Padding(4, 2)
                        .GrowWidth()
                        .Show(ref valVal);

                    if (valChanged && valVal != oldVal)
                    {
                        var oldSnapshot = SnapshotDictionary(dict);
                        dict[entryKeys[i]] = valVal;
                        editorVm.UndoSystem.Execute(
                            new DictionaryChangeAction(dict, oldSnapshot, SnapshotDictionary(dict),
                                $"Change {title} Value"));
                        onChanged?.Invoke();
                    }

                    var removeBtn = ui.Panel(id + "_DRm" + i)
                        .FixedWidth(20)
                        .FixedHeight(20)
                        .CenterChildren()
                        .CornerRadius(3);

                    if (removeBtn.IsHovered())
                    {
                        removeBtn = removeBtn.Background(UiColor.Rgb(60, 60, 65));
                    }

                    using (removeBtn.Open())
                    {
                        ui.Icon(FontAwesome.Minus, UiColor.Rgb(200, 100, 100), 12);
                    }

                    if (removeBtn.WasClicked())
                    {
                        var oldSnapshot = SnapshotDictionary(dict);
                        dict.Remove(entryKeys[i]);
                        editorVm.UndoSystem.Execute(
                            new DictionaryChangeAction(dict, oldSnapshot, SnapshotDictionary(dict),
                                $"Remove {title} Entry"));
                        onChanged?.Invoke();
                    }
                }
            }
        }
    }

    private static object? CreateEntryFromAsset(Type elementType, AssetInfo asset)
    {
        if (elementType == typeof(AnimationEntry))
        {
            var fileName = Path.GetFileNameWithoutExtension(asset.Name);
            return AnimationEntry.External(fileName, asset.Path);
        }

        var instance = Activator.CreateInstance(elementType);
        if (instance == null)
        {
            return null;
        }

        foreach (var p in elementType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (!p.CanWrite)
            {
                continue;
            }

            var attr = p.GetCustomAttribute<AssetRefAttribute>();
            if (attr != null)
            {
                p.SetValue(instance, asset.Path);
                break;
            }
        }

        return instance;
    }

    private static object? UpdateEntryFromAsset(Type elementType, object? existing, AssetInfo asset)
    {
        if (elementType == typeof(AnimationEntry))
        {
            var fileName = Path.GetFileNameWithoutExtension(asset.Name);
            return AnimationEntry.External(fileName, asset.Path);
        }

        if (existing == null)
        {
            return CreateEntryFromAsset(elementType, asset);
        }

        foreach (var p in elementType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (!p.CanWrite)
            {
                continue;
            }

            var attr = p.GetCustomAttribute<AssetRefAttribute>();
            if (attr != null)
            {
                p.SetValue(existing, asset.Path);
                return existing;
            }
        }

        return existing;
    }

    private static string? GetEntrySourcePath(object? entry)
    {
        if (entry is AnimationEntry animEntry)
        {
            return animEntry.SourceRef;
        }

        if (entry == null)
        {
            return null;
        }

        foreach (var p in entry.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            var attr = p.GetCustomAttribute<AssetRefAttribute>();
            if (attr != null)
            {
                return p.GetValue(entry)?.ToString();
            }
        }

        return null;
    }

    private static string? GetAssetPath(object? asset)
    {
        var path = asset switch
        {
            string s => s,
            Mesh mesh => mesh.AssetPath,
            Skeleton skeleton => skeleton.AssetPath,
            Texture2d texture => texture.SourcePath,
            _ => null
        };
        return string.IsNullOrEmpty(path) ? null : path;
    }

    private static string FormatPropertyName(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return name;
        }

        var result = new System.Text.StringBuilder();
        result.Append(char.ToUpper(name[0]));

        for (var i = 1; i < name.Length; i++)
        {
            if (char.IsUpper(name[i]) && !char.IsUpper(name[i - 1]))
            {
                result.Append(' ');
            }
            result.Append(name[i]);
        }

        return result.ToString();
    }

    private static string TruncateToFit(Clay clay, string text, ushort fontSize, float maxWidth)
    {
        var measured = clay.MeasureText(text, 0, fontSize);
        if (measured.Width <= maxWidth)
        {
            return text;
        }

        var lastSlash = text.LastIndexOfAny(['/', '\\']);
        if (lastSlash >= 0)
        {
            var filename = text[(lastSlash + 1)..];
            var fileDims = clay.MeasureText(filename, 0, fontSize);
            if (fileDims.Width <= maxWidth)
            {
                return filename;
            }

            text = filename;
        }

        var ellipsis = "...";
        var ellipsisDims = clay.MeasureText(ellipsis, 0, fontSize);
        var remaining = maxWidth - ellipsisDims.Width;
        if (remaining <= 0)
        {
            return ellipsis;
        }

        var fitChars = clay.GetCharIndexAtOffset(text, remaining, 0, fontSize);
        if (fitChars > 0 && fitChars < text.Length)
        {
            return text[..(int)fitChars] + ellipsis;
        }

        return ellipsis;
    }
}
