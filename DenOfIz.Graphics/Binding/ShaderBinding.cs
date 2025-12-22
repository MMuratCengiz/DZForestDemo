using System.Runtime.CompilerServices;
using DenOfIz;

namespace Graphics.Binding;

public sealed class ShaderBinding : IDisposable
{
    public ResourceBindGroup BindGroup { get; }

    private readonly uint _registerSpace;
    private readonly BindingContext _ctx;
    private readonly ResourceBindingSlot[] _slots;

    private readonly Dictionary<uint, GPUBufferView> _cbvData = [];
    private readonly Dictionary<uint, SrvUavData> _srvData = [];
    private readonly Dictionary<uint, SrvUavData> _uavData = [];
    private readonly Dictionary<uint, Sampler> _samplers = [];

    private bool _isDirty = true;
    private bool _disposed;

    public int PoolHandle { get; internal set; } = -1;
    public uint RegisterSpace => _registerSpace;
    public bool IsInitialized => _cbvData.Count > 0 || _srvData.Count > 0 || _uavData.Count > 0 || _samplers.Count > 0;

    public ShaderBinding(BindingContext ctx, uint registerSpace)
    {
        _ctx = ctx;
        _registerSpace = registerSpace;
        _slots = ctx.GetSlotsForSpace(registerSpace).ToArray();

        ResourceBindGroupDesc groupDesc = new()
        {
            RootSignature = _ctx.RootSignature.Instance,
            RegisterSpace = _registerSpace
        };
        BindGroup = _ctx.LogicalDevice.CreateResourceBindGroup(groupDesc);
    }

    public ShaderBinding(BindingContext ctx, uint registerSpace, ResourceBindGroup bindGroup)
    {
        _ctx = ctx;
        _registerSpace = registerSpace;
        _slots = ctx.GetSlotsForSpace(registerSpace).ToArray();
        BindGroup = bindGroup;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Update()
    {
        if (!_isDirty)
        {
            return;
        }

        BindGroup.BeginUpdate();
        foreach (var slot in _slots)
        {
            UpdateSlot(slot);
        }
        BindGroup.EndUpdate();
        _isDirty = false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void UpdateSlot(ResourceBindingSlot slot)
    {
        switch (slot.Type)
        {
            case ResourceBindingType.ConstantBuffer:
                if (_cbvData.TryGetValue(slot.Binding, out var cbvData))
                {
                    BindGroup.CbvWithDesc(new BindBufferDesc
                    {
                        Binding = slot.Binding,
                        Resource = cbvData.Buffer,
                        ResourceOffset = cbvData.Offset
                    });
                }
                break;

            case ResourceBindingType.ShaderResource:
                if (_srvData.TryGetValue(slot.Binding, out var srvData))
                {
                    if (srvData.Texture != null)
                    {
                        BindGroup.SrvTexture(slot.Binding, srvData.Texture);
                    }
                    else if (srvData.Buffer.HasValue)
                    {
                        BindGroup.SrvBufferWithDesc(new BindBufferDesc
                        {
                            Binding = slot.Binding,
                            Resource = srvData.Buffer.Value.Buffer,
                            ResourceOffset = srvData.Buffer.Value.Offset
                        });
                    }
                }
                break;

            case ResourceBindingType.UnorderedAccess:
                if (_uavData.TryGetValue(slot.Binding, out var uavData))
                {
                    if (uavData.Texture != null)
                    {
                        BindGroup.UavTexture(slot.Binding, uavData.Texture);
                    }
                    else if (uavData.Buffer.HasValue)
                    {
                        BindGroup.UavBufferWithDesc(new BindBufferDesc
                        {
                            Binding = slot.Binding,
                            Resource = uavData.Buffer.Value.Buffer,
                            ResourceOffset = uavData.Buffer.Value.Offset
                        });
                    }
                }
                break;

            case ResourceBindingType.Sampler:
                if (_samplers.TryGetValue(slot.Binding, out var sampler))
                {
                    BindGroup.Sampler(slot.Binding, sampler);
                }
                break;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetTexture(ResourceBindingSlot slot, Texture texture)
    {
        switch (slot.Type)
        {
            case ResourceBindingType.ShaderResource:
                if (!_srvData.TryGetValue(slot.Binding, out var existingSrv) || existingSrv.Texture != texture)
                {
                    _srvData[slot.Binding] = new SrvUavData(texture, null);
                    _isDirty = true;
                }
                break;
            case ResourceBindingType.UnorderedAccess:
                if (!_uavData.TryGetValue(slot.Binding, out var existingUav) || existingUav.Texture != texture)
                {
                    _uavData[slot.Binding] = new SrvUavData(texture, null);
                    _isDirty = true;
                }
                break;
        }
    }

    public void SetTexture(string name, Texture texture)
    {
        SetTexture(_ctx.GetSlot(name), texture);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe void SetData<T>(ResourceBindingSlot slot, string allocationKey, in T data) where T : unmanaged
    {
        var size = (ulong)Unsafe.SizeOf<T>();
        var bufferView = _ctx.GetFreeCpuVisibleAddress(this, allocationKey, size);
        Unsafe.Write(bufferView.MappedMemory.ToPointer(), data);

        if (!_cbvData.TryGetValue(slot.Binding, out var existing) ||
            existing.Buffer != bufferView.Buffer || existing.Offset != bufferView.Offset)
        {
            _cbvData[slot.Binding] = new GPUBufferView
            {
                Buffer = bufferView.Buffer,
                NumBytes = bufferView.NumBytes,
                Offset = bufferView.Offset
            };
            _isDirty = true;
        }
    }

    public unsafe void SetData<T>(string name, in T data) where T : unmanaged
    {
        var slot = _ctx.GetSlot(name);
        if (slot.Type != ResourceBindingType.ConstantBuffer)
        {
            throw new ArgumentException("Only constant buffers can be set with SetData");
        }
        SetData(slot, name, data);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetBuffer(ResourceBindingSlot slot, GPUBufferView buffer)
    {
        switch (slot.Type)
        {
            case ResourceBindingType.ConstantBuffer:
                if (!_cbvData.TryGetValue(slot.Binding, out var existingCbv) ||
                    existingCbv.Buffer != buffer.Buffer || existingCbv.Offset != buffer.Offset)
                {
                    _cbvData[slot.Binding] = buffer;
                    _isDirty = true;
                }
                break;
            case ResourceBindingType.ShaderResource:
                if (!_srvData.TryGetValue(slot.Binding, out var existingSrv) ||
                    !existingSrv.Buffer.HasValue ||
                    existingSrv.Buffer.Value.Buffer != buffer.Buffer ||
                    existingSrv.Buffer.Value.Offset != buffer.Offset)
                {
                    _srvData[slot.Binding] = new SrvUavData(null, buffer);
                    _isDirty = true;
                }
                break;
            case ResourceBindingType.UnorderedAccess:
                if (!_uavData.TryGetValue(slot.Binding, out var existingUav) ||
                    !existingUav.Buffer.HasValue ||
                    existingUav.Buffer.Value.Buffer != buffer.Buffer ||
                    existingUav.Buffer.Value.Offset != buffer.Offset)
                {
                    _uavData[slot.Binding] = new SrvUavData(null, buffer);
                    _isDirty = true;
                }
                break;
        }
    }

    public void SetBuffer(string name, GPUBufferView buffer)
    {
        SetBuffer(_ctx.GetSlot(name), buffer);
    }

    public void SetBuffer(string name, DenOfIz.Buffer buffer, ulong offset = 0, ulong numBytes = 0)
    {
        SetBuffer(_ctx.GetSlot(name), new GPUBufferView
        {
            Buffer = buffer,
            Offset = offset,
            NumBytes = numBytes
        });
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetSampler(ResourceBindingSlot slot, Sampler sampler)
    {
        if (!_samplers.TryGetValue(slot.Binding, out var existing) || existing != sampler)
        {
            _samplers[slot.Binding] = sampler;
            _isDirty = true;
        }
    }

    public void SetSampler(string name, Sampler sampler)
    {
        SetSampler(_ctx.GetSlot(name), sampler);
    }

    public ShaderBinding Copy()
    {
        var copy = new ShaderBinding(_ctx, _registerSpace);
        foreach (var (k, v) in _cbvData)
        {
            copy._cbvData[k] = v;
        }

        foreach (var (k, v) in _srvData)
        {
            copy._srvData[k] = v;
        }

        foreach (var (k, v) in _uavData)
        {
            copy._uavData[k] = v;
        }

        foreach (var (k, v) in _samplers)
        {
            copy._samplers[k] = v;
        }

        copy.Update();
        return copy;
    }

    public void Reset()
    {
        _cbvData.Clear();
        _srvData.Clear();
        _uavData.Clear();
        _samplers.Clear();
        _isDirty = true;
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