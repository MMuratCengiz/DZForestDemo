using System.Numerics;
using NiziKit.Assets;

namespace NiziKit.Components;

[NiziComponent]
public partial class SurfaceComponent
{
    [AssetRef(AssetRefType.Texture, "albedo")]
    public partial Texture2d? Albedo { get; set; }
    public string? AlbedoRef { get; set; }

    [AssetRef(AssetRefType.Texture, "normal")]
    public partial Texture2d? Normal { get; set; }
    public string? NormalRef { get; set; }

    [AssetRef(AssetRefType.Texture, "metallic")]
    public partial Texture2d? Metallic { get; set; }
    public string? MetallicRef { get; set; }

    [AssetRef(AssetRefType.Texture, "roughness")]
    public partial Texture2d? Roughness { get; set; }
    public string? RoughnessRef { get; set; }

    public float MetallicValue { get; set; } = 0.0f;
    public float RoughnessValue { get; set; } = 0.5f;
    public Vector4 AlbedoColor { get; set; } = Vector4.One;
    public Vector3 EmissiveColor { get; set; } = Vector3.Zero;
    public float EmissiveIntensity { get; set; } = 0.0f;
    public Vector2 UVScale { get; set; } = Vector2.One;
    public Vector2 UVOffset { get; set; } = Vector2.Zero;
}
