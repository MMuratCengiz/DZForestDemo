using System.Runtime.CompilerServices;
using DenOfIz;

namespace NiziKit.UI;

public static partial class NiziUi
{
    private static UiContext _ctx = null!;
    private static readonly Dictionary<string, UiStyleClass> _styleClasses = new();
    private static readonly Dictionary<string, UiTextStyleClass> _textStyleClasses = new();

    internal static UiContext Ctx
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _ctx;
    }

    public static bool IsInitialized => _ctx != null!;

    public static bool IsPointerOverUi => _ctx.IsPointerOverUi;
    public static uint FocusedTextFieldId => _ctx.FocusedTextFieldId;
    public static float MouseX => _ctx.MouseX;
    public static float MouseY => _ctx.MouseY;

    public static void Initialize(UiContextDesc desc)
    {
        _ctx?.Dispose();
        _ctx = new UiContext(desc);
        FontAwesome.Initialize(_ctx.Clay);
    }

    public static void Shutdown()
    {
        FontAwesome.Shutdown();
        _ctx?.Dispose();
        _ctx = null!;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void OnEvent(in Event e)
    {
        _ctx.OnEvent(in e);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SetViewportSize(uint width, uint height)
    {
        _ctx.SetViewportSize(width, height);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint GetElementId(string id) => _ctx.GetElementId(id);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UiBounds GetElementBounds(uint id) => _ctx.GetElementBounds(id);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UiInteraction GetInteraction(uint id) => _ctx.GetInteraction(id);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool WasRightClicked(uint id) => _ctx.WasRightClicked(id);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T GetOrCreateState<T>(string id) where T : new() => _ctx.GetOrCreateState<T>(id);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool PointerOver(uint elementId) => _ctx.Clay.PointerOver(elementId);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ClayDimensions MeasureText(string text, ushort fontId, ushort fontSize)
        => _ctx.Clay.MeasureText(StringView.Create(text), fontId, fontSize);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ClayBoundingBox GetElementBoundingBox(uint id)
        => _ctx.Clay.GetElementBoundingBox(id);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint GetCharIndexAtOffset(string text, float offset, ushort fontId, ushort fontSize)
        => _ctx.Clay.GetCharIndexAtOffset(StringView.Create(text), offset, fontId, fontSize);

    public static void RegisterClass(string name, UiStyleClass style)
    {
        _styleClasses[name] = style;
    }

    public static void RegisterTextClass(string name, UiTextStyleClass style)
    {
        _textStyleClasses[name] = style;
    }

    internal static UiStyleClass? GetClass(string name)
    {
        return _styleClasses.GetValueOrDefault(name);
    }

    internal static UiTextStyleClass? GetTextClass(string name)
    {
        return _textStyleClasses.GetValueOrDefault(name);
    }
}
