using System.Numerics;
using System.Runtime.InteropServices;

namespace NiziKit.Graphics.Binding.Data;

[StructLayout(LayoutKind.Sequential)]
public struct GpuLightData
{
    public Vector3 PositionOrDirection;
    public uint Type;
    public Vector3 Color;
    public float Intensity;
    public Vector3 SpotDirection;
    public float Radius;
    public float InnerConeAngle;
    public float OuterConeAngle;
    public int ShadowIndex;
    public float Pad0;
}