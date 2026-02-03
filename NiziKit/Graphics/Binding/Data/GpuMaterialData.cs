using System.Numerics;
using System.Runtime.InteropServices;

namespace NiziKit.Graphics.Binding.Data;

[StructLayout(LayoutKind.Sequential)]
public struct GpuSurfaceData
{
    public Vector4 AlbedoColor;
    public Vector3 EmissiveColor;
    public float _pad0;
    public Vector2 UVScale;
    public Vector2 UVOffset;
    public float MetallicValue;
    public float RoughnessValue;
    public float EmissiveIntensity;
    public float HasAlbedoTexture;
    public float _padding1;
}
