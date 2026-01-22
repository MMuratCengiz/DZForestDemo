using NiziKit.Assets.Store;

namespace NiziKit.Assets;

public class DefaultMaterial : Material
{
    internal DefaultMaterial(ShaderStore shaderStore)
    {
        Name = "Default";
        GpuShader = shaderStore["Builtin/Shaders/Default"];
    }
}
