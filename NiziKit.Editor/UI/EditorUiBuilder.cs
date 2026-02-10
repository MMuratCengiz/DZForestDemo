using NiziKit.Editor.Theme;
using NiziKit.Editor.UI.Dialogs;
using NiziKit.Editor.UI.Panels;
using NiziKit.Editor.ViewModels;
using NiziKit.UI;

namespace NiziKit.Editor.UI;

public static class EditorUiBuilder
{
    public static void Build(UiFrame ui, UiContext ctx, EditorViewModel vm)
    {
        var t = EditorTheme.Current;

        using var root = ui.Root()
            .Vertical()
            .Background(UiColor.Transparent)
            .Open();

        MenuBarBuilder.Build(ui, ctx, vm);

        using (ui.Panel("MainRow").Horizontal().Grow().Open())
        {
            using (ui.Panel("LeftPanel")
                .Vertical()
                .Background(t.PanelBackground)
                .Border(UiBorder.Horizontal(0, UiColor.Transparent)
                    with { Right = 1, Color = t.Border })
                .Width(UiSizing.Percent(0.15f))
                .GrowHeight()
                .BlocksInput()
                .Open())
            {
                EditorUi.SectionHeader(ui, ctx, "HierarchyHeader", FontAwesome.LayerGroup, "Hierarchy");
                SceneHierarchyBuilder.Build(ui, ctx, vm);
            }

            using (ui.Panel("CenterColumn")
                .Vertical()
                .Background(UiColor.Transparent)
                .Grow()
                .Open())
            {
                using (ui.Panel("ToolbarWrap")
                    .Padding(6, 6, 4, 0)
                    .GrowWidth()
                    .FitHeight()
                    .BlocksInput()
                    .Open())
                {
                    ViewportToolbarBuilder.Build(ui, ctx, vm);
                }

                using (ui.Panel("ViewportFill").Grow().Open()) { }
            }

            using (ui.Panel("RightPanel")
                .Vertical()
                .Background(t.PanelBackground)
                .Border(UiBorder.Horizontal(0, UiColor.Transparent)
                    with { Left = 1, Color = t.Border })
                .Width(t.PanelPreferredWidth)
                .GrowHeight()
                .BlocksInput()
                .Open())
            {
                EditorUi.SectionHeader(ui, ctx, "InspectorHeader", FontAwesome.Gear, "Inspector");
                InspectorBuilder.Build(ui, ctx, vm);
            }
        }

        // Context menus rendered at root level so floating AttachTo=Root works correctly
        SceneHierarchyBuilder.BuildContextMenu(ui, ctx, vm);

        if (vm.IsSavePromptOpen)
        {
            SavePromptDialogBuilder.Build(ui, ctx, vm);
        }

        if (vm.IsOpenSceneDialogOpen)
        {
            OpenSceneDialogBuilder.Build(ui, ctx, vm);
        }

        if (vm.IsImportPanelOpen)
        {
            ImportDialogBuilder.Build(ui, ctx, vm);
        }

        if (vm.IsAssetPickerOpen)
        {
            AssetPickerDialogBuilder.Build(ui, ctx, vm);
        }
    }
}
