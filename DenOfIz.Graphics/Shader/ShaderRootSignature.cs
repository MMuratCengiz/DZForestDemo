using DenOfIz;

namespace Graphics.Shader;

public class ShaderRootSignature
{
    private readonly Dictionary<string, ResourceBindingDesc> _bindingDescMap;
    private readonly Dictionary<string, ResourceBindingSlot> _bindingSlotMap;

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

    public class Builder(LogicalDevice device)
    {
        private readonly Dictionary<string, ResourceBindingDesc> _bindingDescMap = [];
        
        public Builder AddBinding(string name, ResourceBindingDesc desc)
        {
            _bindingDescMap.Add(name, desc);
            return this;
        }

        public ShaderRootSignature Build(LogicalDevice logicalDevice)
        {
            return new ShaderRootSignature(logicalDevice, _bindingDescMap);
        }
    }

    public List<ResourceBindingSlot> GetSlots()
    {
        return _bindingSlotMap.Values.ToList();
    }
}