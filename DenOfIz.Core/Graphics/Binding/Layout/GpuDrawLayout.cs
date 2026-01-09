namespace DenOfIz.World.Graphics.Binding.Layout;

public class GpuDrawLayout : ILayout
{
    public static readonly BindingSlot Instances = BindingSlot.ShaderResource(0);
    public static readonly BindingSlot BoneMatrices = BindingSlot.ShaderResource(1);
    public static readonly FrequencySpace FrequencySpace = FrequencySpace.Draw;

    public BindGroupLayout Layout { get; }
    public bool IncludesBones { get; }

    public GpuDrawLayout(LogicalDevice device, bool includeBones = false)
    {
        IncludesBones = includeBones;

        var bindings = new List<BindingDesc>
        {
            new()
            {
                Binding = Instances.Binding,
                ArraySize = 1,
                Stages = (uint)ShaderStageFlagBits.Vertex,
                Descriptor = (uint)ResourceDescriptorFlagBits.StructuredBuffer
            }
        };

        if (includeBones)
        {
            bindings.Add(new BindingDesc
            {
                Binding = BoneMatrices.Binding,
                ArraySize = 1,
                Stages = (uint)ShaderStageFlagBits.Vertex,
                Descriptor = (uint)ResourceDescriptorFlagBits.StructuredBuffer
            });
        }

        var desc = new BindGroupLayoutDesc
        {
            RegisterSpace = (uint)FrequencySpace,
            Bindings = BindingDescArray.Create(bindings.ToArray())
        };
        Layout = device.CreateBindGroupLayout(desc);
    }
}
