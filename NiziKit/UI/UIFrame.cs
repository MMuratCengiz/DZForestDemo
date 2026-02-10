using System.Runtime.CompilerServices;
using DenOfIz;

namespace NiziKit.UI;

public readonly ref struct UiFrame
{
    private readonly UiContext _context;

    internal UiFrame(UiContext context)
    {
        _context = context;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public (Texture Texture, DenOfIz.Semaphore Semaphore) End(uint frameIndex, float deltaTime)
    {
        return _context.EndFrame(frameIndex, deltaTime);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiElement Root(string id = "Root")
    {
        return new UiElement(_context, id)
            .Width(UiSizing.Grow())
            .Height(UiSizing.Grow());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiElement Panel(string id)
    {
        return new UiElement(_context, id);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiElement Row(string id = "Row")
    {
        return new UiElement(_context, id).Direction(UiDirection.Horizontal);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiElement Column(string id = "Column")
    {
        return new UiElement(_context, id).Direction(UiDirection.Vertical);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Text(string text, UiTextStyle style = default)
    {
        var desc = new ClayTextDesc
        {
            TextColor = style.Color.ToClayColor(),
            FontSize = style.FontSize > 0 ? style.FontSize : (ushort)14,
            FontId = style.FontId,
            TextAlignment = style.Alignment switch
            {
                UiTextAlign.Center => ClayTextAlignment.Center,
                UiTextAlign.Right => ClayTextAlignment.Right,
                _ => ClayTextAlignment.Left
            }
        };
        _context.Clay.Text(StringView.Intern(text), desc);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Image(Texture texture, uint width, uint height)
    {
        _context.Clay.Texture(texture, width, height);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Icon(string icon, UiColor color, ushort size = 14)
    {
        var desc = new ClayTextDesc
        {
            TextColor = color.ToClayColor(),
            FontSize = size,
            FontId = FontAwesome.FontId,
            TextAlignment = ClayTextAlignment.Center
        };
        _context.Clay.Text(StringView.Intern(icon), desc);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Icon(string icon, UiTextStyle style)
    {
        var desc = new ClayTextDesc
        {
            TextColor = style.Color.ToClayColor(),
            FontSize = style.FontSize > 0 ? style.FontSize : (ushort)14,
            FontId = FontAwesome.FontId,
            TextAlignment = style.Alignment switch
            {
                UiTextAlign.Center => ClayTextAlignment.Center,
                UiTextAlign.Right => ClayTextAlignment.Right,
                _ => ClayTextAlignment.Left
            }
        };
        _context.Clay.Text(StringView.Intern(icon), desc);
    }
}

public struct UiTextStyle
{
    public UiColor Color;
    public ushort FontSize;
    public ushort FontId;
    public UiTextAlign Alignment;

    public static UiTextStyle Default => new()
    {
        Color = UiColor.White,
        FontSize = 14,
        FontId = 0,
        Alignment = UiTextAlign.Left
    };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiTextStyle WithColor(UiColor color)
    {
        Color = color;
        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiTextStyle WithSize(ushort size)
    {
        FontSize = size;
        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiTextStyle WithFont(ushort fontId)
    {
        FontId = fontId;
        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiTextStyle WithAlignment(UiTextAlign align)
    {
        Alignment = align;
        return this;
    }
}
