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
            .Padding(12)
            .Gap(4)
            .Border(1, t.ComponentBorder);

        componentVm.IsExpanded = section.IsExpanded;

        using var scope = section.Open();

        if (!componentVm.IsExpanded)
        {
            return;
        }

        // Remove button
        using (scope.Panel(sectionId + "_actions")
            .Horizontal()
            .GrowWidth()
            .FitHeight()
            .AlignChildrenX(UiAlignX.Right)
            .Open())
        {
            if (EditorUi.IconButton(ctx, sectionId + "_remove", FontAwesome.Trash))
            {
                componentVm.Remove();
                return;
            }
        }

        // Render component properties
        PropertyEditorRenderer.RenderProperties(ui, ctx, sectionId, componentVm.Component, editorVm, () =>
        {
            componentVm.NotifyChanged();
            editorVm.MarkDirty();
        });
    }
}
