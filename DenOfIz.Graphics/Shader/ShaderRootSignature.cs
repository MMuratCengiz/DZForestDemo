using DenOfIz;
using Graphics.Shader.Binding;

namespace Graphics.Shader;

public sealed class ShaderRootSignature : IDisposable
{
    private readonly Dictionary<string, ResourceBindingDesc> _bindingDescMap;
    private readonly Dictionary<string, ResourceBindingSlot> _bindingSlotMap;
    private bool _disposed;

    public RootSignature Instance { get; }

    private ShaderRootSignature(LogicalDevice device, Dictionary<string, ResourceBindingDesc> descMap)
    {
        _bindingDescMap = descMap;
        RootSignatureDesc rootSignatureDesc = new()
        {
            ResourceBindings = ResourceBindingDescArray.Create(_bindingDescMap.Values.ToArray())
        };
        Instance = device.CreateRootSignature(rootSignatureDesc);
        _bindingSlotMap = _bindingDescMap.ToDictionary(x => x.Key, x => InitSlot(x.Key));
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

    public ResourceBindingSlot GetSlot(string name)
    {
        return _bindingSlotMap[name];
    }

    public ResourceBindingDesc GetBindingDesc(string name)
    {
        return _bindingDescMap[name];
    }

    public bool TryGetSlot(string name, out ResourceBindingSlot slot)
    {
        return _bindingSlotMap.TryGetValue(name, out slot);
    }

    public BindingFrequency GetFrequency(uint registerSpace)
    {
        return (BindingFrequency)registerSpace;
    }

    public IEnumerable<uint> GetRegisterSpaces()
    {
        return _bindingDescMap.Values.Select(x => x.RegisterSpace).Distinct();
    }

    public List<ResourceBindingSlot> GetSlots()
    {
        return _bindingSlotMap.Values.ToList();
    }

    public List<ResourceBindingSlot> GetSlotsForSpace(uint registerSpace)
    {
        return _bindingSlotMap.Values.Where(s => s.RegisterSpace == registerSpace).ToList();
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