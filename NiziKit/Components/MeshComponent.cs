using NiziKit.Assets;

namespace NiziKit.Components;

[NiziComponent]
public partial class MeshComponent
{
    [AssetRef(AssetRefType.Mesh, "mesh")]
    public partial Mesh? Mesh { get; set; }
}
