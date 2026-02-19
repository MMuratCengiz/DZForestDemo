using System.Numerics;
using System.Runtime.InteropServices;

namespace NiziKit.Particles;

[StructLayout(LayoutKind.Sequential)]
public struct Particle
{
    public Vector4 Position; // xyz=position, w=lifetime remaining
    public Vector4 Velocity; // xyz=velocity, w=max lifetime
    public Vector4 Color;    // rgba base color
}
