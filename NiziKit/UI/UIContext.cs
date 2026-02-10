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
    private float _prevMouseX;
    private float _prevMouseY;

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

        StringCache = new StringCache(Clay);
    }

    public Clay Clay { get; }
    internal StringCache StringCache { get; }
    internal bool MouseJustReleased { get; private set; }
    internal bool MousePressed { get; private set; }
    public uint FocusedTextFieldId { get; internal set; }
    internal List<Event> FrameEvents { get; } = [];
    internal float MouseX { get; private set; }
    internal float MouseY { get; private set; }
    internal float MouseDeltaX { get; private set; }
    internal float MouseDeltaY { get; private set; }
    internal uint ActiveDragWidgetId { get; set; }

    /// <summary>
    /// Returns true if the pointer is currently over any UI element rendered in the previous frame.
    /// Set by the UI system during layout; defaults to checking the root element.
    /// </summary>
    public bool IsPointerOverUi { get; internal set; }

    /// <summary>
    /// Hashes a string ID into a uint element ID, suitable for state lookups.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint GetElementId(string id) => StringCache.GetId(id);

    /// <summary>
    /// Gets or creates a persistent state object associated with the given element ID.
    /// </summary>
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void HandleEvent(Event ev)
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

        if (ev.Type == EventType.MouseMotion)
        {
            MouseX = ev.MouseMotion.X;
            MouseY = ev.MouseMotion.Y;
        }
        else if (ev.Type == EventType.MouseButtonDown)
        {
            MouseX = ev.MouseButton.X;
            MouseY = ev.MouseButton.Y;
        }

        Clay.HandleEvent(ev);
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
        Clay.EndLayout(frameIndex, deltaTime, commandList);
        MouseJustReleased = false;
        _mouseJustPressed = false;
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
    internal UiInteraction GetInteraction(uint id)
    {
        var hovered = Clay.PointerOver(id);
        if (hovered)
        {
            IsPointerOverUi = true;
        }

        var pressed = hovered && MousePressed;
        var clicked = hovered && MouseJustReleased;
        return new UiInteraction(hovered, pressed, clicked);
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

    public void RecordEvent(Event ev)
    {
        FrameEvents.Add(ev);
    }

    public void ClearFrameEvents()
    {
        FrameEvents.Clear();
    }
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
