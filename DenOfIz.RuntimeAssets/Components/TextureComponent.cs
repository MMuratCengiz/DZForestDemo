using RuntimeAssets;

namespace ECS.Components;

public struct TextureComponent
{
    public RuntimeTextureHandle Texture;

    public TextureComponent(RuntimeTextureHandle texture)
    {
        Texture = texture;
    }

    public bool IsValid => Texture.IsValid;
}

public struct MaterialComponent
{
    public RuntimeTextureHandle BaseColorTexture;
    public RuntimeTextureHandle NormalTexture;
    public RuntimeTextureHandle MetallicRoughnessTexture;

    public bool HasBaseColor => BaseColorTexture.IsValid;
    public bool HasNormal => NormalTexture.IsValid;
    public bool HasMetallicRoughness => MetallicRoughnessTexture.IsValid;
}
