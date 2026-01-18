using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace NiziKit.Graphics.Binding.Data;

[StructLayout(LayoutKind.Sequential)]
public struct GpuInstanceArray
{
    public const int MaxInstances = 500;

    [InlineArray(MaxInstances)]
    public struct InstanceArray
    {
        private GpuInstanceData _element0;
    }

    public InstanceArray Instances;

    public void CopyFrom(ReadOnlySpan<GpuInstanceData> source)
    {
        var count = Math.Min(source.Length, MaxInstances);
        for (var i = 0; i < count; i++)
        {
            Instances[i] = source[i];
        }
    }
}
