using NiziKit.Editor.Theme;
using NiziKit.Editor.ViewModels;
using NiziKit.UI;

namespace NiziKit.Editor.UI.Dialogs;

public static class SavePromptDialogBuilder
{
    public static void Build(UiFrame ui, UiContext ctx, EditorViewModel vm)
    {
        var t = EditorTheme.Current;

        using var overlay = EditorUi.DialogOverlay(ui, "SavePromptOverlay");
        using var dialog = EditorUi.DialogContainer(ui, ctx, "SavePromptDialog", "Unsaved Changes", 450, 200);

        // Body
        using (ui.Panel("SavePromptBody")
            .Vertical()
            .Padding(24, 16)
            .Gap(12)
            .Grow()
            .Open())
        {
            ui.Icon(FontAwesome.TriangleExclamation, t.Warning, t.IconSizeMedium);
            ui.Text("You have unsaved changes. What would you like to do?",
                new UiTextStyle { Color = t.TextPrimary, FontSize = t.FontSizeBody });
        }

        // Buttons
        using (ui.Panel("SavePromptButtons")
            .Horizontal()
            .Padding(24, 14)
            .Gap(8)
            .GrowWidth()
            .FitHeight()
            .AlignChildrenX(UiAlignX.Right)
            .AlignChildrenY(UiAlignY.Center)
            .Background(t.PanelElevated)
            .Open())
        {
            if (EditorUi.GhostButton(ctx, "SavePromptCancel", "Cancel"))
            {
                vm.SavePromptCancel();
            }

            if (EditorUi.DangerButton(ctx, "SavePromptDiscard", "Discard"))
            {
                vm.SavePromptDiscard();
            }

            if (EditorUi.AccentButton(ctx, "SavePromptSave", "Save"))
            {
                vm.SavePromptSave();
            }
        }
    }
}
