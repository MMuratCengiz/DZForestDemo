using System.Runtime.CompilerServices;
using DenOfIz;

namespace UIFramework;

/// <summary>
/// Result from ending a UI frame.
/// </summary>
public readonly struct UiFrameResult
{
    public readonly TextureResource? Texture;
    public readonly DenOfIz.Semaphore? Semaphore;

    internal UiFrameResult(ClayRenderResult result)
    {
        Texture = result.GetTexture();
        Semaphore = result.GetSemaphore();
    }
}

/// <summary>
/// Main entry point for the UI framework. Wraps Clay with a fluent, zero-allocation API.
/// </summary>
public sealed class UiContext : IDisposable
{
    private bool _mouseJustPressed;

    private bool _disposed;

    /// <summary>
    /// Creates a new UI context.
    /// </summary>
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

    /// <summary>
    /// Gets the underlying Clay instance for advanced usage.
    /// </summary>
    public Clay Clay { get; }

    /// <summary>
    /// Gets the string cache for ID hashing.
    /// </summary>
    internal StringCache StringCache { get; }

    /// <summary>
    /// Updates viewport size (call on resize).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetViewportSize(uint width, uint height) => Clay.SetViewportSize(width, height);

    /// <summary>
    /// Handles input events. Call for each event in your event loop.
    /// </summary>
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

    /// <summary>
    /// Updates scroll containers. Call once per frame after handling events.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void UpdateScroll(float deltaTime, bool enableDrag = false)
    {
        Clay.UpdateScrollContainers(enableDrag, new Float2 { X = 0, Y = 0 }, deltaTime);
    }

    /// <summary>
    /// Begins a new UI frame. Returns a builder for the root element.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiFrame BeginFrame()
    {
        Clay.BeginLayout();
        return new UiFrame(this);
    }

    /// <summary>
    /// Ends the UI frame and returns the render result.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal UiFrameResult EndFrame(uint frameIndex, float deltaTime)
    {
        var result = Clay.EndLayout(frameIndex, deltaTime);

        // Reset per-frame input state
        MouseJustReleased = false;
        _mouseJustPressed = false;

        return new UiFrameResult(result);
    }

    /// <summary>
    /// Checks if the element with given ID is being hovered.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool IsHovered(uint id) => Clay.PointerOver(id);

    /// <summary>
    /// Gets the interaction state for an element.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal UiInteraction GetInteraction(uint id)
    {
        var hovered = Clay.PointerOver(id);
        var pressed = hovered && MousePressed;
        var clicked = hovered && MouseJustReleased;
        return new UiInteraction(hovered, pressed, clicked);
    }

    /// <summary>
    /// Whether a click was just released this frame.
    /// </summary>
    internal bool MouseJustReleased { get; private set; }

    /// <summary>
    /// Whether mouse button is currently pressed.
    /// </summary>
    internal bool MousePressed { get; private set; }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        Clay.Dispose();
        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// Descriptor for creating a UIContext.
/// </summary>
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

    /// <summary>
    /// Creates a descriptor with sensible defaults.
    /// </summary>
    public static UiContextDesc Default => new()
    {
        MaxNumElements = 8192,
        MaxNumTextMeasureCacheElements = 16384,
        MaxNumFonts = 16
    };
}

/// <summary>
/// Caches string hashes for element IDs to avoid repeated allocations.
/// </summary>
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
        // Use tuple key to avoid string allocation
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
