using System.Numerics;
using System.Runtime.InteropServices;

namespace Graphics.Binding.Data;

[StructLayout(LayoutKind.Sequential)]
public struct GpuShadowData
{
    public Matrix4x4 LightViewProjection;
    public Vector4 AtlasScaleOffset;
    public float Bias;
    public float NormalBias;
    public Vector2 Padding;
}