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
    private static readonly List<Dictionary<GameObject, GpuDraw>> Instances = new();

    private readonly BindGroup[] _bindGroups;
    private readonly GpuBufferView[] _dataBuffers;
    
    public static GpuDraw Get(GraphicsContext context, int frameIndex, GameObject gameObject)
    {
        if (Instances.Count < context.NumFrames)
        {
            for (var i = Instances.Count; i < context.NumFrames; i++)
            {
                Instances.Add([]);
            }
        }

        var gpuDraws = Instances[frameIndex];
        if (!gpuDraws.TryGetValue(gameObject, out var value))
        {
            value = new GpuDraw(context, gameObject);
            gpuDraws.Add(gameObject, value);
        }

        return value;
    }

    public BindGroup GetBindGroup(int frameIndex)
    {
        return _bindGroups[frameIndex];
    }
    
    public GpuDraw(GraphicsContext context, GameObject go)
    {
        var bindGroupDesc = new BindGroupDesc
        {
            Layout = context.BindGroupLayoutStore.Draw
        };
        _bindGroups = new BindGroup[context.NumFrames];
        _dataBuffers = new GpuBufferView[context.NumFrames];
        for (var i = 0; i < context.NumFrames; i++)
        {
            _bindGroups[i] = context.LogicalDevice.CreateBindGroup(bindGroupDesc);
            _dataBuffers[i] = context.UniformBufferArena.Request(Marshal.SizeOf<GpuDrawData>());

            var bg = _bindGroups[i];
            bg.BeginUpdate();
            var bindBufferDesc = new BindBufferDesc
            {
                Binding = GpuDrawLayout.Instances.Binding,
                Resource = _dataBuffers[i].Buffer,
                ResourceOffset = _dataBuffers[i].Offset
            };
            bg.CbvWithDesc(bindBufferDesc);
            bg.EndUpdate();
        }
    }

    public BindGroup Get(int frameIndex, GameObject gameObject)
    {
        var transform = gameObject.WorldMatrix;
        this._dataBuffers[frameIndex].Buffer.WriteData(transform);
        return _bindGroups[frameIndex];
    }
}