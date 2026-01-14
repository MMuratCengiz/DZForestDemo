using System.Collections.Concurrent;
using System.Numerics;
using System.Runtime.InteropServices;
using DenOfIz;
using NiziKit.Application;
using NiziKit.Core;
using NiziKit.Graphics.Binding.Data;
using NiziKit.Graphics.Binding.Layout;

namespace NiziKit.Graphics.Binding;

public enum GpuDrawType : byte
{
    Static,
    Skinned
}

public readonly struct GpuDraw
{
    // Per frame + per draw index
    private static readonly List<ConcurrentDictionary<GameObject, GpuDraw>> Instances = new();
    private static readonly object Lock = new();

    private readonly GameObject _gameObject;
    private readonly BindGroup[] _bindGroups;
    private readonly GpuBufferView[] _instanceBuffer;
    private readonly GpuBufferView[] _boneMatricesBuffer;

    public static GpuDraw Get(GraphicsContext context, int frameIndex, GameObject gameObject)
    {
        if (Instances.Count < context.NumFrames)
        {
            lock (Lock)
            {
                while (Instances.Count < context.NumFrames)
                {
                    Instances.Add(new ConcurrentDictionary<GameObject, GpuDraw>());
                }
            }
        }

        var gpuDraws = Instances[frameIndex];
        if (gpuDraws.TryGetValue(gameObject, out var existing))
        {
            return existing;
        }

        return gpuDraws.GetOrAdd(gameObject, go => new GpuDraw(context, go));
    }

    public BindGroup GetBindGroup(int frameIndex)
    {
        return _bindGroups[frameIndex];
    }

    public GpuDraw(GraphicsContext context, GameObject go)
    {
        _gameObject = go;
        var bindGroupDesc = new BindGroupDesc
        {
            Layout = context.BindGroupLayoutStore.Draw
        };
        _bindGroups = new BindGroup[context.NumFrames];
        _instanceBuffer = new GpuBufferView[context.NumFrames];
        _boneMatricesBuffer = new GpuBufferView[context.NumFrames];
        for (var i = 0; i < context.NumFrames; i++)
        {
            _bindGroups[i] = context.LogicalDevice.CreateBindGroup(bindGroupDesc);
            _instanceBuffer[i] = context.UniformBufferArena.Request(Marshal.SizeOf<GpuDrawData>());
            _boneMatricesBuffer[i] = context.UniformBufferArena.Request(Marshal.SizeOf<Matrix4x4>());

            var bg = _bindGroups[i];
            bg.BeginUpdate();
            var bindInstancesBufferDesc = new BindBufferDesc
            {
                Binding = GpuDrawLayout.Instances.Binding,
                Resource = _instanceBuffer[i].Buffer,
                ResourceOffset = _instanceBuffer[i].Offset
            };
            bg.CbvWithDesc(bindInstancesBufferDesc);
            var bindBoneMatricesBufferDesc = new BindBufferDesc
            {
                Binding = GpuDrawLayout.BoneMatrices.Binding,
                Resource = _boneMatricesBuffer[i].Buffer,
                ResourceOffset = _boneMatricesBuffer[i].Offset
            };
            bg.EndUpdate();
        }
    }

    public BindGroup Get(int frameIndex)
    {
        var transform = _gameObject.WorldMatrix;
        _instanceBuffer[frameIndex].Buffer.WriteData(transform);
        return _bindGroups[frameIndex];
    }
}