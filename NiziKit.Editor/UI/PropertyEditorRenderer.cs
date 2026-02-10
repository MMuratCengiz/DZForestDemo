using System.Numerics;
using System.Reflection;
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
            .Where(p => p.CanRead && !SkippedProperties.Contains(p.Name) && !p.PropertyType.IsByRefLike)
            .OrderBy(p => p.MetadataToken);

        using var grid = Ui.PropertyGrid(ctx, prefix + "_PropGrid")
            .LabelWidth(90)
            .FontSize(t.FontSizeCaption)
            .RowHeight(26)
            .Gap(2)
            .LabelColor(t.TextSecondary)
            .Open();

        foreach (var prop in properties)
        {
            var propId = prefix + "_" + prop.Name;
            RenderProperty(ui, ctx, grid, propId, prop, instance, editorVm, onChanged);
        }
    }

    private static void RenderProperty(UiFrame ui, UiContext ctx, UiPropertyGridScope grid,
        string id, PropertyInfo prop, object instance, EditorViewModel editorVm, Action? onChanged)
    {
        var t = EditorTheme.Current;
        var propType = prop.PropertyType;
        var canWrite = prop.CanWrite;

        using var row = grid.Row(FormatPropertyName(prop.Name));

        if (propType == typeof(string))
        {
            RenderStringEditor(ctx, id, prop, instance, canWrite, onChanged);
        }
        else if (propType == typeof(float))
        {
            RenderFloatEditor(ctx, id, prop, instance, canWrite, onChanged);
        }
        else if (propType == typeof(double))
        {
            RenderDoubleEditor(ctx, id, prop, instance, canWrite, onChanged);
        }
        else if (propType == typeof(int))
        {
            RenderIntEditor(ctx, id, prop, instance, canWrite, onChanged);
        }
        else if (propType == typeof(uint))
        {
            RenderUIntEditor(ctx, id, prop, instance, canWrite, onChanged);
        }
        else if (propType == typeof(bool))
        {
            RenderBoolEditor(ctx, id, prop, instance, canWrite, onChanged);
        }
        else if (propType == typeof(Vector2))
        {
            RenderVector2Editor(ctx, id, prop, instance, canWrite, onChanged);
        }
        else if (propType == typeof(Vector3))
        {
            RenderVector3Editor(ctx, id, prop, instance, canWrite, onChanged);
        }
        else if (propType == typeof(Vector4))
        {
            RenderVector4Editor(ctx, id, prop, instance, canWrite, onChanged);
        }
        else if (propType.IsEnum)
        {
            RenderEnumEditor(ctx, id, prop, instance, canWrite, onChanged);
        }
        else if (prop.GetCustomAttribute<AssetRefAttribute>() != null)
        {
            RenderAssetRefEditor(ui, ctx, id, prop, instance, editorVm, onChanged);
        }
        else
        {
            // Read-only fallback
            var value = prop.GetValue(instance);
            ui.Text(value?.ToString() ?? "(null)", new UiTextStyle { Color = t.TextMuted, FontSize = t.FontSizeCaption });
        }
    }

    private static void RenderStringEditor(UiContext ctx, string id, PropertyInfo prop,
        object instance, bool canWrite, Action? onChanged)
    {
        var t = EditorTheme.Current;
        var value = prop.GetValue(instance)?.ToString() ?? "";

        var changed = Ui.TextField(ctx, id, ref value)
            .BackgroundColor(t.SurfaceInset, t.PanelElevated)
            .TextColor(t.TextPrimary)
            .BorderColor(t.Border, t.Accent)
            .FontSize(t.FontSizeCaption)
            .CornerRadius(t.RadiusSmall)
            .Padding(6, 4)
            .GrowWidth()
            .ReadOnly(!canWrite)
            .Show(ref value);

        if (changed && canWrite)
        {
            prop.SetValue(instance, value);
            onChanged?.Invoke();
        }
    }

    private static void RenderFloatEditor(UiContext ctx, string id, PropertyInfo prop,
        object instance, bool canWrite, Action? onChanged)
    {
        var t = EditorTheme.Current;
        var value = (float)(prop.GetValue(instance) ?? 0f);
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
            onChanged?.Invoke();
        }
    }

    private static void RenderDoubleEditor(UiContext ctx, string id, PropertyInfo prop,
        object instance, bool canWrite, Action? onChanged)
    {
        var t = EditorTheme.Current;
        var doubleValue = (double)(prop.GetValue(instance) ?? 0.0);
        var floatValue = (float)doubleValue;

        var changed = Ui.DraggableValue(ctx, id)
            .Sensitivity(0.01f)
            .Format("F3")
            .FontSize(t.FontSizeCaption)
            .Width(UiSizing.Grow())
            .ValueColor(t.InputBackground)
            .ValueTextColor(t.InputText)
            .Show(ref floatValue);

        if (changed && canWrite)
        {
            prop.SetValue(instance, (double)floatValue);
            onChanged?.Invoke();
        }
    }

    private static void RenderIntEditor(UiContext ctx, string id, PropertyInfo prop,
        object instance, bool canWrite, Action? onChanged)
    {
        var t = EditorTheme.Current;
        var intValue = (int)(prop.GetValue(instance) ?? 0);
        var floatValue = (float)intValue;

        var changed = Ui.DraggableValue(ctx, id)
            .Sensitivity(1f)
            .Format("F0")
            .FontSize(t.FontSizeCaption)
            .Width(UiSizing.Grow())
            .ValueColor(t.InputBackground)
            .ValueTextColor(t.InputText)
            .Show(ref floatValue);

        if (changed && canWrite)
        {
            prop.SetValue(instance, (int)floatValue);
            onChanged?.Invoke();
        }
    }

    private static void RenderUIntEditor(UiContext ctx, string id, PropertyInfo prop,
        object instance, bool canWrite, Action? onChanged)
    {
        var t = EditorTheme.Current;
        var uintValue = (uint)(prop.GetValue(instance) ?? 0u);
        var floatValue = (float)uintValue;

        var changed = Ui.DraggableValue(ctx, id)
            .Sensitivity(1f)
            .Format("F0")
            .FontSize(t.FontSizeCaption)
            .Width(UiSizing.Grow())
            .ValueColor(t.InputBackground)
            .ValueTextColor(t.InputText)
            .Show(ref floatValue);

        if (changed && canWrite)
        {
            prop.SetValue(instance, (uint)Math.Max(0, floatValue));
            onChanged?.Invoke();
        }
    }

    private static void RenderBoolEditor(UiContext ctx, string id, PropertyInfo prop,
        object instance, bool canWrite, Action? onChanged)
    {
        var t = EditorTheme.Current;
        var value = (bool)(prop.GetValue(instance) ?? false);

        var newValue = Ui.Checkbox(ctx, id, "", value)
            .BoxColor(t.SurfaceInset, t.Hover)
            .CheckColor(t.Accent)
            .BorderColor(t.Border)
            .BoxSize(14)
            .CornerRadius(t.RadiusSmall)
            .Show();

        if (newValue != value && canWrite)
        {
            prop.SetValue(instance, newValue);
            onChanged?.Invoke();
        }
    }

    private static void RenderVector2Editor(UiContext ctx, string id, PropertyInfo prop,
        object instance, bool canWrite, Action? onChanged)
    {
        var t = EditorTheme.Current;
        var value = (Vector2)(prop.GetValue(instance) ?? Vector2.Zero);
        var x = value.X;
        var y = value.Y;

        if (Ui.Vec2Editor(ctx, id, ref x, ref y, 0.1f, "F2",
            t.AxisX, t.AxisY, t.InputBackground, t.InputBackgroundFocused, t.InputText) && canWrite)
        {
            prop.SetValue(instance, new Vector2(x, y));
            onChanged?.Invoke();
        }
    }

    private static void RenderVector3Editor(UiContext ctx, string id, PropertyInfo prop,
        object instance, bool canWrite, Action? onChanged)
    {
        var t = EditorTheme.Current;
        var value = (Vector3)(prop.GetValue(instance) ?? Vector3.Zero);
        var x = value.X;
        var y = value.Y;
        var z = value.Z;

        if (Ui.Vec3Editor(ctx, id, ref x, ref y, ref z, 0.1f, "F2",
            t.AxisX, t.AxisY, t.AxisZ, t.InputBackground, t.InputBackgroundFocused, t.InputText) && canWrite)
        {
            prop.SetValue(instance, new Vector3(x, y, z));
            onChanged?.Invoke();
        }
    }

    private static void RenderVector4Editor(UiContext ctx, string id, PropertyInfo prop,
        object instance, bool canWrite, Action? onChanged)
    {
        var t = EditorTheme.Current;
        var value = (Vector4)(prop.GetValue(instance) ?? Vector4.Zero);
        var x = value.X;
        var y = value.Y;
        var z = value.Z;
        var w = value.W;

        if (Ui.Vec4Editor(ctx, id, ref x, ref y, ref z, ref w, 0.1f, "F2",
            t.AxisX, t.AxisY, t.AxisZ, UiColor.Rgb(200, 180, 50),
            t.InputBackground, t.InputBackgroundFocused, t.InputText) && canWrite)
        {
            prop.SetValue(instance, new Vector4(x, y, z, w));
            onChanged?.Invoke();
        }
    }

    private static void RenderEnumEditor(UiContext ctx, string id, PropertyInfo prop,
        object instance, bool canWrite, Action? onChanged)
    {
        var t = EditorTheme.Current;
        var propType = prop.PropertyType;
        var names = Enum.GetNames(propType);
        var currentValue = prop.GetValue(instance);
        var selectedIndex = currentValue != null ? Array.IndexOf(Enum.GetValues(propType), currentValue) : 0;

        if (Ui.Dropdown(ctx, id, names)
            .Background(t.SurfaceInset, t.Hover)
            .TextColor(t.TextPrimary)
            .FontSize(t.FontSizeCaption)
            .CornerRadius(t.RadiusSmall)
            .Padding(6, 4)
            .GrowWidth()
            .ItemHoverColor(t.Hover)
            .DropdownBackground(t.PanelBackground)
            .Show(ref selectedIndex) && canWrite)
        {
            var values = Enum.GetValues(propType);
            prop.SetValue(instance, values.GetValue(selectedIndex));
            onChanged?.Invoke();
        }
    }

    private static void RenderAssetRefEditor(UiFrame ui, UiContext ctx, string id,
        PropertyInfo prop, object instance, EditorViewModel editorVm, Action? onChanged)
    {
        var t = EditorTheme.Current;
        var assetRefAttr = prop.GetCustomAttribute<AssetRefAttribute>()!;
        var currentValue = prop.GetValue(instance);
        var displayText = currentValue?.ToString() ?? "(none)";

        if (Ui.Button(ctx, id, displayText)
            .Color(t.SurfaceInset, t.Hover, t.Active)
            .TextColor(t.TextPrimary)
            .FontSize(t.FontSizeCaption)
            .CornerRadius(t.RadiusSmall)
            .Padding(6, 4)
            .Show())
        {
            editorVm.OpenAssetPicker(assetRefAttr.AssetType, currentValue?.ToString(), asset =>
            {
                if (asset != null && prop.CanWrite)
                {
                    var resolved = editorVm.AssetBrowser.ResolveAsset(assetRefAttr.AssetType, asset.Path);
                    if (resolved != null)
                    {
                        prop.SetValue(instance, resolved);
                    }
                    onChanged?.Invoke();
                }
            });
        }
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
}
