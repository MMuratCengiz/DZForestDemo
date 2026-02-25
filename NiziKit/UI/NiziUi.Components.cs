using System.Runtime.CompilerServices;

namespace NiziKit.UI;

public static partial class NiziUi
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UiButton Button(string id, string text)
    {
        return new UiButton(_ctx, id, text);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UiCard Card(string id)
    {
        return new UiCard(_ctx, id);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UiCheckbox Checkbox(string id, string label, bool isChecked)
    {
        return new UiCheckbox(_ctx, id, label, isChecked);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UiSlider Slider(string id)
    {
        var elementId = _ctx.StringCache.GetId(id);
        var state = _ctx.GetOrCreateState<UiSliderState>(elementId);
        return new UiSlider(_ctx, id, state);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UiTextField TextField(string id, ref string text)
    {
        var elementId = _ctx.StringCache.GetId(id);
        var state = _ctx.GetOrCreateState<UiTextFieldState>(elementId);
        if (state.Text != text)
        {
            state.Text = text;
        }
        return new UiTextField(_ctx, id, state, ref text);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UiDropdown Dropdown(string id, string[] items)
    {
        var elementId = _ctx.StringCache.GetId(id);
        var state = _ctx.GetOrCreateState<UiDropdownState>(elementId);
        return new UiDropdown(_ctx, id, state, items);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UiDropdown Dropdown(string id, string[] items, int initialSelectedIndex)
    {
        var elementId = _ctx.StringCache.GetId(id);
        var state = _ctx.GetOrCreateState(() => new UiDropdownState { SelectedIndex = initialSelectedIndex }, elementId);
        return new UiDropdown(_ctx, id, state, items);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UiDraggableValue DraggableValue(string id)
    {
        var elementId = _ctx.StringCache.GetId(id);
        var state = _ctx.GetOrCreateState<UiDraggableValueState>(elementId);
        if (state.TextFieldId == null)
        {
            state.TextFieldId = id + "_EditTF";
        }
        return new UiDraggableValue(_ctx, id, state);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UiCollapsibleSection CollapsibleSection(string id, string title)
    {
        var elementId = _ctx.StringCache.GetId(id);
        var state = _ctx.GetOrCreateState<UiCollapsibleSectionState>(elementId);
        return new UiCollapsibleSection(_ctx, id, state, title);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UiCollapsibleSection CollapsibleSection(string id, string title, bool initialExpanded)
    {
        var elementId = _ctx.StringCache.GetId(id);
        var state = _ctx.GetOrCreateState(() => new UiCollapsibleSectionState { IsExpanded = initialExpanded }, elementId);
        return new UiCollapsibleSection(_ctx, id, state, title);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UiTreeView TreeView(string id, List<UiTreeNode> roots)
    {
        var elementId = _ctx.StringCache.GetId(id);
        var state = _ctx.GetOrCreateState<UiTreeViewState>(elementId);
        return new UiTreeView(_ctx, id, state, roots);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UiPropertyGrid PropertyGrid(string id)
    {
        return new UiPropertyGrid(_ctx, id);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UiColorPicker ColorPicker(string id)
    {
        var elementId = _ctx.StringCache.GetId(id);
        var state = _ctx.GetOrCreateState<UiColorPickerState>(elementId);
        return new UiColorPicker(_ctx, id, state);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UiContextMenu ContextMenu(string id, UiContextMenuItem[] items)
    {
        var elementId = _ctx.StringCache.GetId(id);
        var state = _ctx.GetOrCreateState<UiContextMenuState>(elementId);
        return new UiContextMenu(_ctx, id, state, items);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UiContextMenuState GetContextMenuState(string id)
    {
        var elementId = _ctx.StringCache.GetId(id);
        return _ctx.GetOrCreateState<UiContextMenuState>(elementId);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UiListEditor ListEditor(string id)
    {
        var elementId = _ctx.StringCache.GetId(id);
        var state = _ctx.GetOrCreateState<UiListEditorState>(elementId);
        return new UiListEditor(_ctx, id, state);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Vec2Editor(string id, ref float x, ref float y,
        float sensitivity = 0.5f, string format = "F2")
        => Ui.Vec2Editor(_ctx, id, ref x, ref y, sensitivity, format);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Vec2Editor(string id, ref float x, ref float y,
        float sensitivity, string format,
        UiColor axisX, UiColor axisY,
        UiColor? valueBg, UiColor? valueEditBg, UiColor? valueText)
        => Ui.Vec2Editor(_ctx, id, ref x, ref y, sensitivity, format, axisX, axisY, valueBg, valueEditBg, valueText);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Vec3Editor(string id, ref float x, ref float y, ref float z,
        float sensitivity = 0.5f, string format = "F2")
        => Ui.Vec3Editor(_ctx, id, ref x, ref y, ref z, sensitivity, format);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Vec3Editor(string id, ref float x, ref float y, ref float z,
        float sensitivity, string format,
        UiColor axisX, UiColor axisY, UiColor axisZ,
        UiColor? valueBg, UiColor? valueEditBg, UiColor? valueText)
        => Ui.Vec3Editor(_ctx, id, ref x, ref y, ref z, sensitivity, format, axisX, axisY, axisZ, valueBg, valueEditBg, valueText);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Vec4Editor(string id, ref float x, ref float y, ref float z, ref float w,
        float sensitivity = 0.5f, string format = "F2")
        => Ui.Vec4Editor(_ctx, id, ref x, ref y, ref z, ref w, sensitivity, format);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Vec4Editor(string id, ref float x, ref float y, ref float z, ref float w,
        float sensitivity, string format,
        UiColor axisX, UiColor axisY, UiColor axisZ, UiColor axisW,
        UiColor? valueBg, UiColor? valueEditBg, UiColor? valueText)
        => Ui.Vec4Editor(_ctx, id, ref x, ref y, ref z, ref w, sensitivity, format, axisX, axisY, axisZ, axisW, valueBg, valueEditBg, valueText);
}
