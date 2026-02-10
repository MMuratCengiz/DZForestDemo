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

        // Menu bar
        MenuBarBuilder.Build(ui, ctx, vm);

        // Main 3-column layout
        using (ui.Panel("MainRow").Horizontal().Grow().Open())
        {
            // Left panel - Scene Hierarchy
            using (ui.Panel("LeftPanel")
                .Vertical()
                .Background(t.PanelBackground)
                .Border(UiBorder.Horizontal(0, UiColor.Transparent)
                    with { Right = 1, Color = t.Border })
                .Width(UiSizing.Fit(200, 320))
                .GrowHeight()
                .Open())
            {
                EditorUi.SectionHeader(ui, ctx, "HierarchyHeader", FontAwesome.LayerGroup, "Scene Hierarchy");
                SceneHierarchyBuilder.Build(ui, ctx, vm);
            }

            // Center - Viewport (transparent, scene shows through)
            using (ui.Panel("Viewport")
                .Vertical()
                .Background(UiColor.Transparent)
                .Grow()
                .Open())
            {
                // Toolbar wrapper with padding to float it
                using (ui.Panel("ToolbarWrap")
                    .Padding(8, 8, 6, 0)
                    .GrowWidth()
                    .FitHeight()
                    .Open())
                {
                    ViewportToolbarBuilder.Build(ui, ctx, vm);
                }
                // Viewport fills remaining space
                using (ui.Panel("ViewportFill").Grow().Open()) { }
            }

            // Right panel - Inspector
            using (ui.Panel("RightPanel")
                .Vertical()
                .Background(t.PanelBackground)
                .Border(UiBorder.Horizontal(0, UiColor.Transparent)
                    with { Left = 1, Color = t.Border })
                .Width(UiSizing.Fit(280, 420))
                .GrowHeight()
                .Open())
            {
                EditorUi.SectionHeader(ui, ctx, "InspectorHeader", FontAwesome.Gear, "Inspector");
                InspectorBuilder.Build(ui, ctx, vm);
            }
        }

        // Dialog overlays (rendered last = on top)
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
