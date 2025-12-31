using DenOfIz;

namespace Graphics.RenderGraph;

public enum AttachmentType
{
    Color,
    Depth,
    UavTexture,
    UavBuffer
}

public struct AttachmentDesc
{
    public string Name;
    public AttachmentType Type;
    public Format Format;
    public uint Width;
    public uint Height;
    public uint NumBytes;
    public uint Usage;

    public bool IsAutoSize => Width == 0 || Height == 0;
    public bool IsBuffer => Type == AttachmentType.UavBuffer;

    public static AttachmentDesc Color(string name, Format format, uint usage = 0)
    {
        return new AttachmentDesc
        {
            Name = name,
            Type = AttachmentType.Color,
            Format = format,
            Usage = usage != 0 ? usage : (uint)(TextureUsageFlagBits.RenderAttachment | TextureUsageFlagBits.TextureBinding)
        };
    }

    public static AttachmentDesc Depth(string name, Format format = Format.D32Float, uint usage = 0)
    {
        return new AttachmentDesc
        {
            Name = name,
            Type = AttachmentType.Depth,
            Format = format,
            Usage = usage != 0 ? usage : (uint)(TextureUsageFlagBits.RenderAttachment | TextureUsageFlagBits.TextureBinding)
        };
    }

    public static AttachmentDesc UavTexture(string name, Format format, uint usage = 0)
    {
        return new AttachmentDesc
        {
            Name = name,
            Type = AttachmentType.UavTexture,
            Format = format,
            Usage = usage != 0 ? usage : (uint)(TextureUsageFlagBits.StorageBinding | TextureUsageFlagBits.TextureBinding)
        };
    }

    public static AttachmentDesc UavBuffer(string name, uint numBytes)
    {
        return new AttachmentDesc
        {
            Name = name,
            Type = AttachmentType.UavBuffer,
            NumBytes = numBytes
        };
    }

    public static AttachmentDesc Color(string name, Format format, uint width, uint height, uint usage = 0)
    {
        var desc = Color(name, format, usage);
        desc.Width = width;
        desc.Height = height;
        return desc;
    }

    public static AttachmentDesc Depth(string name, uint width, uint height, Format format = Format.D32Float, uint usage = 0)
    {
        var desc = Depth(name, format, usage);
        desc.Width = width;
        desc.Height = height;
        return desc;
    }
}
