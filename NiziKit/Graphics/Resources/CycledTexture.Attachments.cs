using System.Numerics;
using DenOfIz;

namespace NiziKit.Graphics.Resources;

public partial class CycledTexture
{
    public static CycledTexture ColorAttachment(string name, int width = 0, int height = 0,
        Format format = Format.Undefined)
    {
        var desc = Common(name, width, height);
        desc.Format = format == Format.Undefined ? GraphicsContext.BackBufferFormat : format;
        desc.Usage = (uint)(TextureUsageFlagBits.RenderAttachment | TextureUsageFlagBits.TextureBinding);
        return new CycledTexture(desc);
    }

    public static CycledTexture DepthAttachment(string name, int width = 0, int height = 0)
    {
        var desc = Common(name, width, height);
        desc.Format = Format.D32Float;
        desc.Usage = (uint)(TextureUsageFlagBits.RenderAttachment  | TextureUsageFlagBits.TextureBinding);
        desc.ClearDepthStencilHint = new Vector2(1.0f, 0.0f);
        return new CycledTexture(desc);
    }

    public static CycledTexture DepthStencilAttachment(string name, int width = 0, int height = 0)
    {
        var desc = Common(name, width, height);
        desc.Format = Format.D24UnormS8Uint;
        return new CycledTexture(desc);
    }

    private static TextureDesc Common(string name, int width, int height)
    {
        return new TextureDesc
        {
            Width = width == 0 ? GraphicsContext.Width : (uint)width,
            Height = height == 0 ? GraphicsContext.Height : (uint)height,
            Depth = 1,
            DebugName = StringView.Intern(name),
            ArraySize = 1,
            MipLevels = 1
        };
    }
}