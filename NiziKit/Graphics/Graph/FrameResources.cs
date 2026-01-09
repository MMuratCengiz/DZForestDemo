using DenOfIz;
using Buffer = DenOfIz.Buffer;

namespace NiziKit.Graphics.Graph;

public class FrameResources
{
    private readonly Dictionary<string, Texture> _textures = new(16);
    private readonly Dictionary<string, Buffer> _buffers = new(16);

    public void Set(string name, Texture texture)
    {
        _textures[name] = texture;
    }

    public void Set(string name, Buffer buffer)
    {
        _buffers[name] = buffer;
    }

    public Texture GetTexture(string name)
    {
        if (!_textures.TryGetValue(name, out var texture))
        {
            throw new InvalidOperationException($"Texture '{name}' not found");
        }
        return texture;
    }

    public Buffer GetBuffer(string name)
    {
        if (!_buffers.TryGetValue(name, out var buffer))
        {
            throw new InvalidOperationException($"Buffer '{name}' not found");
        }
        return buffer;
    }

    public bool TryGetTexture(string name, out Texture? texture) => _textures.TryGetValue(name, out texture);
    public bool TryGetBuffer(string name, out Buffer? buffer) => _buffers.TryGetValue(name, out buffer);

    public void Clear()
    {
        _textures.Clear();
        _buffers.Clear();
    }
}
