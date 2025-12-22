using DenOfIz;

namespace Graphics.Binding;

public readonly struct SrvUavData(Texture? texture, GPUBufferView? buffer)
{
    public readonly Texture? Texture = texture;
    public readonly GPUBufferView? Buffer = buffer;
}