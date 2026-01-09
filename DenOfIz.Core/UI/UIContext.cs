using System.Numerics;
using System.Runtime.CompilerServices;

namespace DenOfIz.World.UI;

public sealed class UiContext : IDisposable
{
    private readonly Dictionary<uint, object> _widgetStates = new();
    private bool _disposed;
    private uint _frameElementIndex;
    private bool _mouseJustPressed;

    public UiContext(UiContextDesc desc)
    {
        Clay = new Clay(new ClayDesc
        {
            LogicalDevice = desc.LogicalDevice,
            ResourceTracking = desc.ResourceTracking,
            RenderTargetFormat = desc.RenderTargetFormat,
            NumFrames = desc.NumFrames,
            Width = desc.Width,
            Height = desc.Height,
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

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

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
        Clay.BeginLayout();
        return new UiFrame(this);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal uint NextElementIndex()
    {
        return _frameElementIndex++;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal (Texture Texture, Semaphore Semaphore) EndFrame(uint frameIndex, float deltaTime)
    {
        var result = Clay.EndLayout(frameIndex, deltaTime);
        MouseJustReleased = false;
        _mouseJustPressed = false;
        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool IsHovered(uint id)
    {
        return Clay.PointerOver(id);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal UiInteraction GetInteraction(uint id)
    {
        var hovered = Clay.PointerOver(id);
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
    public LogicalDevice LogicalDevice;
    public ResourceTracking ResourceTracking;
    public Format RenderTargetFormat;
    public uint NumFrames;
    public uint Width;
    public uint Height;
    public uint MaxNumElements;
    public uint MaxNumTextMeasureCacheElements;
    public uint MaxNumFonts;

    public static UiContextDesc Default => new()
    {
        MaxNumElements = 8192,
        MaxNumTextMeasureCacheElements = 16384,
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

        id = clay.HashString(StringView.Intern(name), 0, 0);
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

        id = clay.HashString(StringView.Intern(name), index, 0);
        _indexedCache[key] = id;
        return id;
    }
}