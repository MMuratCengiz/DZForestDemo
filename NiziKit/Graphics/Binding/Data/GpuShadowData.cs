using System.Numerics;
using System.Runtime.InteropServices;

namespace NiziKit.Graphics.Binding.Data;

/// <summary>
/// Per-cascade shadow data uploaded to the GPU (80 bytes, matches ShadowData in View.hlsl).
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct GpuShadowData
{
    public Matrix4x4 LightViewProjection; // 64 bytes
    public float SplitDistance; //  4 bytes – linear view-space depth at this cascade's far edge
    public float Bias; //  4 bytes
    public float NormalBias; //  4 bytes
    public float Pad; //  4 bytes
}
