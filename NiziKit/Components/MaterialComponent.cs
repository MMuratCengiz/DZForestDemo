using NiziKit.Assets;

namespace NiziKit.Components;

[NiziComponent(GenerateFactory = false)]
public partial class MaterialComponent
{
    [AssetRef(AssetRefType.Material, "material")]
    public partial Material? Material { get; set; }
}
