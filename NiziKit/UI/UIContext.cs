using System.Numerics;
using System.Runtime.CompilerServices;
using DenOfIz;
using NiziKit.Graphics;

namespace NiziKit.UI;

public sealed class UiContext : IDisposable
{
    private readonly Dictionary<uint, object> _widgetStates = new();
    private uint _frameElementIndex;
    private bool _mouseJustPressed;
    private bool _mouseRightJustPressed;
    private float _prevMouseX;
    private float _prevMouseY;
    private bool _popupOpenThisFrame;
    private bool _popupOpenPrevFrame;
    private int _popupDepth;

    public UiContext(UiContextDesc desc)
    {
        Clay = new Clay(new ClayDesc
        {
            LogicalDevice = GraphicsContext.Device,
            ResourceTracking = GraphicsContext.ResourceTracking,
            RenderTargetFormat = GraphicsContext.BackBufferFormat,
            NumFrames = GraphicsContext.NumFrames,
            Width = GraphicsContext.Width,
            Height = GraphicsContext.Height,
            MaxNumElements = desc.MaxNumElements,
            MaxNumTextMeasureCacheElements = desc.MaxNumTextMeasureCacheElements,
            MaxNumFonts = desc.MaxNumFonts
        });

        Clay.SetDpiScale(GraphicsContext.Window.GetDisplayScale());
        StringCache = new StringCache(Clay);
    }

    public Clay Clay { get; }
    internal StringCache StringCache { get; }
    internal bool MouseJustReleased { get; private set; }
    internal bool MousePressed { get; private set; }
    internal bool MouseRightJustReleased { get; private set; }
    internal bool MouseRightPressed { get; private set; }
    public uint FocusedTextFieldId { get; internal set; }
    internal List<Event> FrameEvents { get; } = [];
    public float MouseX { get; private set; }
    public float MouseY { get; private set; }
    internal float MouseDeltaX { get; private set; }
    internal float MouseDeltaY { get; private set; }
    internal uint ActiveDragWidgetId { get; set; }

    public bool IsPointerOverUi { get; internal set; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint GetElementId(string id) => StringCache.GetId(id);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T GetOrCreateState<T>(string id) where T : new()
    {
        return GetOrCreateState<T>(StringCache.GetId(id));
    }

    public void Dispose()
    {
        Clay.Dispose();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetViewportSize(uint width, uint height)
    {
        Clay.SetViewportSize(width, height);
    }

    /// <summary>
    /// Returns the element bounding box in logical (DPI-scaled) coordinates,
    /// consistent with <see cref="MouseX"/> and <see cref="MouseY"/>.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiBounds GetElementBounds(uint id)
    {
        var bbox = Clay.GetElementBoundingBox(id);
        var dpi = Clay.GetDpiScale();
        return new UiBounds(bbox.X / dpi, bbox.Y / dpi, bbox.Width / dpi, bbox.Height / dpi);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void OnEvent(in Event ev)
    {
        if (ev is { Type: EventType.MouseButtonDown, MouseButton.Button: MouseButton.Left })
        {
            MousePressed = true;
            _mouseJustPressed = true;
            MouseJustReleased = false;
        }
        else if (ev is { Type: EventType.MouseButtonUp, MouseButton.Button: MouseButton.Left })
        {
            MousePressed = false;
            MouseJustReleased = true;
            _mouseJustPressed = false;
            ActiveDragWidgetId = 0;
        }

        if (ev is { Type: EventType.MouseButtonDown, MouseButton.Button: MouseButton.Right })
        {
            MouseRightPressed = true;
            _mouseRightJustPressed = true;
            MouseRightJustReleased = false;
        }
        else if (ev is { Type: EventType.MouseButtonUp, MouseButton.Button: MouseButton.Right })
        {
            MouseRightPressed = false;
            MouseRightJustReleased = true;
            _mouseRightJustPressed = false;
        }

        var dpiScale = Clay.GetDpiScale();
        if (ev.Type == EventType.MouseMotion)
        {
            MouseX = ev.MouseMotion.X / dpiScale;
            MouseY = ev.MouseMotion.Y / dpiScale;
        }
        else if (ev.Type is EventType.MouseButtonDown or EventType.MouseButtonUp)
        {
            MouseX = ev.MouseButton.X / dpiScale;
            MouseY = ev.MouseButton.Y / dpiScale;
        }

        Clay.HandleEvent(ev);
        FrameEvents.Add(ev);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void UpdateScroll(float deltaTime, bool enableDrag = false)
    {
        Clay.UpdateScrollContainers(enableDrag, new Vector2 { X = 0, Y = 0 }, deltaTime);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiFrame BeginFrame()
    {
        _frameElementIndex = 0;
        IsPointerOverUi = false;
        _popupOpenPrevFrame = _popupOpenThisFrame;
        _popupOpenThisFrame = false;
        _popupDepth = 0;
        Clay.BeginLayout();
        return new UiFrame(this);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void OpenElement(ClayElementDeclaration decl)
    {
        Clay.OpenElement(decl);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal uint NextElementIndex()
    {
        return _frameElementIndex++;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void EndFrame(uint frameIndex, float deltaTime, CommandList commandList)
    {
        FrameEvents.Clear();
        Clay.EndLayout(frameIndex, deltaTime, commandList);
        MouseJustReleased = false;
        _mouseJustPressed = false;
        MouseRightJustReleased = false;
        _mouseRightJustPressed = false;
        MouseDeltaX = MouseX - _prevMouseX;
        MouseDeltaY = MouseY - _prevMouseY;
        _prevMouseX = MouseX;
        _prevMouseY = MouseY;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool IsHovered(uint id)
    {
        var hovered = Clay.PointerOver(id);
        if (hovered)
        {
            IsPointerOverUi = true;
        }

        return hovered;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void BeginPopupScope(uint popupId)
    {
        _popupOpenThisFrame = true;
        _popupDepth++;
        if (Clay.PointerOver(popupId))
        {
            IsPointerOverUi = true;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void EndPopupScope()
    {
        _popupDepth--;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool WasRightClicked(uint id)
    {
        return Clay.PointerOver(id) && MouseRightJustReleased;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal UiInteraction GetInteraction(uint id)
    {
        var hovered = Clay.PointerOver(id);
        if (hovered)
        {
            IsPointerOverUi = true;
        }

        if (_popupOpenPrevFrame && _popupDepth == 0)
        {
            return new UiInteraction(hovered, false, false, false);
        }

        var pressed = hovered && MousePressed;
        var clicked = hovered && MouseJustReleased;
        var rightClicked = hovered && MouseRightJustReleased;
        return new UiInteraction(hovered, pressed, clicked, rightClicked);
    }

    internal T GetOrCreateState<T>(uint id) where T : new()
    {
        if (_widgetStates.TryGetValue(id, out var state) && state is T typedState)
        {
            return typedState;
        }

        var newState = new T();
        _widgetStates[id] = newState;
        return newState;
    }

    internal T GetOrCreateState<T>(uint id, Func<T> factory)
    {
        if (_widgetStates.TryGetValue(id, out var state) && state is T typedState)
        {
            return typedState;
        }

        var newState = factory();
        _widgetStates[id] = newState;
        return newState;
    }

    internal T GetOrCreateState<T>(Func<T> factory, uint id)
    {
        return GetOrCreateState(id, factory);
    }

}

public readonly struct UiBounds(float x, float y, float width, float height)
{
    public float X { get; } = x;
    public float Y { get; } = y;
    public float Width { get; } = width;
    public float Height { get; } = height;
}

public struct UiContextDesc
{
    public uint MaxNumElements;
    public uint MaxNumTextMeasureCacheElements;
    public uint MaxNumFonts;

    public static UiContextDesc Default => new()
    {
        MaxNumElements = 32768,
        MaxNumTextMeasureCacheElements = 262144,
        MaxNumFonts = 16
    };
}

internal sealed class StringCache(Clay clay)
{
    private readonly Dictionary<string, uint> _cache = new();
    private readonly Dictionary<(string, uint), uint> _indexedCache = new();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint GetId(string name)
    {
        if (_cache.TryGetValue(name, out var id))
        {
            return id;
        }

        id = clay.HashString(name,0, 0);
        _cache[name] = id;
        return id;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint GetId(string name, uint index)
    {
        var key = (name, index);
        if (_indexedCache.TryGetValue(key, out var id))
        {
            return id;
        }

        id = clay.HashString(name,index, 0);
        _indexedCache[key] = id;
        return id;
    }

    private readonly Dictionary<(string, uint, uint), uint> _compositeCache = new();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint GetId(string name, uint parentId, uint subIndex)
    {
        var key = (name, parentId, subIndex);
        if (_compositeCache.TryGetValue(key, out var id))
        {
            return id;
        }

        id = clay.HashString(name,parentId, subIndex);
        _compositeCache[key] = id;
        return id;
    }
}
