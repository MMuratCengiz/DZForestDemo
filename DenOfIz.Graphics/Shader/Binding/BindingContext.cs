using DenOfIz;

namespace Graphics.Shader.Binding;

public class BindingContext(LogicalDevice logicalDevice, ShaderRootSignature rootSignature)
{
    public LogicalDevice LogicalDevice { get; } = logicalDevice;
    public ShaderRootSignature RootSignature { get; } = rootSignature;

    public readonly List<ResourceBindingSlot> ResourceBindingSlots = rootSignature.GetSlots();

    public ResourceBindingSlot GetSlot(string name)
    {
        return rootSignature.GetSlot(name);
    }

    public CpuVisibleBufferView GetFreeCpuVisibleAddress(object bindingGroup, string name)
    {
        throw new NotImplementedException();
    }
}