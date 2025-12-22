using DenOfIz;
using Graphics.Binding;

namespace Graphics.RenderGraph;

public struct DrawState
{
    public enum ResourceType
    {
        Data,
        Texture,
        Sampler
    }

    public struct Resource
    {
        public ResourceType Type;
        public byte[]? Data;
        public Texture? Texture;
        public Sampler? Sampler;

        public Resource(byte[] data)
        {
            Type = ResourceType.Data;
            Data = data;
        }

        public Resource(Texture texture)
        {
            Type = ResourceType.Texture;
            Texture = texture;
        }

        public Resource(Sampler sampler)
        {
            Type = ResourceType.Sampler;
            Sampler = sampler;
        }
    }

    public Shader? Shader = null;
    public string Variant = "default";
    public Dictionary<string, Resource> Resources = new();

    public DrawState()
    {
    }
}