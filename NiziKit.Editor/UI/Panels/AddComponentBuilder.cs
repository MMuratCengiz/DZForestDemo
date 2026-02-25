using NiziKit.Editor.Theme;
using NiziKit.Editor.ViewModels;
using NiziKit.UI;

namespace NiziKit.Editor.UI.Panels;

public class AddComponentPanelState
{
    public string SearchText { get; set; } = "";
}

public static class AddComponentBuilder
{
    public static void Build(GameObjectViewModel obj)
    {
        var t = EditorTheme.Current;
        var state = NiziUi.GetOrCreateState<AddComponentPanelState>("AddComponentPanel");

        using var panel = NiziUi.Panel("AddComponentPanel")
            .Vertical()
            .Background(t.PanelElevated)
            .Border(1, t.Border)
            .CornerRadius(t.RadiusMedium)
            .Padding(8)
            .Gap(6)
            .GrowWidth()
            .Height(UiSizing.Fit(0, 260))
            .Open();

        using (NiziUi.Panel("AddCompHeader")
            .Horizontal()
            .GrowWidth()
            .FitHeight()
            .AlignChildrenY(UiAlignY.Center)
            .Gap(6)
            .Open())
        {
            NiziUi.Icon(FontAwesome.Plus, t.Accent, t.IconSizeSmall);
            NiziUi.Text("Add Component", new UiTextStyle { Color = t.TextPrimary, FontSize = t.FontSizeBody });

            NiziUi.FlexSpacer();

            if (EditorUi.IconButton("CloseAddComp", FontAwesome.Xmark))
            {
                obj.IsAddingComponent = false;
                state.SearchText = "";
            }
        }

        var searchText = state.SearchText;
        NiziUi.TextField("CompSearch", ref searchText)
            .BackgroundColor(t.SurfaceInset, t.PanelBackground)
            .TextColor(t.TextPrimary)
            .PlaceholderColor(t.TextMuted)
            .BorderColor(t.Border, t.Accent)
            .FontSize(t.FontSizeCaption)
            .CornerRadius(t.RadiusSmall)
            .Padding(6, 4)
            .GrowWidth()
            .Placeholder("Search components...")
            .Show();
        state.SearchText = searchText;

        using (NiziUi.Panel("AddCompDiv").GrowWidth().Height(1).Background(t.Border).Open()) { }
        using var list = NiziUi.Panel("CompList")
            .Vertical()
            .GrowWidth()
            .Height(UiSizing.Grow())
            .ScrollVertical()
            .Gap(1)
            .Open();

        var availableTypes = obj.GetAvailableComponentTypes();
        var index = 0;

        foreach (var type in availableTypes)
        {
            var typeName = type.Name;
            var displayName = typeName.EndsWith("Component") ? typeName[..^9] : typeName;

            if (!string.IsNullOrEmpty(searchText) &&
                !displayName.Contains(searchText, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (NiziUi.Button("AddComp_" + index, displayName)
                .Color(UiColor.Transparent, t.Hover, t.Active)
                .TextColor(t.TextPrimary)
                .FontSize(t.FontSizeCaption)
                .Padding(6, 4)
                .CornerRadius(t.RadiusSmall)
                .Border(0, UiColor.Transparent)
                .Show())
            {
                obj.AddComponentOfType(type);
                state.SearchText = "";
            }

            index++;
        }
    }
}
