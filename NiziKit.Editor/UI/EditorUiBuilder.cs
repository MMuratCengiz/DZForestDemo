using NiziKit.Editor.Theme;
using NiziKit.Editor.UI.Dialogs;
using NiziKit.Editor.UI.Panels;
using NiziKit.Editor.ViewModels;
using NiziKit.UI;

namespace NiziKit.Editor.UI;

public static class EditorUiBuilder
{
    public static void Build(EditorViewModel vm)
    {
        var t = EditorTheme.Current;

        using var root = NiziUi.Root()
            .Vertical()
            .Background(UiColor.Transparent)
            .Open();

        MenuBarBuilder.Build(vm);
        using (NiziUi.Panel("MainRow").Horizontal().Grow().Open())
        {
            using (NiziUi.Panel("LeftPanel")
                .Vertical()
                .Background(t.PanelBackground)
                .Border(UiBorder.Horizontal(0, UiColor.Transparent)
                    with { Right = 1, Color = t.Border })
                .Width(UiSizing.Percent(0.15f))
                .GrowHeight()
                .Open())
            {
                EditorUi.SectionHeader("HierarchyHeader", FontAwesome.LayerGroup, "Hierarchy");
                SceneHierarchyBuilder.Build(vm);
            }

            using (NiziUi.Panel("CenterColumn")
                .Vertical()
                .Background(UiColor.Transparent)
                .Grow()
                .Open())
            {
                using (NiziUi.Panel("ToolbarWrap")
                    .Padding(6, 6, 4, 0)
                    .GrowWidth()
                    .FitHeight()
                    .Open())
                {
                    ViewportToolbarBuilder.Build(vm);
                }

                using (NiziUi.Panel("ViewportFill").Grow().Open()) { }
            }

            using (NiziUi.Panel("RightPanel")
                .Vertical()
                .Background(t.PanelBackground)
                .Border(UiBorder.Horizontal(0, UiColor.Transparent)
                    with { Left = 1, Color = t.Border })
                .Width(t.PanelPreferredWidth)
                .GrowHeight()
                .Open())
            {
                EditorUi.SectionHeader("InspectorHeader", FontAwesome.Gear, "Inspector");
                InspectorBuilder.Build(vm);
            }
        }

        SceneHierarchyBuilder.BuildContextMenu(vm);

        if (vm.IsSavePromptOpen)
        {
            SavePromptDialogBuilder.Build(vm);
        }

        if (vm.IsOpenSceneDialogOpen)
        {
            OpenSceneDialogBuilder.Build(vm);
        }

        if (vm.IsImportPanelOpen)
        {
            ImportDialogBuilder.Build(vm);
        }

        if (vm.IsAssetPickerOpen)
        {
            AssetPickerDialogBuilder.Build(vm);
        }
    }
}
