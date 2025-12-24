using DenOfIz;
using Buffer = DenOfIz.Buffer;

namespace Graphics.RenderGraph;

public enum RenderGraphResourceType
{
    Texture,
    Buffer
}

public struct TransientTextureDesc
{
    public uint Width;
    public uint Height;
    public uint Depth;
    public Format Format;
    public uint MipLevels;
    public uint ArraySize;
    public uint Usage;
    public string DebugName;
    public Float4 ClearColorHint;
    public Float2 ClearDepthStencilHint;

    public static TransientTextureDesc RenderTarget(uint width, uint height, Format format, string debugName = "")
    {
        return new TransientTextureDesc
        {
            Width = width,
            Height = height,
            Depth = 1,
            Format = format,
            MipLevels = 1,
            ArraySize = 1,
            Usage = (uint)(TextureUsageFlagBits.RenderAttachment | TextureUsageFlagBits.TextureBinding),
            DebugName = debugName
        };
    }

    public static TransientTextureDesc DepthStencil(uint width, uint height, Format format = Format.D32Float,
        string debugName = "", Float2? clearDepthStencilHint = null)
    {
        return new TransientTextureDesc
        {
            Width = width,
            Height = height,
            Depth = 1,
            Format = format,
            MipLevels = 1,
            ArraySize = 1,
            Usage = (uint)(TextureUsageFlagBits.RenderAttachment | TextureUsageFlagBits.TextureBinding),
            DebugName = debugName,
            ClearDepthStencilHint = clearDepthStencilHint ?? new Float2 { X = 1.0f, Y = 0.0f }
        };
    }
}

public struct TransientBufferDesc
{
    public ulong NumBytes;
    public uint Usage;
    public uint Descriptor;
    public HeapType HeapType;
    public string DebugName;
}

internal class RenderGraphResourceEntry
{
    public Buffer? Buffer;
    public TransientBufferDesc BufferDesc;

    public int FirstPassIndex = -1;
    public bool IsImported;
    public bool IsTransient;
    public int LastPassIndex = -1;

    public Texture? Texture;
    public TransientTextureDesc TextureDesc;
    public RenderGraphResourceType Type;
    public uint Version;

    public void Reset()
    {
        Version++;
        IsImported = false;
        IsTransient = false;
        Texture = null;
        Buffer = null;
        FirstPassIndex = -1;
        LastPassIndex = -1;
    }
}