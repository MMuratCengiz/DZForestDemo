using DenOfIz;

namespace NiziKit.Graphics.Binding;

public readonly struct BindingSlot(ResourceBindingType type, uint binding)
{
    public readonly ResourceBindingType Type = type;
    public readonly uint Binding = binding;
    
    public static BindingSlot ConstantBuffer(uint binding) =>
        new BindingSlot(ResourceBindingType.ConstantBuffer, binding);
    
    public static BindingSlot ShaderResource(uint binding) =>
        new BindingSlot(ResourceBindingType.ShaderResource, binding);
    
    public static BindingSlot UnorderedAccess(uint binding) =>
        new BindingSlot(ResourceBindingType.UnorderedAccess, binding);
    
    public static BindingSlot Sampler(uint binding) =>
        new BindingSlot(ResourceBindingType.Sampler, binding);

    public BindingDesc BindingDesc()
    {
        return new BindingDesc
        {
            Binding = Binding,
            ArraySize = 1
        };
    }
}