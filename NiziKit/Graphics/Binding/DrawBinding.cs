using System.Numerics;
using DenOfIz;
using NiziKit.Core;
using NiziKit.Graphics.Binding.Data;
using NiziKit.Graphics.Binding.Layout;
using NiziKit.Graphics.Buffers;

namespace NiziKit.Graphics.Binding;

public class DrawBinding : ShaderBinding<GameObject>
{
    private readonly UniformBuffer<GpuInstanceData> _instanceBuffer;
    private readonly UniformBuffer<Matrix4x4> _boneMatricesBuffer;

    public override BindGroupLayout Layout => GraphicsContext.BindGroupLayoutStore.Draw;
    public override bool RequiresCycling => true;

    public DrawBinding()
    {
        _instanceBuffer = new UniformBuffer<GpuInstanceData>(true);
        _boneMatricesBuffer = new UniformBuffer<Matrix4x4>(true);

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

    protected override void OnUpdate(GameObject target)
    {
        var instanceData = new GpuInstanceData
        {
            Model = target.WorldMatrix,
            BoneOffset = 0
        };
        _instanceBuffer.Write(in instanceData);
    }
}
