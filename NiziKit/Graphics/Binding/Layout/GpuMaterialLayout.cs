using DenOfIz;

namespace NiziKit.Graphics.Binding.Layout;

public class GpuMaterialLayout : ILayout
{
    public static readonly BindingSlot Albedo = BindingSlot.ShaderResource(0);
    public static readonly BindingSlot Normal = BindingSlot.ShaderResource(1);
    public static readonly BindingSlot Roughness = BindingSlot.ShaderResource(2);
    public static readonly BindingSlot Metallic = BindingSlot.ShaderResource(3);
    public static readonly BindingSlot Material = BindingSlot.ConstantBuffer(4);
    public static readonly BindingSlot TextureSampler = BindingSlot.Sampler(0);
    public static readonly FrequencySpace FrequencySpace = FrequencySpace.Material;

    public BindGroupLayout Layout { get; }

    public GpuMaterialLayout(LogicalDevice device)
    {
        var bindings = new List<BindingDesc>();
        bindings.Add(TextureBindingDesc(Albedo.Binding));
        bindings.Add(TextureBindingDesc(Normal.Binding));
        bindings.Add(TextureBindingDesc(Roughness.Binding));
        bindings.Add(TextureBindingDesc(Metallic.Binding));
        bindings.Add(new BindingDesc
        {
            ArraySize = 1,
            Binding = Material.Binding,
            Stages = (uint)ShaderStageFlagBits.Pixel,
            Descriptor = (uint)ResourceDescriptorFlagBits.UniformBuffer,
        });
        bindings.Add(new BindingDesc
        {
            ArraySize = 1,
            Binding = TextureSampler.Binding,
            Stages = (uint)ShaderStageFlagBits.Pixel,
            Descriptor = (uint)ResourceDescriptorFlagBits.Sampler,
        });
        BindGroupLayoutDesc desc = new();
        desc.RegisterSpace = (uint)FrequencySpace;
        desc.Bindings = BindingDescArray.Create(bindings.ToArray());
        Layout = device.CreateBindGroupLayout(desc);
    }

    private static BindingDesc TextureBindingDesc(uint binding)
    {
        return new BindingDesc
        {
            Binding = binding,
            ArraySize = 1,
            Stages = (uint)ShaderStageFlagBits.Pixel,
            Descriptor = (uint)ResourceDescriptorFlagBits.Texture
        };
    }
}