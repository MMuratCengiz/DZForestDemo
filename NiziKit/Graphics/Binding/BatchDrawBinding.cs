using DenOfIz;
using NiziKit.Animation;
using NiziKit.Graphics.Binding.Data;
using NiziKit.Graphics.Binding.Layout;
using NiziKit.Graphics.Buffers;

namespace NiziKit.Graphics.Binding;

public class BatchDrawBinding : ShaderBinding<RenderBatch>
{
    private readonly UniformBuffer<GpuInstanceArray> _instanceBuffer;
    private readonly UniformBuffer<GpuBoneTransforms> _boneMatricesBuffer;

    private GpuInstanceArray _instanceData;
    private GpuBoneTransforms _boneData;

    public override BindGroupLayout Layout => GraphicsContext.BindGroupLayoutStore.Draw;
    public override bool RequiresCycling => true;

    public BatchDrawBinding()
    {
        _instanceBuffer = new UniformBuffer<GpuInstanceArray>(true);
        _boneMatricesBuffer = new UniformBuffer<GpuBoneTransforms>(true);
        _boneData = GpuBoneTransforms.Identity();

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

    protected override void OnUpdate(RenderBatch batch)
    {
        var objects = batch.AsSpan();
        var count = Math.Min(objects.Length, GpuInstanceArray.MaxInstances);
        uint boneOffset = 0;

        for (var i = 0; i < count; i++)
        {
            ref readonly var obj = ref objects[i];

            _instanceData.Instances[i] = new GpuInstanceData
            {
                Model = obj.Owner.WorldMatrix,
                BoneOffset = boneOffset
            };

            var animator = obj.Animator;
            if (animator is { BoneCount: > 0 })
            {
                var boneCount = Math.Min(animator.BoneCount, GpuBoneTransforms.MaxBones - (int)boneOffset);
                var bones = animator.BoneMatrices;
                for (var b = 0; b < boneCount; b++)
                {
                    _boneData.Bones[(int)boneOffset + b] = bones[b];
                }
                boneOffset += (uint)boneCount;
            }
        }

        _instanceBuffer.Write(in _instanceData);
        _boneMatricesBuffer.Write(in _boneData);
    }
}
