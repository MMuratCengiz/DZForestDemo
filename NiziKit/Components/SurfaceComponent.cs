using System.Numerics;
using NiziKit.Assets;

namespace NiziKit.Components;

public partial class SurfaceComponent : NiziComponent
{
    [AssetRef(AssetRefType.Texture, "albedo")]
    public partial Texture2d? Albedo { get; set; }

    [AssetRef(AssetRefType.Texture, "normal")]
    public partial Texture2d? Normal { get; set; }

    [AssetRef(AssetRefType.Texture, "metallic")]
    public partial Texture2d? Metallic { get; set; }

    [AssetRef(AssetRefType.Texture, "roughness")]
    public partial Texture2d? Roughness { get; set; }

    [AssetRef(AssetRefType.Texture, "emissive")]
    public partial Texture2d? Emissive { get; set; }

    public float MetallicValue { get; set; } = 0.0f;
    public float RoughnessValue { get; set; } = 0.5f;
    [Color] public Vector4 AlbedoColor { get; set; } = Vector4.One;
    [Color] public Vector3 EmissiveColor { get; set; } = Vector3.Zero;
    public float EmissiveIntensity { get; set; } = 0.0f;
    public Vector2 UVScale { get; set; } = Vector2.One;
    public Vector2 UVOffset { get; set; } = Vector2.Zero;
}
