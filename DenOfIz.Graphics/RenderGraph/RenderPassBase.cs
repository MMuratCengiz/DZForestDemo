using DenOfIz;
using Graphics;
using Buffer = DenOfIz.Buffer;

namespace Graphics.RenderGraph;

public abstract class RenderPassBase : IDisposable
{
    protected GraphicsContext Context { get; }

    public abstract string Name { get; }
    public virtual ReadOnlySpan<string> Reads => [];
    public virtual ReadOnlySpan<AttachmentDesc> Writes => [];

    private readonly Dictionary<string, Texture[]> _textures = new();
    private readonly Dictionary<string, Buffer[]> _buffers = new();
    private readonly uint _numFrames;
    private uint _currentFrameIndex;
    private uint _width;
    private uint _height;
    private bool _disposed;

    protected RenderPassBase(GraphicsContext context)
    {
        Context = context;
        _numFrames = context.NumFrames;
    }

    protected Texture GetAttachment(string name) => _textures[name][_currentFrameIndex];
    protected Buffer GetBufferAttachment(string name) => _buffers[name][_currentFrameIndex];

    internal void Resize(uint width, uint height, uint frameIndex, FrameResources resources)
    {
        _currentFrameIndex = frameIndex;

        if (_width == width && _height == height)
        {
            RegisterResources(resources);
            return;
        }

        _width = width;
        _height = height;

        foreach (var desc in Writes)
        {
            if (desc.IsBuffer)
            {
                EnsureBuffers(desc);
            }
            else if (desc.IsAutoSize)
            {
                RecreateTextures(desc, width, height);
            }
            else
            {
                EnsureTextures(desc);
            }
        }

        RegisterResources(resources);
        OnResize(width, height);
    }

    private void RegisterResources(FrameResources resources)
    {
        foreach (var (name, textures) in _textures)
        {
            resources.Set(name, textures[_currentFrameIndex]);
        }

        foreach (var (name, buffers) in _buffers)
        {
            resources.Set(name, buffers[_currentFrameIndex]);
        }
    }

    protected virtual void OnResize(uint width, uint height) { }

    private void EnsureTextures(AttachmentDesc desc)
    {
        if (_textures.ContainsKey(desc.Name))
        {
            return;
        }

        var width = desc.Width > 0 ? desc.Width : _width;
        var height = desc.Height > 0 ? desc.Height : _height;
        CreateTextures(desc, width, height);
    }

    private void RecreateTextures(AttachmentDesc desc, uint width, uint height)
    {
        if (_textures.TryGetValue(desc.Name, out var existing))
        {
            foreach (var texture in existing)
            {
                texture.Dispose();
            }
        }

        CreateTextures(desc, width, height);
    }

    private void CreateTextures(AttachmentDesc desc, uint width, uint height)
    {
        var textures = new Texture[_numFrames];

        for (var i = 0; i < _numFrames; i++)
        {
            var texture = Context.LogicalDevice.CreateTexture(new TextureDesc
            {
                Width = width,
                Height = height,
                Depth = 1,
                Format = desc.Format,
                MipLevels = 1,
                ArraySize = 1,
                Usage = desc.Usage,
                HeapType = HeapType.Gpu,
                DebugName = StringView.Intern($"{desc.Name}_{i}")
            });

            Context.ResourceTracking.TrackTexture(texture, QueueType.Graphics);
            textures[i] = texture;
        }

        _textures[desc.Name] = textures;
    }

    private void EnsureBuffers(AttachmentDesc desc)
    {
        if (_buffers.ContainsKey(desc.Name))
        {
            return;
        }

        var buffers = new Buffer[_numFrames];

        for (var i = 0; i < _numFrames; i++)
        {
            var buffer = Context.LogicalDevice.CreateBuffer(new BufferDesc
            {
                NumBytes = desc.NumBytes,
                Usage = (uint)BufferUsageFlagBits.Storage,
                HeapType = HeapType.Gpu,
                DebugName = StringView.Intern($"{desc.Name}_{i}")
            });

            Context.ResourceTracking.TrackBuffer(buffer, QueueType.Graphics);
            buffers[i] = buffer;
        }

        _buffers[desc.Name] = buffers;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        foreach (var textures in _textures.Values)
        {
            foreach (var texture in textures)
            {
                texture.Dispose();
            }
        }

        foreach (var buffers in _buffers.Values)
        {
            foreach (var buffer in buffers)
            {
                buffer.Dispose();
            }
        }

        _textures.Clear();
        _buffers.Clear();

        OnDispose();
    }

    protected virtual void OnDispose() { }
}
