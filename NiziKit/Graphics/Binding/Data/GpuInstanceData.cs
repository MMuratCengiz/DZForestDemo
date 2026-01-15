using System.Numerics;
using System.Runtime.InteropServices;

namespace NiziKit.Graphics.Binding.Data;

[StructLayout(LayoutKind.Sequential)]
public struct GpuInstanceData
{
    public Matrix4x4 Model;
    public uint BoneOffset;
}