using NiziKit.Assets;

namespace NiziKit.Graphics.Renderer.Forward;

public class SkyboxData
{
    public Texture2d? Right { get; set; }
    public Texture2d? Left { get; set; }
    public Texture2d? Up { get; set; }
    public Texture2d? Down { get; set; }
    public Texture2d? Front { get; set; }
    public Texture2d? Back { get; set; }

    public bool IsValid => Right != null || Left != null || Up != null ||
                           Down != null || Front != null || Back != null;
}
