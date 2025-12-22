using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using DenOfIz;
using Graphics.Binding;

namespace Graphics;

public sealed class ShaderRootSignature : IDisposable
{
    private readonly Dictionary<string, ResourceBindingDesc> _bindingDescMap;
    private readonly Dictionary<string, ResourceBindingSlot> _bindingSlotMap;
    private readonly ResourceBindingSlot[] _slots;
    private readonly uint[] _registerSpaces;
    private readonly Dictionary<uint, (int Start, int Count)> _slotsBySpaceRange;
    private readonly ResourceBindingSlot[] _slotsBySpaceFlat;
    private bool _disposed;

    public RootSignature Instance { get; }

    private ShaderRootSignature(LogicalDevice device, Dictionary<string, ResourceBindingDesc> descMap)
    {
        _bindingDescMap = descMap;

        var bindingDescs = new ResourceBindingDesc[_bindingDescMap.Count];
        _bindingDescMap.Values.CopyTo(bindingDescs, 0);

        RootSignatureDesc rootSignatureDesc = new()
        {
            ResourceBindings = ResourceBindingDescArray.Create(bindingDescs)
        };
        Instance = device.CreateRootSignature(rootSignatureDesc);

        _bindingSlotMap = new Dictionary<string, ResourceBindingSlot>(_bindingDescMap.Count);
        foreach (var kvp in _bindingDescMap)
        {
            _bindingSlotMap[kvp.Key] = InitSlot(kvp.Key);
        }

        _slots = new ResourceBindingSlot[_bindingSlotMap.Count];
        _bindingSlotMap.Values.CopyTo(_slots, 0);

        var registerSpaceSet = new HashSet<uint>();
        foreach (var desc in _bindingDescMap.Values)
        {
            registerSpaceSet.Add(desc.RegisterSpace);
        }
        _registerSpaces = new uint[registerSpaceSet.Count];
        registerSpaceSet.CopyTo(_registerSpaces);

        _slotsBySpaceRange = new Dictionary<uint, (int Start, int Count)>(_registerSpaces.Length);
        var slotsBySpaceList = new List<ResourceBindingSlot>();
        foreach (var space in _registerSpaces)
        {
            var start = slotsBySpaceList.Count;
            foreach (var slot in _slots)
            {
                if (slot.RegisterSpace == space)
                {
                    slotsBySpaceList.Add(slot);
                }
            }
            _slotsBySpaceRange[space] = (start, slotsBySpaceList.Count - start);
        }
        _slotsBySpaceFlat = slotsBySpaceList.ToArray();
    }

    private ResourceBindingSlot InitSlot(string name)
    {
        var desc = _bindingDescMap[name];
        var descriptor = desc.Descriptor;
        var bindingType = ResourceBindingType.ShaderResource;
        if ((descriptor & (uint)ResourceDescriptorFlagBits.Sampler) == (uint)ResourceDescriptorFlagBits.Sampler)
        {
            bindingType = ResourceBindingType.Sampler;
        }

        if ((descriptor &
             (uint)(ResourceDescriptorFlagBits.UniformBuffer | ResourceDescriptorFlagBits.RootConstant)) != 0)
        {
            bindingType = ResourceBindingType.ConstantBuffer;
        }

        if ((descriptor & (uint)(ResourceDescriptorFlagBits.RwTexture | ResourceDescriptorFlagBits.RwBuffer)) != 0)
        {
            bindingType = ResourceBindingType.UnorderedAccess;
        }

        return new ResourceBindingSlot
        {
            Binding = desc.Binding,
            RegisterSpace = desc.RegisterSpace,
            Type = bindingType
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ResourceBindingSlot GetSlot(string name)
    {
        return _bindingSlotMap[name];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ResourceBindingDesc GetBindingDesc(string name)
    {
        return _bindingDescMap[name];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetSlot(string name, out ResourceBindingSlot slot)
    {
        return _bindingSlotMap.TryGetValue(name, out slot);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public BindingFrequency GetFrequency(uint registerSpace)
    {
        return (BindingFrequency)registerSpace;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<uint> GetRegisterSpaces()
    {
        return _registerSpaces;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<ResourceBindingSlot> GetSlots()
    {
        return _slots;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<ResourceBindingSlot> GetSlotsForSpace(uint registerSpace)
    {
        if (_slotsBySpaceRange.TryGetValue(registerSpace, out var range))
        {
            return _slotsBySpaceFlat.AsSpan(range.Start, range.Count);
        }
        return ReadOnlySpan<ResourceBindingSlot>.Empty;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        Instance.Dispose();
        GC.SuppressFinalize(this);
    }

    public sealed class Builder(LogicalDevice logicalDevice)
    {
        private readonly Dictionary<string, ResourceBindingDesc> _bindingDescMap = [];

        public Builder AddBinding(string name, ResourceBindingDesc desc)
        {
            _bindingDescMap.Add(name, desc);
            return this;
        }

        public ShaderRootSignature Build()
        {
            return new ShaderRootSignature(logicalDevice, _bindingDescMap);
        }
    }
}