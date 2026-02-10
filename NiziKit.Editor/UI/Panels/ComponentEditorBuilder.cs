using NiziKit.Editor.Theme;
using NiziKit.Editor.ViewModels;
using NiziKit.UI;

namespace NiziKit.Editor.UI.Panels;

public static class ComponentEditorBuilder
{
    public static void Build(UiFrame ui, UiContext ctx, ComponentViewModel componentVm, EditorViewModel editorVm, int componentIndex)
    {
        var t = EditorTheme.Current;
        var sectionId = "Comp_" + componentIndex + "_" + componentVm.TypeName;

        var section = Ui.CollapsibleSection(ctx, sectionId, componentVm.DisplayName, componentVm.IsExpanded)
            .HeaderBackground(t.ComponentAccent, t.Hover)
            .HeaderTextColor(t.TextPrimary)
            .BodyBackground(t.PanelBackground)
            .ChevronColor(t.TextMuted)
            .FontSize(t.FontSizeBody)
            .Padding(8)
            .Gap(2)
            .Border(1, t.ComponentBorder)
            .HeaderAction(FontAwesome.Trash, t.TextMuted, t.Error);

        componentVm.IsExpanded = section.IsExpanded;

        if (section.HeaderActionClicked)
        {
            using var _ = section.Open();
            componentVm.Remove();
            return;
        }

        using var scope = section.Open();

        if (!componentVm.IsExpanded)
        {
            return;
        }

        PropertyEditorRenderer.RenderProperties(ui, ctx, sectionId, componentVm.Component, editorVm, () =>
        {
            componentVm.NotifyChanged();
            editorVm.MarkDirty();
        });
    }
}
