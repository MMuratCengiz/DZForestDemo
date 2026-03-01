using System.Numerics;
using NiziKit.Graphics.Binding.Data;

namespace NiziKit.Graphics.Renderer.Renderer2D;

public class SpriteBatch
{
    private readonly GpuInstanceData[] _instances = new GpuInstanceData[GpuInstanceArray.MaxInstances];
    public int Count { get; private set; }

    public ReadOnlySpan<GpuInstanceData> AsSpan() => _instances.AsSpan(0, Count);

    public void Clear() => Count = 0;

    public void Add(Matrix4x4 model)
    {
        if (Count >= GpuInstanceArray.MaxInstances)
        {
            return;
        }

        _instances[Count++] = new GpuInstanceData
        {
            Model = model
        };
    }
}
