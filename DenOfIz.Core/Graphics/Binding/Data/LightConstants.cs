using System.Numerics;
using System.Runtime.InteropServices;

namespace DenOfIz.World.Graphics.Binding.Data;

public struct LightConstantsCapacity
{
    public const int MaxLights = 8;
    public const int MaxShadowLights = 4;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct LightConstants
{
    public fixed byte Lights[LightConstantsCapacity.MaxLights * 64];
    public fixed byte Shadows[LightConstantsCapacity.MaxShadowLights * 96];
    public Vector3 AmbientSkyColor;
    public uint NumLights;
    public Vector3 AmbientGroundColor;
    public float AmbientIntensity;
    public uint NumShadows;
    public uint Pad0;
    public uint Pad1;
    public uint Pad2;
}