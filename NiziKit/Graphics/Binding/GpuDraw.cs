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

    public static GpuDraw Get(int frameIndex, GameObject gameObject)
    {
        if (Instances.Count < GraphicsContext.NumFrames)
        {
            lock (Lock)
            {
                while (Instances.Count < GraphicsContext.NumFrames)
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

        return gpuDraws.GetOrAdd(gameObject, go => new GpuDraw(go));
    }

    public GpuDraw(GameObject go)
    {
        _gameObject = go;
        var bindGroupDesc = new BindGroupDesc
        {
            Layout = GraphicsContext.BindGroupLayoutStore.Draw
        };
        _bindGroups = new BindGroup[GraphicsContext.NumFrames];
        _instanceBuffer = new GpuBufferView[GraphicsContext.NumFrames];
        _boneMatricesBuffer = new GpuBufferView[GraphicsContext.NumFrames];
        for (var i = 0; i < GraphicsContext.NumFrames; i++)
        {
            _bindGroups[i] = GraphicsContext.Device.CreateBindGroup(bindGroupDesc);
            _instanceBuffer[i] = GraphicsContext.UniformBufferArena.Request(Marshal.SizeOf<GpuInstanceData>());
            _boneMatricesBuffer[i] = GraphicsContext.UniformBufferArena.Request(Marshal.SizeOf<Matrix4x4>());

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
            bg.CbvWithDesc(bindBoneMatricesBufferDesc);
            bg.EndUpdate();
        }
    }

    public BindGroup Get(int frameIndex)
    {
        var transform = _gameObject.WorldMatrix;
        var instanceData = new GpuInstanceData
        {
            Model = transform,
            BoneOffset = 0 // TODO
        };
        _instanceBuffer[frameIndex].Buffer.WriteData(instanceData);
        return _bindGroups[frameIndex];
    }
}