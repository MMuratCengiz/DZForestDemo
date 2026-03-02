using System.Numerics;
using System.Text;
using DenOfIz;
using NiziKit.UI;

namespace DZForestDemo.UI;

public class CombatLogBox
{
    private readonly List<CombatLogEntry> _entries = [];
    private readonly List<string> _plainTextCache = [];

    private string? _tooltipTitle;
    private string? _tooltipContent;
    private string? _tooltipIcon;
    private uint _hoveredSegmentId;

    public void Render()
    {
        _tooltipTitle = null;
        _tooltipContent = null;
        _tooltipIcon = null;
        _hoveredSegmentId = 0;

        using (NiziUi.Panel("CombatLog").Class("scrollPanel").Open())
        {
            using (NiziUi.Column("CombatEntries").GrowWidth().GrowHeight().Open())
            {
                for (var i = 0; i < _entries.Count; i++)
                {
                    RenderEntry(_entries[i], i);
                }
            }
        }

        if (_tooltipTitle != null || _tooltipContent != null)
        {
            RenderTooltip();
        }
    }

    private void RenderEntry(CombatLogEntry entry, int index)
    {
        var rowBg = index % 2 == 0 ? GameTheme.RowEven : GameTheme.RowOdd;

        var elem = NiziUi.Panel("CombatEntry#" + index)
            .Class("entryRow")
            .Background(rowBg);

        if (NiziUi.WasRightClicked(elem.Id))
        {
            Clipboard.SetText(StringView.Intern(_plainTextCache[index]));
        }

        using (elem.Open())
        {
            NiziUi.Text($"[{entry.Timestamp:HH:mm:ss}] ", new UiTextStyle
            {
                Color = GameTheme.TextMuted,
                FontSize = GameTheme.FontTimestamp,
                WrapMode = UiTextWrap.Words
            });

            for (var s = 0; s < entry.Segments.Count; s++)
            {
                var segment = entry.Segments[s];
                var baseColor = GameTheme.ResolveColorKey(segment.ColorKey);

                if (segment.IsHoverable)
                {
                    RenderHoverableSegment(segment, baseColor, index, s);
                }
                else
                {
                    NiziUi.Text(segment.Text, new UiTextStyle
                    {
                        Color = baseColor,
                        FontSize = GameTheme.FontBody,
                        WrapMode = UiTextWrap.Words
                    });
                }
            }
        }
    }

    private void RenderHoverableSegment(TextSegment segment, UiColor baseColor, int entryIndex, int segIndex)
    {
        var elem = NiziUi.Panel("CSeg#" + entryIndex + "_" + segIndex).FitWidth().FitHeight();
        var interaction = NiziUi.GetInteraction(elem.Id);

        // Highlight on hover: brighten the color
        var textColor = interaction.IsHovered ? GameTheme.TextGold : baseColor;

        using (elem.Open())
        {
            NiziUi.Text(segment.Text, new UiTextStyle
            {
                Color = textColor,
                FontSize = GameTheme.FontBody,
                WrapMode = UiTextWrap.Words
            });
        }

        // Update tooltip state if this segment is hovered
        if (interaction.IsHovered && (segment.TooltipTitle != null || segment.TooltipContent != null))
        {
            _tooltipTitle = segment.TooltipTitle;
            _tooltipContent = segment.TooltipContent;
            _tooltipIcon = segment.TooltipIcon;
            _hoveredSegmentId = elem.Id;
        }
    }

    private void RenderTooltip()
    {
        // Position tooltip near the hovered element
        var bounds = NiziUi.GetElementBounds(_hoveredSegmentId);
        var tooltipX = bounds.X;
        var tooltipY = bounds.Y - 8; // above the element

        using (NiziUi.Panel("CombatTooltip")
                   .Class("tooltip")
                   .FitWidth(0, 280)
                   .FitHeight()
                   .Floating(new ClayFloatingDesc
                   {
                       AttachTo = ClayFloatingAttachTo.Root,
                       ParentAttachPoint = ClayFloatingAttachPoint.LeftTop,
                       ElementAttachPoint = ClayFloatingAttachPoint.LeftBottom,
                       Offset = new Vector2(tooltipX, tooltipY),
                       ZIndex = 2000
                   })
                   .Open())
        {
            // Header row (icon + title)
            if (!string.IsNullOrEmpty(_tooltipTitle))
            {
                using (NiziUi.Row("TooltipHeader").FitWidth().FitHeight().Gap(6)
                           .AlignChildrenY(UiAlignY.Center).Open())
                {
                    if (!string.IsNullOrEmpty(_tooltipIcon))
                    {
                        NiziUi.Text(_tooltipIcon, "tooltipTitle");
                    }

                    NiziUi.Text(_tooltipTitle, "tooltipTitle");
                }

                // Separator between title and content
                if (!string.IsNullOrEmpty(_tooltipContent))
                {
                    NiziUi.Divider(GameTheme.TooltipBorder);
                }
            }

            // Content
            if (!string.IsNullOrEmpty(_tooltipContent))
            {
                NiziUi.Text(_tooltipContent, "tooltipBody");
            }
        }
    }

    public void AddEntry(CombatLogEntry entry)
    {
        _entries.Add(entry);
        _plainTextCache.Add(BuildPlainText(entry));
    }

    private static string BuildPlainText(CombatLogEntry entry)
    {
        var sb = new StringBuilder(128);
        sb.Append($"[{entry.Timestamp:HH:mm:ss}] ");
        foreach (var seg in entry.Segments)
        {
            sb.Append(seg.Text);
        }
        return sb.ToString();
    }
}
