using System.Runtime.CompilerServices;
using DenOfIz;

namespace UIFramework;

/// <summary>
/// Represents an active UI frame. Use this to build your UI hierarchy.
/// This is a ref struct to prevent allocations.
/// </summary>
public readonly ref struct UiFrame
{
    private readonly UiContext _context;

    internal UiFrame(UiContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Ends the frame and returns the render result.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiFrameResult End(uint frameIndex, float deltaTime)
    {
        return _context.EndFrame(frameIndex, deltaTime);
    }

    /// <summary>
    /// Creates a root container that fills the entire viewport.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiElement Root(string id = "Root")
    {
        return new UiElement(_context, id)
            .Width(UiSizing.Grow())
            .Height(UiSizing.Grow());
    }

    /// <summary>
    /// Creates a panel element with the given ID.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiElement Panel(string id) => new(_context, id);

    /// <summary>
    /// Creates a row (horizontal layout).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiElement Row(string id = "Row") => new UiElement(_context, id).Direction(UiDirection.Horizontal);

    /// <summary>
    /// Creates a column (vertical layout).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiElement Column(string id = "Column") => new UiElement(_context, id).Direction(UiDirection.Vertical);

    /// <summary>
    /// Adds text at the current position.
    /// </summary>
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

    /// <summary>
    /// Adds a texture/image at the current position.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Image(TextureResource texture, uint width, uint height)
    {
        _context.Clay.Texture(texture, width, height);
    }
}

/// <summary>
/// Style configuration for text elements.
/// </summary>
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
