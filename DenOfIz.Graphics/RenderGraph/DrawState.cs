using DenOfIz;
using Graphics.Binding;

namespace Graphics.RenderGraph;

public struct DrawState
{
    public Shader? Shader = null;
    public string Variant = "default";
    public Dictionary<string, byte[]> Data = new();
    public Dictionary<string, Texture> Textures = new();
    public Dictionary<string, Sampler> Samplers = new();

    public DrawState()
    {
    }
}