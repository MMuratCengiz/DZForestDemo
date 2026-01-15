using System.Numerics;
using DenOfIz;
using Buffer = DenOfIz.Buffer;

namespace NiziKit.Graphics.Graph;

public abstract class RenderPassBase : IDisposable
{
    public abstract string Name { get; }
    public virtual ReadOnlySpan<string> Reads => [];
    public virtual ReadOnlySpan<AttachmentDesc> Writes => [];

    private readonly Dictionary<string, Texture[]> _textures = new();
    private readonly Dictionary<string, Buffer[]> _buffers = new();
    private readonly uint _numFrames = GraphicsContext.NumFrames;
    private uint _currentFrameIndex;
    private uint _width = GraphicsContext.Width;
    private uint _height = GraphicsContext.Height;


    public Texture GetAttachment(string name) => _textures[name][_currentFrameIndex];
    public Buffer GetBufferAttachment(string name) => _buffers[name][_currentFrameIndex];

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
                RecreateTextures(desc);
            }
            else
            {
                EnsureTextures(desc);
            }
        }

        RegisterResources(resources);
        OnResize(width, height);
    }

    protected void RegisterResources(FrameResources resources)
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

    protected void EnsureTextures(AttachmentDesc desc)
    {
        if (_textures.ContainsKey(desc.Name))
        {
            return;
        }
        CreateTextures(desc);
    }

    protected void RecreateTextures(AttachmentDesc desc)
    {
        if (_textures.TryGetValue(desc.Name, out var existing))
        {
            foreach (var texture in existing)
            {
                texture.Dispose();
            }
        }

        CreateTextures(desc);
    }

    protected void CreateTextures(AttachmentDesc desc)
    {
        if (desc.IsAutoSize)
        {
            desc.Width = _width;
            desc.Height = _height;
        }

        if (desc is { Format: Format.Undefined, Type: AttachmentType.Color })
        {
            desc.Format = GraphicsContext.BackBufferFormat;
        }

        var textures = new Texture[_numFrames];
        for (var i = 0; i < _numFrames; i++)
        {
            var texture = GraphicsContext.Device.CreateTexture(new TextureDesc
            {
                Width = desc.Width,
                Height = desc.Height,
                Depth = 1,
                Format = desc.Format,
                MipLevels = 1,
                ArraySize = 1,
                Usage = desc.Usage,
                HeapType = HeapType.Gpu,
                DebugName = StringView.Intern($"{desc.Name}_{i}"),
                ClearDepthStencilHint = new Vector2(1, 0)
            });

            GraphicsContext.ResourceTracking.TrackTexture(texture, QueueType.Graphics);
            textures[i] = texture;
        }

        _textures[desc.Name] = textures;
    }

    protected void EnsureBuffers(AttachmentDesc desc)
    {
        if (_buffers.ContainsKey(desc.Name))
        {
            return;
        }

        var buffers = new Buffer[_numFrames];
        for (var i = 0; i < _numFrames; i++)
        {
            var buffer = GraphicsContext.Device.CreateBuffer(new BufferDesc
            {
                NumBytes = desc.NumBytes,
                Usage = (uint)BufferUsageFlagBits.Storage,
                HeapType = HeapType.Gpu,
                DebugName = StringView.Intern($"{desc.Name}_{i}")
            });

            GraphicsContext.ResourceTracking.TrackBuffer(buffer, QueueType.Graphics);
            buffers[i] = buffer;
        }

        _buffers[desc.Name] = buffers;
    }

    public virtual void Dispose()
    {

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
    }
}
