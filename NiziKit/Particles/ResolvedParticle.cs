using System.Numerics;
using System.Runtime.InteropServices;

namespace NiziKit.Particles;

[StructLayout(LayoutKind.Sequential)]
public struct ResolvedParticle
{
    public Vector4 PositionAndSize; // xyz=position, w=size
    public Vector4 Color;           // rgba
}
