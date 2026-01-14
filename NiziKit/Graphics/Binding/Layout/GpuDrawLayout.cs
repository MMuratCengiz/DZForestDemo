using DenOfIz;

namespace NiziKit.Graphics.Binding.Layout;

public class GpuDrawLayout : ILayout
{
    public static readonly BindingSlot Instances = BindingSlot.ConstantBuffer(0);
    public static readonly BindingSlot BoneMatrices = BindingSlot.ConstantBuffer(1);
    public static readonly FrequencySpace FrequencySpace = FrequencySpace.Draw;

    public BindGroupLayout Layout { get; }

    public GpuDrawLayout(LogicalDevice device)
    {
        var bindings = new List<BindingDesc>
        {
            new()
            {
                Binding = Instances.Binding,
                ArraySize = 1,
                Stages = (uint)(ShaderStageFlagBits.Vertex | ShaderStageFlagBits.Pixel),
                Descriptor = (uint)ResourceDescriptorFlagBits.UniformBuffer
            },
            new()
            {
                Binding = BoneMatrices.Binding,
                ArraySize = 1,
                Stages = (uint)ShaderStageFlagBits.Vertex,
                Descriptor = (uint)ResourceDescriptorFlagBits.UniformBuffer
            }
        };

        var desc = new BindGroupLayoutDesc
        {
            RegisterSpace = (uint)FrequencySpace,
            Bindings = BindingDescArray.Create(bindings.ToArray())
        };
        Layout = device.CreateBindGroupLayout(desc);
    }

    public void Dispose()
    {
        Layout.Dispose();
    }
}
