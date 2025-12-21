using System.Runtime.CompilerServices;
using DenOfIz;

namespace Graphics.Shader.Binding;

public class ShaderBinding
{
    public ResourceBindGroup BindGroup { get; }

    private struct SrvUavData(Texture? texture, GPUBufferView? buffer)
    {
        public readonly Texture? Texture = texture;
        public readonly GPUBufferView? Buffer = buffer;
    }

    private readonly uint _registerSpace;
    private readonly BindingContext _ctx;

    private readonly List<GPUBufferView> _cbvData = [];
    private readonly List<SrvUavData> _srvData = [];
    private readonly List<SrvUavData> _uavData = [];
    private readonly List<Sampler> _samplers = [];

    private bool _isDirty = true;

    public ShaderBinding(BindingContext ctx, uint registerSpace)
    {
        _ctx = ctx;
        _registerSpace = registerSpace;

        ResourceBindGroupDesc groupDesc = new()
        {
            RootSignature = _ctx.RootSignature.Instance,
            RegisterSpace = _registerSpace
        };
        BindGroup = _ctx.LogicalDevice.CreateResourceBindGroup(groupDesc);
    }

    public void Update()
    {
        if (!_isDirty)
        {
            return;
        }

        var bindingSlots = _ctx.ResourceBindingSlots;
        BindGroup.BeginUpdate();
        for (var i = 0; i < bindingSlots.Count; ++i)
        {
            BindResourceSlotsUpdateSlot(bindingSlots[i]);
        }

        BindGroup.EndUpdate();
    }

    private void BindResourceSlotsUpdateSlot(ResourceBindingSlot slot)
    {
        switch (slot.Type)
        {
            case ResourceBindingType.ConstantBuffer:
                var data = _cbvData[(int)slot.Binding];
                BindBufferDesc bindBufferDesc = new()
                {
                    Binding = slot.Binding,
                    Resource = data.Buffer,
                    ResourceOffset = data.Offset
                };
                BindGroup.CbvWithDesc(bindBufferDesc);
                break;
            case ResourceBindingType.ShaderResource:
                var srvData = _srvData[(int)slot.Binding];
                if (srvData.Texture != null)
                {
                    BindGroup.SrvTexture(slot.Binding, srvData.Texture);
                }
                else if (srvData.Buffer != null)
                {
                    BindGroup.SrvBufferWithDesc(
                        new BindBufferDesc
                        {
                            Binding = slot.Binding,
                            Resource = srvData.Buffer.Value.Buffer,
                            ResourceOffset = srvData.Buffer.Value.Offset
                        });
                }

                break;
            case ResourceBindingType.UnorderedAccess:
                var uavData = _uavData[(int)slot.Binding];
                if ((ulong)uavData.Texture == 0)
                {
                    BindGroup.UavTexture(slot.Binding, uavData.Texture);
                }
                else if (uavData.Buffer != null)
                {
                    BindGroup.UavBufferWithDesc(
                        new BindBufferDesc
                        {
                            Binding = slot.Binding,
                            Resource = uavData.Buffer.Value.Buffer,
                            ResourceOffset = uavData.Buffer.Value.Offset
                        });
                }

                break;
            case ResourceBindingType.Sampler:
                var sampler = _samplers[(int)slot.Binding];
                BindGroup.Sampler(slot.Binding, sampler);
                break;
        }
    }

    public void SetTexture(string name, Texture texture)
    {
        _isDirty = true;
        var slot = _ctx.GetSlot(name);
        switch (slot.Type)
        {
            case ResourceBindingType.ConstantBuffer:
                break;
            case ResourceBindingType.ShaderResource:
                _srvData[(int)slot.Binding] = new SrvUavData(texture, null);
                break;
            case ResourceBindingType.UnorderedAccess:
                _uavData[(int)slot.Binding] = new SrvUavData(texture, null);
                break;
            case ResourceBindingType.Sampler:
                break;
        }
    }

    public void SetData(string name, ReadOnlySpan<byte> data)
    {
        var slot = _ctx.GetSlot(name);
        var freeAddress = _ctx.GetFreeCpuVisibleAddress(this, name);

        BindBufferDesc bindBufferDesc = new()
        {
            Binding = slot.Binding,
            Resource = freeAddress.Buffer,
            ResourceOffset = freeAddress.Offset
        };

        if (slot.Type != ResourceBindingType.ConstantBuffer)
        {
            throw new ArgumentException("Only constant buffers can be set with SetData");
        }

        _cbvData[(int)slot.Binding] = new GPUBufferView
            { Buffer = freeAddress.Buffer, NumBytes = freeAddress.NumBytes, Offset = freeAddress.Offset };

        unsafe
        {
            Unsafe.Write(freeAddress.MappedMemory.ToPointer(), data[..(int)freeAddress.NumBytes]);
        }
    }

    public void SetBuffer(string name, GPUBufferView buffer)
    {
        var slot = _ctx.GetSlot(name);
        switch (slot.Type)
        {
            case ResourceBindingType.ConstantBuffer:
                _cbvData[(int)slot.Binding] = buffer;
                break;
            case ResourceBindingType.ShaderResource:
                _srvData[(int)slot.Binding] = new SrvUavData(null, buffer);
                break;
            case ResourceBindingType.UnorderedAccess:
                _uavData[(int)slot.Binding] = new SrvUavData(null, buffer);
                break;
            case ResourceBindingType.Sampler:
                break;
        }
    }

    public void SetSampler(string name, Sampler sampler)
    {
        var slot = _ctx.GetSlot(name);
        _samplers[(int)slot.Binding] = sampler;
    }

    public ShaderBinding Copy()
    {
        ShaderBinding @new = new(_ctx, _registerSpace);
        @new._cbvData.AddRange(_cbvData);
        @new._srvData.AddRange(_srvData);
        @new._uavData.AddRange(_uavData);
        @new.Update();
        return @new;
    }
}