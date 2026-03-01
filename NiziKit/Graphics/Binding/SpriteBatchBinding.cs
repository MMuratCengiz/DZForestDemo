using DenOfIz;
using NiziKit.Graphics.Binding.Data;
using NiziKit.Graphics.Binding.Layout;
using NiziKit.Graphics.Buffers;
using NiziKit.Graphics.Renderer.Renderer2D;

namespace NiziKit.Graphics.Binding;

public class SpriteBatchBinding : ShaderBinding<SpriteBatch>
{
    private readonly UniformBuffer<GpuInstanceArray> _instanceBuffer;
    private readonly UniformBuffer<GpuBoneTransforms> _boneMatricesBuffer;

    private GpuInstanceArray _instanceData;

    public override BindGroupLayout Layout => GraphicsContext.BindGroupLayoutStore.Draw;
    public override bool RequiresCycling => true;

    public SpriteBatchBinding()
    {
        _instanceBuffer = new UniformBuffer<GpuInstanceArray>(true);
        _boneMatricesBuffer = new UniformBuffer<GpuBoneTransforms>(true);

        var boneData = GpuBoneTransforms.Identity();
        for (var i = 0; i < NumBindGroups; i++)
        {
            _boneMatricesBuffer.Write(in boneData);
        }

        for (var i = 0; i < NumBindGroups; i++)
        {
            var instanceView = _instanceBuffer[i];
            var boneView = _boneMatricesBuffer[i];

            var bg = BindGroups[i];
            bg.BeginUpdate();
            bg.CbvWithDesc(new BindBufferDesc
            {
                Binding = GpuDrawLayout.Instances.Binding,
                Resource = instanceView.Buffer,
                ResourceOffset = instanceView.Offset
            });
            bg.CbvWithDesc(new BindBufferDesc
            {
                Binding = GpuDrawLayout.BoneMatrices.Binding,
                Resource = boneView.Buffer,
                ResourceOffset = boneView.Offset
            });
            bg.EndUpdate();
        }
    }

    protected override void OnUpdate(SpriteBatch batch)
    {
        var instances = batch.AsSpan();
        var count = Math.Min(instances.Length, GpuInstanceArray.MaxInstances);

        for (var i = 0; i < count; i++)
        {
            _instanceData.Instances[i] = instances[i];
        }

        _instanceBuffer.Write(in _instanceData);
    }
}
