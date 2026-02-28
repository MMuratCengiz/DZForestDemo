using System.Numerics;
using System.Runtime.InteropServices;

namespace NiziKit.Graphics.Binding.Data;

public struct LightConstantsCapacity
{
    public const int MaxLights = 8;

    /// <summary>
    /// Total cascade slots across all shadow-casting directional lights.
    /// With NumCascades = 4 and one shadow light this equals 4.
    /// </summary>
    public const int MaxShadowCascades = 4;

    // Kept for backward-compatible references.
    public const int MaxShadowLights = MaxShadowCascades;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct LightConstants
{
    public fixed byte Lights[LightConstantsCapacity.MaxLights * 64];                  // 512 bytes
    public fixed byte Shadows[LightConstantsCapacity.MaxShadowCascades * 80];        // 320 bytes
    public Vector3 AmbientSkyColor;
    public uint NumLights;
    public Vector3 AmbientGroundColor;
    public float AmbientIntensity;
    public uint NumShadows;
    public uint Pad0;
    public uint Pad1;
    public uint Pad2;
}
