using DenOfIz;

namespace Graphics.Binding;

public sealed class ShaderBinding : IDisposable
{
    public ResourceBindGroup BindGroup { get; }

    private readonly BindingContext _ctx;
    private bool _disposed;

    public int PoolHandle { get; internal set; } = -1;

    public ShaderBinding(BindingContext ctx, uint registerSpace)
    {
        _ctx = ctx;

        ResourceBindGroupDesc groupDesc = new()
        {
            RootSignature = _ctx.RootSignature.Instance,
            RegisterSpace = registerSpace
        };
        BindGroup = _ctx.LogicalDevice.CreateResourceBindGroup(groupDesc);
    }

    public void ApplyBindGroupData(BindGroupData data)
    {
        BindGroup.BeginUpdate();

        var mask = data.SetMask;
        for (uint i = 0; i < 16 && mask != 0; i++)
        {
            if ((mask & (1u << (int)i)) == 0)
            {
                continue;
            }

            ref readonly var entry = ref data.GetEntry(i);
            if (!entry.IsSet)
            {
                continue;
            }

            switch (entry.Type)
            {
                case ResourceBindingType.ShaderResource:
                    if (entry.Texture != null)
                    {
                        BindGroup.SrvTexture(i, entry.Texture);
                    }
                    else if (entry.Buffer != null)
                    {
                        BindGroup.SrvBufferWithDesc(new BindBufferDesc
                        {
                            Binding = i,
                            Resource = entry.Buffer,
                            ResourceOffset = entry.BufferOffset
                        });
                    }
                    break;

                case ResourceBindingType.UnorderedAccess:
                    if (entry.Texture != null)
                    {
                        BindGroup.UavTexture(i, entry.Texture);
                    }
                    else if (entry.Buffer != null)
                    {
                        BindGroup.UavBufferWithDesc(new BindBufferDesc
                        {
                            Binding = i,
                            Resource = entry.Buffer,
                            ResourceOffset = entry.BufferOffset
                        });
                    }
                    break;

                case ResourceBindingType.ConstantBuffer:
                    if (entry.InlineData != null)
                    {
                        var size = (ulong)entry.InlineData.Length;
                        var bufferView = _ctx.GetFreeCpuVisibleAddress(this, $"slot_{i}", size);
                        unsafe
                        {
                            fixed (byte* src = entry.InlineData)
                            {
                                System.Buffer.MemoryCopy(src, bufferView.MappedMemory.ToPointer(), size, size);
                            }
                        }
                        BindGroup.CbvWithDesc(new BindBufferDesc
                        {
                            Binding = i,
                            Resource = bufferView.Buffer,
                            ResourceOffset = bufferView.Offset
                        });
                    }
                    else if (entry.Buffer != null)
                    {
                        BindGroup.CbvWithDesc(new BindBufferDesc
                        {
                            Binding = i,
                            Resource = entry.Buffer,
                            ResourceOffset = entry.BufferOffset
                        });
                    }
                    break;

                case ResourceBindingType.Sampler:
                    if (entry.Sampler != null)
                    {
                        BindGroup.Sampler(i, entry.Sampler);
                    }
                    break;
            }
        }

        BindGroup.EndUpdate();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        BindGroup.Dispose();
        GC.SuppressFinalize(this);
    }
}
