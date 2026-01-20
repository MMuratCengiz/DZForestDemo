using NiziKit.Assets;

namespace NiziKit.Components;

[NiziComponent]
public partial class MaterialComponent
{
    [AssetRef(AssetRefType.Material, "material")]
    public partial Material? Material { get; set; }
}
