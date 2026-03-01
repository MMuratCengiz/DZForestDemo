using System.Numerics;
using NiziKit.Assets;

namespace NiziKit.Components;

public partial class SpriteComponent : NiziComponent
{
    [AssetRef(AssetRefType.Texture, "texture")]
    public partial Texture2d? Texture { get; set; }

    [Color] public Vector4 Color { get; set; } = Vector4.One;
    public Vector4 UVRect { get; set; } = new(0, 0, 1, 1);
    public Vector2 Size { get; set; } = Vector2.Zero;
    public Vector2 Pivot { get; set; } = new(0.5f, 0.5f);
    public int SortingLayer { get; set; } = 0;
    public int SortOrder { get; set; } = 0;
    public bool FlipX { get; set; }
    public bool FlipY { get; set; }
}
