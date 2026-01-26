using System.Numerics;
using System.Runtime.InteropServices;

namespace NiziKit.Graphics.Binding.Data;

[StructLayout(LayoutKind.Sequential)]
public struct GpuSurfaceData
{
    public float MetallicValue;
    public float RoughnessValue;
    public Vector2 UVScale;
    public Vector2 UVOffset;
    public float EmissiveIntensity;
    public float _padding1;
    public Vector4 AlbedoColor;
    public Vector3 EmissiveColor;
    public float _padding2;
}
