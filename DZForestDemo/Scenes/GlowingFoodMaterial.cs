using NiziKit.Assets;
using NiziKit.ContentPipeline;
using NiziKit.Graphics.Binding;

namespace DZForestDemo.Scenes;

public class GlowingFoodMaterial : Material
{
    private readonly ColorTexture _colorTexture;

    public GlowingFoodMaterial(string name, byte r, byte g, byte b)
    {
        Name = name;
        _colorTexture = new ColorTexture(r, g, b, 255, name);
        Albedo = new Texture2d
        {
            Name = name,
            Width = 1,
            Height = 1,
            GpuTexture = _colorTexture.Texture
        };
        GpuShader = Content.LoadShader("Shaders/GlowingFood.nizishp.json");
    }

    public override void Dispose()
    {
        _colorTexture.Dispose();
        base.Dispose();
    }
}