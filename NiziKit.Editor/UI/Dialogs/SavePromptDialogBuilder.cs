using NiziKit.Editor.Theme;
using NiziKit.Editor.ViewModels;
using NiziKit.UI;

namespace NiziKit.Editor.UI.Dialogs;

public static class SavePromptDialogBuilder
{
    public static void Build(EditorViewModel vm)
    {
        var t = EditorTheme.Current;

        using var overlay = EditorUi.DialogOverlay("SavePromptOverlay");
        using var dialog = EditorUi.DialogContainer("SavePromptDialog", "Unsaved Changes", 450, 200);

        using (NiziUi.Panel("SavePromptBody")
            .Vertical()
            .Padding(20, 12)
            .Gap(10)
            .Grow()
            .Open())
        {
            NiziUi.Icon(FontAwesome.TriangleExclamation, t.Warning, t.IconSizeMedium);
            NiziUi.Text("You have unsaved changes. What would you like to do?",
                new UiTextStyle { Color = t.TextPrimary, FontSize = t.FontSizeBody });
        }

        using (NiziUi.Panel("SavePromptButtons")
            .Horizontal()
            .Padding(16, 10)
            .Gap(8)
            .GrowWidth()
            .FitHeight()
            .AlignChildrenX(UiAlignX.Right)
            .AlignChildrenY(UiAlignY.Center)
            .Background(t.PanelElevated)
            .Open())
        {
            if (EditorUi.GhostButton("SavePromptCancel", "Cancel"))
            {
                vm.SavePromptCancel();
            }

            if (EditorUi.DangerButton("SavePromptDiscard", "Discard"))
            {
                vm.SavePromptDiscard();
            }

            if (EditorUi.AccentButton("SavePromptSave", "Save"))
            {
                vm.SavePromptSave();
            }
        }
    }
}
