using NiziKit.Assets;

namespace NiziKit.Components;

public partial class MeshComponent : NiziComponent
{
    [AssetRef(AssetRefType.Mesh, "mesh")]
    public partial Mesh? Mesh { get; set; }
}
