using DenOfIz;

namespace NiziKit.Graphics.Binding.Layout;

public class GpuCameraLayout : ILayout
{
    public static readonly BindingSlot Camera = BindingSlot.ConstantBuffer(0);
    public static readonly BindingSlot Lights = BindingSlot.ConstantBuffer(1);
    public static readonly BindingSlot ShadowAtlas = BindingSlot.ShaderResource(2);
    public static readonly BindingSlot ShadowSampler = BindingSlot.Sampler(3);
    public static readonly FrequencySpace FrequencySpace = FrequencySpace.Camera;

    public BindGroupLayout Layout { get; }

    public GpuCameraLayout(LogicalDevice device)
    {
        var bindings = new List<BindingDesc>
        {
            new()
            {
                Binding = Camera.Binding,
                ArraySize = 1,
                Stages = (uint)(ShaderStageFlagBits.Vertex | ShaderStageFlagBits.Pixel),
                Descriptor = (uint)ResourceDescriptorFlagBits.UniformBuffer
            },
            new()
            {
                Binding = Lights.Binding,
                ArraySize = 1,
                Stages = (uint)(ShaderStageFlagBits.Vertex | ShaderStageFlagBits.Pixel),
                Descriptor = (uint)ResourceDescriptorFlagBits.UniformBuffer
            },
            new()
            {
                Binding = ShadowAtlas.Binding,
                ArraySize = 1,
                Stages = (uint)ShaderStageFlagBits.Pixel,
                Descriptor = (uint)ResourceDescriptorFlagBits.Texture
            },
            new()
            {
                Binding = ShadowSampler.Binding,
                ArraySize = 1,
                Stages = (uint)ShaderStageFlagBits.Pixel,
                Descriptor = (uint)ResourceDescriptorFlagBits.Sampler
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
