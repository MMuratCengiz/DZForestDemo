using DenOfIz;
using NiziKit.Assets.Store;
using NiziKit.Graphics;

namespace NiziKit.Assets;

public class DefaultMaterial : Material
{
    public DefaultMaterial(GraphicsContext context, ShaderStore shaderStore) : base(context)
    {
        Name = "Default";
        GpuShader = shaderStore["Builtin/Shaders/Default"];
    }
}