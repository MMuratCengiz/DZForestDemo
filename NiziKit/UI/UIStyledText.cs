using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using DenOfIz;

namespace NiziKit.UI;

public static partial class NiziUi
{
    [ThreadStatic] private static List<StyledTextSegment>? _styledTextSegments;

    /// <summary>
    /// Renders text with inline XML-like style tags.
    /// Supported tags:
    ///   Named colors: &lt;red&gt;, &lt;green&gt;, &lt;blue&gt;, &lt;yellow&gt;, &lt;cyan&gt;, &lt;magenta&gt;, etc.
    ///   Hex color: &lt;color=#RRGGBB&gt;
    ///   Named color via attribute: &lt;color=red&gt;
    ///   Font size: &lt;size=20&gt;
    /// All tags are closed with their corresponding closing tag (e.g. &lt;/red&gt;, &lt;/color&gt;, &lt;/size&gt;).
    /// Tags can be nested: &lt;red&gt;&lt;size=20&gt;big red&lt;/size&gt; normal red&lt;/red&gt;
    /// </summary>
    public static void StyledText(string markup, UiTextStyle baseStyle = default)
    {
        if (string.IsNullOrEmpty(markup))
        {
            return;
        }

        // Fast path: no tags at all
        if (!markup.Contains('<'))
        {
            Text(markup, baseStyle);
            return;
        }

        _styledTextSegments ??= new List<StyledTextSegment>(8);
        _styledTextSegments.Clear();
        StyledTextParser.Parse(markup, _styledTextSegments);

        if (_styledTextSegments.Count == 0)
        {
            return;
        }

        // Single segment: no container needed
        if (_styledTextSegments.Count == 1)
        {
            ref var seg = ref CollectionsMarshal.AsSpan(_styledTextSegments)[0];
            Text(seg.Text, ApplyOverrides(baseStyle, seg.Color, seg.FontSize));
            return;
        }

        // Multiple segments: wrap in a horizontal container so they flow inline
        var decl = new ClayElementDeclaration
        {
            Id = _ctx.StringCache.GetId("StyledText", _ctx.NextElementIndex())
        };
        decl.Layout.LayoutDirection = ClayLayoutDirection.LeftToRight;
        decl.Layout.Sizing.Width = ClaySizingAxis.Fit(0, float.MaxValue);
        decl.Layout.Sizing.Height = ClaySizingAxis.Fit(0, float.MaxValue);

        _ctx.OpenElement(decl);
        {
            foreach (ref var seg in CollectionsMarshal.AsSpan(_styledTextSegments))
            {
                var style = ApplyOverrides(baseStyle, seg.Color, seg.FontSize);
                var desc = new ClayTextDesc
                {
                    TextColor = style.Color.ToClayColor(),
                    FontSize = style.FontSize > 0 ? style.FontSize : (ushort)14,
                    FontId = style.FontId,
                    WrapMode = ClayTextWrapMode.None,
                    TextAlignment = style.Alignment switch
                    {
                        UiTextAlign.Center => ClayTextAlignment.Center,
                        UiTextAlign.Right => ClayTextAlignment.Right,
                        _ => ClayTextAlignment.Left
                    }
                };
                _ctx.Clay.Text(seg.Text, desc);
            }
        }
        _ctx.Clay.CloseElement();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static UiTextStyle ApplyOverrides(UiTextStyle baseStyle, UiColor? color, ushort? fontSize)
    {
        if (color.HasValue)
        {
            baseStyle.Color = color.Value;
        }

        if (fontSize.HasValue)
        {
            baseStyle.FontSize = fontSize.Value;
        }

        return baseStyle;
    }
}

internal struct StyledTextSegment
{
    public string Text;
    public UiColor? Color;
    public ushort? FontSize;
}

internal static class StyledTextParser
{
    private static readonly Dictionary<string, UiColor> NamedColors =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["red"] = UiColor.Red,
            ["green"] = UiColor.Green,
            ["blue"] = UiColor.Blue,
            ["white"] = UiColor.White,
            ["black"] = UiColor.Black,
            ["gray"] = UiColor.Gray,
            ["lightgray"] = UiColor.LightGray,
            ["darkgray"] = UiColor.DarkGray,
            ["yellow"] = UiColor.Rgb(255, 255, 0),
            ["cyan"] = UiColor.Rgb(0, 255, 255),
            ["magenta"] = UiColor.Rgb(255, 0, 255),
            ["orange"] = UiColor.Rgb(255, 165, 0),
        };

    private struct StyleState
    {
        public UiColor? Color;
        public ushort? FontSize;
    }

    public static void Parse(string markup, List<StyledTextSegment> segments)
    {
        var pos = 0;
        var stack = new Stack<StyleState>(4);
        var current = new StyleState();

        while (pos < markup.Length)
        {
            var tagStart = markup.IndexOf('<', pos);
            if (tagStart == -1)
            {
                AddSegment(segments, markup[pos..], in current);
                break;
            }

            if (tagStart > pos)
            {
                AddSegment(segments, markup.Substring(pos, tagStart - pos), in current);
            }

            var tagEnd = markup.IndexOf('>', tagStart);
            if (tagEnd == -1)
            {
                // Malformed tag — treat the rest as plain text
                AddSegment(segments, markup[tagStart..], in current);
                break;
            }

            var tagContent = markup.Substring(tagStart + 1, tagEnd - tagStart - 1);

            if (tagContent.Length > 0 && tagContent[0] == '/')
            {
                // Closing tag — restore previous style
                if (stack.Count > 0)
                {
                    current = stack.Pop();
                }
            }
            else
            {
                // Opening tag — push current state and apply the tag
                stack.Push(current);
                ApplyTag(tagContent, ref current);
            }

            pos = tagEnd + 1;
        }
    }

    private static void ApplyTag(string tag, ref StyleState state)
    {
        if (tag.StartsWith("color=", StringComparison.OrdinalIgnoreCase))
        {
            var value = tag[6..];
            if (value.Length > 0 && value[0] == '#')
            {
                if (value.Length == 7 &&
                    uint.TryParse(value.AsSpan(1), NumberStyles.HexNumber, null, out var hex))
                {
                    state.Color = UiColor.FromHex(hex);
                }
            }
            else if (NamedColors.TryGetValue(value, out var nc))
            {
                state.Color = nc;
            }
        }
        else if (tag.StartsWith("size=", StringComparison.OrdinalIgnoreCase))
        {
            if (ushort.TryParse(tag.AsSpan(5), out var size))
            {
                state.FontSize = size;
            }
        }
        else if (NamedColors.TryGetValue(tag, out var nc))
        {
            // Shorthand: <red>, <green>, etc.
            state.Color = nc;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void AddSegment(List<StyledTextSegment> segments, string text, in StyleState state)
    {
        if (text.Length == 0)
        {
            return;
        }

        segments.Add(new StyledTextSegment
        {
            Text = text,
            Color = state.Color,
            FontSize = state.FontSize
        });
    }

    /// <summary>
    /// Returns true if the given color name is recognized as a built-in named color.
    /// </summary>
    public static bool IsNamedColor(string name) => NamedColors.ContainsKey(name);

    /// <summary>
    /// Tries to resolve a color name or #RRGGBB hex string to a UiColor.
    /// </summary>
    public static bool TryParseColor(string value, out UiColor color)
    {
        if (value.Length > 0 && value[0] == '#' && value.Length == 7 &&
            uint.TryParse(value.AsSpan(1), NumberStyles.HexNumber, null, out var hex))
        {
            color = UiColor.FromHex(hex);
            return true;
        }

        if (NamedColors.TryGetValue(value, out color))
        {
            return true;
        }

        color = default;
        return false;
    }
}
