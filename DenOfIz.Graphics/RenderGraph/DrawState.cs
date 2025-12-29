using DenOfIz;
using Graphics.Binding;
using Buffer = DenOfIz.Buffer;

namespace Graphics.RenderGraph;

public struct DrawState
{
    public enum ResourceType
    {
        Data,
        Texture,
        Sampler,
        Buffer
    }

    public struct Resource
    {
        public readonly ResourceType Type;
        public readonly byte[]? Data;
        public readonly Texture? Texture;
        public readonly Sampler? Sampler;
        public readonly Buffer? Buffer;
        public readonly ulong BufferOffset;
        public readonly ulong BufferSize;

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

        public Resource(Buffer buffer, ulong offset = 0, ulong size = 0)
        {
            Type = ResourceType.Buffer;
            Buffer = buffer;
            BufferOffset = offset;
            BufferSize = size;
        }

        public Resource(GpuBufferView bufferView)
        {
            Type = ResourceType.Buffer;
            Buffer = bufferView.Buffer;
            BufferOffset = bufferView.Offset;
            BufferSize = bufferView.NumBytes;
        }
    }

    public Shader? Shader = null;
    public string Variant = "default";
    public readonly Dictionary<string, Resource> Resources = new();

    public DrawState()
    {
    }

    public void BuildBindGroupData(ShaderRootSignature rootSignature, uint registerSpace, BindGroupData data)
    {
        data.Reset();
        var slots = rootSignature.GetSlotsForSpace(registerSpace);

        foreach (var (name, resource) in Resources)
        {
            if (!rootSignature.TryGetSlot(name, out var slot))
            {
                continue;
            }

            if (slot.RegisterSpace != registerSpace)
            {
                continue;
            }

            switch (resource.Type)
            {
                case ResourceType.Data:
                    if (resource.Data != null)
                    {
                        data.SetData(slot, resource.Data);
                    }

                    break;
                case ResourceType.Texture:
                    if (resource.Texture != null)
                    {
                        data.SetTexture(slot, resource.Texture);
                    }

                    break;
                case ResourceType.Sampler:
                    if (resource.Sampler != null)
                    {
                        data.SetSampler(slot, resource.Sampler);
                    }

                    break;
                case ResourceType.Buffer:
                    if (resource.Buffer != null)
                    {
                        data.SetBuffer(slot, resource.Buffer, resource.BufferOffset, resource.BufferSize);
                    }

                    break;
            }
        }
    }
}
