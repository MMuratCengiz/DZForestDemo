using DenOfIz;

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
    public uint Usages;
    public uint Descriptor;
    public string DebugName;

    public static TransientTextureDesc RenderTarget(uint width, uint height, Format format, string debugName = "") => new()
    {
        Width = width,
        Height = height,
        Depth = 1,
        Format = format,
        MipLevels = 1,
        ArraySize = 1,
        Usages = (uint)(ResourceUsageFlagBits.RenderTarget | ResourceUsageFlagBits.ShaderResource),
        Descriptor = (uint)(ResourceDescriptorFlagBits.RenderTarget | ResourceDescriptorFlagBits.Texture),
        DebugName = debugName
    };

    public static TransientTextureDesc DepthStencil(uint width, uint height, Format format = Format.D32Float, string debugName = "") => new()
    {
        Width = width,
        Height = height,
        Depth = 1,
        Format = format,
        MipLevels = 1,
        ArraySize = 1,
        Usages = (uint)(ResourceUsageFlagBits.DepthWrite | ResourceUsageFlagBits.DepthRead),
        Descriptor = (uint)ResourceDescriptorFlagBits.DepthStencil,
        DebugName = debugName
    };
}

public struct TransientBufferDesc
{
    public ulong NumBytes;
    public uint Usages;
    public uint Descriptor;
    public HeapType HeapType;
    public string DebugName;
}

internal class RenderGraphResourceEntry
{
    public RenderGraphResourceType Type;
    public int Version;
    public bool IsImported;
    public bool IsTransient;

    public TextureResource? Texture;
    public TransientTextureDesc TextureDesc;

    public BufferResource? Buffer;
    public TransientBufferDesc BufferDesc;

    public int FirstPassIndex = -1;
    public int LastPassIndex = -1;

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
