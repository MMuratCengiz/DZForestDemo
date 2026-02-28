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
        => Vec2Editor(id, ref x, ref y, sensitivity, format,
            UiColor.Rgb(200, 70, 70), UiColor.Rgb(70, 180, 70), null, null, null);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Vec2Editor(string id, ref float x, ref float y,
        float sensitivity, string format,
        UiColor axisX, UiColor axisY,
        UiColor? valueBg, UiColor? valueEditBg, UiColor? valueText)
    {
        var changed = false;
        var ctx = _ctx;

        var containerId = ctx.StringCache.GetId(id);
        var containerDecl = new DenOfIz.ClayElementDeclaration { Id = containerId };
        containerDecl.Layout.LayoutDirection = DenOfIz.ClayLayoutDirection.LeftToRight;
        containerDecl.Layout.Sizing.Width = DenOfIz.ClaySizingAxis.Grow(0, float.MaxValue);
        containerDecl.Layout.Sizing.Height = DenOfIz.ClaySizingAxis.Fit(0, float.MaxValue);
        containerDecl.Layout.ChildGap = 4;

        ctx.OpenElement(containerDecl);
        {
            var dvX = DraggableValue(id + "_X")
                .LabelWidth(0)
                .Prefix("X", axisX)
                .Sensitivity(sensitivity)
                .Format(format);
            if (valueBg.HasValue)
            {
                dvX = dvX.ValueColor(valueBg.Value);
            }

            if (valueEditBg.HasValue)
            {
                dvX = dvX.ValueEditColor(valueEditBg.Value);
            }

            if (valueText.HasValue)
            {
                dvX = dvX.ValueTextColor(valueText.Value);
            }

            if (dvX.Show(ref x))
            {
                changed = true;
            }

            var dvY = DraggableValue(id + "_Y")
                .LabelWidth(0)
                .Prefix("Y", axisY)
                .Sensitivity(sensitivity)
                .Format(format);
            if (valueBg.HasValue)
            {
                dvY = dvY.ValueColor(valueBg.Value);
            }

            if (valueEditBg.HasValue)
            {
                dvY = dvY.ValueEditColor(valueEditBg.Value);
            }

            if (valueText.HasValue)
            {
                dvY = dvY.ValueTextColor(valueText.Value);
            }

            if (dvY.Show(ref y))
            {
                changed = true;
            }
        }
        ctx.Clay.CloseElement();

        return changed;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Vec3Editor(string id, ref float x, ref float y, ref float z,
        float sensitivity = 0.5f, string format = "F2")
        => Vec3Editor(id, ref x, ref y, ref z, sensitivity, format,
            UiColor.Rgb(200, 70, 70), UiColor.Rgb(70, 180, 70), UiColor.Rgb(70, 120, 220),
            null, null, null);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Vec3Editor(string id, ref float x, ref float y, ref float z,
        float sensitivity, string format,
        UiColor axisX, UiColor axisY, UiColor axisZ,
        UiColor? valueBg, UiColor? valueEditBg, UiColor? valueText)
    {
        var changed = false;
        var ctx = _ctx;

        var containerId = ctx.StringCache.GetId(id);
        var containerDecl = new DenOfIz.ClayElementDeclaration { Id = containerId };
        containerDecl.Layout.LayoutDirection = DenOfIz.ClayLayoutDirection.LeftToRight;
        containerDecl.Layout.Sizing.Width = DenOfIz.ClaySizingAxis.Grow(0, float.MaxValue);
        containerDecl.Layout.Sizing.Height = DenOfIz.ClaySizingAxis.Fit(0, float.MaxValue);
        containerDecl.Layout.ChildGap = 4;

        ctx.OpenElement(containerDecl);
        {
            var dvX = DraggableValue(id + "_X")
                .LabelWidth(0)
                .Prefix("X", axisX)
                .Sensitivity(sensitivity)
                .Format(format);
            if (valueBg.HasValue)
            {
                dvX = dvX.ValueColor(valueBg.Value);
            }

            if (valueEditBg.HasValue)
            {
                dvX = dvX.ValueEditColor(valueEditBg.Value);
            }

            if (valueText.HasValue)
            {
                dvX = dvX.ValueTextColor(valueText.Value);
            }

            if (dvX.Show(ref x))
            {
                changed = true;
            }

            var dvY = DraggableValue(id + "_Y")
                .LabelWidth(0)
                .Prefix("Y", axisY)
                .Sensitivity(sensitivity)
                .Format(format);
            if (valueBg.HasValue)
            {
                dvY = dvY.ValueColor(valueBg.Value);
            }

            if (valueEditBg.HasValue)
            {
                dvY = dvY.ValueEditColor(valueEditBg.Value);
            }

            if (valueText.HasValue)
            {
                dvY = dvY.ValueTextColor(valueText.Value);
            }

            if (dvY.Show(ref y))
            {
                changed = true;
            }

            var dvZ = DraggableValue(id + "_Z")
                .LabelWidth(0)
                .Prefix("Z", axisZ)
                .Sensitivity(sensitivity)
                .Format(format);
            if (valueBg.HasValue)
            {
                dvZ = dvZ.ValueColor(valueBg.Value);
            }

            if (valueEditBg.HasValue)
            {
                dvZ = dvZ.ValueEditColor(valueEditBg.Value);
            }

            if (valueText.HasValue)
            {
                dvZ = dvZ.ValueTextColor(valueText.Value);
            }

            if (dvZ.Show(ref z))
            {
                changed = true;
            }
        }
        ctx.Clay.CloseElement();

        return changed;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Vec4Editor(string id, ref float x, ref float y, ref float z, ref float w,
        float sensitivity = 0.5f, string format = "F2")
        => Vec4Editor(id, ref x, ref y, ref z, ref w, sensitivity, format,
            UiColor.Rgb(200, 70, 70), UiColor.Rgb(70, 180, 70), UiColor.Rgb(70, 120, 220), UiColor.Rgb(200, 180, 50),
            null, null, null);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Vec4Editor(string id, ref float x, ref float y, ref float z, ref float w,
        float sensitivity, string format,
        UiColor axisX, UiColor axisY, UiColor axisZ, UiColor axisW,
        UiColor? valueBg, UiColor? valueEditBg, UiColor? valueText)
    {
        var changed = false;
        var ctx = _ctx;

        var containerId = ctx.StringCache.GetId(id);
        var containerDecl = new DenOfIz.ClayElementDeclaration { Id = containerId };
        containerDecl.Layout.LayoutDirection = DenOfIz.ClayLayoutDirection.LeftToRight;
        containerDecl.Layout.Sizing.Width = DenOfIz.ClaySizingAxis.Grow(0, float.MaxValue);
        containerDecl.Layout.Sizing.Height = DenOfIz.ClaySizingAxis.Fit(0, float.MaxValue);
        containerDecl.Layout.ChildGap = 4;

        ctx.OpenElement(containerDecl);
        {
            var dvX = DraggableValue(id + "_X")
                .LabelWidth(0)
                .Prefix("X", axisX)
                .Sensitivity(sensitivity)
                .Format(format);
            if (valueBg.HasValue)
            {
                dvX = dvX.ValueColor(valueBg.Value);
            }

            if (valueEditBg.HasValue)
            {
                dvX = dvX.ValueEditColor(valueEditBg.Value);
            }

            if (valueText.HasValue)
            {
                dvX = dvX.ValueTextColor(valueText.Value);
            }

            if (dvX.Show(ref x))
            {
                changed = true;
            }

            var dvY = DraggableValue(id + "_Y")
                .LabelWidth(0)
                .Prefix("Y", axisY)
                .Sensitivity(sensitivity)
                .Format(format);
            if (valueBg.HasValue)
            {
                dvY = dvY.ValueColor(valueBg.Value);
            }

            if (valueEditBg.HasValue)
            {
                dvY = dvY.ValueEditColor(valueEditBg.Value);
            }

            if (valueText.HasValue)
            {
                dvY = dvY.ValueTextColor(valueText.Value);
            }

            if (dvY.Show(ref y))
            {
                changed = true;
            }

            var dvZ = DraggableValue(id + "_Z")
                .LabelWidth(0)
                .Prefix("Z", axisZ)
                .Sensitivity(sensitivity)
                .Format(format);
            if (valueBg.HasValue)
            {
                dvZ = dvZ.ValueColor(valueBg.Value);
            }

            if (valueEditBg.HasValue)
            {
                dvZ = dvZ.ValueEditColor(valueEditBg.Value);
            }

            if (valueText.HasValue)
            {
                dvZ = dvZ.ValueTextColor(valueText.Value);
            }

            if (dvZ.Show(ref z))
            {
                changed = true;
            }

            var dvW = DraggableValue(id + "_W")
                .LabelWidth(0)
                .Prefix("W", axisW)
                .Sensitivity(sensitivity)
                .Format(format);
            if (valueBg.HasValue)
            {
                dvW = dvW.ValueColor(valueBg.Value);
            }

            if (valueEditBg.HasValue)
            {
                dvW = dvW.ValueEditColor(valueEditBg.Value);
            }

            if (valueText.HasValue)
            {
                dvW = dvW.ValueTextColor(valueText.Value);
            }

            if (dvW.Show(ref w))
            {
                changed = true;
            }
        }
        ctx.Clay.CloseElement();

        return changed;
    }
}
