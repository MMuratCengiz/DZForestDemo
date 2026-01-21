using DenOfIz;
using NiziKit.Graphics;

namespace NiziKit.Skia;

/// <summary>
/// Tracks resource state transitions between Skia and DenOfIz.
/// Ensures proper synchronization when resources are used by both systems.
/// </summary>
public sealed class SkiaResourceTracker : IDisposable
{
    private static SkiaResourceTracker? _instance;
    public static SkiaResourceTracker Instance => _instance ?? throw new InvalidOperationException("SkiaResourceTracker not initialized");

    private readonly ResourceTracking _resourceTracking;
    private readonly HashSet<Texture> _skiaOwnedTextures = [];
    private readonly object _lock = new();
    private bool _disposed;

    /// <summary>
    /// Creates a new SkiaResourceTracker using the GraphicsContext's ResourceTracking.
    /// </summary>
    public SkiaResourceTracker() : this(GraphicsContext.ResourceTracking)
    {
    }

    /// <summary>
    /// Creates a new SkiaResourceTracker with a specific ResourceTracking instance.
    /// </summary>
    /// <param name="resourceTracking">The DenOfIz ResourceTracking to use.</param>
    public SkiaResourceTracker(ResourceTracking resourceTracking)
    {
        _resourceTracking = resourceTracking ?? throw new ArgumentNullException(nameof(resourceTracking));
        _instance = this;
    }

    /// <summary>
    /// Registers a texture as being used by Skia.
    /// Tracks the texture and prepares it for Skia operations.
    /// </summary>
    /// <param name="texture">The texture to register.</param>
    /// <param name="queueType">The queue type the texture is associated with.</param>
    public void RegisterSkiaTexture(Texture texture, QueueType queueType = QueueType.Graphics)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(texture);

        lock (_lock)
        {
            if (_skiaOwnedTextures.Add(texture))
            {
                _resourceTracking.TrackTexture(texture, queueType);
            }
        }
    }

    /// <summary>
    /// Unregisters a texture from Skia ownership.
    /// </summary>
    /// <param name="texture">The texture to unregister.</param>
    public void UnregisterSkiaTexture(Texture texture)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(texture);

        lock (_lock)
        {
            if (_skiaOwnedTextures.Remove(texture))
            {
                _resourceTracking.UntrackTexture(texture);
            }
        }
    }

    /// <summary>
    /// Notifies DenOfIz that Skia has transitioned a texture to a new state.
    /// Call this after Skia operations that change texture state.
    /// </summary>
    /// <param name="texture">The texture that was transitioned.</param>
    /// <param name="newUsage">The new usage flags after Skia operations.</param>
    public void NotifySkiaTextureTransition(Texture texture, TextureUsageFlagBits newUsage)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(texture);

        _resourceTracking.NotifyTextureTransition(texture, (uint)newUsage);
    }

    /// <summary>
    /// Notifies DenOfIz that Skia has transitioned a buffer to a new state.
    /// </summary>
    /// <param name="buffer">The buffer that was transitioned.</param>
    /// <param name="newUsage">The new usage flags after Skia operations.</param>
    public void NotifySkiaBufferTransition(DenOfIz.Buffer buffer, BufferUsageFlagBits newUsage)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(buffer);

        _resourceTracking.NotifyBufferTransition(buffer, (uint)newUsage);
    }

    /// <summary>
    /// Prepares a texture for Skia rendering by transitioning it to the appropriate state.
    /// </summary>
    /// <param name="commandList">The command list to record the transition.</param>
    /// <param name="texture">The texture to prepare.</param>
    public void PrepareTextureForSkia(CommandList commandList, Texture texture)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(commandList);
        ArgumentNullException.ThrowIfNull(texture);

        // Transition to copy destination for upload operations from Skia
        _resourceTracking.TransitionTexture(
            commandList,
            texture,
            (uint)TextureUsageFlagBits.CopyDst,
            QueueType.Graphics);
    }

    /// <summary>
    /// Prepares a texture for DenOfIz rendering after Skia operations.
    /// </summary>
    /// <param name="commandList">The command list to record the transition.</param>
    /// <param name="texture">The texture to prepare.</param>
    /// <param name="targetUsage">The target usage for DenOfIz rendering.</param>
    public void PrepareTextureForDenOfIz(
        CommandList commandList,
        Texture texture,
        TextureUsageFlagBits targetUsage = TextureUsageFlagBits.TextureBinding)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(commandList);
        ArgumentNullException.ThrowIfNull(texture);

        _resourceTracking.TransitionTexture(
            commandList,
            texture,
            (uint)targetUsage,
            QueueType.Graphics);
    }

    /// <summary>
    /// Performs a batch transition of multiple textures between Skia and DenOfIz states.
    /// </summary>
    /// <param name="commandList">The command list to record transitions.</param>
    /// <param name="textures">The textures to transition.</param>
    /// <param name="toSkia">True to transition for Skia use, false for DenOfIz use.</param>
    public void BatchTransition(CommandList commandList, IEnumerable<Texture> textures, bool toSkia)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(commandList);
        ArgumentNullException.ThrowIfNull(textures);

        var targetUsage = toSkia
            ? TextureUsageFlagBits.CopyDst
            : TextureUsageFlagBits.TextureBinding;

        foreach (var texture in textures)
        {
            _resourceTracking.TransitionTexture(
                commandList,
                texture,
                (uint)targetUsage,
                QueueType.Graphics);
        }
    }

    /// <summary>
    /// Creates a scope that automatically handles resource transitions for Skia operations.
    /// </summary>
    /// <param name="commandList">The command list for transitions.</param>
    /// <param name="texture">The texture to manage.</param>
    /// <returns>A disposable scope that transitions back when disposed.</returns>
    public SkiaRenderScope BeginSkiaRender(CommandList commandList, Texture texture)
    {
        ThrowIfDisposed();
        return new SkiaRenderScope(this, commandList, texture);
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        lock (_lock)
        {
            foreach (var texture in _skiaOwnedTextures)
            {
                _resourceTracking.UntrackTexture(texture);
            }
            _skiaOwnedTextures.Clear();
        }

        if (_instance == this)
        {
            _instance = null;
        }
    }
}

/// <summary>
/// A scope that manages texture state for Skia rendering operations.
/// Automatically transitions texture state when entering and exiting the scope.
/// </summary>
public readonly struct SkiaRenderScope : IDisposable
{
    private readonly SkiaResourceTracker _tracker;
    private readonly CommandList _commandList;
    private readonly Texture _texture;

    internal SkiaRenderScope(SkiaResourceTracker tracker, CommandList commandList, Texture texture)
    {
        _tracker = tracker;
        _commandList = commandList;
        _texture = texture;

        // Transition texture for Skia use
        _tracker.PrepareTextureForSkia(_commandList, _texture);
    }

    /// <summary>
    /// Ends the Skia render scope and transitions the texture back for DenOfIz use.
    /// </summary>
    public void Dispose()
    {
        _tracker.PrepareTextureForDenOfIz(_commandList, _texture);
    }
}
