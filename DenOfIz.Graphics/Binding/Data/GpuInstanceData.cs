using System.Numerics;
using System.Runtime.InteropServices;

namespace Graphics.Binding.Data;

[StructLayout(LayoutKind.Sequential)]
public struct GpuInstanceData
{
    public Matrix4x4 Model;
    public Vector4 BaseColor;
    public float Metallic;
    public float Roughness;
    public float AmbientOcclusion;
    public uint UseAlbedoTexture;
    public uint BoneOffset;
    public uint _Pad0;
    public uint _Pad1;
    public uint _Pad2;
}