using DenOfIz;

namespace Graphics.Binding;

public readonly struct SrvUavData(Texture? texture, GpuBufferView? buffer)
{
    public readonly Texture? Texture = texture;
    public readonly GpuBufferView? Buffer = buffer;
}