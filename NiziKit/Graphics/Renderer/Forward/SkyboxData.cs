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

    public string? RightRef { get; set; }
    public string? LeftRef { get; set; }
    public string? UpRef { get; set; }
    public string? DownRef { get; set; }
    public string? FrontRef { get; set; }
    public string? BackRef { get; set; }

    public bool IsValid => Right != null || Left != null || Up != null ||
                           Down != null || Front != null || Back != null;
}
